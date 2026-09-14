var output = "Artifacts/model-audit/unity";
System.IO.Directory.CreateDirectory(output);
var cameraObject = new GameObject("Temporary model audit camera");
var camera = cameraObject.AddComponent<Camera>();
camera.clearFlags = CameraClearFlags.SolidColor;
camera.backgroundColor = new Color(.16f,.19f,.18f);
camera.cullingMask = 1 << 31;
camera.orthographic = true;
camera.nearClipPlane = .01f;
camera.farClipPlane = 500;
var texture = new RenderTexture(480,480,24);
camera.targetTexture = texture;
var previous = RenderTexture.active;
try {
    foreach (var path in System.IO.Directory.GetFiles("Assets/Art/Models", "*.fbx")) {
        var name = System.IO.Path.GetFileNameWithoutExtension(path);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + name + ".prefab");
        if (prefab == null) prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var model = UnityEngine.Object.Instantiate(prefab, new Vector3(10000,10000,10000), Quaternion.identity);
        try {
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
            var renderers = model.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            camera.orthographicSize = size*.72f;
            for (int i=0;i<2;i++) {
                var direction = i==0 ? new Vector3(1,1.1f,2) : new Vector3(-1,.8f,-2);
                camera.transform.position = bounds.center + direction.normalized*size*3;
                camera.transform.LookAt(bounds.center);
                camera.Render();
                RenderTexture.active = texture;
                var image = new Texture2D(480,480,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,480,480),0,0); image.Apply();
                System.IO.File.WriteAllBytes(output+"/"+name+(i==0?"-front.png":"-back.png"),image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
        } finally { UnityEngine.Object.DestroyImmediate(model); }
    }
} finally {
    RenderTexture.active = previous;
    camera.targetTexture = null;
    UnityEngine.Object.DestroyImmediate(texture);
    UnityEngine.Object.DestroyImmediate(cameraObject);
}
return "Captured all 20 imported models from two angles.";
