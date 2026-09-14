"""Model-only art pass. Called after the original corrections, before FBX export.

Retain every object name, origin and material slot: Unity uses these for prefab
overrides and the animated snake face. New details join existing mesh objects.
"""
import math
import bpy
import bmesh
from mathutils import Vector


def update(obj, vertices, faces, append=False):
    """Replace/add model-space geometry without moving the object's pivot."""
    inverse = obj.matrix_world.inverted()
    points = [inverse @ Vector(p) for p in vertices]
    old = obj.data
    offset = len(old.vertices) if append else 0
    if append:
        points = [v.co.copy() for v in old.vertices] + points
        faces = [tuple(p.vertices) for p in old.polygons] + [tuple(i + offset for i in f) for f in faces]
    mesh = bpy.data.meshes.new(old.name + ' sculpt')
    mesh.from_pydata(points, [], faces)
    mesh.update()
    for mat in old.materials:
        mesh.materials.append(mat)
    if append:
        for i, p in enumerate(old.polygons):
            mesh.polygons[i].material_index = p.material_index
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(mesh)
    bm.free()
    for p in mesh.polygons:
        p.use_smooth = True
    # Stable UVs for generated parts; preserve original UVs on appended geometry.
    uv = mesh.uv_layers.new(name='UVMap')
    for p in mesh.polygons:
        for li in p.loop_indices:
            v = mesh.vertices[mesh.loops[li].vertex_index].co
            uv.data[li].uv = (v.x, v.z)
    if append and old.uv_layers:
        for i, item in enumerate(old.uv_layers.active.data):
            uv.data[i].uv = item.uv
    obj.data = mesh
    if not old.users:
        bpy.data.meshes.remove(old)


def tube(obj, path, radius, append=False, sides=16):
    pts = [Vector(p) for p in path]
    radii = list(radius) if isinstance(radius, (list, tuple)) else [radius]*len(pts)
    # Interpolated centerlines avoid visible elbows on handles and stems.
    if len(pts) > 2:
        smooth, sizes = [], []
        for i in range(len(pts)-1):
            p0, p1 = pts[max(0,i-1)], pts[i]
            p2, p3 = pts[i+1], pts[min(len(pts)-1,i+2)]
            for step in range(4):
                t = step/4
                smooth.append(.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t))
                sizes.append(radii[i]*(1-t)+radii[i+1]*t)
        pts, radii = smooth+[pts[-1]], sizes+[radii[-1]]
    vertices = []
    for i, p in enumerate(pts):
        tangent = pts[min(i+1, len(pts)-1)] - pts[max(0, i-1)]
        rotation = tangent.to_track_quat('Z', 'Y')
        r = radii[i]
        for j in range(sides):
            a = math.tau*j/sides
            vertices.append(p + rotation @ Vector((r*math.cos(a), r*math.sin(a), 0)))
    faces = [(i*sides+j, i*sides+(j+1)%sides, (i+1)*sides+(j+1)%sides, (i+1)*sides+j)
             for i in range(len(pts)-1) for j in range(sides)]
    faces += [tuple(reversed(range(sides))), tuple((len(pts)-1)*sides+j for j in range(sides))]
    update(obj, vertices, faces, append)


def leaf(obj, base, tip, width, curl=.04, append=False, thickness=.018):
    """Closed, pointed, cupped leaf with a soft central ridge."""
    base, tip = Vector(base), Vector(tip)
    axis = tip-base
    side = axis.cross(Vector((0, 0, 1)))
    if side.length < .01:
        side = Vector((1, 0, 0))
    side.normalize()
    normal = side.cross(axis).normalized()
    vertices = []
    rows, sides = 18, 12
    for i in range(rows+1):
        t = i/rows
        w = max(.001, math.sin(math.pi*t)**.85) * width
        center = base + axis*t + normal*(curl*math.sin(math.pi*t) + curl*.6*t*t)
        for j in range(sides):
            a = math.tau*j/sides
            vertices.append(center + side*(w*math.cos(a)) + normal*(thickness*math.sin(a)*math.sin(math.pi*t)))
    faces = [(i*sides+j, i*sides+(j+1)%sides, (i+1)*sides+(j+1)%sides, (i+1)*sides+j)
             for i in range(rows) for j in range(sides)]
    faces += [tuple(reversed(range(sides))), tuple(rows*sides+j for j in range(sides))]
    update(obj, vertices, faces, append)


