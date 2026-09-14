// Records main-thread frame cost and managed allocation while the autopilot plays.
// Architecture agnostic: it only reads profiler counters, never game internals.
if (!Application.isPlaying) throw new System.InvalidOperationException("Play Mode required");
string label = System.IO.File.Exists("Artifacts/perf-label.txt") ? System.IO.File.ReadAllText("Artifacts/perf-label.txt").Trim() : "run";
var main = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal, "Main Thread", 1);
var alloc = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory, "GC Allocated In Frame", 1);
var frameMs = new System.Collections.Generic.List<double>();
var frameAlloc = new System.Collections.Generic.List<double>();
int frames = 0, lastFrame = -1;
EditorApplication.CallbackFunction tick = null;
tick = () => {
    if (!Application.isPlaying) { EditorApplication.update -= tick; main.Dispose(); alloc.Dispose(); return; }
    if (Time.frameCount == lastFrame) return;
    lastFrame = Time.frameCount;
    frames++;
    if (frames <= 60) return;                       // let the run settle before sampling
    frameMs.Add(main.LastValue / 1000000.0);
    frameAlloc.Add(alloc.LastValue);
    if (frameMs.Count < 600) return;
    EditorApplication.update -= tick; main.Dispose(); alloc.Dispose();
    var sorted = new System.Collections.Generic.List<double>(frameMs); sorted.Sort();
    var allocSorted = new System.Collections.Generic.List<double>(frameAlloc); allocSorted.Sort();
    double total = 0; foreach (double v in frameAlloc) total += v;
    System.IO.File.WriteAllLines("Artifacts/perf-" + label + ".txt", new[] {
        "label=" + label,
        "samples=" + sorted.Count,
        "mainMedianMs=" + sorted[sorted.Count / 2].ToString("F3"),
        "mainP95Ms=" + sorted[(int)(sorted.Count * .95)].ToString("F3"),
        "mainMaxMs=" + sorted[sorted.Count - 1].ToString("F3"),
        "allocMedianBytes=" + allocSorted[allocSorted.Count / 2].ToString("F0"),
        "allocP95Bytes=" + allocSorted[(int)(allocSorted.Count * .95)].ToString("F0"),
        "allocTotalBytes=" + total.ToString("F0")
    });
};
EditorApplication.update += tick;
return "profiling " + label;
