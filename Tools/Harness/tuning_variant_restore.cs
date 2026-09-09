if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
var c = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeController>();
var waves = UnityEngine.Object.FindFirstObjectByType<GardenSnake.GridCellWaves>();
var skin = (GardenSnake.SnakeSkinSettings)new SerializedObject(c).FindProperty("skinSettings").objectReferenceValue;
foreach (var pair in new[] {
    new System.Collections.Generic.KeyValuePair<string, UnityEngine.Object>("Controller", c),
    new System.Collections.Generic.KeyValuePair<string, UnityEngine.Object>("Waves", waves),
    new System.Collections.Generic.KeyValuePair<string, UnityEngine.Object>("Skin", skin) }) {
    string key = "TuningVariant." + pair.Key;
    string json = SessionState.GetString(key, "");
    if (string.IsNullOrEmpty(json)) throw new System.InvalidOperationException("Missing variant backup: " + key);
    EditorJsonUtility.FromJsonOverwrite(json, pair.Value);
    EditorUtility.SetDirty(pair.Value);
    SessionState.EraseString(key);
}
AssetDatabase.SaveAssetIfDirty(skin);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
return "Restored and saved original Inspector/SO settings.";
