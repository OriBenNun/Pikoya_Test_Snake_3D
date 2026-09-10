"""Update only the flowering bush in the editable source and FBX export."""
import bpy, os, runpy
root = os.path.abspath(os.path.join(os.path.dirname(__file__), '../..'))
collection = bpy.data.collections['Bush']
objects = list(collection.objects)
# The source sheet places Bush at column 3, row 3 (index 15 in the source inventory).
# Derive its offset from the first lobe rather than assuming its sheet position.
lobe = next(o for o in objects if o.name.split('.')[0] == 'Bush lobe')
offset = lobe.location.copy()
offset.x -= -.45; offset.y -= 0; offset.z -= .43
for ob in objects: ob.location -= offset
# Remove prior blossom geometry so repeated runs are deterministic.
for ob in list(objects):
    if ob.name.startswith(('Bush petals','Bush flower centers','Bush leaf accents')):
        objects.remove(ob); bpy.data.objects.remove(ob, do_unlink=True)
polish = runpy.run_path(os.path.join(root,'Tools/Blender/polish_assets.py'))['polish']
polish('Bush',objects)
bpy.ops.object.select_all(action='DESELECT')
for ob in objects: ob.select_set(True)
bpy.ops.export_scene.fbx(filepath=os.path.join(root,'Assets/GardenSnake/Art/Models/Bush.fbx'),
    use_selection=True,object_types={'MESH'},add_leaf_bones=False,
    axis_forward='-Z',axis_up='Y',bake_anim=False,use_mesh_modifiers=True)
for ob in objects:
    for previous in list(ob.users_collection): previous.objects.unlink(ob)
    collection.objects.link(ob)
    ob.location += offset
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(root,'Tools/Blender/GardenSnake.blend'))
print('FLOWERING_BUSH_OK')
