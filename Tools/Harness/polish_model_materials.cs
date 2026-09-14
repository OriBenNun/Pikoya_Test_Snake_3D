if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before saving model materials.");
const string folder = "Assets/Materials/";
Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>(folder + name + ".mat");
Color Hex(string text) { ColorUtility.TryParseHtmlString("#" + text, out var color); return color; }
void Tune(string name, float smoothness, string color = null) {
    var material = Load(name);
    if (material == null) throw new System.InvalidOperationException("Missing material " + name);
    Undo.RecordObject(material, "Polish model material");
    material.SetFloat("_Smoothness", smoothness);
    material.SetFloat("_Metallic", 0);
    if (color != null) {
        material.SetColor("_BaseColor", Hex(color));
        material.SetColor("_Color", Hex(color));
    }
    EditorUtility.SetDirty(material);
    AssetDatabase.SaveAssetIfDirty(material);
}
foreach (string name in new[] { "FoliageSun", "FoliageShade" }) {
    if (Load(name) != null) continue;
    var material = new Material(Load("Foliage"));
    material.name = name;
    AssetDatabase.CreateAsset(material, folder + name + ".mat");
}
Tune("Coral", .56f, "ED3D2B");
Tune("Ink", .60f);
Tune("Jade", .38f);
Tune("JadeHead", .40f);
Tune("Lime", .34f);
Tune("Cream", .32f);
Tune("Blush", .34f);
Tune("Aqua", .48f);
Tune("Base", .50f);
Tune("Bark", .18f);
Tune("Wood", .16f);
Tune("Stone", .29f);
Tune("Foliage", .28f, "70AC43");
Tune("FoliageSun", .29f, "84B94C");
Tune("FoliageShade", .25f, "619B3B");
Tune("Leaf", .34f);
Tune("Petal", .33f);
Tune("PetalBlush", .31f);
Tune("PetalButter", .31f);
Tune("PetalLilac", .31f, "C4ACEB");
Tune("PollenRose", .30f);
Tune("WildlifeFeather", .36f);
Tune("WildlifeShell", .41f);
Tune("WildlifeShellLight", .41f);
Tune("WildlifeSkin", .34f);
Tune("WildlifeWing", .32f);
foreach (string model in new[] { "Tree", "Bush" }) {
    var importer = (ModelImporter)AssetImporter.GetAtPath("Assets/Art/Models/" + model + ".fbx");
    foreach (string material in new[] { "FoliageSun", "FoliageShade" })
        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), material), Load(material));
    importer.SaveAndReimport();
}
var lavender = (ModelImporter)AssetImporter.GetAtPath("Assets/Art/Models/Lavender.fbx");
lavender.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Aqua"), Load("PetalLilac"));
lavender.SaveAndReimport();
return "Saved 26 shared model materials; remapped foliage variants and lilac lavender. Board, lighting and UI unchanged.";
