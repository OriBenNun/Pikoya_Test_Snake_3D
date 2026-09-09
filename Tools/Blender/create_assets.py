"""Rebuild original Garden Snake models with Blender 5.2+ (no external assets)."""
import bpy
import math
import os
import runpy
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../.."))
OUT = os.path.join(ROOT, "Assets/GardenSnake/Art/Models")
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for material in list(bpy.data.materials):
    bpy.data.materials.remove(material)

def material(name, color, roughness=.4):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = roughness
    return m

mats = {
    'Jade': material('Jade', (.13, .54, .27)),
    'Lime': material('Lime', (.49, .76, .19)),
    'Cream': material('Cream', (.98, .92, .69)),
    'Ink': material('Ink', (.035, .075, .06)),
    'Coral': material('Coral', (.94, .19, .15)),
    'Wood': material('Wood', (.29, .14, .08)),
    'Leaf': material('Leaf', (.14, .38, .18)),
    'Stone': material('Stone', (.34, .46, .40)),
    'Petal': material('Petal', (1, .77, .40)),
    'Tile': material('Tile', (.67, .78, .49)),
    'Aqua': material('Aqua', (.18, .65, .7)),
    'Blush': material('Blush', (1, .48, .48)),
    'Bark': material('Bark', (.47, .29, .15)),
    'Foliage': material('Foliage', (.29, .58, .18)),
    'Base': material('Base', (.12, .26, .20)),
}
polish = runpy.run_path(os.path.join(os.path.dirname(__file__), 'polish_assets.py'))['polish']
assets = {}
current = []

def finish(obj, name, mat):
    obj.name = name
    obj.data.materials.append(mats[mat])
    current.append(obj)
    return obj

def sphere(name, loc, scale, mat, segments=20):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=max(32, segments), ring_count=24, location=loc)
    obj = bpy.context.object
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    for p in obj.data.polygons:
        p.use_smooth = True
    return finish(obj, name, mat)

def box(name, loc, scale, mat, bevel=.1):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    mod = obj.modifiers.new('Soft edges', 'BEVEL')
    mod.width = bevel
    mod.segments = 3
    bpy.ops.object.modifier_apply(modifier=mod.name)
    for p in obj.data.polygons:
        p.use_smooth = True
    mod = obj.modifiers.new('Weighted normals', 'WEIGHTED_NORMAL')
    mod.keep_sharp = True
    return finish(obj, name, mat)

def export(name):
    polish(name, current)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in current:
        obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name + '.fbx'),
        use_selection=True, object_types={'MESH'}, add_leaf_bones=False,
        axis_forward='-Z', axis_up='Y', bake_anim=False, use_mesh_modifiers=True)
    assets[name] = list(current)
    current.clear()

# Plump, asymmetric face. Low tessellation, smooth normals, readable silhouette.
sphere('Head', (0, 0, .4), (.44, .47, .36), 'Jade', 16)
sphere('Muzzle', (0, -.31, .29), (.38, .24, .2), 'Cream', 16)
for side, x in enumerate([-.25, .25]):
    z = .69 + side * .035
    sphere('Eye white', (x, -.23, z), (.18, .15, .21), 'Cream', 16)
    sphere('Pupil', (x + .018, -.362, z + .018), (.084, .045, .118), 'Ink', 12)
    sphere('Eye glint', (x-.005, -.402, z+.065), (.029, .014, .035), 'Cream', 8)
    sphere('Cheek', (x*1.4, -.335, .38), (.09, .043, .065), 'Blush', 12)
    sphere('Nostril', (x*.56, -.526, .38), (.023, .012, .019), 'Ink', 8)
    brow = sphere('Eyebrow', (x, -.27, z+.225), (.13, .045, .033), 'Lime', 12)
    brow.rotation_euler[1] = -.18 if side else .24
# Curved grin and two ridiculous little buck teeth.
for i in range(9):
    x = (i-4)*.05
    sphere('Smile', (x, -.513, .23 + .6*x*x), (.033, .014, .018), 'Ink', 8)
for x in [-.055, .055]:
    box('Tiny tooth', (x, -.529, .218), (.075, .025, .07), 'Cream', .018)
sphere('Tongue', (.13, -.545, .175), (.065, .1, .025), 'Blush', 12)
for x,y,z in [(-.17,.13,.715),(.1,.19,.724),(.02,.3,.673)]:
    sphere('Freckle', (x,y,z), (.038,.046,.014), 'Lime', 8)
