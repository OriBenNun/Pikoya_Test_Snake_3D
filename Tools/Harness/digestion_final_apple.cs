// A near-full live board checks the final pickup separately from its delayed victory.
if (!Application.isPlaying) throw new System.InvalidOperationException("Play Mode required");
var c = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeController>();
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var game = c.Game;
var gameType = game.GetType();
var controllerType = c.GetType();
var body = (System.Collections.Generic.List<GardenSnake.Core.Cell>)gameType.GetField("body", flags).GetValue(game);
var digestion = (System.Collections.Generic.List<float>)gameType.GetField("digestion", flags).GetValue(game);
var cycle = new System.Collections.Generic.List<GardenSnake.Core.Cell>();
for (int x = 0; x < game.Width; x++) cycle.Add(new GardenSnake.Core.Cell(x, 0));
for (int y = 1; y < game.Height; y++)
    for (int x = 1; x < game.Width; x++) cycle.Add(new GardenSnake.Core.Cell(y % 2 == 1 ? game.Width - x : x, y));
for (int y = game.Height - 1; y > 0; y--) cycle.Add(new GardenSnake.Core.Cell(0, y));
game.Reset(); game.Start();
body.Clear(); body.Add(cycle[0]);
for (int i = cycle.Count - 1; i >= 2; i--) body.Add(cycle[i]);
int finalScore = cycle.Count - 3;
gameType.GetField("<Score>k__BackingField", flags).SetValue(game, finalScore - 1);
gameType.GetField("<Food>k__BackingField", flags).SetValue(game, cycle[1]);
int originalBest = c.Best;
controllerType.GetField("best", flags).SetValue(c, cycle.Count + 1);
controllerType.GetField("bestUnsaved", flags).SetValue(c, false);
controllerType.GetMethod("ResetVisuals", flags).Invoke(c, null);
var report = new System.Collections.Generic.List<string>();
var errors = new System.Collections.Generic.List<string>();
Application.LogCallback log = (message, trace, type) => { if (type == LogType.Exception || type == LogType.Error) errors.Add(message); };
Application.logMessageReceived += log;
int stage = 0;
float start = Time.unscaledTime;
EditorApplication.CallbackFunction tick = null;
void Finish(string result) {
    EditorApplication.update -= tick; Application.logMessageReceived -= log;
    controllerType.GetField("best", flags).SetValue(c, originalBest);
    controllerType.GetField("bestUnsaved", flags).SetValue(c, false);
    report.Add(result); report.Add("runtimeErrors=" + errors.Count); report.AddRange(errors);
    System.IO.File.WriteAllLines("Artifacts/digestion-final-apple.txt", report);
}
tick = () => {
    try {
        if (Time.unscaledTime - start > 8) { Finish("FAIL timeout"); return; }
        if (stage == 0 && game.Score == finalScore) {
            var apple = (Transform)controllerType.GetField("apple", flags).GetValue(c);
            if (game.HasFood || apple.gameObject.activeSelf || body.Count != cycle.Count - 1 || digestion.Count != 1 || game.State != GardenSnake.Core.RunState.Playing)
            { Finish("FAIL final pickup did not wait for digestion"); return; }
            report.Add("PASS final apple reserved last cell, hid food, and delayed victory");
            // Place digestion on the tail; the real next Update performs growth.
            digestion[0] = body.Count - GardenSnake.Core.SnakeGame.DigestionPerStep;
            stage = 1;
        }
        if (stage == 1 && game.State == GardenSnake.Core.RunState.Won) {
            ScreenCapture.CaptureScreenshot("Artifacts/shots/digestion-victory.png");
            Finish(body.Count == cycle.Count && digestion.Count == 0 && game.Score == finalScore && errors.Count == 0
                ? "PASS tail growth fills board and wins without awarding another apple" : "FAIL victory accounting");
        }
        if (game.State == GardenSnake.Core.RunState.Lost) Finish("FAIL unexpected collision");
    } catch (System.Exception e) { Finish("FAIL " + e); }
};
EditorApplication.update += tick;
return "Live final-apple verification started";
