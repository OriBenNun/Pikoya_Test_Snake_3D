// Visual system verification in Play Mode. The controller is suspended; its grid state remains untouched.
var c = UnityEngine.Object.FindAnyObjectByType<GardenSnake.SnakeController>();
var waves = UnityEngine.Object.FindAnyObjectByType<GardenSnake.GridCellWaves>();
if (c.Game.State != GardenSnake.Core.RunState.Ready) return "Start from Ready";
c.PrimaryAction();
c.enabled = false;
waves.Clear();
var cells = new System.Collections.Generic.List<Transform>();
foreach (Transform child in waves.transform) if (child.name.StartsWith("Patch ")) cells.Add(child);
var rest = new Vector3[cells.Count];
for (int i = 0; i < cells.Count; i++) rest[i] = cells[i].localPosition;
var head = c.Game.Body[0];
int stage = 0, checks = 0, failures = 0;
float peak = 0, nearPeak = 0, farPeak = 0;
bool near = false, far = false, capture = false;
float begun = Time.unscaledTime;
var lines = new System.Collections.Generic.List<string>();
System.Action<bool,string> check = (ok, message) => { checks++; if (!ok) failures++; lines.Add((ok ? "PASS " : "FAIL ") + message); };
waves.Play(GardenSnake.GridCellWaves.Pattern.Ripple, new GardenSnake.Core.Cell(0, 0));
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () => {
    if (!UnityEditor.EditorApplication.isPlaying || waves == null) { UnityEditor.EditorApplication.update -= tick; return; }
    float age = Time.unscaledTime - begun;
    bool bounded = true;
    float max = 0;
    for (int i = 0; i < cells.Count; i++) {
        float height = cells[i].localPosition.y - rest[i].y;
        if (float.IsNaN(height) || height < -.081f || height > .481f) bounded = false;
        max = Mathf.Max(max, height);
        if (stage == 0 && cells[i].name == "Patch 0,0") { nearPeak = Mathf.Max(nearPeak, height); if (height > .03f) near = true; }
        if (stage == 0 && cells[i].name == "Patch 20,11") { farPeak = Mathf.Max(farPeak, height); if (height > .03f) far = true; }
    }
    peak = Mathf.Max(peak, max);
    if (!bounded) check(false, "Cell height outside safe range");
    if (!capture && age > .85f) {
        capture = true;
        ScreenCapture.CaptureScreenshot("Artifacts/wave-" + stage + ".png");
    }
    // The visual clock deliberately slows during stalls; wait for completion, with a real timeout.
    if (age < .9f || (waves.ActiveCount > 0 && age < 15f)) return;
    check(peak > .12f, "Pattern " + stage + " visibly lifts cells: " + peak);
    check(max < .001f && waves.ActiveCount == 0, "Pattern " + stage + " returns all cells to rest");
    check(c.Game.Body[0] == head && c.Game.Score == 0, "Visual waves preserve simulation coordinates");
    if (stage == 0) check(near && far, "Corner ripple reaches source and opposite corner: source=" + nearPeak + " opposite=" + farPeak);
    stage++;
    if(stage == 5) {
        waves.Clear(); c.enabled = true;
        UnityEditor.EditorApplication.update -= tick;
        lines.Add("checks=" + checks + " failures=" + failures);
        System.IO.File.WriteAllLines("Artifacts/wave-verification.txt", lines);
        return;
    }
    peak = 0; capture = false; begun = Time.unscaledTime;
    var pattern = (GardenSnake.GridCellWaves.Pattern)Mathf.Min(stage, 3);
    waves.Play(pattern, stage == 2 ? new GardenSnake.Core.Cell(20, 11) : head);
    if (stage == 4) {
        for (int i = 0; i < 12; i++) waves.Play(GardenSnake.GridCellWaves.Pattern.Bloom, new GardenSnake.Core.Cell(i, i % 12));
        check(waves.ActiveCount == 8, "Concurrent wave buffer stays bounded at eight");
    }
};
UnityEditor.EditorApplication.update += tick;
return "five visual wave scenarios started";
