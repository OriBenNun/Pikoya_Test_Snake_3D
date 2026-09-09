// Run with: unity command eval_file Tools/Harness/fluff_audit.cs
// Supplemental Play Mode feedback probe; the real-input playtest covers actual gameplay.
if (!Application.isPlaying) throw new System.InvalidOperationException("Enter Play mode first.");
var animals = UnityEngine.Object.FindObjectsByType<GardenSnake.GardenAnimal>(FindObjectsSortMode.None);
var wind = UnityEngine.Object.FindFirstObjectByType<GardenSnake.GardenWind>();
var feel = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeFeel>();
var plants = new System.Collections.Generic.List<Transform>();
var rotations = new System.Collections.Generic.List<Quaternion>();
var windData = new SerializedObject(wind).FindProperty("stems");
for (int i = 0; i < windData.arraySize; i++)
{
    var plant = (Transform)windData.GetArrayElementAtIndex(i).objectReferenceValue;
    if (plant == null || !plant.gameObject.activeInHierarchy) continue;
    plants.Add(plant); rotations.Add(plant.localRotation);
}
var moved = new bool[plants.Count];
var reactions = new int[animals.Length];
var states = new System.Collections.Generic.HashSet<string>[animals.Length];
for (int i = 0; i < animals.Length; i++) { reactions[i] = animals[i].ReactionCount; states[i] = new System.Collections.Generic.HashSet<string>(); }
int beat = 0, shot = 0, samples = 0, boardIntrusions = 0, overlappingAnimals = 0;
float nextSample = 0, nextShot = 0;
float start = Time.time;
var report = new System.Text.StringBuilder();
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    if (!Application.isPlaying) { EditorApplication.update -= tick; return; }
    float age = Time.time - start;
    if (beat < 5 && age > 1 + beat * 4)
    {
        switch (beat)
        {
            case 0: feel.Pickup(Vector3.zero, 8); break;
            case 1: feel.Death(Vector3.zero); break;
            case 2: feel.RunStart(Vector3.zero); break;
            case 3: feel.NewBest(Vector3.zero); break;
            case 4: feel.RecordApple(Vector3.zero); break;
        }
        beat++;
    }
    if (age >= nextSample)
    {
        nextSample = age + .15f; samples++;
        for (int i = 0; i < plants.Count; i++)
            if (Quaternion.Angle(rotations[i], plants[i].localRotation) > .1f) moved[i] = true;
        for (int i = 0; i < animals.Length; i++)
        {
            states[i].Add(animals[i].State.ToString());
            Vector3 p = animals[i].transform.position;
            if (Mathf.Abs(p.x) < 10.8f && Mathf.Abs(p.z) < 6.3f) boardIntrusions++;
            for (int j = i + 1; j < animals.Length; j++)
                if (Vector3.Distance(p, animals[j].transform.position) < .5f) overlappingAnimals++;
        }
    }
    if (age >= nextShot)
    {
        nextShot = age + .8f;
        ScreenCapture.CaptureScreenshot("Artifacts/shots/fluff-audit-" + shot.ToString("00") + ".png"); shot++;
    }
    if (age < 23) return;
    EditorApplication.update -= tick;
    int movingCount = 0; foreach (bool motion in moved) if (motion) movingCount++;
    report.AppendLine("Supplemental Play Mode probe: five explicit SnakeFeel beats, not simulated gameplay.");
    report.AppendLine("Samples=" + samples + "; moving vegetation=" + movingCount + "/" + plants.Count);
    report.AppendLine("Board intrusions=" + boardIntrusions + "; animal proximity violations=" + overlappingAnimals);
    report.AppendLine("Wildlife colliders=" + GameObject.Find("Garden wildlife").GetComponentsInChildren<Collider>().Length);
    for (int i = 0; i < animals.Length; i++)
        report.AppendLine(animals[i].name + ": states=" + string.Join(",", states[i]) + "; beats=" + (animals[i].ReactionCount - reactions[i]));
    System.IO.File.WriteAllText("Artifacts/fluff-audit.txt", report.ToString());
};
EditorApplication.update += tick;
return "Observing 23 seconds of Play Mode motion and five feedback beats.";
