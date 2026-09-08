using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using static GardenSnake.Editor.GardenPalette;

namespace GardenSnake.Editor
{
    /// <summary>Reproducible authoring and build entry points. All generated assets stay in GardenSnake.</summary>
    public static class GardenBuilder
    {
        public const string Root = "Assets/GardenSnake";
        public const string ScenePath = Root + "/Scenes/GardenSnake.unity";
        public const int BoardSize = 15;

        [MenuItem("Garden Snake/Rebuild game scene")]
        public static void CreateScene()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before rebuilding.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save or discard your unsaved scene changes before rebuilding.");
            foreach (string folder in new[] { "/Scenes", "/Materials", "/Prefabs", "/UI", "/Rendering" })
                Directory.CreateDirectory(Root + folder);
            AssetDatabase.Refresh();
            PrepareMaterials();
            GardenHud.PrepareFont();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "GardenSnake";

            Camera camera = CreateCamera(out Transform cameraRig);
            CreateLighting();
            CreateBoard();
            CreateSurroundings();

            var game = new GameObject("Snake Game").AddComponent<SnakeController>();
            var audio = game.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0;
            ParticleSystem pickupParticles = CreatePickupBurst();
            ParticleSystem trailParticles = CreateSnakeTrail();
            CreateDustMotes();
            SnakeHud hud = GardenHud.Create();

            var settings = new SerializedObject(game);
            Set(settings, "headPrefab", Prefab("SnakeHead"));
            Set(settings, "bodyPrefab", Prefab("SnakeBody"));
            Set(settings, "tailPrefab", Prefab("SnakeTail"));
            Set(settings, "applePrefab", Prefab("Apple"));
            Set(settings, "gameCamera", camera);
            Set(settings, "cameraRig", cameraRig);
            Set(settings, "pickupParticles", pickupParticles);
            Set(settings, "trailParticles", trailParticles);
            Set(settings, "audioSource", audio);
            Set(settings, "hud", hud);
            Set(settings, "pickupSound", Clip("Pickup"));
            Set(settings, "turnSound", Clip("Turn"));
            Set(settings, "loseSound", Clip("Lose"));
            Set(settings, "startSound", Clip("Start"));
            settings.FindProperty("boardSize").intValue = BoardSize;
            settings.ApplyModifiedPropertiesWithoutUndo();

            GardenFeel.Create(game, camera, hud, pickupParticles);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[GardenSnake] Scene built: " + ScenePath);
        }

        private static AudioClip Clip(string name) =>
            AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/" + name + ".wav");

        // ---------------------------------------------------------------- camera, light, volume

