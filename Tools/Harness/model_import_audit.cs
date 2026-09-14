var rows = new System.Collections.Generic.List<string>();
var failures = new System.Collections.Generic.List<string>();
foreach (var path in System.IO.Directory.GetFiles("Assets/GardenSnake/Art/Models", "*.fbx")) {
    var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
    if (model == null) { failures.Add("Missing model " + path); continue; }
    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path)) {
        if (!(asset is Mesh mesh)) continue;
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string guid, out long id);
        rows.Add(System.IO.Path.GetFileNameWithoutExtension(path) + "|" + mesh.name + "|" + id);
    }
    var prefabPath = "Assets/GardenSnake/Prefabs/" + System.IO.Path.GetFileNameWithoutExtension(path) + ".prefab";
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    if (prefab == null) { failures.Add("Missing prefab " + prefabPath); continue; }
    foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
        if (filter.sharedMesh == null) failures.Add("Missing mesh " + prefab.name + "/" + filter.name);
    foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
        foreach (var material in renderer.sharedMaterials)
            if (material == null || material.shader == null || !material.shader.isSupported)
                failures.Add("Invalid material " + prefab.name + "/" + renderer.name);
}
rows.Sort(System.StringComparer.Ordinal);
string label = SessionState.GetString("ModelSweep.Audit", "before");
System.IO.Directory.CreateDirectory("Artifacts/model-sweep/unity-" + label);
System.IO.File.WriteAllLines("Artifacts/model-sweep/unity-" + label + "/mesh-ids.txt", rows);
System.IO.File.WriteAllLines("Artifacts/model-sweep/unity-" + label + "/failures.txt", failures);
return "models=20 meshes=" + rows.Count + " failures=" + failures.Count + " " + string.Join(";", failures);
