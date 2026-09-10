if (!Application.isPlaying) throw new System.InvalidOperationException("Play Mode required.");
var controller = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeController>();
if (controller.Game.State != GardenSnake.Core.RunState.Paused) throw new System.InvalidOperationException("Pause first.");
var button = GameObject.Find("Garden HUD/Sound").GetComponent<UnityEngine.UI.Button>();
var rect = (RectTransform)button.transform;
Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
var behavior = UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
var mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("ButtonVerificationMouse");
bool muted = controller.Muted;
var rows = new System.Collections.Generic.List<string>();
string folder = "Artifacts/shots/button-motion";
System.IO.Directory.CreateDirectory(folder);
int stage = -1, frame = 0;
double started = EditorApplication.timeSinceStartup, next = 0;
EditorApplication.CallbackFunction tick = null;
tick = () => {
    double age = EditorApplication.timeSinceStartup - started;
    int desired = age < .5 ? 0 : age < 1.1 ? 1 : age < 1.6 ? 2 : age < 2.2 ? 3 : age < 2.8 ? 4 : 5;
    if (!Application.isPlaying || age > 3.1) {
        EditorApplication.update -= tick;
        if (controller != null && controller.Muted != muted) controller.ToggleMute();
        UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
        UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = behavior;
        System.IO.File.WriteAllLines(folder + "/index.txt", rows);
        return;
    }
    if (stage != desired) {
        stage = desired;
        var state = new UnityEngine.InputSystem.LowLevel.MouseState { position = stage == 0 || stage >= 4 ? Vector2.zero : point };
        if (stage == 2) state = state.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left);
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, state);
    }
    if (age < next) return;
    next = age + .1;
    string name = (frame++).ToString("000");
    rows.Add(name + " stage=" + stage + " scale=" + rect.localScale.x.ToString("F3") + " muted=" + controller.Muted);
    ScreenCapture.CaptureScreenshot(folder + "/" + name + ".png");
};
EditorApplication.update += tick;
return "Capturing idle, hover, press, release, and pointer exit through real mouse input.";
