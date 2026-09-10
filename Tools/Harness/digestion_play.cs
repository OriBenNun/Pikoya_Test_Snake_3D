// Run with: unity command eval_file --file Tools/Harness/digestion_play.cs
// Drives the real controller in Play Mode. Food placement is controlled for repeatable overlap and turns.
if (!Application.isPlaying) throw new System.InvalidOperationException("Play Mode required");
var controller = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeController>();
var mouth = controller.GetComponentInChildren<GardenSnake.SnakeMouth>();
var original = new System.Collections.Generic.Dictionary<string, int>();
foreach (var key in new[] { "GardenSnake.Best", "GardenSnake.HasPlayed" })
    if (PlayerPrefs.HasKey(key)) original[key] = PlayerPrefs.GetInt(key);
var report = new System.Collections.Generic.List<string>();
var errors = new System.Collections.Generic.List<string>();
Application.LogCallback log = (message, trace, type) => {
    if (type == LogType.Exception || type == LogType.Error) errors.Add(message);
};
Application.logMessageReceived += log;
System.IO.Directory.CreateDirectory("Artifacts/shots/digestion");
controller.PrimaryAction();
int score = 0, length = 3, maxPending = 0, turns = 0, shots = 0, stage = 0;
float start = Time.unscaledTime, nextShot = start, stageAt = start, pausedMouth = 0;
bool anticipation = false, delayed = false, growth = false, bentBulge = false;
var pausedDigestion = new float[0];
var lastHead = controller.Game.Body[0];
var lastHeading = controller.Game.Heading;
var foodField = typeof(GardenSnake.Core.SnakeGame).GetField("<Food>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
EditorApplication.CallbackFunction tick = null;
void Finish(string reason) {
    EditorApplication.update -= tick;
    Application.logMessageReceived -= log;
    report.Add(reason);
    report.Add("anticipation=" + anticipation + " delayedGrowth=" + delayed + " tailGrowth=" + growth + " bentBulge=" + bentBulge);
    report.Add("maxPending=" + maxPending + " turns=" + turns + " frames=" + shots + " runtimeErrors=" + errors.Count);
    report.AddRange(errors);
    foreach (var key in new[] { "GardenSnake.Best", "GardenSnake.HasPlayed" }) {
        if (original.ContainsKey(key)) PlayerPrefs.SetInt(key, original[key]); else PlayerPrefs.DeleteKey(key);
    }
    PlayerPrefs.Save();
    System.IO.File.WriteAllLines("Artifacts/digestion-verification.txt", report);
}
tick = () => {
    try {
        if (!Application.isPlaying || controller == null) { Finish("FAIL Play Mode interrupted"); return; }
        var game = controller.Game;
        float now = Time.unscaledTime;
        if (now >= nextShot && stage < 3) {
            nextShot = now + (now - start < 4 ? .08f : .35f);
            ScreenCapture.CaptureScreenshot("Artifacts/shots/digestion/frame-" + (shots++).ToString("000") + ".png");
        }
        if (game.Score == 0 && mouth.Openness > .5f) anticipation = true;
        maxPending = Mathf.Max(maxPending, game.Digestion.Count);
        if (game.Score >= 8 && stage == 2)
        {
            // Move the next apple out of this route so all pending digestion can finish.
            foodField.SetValue(game, new GardenSnake.Core.Cell(game.Body[0].X < game.Width / 2 ? game.Width - 2 : 1,
                game.Body[0].Y < game.Height / 2 ? game.Height - 2 : 1));
        }
        if (game.Body.Count != 3 + game.Score - game.Digestion.Count) { Finish("FAIL growth accounting"); return; }
        if (game.Score > score) {
            if (score == 0) delayed = game.Body.Count == 3 && game.Digestion.Count == 1;
            report.Add("Pickup score=" + game.Score + " length=" + game.Body.Count + " pending=" + game.Digestion.Count);
            score = game.Score;
            // Keep feeding until several apples are traveling together.
            if (score < 8) {
                for (int offset = 0; offset < 4; offset++) {
                    int d = ((int)game.Heading + offset) % 4;
                    if (d == ((int)game.Heading + 2) % 4) continue;
                    var direction = GardenSnake.Core.SnakeGame.Offset((GardenSnake.Core.Direction)d);
                    var next = game.Body[0] + direction;
                    var food = next + direction;
                    if (food.X < 1 || food.Y < 1 || food.X >= game.Width - 1 || food.Y >= game.Height - 1) continue;
                    if (game.Body.Contains(next) || game.Body.Contains(food)) continue;
                    foodField.SetValue(game, food);
                    break;
                }
            }
        }
        if (game.Body.Count > length) { growth = true; length = game.Body.Count; report.Add("Tail grew to " + length); }
        if (lastHeading != game.Heading) {
            turns++;
            if (game.Digestion.Count > 0) bentBulge = true;
            lastHeading = game.Heading;
        }
        if (stage == 0 && game.Score >= 3 && game.Digestion.Count > 0) {
            controller.TogglePause();
            pausedDigestion = game.Digestion.ToArray(); pausedMouth = mouth.Openness;
            stage = 1; stageAt = now;
            return;
        }
        if (stage == 1) {
            if (!game.Digestion.SequenceEqual(pausedDigestion) || mouth.Openness != pausedMouth) { Finish("FAIL pause advanced digestion or jaw"); return; }
            if (now - stageAt < .8f) return;
            controller.TogglePause(); report.Add("PASS pause freezes digestion and jaw; resumed"); stage = 2;
        }
        if (stage == 2 && game.Score >= 8 && game.Digestion.Count == 0) {
            stage = 3; report.Add("PASS all swallowed apples became tail cells");
        }
        if (stage == 3 && game.State == GardenSnake.Core.RunState.Lost) { stage = 4; stageAt = now; }
        if (stage == 4 && now - stageAt > 1) {
            controller.PrimaryAction(); stage = 5; stageAt = now;
            if (game.Digestion.Count != 0 || game.Body.Count != 3 || mouth.Openness != 0) { Finish("FAIL restart retained digestion"); return; }
            report.Add("PASS collision and restart clear digestion and jaw");
        }
        if (stage == 5) {
            Finish(anticipation && delayed && growth && bentBulge && maxPending >= 2 && errors.Count == 0 ? "PASS digestion presentation flow" : "FAIL missing acceptance condition");
            return;
        }
        if (now - start > 50 || (game.State == GardenSnake.Core.RunState.Lost && stage < 3)) { Finish("FAIL timeout or premature collision"); return; }
        if (stage >= 3 || game.State != GardenSnake.Core.RunState.Playing || game.Body[0] == lastHead) return;
        lastHead = game.Body[0];
        int best = -1, cost = int.MaxValue;
        for (int d = 0; d < 4; d++) {
            if (d == ((int)game.Heading + 2) % 4) continue;
            var next = game.Body[0] + GardenSnake.Core.SnakeGame.Offset((GardenSnake.Core.Direction)d);
            if (next.X < 0 || next.Y < 0 || next.X >= game.Width || next.Y >= game.Height) continue;
            bool blocked = false;
            for (int i = 0; i < game.Body.Count; i++) if (game.Body[i] == next) blocked = true;
            if (blocked) continue;
            int distance = System.Math.Abs(next.X - game.Food.X) + System.Math.Abs(next.Y - game.Food.Y);
            if (distance < cost) { best = d; cost = distance; }
        }
        if (best >= 0 && best != (int)game.Heading) controller.Turn(best);
    } catch (System.Exception e) { Finish("FAIL " + e); }
};
EditorApplication.update += tick;
return "Live digestion verification started; report: Artifacts/digestion-verification.txt";
