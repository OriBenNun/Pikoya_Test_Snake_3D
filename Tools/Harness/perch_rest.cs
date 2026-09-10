if (!Application.isPlaying) return "Play Mode required";
var perch=GameObject.Find("Bird perch").transform;
GardenSnake.GardenAnimal bird=null;
foreach(var animal in UnityEngine.Object.FindObjectsByType<GardenSnake.GardenAnimal>(FindObjectsSortMode.None)) {
    if(new SerializedObject(animal).FindProperty("perch").objectReferenceValue==perch){bird=animal;break;}
}
if(bird==null) return "No perching bird";
double deadline=EditorApplication.timeSinceStartup+45;
double restingSince=-1;
EditorApplication.CallbackFunction tick=null;
tick=()=> {
    if(!Application.isPlaying || bird==null) {EditorApplication.update-=tick;return;}
    if(bird.State!=GardenSnake.GardenAnimal.Activity.Rest || Vector3.Distance(bird.transform.position,perch.position)>.02f) {
        restingSince=-1;
        if(EditorApplication.timeSinceStartup>deadline) {EditorApplication.update-=tick;System.IO.File.WriteAllText("Artifacts/perch-verification.txt","FAIL no perch rest observed");} return;
    }
    if(restingSince<0) restingSince=EditorApplication.timeSinceStartup;
    if(EditorApplication.timeSinceStartup-restingSince<.6) return;
    EditorApplication.update-=tick;
    var camObject=new GameObject("Rest pose camera");var camera=camObject.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=1.5f;
    Vector3 focus=perch.position+Vector3.up*.24f;
    camera.transform.position=focus+new Vector3(1.4f,.75f,-2)*2;camera.transform.LookAt(focus);
    var rt=new RenderTexture(900,900,24);camera.targetTexture=rt;var prior=RenderTexture.active;
    camera.Render();RenderTexture.active=rt;
    var png=new Texture2D(900,900,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,900,900),0,0);png.Apply();
    System.IO.File.WriteAllBytes("Artifacts/motion-audit/perch-rest.png",png.EncodeToPNG());
    RenderTexture.active=prior;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(png);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(camObject);
    System.IO.File.WriteAllText("Artifacts/perch-verification.txt","PASS bird naturally resting at attached perch; distance="+Vector3.Distance(bird.transform.position,perch.position));
};
EditorApplication.update+=tick;
return "Watching for natural perch rest";
