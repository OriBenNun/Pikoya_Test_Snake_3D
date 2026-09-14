using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GardenSnake.Editor
{
    /// <summary>Authored silhouettes, baked once into shared meshes by the wildlife builder.</summary>
    public static class GardenWildlifeMeshes
    {
        private const string Folder = GardenBuilder.Root + "/Art/Wildlife";
        private static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();

        public static void Prepare()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(GardenBuilder.Root + "/Art", "Wildlife");
            meshes.Clear();
            Save("Pear", Surface(p => new Vector3(p.x * (1 - p.y * .65f), p.y, p.z * (1 - p.y * .45f))));
            Save("Ear", Surface(p => new Vector3(p.x * (1 - p.y * .7f), p.y, p.z + .22f * p.y * p.y)));
            Save("Bird", Surface(p => new Vector3(p.x * (1 + p.z * .55f), p.y + .13f * p.z, p.z)));
            Save("Beak", Surface(p => new Vector3(p.x * (1 - p.z * 1.6f), p.y * (1 - p.z), p.z)));
            Save("Feather", Surface(p => new Vector3(p.x * (1 + p.z * 1.1f), p.y, p.z)));
            Save("BirdWing", Wing(new[] {
                new Vector2(0,.11f), new Vector2(.16f,.21f), new Vector2(.39f,.18f),
                new Vector2(.61f,.04f), new Vector2(.69f,-.15f), new Vector2(.57f,-.11f),
                new Vector2(.56f,-.23f), new Vector2(.43f,-.17f), new Vector2(.4f,-.28f),
                new Vector2(.27f,-.20f), new Vector2(.13f,-.22f), new Vector2(0,-.1f)
            }, .045f));
            Save("ButterflyWing", Wing(new[] {
                new Vector2(0,.1f), new Vector2(.16f,.32f), new Vector2(.40f,.43f),
                new Vector2(.57f,.38f), new Vector2(.59f,.23f), new Vector2(.47f,.06f),
                new Vector2(.32f,-.02f), new Vector2(.47f,-.15f), new Vector2(.44f,-.34f),
                new Vector2(.3f,-.4f), new Vector2(.15f,-.3f), new Vector2(0,-.08f)
            }, .018f));
            Save("Shell", Surface(p => new Vector3(p.x, p.y < 0 ? p.y * .28f : p.y, p.z)));
            var hex = new Vector2[6];
            for (int i = 0; i < 6; i++) hex[i] = new Vector2(Mathf.Cos(i * Mathf.PI / 3), Mathf.Sin(i * Mathf.PI / 3)) * .22f;
            Save("ShellCenter", Plate(hex));
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3, b = (i + 1) * Mathf.PI / 3;
                var edge = new List<Vector2> { hex[i], hex[(i + 1) % 6] };
                for (int j = 0; j <= 6; j++)
                {
                    float angle = Mathf.Lerp(b, a, j / 6f);
                    edge.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * .485f);
                }
                Save("ShellPlate" + i, Plate(edge.ToArray()));
            }
            AssetDatabase.SaveAssets();
        }

        public static Mesh Get(string name)
        {
            if (meshes.TryGetValue(name, out Mesh mesh)) return mesh;
            mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/" + name + ".asset");
            if (mesh == null) throw new InvalidOperationException("Missing authored wildlife mesh: " + name);
            meshes[name] = mesh;
            return mesh;
        }

        private static void Save(string name, Mesh mesh)
        {
            string path = Folder + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            mesh.name = name;
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); existing = mesh; }
            // Existing assets contain the Blender art pass. Rebuilding scene rigs
            // must preserve those sculpted meshes and their stable references.
            else UnityEngine.Object.DestroyImmediate(mesh);
            meshes[name] = existing;
        }

        private static Mesh Build(List<Vector3> vertices, List<int> triangles)
        {
            var mesh = new Mesh();
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh Surface(Func<Vector3, Vector3> deform)
        {
            const int rings = 20, sides = 32;
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float latitude = Mathf.PI * r / rings;
                for (int s = 0; s <= sides; s++)
                {
                    float longitude = Mathf.PI * 2 * s / sides;
                    vertices.Add(deform(new Vector3(Mathf.Sin(latitude) * Mathf.Cos(longitude), Mathf.Cos(latitude), Mathf.Sin(latitude) * Mathf.Sin(longitude)) * .5f));
                    if (r == rings || s == sides) continue;
                    int a = r * (sides + 1) + s, b = a + sides + 1;
                    triangles.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                }
            }
            var mesh = Build(vertices, triangles);
            var normals = mesh.normals;
            for (int r = 0; r <= rings; r++)
            {
                int first = r * (sides + 1), last = first + sides;
                normals[first] = normals[last] = (normals[first] + normals[last]).normalized;
                if (r == 0 || r == rings)
                    for (int s = 0; s <= sides; s++) normals[first + s] = r == 0 ? Vector3.up : Vector3.down;
            }
            mesh.normals = normals;
            return mesh;
        }

        private static Mesh Wing(Vector2[] outline, float thickness)
        {
            var rounded = new List<Vector2>();
            for (int i = 0; i < outline.Length; i++)
            {
                var before = Vector2.Lerp(outline[(i + outline.Length - 1) % outline.Length], outline[i], .6f);
                var after = Vector2.Lerp(outline[i], outline[(i + 1) % outline.Length], .4f);
                for (int j = 0; j < 4; j++)
                {
                    float t = j / 3f;
                    rounded.Add((1 - t) * (1 - t) * before + 2 * (1 - t) * t * outline[i] + t * t * after);
                }
            }
            outline = rounded.ToArray();
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            Vector2 center = new Vector2(.25f, 0);
            for (int side = 0; side < 2; side++)
            {
                int offset = vertices.Count;
                const int rings = 6;
                for (int r = 0; r <= rings; r++)
                {
                    float radius = Mathf.Max(.001f, r / (float)rings);
                    foreach (var edge in outline)
                    {
                        Vector2 p = Vector2.Lerp(center, edge, radius);
                        vertices.Add(new Vector3(p.x, (side == 0 ? 1 : -1) * thickness * (1 - radius * radius), p.y));
                    }
                    if (r == 0) continue;
                    for (int j = 0; j < outline.Length; j++)
                    {
                        int a = offset + (r - 1) * outline.Length + j;
                        int b = offset + (r - 1) * outline.Length + (j + 1) % outline.Length;
                        int c = a + outline.Length, d = b + outline.Length;
                        triangles.AddRange(side == 0 ? new[] { a, c, d, a, d, b } : new[] { a, d, c, a, b, d });
                    }
                }
            }
            return Build(vertices, triangles);
        }

        private static Mesh Plate(Vector2[] outline)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            Vector2 center = Vector2.zero;
            foreach (var p in outline) center += p;
            center /= outline.Length;
            // Inset each tile on the same dome. Narrow exposed shell strips form seams.
            for (int i = 0; i < outline.Length; i++) outline[i] = Vector2.Lerp(center, outline[i], .94f);
            Vector3 Project(Vector2 p) => new Vector3(p.x, Mathf.Sqrt(Mathf.Max(0, .25f - p.sqrMagnitude)) + .005f, p.y);
            for (int edge = 0; edge < outline.Length; edge++)
            {
                const int steps = 6;
                int start = vertices.Count;
                for (int r = 0; r <= steps; r++)
                    for (int s = 0; s <= steps; s++)
                        vertices.Add(Project(Vector2.Lerp(center, Vector2.Lerp(outline[edge], outline[(edge + 1) % outline.Length], s / (float)steps), r / (float)steps)));
                for (int r = 0; r < steps; r++) for (int s = 0; s < steps; s++)
                {
                    int a = start + r * (steps + 1) + s, b = a + steps + 1;
                    // Determine winding from the actual projected face; every plate faces outward.
                    bool up = Vector3.Cross(vertices[b] - vertices[a], vertices[b + 1] - vertices[a]).y > 0;
                    triangles.AddRange(up ? new[] { a, b, b + 1, a, b + 1, a + 1 } : new[] { a, b + 1, b, a, a + 1, b + 1 });
                }
            }
            var mesh = Build(vertices, triangles);
            var normals = new Vector3[vertices.Count];
            for (int i = 0; i < normals.Length; i++) normals[i] = new Vector3(vertices[i].x, vertices[i].y - .005f, vertices[i].z).normalized;
            mesh.normals = normals;
            return mesh;
        }
    }
}
