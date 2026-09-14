if(Application.isPlaying) throw new System.Exception("Export the saved rest pose outside Play Mode.");
var models = new System.Collections.Generic.List<object>();
var meshes = new System.Collections.Generic.Dictionary<string,object>();
var seen = new System.Collections.Generic.HashSet<string>();
float[] V(Vector3 v) => new [] { v.x,v.y,v.z };
foreach(var animal in UnityEngine.Object.FindObjectsByType<GardenSnake.GardenAnimal>()) {
 string species = animal.GetType().Name.Replace("Garden", "");
 if(!seen.Add(species)) continue;
 var transforms = animal.GetComponentsInChildren<Transform>(true);
 var nodes = new System.Collections.Generic.List<object>();
 var so = new SerializedObject(animal);
 var bindings = new System.Collections.Generic.Dictionary<string,object>();
 foreach(string field in new[]{"body","head","leftWing","rightWing","limbs","ears"}) {
  var property=so.FindProperty(field); if(property==null) continue;
  if(property.isArray) { var ids=new System.Collections.Generic.List<int>(); for(int j=0;j<property.arraySize;j++) ids.Add(System.Array.IndexOf(transforms,(Transform)property.GetArrayElementAtIndex(j).objectReferenceValue)); bindings[field]=ids; }
  else bindings[field]=System.Array.IndexOf(transforms,(Transform)property.objectReferenceValue);
 }
 for(int i=0;i<transforms.Length;i++) {
  var t=transforms[i]; var f=t.GetComponent<MeshFilter>(); var r=t.GetComponent<MeshRenderer>();
  string meshKey=null;
  if(f!=null) {
   var mesh=f.sharedMesh; meshKey=mesh.GetEntityId().ToString();
   if(!meshes.ContainsKey(meshKey)) {
    var vertices=new System.Collections.Generic.List<float[]>(); foreach(var v in mesh.vertices) vertices.Add(V(v));
    var normals=new System.Collections.Generic.List<float[]>(); foreach(var v in mesh.normals) normals.Add(V(v));
    meshes[meshKey]=new { name=mesh.name, path=AssetDatabase.GetAssetPath(mesh), vertices,normals,triangles=mesh.triangles };
   }
  }
  var rotation=t.localRotation;
  var mats=new System.Collections.Generic.List<string>(); if(r!=null) foreach(var m in r.sharedMaterials) mats.Add(m.name);
  nodes.Add(new { id=i,name=t.name,parent=i==0?-1:System.Array.IndexOf(transforms,t.parent),position=i==0?new[]{0f,0f,0f}:V(t.localPosition),rotation=i==0?new[]{0f,0f,0f,1f}:new[]{rotation.x,rotation.y,rotation.z,rotation.w},scale=i==0?new[]{1f,1f,1f}:V(t.localScale),mesh=meshKey, materials=mats, cast=r==null?0:(int)r.shadowCastingMode,receive=r!=null&&r.receiveShadows });
 }
 models.Add(new { species,nodes,bindings });
}
System.IO.Directory.CreateDirectory("Artifacts/wildlife-fbx");
System.IO.File.WriteAllText("Artifacts/wildlife-fbx/source.json",Newtonsoft.Json.JsonConvert.SerializeObject(new { models,meshes }));
return "Exported "+models.Count+" species, "+meshes.Count+" shared mesh sources.";

