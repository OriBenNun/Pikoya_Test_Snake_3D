// The tree's "Bird perch" marker survived the scene cleanup but the GardenBird that used it lost
// the reference, so the bird just hopped around the lawn. Bind the marker back to whichever bird
// stands under it.

UnityEngine.Transform perch = null;
foreach (var candidate in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(
             UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
{
    if (candidate.name != "Bird perch") continue;
    if (perch != null) return "more than one 'Bird perch' in the scene";
    perch = candidate;
}
if (perch == null) return "no 'Bird perch' marker in the scene";

GardenSnake.Garden.GardenBird nearest = null;
var best = float.MaxValue;
foreach (var bird in UnityEngine.Object.FindObjectsByType<GardenSnake.Garden.GardenBird>(
             UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
{
    // The perch sits above the bird's home patch, so compare on the ground plane only.
    var offset = bird.transform.position - perch.position;
    var distance = new Vector2(offset.x, offset.z).magnitude;
    if (distance >= best) continue;
    best = distance;
    nearest = bird;
}
if (nearest == null) return "no GardenBird in the scene";

var serialized = new UnityEditor.SerializedObject(nearest);
serialized.FindProperty("perch").objectReferenceValue = perch;
serialized.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(nearest);

var scene = nearest.gameObject.scene;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return "perch " + perch.position.ToString("0.00") + " bound to the bird at " +
       nearest.transform.position.ToString("0.00") + " (" + best.ToString("0.00") + " away)";
