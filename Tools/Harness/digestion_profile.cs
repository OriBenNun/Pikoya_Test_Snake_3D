// Measures the actual skin draw across separate Play Mode frames, without screenshots.
// Fixed paused poses keep the workload identical between before/after measurements.
if (!Application.isPlaying) throw new System.InvalidOperationException("Play Mode required");
var c = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeController>();
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var game = c.Game;
var body = (System.Collections.Generic.List<GardenSnake.Core.Cell>)game.GetType().GetField("body", flags).GetValue(game);
var digestion = (System.Collections.Generic.List<float>)game.GetType().GetField("digestion", flags).GetValue(game);
var skin = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Scripts, "GardenSnake.Skin", 1);
var main = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal, "Main Thread", 1);
var samples = new System.Collections.Generic.List<double>();
var frameSamples = new System.Collections.Generic.List<double>();
var report = new System.Collections.Generic.List<string>();
int[] lengths = { 20, 20, 80, 80 };
int[] apples = { 0, 8, 0, 16 };
int stage = 0, frames = 0, lastFrame = -1;
void Setup() {
    game.Reset(); game.Start(); game.TogglePause();
    body.Clear();
    for (int i = 0; i < lengths[stage]; i++) {
        int row = i / 18;
        body.Add(new GardenSnake.Core.Cell(row % 2 == 0 ? 18 - i % 18 : 1 + i % 18, 2 + row));
    }
    for (int i = 0; i < apples[stage]; i++) digestion.Add((lengths[stage] - 2f) * (i + 1) / (apples[stage] + 1));
    c.GetType().GetMethod("ResetVisuals", flags).Invoke(c, null);
    frames = 0; samples.Clear(); frameSamples.Clear();
}
Setup();
EditorApplication.CallbackFunction tick = null;
tick = () => {
    if (!Application.isPlaying || c == null) { EditorApplication.update -= tick; skin.Dispose(); main.Dispose(); return; }
    if (Time.frameCount == lastFrame) return;
    lastFrame = Time.frameCount;
    frames++;
    if (frames <= 20) return;
    samples.Add(skin.LastValue / 1000000.0);
    frameSamples.Add(main.LastValue / 1000000.0);
    if (samples.Count < 100) return;
    samples.Sort(); frameSamples.Sort();
    report.Add("length=" + lengths[stage] + " pending=" + apples[stage] + " samples=" + samples.Count +
        " skinMedianMs=" + samples[50].ToString("F3") + " skinP95Ms=" + samples[95].ToString("F3") +
        " mainMedianMs=" + frameSamples[50].ToString("F3") + " mainP95Ms=" + frameSamples[95].ToString("F3"));
    stage++;
    if (stage < lengths.Length) { Setup(); return; }
    EditorApplication.update -= tick; skin.Dispose(); main.Dispose();
    System.IO.File.WriteAllLines("Artifacts/digestion-profile.txt", report);
};
EditorApplication.update += tick;
return "Profiling 480 live frames without screenshots";
