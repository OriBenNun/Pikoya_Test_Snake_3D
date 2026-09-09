using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GardenSnake
{
    /// <summary>A continuous surface over the controller's visual poses; game cells remain authoritative.</summary>
    public sealed class SnakeSkin : MonoBehaviour
    {
        private const int Sides = 20;
        private const int SamplesPerCell = 8;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<int> top = new List<int>();
        private readonly List<int> belly = new List<int>();
        private readonly List<int> markings = new List<int>();
        private Mesh mesh;
        private MeshRenderer skinRenderer;
        private Vector3[] points;
        private float[] widths;
        private int count;

        public void Initialize(GameObject source, int capacity)
        {
            points = new Vector3[capacity + 1]; widths = new float[capacity + 1];
            int maxVertices = (capacity * SamplesPerCell + 1) * (Sides + 1) + capacity * 165;
            vertices.Capacity = normals.Capacity = maxVertices;
            top.Capacity = belly.Capacity = maxVertices * 3;
            markings.Capacity = capacity * 4 * 32 * 6;
            Material skin = null, cream = null, spot = null;
            foreach (var renderer in source.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.name == "Body") skin = renderer.sharedMaterial;
                if (renderer.name == "Belly") cream = renderer.sharedMaterial;
                if (renderer.name == "Dorsal spot") spot = renderer.sharedMaterial;
            }
            mesh = new Mesh { name = "Continuous snake skin", indexFormat = IndexFormat.UInt32 };
            mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            skinRenderer = gameObject.AddComponent<MeshRenderer>();
            skinRenderer.sharedMaterials = new[] { skin, cream, spot };
        }

        public void Draw(IReadOnlyList<Transform> poses, Transform tail, int bodyCount, float bodyScale, float headScale, float tailScale)
        {
            count = bodyCount + 1;
            for (int i = 0; i < bodyCount; i++)
            {
                Transform pose = i == bodyCount - 1 ? tail : poses[i];
                points[i] = pose.position;
                widths[i] = pose.localScale.x / (i == bodyCount - 1 ? tailScale : i == 0 ? headScale : bodyScale);
            }
            Vector3 end = points[bodyCount - 1] - points[bodyCount - 2];
            if (end.sqrMagnitude < .0001f) end = -tail.forward;
            points[bodyCount] = points[bodyCount - 1] + end.normalized * .42f;
            widths[bodyCount] = 0;
            // Hide the neck's open end inside the head, and taper the final cell to a single tip.
            widths[0] *= .83f;
            widths[bodyCount - 1] *= .58f;
            skinRenderer.enabled = widths[0] > .001f || widths[bodyCount - 1] > .001f;
            vertices.Clear(); normals.Clear(); top.Clear(); belly.Clear(); markings.Clear();
            int rings = (count - 1) * SamplesPerCell + 1;
            for (int ring = 0; ring < rings; ring++)
            {
                float u = ring / (float)SamplesPerCell;
                for (int s = 0; s <= Sides; s++)
                    Surface(u, s * Mathf.PI * 2 / Sides, bodyScale, 0);
                if (ring == 0) continue;
                for (int s = 0; s < Sides; s++)
                {
                    int a = (ring - 1) * (Sides + 1) + s, b = a + Sides + 1;
                    var triangles = s >= 7 && s < 13 ? belly : top;
                    triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                    triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
                }
            }
            // Surface-following oval markings retain the character's original palette.
            for (int i = 1; i < bodyCount; i++)
            {
                int start = vertices.Count;
                const int steps = 32;
                for (int ring = 0; ring <= 4; ring++)
                {
                    float radius = Mathf.Max(.001f, ring / 4f);
                    for (int s = 0; s <= steps; s++)
                    {
                        float angle = s * Mathf.PI * 2 / steps;
                        Surface(i + Mathf.Cos(angle) * .25f * radius, Mathf.Sin(angle) * .39f * radius, bodyScale, .006f);
                        if (ring == 0 || s == steps) continue;
                        int a = start + (ring - 1) * (steps + 1) + s, b = a + steps + 1;
                        markings.Add(a); markings.Add(b); markings.Add(b + 1);
                        markings.Add(a); markings.Add(b + 1); markings.Add(a + 1);
                    }
                }
            }
            mesh.Clear(); mesh.SetVertices(vertices); mesh.SetNormals(normals);
            mesh.subMeshCount = 3;
            mesh.SetTriangles(top, 0, false); mesh.SetTriangles(belly, 1, false); mesh.SetTriangles(markings, 2, false);
            mesh.RecalculateBounds();
        }

        private void Surface(float u, float angle, float size, float offset)
        {
            u = Mathf.Clamp(u, 0, count - 1);
            int i = Mathf.Min(count - 2, Mathf.FloorToInt(u));
            float t = u - i;
            Vector3 center = Center(u);
            Vector3 forward = Center(Mathf.Min(count - 1, u + .025f)) - Center(Mathf.Max(0, u - .025f));
            forward.y = 0;
            if (forward.sqrMagnitude < .0001f) forward = Vector3.back;
            Vector3 side = Vector3.Cross(Vector3.up, forward.normalized);
            float width = Mathf.Lerp(widths[i], widths[i + 1], Mathf.SmoothStep(0, 1, t));
            float radius = .335f * size * width;
            float height = .235f * size * width;
            center += Vector3.up * (.255f * size * width);
            Vector3 normal = (side * Mathf.Sin(angle) / .335f + Vector3.up * Mathf.Cos(angle) / .235f).normalized;
            Vector3 vertex = center + side * (Mathf.Sin(angle) * radius) + Vector3.up * (Mathf.Cos(angle) * height) + normal * offset * width;
            vertices.Add(transform.InverseTransformPoint(vertex));
            normals.Add(transform.InverseTransformDirection(normal));
        }

        private Vector3 Center(float u)
        {
            int i = Mathf.Min(count - 2, Mathf.FloorToInt(u));
            float t = u - i;
            Vector3 a = points[Mathf.Max(0, i - 1)], b = points[i], c = points[i + 1], d = points[Mathf.Min(count - 1, i + 2)];
            // Restrained Hermite tangents round corners without swinging into adjacent cells.
            Vector3 m0 = (c - a) * .4f, m1 = (d - b) * .4f;
            return (2 * t * t * t - 3 * t * t + 1) * b + (t * t * t - 2 * t * t + t) * m0 +
                (-2 * t * t * t + 3 * t * t) * c + (t * t * t - t * t) * m1;
        }

        private void OnDestroy() { if (mesh != null) Destroy(mesh); }
    }
}