export('SnakeHead')
sphere('Body', (0, 0, .3), (.37, .4, .29), 'Jade', 16)
sphere('Belly', (0, -.01, .13), (.33,.36,.10), 'Cream', 12)
sphere('Dorsal spot', (0, 0, .579), (.18, .23, .035), 'Lime', 12)
for x in [-.27,.27]:
    sphere('Side freckle', (x,-.03,.45), (.037,.08,.03), 'Lime', 8)
export('SnakeBody')
sphere('Tail', (0, 0, .23), (.29, .39, .23), 'Jade', 16)
sphere('Tail tip', (.09, .3, .22), (.16,.22,.14), 'Jade', 12)
sphere('Tail spot', (0, 0, .445), (.14,.2,.025), 'Lime', 12)
export('SnakeTail')
for x in [-.13, .13]:
    sphere('Apple flesh', (x, 0, .36), (.29, .32, .32), 'Coral')
stem = box('Stem', (0, 0, .73), (.075, .075, .24), 'Wood', .02)
stem.rotation_euler[1] = -.2
leaf = sphere('Leaf', (.17, 0, .78), (.22, .10, .045), 'Leaf')
leaf.rotation_euler[1] = -.4
export('Apple')
box('Garden tile', (0, 0, -.12), (.975, .975, .24), 'Tile', .075)
export('Tile')
box('Garden base', (0, 0, -.37), (1, 1, .60), 'Base', .14)
export('Planter')
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1, location=(0, 0, .28))
rock = bpy.context.object
rock.scale = (.48, .37, .39)
finish(rock, 'River stone', 'Stone')
export('Rock')
box('Stem', (0, 0, .27), (.045, .045, .54), 'Leaf', .015)
for i in range(5):
    a = i * math.tau / 5
    sphere('Petal', (.15*math.cos(a), .15*math.sin(a), .54), (.13, .13, .065), 'Cream', 12)
sphere('Pollen', (0, 0, .59), (.085, .085, .065), 'Petal', 12)
export('Flower')


# Board joinery, preserving bevel radius at its final size.
box('Glazed rim', (0,0,0), (22.16,.58,.36), 'Base', .14)
export('RimLong')
box('Glazed rim', (0,0,0), (.58,12,.36), 'Base', .14)
export('RimShort')

def cone(name, loc, radius, depth, mat, tip=0):
    bpy.ops.mesh.primitive_cone_add(vertices=32, radius1=radius, radius2=tip, depth=depth, location=loc)
    obj=bpy.context.object
    for p in obj.data.polygons: p.use_smooth=(len(p.vertices) == 4)
    return finish(obj,name,mat)

def rod(name, a, b, radius, mat):
    a,b=Vector(a),Vector(b)
    obj=cone(name,(a+b)/2,radius,(b-a).length,mat,radius)
    obj.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    return obj

def torus(name, loc, radius, tube, mat):
    bpy.ops.mesh.primitive_torus_add(major_segments=48,minor_segments=12,location=loc,major_radius=radius,minor_radius=tube)
    obj=bpy.context.object
    for p in obj.data.polygons: p.use_smooth=True
    return finish(obj,name,mat)

for x in [-.18,.18]: sphere('Boot', (x,-.12,.12), (.19,.29,.13),'Bark',12)
sphere('Coat',(0,0,.5),(.38,.3,.46),'Aqua',16)
sphere('Face',(0,-.1,.97),(.26,.23,.28),'Cream',16)
for i in range(5):
    x=(i-2)*.09
    sphere('Fluffy beard',(x,-.27,.72+abs(x)*.7),(.105,.09,.23),'Cream',12)
sphere('Nose',(0,-.35,.96),(.115,.1,.115),'Blush',12)
for x in [-.105,.105]: sphere('Happy eye',(x,-.294,1.065),(.03,.02,.03),'Ink',8)
hat=cone('Pointy hat',(.04,0,1.44),.35,.77,'Coral'); hat.rotation_euler[1]=-.17
sphere('Hat brim',(0,0,1.15),(.37,.29,.075),'Coral',12)
for side in [-1,1]:
    rod('Sleeve',(side*.28,0,.68),(side*.43,-.03,.4),.115,'Aqua')
    sphere('Hand',(side*.43,-.03,.4),(.12,.1,.12),'Cream',12)
export('Gnome')

cone('Trunk',(0,0,.95),.21,1.9,'Bark',.12)
rod('Branch',(0,0,1.1),(-.65,0,1.85),.09,'Bark')
for x,y,z,scale in [(0,0,2.5,1.1),(-.7,.1,2,.8),(.72,0,2.17,.86),(0,.5,2.03,.88)]:
    sphere('Puffy canopy',(x,y,z),(scale,scale*.85,scale*.93),'Foliage',12)
