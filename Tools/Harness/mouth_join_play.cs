// Run through unity command eval_file while the game is in Play Mode.
if (!Application.isPlaying) throw new System.InvalidOperationException("Play Mode required.");
var mouth = UnityEngine.Object.FindAnyObjectByType<GardenSnake.SnakeMouth>();
if (mouth == null || mouth.transform.lossyScale.sqrMagnitude < .01f)
    throw new System.InvalidOperationException("Start a live run and advance one frame before verification.");
var head = mouth.GetComponentsInChildren<MeshRenderer>().Single(r => r.name == "Head").transform;
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var pose = typeof(GardenSnake.SnakeMouth).GetMethod("Pose", flags);
var openness = typeof(GardenSnake.SnakeMouth).GetProperty("Openness");
float originalOpen = mouth.Openness;
bool originalPause = EditorApplication.isPaused;
var originalRotation = mouth.transform.rotation;
var anchor = mouth.transform.InverseTransformPoint(head.position);
var report = new System.Collections.Generic.List<string>();
string folder = "Artifacts/shots/mouth-join";
System.IO.Directory.CreateDirectory(folder);
var cameraObject = new GameObject("Mouth join verification camera");
var camera = cameraObject.AddComponent<Camera>();
camera.CopyFrom(Camera.main);
camera.enabled = false;
camera.orthographic = true;
camera.orthographicSize = 2.1f;
var target = new RenderTexture(1000, 700, 24);
var texture = new Texture2D(1000, 700, TextureFormat.RGB24, false);
var previousTarget = RenderTexture.active;
try
{
    EditorApplication.isPaused = true;
    foreach (float turn in new[] { 0f, 90f, 180f, 270f })
    {
        mouth.transform.rotation = originalRotation * Quaternion.Euler(0, turn, 0);
        foreach (float open in new[] { 0f, .5f, 1f, 0f })
        {
            openness.SetValue(mouth, open);
            pose.Invoke(mouth, null);
            float drift = Vector3.Distance(anchor, mouth.transform.InverseTransformPoint(head.position));
            if (drift > .0001f) throw new System.Exception("Head hinge detached: " + drift);
            report.Add("turn=" + turn + " open=" + open + " anchorDrift=" + drift);
            // Capture actual body alignment; other headings verify local hinge invariance.
            if (turn != 0) continue;
            var center = mouth.transform.TransformPoint(new Vector3(0, .55f, -.25f));
            camera.transform.position = center + mouth.transform.TransformDirection(new Vector3(5, 2.5f, 2));
            camera.transform.LookAt(center);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1000, 700), 0, 0);
            texture.Apply();
            System.IO.File.WriteAllBytes(folder + "/open-" + open.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + ".png", texture.EncodeToPNG());
        }
    }
    report.Add("PASS: head anchor remains fixed through opening, closing, and four headings.");
    System.IO.File.WriteAllLines(folder + "/verification.txt", report);
}
finally
{
    mouth.transform.rotation = originalRotation;
    openness.SetValue(mouth, originalOpen);
    pose.Invoke(mouth, null);
    RenderTexture.active = previousTarget;
    camera.targetTexture = null;
    UnityEngine.Object.DestroyImmediate(cameraObject);
    UnityEngine.Object.DestroyImmediate(texture);
    target.Release();
    UnityEngine.Object.DestroyImmediate(target);
    EditorApplication.isPaused = originalPause;
}
return report;
