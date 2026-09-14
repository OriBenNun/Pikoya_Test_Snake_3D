"""Import native mesh assets into Blender and export sculpted mesh buffers.

The reader accepts only the observed single-submesh, float32 position/normal,
uint16 triangle layout and refuses other layouts. Unity consumes the output via
Tools/Harness/import_wildlife_models.cs and updates the existing assets itself.

Example: blender -b --python Tools/Blender/wildlife_models.py -- --polish
  --output Artifacts/model-sweep/wildlife --blend Artifacts/model-sweep/Wildlife.blend
"""
import argparse
import json
import math
import re
import struct
import sys
from pathlib import Path
import bpy
import bmesh
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]


def read_native(path):
    text = path.read_text()
    assert '--- !u!43 &4300000' in text and 'm_IndexFormat: 0' in text
    assert 'm_MeshCompression: 0' in text and 'm_BindPose: []' in text
    count = int(re.search(r'm_VertexCount: (\d+)', text)[1])
    channels = re.findall(r'- stream: (\d+)\s+offset: (\d+)\s+format: (\d+)\s+dimension: (\d+)', text)
    assert channels[:2] == [('0','0','0','3'), ('0','12','0','3')]
    assert all(c[3] == '0' for c in channels[2:])
    data = bytes.fromhex(re.search(r'_typelessdata: ([0-9a-f]+)', text)[1])
    assert len(data) == count*24
    records = list(struct.iter_unpack('<6f', data))
    indices = list(struct.iter_unpack('<H', bytes.fromhex(re.search(r'm_IndexBuffer: ([0-9a-f]+)', text)[1])))
    indices = [i[0] for i in indices]
    assert len(indices)%3 == 0 and max(indices) < count
    vertices = [(v[0],-v[2],v[1]) for v in records]
    faces = [tuple(indices[i:i+3]) for i in range(0,len(indices),3)]
    mesh = bpy.data.meshes.new(path.stem)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    # (x,y,z) -> (x,-z,y) is a rotation, so preserve triangle winding.
    for face in mesh.polygons: face.use_smooth = True
    obj = bpy.data.objects.new(path.stem, mesh)
    collection = bpy.data.collections.new(path.stem)
    bpy.context.scene.collection.children.link(collection)
    collection.objects.link(obj)
    return obj, text


def polish(obj):
    mesh = obj.data
    bm = bmesh.new(); bm.from_mesh(mesh)
    # Weld duplicated seams/poles and remove zero-area fan centers.
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=.000002)
    bmesh.ops.dissolve_degenerate(bm, edges=list(bm.edges), dist=.000001)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(mesh); bm.free()
    name = obj.name
    for vertex in mesh.vertices:
        # Work in Unity's familiar axes; export conversion is an exact rotation.
        x, y, z = vertex.co.x, vertex.co.z, -vertex.co.y
        if name == 'Pear':
            shoulder = .045*max(0,1-(2*y)**2)
            x *= 1+shoulder; z *= 1+shoulder*.6
        elif name == 'Ear':
            z += .018*(y+.5)**2
            x *= 1+.025*math.cos(y*math.pi)
        elif name == 'Bird':
            x *= 1+.04*max(0, -2*y)
            y -= .012*max(0,1-(2*z)**2)*max(0,-2*y)
        elif name == 'Beak':
            x *= 1+.045*max(0,1-(2*z)**2)
        elif name == 'Feather':
            y += .025*max(0,1-(2*z)**2)
        vertex.co = (x,-z,y)
    if name in ('BirdWing','ButterflyWing'):
        bpy.context.view_layer.objects.active = obj
        # Relax jagged outline transitions while retaining hinge and eyespot fit.
        mod = obj.modifiers.new('Soft wing outline','SMOOTH')
        mod.factor = .16 if name == 'BirdWing' else .10
        mod.iterations = 2
        bpy.ops.object.modifier_apply(modifier=mod.name)
    if name.startswith('ShellPlate') or name == 'ShellCenter':
        bpy.context.view_layer.objects.active = obj
        # Round the exposed scute edges into the shell instead of paper-thin cutouts.
        mod = obj.modifiers.new('Scute edge thickness','SOLIDIFY')
        mod.thickness = .006; mod.offset = -1
        bpy.ops.object.modifier_apply(modifier=mod.name)
        mod = obj.modifiers.new('Soft scute lip','BEVEL')
        mod.width = .0025; mod.segments = 2
        mod.limit_method = 'ANGLE'; mod.angle_limit = .5
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for face in mesh.polygons: face.use_smooth = True
    mesh.update()


def write_geometry(obj, path):
    mesh = obj.data
    mesh.calc_loop_triangles()
    vertices = [(v.co.x,v.co.z,-v.co.y) for v in mesh.vertices]
    normals = [(v.normal.x,v.normal.z,-v.normal.y) for v in mesh.vertices]
    triangles = [tuple(t.vertices) for t in mesh.loop_triangles]
    assert len(vertices) < 65536
    assert all(math.isfinite(c) for p in vertices+normals for c in p)
    assert all(.95 < Vector(n).length < 1.05 for n in normals)
    assert all((Vector(vertices[b])-Vector(vertices[a])).cross(Vector(vertices[c])-Vector(vertices[a])).dot(
        Vector(normals[a])+Vector(normals[b])+Vector(normals[c])) >= -.000001 for a,b,c in triangles)
    packed = b''.join(struct.pack('<6f',*p,*n) for p,n in zip(vertices,normals))
    indices = b''.join(struct.pack('<3I',*t) for t in triangles)
    lo = [min(p[i] for p in vertices) for i in range(3)]
    hi = [max(p[i] for p in vertices) for i in range(3)]
    path.write_bytes(struct.pack('<2I',len(vertices),len(triangles)*3)+packed+indices)
    return {'vertices':len(vertices),'triangles':len(triangles),'bounds':[lo,hi]}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--input',type=Path,default=ROOT/'Assets/GardenSnake/Art/Wildlife')
    parser.add_argument('--output',type=Path)
    parser.add_argument('--blend',type=Path,required=True)
    parser.add_argument('--polish',action='store_true')
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:])
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
    for c in list(bpy.data.collections): bpy.data.collections.remove(c)
    report = {}
    if args.output: args.output.mkdir(parents=True,exist_ok=True)
    for i,path in enumerate(sorted(args.input.glob('*.asset'))):
        obj, text = read_native(path)
        if args.polish: polish(obj)
        if args.output: report[path.stem] = write_geometry(obj,args.output/(path.stem+'.meshbin'))
        mat = bpy.data.materials.new(path.stem+' preview')
        mat.diffuse_color = (.56,.72,.25,1) if path.stem.startswith('Shell') else (.19,.65,.69,1)
        obj.data.materials.append(mat)
        obj.location = ((i%4)*1.5,(i//4)*1.5,0)
    args.blend.parent.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(args.blend.resolve()))
    if args.output: (args.output/'manifest.json').write_text(json.dumps(report,indent=2))
    print('WILDLIFE_MODELS_OK',len(report))


if __name__ == '__main__': main()
