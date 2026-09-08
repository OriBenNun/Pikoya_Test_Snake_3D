using System.IO;
using UnityEditor;
using UnityEngine;
using static GardenSnake.Editor.GardenPalette;

namespace GardenSnake.Editor
{
    /// <summary>Reproducible garden vignettes and small, seamless hand-patterned material maps.</summary>
    public static class GardenDecor
    {
        public static void Prepare()
        {
            foreach (string name in new[] { "RimLong", "RimShort", "Gnome", "Tree", "Bush", "WateringCan", "Spade", "Rake", "FlowerPot", "Tulip", "Lavender", "Daisy", "Tile", "Planter" })
                GardenBuilder.Prefab(name);
        }

        public static void TextureBoard(Material[] grass)
        {
            var lawn = Pattern("GrassWeave", false);
            foreach (var material in grass)
            {
                material.SetTexture("_BaseMap", lawn);
                material.SetFloat("_Smoothness", .16f);
                EditorUtility.SetDirty(material);
            }
            var clay = AssetDatabase.LoadAssetAtPath<Material>(GardenBuilder.Root + "/Materials/Base.mat");
            clay.SetTexture("_BaseMap", Pattern("ClayGlaze", true));
            clay.SetTextureScale("_BaseMap", new Vector2(4, 1));
            clay.SetFloat("_Smoothness", .48f);
            EditorUtility.SetDirty(clay);
        }

        private static Texture2D Pattern(string name, bool clay)
        {
            const int size = 128;
            var pixels = new Color[size * size];
            var random = new System.Random(clay ? 118 : 932);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float grain = (float)random.NextDouble();
                float value = clay ? .94f + grain * .06f : .91f + grain * .05f;
                if (clay) value -= Mathf.Pow(.5f + .5f * Mathf.Sin(y * Mathf.PI * 8 / size + Mathf.Sin(x * Mathf.PI * 2 / size) * .5f), 12) * .045f;
                pixels[y * size + x] = new Color(value, value, value);
            }
            // Short pairs of strokes suggest blades; no noisy photographic detail at game scale.
            for (int i = 0; i < (clay ? 100 : 46); i++)
            {
                int x = random.Next(size), y = random.Next(size);
                int length = clay ? 1 : random.Next(3, 7);
                for (int p = 0; p < length; p++)
                {
                    float value = clay ? .8f : .78f;
                    pixels[((y + p) % size) * size + (x + p / 3) % size] = new Color(value, value, value);
                    if (!clay) pixels[((y + p) % size) * size + (x - p / 3 + size) % size] = new Color(.99f, .99f, .93f);
                }
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            texture.SetPixels(pixels); texture.Apply();
            string path = GardenBuilder.Root + "/Art/" + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = size;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static void Create()
        {
            var root = new GameObject("Garden vignettes").transform;
            // Tall silhouettes sit behind or beside the board; the near edge stays readable.
            Prop(root, "Tree", -12.7f, 6.8f, 1.25f, 20);
            Prop(root, "Tree", 12.8f, 6.5f, 1.35f, -25);
            Prop(root, "Tree", -5.8f, 10.1f, .95f, 15);
            Prop(root, "Tree", 5.8f, 10.6f, 1.1f, -40);
            Prop(root, "Tree", -15.5f, -1, 1.25f, 0);
            Prop(root, "Tree", 15.6f, -2.2f, 1.1f, 35);
            foreach (var p in new[] { new Vector2(-12.4f, 2.5f), new Vector2(12.5f, 1.2f), new Vector2(-12.5f, -4.8f),
                new Vector2(12.6f, -4.5f), new Vector2(-9.3f, 8.2f), new Vector2(9.5f, 8.3f), new Vector2(-2.3f, 9.5f), new Vector2(2.2f, 9.6f) })
                Prop(root, "Bush", p.x, p.y, 1.25f, p.x * 12);

            Prop(root, "Gnome", -8.1f, -7.8f, 1.18f, 168);
            Prop(root, "Gnome", 11.9f, 4.3f, 1.1f, 142);
            Prop(root, "FlowerPot", -6.6f, -7.7f, 1.05f, 20);
            Prop(root, "FlowerPot", -9.4f, -7.6f, .8f, -10);
            Prop(root, "WateringCan", 7.3f, -7.7f, 1.35f, -30);
            var spade = Prop(root, "Spade", 9.2f, -7.6f, 1.15f, 20);
            spade.localRotation *= Quaternion.Euler(0, 0, -25);
            var rake = Prop(root, "Rake", 10.3f, -7.9f, 1.1f, -15);
            rake.localRotation *= Quaternion.Euler(0, 0, 30);
            Prop(root, "FlowerPot", 5.8f, -7.8f, .95f, 0);
            Prop(root, "WateringCan", -12.3f, .2f, 1, 45);

            // Curved stepping paths lead into the garden instead of an endless random lawn.
            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 7; i++)
            {
                var stone = Prop(root, "Rock", side * (11.9f + .2f * Mathf.Sin(i)), -6 + i * 1.65f, 1, i * 37);
                stone.localScale = new Vector3(.8f, .24f, .62f);
            }
            // Small flowerbeds mix tall spires, tulips and flat daisies around the vignettes.
            for (int i = 0; i < 36; i++)
            {
                float side = i < 18 ? -1 : 1;
                float z = -6.5f + (i % 18) * .78f;
                float x = side * (13.3f + .5f * Mathf.Sin(i * 2.1f));
                Prop(root, i % 3 == 0 ? "Tulip" : i % 3 == 1 ? "Lavender" : "Daisy", x, z, .75f + i % 4 * .12f, i * 47);
            }
        }

        private static Transform Prop(Transform parent, string name, float x, float z, float scale, float yaw)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GardenBuilder.Root + "/Prefabs/" + name + ".prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = new Vector3(x, -.76f, z);
            instance.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            instance.transform.localScale = Vector3.one * scale;
            return instance.transform;
        }
    }
}
