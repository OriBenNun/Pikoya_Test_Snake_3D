if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.isDirty) throw new System.InvalidOperationException("Scene has unsaved edits.");
var hud = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeHud>();
foreach (var button in hud.GetComponentsInChildren<UnityEngine.UI.Button>(true))
    if (button.GetComponent<GardenSnake.ButtonFeel>() == null)
        button.gameObject.AddComponent<GardenSnake.ButtonFeel>();
GardenSnake.Editor.GardenSprites.PlayIcon("IconPlay", 64);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return "Button feedback installed; Resume glyph rasterized with pixel-space antialiasing.";