        private static Camera CreateCamera(out Transform rig)
        {
            rig = new GameObject("Camera Rig").transform;
            var camera = new GameObject("Garden Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.transform.SetParent(rig, false);
            camera.tag = "MainCamera";
            // 55 degrees keeps the snake's face readable while the grid still reads as a square.
            const float pitch = 55f;
            const float distance = 26f;
            Vector3 direction = Quaternion.Euler(pitch, 0, 0) * Vector3.forward;
            camera.transform.localPosition = -direction * distance;
            camera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            camera.orthographic = true;
            camera.orthographicSize = 9.5f;
            camera.nearClipPlane = .3f;
            camera.farClipPlane = 90;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Surround;
            camera.allowHDR = true;

            var extra = camera.GetUniversalAdditionalCameraData();
            extra.renderPostProcessing = true;
            extra.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            extra.antialiasingQuality = AntialiasingQuality.High;

            camera.gameObject.AddComponent<MMWiggle>();
            var shaker = camera.gameObject.AddComponent<MMCameraShaker>();
            shaker.CooldownBetweenShakes = 0f;
            CreateVolume();
            return camera;
        }

        private static void CreateVolume()
        {
            string path = Root + "/Rendering/GardenVolume.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            foreach (var component in profile.components.ToArray()) UnityEngine.Object.DestroyImmediate(component, true);
            profile.components.Clear();

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(.92f);
            bloom.intensity.Override(.85f);
            bloom.scatter.Override(.62f);
            bloom.tint.Override(Pollen);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(.34f);
            vignette.smoothness.Override(.5f);
            vignette.color.Override(Hex("07100B"));

            var grading = profile.Add<ColorAdjustments>(true);
            grading.postExposure.Override(.12f);
            grading.contrast.Override(11f);
            grading.saturation.Override(9f);

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);

            var toning = profile.Add<SplitToning>(true);
            toning.shadows.Override(Hex("22453A"));
            toning.highlights.Override(Hex("FFE7B8"));
            toning.balance.Override(-8f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            var volume = new GameObject("Garden Grade").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1;
            volume.sharedProfile = profile;
        }

        private static void CreateLighting()
        {
            var key = new GameObject("Afternoon Sun", typeof(Light)).GetComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.35f;
            key.color = Hex("FFF0D2");
            key.shadows = LightShadows.Soft;
            key.shadowStrength = .62f;
            key.transform.rotation = Quaternion.Euler(52, -34, 0);

            var fill = new GameObject("Cool fill", typeof(Light)).GetComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = .34f;
            fill.color = Hex("9FC7DD");
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(24, 148, 0);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("6E8C74");
            RenderSettings.ambientEquatorColor = Hex("40584A");
            RenderSettings.ambientGroundColor = Hex("1B2A22");
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
        }

        // ---------------------------------------------------------------- world

        private static void CreateBoard()
        {
            Transform board = new GameObject("Garden / " + BoardSize + " x " + BoardSize).transform;
            float half = (BoardSize - 1) * .5f;

            var planter = Spawn("Planter", board, new Vector3(0, -.09f, 0));
            planter.transform.localScale = new Vector3(BoardSize + .7f, 1.15f, BoardSize + .7f);

            Material light = MakeMaterial("TileA", GrassLight);
            Material dark = MakeMaterial("TileB", GrassDark);
            for (int y = 0; y < BoardSize; y++)
            for (int x = 0; x < BoardSize; x++)
            {
                var tile = Spawn("Tile", board, new Vector3(x - half, 0, y - half));
                tile.name = "Patch " + x + "," + y;
                foreach (var renderer in tile.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterial = (x + y) % 2 == 0 ? light : dark;
                tile.isStatic = true;
            }
        }

        private static void CreateSurroundings()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Matte backdrop";
            ground.transform.position = new Vector3(0, -.82f, 0);
            ground.transform.localScale = Vector3.one * 24;
            ground.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Backdrop", SurroundFloor);
            UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());

            // Decoration rings the whole board so no side of the frame looks unfinished.
            Transform decor = new GameObject("Outside the garden").transform;
            var random = new System.Random(20260908);
            float edge = BoardSize * .5f + .9f;
            for (int side = 0; side < 4; side++)
            {
                Quaternion facing = Quaternion.Euler(0, side * 90, 0);
                for (int i = 0; i < 7; i++)
                {
                    float along = Mathf.Lerp(-edge + .6f, edge - .6f, (i + (float)random.NextDouble() * .55f) / 6.6f);
                    float outward = .55f + (float)random.NextDouble() * 1.5f;
                    var flower = Spawn("Flower", decor, facing * new Vector3(along, -.74f, -(edge + outward)));
                    flower.transform.localScale = Vector3.one * (1f + (float)random.NextDouble() * .45f);
                    flower.transform.rotation = Quaternion.Euler(0, (float)random.NextDouble() * 360f,
                        ((float)random.NextDouble() - .5f) * 16f);
                }
                for (int i = 0; i < 3; i++)
                {
                    float along = Mathf.Lerp(-edge, edge, (i + .35f + (float)random.NextDouble() * .3f) / 3.3f);
                    float outward = .45f + (float)random.NextDouble() * 1.8f;
                    var rock = Spawn("Rock", decor, facing * new Vector3(along, -.72f, -(edge + outward)));
                    rock.transform.localScale = Vector3.one * (.55f + (float)random.NextDouble() * .55f);
                    rock.transform.rotation = Quaternion.Euler(0, (float)random.NextDouble() * 360f, 0);
                }
            }
        }

        // ---------------------------------------------------------------- particles

