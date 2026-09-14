if(Application.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before importing wildlife models.");
AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
var definitions=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("Assets/Art/Models/Wildlife/Rigs.json"));
foreach(var model in definitions) {
    string path="Assets/Art/Models/Wildlife/"+(string)model["species"]+".fbx";
    var importer=(ModelImporter)AssetImporter.GetAtPath(path);
    if(importer==null) throw new System.InvalidOperationException("Missing FBX: "+path);
    importer.importAnimation=false;
    importer.bakeAxisConversion=false;
    importer.importNormals=ModelImporterNormals.Import;
    importer.importTangents=ModelImporterTangents.None;
    importer.optimizeMeshPolygons=false;
    importer.optimizeMeshVertices=false;
    importer.isReadable=true;
    var names=new System.Collections.Generic.HashSet<string>();
    foreach(var node in model["nodes"]) foreach(var name in node["materials"]) names.Add((string)name);
    foreach(string name in names) {
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/"+name+".mat");
        if(material==null) throw new System.InvalidOperationException("Missing material: "+name);
        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name),material);
    }
    importer.SaveAndReimport();
}
return "Imported "+definitions.Count+" wildlife FBX models with shared garden materials.";
