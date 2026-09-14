try {
    GardenSnake.Editor.GardenBuild.BuildWebGL();
    return "SUCCEEDED";
}
catch (System.Exception e) { return "FAILED " + e.Message; }
