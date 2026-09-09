// Play Mode verification: compare actual frames at different serialized settings.
// Temporary SO clones and component JSON backups keep authored settings intact.
if (!Application.isPlaying) throw new System.InvalidOperationException("Play Mode required.");
var c = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeController>();
if (c.Game.State != GardenSnake.Core.RunState.Ready) throw new System.InvalidOperationException("Start in a fresh Play Mode session.");
var wind = UnityEngine.Object.FindFirstObjectByType<GardenSnake.GardenWind>();
var waves = UnityEngine.Object.FindFirstObjectByType<GardenSnake.GridCellWaves>();
var skin = c.GetComponentInChildren<GardenSnake.SnakeSkin>();
var mouth = c.GetComponentInChildren<GardenSnake.SnakeMouth>();
var hud = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeHud>();
var hudFields = new SerializedObject(hud);
var card = (GameObject)hudFields.FindProperty("card").objectReferenceValue;
var scrim = (CanvasGroup)hudFields.FindProperty("scrimGroup").objectReferenceValue;
bool cardWasActive = card.activeSelf;
float scrimAlpha = scrim.alpha;
// Keep score and controls visible, but uncover the snake for the comparison captures.
hud.enabled = false; card.SetActive(false); scrim.alpha = 0;
var animals = UnityEngine.Object.FindObjectsByType<GardenSnake.GardenAnimal>(FindObjectsSortMode.None);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var skinAsset = (GardenSnake.SnakeSkinSettings)typeof(GardenSnake.SnakeSkin).GetField("settings", flags).GetValue(skin);
var mouthAsset = (GardenSnake.SnakeMouthSettings)typeof(GardenSnake.SnakeMouth).GetField("settings", flags).GetValue(mouth);
var skinCopy = UnityEngine.Object.Instantiate(skinAsset);
var mouthCopy = UnityEngine.Object.Instantiate(mouthAsset);
string windJson = EditorJsonUtility.ToJson(wind), waveJson = EditorJsonUtility.ToJson(waves);
var report = new System.Collections.Generic.List<string>();
int failures = 0;
void Check(bool ok, string message) { if (!ok) failures++; report.Add((ok ? "PASS " : "FAIL ") + message); }
void Number(UnityEngine.Object target, string field, float value) {
    var so = new SerializedObject(target); so.FindProperty(field).floatValue = value; so.ApplyModifiedPropertiesWithoutUndo();
}
void Reference(UnityEngine.Object target, string field, UnityEngine.Object value) {
    var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
}
Check(AssetDatabase.Contains(skinAsset) && AssetDatabase.Contains(mouthAsset), "Runtime skin and mouth use persistent tuning assets");
Check(c.Game.DigestionSpeed == new SerializedObject(c).FindProperty("digestionPerStep").floatValue,
    "Simulation uses serialized digestion speed");
