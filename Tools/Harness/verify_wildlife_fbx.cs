if(!Application.isPlaying) throw new System.Exception("Play Mode required");
var animals=UnityEngine.Object.FindObjectsByType<GardenSnake.Garden.GardenAnimal>();
var poses=new System.Collections.Generic.Dictionary<Transform,Vector3>();
var rotations=new System.Collections.Generic.Dictionary<Transform,Quaternion>();
var states=new System.Collections.Generic.Dictionary<string,System.Collections.Generic.HashSet<string>>();
var moving=new System.Collections.Generic.HashSet<Transform>();
var errors=new System.Collections.Generic.List<string>();
foreach(var animal in animals) {
 states[animal.GetType().Name]=new System.Collections.Generic.HashSet<string>();
 foreach(var t in animal.GetComponentsInChildren<Transform>()) { poses[t]=t.localPosition;rotations[t]=t.localRotation; }
 foreach(var f in animal.GetComponentsInChildren<MeshFilter>())
  if(!AssetDatabase.GetAssetPath(f.sharedMesh).EndsWith(".fbx")) throw new System.Exception("Non-FBX wildlife part: "+f.name);
}
Application.LogCallback log=(message,trace,type)=>{if(type==LogType.Error||type==LogType.Exception)errors.Add(message);};
Application.logMessageReceived+=log;
double start=EditorApplication.timeSinceStartup;
EditorApplication.CallbackFunction tick=null;
tick=()=>{
 foreach(var animal in animals) if(animal!=null) {
  states[animal.GetType().Name].Add(animal.State.ToString());
  foreach(var t in animal.GetComponentsInChildren<Transform>())
   if(Vector3.Distance(poses[t],t.localPosition)>.001f||Quaternion.Angle(rotations[t],t.localRotation)>.5f) moving.Add(t);
 }
 if(Application.isPlaying&&EditorApplication.timeSinceStartup-start<40) return;
 EditorApplication.update-=tick;Application.logMessageReceived-=log;
 var rows=new System.Collections.Generic.List<string>{"animals="+animals.Length,"animatedTransforms="+moving.Count,"runtimeErrors="+errors.Count};
 foreach(var pair in states) rows.Add(pair.Key+"="+string.Join(",",pair.Value));
 rows.AddRange(errors);System.IO.File.WriteAllLines("Artifacts/wildlife-fbx/motion.txt",rows);
};
EditorApplication.update+=tick;
return "Monitoring FBX wildlife animation for 40 seconds.";
