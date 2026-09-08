// Run after entering Play Mode. Drives real keyboard events and records observations each apple.
var controller = UnityEngine.Object.FindAnyObjectByType<GardenSnake.SnakeController>();
if (controller == null || controller.Game.State != GardenSnake.Core.RunState.Ready) return "Start from Ready";
string label = UnityEditor.SessionState.GetString("Polish.Scenario", "record");
float seconds = label == "record" ? 65 : label == "zero" ? 8 : 18;
var lines = new System.Collections.Generic.List<string>();
int checks = 0, failures = 0, maxScore = 0, lastScore = -1, bigCount = 0, whisperCount = 0;
double ends = UnityEditor.EditorApplication.timeSinceStartup + seconds + .2;
UnityEditor.EditorApplication.CallbackFunction observe = null;
observe = () => {
    if (!UnityEditor.EditorApplication.isPlaying || controller == null) {
        UnityEditor.EditorApplication.update -= observe;
        System.IO.File.WriteAllText("Artifacts/" + label + "-verification.txt", "ABORTED"); return;
    }
    int score = controller.Game.Score;
    if (score != lastScore) {
        lastScore = score;
        maxScore = Mathf.Max(maxScore, score);
        int expectedBig = controller.Record.Eligible && score > controller.Record.PreviousBest ? 1 : 0;
        int expectedSmall = expectedBig == 1 ? Mathf.Max(0, score - controller.Record.PreviousBest - 1) : 0;
        bool pass = controller.RecordCelebrations == expectedBig && controller.RecordWhispers == expectedSmall;
        checks++; if (!pass) failures++;
        bigCount = Mathf.Max(bigCount, controller.RecordCelebrations);
        whisperCount = Mathf.Max(whisperCount, controller.RecordWhispers);
        lines.Add((pass ? "PASS" : "FAIL") + " score=" + score + " baseline=" + controller.Record.PreviousBest + " eligible=" + controller.Record.Eligible + " big=" + controller.RecordCelebrations + " whisper=" + controller.RecordWhispers);
        if (expectedBig == 1 && expectedSmall == 0) ScreenCapture.CaptureScreenshot("Artifacts/" + label + "-record-crossing.png");
        if (expectedSmall == 1) ScreenCapture.CaptureScreenshot("Artifacts/" + label + "-record-whisper.png");
    }
    if (UnityEditor.EditorApplication.timeSinceStartup >= ends) {
        UnityEditor.EditorApplication.update -= observe;
        lines.Add("checks=" + checks + " failures=" + failures + " maxScore=" + maxScore + " big=" + bigCount + " whisper=" + whisperCount);
        System.IO.File.WriteAllLines("Artifacts/" + label + "-verification.txt", lines);
    }
};
UnityEditor.EditorApplication.update += observe;
return GardenSnake.Editor.GardenPlaytest.Run(label, seconds, 5);
