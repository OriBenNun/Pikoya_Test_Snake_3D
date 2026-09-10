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
        // The board matches the shape of a widescreen window, so the garden fills it instead
        // of leaving two empty gutters either side.
        /// <summary>Set by automation (the playtest harness) to skip the destructive-rebuild prompt.</summary>
        public static bool SkipConfirmation;

        public const int BoardWidth = 21;
        public const int BoardHeight = 12;

        /// <summary>
        /// Regenerates the scene from scratch. This is destructive: anything authored by hand in
        /// the Garden Snake scene, and the generated prefabs and materials, are replaced. Once a
        /// scene is being edited by hand, tune from the Inspector instead and leave this alone.
        /// </summary>
        [MenuItem("Garden Snake/Rebuild game scene")]
        public static void CreateScene()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before rebuilding.");
            if (!Application.isBatchMode && !SkipConfirmation &&
                !EditorUtility.DisplayDialog("Rebuild the Garden Snake scene?",
                    "This replaces " + ScenePath + " and the generated prefabs and materials.\n\n" +
                    "Any hand-authored changes in that scene will be lost.",
                    "Rebuild", "Cancel"))
                return;
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save or discard your unsaved scene changes before rebuilding.");
            foreach (string folder in new[] { "/Scenes", "/Materials", "/Prefabs", "/UI", "/Rendering" })
                Directory.CreateDirectory(Root + folder);
            AssetDatabase.Refresh();
            PrepareAudio();
            PrepareMaterials();
            GardenDecor.Prepare();
            GardenHud.PrepareFont();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "GardenSnake";

            Camera camera = CreateCamera(out Transform cameraRig);
            CreateLighting();
            Transform board = CreateBoard();
            CreateSurroundings();
            GardenDecor.Create();
            GardenWildlifeBuilder.Create();

            var game = new GameObject("Snake Game").AddComponent<SnakeController>();
            var audio = game.gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0;
            var music = new GameObject("Ambience", typeof(AudioSource));
            music.transform.SetParent(game.transform, false);
            var musicSource = music.GetComponent<AudioSource>();
            musicSource.clip = Clip("Ambience");
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = .28f;
            musicSource.spatialBlend = 0;
            Transform appleMarker = CreateAppleMarker();
            Transform burstRing = CreateBurstRing();
            ParticleSystem pickupParticles = CreatePickupBurst();
            ParticleSystem trailParticles = CreateSnakeTrail();
            CreateDustMotes();
            SnakeHud hud = GardenHud.Create();

            var settings = new SerializedObject(game);
            Set(settings, "headPrefab", TintHead(Prefab("SnakeHead")));
            Set(settings, "bodyPrefab", Prefab("SnakeBody"));
            Set(settings, "tailPrefab", Prefab("SnakeTail"));
            Set(settings, "applePrefab", Prefab("Apple"));
            Set(settings, "gameCamera", camera);
            Set(settings, "cameraRig", cameraRig);
            Set(settings, "paceVolume", GameObject.Find("Pace").GetComponent<Volume>());
            Set(settings, "appleMarker", appleMarker);
            Set(settings, "burstRing", burstRing);
            Set(settings, "pickupParticles", pickupParticles);
            Set(settings, "trailParticles", trailParticles);
            Set(settings, "audioSource", audio);
            Set(settings, "musicSource", musicSource);
            Set(settings, "hud", hud);
            Set(settings, "cellWaves", board.GetComponent<GridCellWaves>());
            settings.FindProperty("hudBandBottom").floatValue = .15f;
            Set(settings, "pickupSound", Clip("Pickup"));
            Set(settings, "turnSound", Clip("Turn"));
            Set(settings, "loseSound", Clip("Lose"));
            Set(settings, "startSound", Clip("Start"));
            Set(settings, "bestSound", Clip("Best"));
            Set(settings, "clickSound", Clip("Click"));
            settings.FindProperty("boardWidth").intValue = BoardWidth;
            settings.FindProperty("boardHeight").intValue = BoardHeight;
            settings.ApplyModifiedPropertiesWithoutUndo();

            GardenTuning.Bind(game);
            GardenFeel.Create(game, hud, board, pickupParticles);

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
            bloom.threshold.Override(1.05f);
            bloom.intensity.Override(.42f);
            bloom.scatter.Override(.68f);
            bloom.tint.Override(Pollen);

            // Just enough vignette to hold the eye on the board; the corners stay sunny.
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(.28f);
            vignette.smoothness.Override(.65f);
            vignette.color.Override(Hex("2C4F26"));

            var grading = profile.Add<ColorAdjustments>(true);
            grading.postExposure.Override(-.06f);
            grading.contrast.Override(5f);
            grading.saturation.Override(4f);

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);

            var toning = profile.Add<SplitToning>(true);
            toning.shadows.Override(Hex("788078"));
            toning.highlights.Override(Hex("89847B"));
            toning.balance.Override(6f);

            foreach (var component in profile.components)
                if (!AssetDatabase.Contains(component)) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            var volume = new GameObject("Garden Grade").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1;
            volume.sharedProfile = profile;
            CreateSpeedVolume();
        }

        /// <summary>
        /// A second, heavier vignette the controller fades in as the run speeds up. It is the
        /// only cue for pace, and it arrives slowly enough that the player feels it rather than
        /// reads it.
        /// </summary>
        private static void CreateSpeedVolume()
        {
            string path = Root + "/Rendering/SpeedVolume.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            foreach (var component in profile.components.ToArray()) UnityEngine.Object.DestroyImmediate(component, true);
            profile.components.Clear();
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(.3f);
            vignette.smoothness.Override(.55f);
            vignette.color.Override(Hex("2C4F26"));
            var grading = profile.Add<ColorAdjustments>(true);
            grading.saturation.Override(8f);
            grading.contrast.Override(6f);
            foreach (var component in profile.components)
                if (!AssetDatabase.Contains(component)) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            var volume = new GameObject("Pace").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 2;
            volume.weight = 0;
            volume.sharedProfile = profile;
        }

        private static void CreateLighting()
        {
            var key = new GameObject("Afternoon Sun", typeof(Light)).GetComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.28f;
            key.color = Hex("FFF3D8");
            key.shadows = LightShadows.Soft;
            key.shadowStrength = .62f;
            key.transform.rotation = Quaternion.Euler(52, -34, 0);

            var fill = new GameObject("Cool fill", typeof(Light)).GetComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = .3f;
            fill.color = Hex("BFE2F2");
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(24, 148, 0);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("E6E7C9");
            RenderSettings.ambientEquatorColor = Hex("BAC6AB");
            RenderSettings.ambientGroundColor = Hex("8A9373");
            RenderSettings.ambientIntensity = .85f;
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
        }

        // ---------------------------------------------------------------- world

        private static Transform CreateBoard()
        {
            Transform board = new GameObject("Garden / " + BoardWidth + " x " + BoardHeight).transform;
            float halfX = (BoardWidth - 1) * .5f;
            float halfZ = (BoardHeight - 1) * .5f;

            var planter = Spawn("Planter", board, new Vector3(0, -.09f, 0));
            planter.transform.localScale = new Vector3(BoardWidth + 1.5f, 1.2f, BoardHeight + 1.5f);
            BuildRim(board);

            var soil = GameObject.CreatePrimitive(PrimitiveType.Plane);
            soil.name = "Soil";
            soil.transform.SetParent(board, false);
            soil.transform.position = new Vector3(0, -.06f, 0);
            soil.transform.localScale = new Vector3((BoardWidth + .6f) * .1f, 1, (BoardHeight + .6f) * .1f);
            soil.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Soil", PlanterSoil);
            UnityEngine.Object.DestroyImmediate(soil.GetComponent<Collider>());

            // Four grass tones instead of two: the checker still reads, but the field stops
            // looking like a spreadsheet.
            Material[] grass =
            {
                MakeMaterial("TileA", GrassLight),
                MakeMaterial("TileB", Color.Lerp(GrassDark, PlanterSoil, .22f)),
                MakeMaterial("TileC", Color.Lerp(GrassLight, GrassDark, .55f)),
                MakeMaterial("TileD", Color.Lerp(GrassDark, GrassLight, .18f))
            };
            GardenDecor.TextureBoard(grass);
            var cells = new Transform[BoardWidth * BoardHeight];
            for (int y = 0; y < BoardHeight; y++)
            for (int x = 0; x < BoardWidth; x++)
            {
                var tile = Spawn("Tile", board, new Vector3(x - halfX, 0, y - halfZ));
                tile.name = "Patch " + x + "," + y;
                int checker = (x + y) % 2;
                bool speckle = (x * 7 + y * 13 + x * y * 3) % 11 == 0;
                foreach (var renderer in tile.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterial = grass[checker + (speckle ? 2 : 0)];
                foreach (Transform part in tile.GetComponentsInChildren<Transform>()) part.gameObject.isStatic = false;
                cells[y * BoardWidth + x] = tile.transform;
            }
            var waves = board.gameObject.AddComponent<GridCellWaves>();
            var waveSettings = new SerializedObject(waves);
            waveSettings.FindProperty("columns").intValue = BoardWidth;
            var cellArray = waveSettings.FindProperty("cells");
            cellArray.arraySize = cells.Length;
            for (int i = 0; i < cells.Length; i++) cellArray.GetArrayElementAtIndex(i).objectReferenceValue = cells[i];
            waveSettings.ApplyModifiedPropertiesWithoutUndo();
            return board;
        }

        /// <summary>Four bars around the grass. A lip the snake can be seen to run up against.</summary>
        private static void BuildRim(Transform board)
        {
            Material material = MakeMaterial("Base", PlanterRim);
            const float thickness = .58f;
            const float height = .34f;
            for (int side = 0; side < 4; side++)
            {
                bool horizontal = side % 2 == 0;
                var bar = Spawn(horizontal ? "RimLong" : "RimShort", board, Vector3.zero);
                bar.name = "Rim " + side;
                bar.transform.SetParent(board, false);
                UnityEngine.Object.DestroyImmediate(bar.GetComponent<Collider>());
                float direction = side < 2 ? -1 : 1;
                bar.transform.localPosition = horizontal
                    ? new Vector3(0, height * .5f - .18f, direction * (BoardHeight + thickness) * .5f)
                    : new Vector3(direction * (BoardWidth + thickness) * .5f, height * .5f - .18f, 0);
                bar.transform.localScale = Vector3.one;
                foreach(var renderer in bar.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = material;
                bar.isStatic = true;
            }
        }

        private static void CreateSurroundings()
        {
            // An unlit lawn with light pooling under the board keeps the surround bright and
            // stops the corners of a wide screen going flat.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Sunny lawn";
            ground.transform.position = new Vector3(0, -.82f, 0);
            ground.transform.localScale = Vector3.one * 10;
            var lawn = MakeMaterial("Backdrop", Color.white, "Universal Render Pipeline/Unlit");
            lawn.SetTexture("_BaseMap", GardenSprites.RadialGradient("Lawn", 512, SurroundGlow, Surround));
            ground.GetComponent<Renderer>().sharedMaterial = lawn;
            ground.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());

            CreateMeadow();
        }

        /// <summary>
        /// Fills the lawn around the board with flowers and stones. Placement is a deterministic
        /// jittered grid so the meadow looks scattered but rebuilds identically every time, and
        /// every stem is handed to the wind so the whole field moves together.
        /// </summary>
        private static void CreateMeadow()
        {
            Transform meadow = new GameObject("Meadow").transform;
            var wind = meadow.gameObject.AddComponent<GardenWind>();
            var random = new System.Random(20260908);
            var stems = new System.Collections.Generic.List<Transform>();

            Material[] petals =
            {
                MakeMaterial("Cream", Petal),
                MakeMaterial("PetalBlush", Hex("FFD8D0")),
                MakeMaterial("PetalButter", Hex("FFEFB0")),
                MakeMaterial("PetalLilac", Hex("E4DAF6"))
            };
            Material[] centres = { MakeMaterial("Petal", Pollen), MakeMaterial("PollenRose", Accent) };

            float clearX = BoardWidth * .5f + 1.4f;
            float clearZ = BoardHeight * .5f + 1.4f;
            const float reach = 31f;
            const float depth = 20f;
            const float spacing = 2.6f;
            for (float x = -reach; x <= reach; x += spacing)
            for (float z = -depth; z <= depth; z += spacing)
            {
                float px = x + ((float)random.NextDouble() - .5f) * spacing * 1.4f;
                float pz = z + ((float)random.NextDouble() - .5f) * spacing * 1.4f;
                double roll = random.NextDouble();
                if (roll < .22) continue;  // bare grass, so the meadow is not a uniform carpet
                if (roll < .38)
                {
                    if (Clear(px, pz, clearX, clearZ)) Pebble(meadow, random, px, pz);
                    continue;
                }
                // Flowers arrive in small clumps, the way they seed themselves.
                int clump = 1 + random.Next(3);
                for (int i = 0; i < clump; i++)
                {
                    float ox = px + ((float)random.NextDouble() - .5f) * 2.1f;
                    float oz = pz + ((float)random.NextDouble() - .5f) * 2.1f;
                    if (!Clear(ox, oz, clearX, clearZ)) continue;
                    string variant = (x.GetHashCode() ^ z.GetHashCode() ^ i) % 4 == 0 ? "Tulip" : i % 3 == 1 ? "Daisy" : i % 3 == 2 ? "Lavender" : "Flower";
                    var flower = Spawn(variant, meadow, new Vector3(ox, -.74f, oz));
                    flower.transform.localScale = Vector3.one * (.5f + (float)random.NextDouble() * 1.25f);
                    flower.transform.rotation = Quaternion.Euler(0, (float)random.NextDouble() * 360f, 0);
                    Material petal = petals[random.Next(petals.Length)];
                    Material centre = centres[random.Next(centres.Length)];
                    foreach (var renderer in flower.GetComponentsInChildren<Renderer>())
                    {
                        if (renderer.sharedMaterial == null) continue;
                        if (renderer.sharedMaterial.name == "Cream") renderer.sharedMaterial = petal;
                        else if (renderer.sharedMaterial.name == "Petal") renderer.sharedMaterial = centre;
                    }
                    stems.Add(flower.transform);
                }
            }

            var bound = new SerializedObject(wind);
            var array = bound.FindProperty("stems");
            array.arraySize = stems.Count;
            for (int i = 0; i < stems.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = stems[i];
            bound.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[GardenSnake] Meadow: " + stems.Count + " flowers");
        }

        /// <summary>A warm pool of light under the apple so the eye finds it instantly.</summary>
        private static Transform CreateAppleMarker()
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Apple marker";
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.rotation = Quaternion.Euler(90, 0, 0);
            quad.transform.localScale = Vector3.one * 2.1f;
            var renderer = quad.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = Glowing("AppleGlow", Accent, GardenSprites.SoftDot("Dot", 128, 2.2f), 3.5f);
            return quad.transform;
        }

        /// <summary>The ring a picked apple throws out; it expands once and fades.</summary>
        private static Transform CreateBurstRing()
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Pickup ring";
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.rotation = Quaternion.Euler(90, 0, 0);
            quad.transform.localScale = Vector3.one;
            var renderer = quad.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = Glowing("PickupRing", Color.Lerp(Paper, Accent, .45f),
                GardenSprites.RingTexture("Ring", 128, .78f, .3f), 6f);
            quad.SetActive(false);
            return quad.transform;
        }

        /// <summary>
        /// An All In 1 Sprite Shader material with its glow pass on, so these flat quads read as
        /// emissive light and pick up the scene bloom rather than sitting flat on the grass.
        /// </summary>
        private static Material Glowing(string name, Color tint, Texture texture, float glow)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("AllIn1SpriteShader/AllIn1SpriteShader");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetTexture("_MainTex", texture);
            material.SetTexture("_GlowTex", texture);
            material.SetColor("_Color", tint);
            material.SetColor("_GlowColor", tint);
            material.SetFloat("_Glow", glow);
            material.SetFloat("_GlowGlobal", 1f);
            material.SetFloat("_Alpha", 1f);
            material.EnableKeyword("GLOW_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
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
            main.startColor = Confetti();
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
            Billboard(particles);
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
            Billboard(particles);
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
            main.startSize = new ParticleSystem.MinMaxCurve(.07f, .17f);
            main.startColor = new ParticleSystem.MinMaxGradient(Petal.With(.22f), Pollen.With(.16f));
            main.gravityModifier = -.01f;
            var emission = particles.emission;
            emission.rateOverTime = 5;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(BoardWidth + 6, 4.5f, BoardHeight + 6);
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
            Billboard(particles);
            particles.Play();
        }

        /// <summary>Four hues at random, so a pickup throws colour rather than one tint.</summary>
        private static ParticleSystem.MinMaxGradient Confetti()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Pollen, 0f), new GradientColorKey(AppleSkin, .34f),
                    new GradientColorKey(SnakeSpot, .67f), new GradientColorKey(Petal, 1f)
                },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            gradient.mode = GradientMode.Fixed;
            return new ParticleSystem.MinMaxGradient(gradient)
            {
                mode = ParticleSystemGradientMode.RandomColor
            };
        }

        private static bool Clear(float x, float z, float clearX, float clearZ) =>
            Mathf.Abs(x) > clearX || Mathf.Abs(z) > clearZ;

        private static void Pebble(Transform parent, System.Random random, float x, float z)
        {
            var rock = Spawn("Rock", parent, new Vector3(x, -.72f, z));
            rock.transform.localScale = Vector3.one * (.4f + (float)random.NextDouble() * .7f);
            rock.transform.rotation = Quaternion.Euler(0, (float)random.NextDouble() * 360f, 0);
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

        private static void Billboard(ParticleSystem particles)
        {
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = Additive("Confetti", Color.white,
                GardenSprites.SoftDot("Dot", 128, 2.2f), "Universal Render Pipeline/Particles/Unlit");
        }

        /// <summary>An unlit additive material; used for every glow the game draws.</summary>
        private static Material Additive(string name, Color tint, Texture texture,
            string shader = "Universal Render Pipeline/Unlit")
        {
            Material material = MakeMaterial(name, tint, shader);
            // URP recomputes the blend factors from _Surface/_Blend, so set those and let it.
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 2);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ---------------------------------------------------------------- assets

        /// <summary>Short effects stay in memory; the long ambient bed streams.</summary>
        private static void PrepareAudio()
        {
            foreach (string path in Directory.GetFiles(Root + "/Audio", "*.wav"))
            {
                string normalized = path.Replace('\\', '/');
                var importer = (AudioImporter)AssetImporter.GetAtPath(normalized);
                bool ambient = normalized.EndsWith("Ambience.wav");
                var settings = importer.defaultSampleSettings;
                settings.loadType = ambient ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = ambient ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
                settings.quality = .7f;
                settings.preloadAudioData = !ambient;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.SaveAndReimport();
            }
        }

        private static void PrepareMaterials()
        {
            var swatches = new (string Name, Color Color)[]
            {
                ("Jade", SnakeBody), ("Lime", SnakeSpot), ("Cream", SnakeCream), ("Ink", Ink),
                ("Coral", AppleSkin), ("Wood", Stem), ("Leaf", AppleLeaf), ("Stone", Stone),
                ("Petal", Pollen), ("Tile", GrassLight), ("Base", PlanterRim),
                ("Aqua", Hex("58BBC1")), ("Blush", Hex("F7A1A2")), ("Bark", Hex("996843")), ("Foliage", Hex("6EA447"))
            };
            foreach (var swatch in swatches) MakeMaterial(swatch.Name, swatch.Color);

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

        /// <summary>
        /// Gives the head its own slightly paler skin. The shape already reads as a head; this
        /// makes it findable at a glance when the body is long and folded over itself.
        /// </summary>
        private static GameObject TintHead(GameObject prefab)
        {
            Material skin = MakeMaterial("JadeHead", SnakeHead);
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            foreach (var renderer in contents.GetComponentsInChildren<Renderer>(true))
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.name == "Jade")
                    renderer.sharedMaterial = skin;
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
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
            // The shipping configuration lives in Project Settings so Build And Run from the
            // toolbar produces exactly this player too; applying it here only keeps them in step.
            GardenSettings.Apply();
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
