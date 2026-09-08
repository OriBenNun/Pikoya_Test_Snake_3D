try {
    GardenSnake.Editor.GardenBuilder.BuildWebGL();
    return "SUCCEEDED";
}
catch (System.Exception e) { return "FAILED " + e.Message; }
