var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
return new { scene = scene.path, dirty = scene.isDirty, playing = Application.isPlaying,
    best = PlayerPrefs.GetInt("GardenSnake.Best", -1) };
