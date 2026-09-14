"""Export five editable wildlife characters from Wildlife.blend to FBX.

Run: blender -b --python Tools/Blender/wildlife_models.py
One-time migration: append -- --source-json Artifacts/wildlife-fbx/source.json
The Blender file is authoritative after migration; Unity mesh buffers are not
part of the authoring loop. Node IDs preserve existing procedural motion rigs.
"""
import argparse
import json
import sys
from pathlib import Path
import bpy
from mathutils import Matrix, Quaternion

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT/'Tools/Blender/Wildlife.blend'
OUTPUT = ROOT/'Assets/Art/Models/Wildlife'
SPECIES = ('Bird', 'Bunny', 'Turtle', 'Butterfly', 'Ladybug')
# Verified against Unity's FBX importer: Unity = (-Blender.x, Blender.z, -Blender.y).
BASIS = Matrix(((-1,0,0,0),(0,0,-1,0),(0,1,0,0),(0,0,0,1)))


def bootstrap(path):
    data = json.loads(path.read_text())
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for model in data['models']:
        collection = bpy.data.collections.new(model['species'])
        bpy.context.scene.collection.children.link(collection)
        objects = []
        for node in model['nodes']:
            label = model['species'] + '_' + str(node['id']).zfill(3) + '_' + node['name']
            mesh = None
            if node['mesh']:
                source = data['meshes'][node['mesh']]
                mesh = bpy.data.meshes.new(label)
                points = [(-x,-z,y) for x,y,z in source['vertices']]
                indices = source['triangles']
                # Handedness conversion reverses triangle winding.
                faces = [(indices[i],indices[i+2],indices[i+1]) for i in range(0,len(indices),3)]
                mesh.from_pydata(points,[],faces)
                mesh.update()
                for face in mesh.polygons: face.use_smooth = True
                mesh.normals_split_custom_set_from_vertices([(-x,-z,y) for x,y,z in source['normals']])
                for name in node['materials']:
                    material = bpy.data.materials.get(name) or bpy.data.materials.new(name)
                    mesh.materials.append(material)
            obj = bpy.data.objects.new(label,mesh)
            collection.objects.link(obj)
            obj['rig_id'] = node['id']
            obj['rig_name'] = node['name']
            obj['rig_parent'] = node['parent']
            obj['cast_shadows'] = node['cast']
            obj['receive_shadows'] = node['receive']
            if node['parent'] >= 0: obj.parent = objects[node['parent']]
            q = node['rotation']
            matrix = Matrix.LocRotScale(node['position'],Quaternion((q[3],q[0],q[1],q[2])),node['scale'])
            obj.matrix_local = BASIS @ matrix @ BASIS.inverted()
            objects.append(obj)
        collection['bindings'] = json.dumps(model['bindings'])
    # Set preview colors from the project's serialized materials (read-only).
    import re
    for material in bpy.data.materials:
        p=ROOT/'Assets/Materials'/(material.name+'.mat')
        if not p.exists(): continue
        match=re.search(r'_BaseColor: \{r: ([\d.eE+-]+), g: ([\d.eE+-]+), b: ([\d.eE+-]+), a: ([\d.eE+-]+)\}',p.read_text())
        if match:
            rgb=[float(c) for c in match.groups()[:3]]
            material.diffuse_color=(*[(c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4) for c in rgb],1)
    bpy.context.scene.unit_settings.system='METRIC'
    bpy.context.scene.unit_settings.scale_length=1
    for index,species in enumerate(SPECIES):
        root=next(o for o in bpy.data.collections[species].objects if o['rig_id']==0)
        root.location=(index*2,0,0)
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))


def export():
    OUTPUT.mkdir(parents=True,exist_ok=True)
    manifest=[]
    for species in SPECIES:
        collection=bpy.data.collections[species]
        objects=sorted(collection.objects,key=lambda o:o['rig_id'])
        root=objects[0]
        saved=root.matrix_world.copy()
        root.matrix_world=Matrix.Identity(4)
        bpy.context.view_layer.update()
        bpy.ops.object.select_all(action='DESELECT')
        for obj in objects: obj.select_set(True)
        bpy.ops.export_scene.fbx(filepath=str(OUTPUT/(species+'.fbx')),use_selection=True,
            # Export flat geometry; Blender retains the editable parenting while Unity
            # retains the live motion pivots. Baking FBX axes on nested empties
            # changes child transforms in Unity's importer.
            object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_scale_options='FBX_SCALE_ALL',
            bake_anim=False,add_leaf_bones=False,use_custom_props=True,use_mesh_modifiers=True)
        manifest.append({'species':species,'bindings':json.loads(collection['bindings']),
            'nodes':[{'id':o['rig_id'],'name':o['rig_name'],'parent':o['rig_parent'],
                      'fbxName':o.name,'mesh':o.data.name if o.type=='MESH' else None,
                      'cast':o['cast_shadows'],'receive':o['receive_shadows'],
                      'materials':[m.name for m in o.data.materials] if o.type=='MESH' else []} for o in objects]})
        root.matrix_world=saved
    (OUTPUT/'Rigs.json').write_text(json.dumps(manifest,indent=2))
    print('WILDLIFE_FBX_OK',len(manifest))


parser=argparse.ArgumentParser()
parser.add_argument('--source-json',type=Path)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
if args.source_json: bootstrap(args.source_json)
else: bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
export()
