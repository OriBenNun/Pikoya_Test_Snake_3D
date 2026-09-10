if (!Application.isPlaying) throw new System.InvalidOperationException("Play Mode required.");
var controller = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeController>();
var mouth = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeMouth>();
string label = SessionState.GetString("PolishSweep.Label", "mouth");
string folder = "Artifacts/shots/" + label;
System.IO.Directory.CreateDirectory(folder);
var rows = new System.Collections.Generic.List<string>();
var errors = new System.Collections.Generic.List<string>();
var original = new System.Collections.Generic.Dictionary<string, int>();
foreach (string key in new[] { "GardenSnake.Best", "GardenSnake.HasPlayed", "GardenSnake.Muted" })
    if (PlayerPrefs.HasKey(key)) original[key] = PlayerPrefs.GetInt(key);
Application.LogCallback log = (message, trace, type) => { if (type == LogType.Error || type == LogType.Exception) errors.Add(message); };
Application.logMessageReceived += log;
int index = 0;
double started = EditorApplication.timeSinceStartup, next = 0;
EditorApplication.CallbackFunction tick = null;
tick = () => {
    double age = EditorApplication.timeSinceStartup - started;
    if (!Application.isPlaying || controller == null || age > 26) {
        EditorApplication.update -= tick;
        Application.logMessageReceived -= log;
        rows.Add("runtimeErrors=" + errors.Count);
        rows.AddRange(errors);
        System.IO.File.WriteAllLines(folder + "/index.txt", rows);
        foreach (string key in new[] { "GardenSnake.Best", "GardenSnake.HasPlayed", "GardenSnake.Muted" }) {
            if (original.ContainsKey(key)) PlayerPrefs.SetInt(key, original[key]); else PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();
        return;
    }
    if (age < next) return;
    var food = controller.Game.Food;
    var head = controller.Game.Body[0];
    bool nearFood = System.Math.Abs(food.X - head.X) + System.Math.Abs(food.Y - head.Y) <= 3;
    next = age + (nearFood || mouth.Openness > .15f ? .07 : .65);
    Vector3 point = Camera.main.WorldToScreenPoint(mouth.transform.position + Vector3.up * .6f);
    string name = (index++).ToString("000");
    rows.Add(name + " t=" + age.ToString("F2") + " heading=" + controller.Game.Heading +
        " open=" + mouth.Openness.ToString("F2") + " score=" + controller.Game.Score +
        " x=" + point.x.ToString("F0") + " y=" + (Screen.height - point.y).ToString("F0"));
    ScreenCapture.CaptureScreenshot(folder + "/" + name + ".png");
};
EditorApplication.update += tick;
return GardenSnake.Editor.GardenPlaytest.Run(label + "-wide", 25, 3);
