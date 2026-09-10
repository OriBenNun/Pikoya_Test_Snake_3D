if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (scene.isDirty) throw new System.InvalidOperationException("Scene has unsaved edits.");
var hud = UnityEngine.Object.FindFirstObjectByType<GardenSnake.SnakeHud>();
if (hud == null) throw new System.InvalidOperationException("Garden HUD missing.");
GardenSnake.Editor.GardenHud.ConfigureInstructions(hud);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return "Instructions organized inside the Start and Pause panels.";