def deform(obj, fn):
    for vertex in obj.data.vertices:
        vertex.co = fn(vertex.co.copy())
    obj.data.update()


def rounded_box(obj, amount=.007):
    # Relax existing bevel transitions without multiplying repeated board geometry.
    bpy.context.view_layer.objects.active = obj
    mod = obj.modifiers.new('Relax bevel transitions', 'SMOOTH')
    mod.factor = amount*.35
    mod.iterations = 1
    bpy.ops.object.modifier_apply(modifier=mod.name)


def sculpt(name, objects):
    bpy.context.view_layer.update()
    def find(label):
        return [o for o in objects if o.name.split('.')[0] == label]
    def one(label):
        return find(label)[0]
    def foliage_variant(obj, label, color):
        mat = bpy.data.materials.get(label)
        if mat is None:
            mat = bpy.data.materials['Foliage'].copy()
            mat.name = label
            mat.diffuse_color = (*color,1)
            mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = (*color,1)
        obj.data.materials[0] = mat

    if name == 'Apple':
        def flesh(v):
            a = math.atan2(v.y, v.x)
            t = (v.z-.36)/.33
            lobes = 1 + .035*math.cos(5*a+.3)*(abs(t)**1.5)
            v.x *= lobes
            v.y *= lobes
            return v
        deform(one('Apple flesh'), flesh)
        tube(one('Stem'), [(0,0,.60),(-.008,0,.66),(-.023,0,.73),(-.017,.008,.79)], [.034,.035,.031,.025])
        leaf(one('Leaf'), (-.018,0,.725), (.385,.035,.855), .103, .044, thickness=.021)

    elif name == 'SnakeHead':
        # Eye rig pivots and muzzle surface stay fixed for mouth animation.
        # The broad lower skull and larger gaze give the hero a softer expression.
        deform(one('Head'), lambda v: Vector((v.x*(1+.045*math.exp(-((v.z+.07)/.14)**2)),v.y,v.z)))
        whites = find('Eye white')
        for ob in whites:
            deform(ob, lambda v: Vector((v.x*1.075,v.y*1.035,v.z*1.075)))
        for ob in find('Pupil'):
            shift = -.012 if ob.location.x > 0 else .012
            deform(ob, lambda v, shift=shift: Vector((v.x*1.17+shift,v.y*1.05-.004,v.z*1.10)))
        for i, ob in enumerate(find('Eyebrow')):
            eye = whites[i].location
            path = []
            for j in range(13):
                x = -.14+j*.28/12
                z = eye.z+.22575*math.sqrt(max(0,1-(x/.1935)**2-(.048/.15525)**2))+.011
                path.append((eye.x+x,eye.y-.048,z))
            tube(ob,path,[.010+.015*math.sin(math.pi*j/12)**.5 for j in range(13)],sides=12)
        for ob in find('Cheek'):
            side = 1 if ob.location.x > 0 else -1
            deform(ob, lambda v,side=side: Vector((v.x*1.17, v.y*1.06, v.z*1.13+side*v.x*.10)))
        for ob in find('Eye glint'):
            shift = -.012 if ob.location.x > 0 else .012
            deform(ob, lambda v,shift=shift: Vector((v.x*1.16+shift, v.y-.006, v.z*1.16)))
            # A second tiny sparkle joins the same mesh and follows the existing eye rig.
            center = ob.location+Vector((.045+shift,-.012,-.080))
            bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=10)
            temp = bpy.context.object
            vertices = [center+Vector((v.co.x*.013,v.co.y*.009,v.co.z*.016)) for v in temp.data.vertices]
            faces = [tuple(p.vertices) for p in temp.data.polygons]
            bpy.data.objects.remove(temp,do_unlink=True)
            update(ob,vertices,faces,True)
        for ob in find('Tiny tooth'):
            rounded_box(ob, .16)

    elif name == 'SnakeBody':
        # Raise side markings onto the actual ellipsoid; previously almost buried.
        for ob in find('Side freckle'):
            x = ob.location.x
            vertices = []
            for ring in range(6):
                r = max(.001,ring/5)
                for j in range(24):
                    a = j*math.tau/24
                    px, py = x+.027*r*math.cos(a), -.03+.07*r*math.sin(a)
                    z = .3+.29*math.sqrt(max(0,1-(px/.37)**2-(py/.4)**2))+.004
                    vertices.append((px,py,z))
            faces = [(i*24+j,i*24+(j+1)%24,(i+1)*24+(j+1)%24,(i+1)*24+j) for i in range(5) for j in range(24)]
            update(ob,vertices,faces)

    elif name == 'SnakeTail':
        # A tapered swept tip overlaps the tail deeply and ends in a soft point.
        tube(one('Tail tip'), [(0,.20,.22),(.028,.29,.22),(.067,.38,.215),(.10,.46,.205),(.12,.515,.21)],
             [.19,.155,.108,.058,.008], sides=24)

    elif name in ('Tile', 'Planter', 'RimLong', 'RimShort'):
        for ob in objects:
            rounded_box(ob, .12)

    elif name == 'Rock':
        ob = one('River stone')
        # Rounded river pebble with restrained broad asymmetry, not a faceted gem.
        bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=20)
        temp = bpy.context.object
        verts = []
        for v in temp.data.vertices:
            p = v.co
            wobble = 1+.035*math.sin(p.x*4+p.y*3)*math.sin(p.z*3+1)
            verts.append((p.x*.445*wobble, p.y*.345*wobble, .28+p.z*.36*wobble))
        faces = [tuple(p.vertices) for p in temp.data.polygons]
        bpy.data.objects.remove(temp, do_unlink=True)
        update(ob, verts, faces)

    elif name in ('Flower', 'Daisy'):
        height = .54 if name == 'Flower' else .65
        stem = one('Stem')
        tube(stem, [(0,0,0),(-.025,0,height*.3),(-.028,.005,height*.65),(0,0,height)], .024)
        leaf(stem, (-.025,0,height*.25), (-.23,.045,height*.53), .065, .025, True)
        leaf(stem, (-.025,0,height*.48), (.22,.012,height*.72), .060, .024, True)
        for i, ob in enumerate(find('Petal' if name == 'Flower' else 'Daisy petal')):
            angle = i*math.tau/(5 if name == 'Flower' else 9)
            def cup(v, ob=ob, angle=angle):
                if name == 'Flower':
                    radial = v.x*math.cos(angle)+v.y*math.sin(angle)
                else:
                    radial = v.x
                v.z += .23*radial + .7*radial*radial
                return v
            deform(ob, cup)

    elif name == 'FlowerPot':
        for i, ob in enumerate(find('Sprout')):
            a = i*math.tau/5
            leaf(ob, (.09*math.cos(a),.09*math.sin(a),.46),
                 (.31*math.cos(a),.31*math.sin(a),.93 + .04*math.sin(a)), .11, .052, thickness=.034)

    elif name == 'Tulip':
        leaf(one('Leaf'), (0,0,.12), (.23,.025,.55), .090, .035, thickness=.016)
        stem = one('Stem')
        leaf(stem, (0,0,.20), (-.18,.03,.46), .055, .025, True)
        # Broader shoulders and tapered scalloped petals read as a flower cup.
        for ob in find('Tulip petal'):
            deform(ob, lambda v: Vector((v.x*(1-.18*v.z/.2), v.y*(1-.18*v.z/.2), v.z*.92)))

    elif name == 'Lavender':
        for i, ob in enumerate(find('Lavender')):
            # Tiny fluted calyces break up the stack-of-balls silhouette.
            def fluted(v, phase=i*.55):
                factor = 1+.08*math.cos(math.atan2(v.y,v.x)*5+phase)
                return Vector((v.x*factor,v.y*factor,v.z))
            deform(ob, fluted)
        for i, ob in enumerate(find('Stem')):
            x = (i-1)*.13
            leaf(ob, (x*.25,0,.22), (x-.11,.02,.43), .031, .015, True, .008)
            leaf(ob, (x*.4,0,.32), (x+.12,.01,.5), .033, .017, True, .008)

    elif name == 'Bush':
        for i, ob in enumerate(find('Bush lobe')):
            if i == 1: foliage_variant(ob,'FoliageSun',(.37,.64,.23))
            # Keep the flower-bearing upper hemisphere unchanged so blossoms stay seated.
            def lobe(v, phase=i):
                weight = max(0, min(1, -v.z*5))
                f = 1+.025*weight*math.sin(math.atan2(v.y,v.x)*5+phase)
                return Vector((v.x*f, v.y*f, v.z))
            deform(ob, lobe)

    elif name == 'Tree':
        for i, ob in enumerate(find('Puffy canopy')):
            if i == 0: foliage_variant(ob,'FoliageSun',(.37,.64,.23))
            if i == 3: foliage_variant(ob,'FoliageShade',(.24,.49,.16))
            def crown(v, phase=i):
                f = 1+.032*math.sin(v.x*4+phase)*math.sin(v.y*4+.7)*max(0,v.z)
                return Vector((v.x*f,v.y*f,v.z*f))
            deform(ob, crown)
        trunk = one('Trunk')
        # Flared root foot grounds the toy tree; taper disappears inside canopy.
        deform(trunk, lambda v: Vector((v.x*(1+.32*max(0,-v.z/.95)**5), v.y*(1+.32*max(0,-v.z/.95)**5), v.z)))
        foliage = find('Puffy canopy')[0]
        for i, ob in enumerate(find('Garden apple')):
            center = ob.location
            deform(ob, lambda v: Vector((v.x*(1+.04*math.cos(math.atan2(v.y,v.x)*5)),v.y*(1+.04*math.cos(math.atan2(v.y,v.x)*5)),v.z)))
            tube(trunk, [center+Vector((0,0,.105)),center+Vector((-.013,0,.18))], .016, True)
            leaf(foliage, center+Vector((0,0,.135)), center+Vector((.14,.025,.20)), .042, .015, True, .008)

    elif name == 'Gnome':
        # Curl the crown without moving the hat base or enlarging the character.
        hat = one('Pointy hat')
        deform(hat, lambda v: Vector((v.x-.075*(v.z/.77)**4, v.y, v.z-.07*(v.z/.77)**6)))
        for i, ob in enumerate(find('Fluffy beard')):
            deform(ob, lambda v: Vector((v.x*(1+.12*v.z/.23),v.y,v.z)))
        # Three rounded buttons use existing dark eye mesh/material, no new renderer.
        eye = one('Happy eye')
        for z in [.43,.53,.63]:
            tube(eye, [(0,-.302,z),(0,-.321,z)], .026, True)

    elif name == 'WateringCan':
        # Fuller curved spout; retains original endpoints and rose orientation.
        tube(one('Spout'), [(.30,0,.26),(.44,0,.31),(.57,0,.41),(.71,0,.53),(.83,0,.62)], [.095,.095,.09,.086,.085], sides=24)

    elif name == 'Spade':
        blade = one('Spade blade')
        deform(blade, lambda v: Vector((v.x,v.y+.06*(v.x/.19)**2,v.z)))
        # Broad D handle with a rounded grip; more readable than the thin ring.
        tube(one('Spade grip'), [(0,0,1.565),(-.10,0,1.58),(-.15,0,1.69),(-.145,0,1.79),(-.10,0,1.83),
                                (0,0,1.835),(.10,0,1.83),(.145,0,1.79),(.15,0,1.69),(.10,0,1.58),(0,0,1.565)], .042, sides=20)

    elif name == 'Rake':
        for ob in find('Rake tooth'):
            x = ob.location.x
            tube(ob, [(x,0,.15),(x,-.09,.14),(x,-.18,.11),(x,-.23,.065)], [.031,.031,.029,.02])
        bar = one('Rake crossbar')
        tube(bar, [(-.35,0,.15),(.35,0,.15)], .048)
        handle = one('Rake handle')
        # Rounded wooden end cap belongs to the existing handle mesh.
        tube(handle, [(0,0,1.63),(0,0,1.72),(0,0,1.77)], [.061,.061,.039], True)

    bpy.context.view_layer.update()
