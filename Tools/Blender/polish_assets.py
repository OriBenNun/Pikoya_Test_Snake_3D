"""Individually authored corrections applied before model export.

Keep object names and material slots stable so Unity prefab overrides survive.
"""
import bpy
import math
from mathutils import Vector


def replace_mesh(obj, vertices, faces, smooth=True):
    old = obj.data
    mesh = bpy.data.meshes.new(old.name + ' polished')
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    for material in old.materials:
        mesh.materials.append(material)
    obj.data = mesh
    obj.rotation_euler = (0, 0, 0)
    obj.scale = (1, 1, 1)
    for polygon in mesh.polygons:
        polygon.use_smooth = smooth
    if old.users == 0:
        bpy.data.meshes.remove(old)


def lathe(obj, profile, segments=48):
    vertices = [(r*math.cos(j*math.tau/segments), r*math.sin(j*math.tau/segments), z)
                for r,z in profile for j in range(segments)]
    faces = []
    for i in range(len(profile)-1):
        for j in range(segments):
            a=i*segments+j; b=i*segments+(j+1)%segments
            faces.append((a,b,b+segments,a+segments))
    replace_mesh(obj,vertices,faces)


def top_patch(obj, center, radii, xy, patch):
    vertices=[]
    for ring in range(9):
        r=max(.0001,ring/8)
        for j in range(48):
            a=j*math.tau/48
            x=xy[0]+patch[0]*r*math.cos(a)
            y=xy[1]+patch[1]*r*math.sin(a)
            z=center[2]+radii[2]*math.sqrt(max(0,1-((x-center[0])/radii[0])**2-((y-center[1])/radii[1])**2))+.004
            vertices.append((x,y,z))
    faces=[(i*48+j,i*48+(j+1)%48,(i+1)*48+(j+1)%48,(i+1)*48+j) for i in range(8) for j in range(48)]
    replace_mesh(obj,vertices,[tuple(reversed(face)) for face in faces])
    obj.location=(0,0,0)


