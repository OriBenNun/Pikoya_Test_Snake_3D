var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (Application.isPlaying || scene.isDirty) throw new System.InvalidOperationException("Saved scene required");
AssetDatabase.ImportAsset("Assets/Art/Models/Bush.fbx", ImportAssetOptions.ForceUpdate);
GardenSnake.Editor.GardenBuilder.Prefab("Bush");
GardenSnake.Editor.GardenWildlifeBuilder.Install();
AssetDatabase.SaveAssets();
return "Imported flowering bushes; installed connected perch";
