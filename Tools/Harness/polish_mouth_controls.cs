if (!Application.isPlaying) throw new System.InvalidOperationException("Play Mode required.");
var controller = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeController>();
var mouth = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeMouth>();
if (controller.Game.State != GardenSnake.Core.RunState.Ready) throw new System.InvalidOperationException("Start from Ready.");
var behavior = UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("MouthVerificationKeyboard");
var rows = new System.Collections.Generic.List<string>();
var errors = new System.Collections.Generic.List<string>();
Application.LogCallback log = (message, trace, type) => { if (type == LogType.Error || type == LogType.Exception) errors.Add(message); };
Application.logMessageReceived += log;
int stage = 0, releaseAt = -1;
double stageAt = EditorApplication.timeSinceStartup;
float pausedOpen = 0;
var pausedPositions = new System.Collections.Generic.List<Vector3>();
var pausedScales = new System.Collections.Generic.List<Vector3>();
Transform[] face = null;
EditorApplication.CallbackFunction tick = null;
void Press(UnityEngine.InputSystem.Key key) {
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(key));
    releaseAt = Time.frameCount + 2;
}
void Next(string message) {
    rows.Add("PASS: " + message);
    ScreenCapture.CaptureScreenshot("Artifacts/mouth-check-" + stage + ".png");
    stage++;
    stageAt = EditorApplication.timeSinceStartup;
}
void Finish(string error) {
    EditorApplication.update -= tick;
    Application.logMessageReceived -= log;
    UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = behavior;
    if (error != null) rows.Add("FAIL: " + error);
    rows.Add("runtimeErrors=" + errors.Count);
    rows.AddRange(errors);
    System.IO.File.WriteAllLines("Artifacts/mouth-controls.txt", rows);
}
tick = () => {
    try {
        if (!Application.isPlaying) { Finish("Play Mode interrupted."); return; }
        if (releaseAt >= 0 && Time.frameCount >= releaseAt) {
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
            releaseAt = -1;
        }
        if (EditorApplication.timeSinceStartup - stageAt > 8) { Finish("Timeout at stage " + stage); return; }
        var game = controller.Game;
        switch (stage) {
            case 0:
                if (mouth.Openness != 0) throw new System.Exception("Mouth is not at rest on Ready.");
                Press(UnityEngine.InputSystem.Key.Space);
                Next("Ready face rests; keyboard starts run.");
                break;
            case 1:
                if (mouth.Openness < .75f) return;
                if (game.Score != 0) throw new System.Exception("Anticipation started after eating.");
                if (mouth.transform.Find("Excited eye").localScale.y < 1.5f) throw new System.Exception("Eyes did not enlarge.");
                Press(UnityEngine.InputSystem.Key.P);
                Next("Huge mouth and excited eyes appear before the first apple.");
                break;
            case 2:
                if (game.State != GardenSnake.Core.RunState.Paused) return;
                pausedOpen = mouth.Openness;
                face = mouth.GetComponentsInChildren<Transform>();
                foreach (var part in face) { pausedPositions.Add(part.localPosition); pausedScales.Add(part.localScale); }
                Next("Pause preserves anticipation.");
                break;
            case 3:
                if (EditorApplication.timeSinceStartup - stageAt < .7) return;
                if (mouth.Openness != pausedOpen) throw new System.Exception("Jaw moved during pause.");
                for (int i = 0; i < face.Length; i++)
                    if (Vector3.Distance(face[i].localPosition, pausedPositions[i]) > .001f || Vector3.Distance(face[i].localScale, pausedScales[i]) > .001f)
                        throw new System.Exception("Face changed during pause: " + face[i].name);
                Press(UnityEngine.InputSystem.Key.Space);
                Next("All facial positions and scales stay frozen while paused.");
                break;
            case 4:
                if (game.Score < 1) return;
                Press(game.Food.Y > game.Body[0].Y ? UnityEngine.InputSystem.Key.DownArrow : UnityEngine.InputSystem.Key.UpArrow);
                Next("Resume reaches the apple and starts swallowing.");
                break;
            case 5:
                if (EditorApplication.timeSinceStartup - stageAt < .7) return;
                if (mouth.Openness > .01f) throw new System.Exception("Mouth did not close when steering away.");
                if (Vector3.Distance(mouth.transform.Find("Excited eye").localScale, Vector3.one) > .001f)
                    throw new System.Exception("Excited eyes did not settle.");
                var swallowed = mouth.transform.parent.Find("Swallowed apple");
                if (swallowed.gameObject.activeSelf) throw new System.Exception("Swallowed apple remained visible.");
                Next("Mouth and eyes settle; swallowed apple disappears.");
                break;
            case 6:
                if (game.State != GardenSnake.Core.RunState.Lost) return;
                Next("Normal wall collision remains intact.");
                break;
            case 7:
                if (EditorApplication.timeSinceStartup - stageAt < 1.1) return;
                Press(UnityEngine.InputSystem.Key.Space);
                Next("Results allow a fresh run.");
                break;
            case 8:
                if (game.State != GardenSnake.Core.RunState.Playing) return;
                if (game.Score != 0 || game.Body.Count != 3) throw new System.Exception("Restart did not reset the snake.");
                Press(UnityEngine.InputSystem.Key.P);
                Next("Restart resets score and length.");
                break;
            case 9:
                if (game.State == GardenSnake.Core.RunState.Paused) Finish(null);
                break;
        }
    } catch (System.Exception error) { Finish(error.Message); }
};
EditorApplication.update += tick;
return "Driving mouth anticipation, pause/resume, swallow, recovery, collision, and restart through keyboard input.";