def polish(name, objects):
    def find(prefix): return [o for o in objects if o.name.split('.')[0] == prefix]
    if name == 'Apple':
        flesh=find('Apple flesh')
        profile=[]
        for i in range(49):
            a=math.pi*i/48
            profile.append((max(.0001,.38*math.sin(a)*(1+.08*math.cos(a))), .36+.33*math.cos(a)-.075*math.exp(-(a/.32)**2)))
        lathe(flesh[0],list(reversed(profile)),64)
        flesh[0].location=(0,0,0)
        objects.remove(flesh[1]); bpy.data.objects.remove(flesh[1],do_unlink=True)
        find('Stem')[0].location.z=.68
        find('Leaf')[0].location.z=.74
    elif name == 'SnakeBody':
        top_patch(find('Dorsal spot')[0],(0,0,.3),(.37,.4,.29),(0,0),(.18,.23))
        belly=find('Belly')[0]
        belly.scale=(.96,.96,1.2)
    elif name == 'SnakeTail':
        top_patch(find('Tail spot')[0],(0,0,.23),(.29,.39,.23),(0,0),(.14,.2))
        # Blend the tip into the body instead of attaching a separate ball.
        tip=find('Tail tip')[0]
        tip.location=(.045,.27,.20); tip.scale=(.9,1.1,.9)
    elif name == 'SnakeHead':
        for o in find('Eyebrow'): o.location.z-=.025
        for o in find('Freckle'):
            xy=(o.location.x,o.location.y)
            top_patch(o,(0,0,.4),(.44,.47,.36),xy,(.033,.04))
        # A continuous smile follows the muzzle surface; no buried middle beads.
        smiles=find('Smile')
        curve=bpy.data.curves.new('Continuous smile','CURVE')
        curve.dimensions='3D'; curve.bevel_depth=.012; curve.bevel_resolution=3
        spline=curve.splines.new('POLY'); spline.points.add(40)
        for i,p in enumerate(spline.points):
            x=(i-20)*.0105; z=.225+.65*x*x
            y=-.31-.24*math.sqrt(1-(x/.38)**2-((z-.29)/.2)**2)-.004
            p.co=(x,y,z,1)
        ob=bpy.data.objects.new('Smile curve',curve); bpy.context.collection.objects.link(ob)
        bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active=ob
        bpy.ops.object.convert(target='MESH')
        mesh=ob.data.copy(); mesh.materials.append(smiles[0].data.materials[0])
        smiles[0].data=mesh; smiles[0].location=(0,0,0)
        bpy.data.objects.remove(ob,do_unlink=True)
        for o in smiles[1:]: objects.remove(o); bpy.data.objects.remove(o,do_unlink=True)
        for o in find('Tiny tooth'): o.location.y=-.551
        find('Tongue')[0].location.y=-.55
    elif name == 'Gnome':
        hat=find('Pointy hat')[0]
        vertices=[]
        for i in range(17):
            t=i/16
            for j in range(48):
                a=j*math.tau/48; r=max(.001,.345*(1-t)**.85)
                vertices.append((r*math.cos(a)-.13*t*t,r*math.sin(a),.77*t))
        faces=[(i*48+j,i*48+(j+1)%48,(i+1)*48+(j+1)%48,(i+1)*48+j) for i in range(16) for j in range(48)]
        faces.append(tuple(reversed(range(48))))
        replace_mesh(hat,vertices,faces); hat.location=(0,0,1.15)
    elif name == 'WateringCan':
        mouth=find('Can mouth')[0]
        lathe(mouth,list(reversed([(.001,-.08),(.19,-.08),(.19,.002),(.21,.012),(.235,.008),(.245,-.012),(.235,-.035)])))
        body=find('Can body')[0]
        bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=.198, depth=.35, location=(0,0,.735))
        cutter=bpy.context.object
        bpy.context.view_layer.objects.active=body
        cut=body.modifiers.new('Recessed fill opening','BOOLEAN'); cut.operation='DIFFERENCE'; cut.object=cutter
        bpy.ops.object.modifier_apply(modifier=cut.name)
        triangulate=body.modifiers.new('Stable opening triangulation','TRIANGULATE')
        bpy.ops.object.modifier_apply(modifier=triangulate.name)
        bpy.data.objects.remove(cutter,do_unlink=True)
        mouth.location.z=.69
        mouth.data.materials[0]=bpy.data.materials['Aqua']
        # Recessed inner well, rolled lip, perforated rose aligned with the spout.
        rose=find('Rose')[0]
        center=Vector((.86,0,.65)); normal=Vector((.53,0,.36)).normalized()
        rotation=normal.to_track_quat('Z','Y')
        rose.location=center; rose.rotation_euler=rotation.to_euler()
        for i,o in enumerate(find('Water hole')):
            a=i*math.tau/5
            o.location=center+rotation@Vector((.07*math.cos(a),.07*math.sin(a),.074))
            o.rotation_euler=rotation.to_euler()
    elif name == 'FlowerPot':
        pot=find('Pot')[0]
        lathe(pot,[(.001,-.23),(.25,-.23),(.28,-.20),(.37,.23),(.32,.23),(.24,-.16),(.001,-.16)])
        soil=find('Pot soil')[0]; soil.location.z=.435
    elif name == 'Spade':
        blade=find('Spade blade')[0]
        outline=[(-.19,.20),(-.19,-.06),(-.12,-.19),(0,-.25),(.12,-.19),(.19,-.06),(.19,.20)]
        vertices=[(x,y,z) for y in [-.035,.035] for x,z in outline]
        faces=[tuple(range(7)),tuple(reversed(range(7,14)))]+[(i,i+7,(i+1)%7+7,(i+1)%7) for i in range(7)]
        replace_mesh(blade,vertices,faces,False)
        bevel=blade.modifiers.new('Rounded blade edge','BEVEL'); bevel.width=.025; bevel.segments=3
        blade.modifiers.new('Blade normals','WEIGHTED_NORMAL')
    elif name == 'Tree':
        for o,loc in zip(find('Garden apple'),[(-.62,-.59,2.12),(.40,-.88,2.47),(1.02,-.58,1.93)]): o.location=loc
    elif name == 'Lavender':
        for i,o in enumerate(find('Lavender')):
            branch=i//5; tier=i%5
            o.location.z-=abs((branch-1)*.13)*.65
            o.scale=(1,1,1.12)
