if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before importing meshes.");
string source = SessionState.GetString("ModelSweep.MeshSource", "Artifacts/model-sweep/wildlife");
int count = 0;
foreach (var path in System.IO.Directory.GetFiles(source, "*.meshbin")) {
    string name = System.IO.Path.GetFileNameWithoutExtension(path);
    string target = "Assets/Art/Wildlife/" + name + ".asset";
    var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(target);
    bool create = mesh == null && (name == "LadybugShell" || name == "LadybugMarkings");
    if (mesh == null && !create) throw new System.InvalidOperationException("Missing existing mesh: " + target);
    Vector3[] vertices, normals;
    int[] indices;
    using (var reader = new System.IO.BinaryReader(System.IO.File.OpenRead(path))) {
        int vertexCount = reader.ReadInt32(), indexCount = reader.ReadInt32();
        if (vertexCount < 3 || vertexCount > 65535 || indexCount % 3 != 0)
            throw new System.InvalidOperationException("Invalid mesh header: " + name);
        vertices = new Vector3[vertexCount]; normals = new Vector3[vertexCount]; indices = new int[indexCount];
        for (int i = 0; i < vertexCount; i++) {
            vertices[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            normals[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            if (!float.IsFinite(vertices[i].sqrMagnitude) || normals[i].sqrMagnitude < .9f)
                throw new System.InvalidOperationException("Invalid vertex: " + name);
        }
        for (int i = 0; i < indexCount; i++) {
            indices[i] = reader.ReadInt32();
            if (indices[i] < 0 || indices[i] >= vertexCount) throw new System.InvalidOperationException("Invalid index: " + name);
        }
        if (reader.BaseStream.Position != reader.BaseStream.Length)
            throw new System.InvalidOperationException("Trailing mesh data: " + name);
    }
    if (create) mesh = new Mesh { name = name };
    else Undo.RecordObject(mesh, "Polish wildlife model");
    mesh.Clear(); mesh.vertices = vertices; mesh.normals = normals; mesh.triangles = indices;
    mesh.RecalculateBounds();
    if (create) AssetDatabase.CreateAsset(mesh, target);
    EditorUtility.SetDirty(mesh);
    AssetDatabase.SaveAssetIfDirty(mesh);
    count++;
}
return "Imported " + count + " Blender wildlife meshes.";
