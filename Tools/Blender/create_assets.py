"""Rebuild original Garden Snake models with Blender 3.5+ (no external assets)."""
import bpy
import math
import os
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
    m.use_nodes = True
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
    'Base': material('Base', (.12, .26, .20)),
}
assets = {}
current = []

def finish(obj, name, mat):
    obj.name = name
    obj.data.materials.append(mats[mat])
    current.append(obj)
    return obj

def sphere(name, loc, scale, mat, segments=20):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=12, location=loc)
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
    obj.data.use_auto_smooth = True
    obj.modifiers.new('Weighted normals', 'WEIGHTED_NORMAL')
    return finish(obj, name, mat)

def export(name):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in current:
        obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name + '.fbx'),
        use_selection=True, object_types={'MESH'}, add_leaf_bones=False,
        axis_forward='-Z', axis_up='Y', bake_anim=False, use_mesh_modifiers=True)
    assets[name] = list(current)
    current.clear()

box('Head', (0, 0, .38), (.85, .92, .66), 'Jade', .24)
sphere('Muzzle', (0, -.32, .25), (.36, .22, .18), 'Cream')
for x in [-.24, .24]:
    sphere('Eye white', (x, -.31, .61), (.135, .12, .16), 'Cream')
    sphere('Pupil', (x, -.412, .63), (.068, .045, .09), 'Ink')
    sphere('Eye glint', (x-.018, -.45, .67), (.02, .013, .027), 'Cream', 12)
    sphere('Nostril', (x*.58, -.459, .36), (.027, .015, .021), 'Ink', 12)
export('SnakeHead')
box('Body', (0, 0, .31), (.76, .78, .56), 'Jade', .23)
sphere('Dorsal spot', (0, 0, .57), (.22, .26, .045), 'Lime')
export('SnakeBody')
sphere('Tail', (0, 0, .25), (.31, .4, .25), 'Jade')
sphere('Tail spot', (0, 0, .47), (.16, .22, .035), 'Lime')
export('SnakeTail')
for x in [-.13, .13]:
    sphere('Apple flesh', (x, 0, .36), (.29, .32, .32), 'Coral')
stem = box('Stem', (0, 0, .73), (.075, .075, .24), 'Wood', .02)
stem.rotation_euler[1] = -.2
leaf = sphere('Leaf', (.17, 0, .78), (.22, .10, .045), 'Leaf')
leaf.rotation_euler[1] = -.4
export('Apple')
box('Garden tile', (0, 0, -.085), (.98, .98, .17), 'Tile', .035)
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
