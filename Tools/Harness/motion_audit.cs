if (!Application.isPlaying) return "Play Mode required";
var animals = UnityEngine.Object.FindObjectsByType<GardenSnake.GardenAnimal>(FindObjectsSortMode.None);
var chosen = new System.Collections.Generic.List<GardenSnake.GardenAnimal>();
foreach (string species in new[] {"Bunny", "Turtle", "Bird", "Butterfly", "Ladybug"})
    foreach (var animal in animals) if (animal.name == species) { chosen.Add(animal); break; }
var cameraObject = new GameObject("Motion audit camera");
var camera = cameraObject.AddComponent<Camera>();
camera.enabled = false; camera.orthographic = true; camera.orthographicSize = .85f;
camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.17f,.22f,.21f);
camera.cullingMask = 1 << 31; camera.nearClipPlane=.01f;
var texture = new RenderTexture(360,360,24); camera.targetTexture=texture;
var atlas = new Texture2D(1800,1080,TextureFormat.RGB24,false);
var layers = new System.Collections.Generic.Dictionary<Transform,int>();
foreach(var animal in chosen) foreach(var t in animal.GetComponentsInChildren<Transform>()) { layers[t]=t.gameObject.layer;  }
foreach(var animal in chosen) typeof(GardenSnake.GardenAnimal).GetMethod("BeginTravel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(animal,null);
int frame=0; double next=EditorApplication.timeSinceStartup;
string label=SessionState.GetString("MotionAudit.Label","before");
EditorApplication.CallbackFunction tick=null;
tick=()=> {
    if(EditorApplication.timeSinceStartup<next) return;
    next=EditorApplication.timeSinceStartup+.22;
    var active=RenderTexture.active;
    for(int i=0;i<chosen.Count;i++) {
        var animal=chosen[i];
        foreach(var part in animal.GetComponentsInChildren<Transform>()) part.gameObject.layer=31;
        var center=animal.transform.position+Vector3.up*.38f;
        camera.transform.position=center+animal.transform.rotation*new Vector3(1,1.05f,1.6f)*2;
        camera.transform.LookAt(center); camera.Render(); RenderTexture.active=texture;
        atlas.ReadPixels(new Rect(0,0,360,360),i*360,(2-frame)*360);
        foreach(var part in animal.GetComponentsInChildren<Transform>()) part.gameObject.layer=layers[part];
    }
    RenderTexture.active=active; frame++;
    if(frame<3) return;
    EditorApplication.update-=tick;
    atlas.Apply(); System.IO.Directory.CreateDirectory("Artifacts/motion-audit");
    System.IO.File.WriteAllBytes("Artifacts/motion-audit/"+label+".png",atlas.EncodeToPNG());
    foreach(var pair in layers) if(pair.Key!=null) pair.Key.gameObject.layer=pair.Value;
    camera.targetTexture=null; UnityEngine.Object.DestroyImmediate(texture);
    UnityEngine.Object.DestroyImmediate(atlas); UnityEngine.Object.DestroyImmediate(cameraObject);
};
EditorApplication.update+=tick;
return "Capturing wildlife motion";
