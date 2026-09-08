UnityEditor.EditorApplication.delayCall += () => {
    try {
        UnityEditor.PlayerSettings.companyName = "Pikoya Demo";
        UnityEditor.PlayerSettings.productName = "Garden Snake";
        UnityEditor.PlayerSettings.WebGL.compressionFormat = UnityEditor.WebGLCompressionFormat.Disabled;
        UnityEditor.PlayerSettings.WebGL.template = "PROJECT:Garden";
        UnityEditor.PlayerSettings.WebGL.dataCaching = true;
        UnityEditor.PlayerSettings.runInBackground = true;
        var options = new UnityEditor.BuildPlayerOptions {
            scenes = new[] { GardenSnake.Editor.GardenBuilder.ScenePath },
            locationPathName = System.IO.Path.GetFullPath("Builds/WebGL"),
            target = UnityEditor.BuildTarget.WebGL,
            targetGroup = UnityEditor.BuildTargetGroup.WebGL,
            options = UnityEditor.BuildOptions.None
        };
        var report = UnityEditor.BuildPipeline.BuildPlayer(options);
        var summary = report.summary;
        System.IO.File.WriteAllText("Artifacts/web-build.txt",
            summary.result + " size=" + summary.totalSize + " errors=" + summary.totalErrors
            + " path=" + summary.outputPath + " exists=" + System.IO.Directory.Exists(summary.outputPath));
    }
    catch (System.Exception e) { System.IO.File.WriteAllText("Artifacts/web-build.txt", "EXCEPTION " + e.ToString()); }
};
return "Build scheduled";
