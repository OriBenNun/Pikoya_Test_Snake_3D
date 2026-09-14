// Measures managed allocation per frame while switching one suspect off at a time.
// Everything runs from the same paused pose, so the only variable is the suspect.
if (!Application.isPlaying) throw new System.InvalidOperationException("Play Mode required");
var alloc = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory, "GC Allocated In Frame", 1);
var main = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal, "Main Thread", 1);
var times = new System.Collections.Generic.List<double>();
var gauge = UnityEngine.Object.FindAnyObjectByType<GardenSnake.SpeedGauge>();
var snake = UnityEngine.Object.FindAnyObjectByType<GardenSnake.SnakeManager>();
var hud = UnityEngine.Object.FindAnyObjectByType<GardenSnake.SnakeHud>();
var wind = UnityEngine.Object.FindAnyObjectByType<GardenSnake.GardenWind>();
var animals = UnityEngine.Object.FindObjectsByType<GardenSnake.GardenAnimal>(FindObjectsSortMode.None);
var waves = UnityEngine.Object.FindAnyObjectByType<GardenSnake.GridCellWaves>();
var feedback = UnityEngine.Object.FindAnyObjectByType<GardenSnake.FeedbackManager>();
var stages = new (string name, System.Action off, System.Action on)[] {
    ("everything on", () => {}, () => {}),
    ("gauge off", () => gauge.enabled = false, () => gauge.enabled = true),
    ("hud off", () => hud.enabled = false, () => hud.enabled = true),
    ("snake off", () => snake.enabled = false, () => snake.enabled = true),
    ("wind off", () => wind.enabled = false, () => wind.enabled = true),
    ("animals off", () => { foreach (var a in animals) a.enabled = false; }, () => { foreach (var a in animals) a.enabled = true; }),
    ("waves off", () => waves.enabled = false, () => waves.enabled = true),
    ("feedback off", () => feedback.enabled = false, () => feedback.enabled = true),
};
var report = new System.Collections.Generic.List<string>();
var samples = new System.Collections.Generic.List<double>();
int stage = 0, frames = 0, lastFrame = -1;
stages[0].off();
EditorApplication.CallbackFunction tick = null;
tick = () => {
    if (!Application.isPlaying) { EditorApplication.update -= tick; alloc.Dispose(); main.Dispose(); return; }
    if (Time.frameCount == lastFrame) return;
    lastFrame = Time.frameCount;
    frames++;
    if (frames <= 20) return;
    samples.Add(alloc.LastValue);
    times.Add(main.LastValue / 1000000.0);
    if (samples.Count < 200) return;
    samples.Sort(); times.Sort();
    report.Add(stages[stage].name.PadRight(16) + " medianBytes=" + samples[100].ToString("F0") +
        " mainMedianMs=" + times[100].ToString("F3") + " mainP95Ms=" + times[190].ToString("F3"));
    stages[stage].on();
    stage++;
    frames = 0; samples.Clear(); times.Clear();
    if (stage < stages.Length) { stages[stage].off(); return; }
    EditorApplication.update -= tick; alloc.Dispose(); main.Dispose();
    System.IO.File.WriteAllLines("Artifacts/alloc-hunt.txt", report);
};
EditorApplication.update += tick;
return "hunting across " + stages.Length + " stages";
