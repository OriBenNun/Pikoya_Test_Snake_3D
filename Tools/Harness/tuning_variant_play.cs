if (!Application.isPlaying) throw new System.InvalidOperationException("Play Mode required.");
var c = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeController>();
var waves = UnityEngine.Object.FindFirstObjectByType<GardenSnake.GridCellWaves>();
var skin = c.GetComponentInChildren<GardenSnake.SnakeSkin>();
var mesh = skin.GetComponent<MeshFilter>().sharedMesh;
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var report = new System.Collections.Generic.List<string>();
int failures = 0, moveCount = 0, pickupStep = -1;
void Check(bool ok, string message) { if (!ok) failures++; report.Add((ok ? "PASS " : "FAIL ") + message); }
Check(c.Game.Body.Count == 2 && c.Game.InitialLength == 2, "Serialized initial length used by real controller Awake");
Check(c.Game.DigestionSpeed == 1 && c.Game.TurnBufferSize == 3, "Stationary digestion and serialized turn buffer used by simulation");
Check(c.Game.Food.X - c.Game.Body[0].X == 3, "Serialized first apple distance applied");
Check(mesh.vertexCount == (2 * 3 + 1) * 9 + 165, "Skin SO mesh quality applied: " + mesh.vertexCount + " vertices");
for (int i = 0; i < 10; i++) waves.Play(GardenSnake.GridCellWaves.Pattern.Bloom, new GardenSnake.Core.Cell(10, 6));
Check(waves.ActiveCount == 3, "Serialized concurrency limit keeps only three waves");
var prefKeys = new[] { "GardenSnake.Best", "GardenSnake.HasPlayed" };
var prefs = new System.Collections.Generic.Dictionary<string, int>();
foreach (var key in prefKeys) if (PlayerPrefs.HasKey(key)) prefs[key] = PlayerPrefs.GetInt(key);
var errors = new System.Collections.Generic.List<string>();
Application.LogCallback log = (message, trace, type) => { if (type == LogType.Exception || type == LogType.Error) errors.Add(message); };
Application.logMessageReceived += log;
c.PrimaryAction();
var previous = c.Game.Body[0];
float start = Time.unscaledTime;
EditorApplication.CallbackFunction tick = null;
void Finish() {
    EditorApplication.update -= tick; Application.logMessageReceived -= log;
    Check(errors.Count == 0, "No runtime errors");
    report.AddRange(errors); report.Add("failures=" + failures);
    System.IO.File.WriteAllLines("Artifacts/tuning-variant-verification.txt", report);
    foreach (var key in prefKeys) { if (prefs.ContainsKey(key)) PlayerPrefs.SetInt(key, prefs[key]); else PlayerPrefs.DeleteKey(key); }
    PlayerPrefs.Save();
    c.enabled = false;
}
tick = () => {
    try {
        if (!Application.isPlaying || c == null) { Check(false, "Play Mode interrupted"); Finish(); return; }
        if (c.Game.State == GardenSnake.Core.RunState.Paused) c.TogglePause();
        if (Time.unscaledTime - start > 12 || c.Game.State == GardenSnake.Core.RunState.Lost) { Check(false, "Timeout or premature collision"); Finish(); return; }
        if (c.Game.Body[0] == previous) return;
        previous = c.Game.Body[0]; moveCount++;
        if (c.Game.Score == 1 && pickupStep < 0) {
            pickupStep = moveCount;
            Check(c.Game.Body.Count == 2 && c.Game.Digestion.Count == 1, "Pickup waits for digestion at custom speed");
            typeof(GardenSnake.Core.SnakeGame).GetField("<Food>k__BackingField", flags).SetValue(c.Game, new GardenSnake.Core.Cell(-1, -1));
        }
        if (pickupStep >= 0 && c.Game.Body.Count == 3) {
            Check(moveCount - pickupStep == 4 && c.Game.Digestion.Count == 0, "Quarter-cell digestion grows tail after exactly four movement steps");
            ScreenCapture.CaptureScreenshot("Artifacts/shots/tuning/custom-rules.png");
            Finish();
        }
    } catch (System.Exception e) { Check(false, e.ToString()); Finish(); }
};
EditorApplication.update += tick;
return "Custom startup settings verification started.";
