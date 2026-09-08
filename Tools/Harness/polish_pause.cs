// Run after the wave capture suite has completed.
var c = UnityEngine.Object.FindAnyObjectByType<GardenSnake.SnakeController>();
var w = UnityEngine.Object.FindAnyObjectByType<GardenSnake.GridCellWaves>();
c.PrimaryAction(); c.enabled = false; w.Clear();
w.Play(GardenSnake.GridCellWaves.Pattern.Bloom, c.Game.Body[0]);
float began = Time.unscaledTime;
int phase = 0;
var positions = new System.Collections.Generic.List<Vector3>();
var cells = new System.Collections.Generic.List<Transform>();
foreach(Transform t in w.transform) if(t.name.StartsWith("Patch ")) cells.Add(t);
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () => {
    if(!UnityEditor.EditorApplication.isPlaying || c == null) { UnityEditor.EditorApplication.update -= tick; return; }
    float age = Time.unscaledTime - began;
    if(phase == 0 && age > .3f) {
        c.TogglePause(); foreach(var cell in cells) positions.Add(cell.localPosition); phase=1;
        ScreenCapture.CaptureScreenshot("Artifacts/wave-paused.png");
    }
    if(phase == 1 && age > 1) {
        bool fixedCells = c.Game.State == GardenSnake.Core.RunState.Paused;
        for(int i=0;i<cells.Count;i++) fixedCells &= Vector3.Distance(cells[i].localPosition,positions[i]) < .0001f;
        System.IO.File.WriteAllText("Artifacts/wave-pause-verification.txt", fixedCells ? "PASS paused waves remain fixed" : "FAIL paused waves drift");
        c.TogglePause(); phase=2;
    }
    if(phase == 2 && age > 1.2f) {
        bool changed=false;
        for(int i=0;i<cells.Count;i++) changed |= Vector3.Distance(cells[i].localPosition,positions[i]) > .005f;
        System.IO.File.AppendAllText("Artifacts/wave-pause-verification.txt", changed ? "\nPASS waves resume" : "\nFAIL waves did not resume");
        c.enabled = true;
        UnityEditor.EditorApplication.update -= tick;
    }
};
UnityEditor.EditorApplication.update += tick;
return "pause check started";
