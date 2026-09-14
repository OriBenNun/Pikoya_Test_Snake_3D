// Creates any missing tuning asset in Assets/Tuning and assigns it to the components that read it.
// Existing assets are never replaced, so authored values survive. Run after adding a settings type:
//   python Tools/Harness/gs.py eval_file Tools/Harness/bind_tuning.cs
var log = new System.Text.StringBuilder();
const string folder = "Assets/Tuning";
if (!UnityEditor.AssetDatabase.IsValidFolder(folder)) UnityEditor.AssetDatabase.CreateFolder("Assets", "Tuning");

UnityEngine.ScriptableObject Ensure(System.Type type, string name)
{
    string path = folder + "/" + name + ".asset";
    var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.ScriptableObject>(path);
    if (existing != null && existing.GetType() == type) return existing;
    if (existing != null) UnityEditor.AssetDatabase.DeleteAsset(path);
    var created = UnityEngine.ScriptableObject.CreateInstance(type);
    UnityEditor.AssetDatabase.CreateAsset(created, path);
    log.Append("created " + name + "\n");
    return created;
}

void Bind(UnityEngine.Object component, string field, UnityEngine.Object value)
{
    if (component == null) { log.Append("MISSING COMPONENT for " + field + "\n"); return; }
    var serialized = new UnityEditor.SerializedObject(component);
    var property = serialized.FindProperty(field);
    if (property == null) { log.Append("NO FIELD " + field + " on " + component.GetType().Name + "\n"); return; }
    if (property.objectReferenceValue == value) return;
    property.objectReferenceValue = value;
    serialized.ApplyModifiedPropertiesWithoutUndo();
    log.Append("bound " + component.GetType().Name + "." + field + "\n");
}

var rules = Ensure(typeof(GardenSnake.Gameplay.RunRulesSettings), "Run Rules");
var swipe = Ensure(typeof(GardenSnake.Gameplay.SwipeSettings), "Swipe");
var boardWaves = Ensure(typeof(GardenSnake.Presentation.BoardWaveSettings), "Board Waves");
var appleMotion = Ensure(typeof(GardenSnake.Presentation.AppleMotionSettings), "Apple Motion");
var snakeMotion = Ensure(typeof(GardenSnake.Presentation.SnakeMotionSettings), "Snake Motion");
var hudChrome = Ensure(typeof(GardenSnake.Presentation.Hud.HudChromeSettings), "HUD Chrome");
var hudToast = Ensure(typeof(GardenSnake.Presentation.Hud.HudToastSettings), "HUD Toast");
var hudCard = Ensure(typeof(GardenSnake.Presentation.Hud.HudCardSettings), "HUD Card");
var hudCopy = Ensure(typeof(GardenSnake.Presentation.Hud.HudCopySettings), "HUD Copy");
var buttonFeel = Ensure(typeof(GardenSnake.Presentation.Hud.ButtonFeelSettings), "Button Feel");
var gauge = Ensure(typeof(GardenSnake.Presentation.Hud.SpeedGaugeSettings), "Speed Gauge");
var soundMix = Ensure(typeof(GardenSnake.SoundMixSettings), "Sound Mix");
var feelBeats = Ensure(typeof(GardenSnake.FeelBeatSettings), "Feel Beats");
var waveCues = Ensure(typeof(GardenSnake.WaveCueSettings), "Wave Cues");
var reactions = Ensure(typeof(GardenSnake.ReactionSettings), "Reactions");
var wind = Ensure(typeof(GardenSnake.Garden.WindSettings), "Wind");
var animalMotion = Ensure(typeof(GardenSnake.Garden.AnimalMotionSettings), "Animal Motion");
var bird = (GardenSnake.Garden.BirdSettings)Ensure(typeof(GardenSnake.Garden.BirdSettings), "Bird");
var butterfly = (GardenSnake.Garden.ButterflySettings)Ensure(typeof(GardenSnake.Garden.ButterflySettings), "Butterfly");
var bunny = (GardenSnake.Garden.BunnySettings)Ensure(typeof(GardenSnake.Garden.BunnySettings), "Bunny");
var turtle = (GardenSnake.Garden.TurtleSettings)Ensure(typeof(GardenSnake.Garden.TurtleSettings), "Turtle");
var ladybug = (GardenSnake.Garden.CrawlerSettings)Ensure(typeof(GardenSnake.Garden.CrawlerSettings), "Ladybug");

// The values that differ from the class defaults, as the scene had them authored.
butterfly.restSeconds = 2.5f;
butterfly.flightHeight = .5f;
UnityEditor.EditorUtility.SetDirty(butterfly);
turtle.travelSeconds = 8f;
turtle.restSeconds = 9f;
turtle.strideCycles = 5f;
turtle.strideAngle = 20f;
UnityEditor.EditorUtility.SetDirty(turtle);
ladybug.travelSeconds = 6f;
UnityEditor.EditorUtility.SetDirty(ladybug);

Bind(UnityEngine.Object.FindAnyObjectByType<GardenSnake.Gameplay.GameLoopManager>(), "rules", rules);
Bind(UnityEngine.Object.FindAnyObjectByType<GardenSnake.PlayerController>(), "swipe", swipe);
Bind(UnityEngine.Object.FindAnyObjectByType<GardenSnake.Presentation.GridCellWaves>(), "waves", boardWaves);
Bind(UnityEngine.Object.FindAnyObjectByType<GardenSnake.Presentation.AppleView>(), "motion", appleMotion);
Bind(UnityEngine.Object.FindAnyObjectByType<GardenSnake.Presentation.SnakeManager>(), "motion", snakeMotion);
var hud = UnityEngine.Object.FindAnyObjectByType<GardenSnake.Presentation.Hud.SnakeHud>();
Bind(hud, "chrome", hudChrome);
Bind(hud, "toastStyle", hudToast);
Bind(hud, "cardStyle", hudCard);
Bind(hud, "copy", hudCopy);
Bind(UnityEngine.Object.FindAnyObjectByType<GardenSnake.Presentation.Hud.SpeedGauge>(), "dial", gauge);
foreach (var each in UnityEngine.Object.FindObjectsByType<GardenSnake.Presentation.Hud.ButtonFeel>(UnityEngine.FindObjectsSortMode.None))
    Bind(each, "feel", buttonFeel);
var feedback = UnityEngine.Object.FindAnyObjectByType<GardenSnake.FeedbackManager>();
Bind(feedback, "sound", soundMix);
Bind(feedback, "beats", feelBeats);
Bind(feedback, "waves", waveCues);
Bind(feedback, "reactions", reactions);
Bind(UnityEngine.Object.FindAnyObjectByType<GardenSnake.Garden.GardenWind>(), "wind", wind);
foreach (var animal in UnityEngine.Object.FindObjectsByType<GardenSnake.Garden.GardenAnimal>(UnityEngine.FindObjectsSortMode.None))
{
    Bind(animal, "motion", animalMotion);
    UnityEngine.ScriptableObject species = null;
    if (animal is GardenSnake.Garden.GardenBird) species = bird;
    else if (animal is GardenSnake.Garden.GardenButterfly) species = butterfly;
    else if (animal is GardenSnake.Garden.GardenBunny) species = bunny;
    else if (animal is GardenSnake.Garden.GardenTurtle) species = turtle;
    else if (animal is GardenSnake.Garden.GardenLadybug) species = ladybug;
    Bind(animal, "species", species);
}

var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
UnityEditor.AssetDatabase.SaveAssets();
log.Append("saved " + scene.path);
return log.ToString();
