using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GardenSnake.Editor
{
    /// <summary>
    /// Draws the handful of UI shapes the game needs straight into PNG assets, so the project
    /// carries no imported UI art and every corner radius, ring and icon stays editable in code.
    /// Shapes are described as signed distance fields and sampled 4x4 per pixel for clean edges.
    /// </summary>
    public static class GardenSprites
    {
        private const string Folder = GardenBuilder.Root + "/UI/Generated";

        public static Sprite RoundedRect(string name, int size, int radius) =>
            Ensure(name, size, radius + 1, point => Fill(RoundedBox(point, size, radius)));

        public static Sprite RoundedOutline(string name, int size, int radius, float thickness) =>
            Ensure(name, size, radius + 1, point => Fill(Mathf.Abs(RoundedBox(point, size, radius) + thickness * .5f) - thickness * .5f));

        public static Sprite Circle(string name, int size) =>
            Ensure(name, size, 0, point => Fill(Vector2.Distance(point, Half(size)) - (size * .5f - 1.5f)));

        /// <summary>A soft radial falloff used for glows and the apple's ground pool.</summary>
        public static Sprite Glow(string name, int size, float power) =>
            Ensure(name, size, 0, point =>
            {
                float distance = Vector2.Distance(point, Half(size)) / (size * .5f);
                return Mathf.Pow(Mathf.Clamp01(1 - distance), power);
            });

        /// <summary>A soft round pool of light, written in colour, for the lawn backdrop.</summary>
        public static Texture2D RadialGradient(string name, int size, Color inner, Color outer)
        {
            Directory.CreateDirectory(Folder);
            string path = Folder + "/" + name + ".png";
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
            var pixels = new Color[size * size];
            var centre = new Vector2(size * .5f, size * .5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), centre) / (size * .5f);
                pixels[y * size + x] = Color.Lerp(inner, outer, Mathf.SmoothStep(0, 1, Mathf.Clamp01(distance)));
            }
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>The soft round mote every particle system in the game is drawn with.</summary>
        public static Texture2D SoftDot(string name, int size, float power)
        {
            Glow(name, size, power);
            return Texture(name);
        }

        public static Texture2D Texture(string name) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/" + name + ".png");

        public static Sprite PauseIcon(string name, int size) =>
            Ensure(name, size, 0, point =>
            {
                float bar = size * .17f;
                float gap = size * .13f;
                float height = size * .3f;
                Vector2 centred = point - Half(size);
                float left = Box(centred + new Vector2(bar * .5f + gap * .5f, 0), new Vector2(bar * .5f, height), bar * .22f);
                float right = Box(centred - new Vector2(bar * .5f + gap * .5f, 0), new Vector2(bar * .5f, height), bar * .22f);
                return Fill(Mathf.Min(left, right));
            });

        public static Sprite PlayIcon(string name, int size) =>
            Ensure(name, size, 0, point =>
            {
                Vector2 centred = (point - Half(size)) / size;
                centred.x -= .04f;
                return Fill(Triangle(centred, .3f) - .02f);
            });

        public static Sprite SoundIcon(string name, int size, bool on) =>
            Ensure(name, size, 0, point =>
            {
                Vector2 p = (point - Half(size)) / size;
                p.x += .06f;
                float body = Box(p + new Vector2(.16f, 0), new Vector2(.06f, .1f), .02f);
                float cone = Triangle(new Vector2(-p.x + .04f, p.y), .26f);
                float shape = Mathf.Min(body, cone);
                if (on)
                {
                    shape = Mathf.Min(shape, Arc(p - new Vector2(.06f, 0), .19f, .028f));
                    shape = Mathf.Min(shape, Arc(p - new Vector2(.06f, 0), .3f, .028f));
                }
                else
                {
                    shape = Mathf.Min(shape, Segment(p, new Vector2(.14f, -.11f), new Vector2(.32f, .11f), .03f));
                    shape = Mathf.Min(shape, Segment(p, new Vector2(.14f, .11f), new Vector2(.32f, -.11f), .03f));
                }
                return Fill(shape * size);
            });

        // ---------------------------------------------------------------- distance fields

        private static Vector2 Half(int size) => new Vector2(size * .5f, size * .5f);

        private static float Fill(float distance) => Mathf.Clamp01(.5f - distance);

        private static float RoundedBox(Vector2 point, int size, int radius) =>
            Box(point - Half(size), new Vector2(size * .5f - 1, size * .5f - 1), radius);

        private static float Box(Vector2 point, Vector2 extents, float radius)
        {
            Vector2 delta = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) - extents + Vector2.one * radius;
            return Vector2.Max(delta, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(delta.x, delta.y), 0) - radius;
        }

        private static float Triangle(Vector2 point, float size)
        {
            // pointing right, roughly equilateral
            float a = Segment(point, new Vector2(-size * .55f, size), new Vector2(size, 0), 0);
            float b = Segment(point, new Vector2(-size * .55f, -size), new Vector2(size, 0), 0);
            float c = Segment(point, new Vector2(-size * .55f, -size), new Vector2(-size * .55f, size), 0);
            float distance = Mathf.Min(a, Mathf.Min(b, c));
            bool inside = point.x < size && point.x > -size * .55f &&
                          Mathf.Abs(point.y) < size * (1 - (point.x + size * .55f) / (size * 1.55f));
            return inside ? -distance : distance;
        }

        private static float Segment(Vector2 point, Vector2 from, Vector2 to, float thickness)
        {
            Vector2 span = to - from;
            float t = Mathf.Clamp01(Vector2.Dot(point - from, span) / Vector2.Dot(span, span));
            return Vector2.Distance(point, from + span * t) - thickness;
        }

        private static float Arc(Vector2 point, float radius, float thickness)
        {
            if (point.x < 0) return 10;
            float angle = Mathf.Abs(Mathf.Atan2(point.y, point.x));
            if (angle > .8f) return 10;
            return Mathf.Abs(point.magnitude - radius) - thickness;
        }

        // ---------------------------------------------------------------- rasterising

        private static Sprite Ensure(string name, int size, int border, Func<Vector2, float> coverage)
        {
            Directory.CreateDirectory(Folder);
            string path = Folder + "/" + name + ".png";
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float total = 0;
                for (int sy = 0; sy < 4; sy++)
                for (int sx = 0; sx < 4; sx++)
                    total += Mathf.Clamp01(coverage(new Vector2(x + (sx + .5f) / 4f, y + (sy + .5f) / 4f)));
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(total / 16f) * 255);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 100;
            importer.spriteBorder = border > 0
                ? new Vector4(border, border, border, border)
                : Vector4.zero;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
