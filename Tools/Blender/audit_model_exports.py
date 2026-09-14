"""Inspect actual FBX exports, recording import contracts and three-angle renders.

Usage: blender -b --python Tools/Blender/audit_model_exports.py --
       --input Assets/Art/Models --output Artifacts/model-sweep/before
"""
import argparse
import json
import math
import re
import sys
from pathlib import Path
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('--input',type=Path,required=True)
parser.add_argument('--output',type=Path,required=True)
parser.add_argument('--only',nargs='*')
parser.add_argument('--no-render',action='store_true')
args = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
args.output.mkdir(parents=True,exist_ok=True)
report = {}
for path in sorted(args.input.glob('*.fbx')):
    if args.only and path.stem not in args.only: continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(path.resolve()))
    objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    models = {}
    for obj in objects:
        mesh = obj.data
        mesh.calc_loop_triangles()
        assert len(mesh.vertices) and len(mesh.polygons)
        assert all(math.isfinite(c) for v in mesh.vertices for c in v.co)
        models[obj.name] = {
            'mesh':mesh.name,
            'vertices':len(mesh.vertices),
            'triangles':len(mesh.loop_triangles),
            'materials':[m.name for m in mesh.materials],
            'matrix':[round(c,6) for row in obj.matrix_world for c in row],
            'parent':obj.parent.name if obj.parent else None,
            'bounds':[[round(v,6) for v in obj.matrix_world @ Vector(c)] for c in obj.bound_box],
            'uv_layers':len(mesh.uv_layers),
        }
    report[path.stem] = models
    if args.no_render: continue
    for mat in bpy.data.materials:
        label = 'JadeHead' if path.stem == 'SnakeHead' and mat.name == 'Jade' else mat.name
        source = ROOT/'Assets/Materials'/f'{label}.mat'
        if source.exists():
            match = re.search(r'_BaseColor: \{r: ([\d.eE+-]+), g: ([\d.eE+-]+), b: ([\d.eE+-]+), a: ([\d.eE+-]+)\}',source.read_text())
            if match:
                rgb = [float(c) for c in match.groups()[:3]]
                mat.diffuse_color = (*[(c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4) for c in rgb],1)
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x = scene.render.resolution_y = 640 if path.stem == 'SnakeHead' else 400
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.view_settings.view_transform = 'Standard'
    shading = scene.display.shading
    shading.light = 'STUDIO'; shading.color_type = 'MATERIAL'
    shading.show_shadows = True; shading.show_cavity = True
    shading.cavity_type = 'BOTH'; shading.curvature_ridge_factor = .6
    shading.curvature_valley_factor = .45
    shading.show_specular_highlight = True
    shading.background_type = 'WORLD'
    scene.world = bpy.data.worlds.new('Audit world')
    scene.world.color = (.17,.20,.18)
    camdata = bpy.data.cameras.new('Audit camera')
    camera = bpy.data.objects.new('Audit camera',camdata)
    scene.collection.objects.link(camera); scene.camera = camera
    camdata.type = 'ORTHO'
    corners = [o.matrix_world@Vector(p) for o in objects for p in o.bound_box]
    low = Vector([min(v[i] for v in corners) for i in range(3)])
    high = Vector([max(v[i] for v in corners) for i in range(3)])
    center = (low+high)/2
    size = max(high-low)
    camdata.ortho_scale = size*1.4
    for side,direction in [('front',(1,-2,1.1)),('back',(-1,2,.8)),('game',(0,-1,1.8))]:
        camera.location = center+Vector(direction).normalized()*size*3
        camera.rotation_euler = (center-camera.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath = str((args.output/f'{path.stem}-{side}.png').resolve())
        bpy.ops.render.render(write_still=True)
(args.output/'manifest.json').write_text(json.dumps(report,indent=2))
print('MODEL_EXPORT_AUDIT_OK',len(report))
