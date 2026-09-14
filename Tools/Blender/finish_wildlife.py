"""Finish surface details in Blender; export mesh buffers for the Unity importer.

Run: blender -b --python Tools/Blender/finish_wildlife.py
The shell markings use the ladybug's authored ellipsoid. Turtle scutes end on
one circular latitude, avoiding the scalloped lip of projected polygon edges.
"""
import math
import sys
from pathlib import Path
import bpy
import bmesh

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from wildlife_models import write_geometry

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Artifacts/model-sweep/details-meshes'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)


def mesh_object(name, points, faces, color):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([(x, -z, y) for x, y, z in points], [], faces)
    mesh.update()
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    # These are open upper surfaces; recalculation alone cannot determine which
    # side is exterior. Blender Z is Unity Y.
    bmesh.ops.reverse_faces(bm, faces=[face for face in bm.faces if face.normal.z < 0])
    bm.to_mesh(mesh)
    bm.free()
    for face in mesh.polygons:
        face.use_smooth = True
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    mat = bpy.data.materials.new(name + ' preview')
    mat.diffuse_color = (*color, 1)
    mesh.materials.append(mat)
    return obj


def save(obj):
    result = write_geometry(obj, OUT / (obj.name + '.meshbin'))
    print(obj.name, result)


# Smooth replacement for Unity's low resolution primitive. Its unit diameter
# preserves the authored Red shell transform and bounds.
bpy.ops.mesh.primitive_uv_sphere_add(segments=64, ring_count=40, radius=.5)
shell = bpy.context.object
shell.name = 'LadybugShell'
for face in shell.data.polygons:
    face.use_smooth = True
save(shell)

points, faces = [], []


def surface(x, z):
    y = .15 + .13 * math.sqrt(max(0, 1 - (x/.155)**2 - (z/.195)**2))
    return (x, y + .0015, z)


for x in (-.078, .078):
    for z in (-.079, .079):
        start = len(points)
        points.append(surface(x, z))
        steps, rings = 40, 6
        for ring in range(1, rings + 1):
            radius = .030 * ring / rings
            for j in range(steps):
                a = j * math.tau / steps
                points.append(surface(x + radius*math.cos(a), z + radius*math.sin(a)))
        for j in range(steps):
            faces.append((start, start+1+j, start+1+(j+1)%steps))
        for ring in range(rings-1):
            a = start + 1 + ring*steps
            b = a + steps
            for j in range(steps):
                k = (j+1)%steps
                faces.append((a+j,b+j,b+k,a+k))

# Thin continuous seam follows the dome instead of poking through its ends.
start = len(points)
for j in range(49):
    z = -.177 + .345*j/48
    taper = min(1, (j+1)/4, (49-j)/4)
    points.extend([surface(-.003*taper,z), surface(.003*taper,z)])
    if j:
        a = start+(j-1)*2
        faces.append((a,a+2,a+3,a+1))
markings = mesh_object('LadybugMarkings', points, faces, (.025,.05,.035))
save(markings)

for index in range(6):
    points, faces = [], []
    rows, columns = 14, 18
    angle_a, angle_b = index*math.tau/6, (index+1)*math.tau/6
    for r in range(rows+1):
        t = r/rows
        for c in range(columns+1):
            # Angular inset leaves a narrow seam; the outer radius is constant.
            a = angle_a+.012+(angle_b-angle_a-.024)*c/columns
            inner = .22*math.cos(math.pi/6)/math.cos(a-(angle_a+angle_b)/2)+.008
            radius = inner*(1-t)+.485*t
            x, z = radius*math.cos(a), radius*math.sin(a)
            y = math.sqrt(.25-radius*radius)+.005
            points.append((x,y,z))
            if r and c:
                d = r*(columns+1)+c
                faces.append((d-columns-2,d-columns-1,d,d-1))
    obj = mesh_object('ShellPlate'+str(index),points,faces,(.42,.61,.24))
    bpy.context.view_layer.objects.active = obj
    mod = obj.modifiers.new('Soft scute edge','SOLIDIFY')
    mod.thickness = .004
    mod.offset = -1
    bpy.ops.object.modifier_apply(modifier=mod.name)
    save(obj)

# Separate source collections make inspection convenient without changing
# exported coordinates.
for i,obj in enumerate(list(bpy.context.scene.objects)):
    obj.location.x += (i%4)*1.5
    obj.location.y += (i//4)*1.5
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Tools/Blender/WildlifeDetails.blend'))
print('WILDLIFE_DETAILS_OK')
