// Run from the paused restart at the end of GardenPlaythrough.
var c = UnityEngine.Object.FindAnyObjectByType<GardenSnake.SnakeController>();
if (c.Game.State != GardenSnake.Core.RunState.Paused) return "Run after controls verification";
c.TogglePause();
var mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("PolishSwipeMouse");
var previousBackground = UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
var from = new Vector2(Screen.width * .5f, Screen.height * .5f);
var to = from + new Vector2(0, Screen.height * .16f);
var start = c.Game.Body[0];
int phase = 0, frame = Time.frameCount;
float began = Time.unscaledTime;
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () => {
    if (!UnityEditor.EditorApplication.isPlaying || c == null) { UnityEditor.EditorApplication.update -= tick; return; }
    if(phase==0 && Time.frameCount > frame+2) {
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState { position=from }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left)); phase=1; frame=Time.frameCount;
    } else if(phase==1 && Time.frameCount > frame+2) {
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState { position=to }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left)); phase=2; frame=Time.frameCount;
    } else if(phase==2 && Time.frameCount > frame+2) {
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState { position=to }); phase=3;
    }
    bool moved = c.Game.Heading==GardenSnake.Core.Direction.Up && c.Game.Body[0].Y > start.Y;
    if(moved || Time.unscaledTime-began>3) {
        System.IO.File.WriteAllText("Artifacts/swipe-verification.txt",moved ? "PASS real pointer swipe turns snake upward" : "FAIL swipe did not turn snake");
        ScreenCapture.CaptureScreenshot("Artifacts/polish-final.png");
        UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
        UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = previousBackground;
        UnityEditor.EditorApplication.update -= tick;
    }
};
UnityEditor.EditorApplication.update += tick;
return "swipe started";
