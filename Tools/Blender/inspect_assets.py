"""Render each authored model from two angles without modifying the source."""
import bpy, os, sys, math
from mathutils import Vector
root = os.path.abspath(os.path.join(os.path.dirname(__file__), '../..'))
label = sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'before'
out = os.path.join(root, 'Artifacts/model-audit', label)
os.makedirs(out, exist_ok=True)
scene = bpy.context.scene
scene.render.engine = 'BLENDER_WORKBENCH'
scene.render.resolution_x = 480
scene.render.resolution_y = 480
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
s = scene.display.shading
s.light = 'STUDIO'
s.color_type = 'MATERIAL'
s.show_shadows = True
s.show_cavity = True
s.cavity_type = 'BOTH'
s.show_specular_highlight = True
s.background_type = 'WORLD'
scene.world.color = (.12,.12,.12)
camdata = bpy.data.cameras.new('Audit camera')
cam = bpy.data.objects.new('Audit camera', camdata)
scene.collection.objects.link(cam)
scene.camera = cam
camdata.type = 'ORTHO'
collections = [c for c in bpy.data.collections if any(o.type == 'MESH' for o in c.objects)]
for c in collections:
    for o in c.objects: o.hide_render = True
for c in collections:
    objects = [o for o in c.objects if o.type == 'MESH']
    for o in objects: o.hide_render = False
    corners = [o.matrix_world @ Vector(v) for o in objects for v in o.bound_box]
    low = Vector([min(v[i] for v in corners) for i in range(3)])
    high = Vector([max(v[i] for v in corners) for i in range(3)])
    center = (low+high)/2
    size = max(high-low)
    camdata.ortho_scale = size*1.45
    for side, direction in [('front',(1,-2,1.1)),('back',(-1,2,.8))]:
        cam.location = center + Vector(direction).normalized()*size*3
        cam.rotation_euler = (center-cam.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath = os.path.join(out,c.name+'-'+side+'.png')
        bpy.ops.render.render(write_still=True)
    for o in objects: o.hide_render = True
print('AUDIT_RENDERS_OK',len(collections))
