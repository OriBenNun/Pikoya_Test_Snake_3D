GardenSnake.Editor.GardenBuilder.SkipConfirmation = true;
try { GardenSnake.Editor.GardenBuilder.CreateScene(); return "OK"; }
catch (System.Exception e) { return "FAIL " + e.Message + "\n" + e.StackTrace; }
finally { GardenSnake.Editor.GardenBuilder.SkipConfirmation = false; }
