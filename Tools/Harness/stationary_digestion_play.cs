// Real Play Mode movement on a repeatable route. Captures every part passing fixed food cells.
if (!Application.isPlaying) throw new System.InvalidOperationException("Play Mode required.");
var c = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeController>();
if (c.Game.State != GardenSnake.Core.RunState.Ready) throw new System.InvalidOperationException("Fresh Play Mode required.");
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var ct = c.GetType();
ct.GetField("<Game>k__BackingField", flags).SetValue(c, new GardenSnake.Core.SnakeGame(21, 12, 77, 8));
var prefs = new System.Collections.Generic.Dictionary<string, int>();
foreach (string key in new[] { "GardenSnake.Best", "GardenSnake.HasPlayed" })
    if (PlayerPrefs.HasKey(key)) prefs[key] = PlayerPrefs.GetInt(key);
c.PrimaryAction();
var game = c.Game;
var foodField = game.GetType().GetField("<Food>k__BackingField", flags);
var foodCells = new[] { new GardenSnake.Core.Cell(12, 6), new GardenSnake.Core.Cell(14, 6), new GardenSnake.Core.Cell(16, 6) };
foodField.SetValue(game, foodCells[0]);
var skin = c.GetComponentInChildren<GardenSnake.SnakeSkin>();
var mesh = skin.GetComponent<MeshFilter>().sharedMesh;
var settings = (GardenSnake.SnakeSkinSettings)skin.GetType().GetField("settings", flags).GetValue(skin);
var evalFrame = skin.GetType().GetMethod("EvaluateFrame", flags);
var report = new System.Collections.Generic.List<string>();
var errors = new System.Collections.Generic.List<string>();
Application.LogCallback log = (message, trace, type) => { if (type == LogType.Exception || type == LogType.Error) errors.Add(message); };
Application.logMessageReceived += log;
System.IO.Directory.CreateDirectory("Artifacts/shots/stationary-digestion");
var pending = new System.Collections.Generic.List<GardenSnake.Core.Cell>();
var parts = new System.Collections.Generic.HashSet<int>();
int score = 0, length = 8, shots = 0, frames = 0, lastFrame = -1, growthChecks = 0, turns = 0;
float start = Time.unscaledTime, nextShot = start, peakError = 0, anchorError = 0, lastGrowthAt = 0;
var oldHead = game.Body[0];
var oldTail = game.Body[game.Body.Count - 1];
var heading = game.Heading;
bool paused = false, resumed = false;
float pauseAt = 0;
Vector3[] pausedVertices = null;
EditorApplication.CallbackFunction tick = null;
void Finish(string verdict) {
    EditorApplication.update -= tick; Application.logMessageReceived -= log;
    if (game.State == GardenSnake.Core.RunState.Playing) c.TogglePause();
    foreach (string key in new[] { "GardenSnake.Best", "GardenSnake.HasPlayed" }) {
        if (prefs.ContainsKey(key)) PlayerPrefs.SetInt(key, prefs[key]); else PlayerPrefs.DeleteKey(key);
    }
    PlayerPrefs.Save();
    report.Add(verdict);
    report.Add("frames=" + frames + " shots=" + shots + " turns=" + turns + " parts=" + string.Join(",", parts));
    report.Add("anchorDrift=" + anchorError + " peakDistanceFromPickup=" + peakError + " growthChecks=" + growthChecks + " runtimeErrors=" + errors.Count);
    report.AddRange(errors);
    System.IO.File.WriteAllLines("Artifacts/stationary-digestion-verification.txt", report);
}
tick = () => {
    try {
        if (!Application.isPlaying || c == null) { Finish("FAIL interrupted"); return; }
        if (Time.frameCount == lastFrame) return;
        lastFrame = Time.frameCount; frames++;
        float now = Time.unscaledTime;
        if (now - start > 18) { Finish("FAIL timeout"); return; }
        if (game.State == GardenSnake.Core.RunState.Lost) { Finish("FAIL collision"); return; }
        if (game.Body.Count > length) {
            if (pending.Count == 0 || oldTail != pending[0]) { Finish("FAIL growth before tail reached pickup"); return; }
            pending.RemoveAt(0); length = game.Body.Count; growthChecks++; lastGrowthAt = now;
        }
        if (game.Score > score) {
            pending.Add(foodCells[score]); score = game.Score;
            foodField.SetValue(game, score < foodCells.Length ? foodCells[score] : new GardenSnake.Core.Cell(1, 1));
        }
        if (pending.Count != game.Digestion.Count || game.Body.Count != 8 + game.Score - pending.Count) { Finish("FAIL accounting"); return; }
        var anchors = (System.Collections.Generic.List<Vector3>)ct.GetField("digestionAnchors", flags).GetValue(c);
        for (int a = 0; a < pending.Count; a++) {
            int index = (int)game.Digestion[a];
            if (game.Body[index] != pending[a]) { Finish("FAIL apple moved from pickup cell"); return; }
            anchorError = Mathf.Max(anchorError, Vector3.Distance(anchors[a], c.World(pending[a])));
            parts.Add(index);
        }
        // Measure the actual deformed surface against the same surface with only food hidden.
        float blend = Mathf.Clamp01((float)ct.GetField("elapsed", flags).GetValue(c) / (float)ct.GetField("currentStep", flags).GetValue(c));
        var visibility = skin.GetType().GetField("digestionVisibility", flags);
        for (int a = 0; a < pending.Count; a++) {
            float travel = game.Digestion[a] + blend - 1;
            if (travel < 1 || travel > game.Body.Count - 2) continue;
            float maxRatio = 0; Vector3 peak = Vector3.zero;
            int firstRing = Mathf.CeilToInt(Mathf.Max(.5f, travel - .65f) * settings.SamplesPerCell);
            int lastRing = Mathf.FloorToInt(Mathf.Min(game.Body.Count - 1, travel + .65f) * settings.SamplesPerCell);
            for (int ring = firstRing; ring <= lastRing; ring++) {
                float u = ring / (float)settings.SamplesPerCell;
                var frame = evalFrame.Invoke(skin, new object[] { u });
                var type = frame.GetType();
                float width = (float)type.GetField("width").GetValue(frame);
                visibility.SetValue(skin, 0f);
                var baseline = evalFrame.Invoke(skin, new object[] { u });
                visibility.SetValue(skin, 1f);
                float ratio = width / (float)type.GetField("width").GetValue(baseline);
                if (ratio > maxRatio) { maxRatio = ratio; peak = (Vector3)type.GetField("center").GetValue(frame); }
            }
            if (maxRatio > 1.05f) {
                Vector3 offset = peak - c.World(pending[a]); offset.y = 0;
                peakError = Mathf.Max(peakError, offset.magnitude);
            }
        }
        foreach (var v in mesh.vertices)
            if (float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z)) { Finish("FAIL invalid mesh"); return; }
        if (score == 2 && !paused) {
            c.TogglePause(); paused = true; pauseAt = now;
            return;
        }
        if (paused && !resumed) {
            // Take the baseline after the first paused frame: board waves can update
            // after the controller on the frame in which the pause was requested.
            if (pausedVertices == null) { pausedVertices = mesh.vertices; return; }
            if (!mesh.vertices.SequenceEqual(pausedVertices)) { Finish("FAIL paused skin changed"); return; }
            if (now - pauseAt < .4f) return;
            c.TogglePause(); resumed = true; report.Add("PASS pause freezes skin and digestion");
        }
        if (now >= nextShot) {
            nextShot = now + .09f;
            ScreenCapture.CaptureScreenshot("Artifacts/shots/stationary-digestion/frame-" + (shots++).ToString("000") + ".png");
        }
        if (game.Heading != heading) { turns++; heading = game.Heading; }
        if (game.Body[0] != oldHead) {
            oldHead = game.Body[0]; oldTail = game.Body[game.Body.Count - 1];
            if (game.Heading == GardenSnake.Core.Direction.Right && oldHead.X == 16) c.Turn((int)GardenSnake.Core.Direction.Up);
            else if (game.Heading == GardenSnake.Core.Direction.Up && oldHead.Y == 9) c.Turn((int)GardenSnake.Core.Direction.Left);
            else if (game.Heading == GardenSnake.Core.Direction.Left && oldHead.X == 4) c.Turn((int)GardenSnake.Core.Direction.Down);
        }
        if (score == 3 && pending.Count == 0 && now - lastGrowthAt > .3f) {
            Finish(anchorError < .001f && peakError < .08f && growthChecks == 3 && parts.Count >= 8 && turns >= 2 && resumed && errors.Count == 0
                ? "PASS fixed pickups, deformed surface, every body part, turns, tail contact and growth" : "FAIL acceptance thresholds");
        }
    } catch (System.Exception e) { Finish("FAIL " + e); }
};
EditorApplication.update += tick;
return "Stationary digestion capture and measurements started.";