Reference(skin, "settings", skinCopy); Reference(mouth, "settings", mouthCopy);
Number(wind, "amplitude", 0); Number(wind, "pulseAmplitude", 0);
waves.Clear();
Number(waves, "ripple.amplitude", 0);
waves.Play(GardenSnake.GridCellWaves.Pattern.Ripple, new GardenSnake.Core.Cell(10, 6));
var stems = (Transform[])typeof(GardenSnake.GardenWind).GetField("stems", flags).GetValue(wind);
var rest = (Quaternion[])typeof(GardenSnake.GardenWind).GetField("rest", flags).GetValue(wind);
var cells = (Transform[])typeof(GardenSnake.GridCellWaves).GetField("cells", flags).GetValue(waves);
var cellRest = (Vector3[])typeof(GardenSnake.GridCellWaves).GetField("rest", flags).GetValue(waves);
var mesh = skin.GetComponent<MeshFilter>().sharedMesh;
var bodyCell = c.Game.Body[0];
var firstPositions = new Vector3[animals.Length];
for (int i = 0; i < animals.Length; i++) firstPositions[i] = animals[i].transform.position;
int stage = 0, lastFrame = -1, frames = 0;
float start = Time.unscaledTime, stageAt = start, windPeak = 0, lowPeak = 0, highPeak = 0, narrowWidth = 0, smallJaw = 0;
bool caughtWave = false;
var errors = new System.Collections.Generic.List<string>();
Application.LogCallback log = (message, trace, type) => { if (type == LogType.Exception || type == LogType.Error) errors.Add(message); };
Application.logMessageReceived += log;
System.IO.Directory.CreateDirectory("Artifacts/shots/tuning");
EditorApplication.CallbackFunction tick = null;
void Finish() {
    EditorApplication.update -= tick; Application.logMessageReceived -= log;
    if (wind != null) EditorJsonUtility.FromJsonOverwrite(windJson, wind);
    if (waves != null) { waves.Clear(); EditorJsonUtility.FromJsonOverwrite(waveJson, waves); }
    if (skin != null) Reference(skin, "settings", skinAsset);
    if (mouth != null) Reference(mouth, "settings", mouthAsset);
    UnityEngine.Object.Destroy(skinCopy); UnityEngine.Object.Destroy(mouthCopy);
    if (c != null) c.enabled = true;
    if (hud != null) { card.SetActive(cardWasActive); scrim.alpha = scrimAlpha; hud.enabled = true; hud.Refresh(); }
    Check(errors.Count == 0, "No runtime errors: " + errors.Count);
    report.AddRange(errors);
    report.Add("failures=" + failures + " frames=" + frames);
    System.IO.File.WriteAllLines("Artifacts/tuning-verification.txt", report);
}
tick = () => {
    try {
        if (!Application.isPlaying || c == null) { Check(false, "Play Mode interrupted"); Finish(); return; }
        if (Time.frameCount == lastFrame) return;
        lastFrame = Time.frameCount; frames++;
        float now = Time.unscaledTime, age = now - stageAt;
        if (now - start > 30) { Check(false, "Verification timeout"); Finish(); return; }
        float height = 0, lean = 0;
        for (int i = 0; i < cells.Length; i++) height = Mathf.Max(height, Mathf.Abs(cells[i].localPosition.y - cellRest[i].y));
        for (int i = 0; i < stems.Length; i++) lean = Mathf.Max(lean, Quaternion.Angle(stems[i].localRotation, rest[i]));
        foreach (var vertex in mesh.vertices)
            if (float.IsNaN(vertex.x) || float.IsNaN(vertex.y) || float.IsNaN(vertex.z)) { Check(false, "Non-finite skin vertex"); Finish(); return; }
        if (stage == 0 && age > .6f) {
            Check(lean < .001f, "Zero wind amplitude and pulse amplitude restore plant rotations");
            Check(height < .001f, "Zero ripple amplitude keeps cells still");
            narrowWidth = mesh.bounds.size.z;
            Number(skinCopy, "radius", .5f);
            Number(wind, "amplitude", 15); Number(wind, "frequency", 2);
            waves.Clear(); Number(waves, "ripple.amplitude", .1f); Number(waves, "maximumHeight", .04f);
            waves.Play(GardenSnake.GridCellWaves.Pattern.Ripple, new GardenSnake.Core.Cell(10, 6));
            stage = 1; stageAt = now;
        } else if (stage == 1) {
            windPeak = Mathf.Max(windPeak, lean); lowPeak = Mathf.Max(lowPeak, height);
            if (age > 2.8f) {
                Check(windPeak > 2, "Serialized wind strength changes live plant rotation: " + windPeak);
                Check(lowPeak > .02f && lowPeak <= .0401f, "Serialized wave limit caps height: " + lowPeak);
                Check(mesh.bounds.size.z > narrowWidth * 1.2f, "Skin SO radius changes mesh width on subsequent frames");
                ScreenCapture.CaptureScreenshot("Artifacts/shots/tuning/wide-skin.png");
                Number(skinCopy, "radius", skinAsset.Radius);
                Number(waves, "maximumHeight", .3f); Number(waves, "ripple.amplitude", .3f);
                waves.Clear(); waves.Play(GardenSnake.GridCellWaves.Pattern.Ripple, new GardenSnake.Core.Cell(10, 6));
                stage = 2; stageAt = now;
            }
        } else if (stage == 2) {
            highPeak = Mathf.Max(highPeak, height);
            if (!caughtWave && height > .15f) {
                caughtWave = true; ScreenCapture.CaptureScreenshot("Artifacts/shots/tuning/strong-wave.png");
            }
            if (age > 2.8f) {
                Check(highPeak > lowPeak * 2, "Larger serialized wave amplitude visibly increases height: " + highPeak);
                Check(c.Game.Body[0] == bodyCell && c.Game.Score == 0, "Environment tuning preserves game coordinates");
                int moved = 0;
                for (int i = 0; i < animals.Length; i++) if (Vector3.Distance(firstPositions[i], animals[i].transform.position) > .02f) moved++;
                Check(moved >= 3, "Wildlife still travels with serialized defaults: " + moved + " animals");
                c.enabled = false; c.Game.Start();
                Number(mouthCopy, "anticipationDistance", 0);
                stage = 3; stageAt = now;
            }
        } else if (stage == 3) {
            mouth.Animate(c.Game, mouth.transform.position + mouth.transform.forward * .8f, Time.unscaledDeltaTime);
            if (age > .4f) {
                Check(mouth.Openness < .001f, "Zero anticipation distance keeps mouth closed");
                Number(mouthCopy, "anticipationDistance", 3);
                Number(mouthCopy, "anticipationRamp", 1);
                stage = 4; stageAt = now;
            }
        } else if (stage == 4) {
            mouth.Animate(c.Game, mouth.transform.position + mouth.transform.forward * .8f, Time.unscaledDeltaTime);
            if (age > .4f) {
                Check(mouth.Openness > .99f, "Mouth SO anticipation distance opens jaw without restarting");
                smallJaw = mouth.transform.Find("Mouth interior").localScale.y;
                var so = new SerializedObject(mouthCopy);
                so.FindProperty("cavityOpeningScale").vector3Value = new Vector3(.18f, .5f, .23f);
                so.ApplyModifiedPropertiesWithoutUndo();
                stage = 5; stageAt = now;
            }
        } else if (stage == 5) {
            mouth.Animate(c.Game, mouth.transform.position + mouth.transform.forward * .8f, Time.unscaledDeltaTime);
            if (age > .2f) {
                Check(mouth.transform.Find("Mouth interior").localScale.y > smallJaw + .1f, "Serialized mouth geometry changes live pose");
                ScreenCapture.CaptureScreenshot("Artifacts/shots/tuning/open-mouth.png");
                Finish();
            }
        }
    } catch (System.Exception e) { Check(false, e.ToString()); Finish(); }
};
EditorApplication.update += tick;
return "Live serialized tuning comparisons started; report: Artifacts/tuning-verification.txt";
