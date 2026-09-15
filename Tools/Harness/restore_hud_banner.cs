// Rebuilds the HUD's Brand plaque and Banner, which a manual scene cleanup dropped while the
// code kept animating them: SnakeHud.Start() and Update() both dereference them every frame, so
// the Web player trapped on the first frame ("RuntimeError: null function") and the Editor threw
// a NullReferenceException per frame. Values come from the last scene that still had them.

var hudObject = GameObject.Find("Garden HUD");
if (hudObject == null) return "no Garden HUD in the scene";
var hud = hudObject.GetComponent<GardenSnake.Presentation.Hud.SnakeHud>();
if (hud == null) return "Garden HUD has no SnakeHud";
var canvas = (RectTransform)hudObject.transform;

var pill = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Generated/Pill.png");
var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/UI/GardenFont.asset");
if (pill == null || font == null) return "missing Pill.png or GardenFont.asset";

var cream = new Color(1f, 0.9764706f, 0.9254902f, 1f);
var ember = new Color(0.8862745f, 0.38431373f, 0.18039216f, 1f);
var bark = new Color(0.11764706f, 0.22745098f, 0.16470589f, 0.9f);

RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
{
    var existing = parent.Find(name);
    if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
    var created = new GameObject(name, typeof(RectTransform));
    var rect = (RectTransform)created.transform;
    rect.SetParent(parent, false);
    rect.anchorMin = rect.anchorMax = anchor;
    rect.pivot = pivot;
    rect.anchoredPosition = position;
    rect.sizeDelta = size;
    return rect;
}

UnityEngine.UI.Image Fill(RectTransform rect, Color color, float pixelsPerUnit)
{
    var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
    image.sprite = pill;
    image.type = UnityEngine.UI.Image.Type.Sliced;
    image.color = color;
    image.raycastTarget = false;
    image.pixelsPerUnitMultiplier = pixelsPerUnit;
    return image;
}

TMPro.TextMeshProUGUI Label(RectTransform rect, string text, float size, Color color,
                            TMPro.TextAlignmentOptions alignment, float spacing)
{
    var label = rect.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
    label.font = font;
    label.text = text;
    label.fontSize = size;
    label.fontStyle = TMPro.FontStyles.Bold;
    label.color = color;
    label.alignment = alignment;
    label.characterSpacing = spacing;
    label.raycastTarget = false;
    label.enableAutoSizing = false;
    return label;
}

// The wordmark in the top-left corner. AnimateChrome() fades this group down while a run is
// playing, so it needs the CanvasGroup rather than a per-graphic alpha.
var brand = Rect("Brand", canvas, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -20), new Vector2(330, 58));
Fill(brand, new Color(cream.r, cream.g, cream.b, 0.94f), 1f);
var brandGroup = brand.gameObject.AddComponent<CanvasGroup>();
brandGroup.blocksRaycasts = false;
brandGroup.interactable = false;
var wordmark = Rect("Wordmark", brand, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -12), new Vector2(300, 28));
Label(wordmark, "GARDEN SNAKE", 21f, bark, TMPro.TextAlignmentOptions.Left, 12f);
var rule = Rect("Rule", brand, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -44), new Vector2(40, 3));
Fill(rule, new Color(ember.r, ember.g, ember.b, 0.95f), 1f);
brand.SetSiblingIndex(0);

// The banner that announces a new best or a finished garden, centred under the top edge.
var banner = Rect("Banner", canvas, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(300, 46));
var bannerFill = Fill(banner, cream, 2.4f);
var bannerText = Rect("Text", banner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 46));
var bannerLabel = Label(bannerText, string.Empty, 18f, ember, TMPro.TextAlignmentOptions.Center, 16f);
// Behind the scrim and the run card, above the rest of the chrome.
var scrim = canvas.Find("Scrim");
banner.SetSiblingIndex(scrim != null ? scrim.GetSiblingIndex() : canvas.childCount - 1);

var serialized = new UnityEditor.SerializedObject(hud);
serialized.FindProperty("brandGroup").objectReferenceValue = brandGroup;
serialized.FindProperty("bannerRoot").objectReferenceValue = banner;
serialized.FindProperty("banner").objectReferenceValue = bannerLabel;
serialized.FindProperty("bannerFill").objectReferenceValue = bannerFill;
serialized.ApplyModifiedPropertiesWithoutUndo();

UnityEditor.EditorUtility.SetDirty(hud);
var scene = hudObject.scene;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return "restored Brand and Banner in " + scene.path;
