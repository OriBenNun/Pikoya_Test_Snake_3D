using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GardenSnake.Editor
{
    /// <summary>Small, reproducible clay wildlife rigs. No colliders or gameplay components.</summary>
    public static class GardenWildlifeBuilder
    {
        private static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(GardenBuilder.Root + "/Materials/" + name + ".mat");

        [MenuItem("Garden Snake/Add ambient wildlife")]
        public static void Install()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != GardenBuilder.ScenePath || scene.isDirty)
                throw new InvalidOperationException("Install wildlife in the saved GardenSnake scene, outside Play mode.");
            var old = GameObject.Find("Garden wildlife");
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t.name == "Bird perch") UnityEngine.Object.DestroyImmediate(t.gameObject);
            Create();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void Create()
        {
            if (GameObject.Find("Garden wildlife") != null) return;
            var root = new GameObject("Garden wildlife").transform;
            Animal(root, GardenAnimal.Species.Bunny, new Vector3(-3.3f, -.74f, 7.75f), new Vector3(-2.3f, -.74f, 7.75f), .37f);
            Animal(root, GardenAnimal.Species.Bunny, new Vector3(-4.1f, -.74f, -7.7f), new Vector3(-2.7f, -.74f, -7.8f), 2);
            Animal(root, GardenAnimal.Species.Turtle, new Vector3(12.1f, -.74f, -.6f), new Vector3(12.1f, -.74f, -1.6f), 1.73f);
            Animal(root, GardenAnimal.Species.Turtle, new Vector3(3.6f, -.74f, -7.7f), new Vector3(2.2f, -.74f, -7.8f), 3);
            for (int i = 0; i < 2; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                var a = new Vector3(side * 12.1f, -.74f, i == 0 ? 4.5f : -6.6f);
                var b = a + new Vector3(side * .55f, 0, -.35f);
                Transform perch = null;
                if (i == 0)
                {
                    Transform tree = null;
                    float nearest = float.MaxValue;
                    foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                    {
                        if (t.name != "Tree") continue;
                        float distance = Vector3.Distance(t.position, a);
                        if (distance < nearest) { nearest = distance; tree = t; }
                    }
                    if (tree != null)
                    {
                        // Imported meshes may sit below another model root. Measure the whole crown.
                        Bounds bounds = new Bounds(tree.position + Vector3.up * 2, Vector3.zero);
                        bool found = false;
                        foreach (var renderer in tree.GetComponentsInChildren<MeshRenderer>())
                        {
                            if (!renderer.name.StartsWith("Puffy canopy")) continue;
                            if (!found) { bounds = renderer.bounds; found = true; }
                            else bounds.Encapsulate(renderer.bounds);
                        }
                        perch = Group("Bird perch", tree, Vector3.zero);
                        perch.position = new Vector3(tree.position.x + .65f, tree.position.y + 1.9f, bounds.min.z - .45f);
                        Part("Perch twig", perch, new Vector3(0, -.06f, .2f), new Vector3(.13f, .12f, .65f), "Bark");
                    }
                }
                Animal(root, GardenAnimal.Species.Bird, a, b, i + .5f, perch);
            }
            for (int i = 0; i < 3; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                var a = new Vector3(side * 12.8f, -.15f, -3.2f + i * 3.4f);
                Animal(root, GardenAnimal.Species.Butterfly, a, a + new Vector3(side * .08f, .1f, .7f), i * 1.37f);
            }
            var bugPatches = new[] { new Vector3(-7.8f, -.74f, 7.55f), new Vector3(6.2f, -.74f, 7.7f),
                new Vector3(-5.5f, -.74f, -8.7f), new Vector3(8f, -.74f, -8.6f), new Vector3(-6f, -.74f, 8.2f) };
            for (int i = 0; i < bugPatches.Length; i++)
            {
                var a = bugPatches[i];
                Animal(root, GardenAnimal.Species.Ladybug, a, a + new Vector3(.4f, 0, .15f), i * 1.71f);
            }
            // Actual grass blades catch the same wind as the flowers; the board stays unobscured.
            for (int i = 0; i < 24; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                var tuft = Group("Grass tuft", root, new Vector3(side * (13.5f + .55f * Mathf.Sin(i * 7.13f)), -.75f, -7 + i / 2 * 1.2f + .25f * Mathf.Sin(i * 2.7f)));
                for (int j = 0; j < 3; j++)
                {
                    var blade = Part("Blade", tuft, new Vector3((j - 1) * .07f, .16f, 0), new Vector3(.07f, .4f + j * .04f, .055f), "Leaf");
                    blade.localRotation = Quaternion.Euler(0, i * 31, (j - 1) * 19);
                }
            }
        }

        private static void Animal(Transform parent, GardenAnimal.Species species, Vector3 a, Vector3 b, float phase, Transform perch = null)
        {
            var root = Group(species.ToString(), parent, a);
            root.localScale = Vector3.one * (species == GardenAnimal.Species.Butterfly ? .58f : species == GardenAnimal.Species.Ladybug ? .7f : .88f);
            ReservePatch(a, b, species == GardenAnimal.Species.Bunny || species == GardenAnimal.Species.Turtle ? .8f : .42f);
            root.localRotation = Quaternion.Euler(0, 150 + phase * 35, 0);
            Transform body = Group("Body", root, Vector3.zero), head, left = null, right = null;
            var limbs = new System.Collections.Generic.List<Transform>();
            if (species == GardenAnimal.Species.Bunny)
            {
                Part("Pear body", body, new Vector3(0, .34f, 0), new Vector3(.55f, .64f, .73f), "Cream");
                Part("Cotton tail", body, new Vector3(0, .37f, -.42f), Vector3.one * .24f, "Cream");
                head = Group("Head", body, new Vector3(0, .57f, .34f));
                Part("Cheeks", head, Vector3.zero, new Vector3(.48f, .44f, .44f), "Cream");
                for (int side = -1; side <= 1; side += 2)
                {
                    var ear = Group("Ear", head, new Vector3(side * .13f, .16f, -.02f));
                    ear.localRotation = Quaternion.Euler(-8, 0, -side * 12);
                    Part("Velvet ear", ear, new Vector3(0, .25f, 0), new Vector3(.15f, .59f, .14f), "Cream");
                    Part("Pink inner ear", ear, new Vector3(0, .26f, .065f), new Vector3(.085f, .43f, .025f), "PetalBlush");
                    limbs.Add(ear);
                    Part("Paw", body, new Vector3(side * .22f, .1f, .21f), new Vector3(.23f, .19f, .36f), "Cream");
                    Part("Blush", head, new Vector3(side * .17f, -.035f, .17f), new Vector3(.13f, .07f, .06f), "PetalBlush");
                }
                Eyes(head, .12f, .07f, .19f, .068f);
                Part("Nose", head, new Vector3(0, -.015f, .24f), new Vector3(.085f, .06f, .07f), "Coral");
            }
            else if (species == GardenAnimal.Species.Turtle)
            {
                Part("Shell rim", body, new Vector3(0, .23f, 0), new Vector3(.81f, .26f, .94f), "Lime");
                Part("Domed shell", body, new Vector3(0, .34f, -.03f), new Vector3(.74f, .52f, .83f), "Foliage");
                for (int i = 0; i < 5; i++)
                {
                    float angle = i * Mathf.PI * 2 / 5;
                    Part("Shell scute", body, new Vector3(Mathf.Cos(angle) * .21f, .55f, Mathf.Sin(angle) * .25f - .03f), new Vector3(.22f, .095f, .25f), "Leaf");
                }
                head = Group("Head", body, new Vector3(0, .25f, .49f));
                Part("Head shape", head, Vector3.zero, new Vector3(.34f, .31f, .4f), "Lime");
                Eyes(head, .11f, .08f, .155f, .068f);
                for (int i = 0; i < 4; i++)
                {
                    var foot = Group("Foot", body, new Vector3(i % 2 == 0 ? -.3f : .3f, .105f, i < 2 ? .25f : -.25f));
                    Part("Flipper", foot, Vector3.zero, new Vector3(.25f, .16f, .29f), "Lime"); limbs.Add(foot);
                }
            }
            else if (species == GardenAnimal.Species.Bird || species == GardenAnimal.Species.Butterfly)
            {
                bool bird = species == GardenAnimal.Species.Bird;
                Part("Body shape", body, new Vector3(0, .29f, 0), bird ? new Vector3(.4f, .44f, .57f) : new Vector3(.1f, .1f, .37f), bird ? "Aqua" : "Ink");
                head = Group("Head", body, new Vector3(0, bird ? .48f : .3f, .2f));
                Part("Head shape", head, Vector3.zero, Vector3.one * (bird ? .34f : .13f), bird ? "Aqua" : "Ink");
                if (bird)
                {
                    Part("Cream bib", body, new Vector3(0, .29f, .23f), new Vector3(.29f, .29f, .13f), "Cream");
                    Part("Beak", head, new Vector3(0, -.04f, .2f), new Vector3(.13f, .1f, .23f), "PetalButter");
                    Eyes(head, .1f, .045f, .135f, .06f);
                    Part("Tail", body, new Vector3(0, .32f, -.36f), new Vector3(.23f, .09f, .4f), "Aqua");
                    for (int s = -1; s <= 1; s += 2) Part("Foot", body, new Vector3(s * .1f, .035f, .06f), new Vector3(.07f, .07f, .19f), "PetalButter");
                }
                left = Group("Left wing", body, new Vector3(-.1f, .33f, 0));
                right = Group("Right wing", body, new Vector3(.1f, .33f, 0));
                for (int side = -1; side <= 1; side += 2)
                {
                    Transform wing = side < 0 ? left : right;
                    string color = bird ? "Aqua" : phase % 2 < 1 ? "PetalLilac" : "PetalButter";
                    Part("Wing", wing, new Vector3(side * .23f, 0, 0), new Vector3(.55f, .07f, bird ? .3f : .46f), color);
                    if (!bird) Part("Wing spot", wing, new Vector3(side * .28f, .042f, .045f), new Vector3(.16f, .018f, .2f), "Coral");
                }
            }
            else
            {
                Part("Red shell", body, new Vector3(0, .15f, 0), new Vector3(.31f, .26f, .39f), "Coral");
                Part("Wing seam", body, new Vector3(0, .267f, 0), new Vector3(.018f, .02f, .3f), "Ink");
                head = Group("Head", body, new Vector3(0, .13f, .2f));
                Part("Head shape", head, Vector3.zero, Vector3.one * .17f, "Ink");
                for (int i = 0; i < 4; i++) Part("Spot", body, new Vector3(i % 2 == 0 ? -.085f : .085f, .25f, i < 2 ? .085f : -.085f), new Vector3(.065f, .022f, .065f), "Ink");
                for (int i = 0; i < 6; i++)
                {
                    var leg = Group("Leg", body, new Vector3(i % 2 == 0 ? -.14f : .14f, .06f, (i / 2 - 1) * .1f));
                    Part("Leg shape", leg, Vector3.zero, new Vector3(.13f, .035f, .04f), "Ink"); limbs.Add(leg);
                }
            }
            var animal = root.gameObject.AddComponent<GardenAnimal>();
            var so = new SerializedObject(animal);
            so.FindProperty("species").enumValueIndex = (int)species;
            so.FindProperty("body").objectReferenceValue = body;
            so.FindProperty("head").objectReferenceValue = head;
            so.FindProperty("leftWing").objectReferenceValue = left;
            so.FindProperty("rightWing").objectReferenceValue = right;
            so.FindProperty("perch").objectReferenceValue = perch;
            so.FindProperty("groundA").vector3Value = a;
            so.FindProperty("groundB").vector3Value = b;
            so.FindProperty("phase").floatValue = phase;
            so.FindProperty("travelSeconds").floatValue = species == GardenAnimal.Species.Turtle ? 8 : species == GardenAnimal.Species.Ladybug ? 6 : 3;
            so.FindProperty("restSeconds").floatValue = species == GardenAnimal.Species.Butterfly ? 2.5f : species == GardenAnimal.Species.Turtle ? 9 : 6;
            so.FindProperty("flightHeight").floatValue = species == GardenAnimal.Species.Butterfly ? .5f : 1.5f;
            var array = so.FindProperty("limbs"); array.arraySize = limbs.Count;
            for (int i = 0; i < limbs.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = limbs[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Eyes(Transform head, float x, float y, float z, float size)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Part("Eye", head, new Vector3(side * x, y, z), Vector3.one * size, "Ink");
                Part("Eye sparkle", head, new Vector3(side * x - size * .13f, y + size * .17f, z + size * .43f), Vector3.one * size * .29f, "Cream");
            }
        }

        private static void ReservePatch(Vector3 a, Vector3 b, float radius)
        {
            // Leave a small bare lawn patch around each ground route, instead of layering animals on flowers.
            a.y = b.y = 0;
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.name != "Flower" && t.name != "Daisy" && t.name != "Lavender" && t.name != "Tulip" && t.name != "Rock") continue;
                Vector3 point = t.position; point.y = 0;
                Vector3 line = b - a;
                float along = line.sqrMagnitude < .001f ? 0 : Mathf.Clamp01(Vector3.Dot(point - a, line) / line.sqrMagnitude);
                float clearance = radius + .35f * t.lossyScale.x;
                if (Vector3.Distance(point, a + line * along) < clearance) t.gameObject.SetActive(false);
            }
        }

        private static Transform Group(string name, Transform parent, Vector3 position)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = position; return t;
        }

        private static Transform Part(string name, Transform parent, Vector3 position, Vector3 scale, string material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = Mat(material);
            return go.transform;
        }
    }
}
