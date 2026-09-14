// The snake skin is the game's heaviest per-frame work, and it scales with body length.
// This stages fixed body lengths, paused, and measures the skin draw on a steady frame and on
// a frame where the body grows - the moment a player feels as a stutter when an apple lands.
// Writes Artifacts/skin-load.txt. Run it after any change to SnakeSkin.
if (!Application.isPlaying) throw new System.InvalidOperationException("Play Mode required");
var loop = UnityEngine.Object.FindFirstObjectByType<GardenSnake.GameLoopManager>();
var visuals = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeManager>();
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var game = loop.Game;
var body = (System.Collections.Generic.List<GardenSnake.Core.Cell>)game.GetType().GetField("body", flags).GetValue(game);
var digestion = (System.Collections.Generic.List<float>)game.GetType().GetField("digestion", flags).GetValue(game);
var reset = visuals.GetType().GetMethod("ResetVisuals", flags);
var skinR = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Scripts, "GardenSnake.Skin", 1);
var report = new System.Collections.Generic.List<string>();
int[] lengths = { 10, 30, 60, 100, 160 };
int stage = 0, lastFrame = -1, frames = 0, cycle = 0;
var steady = new System.Collections.Generic.List<double>();
var grow = new System.Collections.Generic.List<double>();
GardenSnake.Core.Cell At(int i) { int row = i / 19; return new GardenSnake.Core.Cell(row % 2 == 0 ? 19 - i % 19 : 1 + i % 19, Mathf.Min(11, row)); }
void Stage() {
    game.Reset(); game.Start(); game.TogglePause();
    body.Clear(); digestion.Clear();
    for (int i = 0; i < lengths[stage]; i++) body.Add(At(i));
    reset.Invoke(visuals, null);
    frames = 0; cycle = 0; steady.Clear(); grow.Clear();
}
Stage();
EditorApplication.CallbackFunction tick = null;
tick = () => {
    if (!Application.isPlaying) { EditorApplication.update -= tick; skinR.Dispose(); return; }
    if (Time.frameCount == lastFrame) return;
    lastFrame = Time.frameCount;
    frames++;
    if (frames <= 20) return;
    double skin = skinR.LastValue / 1000000.0;
    // Alternate: grow one segment, measure that frame, then hold still for four frames.
    int phase = (frames - 20) % 5;
    if (phase == 1) grow.Add(skin); else steady.Add(skin);
    if (phase == 0) {
        // Toggle length by one so the skin sees a changed body count next frame.
        if (body.Count == lengths[stage]) body.Add(At(body.Count)); else body.RemoveAt(body.Count - 1);
        cycle++;
    }
    if (cycle < 60) return;
    string Med(System.Collections.Generic.List<double> v) {
        var q = new System.Collections.Generic.List<double>(v); q.Sort();
        return q[q.Count / 2].ToString("F2") + " (p95 " + q[(int)(q.Count * .95)].ToString("F2") + ")";
    }
    report.Add("length=" + lengths[stage].ToString().PadLeft(3) +
        " steadySkinMs=" + Med(steady) + "  growSkinMs=" + Med(grow));
    stage++;
    if (stage < lengths.Length) { Stage(); return; }
    EditorApplication.update -= tick; skinR.Dispose();
    System.IO.File.WriteAllLines("Artifacts/skin-load.txt", report);
};
EditorApplication.update += tick;
return "measuring the growth frame at " + lengths.Length + " lengths";
