if (!Application.isPlaying) throw new System.InvalidOperationException("Play Mode required.");
string label = SessionState.GetString("ModelSweep.Detail", "details");
string folder = "Artifacts/model-sweep/" + label;
System.IO.Directory.CreateDirectory(folder);
void Capture(GameObject source, string label, bool back = false) {
    var model = UnityEngine.Object.Instantiate(source);
    var cameraObject = new GameObject("Temporary detail camera");
    var camera = cameraObject.AddComponent<Camera>();
    var target = new RenderTexture(640, 640, 24) { antiAliasing = 4 };
    var previous = RenderTexture.active;
    try {
        foreach (var behaviour in model.GetComponentsInChildren<MonoBehaviour>()) behaviour.enabled = false;
        // Layer isolation is sufficient. Large coordinates quantize tiny surface
        // details and introduce false z-fighting in inspection captures.
        model.transform.SetPositionAndRotation(new Vector3(0, 20, 0), Quaternion.identity);
        foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
        var renderers = model.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) if (renderer.enabled) bounds.Encapsulate(renderer.bounds);
        float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        camera.enabled = false;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.16f, .19f, .18f);
        camera.cullingMask = 1 << 31;
        camera.orthographic = true;
        camera.orthographicSize = size * .69f;
        camera.nearClipPlane = .01f;
        camera.farClipPlane = 500;
        camera.targetTexture = target;
        var direction = back ? new Vector3(-1, .9f, -2) : new Vector3(.7f, 1.0f, 2);
        camera.transform.position = bounds.center + direction.normalized * size * 3;
        camera.transform.LookAt(bounds.center);
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(640, 640, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); image.Apply();
        System.IO.File.WriteAllBytes(folder + "/" + label + ".png", image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);
    } finally {
        RenderTexture.active = previous;
        camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(cameraObject);
        UnityEngine.Object.DestroyImmediate(model);
    }
}
var seen = new System.Collections.Generic.HashSet<string>();
foreach (var animal in UnityEngine.Object.FindObjectsByType<GardenSnake.Garden.GardenAnimal>(FindObjectsSortMode.None)) {
    if (!seen.Add(animal.name)) continue;
    Capture(animal.gameObject, animal.name + "-front");
    Capture(animal.gameObject, animal.name + "-back", true);
}
var mouth = UnityEngine.Object.FindFirstObjectByType<GardenSnake.Presentation.SnakeMouth>();
var controller = UnityEngine.Object.FindFirstObjectByType<GardenSnake.Gameplay.GameLoopManager>();
if (mouth == null || controller == null) throw new System.InvalidOperationException("No live snake.");
seen.Clear();
var rows = new System.Collections.Generic.List<string>();
var errors = new System.Collections.Generic.List<string>();
Application.LogCallback log = (message, trace, type) => { if (type == LogType.Error || type == LogType.Exception) errors.Add(message); };
Application.logMessageReceived += log;
double started = EditorApplication.timeSinceStartup;
EditorApplication.CallbackFunction tick = null;
tick = () => {
    if (!Application.isPlaying || mouth == null || EditorApplication.timeSinceStartup - started > 22) {
        EditorApplication.update -= tick;
        Application.logMessageReceived -= log;
        rows.Add("runtimeErrors=" + errors.Count);
        rows.AddRange(errors);
        System.IO.File.WriteAllLines(folder + "/index.txt", rows);
        return;
    }
    string phase = controller.Score == 0 ? "anticipate" : "swallow";
    string key = phase + "-" + Mathf.Clamp(Mathf.FloorToInt(mouth.Openness * 4), 0, 3);
    if (seen.Add(key)) {
        Capture(mouth.gameObject, "head-" + key);
        rows.Add(key + " openness=" + mouth.Openness.ToString("F3") + " score=" + controller.Score);
    }
};
EditorApplication.update += tick;
return GardenSnake.Editor.GardenPlaytest.Run("model-sweep-final", 20, 2);
