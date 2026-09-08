using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GardenSnake.Editor
{
    /// <summary>Reproducible authoring and build entry points. All generated assets stay in GardenSnake.</summary>
    public static class GardenBuilder
    {
        private const string Root = "Assets/GardenSnake";
        public const string ScenePath = Root + "/Scenes/GardenSnake.unity";
        private static readonly Color Ink = Hex("233E32");
        private static readonly Color Muted = Hex("61745F");
        private static readonly Color Cream = Hex("FFFBEA");
        private static TMP_FontAsset font;

        [MenuItem("Garden Snake/Rebuild game scene")]
        public static void CreateScene()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before rebuilding.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save or discard your unsaved scene changes before rebuilding.");
            Directory.CreateDirectory(Root + "/Scenes");
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Root + "/UI");
            AssetDatabase.Refresh();
            PrepareMaterials();
            PrepareFont();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "GardenSnake";

            var camera = new GameObject("Garden Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 17, -12.8f);
            camera.transform.LookAt(new Vector3(0, 0, .15f));
            camera.orthographic = true;
            camera.orthographicSize = 8.7f;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 70;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("E3EAD9");
            camera.allowHDR = false;
            var light = new GameObject("Afternoon Sun", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = .85f;
            light.color = Hex("FFF8EB");
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(48, -32, 0);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Hex("899C91");
            RenderSettings.ambientIntensity = .85f;
            RenderSettings.skybox = null;

            Transform board = new GameObject("Garden / 12 x 12").transform;
            var planter = Spawn("Planter", board, new Vector3(0, -.09f, 0));
            planter.transform.localScale = new Vector3(12.6f, 1, 12.6f);
            Material tileA = MakeMaterial("TileA", Hex("BCD38B"));
            Material tileB = MakeMaterial("TileB", Hex("B3CD81"));
            for (int y = 0; y < 12; y++)
            for (int x = 0; x < 12; x++)
            {
                var tile = Spawn("Tile", board, new Vector3(x - 5.5f, 0, y - 5.5f));
                tile.name = $"Patch {x},{y}";
                foreach (var renderer in tile.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterial = (x + y) % 2 == 0 ? tileA : tileB;
                tile.isStatic = true;
            }
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Matte backdrop";
            ground.transform.position = new Vector3(0, -.79f, 0);
            ground.transform.localScale = Vector3.one * 20;
            ground.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Backdrop", Hex("E3EAD9"));
            UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
            Transform decor = new GameObject("Outside the garden").transform;
            for (int i = 0; i < 14; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                var flower = Spawn("Flower", decor, new Vector3(side * (6.6f + i % 3 * .27f), -.72f, -5.5f + i * .85f));
                flower.transform.localScale = Vector3.one * (1.1f + i % 3 * .2f);
                flower.transform.rotation = Quaternion.Euler(0, i * 57, side * 9);
            }
            for (int i = 0; i < 6; i++)
            {
                var rock = Spawn("Rock", decor, new Vector3(i % 2 == 0 ? -6.7f : 6.7f, -.71f, -4 + i * 1.8f));
                rock.transform.localScale = Vector3.one * (.65f + i % 3 * .2f);
            }

            var game = new GameObject("Snake Game").AddComponent<SnakeController>();
            var audio = game.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0;
            var particles = CreateParticles();
            var hud = CreateHud();
            var settings = new SerializedObject(game);
            Set(settings, "headPrefab", Prefab("SnakeHead"));
            Set(settings, "bodyPrefab", Prefab("SnakeBody"));
            Set(settings, "tailPrefab", Prefab("SnakeTail"));
            Set(settings, "applePrefab", Prefab("Apple"));
            Set(settings, "gameCamera", camera);
            Set(settings, "pickupParticles", particles);
            Set(settings, "audioSource", audio);
            Set(settings, "hud", hud);
            Set(settings, "pickupSound", AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/Pickup.wav"));
            Set(settings, "turnSound", AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/Turn.wav"));
            Set(settings, "loseSound", AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/Lose.wav"));
            Set(settings, "startSound", AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/Start.wav"));
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[GardenSnake] Scene built: " + ScenePath);
        }

        private static void PrepareMaterials()
        {
            string[] names = { "Jade", "Lime", "Cream", "Ink", "Coral", "Wood", "Leaf", "Stone", "Petal", "Tile", "Base" };
            string[] colors = { "32A96A", "B5DE53", "FFF1C4", "143C31", "F25D46", "78523A", "418B4F", "82998B", "F8BD53", "BCD38B", "395B46" };
            for (int i = 0; i < names.Length; i++) MakeMaterial(names[i], Hex(colors[i]));
            foreach (string path in Directory.GetFiles(Root + "/Art/Models", "*.fbx"))
            {
                string normalized = path.Replace('\\', '/');
                var importer = (ModelImporter)AssetImporter.GetAtPath(normalized);
                importer.importCameras = false;
                importer.importLights = false;
                importer.importAnimation = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                foreach (string name in names)
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/" + name + ".mat"));
                importer.SaveAndReimport();
            }
        }

        private static GameObject Prefab(string name)
        {
            string path = Root + "/Prefabs/" + name + ".prefab";
            var instance = new GameObject(name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Art/Models/" + name + ".fbx"));
            model.transform.SetParent(instance.transform, false);
            // FBX preserves Blender's forward-facing -Y as Unity +Z.
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            UnityEngine.Object.DestroyImmediate(instance);
            return prefab;
        }

        private static GameObject Spawn(string name, Transform parent, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + name + ".prefab") ?? Prefab(name);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent, false);
            instance.transform.position = position;
            return instance;
        }

        private static ParticleSystem CreateParticles()
        {
            var particles = new GameObject("Apple confetti").AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = .6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(.07f, .17f);
            main.startColor = new ParticleSystem.MinMaxGradient(Hex("F8BD53"), Hex("FFF6B5"));
            main.gravityModifier = .8f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;
            var emission = particles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 24) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .16f;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = MakeMaterial("Confetti", Hex("FFD97B"), "Universal Render Pipeline/Particles/Unlit");
            return particles;
        }

        private static void PrepareFont()
        {
            Directory.CreateDirectory(Root + "/UI/Resources");
            AssetDatabase.Refresh();
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<TMP_Settings>(), Root + "/UI/Resources/TMP Settings.asset");
            string path = Root + "/UI/GardenFont.asset";
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null) return;
            const string sourcePath = Root + "/UI/LiberationSans.ttf";
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (sourceFont == null) throw new InvalidOperationException("The included Liberation Sans font is missing.");
            font = TMP_FontAsset.CreateFontAsset(sourceFont, 64, 8,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
            font.name = "GardenFont";
            font.TryAddCharacters(new string(Enumerable.Range(32, 95).Select(c => (char)c).ToArray()));
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font, path);
            foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
            AssetDatabase.AddObjectToAsset(font.material, font);
            AssetDatabase.SaveAssets();
        }

        private static SnakeHud CreateHud()
        {
            var root = new GameObject("Garden HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(SnakeHud));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 900);
            scaler.matchWidthOrHeight = .5f;
            var hud = root.GetComponent<SnakeHud>();
            var settings = new SerializedObject(hud);
            var header = Rect("Header", root.transform, new Vector2(.5f, 1), new Vector2(0, -54), new Vector2(1300, 84));
            var brand = Label("Garden Snake", header, "garden snake", 40, Ink, new Vector2(-466, 7), new Vector2(330, 56));
            brand.fontStyle = FontStyles.Bold;
            Label("Subtitle", header, "SMALL GARDEN. ENDLESS GROWING.", 12, Muted, new Vector2(-458, -28), new Vector2(344, 24));
            Label("Harvest label", header, "APPLES", 13, Muted, new Vector2(360, 21), new Vector2(100, 26));
            var score = Label("Apples", header, "00", 44, Ink, new Vector2(360, -15), new Vector2(100, 52));
            score.fontStyle = FontStyles.Bold;
            var best = Label("Best", header, "BEST  00", 16, Ink, new Vector2(490, 15), new Vector2(140, 30));
            var speed = Label("Pace", header, "PACE  1.0x", 13, Muted, new Vector2(490, -15), new Vector2(140, 30));
            var toast = Label("Pickup message", root.transform, "", 22, Ink, new Vector2(0, -137), new Vector2(420, 42), new Vector2(.5f, 1));
            toast.fontStyle = FontStyles.Bold;

            var footer = Rect("Footer", root.transform, new Vector2(.5f, 0), new Vector2(0, 56), new Vector2(1300, 84));
            Label("Controls", footer, "WASD / ARROWS   TO STEER", 16, Ink, new Vector2(-430, 12), new Vector2(370, 30));
            Label("Hints", footer, "Swipe works too.   Space to start.   P to pause.", 13, Muted, new Vector2(-430, -14), new Vector2(390, 26));
            var pause = Button("Pause", footer, "PAUSE", new Vector2(355, 0), new Vector2(116, 46), Cream, Ink);
            var mute = Button("Sound", footer, "SOUND ON", new Vector2(495, 0), new Vector2(138, 46), Cream, Ink);
            var pad = Rect("Direction pad", root.transform, new Vector2(0, .5f), new Vector2(112, -40), new Vector2(170, 170));
            var directions = new[] {
                Button("Up", pad, "^", new Vector2(0, 56), new Vector2(50, 50), Cream, Ink),
                Button("Right", pad, ">", new Vector2(56, 0), new Vector2(50, 50), Cream, Ink),
                Button("Down", pad, "v", new Vector2(0, -56), new Vector2(50, 50), Cream, Ink),
                Button("Left", pad, "<", new Vector2(-56, 0), new Vector2(50, 50), Cream, Ink)
            };
            var card = Rect("Run card", root.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(550, 308));
            var shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(.1f, .2f, .14f, .14f);
            shadow.effectDistance = new Vector2(0, -8);
            card.gameObject.AddComponent<UnityEngine.UI.Image>().color = Cream;
            var group = card.gameObject.AddComponent<CanvasGroup>();
            var eyebrow = Label("Eyebrow", card, "", 13, Muted, new Vector2(0, 109), new Vector2(500, 28));
            var title = Label("Title", card, "", 40, Ink, new Vector2(0, 58), new Vector2(510, 64));
            title.fontStyle = FontStyles.Bold;
            var body = Label("Description", card, "", 19, Muted, new Vector2(0, -4), new Vector2(500, 62));
            var primary = Button("Primary", card, "LET'S GROW   >", new Vector2(0, -94), new Vector2(340, 60), Ink, Cream);
            Set(settings, "scoreText", score); Set(settings, "bestText", best); Set(settings, "speedText", speed);
            Set(settings, "modal", card.gameObject); Set(settings, "modalGroup", group);
            Set(settings, "modalEyebrow", eyebrow); Set(settings, "modalTitle", title); Set(settings, "modalBody", body);
            Set(settings, "primaryButton", primary); Set(settings, "primaryLabel", primary.GetComponentInChildren<TMP_Text>());
            Set(settings, "pauseButton", pause); Set(settings, "pauseLabel", pause.GetComponentInChildren<TMP_Text>());
            Set(settings, "muteButton", mute); Set(settings, "muteLabel", mute.GetComponentInChildren<TMP_Text>());
            Set(settings, "toast", toast);
            var array = settings.FindProperty("directionButtons");
            array.arraySize = directions.Length;
            for (int i = 0; i < directions.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = directions[i];
            settings.ApplyModifiedPropertiesWithoutUndo();
            new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            return hud;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static TMP_Text Label(string name, Transform parent, string value, float size, Color color,
            Vector2 position, Vector2 dimensions, Vector2? anchor = null)
        {
            var rect = Rect(name, parent, anchor ?? new Vector2(.5f, .5f), position, dimensions);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = value;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        private static UnityEngine.UI.Button Button(string name, Transform parent, string label, Vector2 position, Vector2 size, Color background, Color foreground)
        {
            var rect = Rect(name, parent, new Vector2(.5f, .5f), position, size);
            rect.gameObject.AddComponent<UnityEngine.UI.Image>().color = background;
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(.88f, .95f, .85f);
            colors.pressedColor = new Color(.72f, .85f, .66f);
            colors.fadeDuration = .08f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Label("Label", rect, label, 17, foreground, Vector2.zero, size);
            return button;
        }

        private static void Set(SerializedObject target, string field, UnityEngine.Object value)
        {
            if (value == null) throw new InvalidOperationException("Missing scene dependency: " + field);
            target.FindProperty(field).objectReferenceValue = value;
        }

        private static Material MakeMaterial(string name, Color color, string shader = "Universal Render Pipeline/Lit")
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .25f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color color); return color; }

        [MenuItem("Garden Snake/Build WebGL")]
        public static void BuildWebGL()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new InvalidOperationException("Install Web Build Support for Unity 6000.6.0f1, then restart the Editor.");
            PlayerSettings.companyName = "Pikoya Demo";
            PlayerSettings.productName = "Garden Snake";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.template = "PROJECT:Garden";
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.defaultWebScreenWidth = 1440;
            PlayerSettings.defaultWebScreenHeight = 900;
            PlayerSettings.runInBackground = true;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = "Builds/WebGL", target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Web build failed: " + report.summary.result);
            Debug.Log("[GardenSnake] Web build succeeded: " + report.summary.totalSize + " bytes");
        }
    }
}
