if (!Application.isPlaying) throw new System.InvalidOperationException("Play mode required.");
var controller = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeController>();
string label = SessionState.GetString("PolishSweep.Label", "polish");
string folder = "Artifacts/shots/" + label;
System.IO.Directory.CreateDirectory(folder);
var rows = new System.Collections.Generic.List<string>();
int index = 0, lastScore = -1;
var lastState = (GardenSnake.Core.RunState)(-1);
double started = EditorApplication.timeSinceStartup, next = 0, burstUntil = 0;
EditorApplication.CallbackFunction tick = null;
tick = () => {
    double now = EditorApplication.timeSinceStartup;
    if (!Application.isPlaying || controller == null || now - started > 18) {
        EditorApplication.update -= tick;
        System.IO.File.WriteAllLines(folder + "/index.txt", rows);
        return;
    }
    var game = controller.Game;
    if (game.State != lastState || game.Score != lastScore) {
        lastState = game.State; lastScore = game.Score;
        burstUntil = now + 1.1; next = now;
    }
    if (now < next) return;
    next = now + (now < burstUntil ? .12 : .8);
    string name = (index++).ToString("000");
    string title = "", tally = "";
    foreach (var text in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None)) {
        if (text.name == "Title") title = text.text;
        if (text.name == "Value") tally = text.text;
    }
    rows.Add(name + " t=" + (now - started).ToString("F2") + " state=" + game.State + " score=" + game.Score + " title=" + title + " tally=" + tally);
    ScreenCapture.CaptureScreenshot(folder + "/" + name + ".png");
};
EditorApplication.update += tick;
return "storyboard started: " + folder;