        private static ParticleSystem CreatePickupBurst()
        {
            var particles = NewParticles("Apple confetti", 96);
            var main = particles.main;
            main.duration = .7f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.4f, .85f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 5.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(.08f, .2f);
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0, 6.28f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(Pollen, Petal);
            main.gravityModifier = 1.5f;
            var emission = particles.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 26) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = .2f;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, Shrink());
            var rotation = particles.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = BuiltinQuad();
            renderer.sharedMaterial = ConfettiMaterial();
            return particles;
        }

        /// <summary>A soft sparkle that follows the head so movement always feels alive.</summary>
        private static ParticleSystem CreateSnakeTrail()
        {
            var particles = NewParticles("Snake sparkle", 64);
            var main = particles.main;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.15f, .6f);
            main.startSize = new ParticleSystem.MinMaxCurve(.05f, .11f);
            main.startColor = new ParticleSystem.MinMaxGradient(Petal.With(.85f), Pollen.With(.5f));
            main.gravityModifier = -.12f;
            var emission = particles.emission;
            emission.rateOverTime = 14;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .22f;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, Shrink());
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = BuiltinQuad();
            renderer.sharedMaterial = ConfettiMaterial();
            return particles;
        }

        /// <summary>Slow motes drifting over the whole garden; pure atmosphere, never gameplay.</summary>
        private static void CreateDustMotes()
        {
            var particles = NewParticles("Pollen in the air", 120);
            particles.transform.position = new Vector3(0, 2.4f, 0);
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.05f, .22f);
            main.startSize = new ParticleSystem.MinMaxCurve(.03f, .08f);
            main.startColor = new ParticleSystem.MinMaxGradient(Petal.With(.4f), Pollen.With(.3f));
            main.gravityModifier = -.01f;
            var emission = particles.emission;
            emission.rateOverTime = 9;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(BoardSize + 6, 4.5f, BoardSize + 6);
            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = .28f;
            noise.frequency = .18f;
            var fade = particles.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[]
                {
                    new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .25f),
                    new GradientAlphaKey(1, .7f), new GradientAlphaKey(0, 1)
                });
            fade.color = new ParticleSystem.MinMaxGradient(gradient);
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = BuiltinQuad();
            renderer.sharedMaterial = ConfettiMaterial();
            particles.Play();
        }

        private static ParticleSystem NewParticles(string name, int maxParticles)
        {
            var particles = new GameObject(name).AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            var emission = particles.emission;
            emission.rateOverTime = 0;
            return particles;
        }

        private static AnimationCurve Shrink() => new AnimationCurve(
            new Keyframe(0, 0f, 6f, 6f), new Keyframe(.18f, 1f), new Keyframe(1, 0f));

        private static Mesh BuiltinQuad()
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Mesh mesh = quad.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(quad);
            return mesh;
        }

        private static Material ConfettiMaterial() =>
            MakeMaterial("Confetti", Color.white, "Universal Render Pipeline/Particles/Unlit");

        // ---------------------------------------------------------------- assets

        private static void PrepareMaterials()
        {
            var swatches = new (string Name, Color Color)[]
            {
                ("Jade", SnakeBody), ("Lime", SnakeSpot), ("Cream", SnakeCream), ("Ink", Ink),
                ("Coral", AppleSkin), ("Wood", Stem), ("Leaf", AppleLeaf), ("Stone", Stone),
                ("Petal", Pollen), ("Tile", GrassLight), ("Base", PlanterRim)
            };
            foreach (var swatch in swatches) MakeMaterial(swatch.Name, swatch.Color);
            MakeMaterial("Backdrop", SurroundFloor);

            foreach (string path in Directory.GetFiles(Root + "/Art/Models", "*.fbx"))
            {
                string normalized = path.Replace('\\', '/');
                var importer = (ModelImporter)AssetImporter.GetAtPath(normalized);
                importer.importCameras = false;
                importer.importLights = false;
                importer.importAnimation = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                foreach (var swatch in swatches)
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), swatch.Name),
                        AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/" + swatch.Name + ".mat"));
                importer.SaveAndReimport();
            }
        }

        public static GameObject Prefab(string name)
        {
            string path = Root + "/Prefabs/" + name + ".prefab";
            var instance = new GameObject(name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Art/Models/" + name + ".fbx"));
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

        public static void Set(SerializedObject target, string field, UnityEngine.Object value)
        {
            var property = target.FindProperty(field);
            if (property == null) throw new InvalidOperationException("Unknown serialized field: " + field);
            if (value == null) throw new InvalidOperationException("Missing scene dependency: " + field);
            property.objectReferenceValue = value;
        }

        public static Material MakeMaterial(string name, Color color, string shader = "Universal Render Pipeline/Lit")
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .22f);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ---------------------------------------------------------------- build

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
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/WebGL",
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Web build failed: " + report.summary.result);
            Debug.Log("[GardenSnake] Web build succeeded: " + report.summary.totalSize + " bytes");
        }
    }
}
