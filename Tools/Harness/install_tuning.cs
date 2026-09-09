if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before installing tuning assets.");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.isDirty) throw new System.InvalidOperationException("Scene has unsaved edits; preserve them before installing tuning.");
var controller = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeController>();
if (controller == null) throw new System.InvalidOperationException("Open the Garden Snake scene first.");
GardenSnake.Editor.GardenTuning.Bind(controller);
// Serialize the new component defaults along with the shared asset references.
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
AssetDatabase.SaveAssets();
return "Saved Inspector tuning and shared snake assets in " + GardenSnake.Editor.GardenTuning.Folder;
