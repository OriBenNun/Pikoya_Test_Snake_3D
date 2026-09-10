if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play mode first.");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.isDirty) throw new System.InvalidOperationException("Scene has unsaved edits.");
var hud = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeHud>();
if (hud == null) throw new System.InvalidOperationException("Garden HUD missing.");
var card = hud.transform.Find("Run card");
var oldFill = card.GetComponent<UnityEngine.UI.Image>();
if (oldFill != null) {
    var surface = new GameObject("Surface", typeof(RectTransform), typeof(UnityEngine.UI.Image));
    var rect = (RectTransform)surface.transform;
    rect.SetParent(card, false);
    rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
    rect.offsetMin = rect.offsetMax = Vector2.zero;
    rect.SetSiblingIndex(1);
    EditorUtility.CopySerialized(oldFill, surface.GetComponent<UnityEngine.UI.Image>());
    UnityEngine.Object.DestroyImmediate(oldFill);
}
var ink = GardenSnake.Editor.GardenPalette.TextStrong;
foreach (var item in new[] { ("Description", .85f), ("Key hint", .7f), ("Tally/Caption", .8f) }) {
    var text = card.Find(item.Item1).GetComponent<TMPro.TMP_Text>();
    text.color = new Color(ink.r, ink.g, ink.b, item.Item2);
    EditorUtility.SetDirty(text);
}
var hints = hud.transform.Find("Hints");
if (hints.Find("Paper") == null) {
    var paper = new GameObject("Paper", typeof(RectTransform), typeof(UnityEngine.UI.Image));
    var rect = (RectTransform)paper.transform;
    rect.SetParent(hints, false); rect.SetAsFirstSibling();
    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
    rect.sizeDelta = new Vector2(668, 38);
    var fill = paper.GetComponent<UnityEngine.UI.Image>();
    fill.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GardenSnake/UI/Generated/Pill.png");
    fill.type = UnityEngine.UI.Image.Type.Sliced;
    var color = GardenSnake.Editor.GardenPalette.Paper; color.a = .94f;
    fill.color = color; fill.raycastTarget = false;
}
hints.Find("Line").GetComponent<TMPro.TMP_Text>().color = new Color(ink.r, ink.g, ink.b, .85f);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return "Card surface now covers its shadow; instructions have stronger contrast.";