for x,y,z in [(-.55,-.52,2.25),(.46,-.65,2.5),(.9,-.38,1.94)]:
    sphere('Garden apple',(x,y,z),(.13,.13,.14),'Coral',12)
export('Tree')
for x,y,z,r in [(-.45,0,.43,.5),(.35,.1,.52,.62),(0,-.18,.38,.5)]:
    sphere('Bush lobe',(x,y,z),(r,r*.8,r),'Foliage',12)
for x,y,z in [(-.45,-.32,.6),(.3,-.4,.7),(.13,-.48,.41)]:
    sphere('Berry',(x,y,z),(.065,.065,.07),'Blush',8)
export('Bush')

box('Can body',(0,0,.35),(.8,.53,.63),'Aqua',.2)
cone('Can mouth',(0,0,.7),.24,.08,'Bark',.24)
rod('Spout',(.3,0,.26),(.83,0,.62),.085,'Aqua')
sphere('Rose',(.86,0,.66),(.14,.14,.085),'Cream',12)
handle=torus('Handle',(-.42,0,.47),.28,.055,'Aqua'); handle.rotation_euler[0]=math.pi/2
for i in range(5):
    a=i*math.tau/5
    sphere('Water hole',(.86+.07*math.cos(a),.07*math.sin(a),.732),(.014,.014,.01),'Ink',8)
export('WateringCan')

rod('Spade handle',(0,0,.3),(0,0,1.6),.055,'Bark')
box('Spade blade',(0,0,.2),(.38,.09,.45),'Aqua',.11)
handle=torus('Spade grip',(0,0,1.7),.16,.04,'Aqua'); handle.rotation_euler[0]=math.pi/2
export('Spade')
rod('Rake handle',(0,0,.1),(0,0,1.75),.045,'Bark')
rod('Rake crossbar',(-.35,0,.15),(.35,0,.15),.04,'Aqua')
for i in range(6): rod('Rake tooth',(-.3+i*.12,0,.15),(-.3+i*.12,-.23,.08),.025,'Aqua')
export('Rake')

cone('Pot',(0,0,.23),.28,.46,'Base',.37)
torus('Pot rim',(0,0,.46),.37,.065,'Base')
cone('Pot soil',(0,0,.47),.31,.025,'Bark',.31)
for i in range(5):
    a=i*math.tau/5
    leaf=sphere('Sprout',(.18*math.cos(a),.18*math.sin(a),.67),(.09,.1,.33),'Leaf',12)
    leaf.rotation_euler=(.4*math.sin(a),.4*math.cos(a),a)
export('FlowerPot')

# Distinct flower silhouettes: tulip cups, tall lavender and broad daisies.
rod('Stem',(0,0,0),(0,0,.67),.024,'Leaf')
sphere('Tulip cup',(0,0,.72),(.17,.17,.2),'Blush',12)
for i in range(3):
    a=i*math.tau/3
    sphere('Tulip petal',(.10*math.cos(a),.10*math.sin(a),.81),(.1,.1,.2),'Blush',12)
sphere('Leaf',(.1,0,.31),(.15,.045,.2),'Leaf',12)
export('Tulip')
for branch in range(3):
    x=(branch-1)*.13
    rod('Stem',(0,0,0),(x,0,.9-abs(x)),.018,'Leaf')
    for i in range(5): sphere('Lavender',(x,0,.5+i*.085),(.085-i*.01,.075-i*.008,.07),'Aqua',8)
export('Lavender')
rod('Stem',(0,0,0),(0,0,.65),.025,'Leaf')
for i in range(9):
    a=i*math.tau/9
    petal=sphere('Daisy petal',(.21*math.cos(a),.21*math.sin(a),.68),(.16,.065,.055),'Cream',12)
    petal.rotation_euler[2]=a
sphere('Daisy center',(0,0,.72),(.12,.12,.065),'Petal',12)
export('Daisy')

# A readable source scene: one collection per exported asset, arranged for inspection.
for index, (name, objects) in enumerate(assets.items()):
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    for obj in objects:
        for old in list(obj.users_collection):
            old.objects.unlink(obj)
        collection.objects.link(obj)
        obj.location.x += (index % 4) * 2.5
        obj.location.y += (index // 4) * 2.5
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, 'Tools/Blender/GardenSnake.blend'))
print('GARDEN_ASSETS_OK: ' + ', '.join(assets))
