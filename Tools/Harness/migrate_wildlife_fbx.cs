if(Application.isPlaying) throw new System.Exception("Stop Play Mode before migrating wildlife.");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/Scenes/GardenSnake.unity" || scene.isDirty) throw new System.Exception("Expected clean saved GardenSnake scene.");
var definitions=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("Assets/Art/Models/Wildlife/Rigs.json"));
var swaps=new System.Collections.Generic.List<(MeshFilter filter,Mesh mesh)>();
var legacy=new System.Collections.Generic.Dictionary<Mesh,Mesh>();
var verified=new System.Collections.Generic.HashSet<string>();
string Point(Vector3 v)=>Mathf.RoundToInt(v.x*10000)+","+Mathf.RoundToInt(v.y*10000)+","+Mathf.RoundToInt(v.z*10000);
void Verify(Mesh before,Mesh after) {
 string pair=before.name+"|"+after.name; if(!verified.Add(pair)) return;
 if(before.triangles.Length!=after.triangles.Length) throw new System.Exception("Triangle count changed: "+pair);
 if(Vector3.Distance(before.bounds.center,after.bounds.center)>.00002f || Vector3.Distance(before.bounds.size,after.bounds.size)>.00002f) throw new System.Exception("Bounds changed: "+pair);
 var points=new System.Collections.Generic.Dictionary<string,System.Collections.Generic.List<int>>();
 var v=before.vertices; var n=before.normals;
 for(int i=0;i<v.Length;i++) { string key=Point(v[i]); if(!points.TryGetValue(key,out var ids)) points[key]=ids=new System.Collections.Generic.List<int>(); ids.Add(i); }
 var av=after.vertices; var an=after.normals;
 for(int i=0;i<av.Length;i++) {
  if(!points.TryGetValue(Point(av[i]),out var ids)) throw new System.Exception("Position changed: "+pair+" "+av[i].ToString("F6"));
  bool match=false; foreach(int j in ids) if(Vector3.Distance(v[j],av[i])<.00003f && Vector3.Dot(n[j],an[i])>.999f) { match=true;break; }
  if(!match) throw new System.Exception("Normal changed: "+pair+" "+i);
 }
}
foreach(var animal in UnityEngine.Object.FindObjectsByType<GardenSnake.Garden.GardenAnimal>()) {
 string species=animal.GetType().Name.Replace("Garden","");
 Newtonsoft.Json.Linq.JToken definition=null; foreach(var d in definitions) if((string)d["species"]==species) definition=d;
 var nodes=animal.GetComponentsInChildren<Transform>(true);
 if(nodes.Length!=definition["nodes"].Count()) throw new System.Exception("Rig shape changed: "+species);
 var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Wildlife/"+species+".fbx");
 var models=new System.Collections.Generic.Dictionary<string,Mesh>(); foreach(var f in model.GetComponentsInChildren<MeshFilter>()) models[f.name]=f.sharedMesh;
 foreach(var node in definition["nodes"]) {
  int id=(int)node["id"]; if(nodes[id].name!=(string)node["name"]) throw new System.Exception("Rig name mismatch: "+species+" "+id);
  if(node["mesh"].Type==Newtonsoft.Json.Linq.JTokenType.Null) continue;
  var filter=nodes[id].GetComponent<MeshFilter>(); var mesh=models[(string)node["fbxName"]];
  Verify(filter.sharedMesh,mesh);
  if(AssetDatabase.GetAssetPath(filter.sharedMesh).StartsWith("Assets/Art/Wildlife/")) legacy[filter.sharedMesh]=mesh;
  swaps.Add((filter,mesh));
 }
}
// Non-animal uses (the bird's perch leaf) also move off the old native assets.
foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>())
 if(legacy.TryGetValue(filter.sharedMesh,out var replacement) && !swaps.Exists(p=>p.filter==filter)) swaps.Add((filter,replacement));
System.IO.File.WriteAllText("Artifacts/wildlife-fbx/migration-report.txt","Verified "+verified.Count+" geometry pairs; prepared "+swaps.Count+" references.");
if(!SessionState.GetBool("WildlifeFbx.Apply",false)) return "Verified "+verified.Count+" geometry pairs; prepared "+swaps.Count+" references. Dry run.";
foreach(var pair in swaps) { Undo.RecordObject(pair.filter,"Use wildlife FBX geometry"); pair.filter.sharedMesh=pair.mesh; EditorUtility.SetDirty(pair.filter); }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
SessionState.SetBool("WildlifeFbx.Apply",false);
return "Migrated "+swaps.Count+" MeshFilter references; preserved transforms, materials and animation components.";
