using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GardenSnake.Editor
{
    /// <summary>Small, reproducible clay wildlife rigs. No colliders or gameplay components.</summary>
    public static class GardenWildlifeBuilder
    {
        private static Material SharedMaterial(string name) => AssetDatabase.LoadAssetAtPath<Material>(GardenBuilder.Root + "/Materials/" + name + ".mat");

        [MenuItem("Garden Snake/Add ambient wildlife")]
        public static void Install()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != GardenBuilder.ScenePath || scene.isDirty)
                throw new InvalidOperationException("Install wildlife in the saved GardenSnake scene, outside Play mode.");
            var old = GameObject.Find("Garden wildlife");
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t != null && t.name is "Bird perch" or "Perch branch") UnityEngine.Object.DestroyImmediate(t.gameObject);
            Create();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void Create()
        {
            if (GameObject.Find("Garden wildlife") != null) return;
            GardenWildlifeMeshes.Prepare();
            Tint("WildlifeShell", "78954E");
            Tint("WildlifeSkin", "A8BE6D");
            Tint("WildlifeShellLight", "91AA60");
            Tint("WildlifeSeam", "425E37");
            Tint("WildlifeFeather", "367F8A");
            Tint("WildlifeWing", "ECAF60");
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
                        Vector3 seat = perch.localPosition + Vector3.down * .065f;
                        Vector3 trunk = new Vector3(0, 1.02f, 0);
                        Vector3 elbow = Vector3.Lerp(trunk, seat, .55f) + Vector3.down * .12f;
                        Branch(tree, trunk, elbow, .13f);
                        Branch(tree, elbow, seat + (seat - elbow).normalized * .22f, .09f);
                        Vector3 twig = Vector3.Lerp(elbow, seat, .6f);
                        Branch(tree, twig, twig + new Vector3(.32f, .14f, .12f), .038f);
                        var leaf = Sculpt("Branch leaf", "Ear", perch, new Vector3(.19f, -.01f, .16f), new Vector3(.13f, .33f, .045f), "Leaf");
                        leaf.localRotation = Quaternion.Euler(60, 20, -45);
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
            ReservePatch(a, b, species is GardenAnimal.Species.Bunny or GardenAnimal.Species.Turtle ? .8f : .42f);
            root.localRotation = Quaternion.Euler(0, 150 + phase * 35, 0);
            Transform body = Group("Body", root, Vector3.zero), head, left = null, right = null;
            var limbs = new List<Transform>();
            var ears = new List<Transform>();
            if (species == GardenAnimal.Species.Bunny)
            {
                Sculpt("Pear body", "Pear", body, new Vector3(0, .36f, -.06f), new Vector3(.56f, .67f, .7f), "Cream");
                Part("Cotton tail", body, new Vector3(0, .37f, -.42f), Vector3.one * .24f, "Cream");
                head = Group("Head", body, new Vector3(0, .57f, .34f));
                Part("Cheeks", head, Vector3.zero, new Vector3(.48f, .44f, .44f), "Cream");
                for (int side = -1; side <= 1; side += 2)
                {
                    var ear = Group("Ear", head, new Vector3(side * .13f, .16f, -.02f));
                    ear.localRotation = Quaternion.Euler(-8, 0, -side * 12);
                    Sculpt("Velvet ear", "Ear", ear, new Vector3(0, .25f, 0), new Vector3(.18f, .59f, .13f), "Cream");
                    Sculpt("Pink inner ear", "Ear", ear, new Vector3(0, .26f, .058f), new Vector3(.105f, .43f, .025f), "PetalBlush");
                    ears.Add(ear);
                    Part("Haunch", body, new Vector3(side * .2f, .22f, -.19f), new Vector3(.34f, .37f, .41f), "Cream");
                    var rear = Group("Hind paw", body, new Vector3(side * .23f, .105f, -.24f));
                    Part("Hind foot", rear, new Vector3(0, -.015f, .10f), new Vector3(.24f, .18f, .39f), "Cream");
                    limbs.Add(rear);
                    var front = Group("Forepaw", body, new Vector3(side * .155f, .24f, .24f));
                    Part("Foreleg", front, new Vector3(0, -.12f, .025f), new Vector3(.13f, .3f, .15f), "Cream");
                    Part("Toes", front, new Vector3(0, -.195f, .07f), new Vector3(.15f, .095f, .20f), "Cream");
                    limbs.Add(front);
                    Part("Muzzle", head, new Vector3(side * .065f, -.072f, .184f), new Vector3(.15f, .115f, .10f), "Cream");
                    Part("Blush", head, new Vector3(side * .164f, -.025f, .147f), new Vector3(.10f, .055f, .025f), "PetalBlush");
                }
                Eyes(head, .12f, .07f, .19f, .068f);
                Sculpt("Nose", "Beak", head, new Vector3(0, -.035f, .238f), new Vector3(.07f, .045f, .045f), "PetalBlush");
                Part("Mouth", head, new Vector3(0, -.10f, .227f), new Vector3(.025f, .035f, .014f), "Ink");
            }
            else if (species == GardenAnimal.Species.Turtle)
            {
                Part("Shell rim", body, new Vector3(0, .23f, 0), new Vector3(.81f, .26f, .94f), "WildlifeSkin");
                var shellPosition = new Vector3(0, .30f, -.03f);
                var shellSize = new Vector3(.76f, .58f, .86f);
                Sculpt("Domed shell", "Shell", body, shellPosition, shellSize * .975f, "WildlifeSeam");
                Sculpt("Crown scute", "ShellCenter", body, shellPosition, shellSize, "WildlifeShell");
                for (int i = 0; i < 6; i++)
                    Sculpt("Shell scute", "ShellPlate" + i, body, shellPosition, shellSize, i % 2 == 0 ? "WildlifeShell" : "WildlifeShellLight");
                head = Group("Head", body, new Vector3(0, .25f, .49f));
                Part("Head shape", head, Vector3.zero, new Vector3(.34f, .31f, .4f), "WildlifeSkin");
                Eyes(head, .11f, .08f, .155f, .068f);
                for (int i = 0; i < 4; i++)
                {
                    var foot = Group("Foot", body, new Vector3(i % 2 == 0 ? -.3f : .3f, .105f, i < 2 ? .25f : -.25f));
                    Part("Flipper", foot, Vector3.zero, new Vector3(.25f, .16f, .29f), "WildlifeSkin"); limbs.Add(foot);
                }
            }
            else if (species is GardenAnimal.Species.Bird or GardenAnimal.Species.Butterfly)
            {
                bool bird = species == GardenAnimal.Species.Bird;
                Sculpt("Body shape", "Bird", body, new Vector3(0, .29f, 0), bird ? new Vector3(.43f, .46f, .65f) : new Vector3(.10f, .1f, .43f), bird ? "Aqua" : "Ink");
                head = Group("Head", body, new Vector3(0, bird ? .48f : .3f, .2f));
                Part("Head shape", head, Vector3.zero, Vector3.one * (bird ? .34f : .13f), bird ? "Aqua" : "Ink");
                if (bird)
                {
                    Part("Cream bib", body, new Vector3(0, .29f, .23f), new Vector3(.29f, .29f, .13f), "Cream");
                    Sculpt("Beak", "Beak", head, new Vector3(0, -.04f, .20f), new Vector3(.14f, .10f, .24f), "PetalButter");
                    Eyes(head, .1f, .045f, .135f, .06f);
                    for (int f = -1; f <= 1; f++)
                    {
                        var feather = Sculpt("Tail feather", "Feather", body, new Vector3(f * .065f, .31f, -.40f), new Vector3(.10f, .06f, .43f), f == 0 ? "Aqua" : "WildlifeFeather");
                        feather.localRotation = Quaternion.Euler(-12, -f * 15, 0);
                    }
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var foot = Group("Foot", body, new Vector3(s * .1f, .12f, .06f));
                        Part("Ankle", foot, new Vector3(0, -.055f, 0), new Vector3(.045f, .12f, .045f), "PetalButter");
                        for (int toe = -1; toe <= 1; toe++)
                        {
                            var digit = Part("Toe", foot, new Vector3(toe * .025f, -.095f, .05f), new Vector3(.029f, .035f, .15f), "PetalButter");
                            digit.localRotation = Quaternion.Euler(0, toe * 18, 0);
                        }
                        limbs.Add(foot);
                    }
                }
                else for (int s = -1; s <= 1; s += 2)
                {
                    var antenna = Part("Antenna", head, new Vector3(s * .045f, .09f, .03f), new Vector3(.018f, .19f, .018f), "Ink");
                    antenna.localRotation = Quaternion.Euler(15, 0, -s * 25);
                    Part("Antenna tip", head, new Vector3(s * .085f, .18f, .06f), Vector3.one * .038f, "PetalButter");
                }
                left = Group("Left wing", body, new Vector3(-.1f, .33f, 0));
                right = Group("Right wing", body, new Vector3(.1f, .33f, 0));
                for (int side = -1; side <= 1; side += 2)
                {
                    Transform wing = side < 0 ? left : right;
                    string color = bird ? "Aqua" : "WildlifeWing";
                    // A 180-degree rotation mirrors the authored wing without negative scale/culling.
                    var surface = Group("Wing surface", wing, Vector3.zero);
                    if (side < 0) surface.localRotation = Quaternion.Euler(0, 0, 180);
                    Sculpt("Flight feathers", bird ? "BirdWing" : "ButterflyWing", surface, Vector3.zero, Vector3.one, bird ? color : "Ink");
                    if (!bird)
                    {
                        Sculpt("Wing color", "ButterflyWing", surface, new Vector3(.025f, side * .014f, 0), new Vector3(.87f, 1, .86f), color);
                        foreach (float face in new[] { -1f, 1f })
                        {
                            Part("Eyespot border", surface, new Vector3(.39f, face * .038f, .23f), new Vector3(.16f, .013f, .18f), "Ink");
                            Part("Eyespot", surface, new Vector3(.39f, face * .046f, .23f), new Vector3(.095f, .013f, .11f), "PetalButter");
                            Part("Hindwing mark", surface, new Vector3(.28f, face * .035f, -.23f), new Vector3(.12f, .013f, .11f), "Coral");
                        }
                    }
                }
            }
            else
            {
                Sculpt("Red shell", "LadybugShell", body, new Vector3(0, .15f, 0), new Vector3(.31f, .26f, .39f), "Coral");
                Sculpt("Shell markings", "LadybugMarkings", body, Vector3.zero, Vector3.one, "Ink");
                head = Group("Head", body, new Vector3(0, .13f, .2f));
                Part("Head shape", head, Vector3.zero, Vector3.one * .17f, "Ink");
                LadybugEyes(head);
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
            var earArray = so.FindProperty("ears"); earArray.arraySize = ears.Count;
            for (int i = 0; i < ears.Count; i++) earArray.GetArrayElementAtIndex(i).objectReferenceValue = ears[i];
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

        /// <summary>Updates only ladybug art, keeping existing paths and animation rigs.</summary>
        public static int PolishLadybugs()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before editing wildlife art.");
            int count = 0;
            foreach (var animal in UnityEngine.Object.FindObjectsByType<GardenAnimal>(FindObjectsSortMode.None))
            {
                Transform body = animal.transform.Find("Body");
                Transform shell = body != null ? body.Find("Red shell") : null;
                if (shell == null) continue;
                shell.GetComponent<MeshFilter>().sharedMesh = GardenWildlifeMeshes.Get("LadybugShell");
                for (int i = body.childCount - 1; i >= 0; i--)
                {
                    Transform part = body.GetChild(i);
                    if (part.name is "Spot" or "Wing seam" or "Shell markings")
                        UnityEngine.Object.DestroyImmediate(part.gameObject);
                }
                Sculpt("Shell markings", "LadybugMarkings", body, Vector3.zero, Vector3.one, "Ink");
                LadybugEyes(body.Find("Head"));
                count++;
            }
            return count;
        }

        private static void LadybugEyes(Transform head)
        {
            if (head.Find("Eye white") != null) return;
            for (int side = -1; side <= 1; side += 2)
            {
                Part("Eye white", head, new Vector3(side * .033f, .025f, .071f), new Vector3(.045f, .048f, .023f), "Cream");
                Part("Pupil", head, new Vector3(side * .032f, .025f, .082f), new Vector3(.023f, .029f, .012f), "Ink");
                Part("Eye glint", head, new Vector3(side * .032f - .004f, .032f, .088f), Vector3.one * .008f, "Cream");
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
            go.GetComponent<Renderer>().sharedMaterial = SharedMaterial(material);
            return go.transform;
        }

        private static Transform Sculpt(string name, string mesh, Transform parent, Vector3 position, Vector3 scale, string material)
        {
            var t = Group(name, parent, position); t.localScale = scale;
            t.gameObject.AddComponent<MeshFilter>().sharedMesh = GardenWildlifeMeshes.Get(mesh);
            var renderer = t.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = SharedMaterial(material);
            if (mesh == "LadybugMarkings")
            {
                // Painted surface detail must not cast a second, almost coincident
                // shadow onto its shell or receive the shell's shadow acne.
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            return t;
        }

        private static void Tint(string name, string hex)
        {
            string path = GardenBuilder.Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return; // Preserve the authored palette and finish on rebuild.
            material = new Material(SharedMaterial("Aqua")) { name = name };
            AssetDatabase.CreateAsset(material, path);
            material.SetColor("_BaseColor", GardenPalette.Hex(hex));
            material.SetFloat("_Smoothness", .26f);
            EditorUtility.SetDirty(material);
        }

        private static void Branch(Transform tree, Vector3 from, Vector3 to, float radius)
        {
            var branch = Part("Perch branch", tree, (from + to) * .5f, new Vector3(radius * 2, Vector3.Distance(from, to) + radius, radius * 2), "Bark");
            branch.localRotation = Quaternion.FromToRotation(Vector3.up, to - from);
        }
    }
}
