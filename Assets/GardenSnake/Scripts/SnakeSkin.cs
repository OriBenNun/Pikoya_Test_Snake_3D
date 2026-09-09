using System.Collections.Generic;
using GardenSnake.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace GardenSnake
{
    /// <summary>A continuous surface over the controller's visual poses; game cells remain authoritative.</summary>
    public sealed class SnakeSkin : MonoBehaviour
    {
        private static readonly Unity.Profiling.ProfilerMarker DrawMarker = new Unity.Profiling.ProfilerMarker("GardenSnake.Skin");
        private const int Sides = 20;
        private const int SamplesPerCell = 12;
        [SerializeField, Range(0f, .6f)] private float bellyBulge = .3f;
        private struct SurfaceFrame
        {
            public Vector3 center, forward, side;
            public float width, slope;
        }
        private SurfaceFrame[] surfaceFrames;
        private readonly Vector2[] circle = new Vector2[Sides + 1];
        private readonly Vector3[] spotSamples = new Vector3[165];
        private Matrix4x4 worldToLocal;
        private Quaternion inverseRotation;
        private int topologyCount;
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
        private IReadOnlyList<float> digestion;
        private float digestionBlend;
        private float digestionVisibility;

        public void Initialize(GameObject source, int capacity)
        {
            points = new Vector3[capacity + 1]; widths = new float[capacity + 1];
            surfaceFrames = new SurfaceFrame[capacity * SamplesPerCell + 1];
            for (int s = 0; s <= Sides; s++)
            {
                float angle = s * Mathf.PI * 2 / Sides;
                circle[s] = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
            }
            for (int ring = 0; ring <= 4; ring++)
            for (int s = 0; s <= 32; s++)
            {
                float radius = Mathf.Max(.001f, ring / 4f);
                float angle = s * Mathf.PI * 2 / 32;
                float theta = Mathf.Sin(angle) * .39f * radius;
                spotSamples[ring * 33 + s] = new Vector3(Mathf.Cos(angle) * .25f * radius, Mathf.Sin(theta), Mathf.Cos(theta));
            }
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

        public void Draw(IReadOnlyList<Transform> poses, Transform tail, int bodyCount, float bodyScale, float headScale, float tailScale,
            IReadOnlyList<float> swallowedApples, float stepBlend, float bulgeVisibility)
        {
            using var profileScope = DrawMarker.Auto();
            digestion = swallowedApples;
            digestionBlend = stepBlend;
            digestionVisibility = bulgeVisibility;
            count = bodyCount + 1;
            worldToLocal = transform.worldToLocalMatrix;
            inverseRotation = Quaternion.Inverse(transform.rotation);
            bool rebuildTopology = topologyCount != bodyCount;
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
            vertices.Clear(); normals.Clear();
            if (rebuildTopology) { top.Clear(); belly.Clear(); markings.Clear(); }
            int rings = (count - 1) * SamplesPerCell + 1;
            // Centerline and digestion cost scales with rings, not rings multiplied by 21 vertices.
            for (int ring = 0; ring < rings; ring++)
                surfaceFrames[ring] = EvaluateFrame(ring / (float)SamplesPerCell);
            for (int ring = 0; ring < rings; ring++)
            {
                for (int s = 0; s <= Sides; s++)
                    Surface(surfaceFrames[ring], circle[s].x, circle[s].y, bodyScale, 0);
                if (ring == 0 || !rebuildTopology) continue;
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
                    for (int s = 0; s <= steps; s++)
                    {
                        Vector3 sample = spotSamples[ring * 33 + s];
                        Surface(InterpolateFrame(i + sample.x), sample.y, sample.z, bodyScale, .018f);
                        if (ring == 0 || s == steps || !rebuildTopology) continue;
                        int a = start + (ring - 1) * (steps + 1) + s, b = a + steps + 1;
                        markings.Add(a); markings.Add(b); markings.Add(b + 1);
                        markings.Add(a); markings.Add(b + 1); markings.Add(a + 1);
                    }
                }
            }
            if (rebuildTopology) mesh.Clear();
            mesh.SetVertices(vertices); mesh.SetNormals(normals);
            if (rebuildTopology)
            {
                mesh.subMeshCount = 3;
                mesh.SetTriangles(top, 0, false); mesh.SetTriangles(belly, 1, false); mesh.SetTriangles(markings, 2, false);
                topologyCount = bodyCount;
            }
            mesh.RecalculateBounds();
        }

        private SurfaceFrame EvaluateFrame(float u)
        {
            u = Mathf.Clamp(u, 0, count - 1);
            int i = Mathf.Min(count - 2, Mathf.FloorToInt(u));
            float t = u - i;
            Vector3 center = Center(u);
            float before = Mathf.Max(0, u - .025f), after = Mathf.Min(count - 1, u + .025f);
            Vector3 forward = (Center(after) - Center(before)) / Mathf.Max(.001f, after - before);
            forward.y = 0;
            if (forward.sqrMagnitude < .0001f) forward = Vector3.back;
            Vector3 side = Vector3.Cross(Vector3.up, forward.normalized);
            float width = Mathf.Lerp(widths[i], widths[i + 1], Mathf.SmoothStep(0, 1, t));
            float widthSlope = (widths[i + 1] - widths[i]) * 6 * t * (1 - t);
            // A round lump follows the same curved centerline as the skin, even through turns.
            // Sample continuously instead of swelling whole cells, so the apple visibly rolls.
            float bulge = 0, bulgeSlope = 0;
            for (int apple = 0; apple < digestion.Count; apple++)
            {
                float travel = Mathf.Max(0, digestion[apple] + (digestionBlend - 1) * SnakeGame.DigestionPerStep);
                float distance = Mathf.Abs(u - travel) / .9f;
                if (distance >= 1) continue;
                float entrance = Mathf.SmoothStep(0, 1, travel / .4f);
                float round = (.5f + .5f * Mathf.Cos(distance * Mathf.PI)) * entrance;
                if (round <= bulge) continue;
                bulge = round;
                bulgeSlope = -.5f * Mathf.PI / .9f * Mathf.Sin(distance * Mathf.PI) * Mathf.Sign(u - travel) * entrance;
            }
            widthSlope = widthSlope * (1 + bulge * bellyBulge * digestionVisibility) + width * bulgeSlope * bellyBulge * digestionVisibility;
            width *= 1 + bulge * bellyBulge * digestionVisibility;
            return new SurfaceFrame { center = center, forward = forward, side = side, width = width, slope = widthSlope };
        }

        private SurfaceFrame InterpolateFrame(float u)
        {
            float sample = Mathf.Clamp(u, 0, count - 1) * SamplesPerCell;
            int i = Mathf.Min((count - 1) * SamplesPerCell - 1, Mathf.FloorToInt(sample));
            float t = sample - i;
            SurfaceFrame a = surfaceFrames[i], b = surfaceFrames[i + 1];
            return new SurfaceFrame {
                center = Vector3.LerpUnclamped(a.center, b.center, t),
                forward = Vector3.LerpUnclamped(a.forward, b.forward, t),
                side = Vector3.LerpUnclamped(a.side, b.side, t),
                width = Mathf.LerpUnclamped(a.width, b.width, t),
                slope = Mathf.LerpUnclamped(a.slope, b.slope, t)
            };
        }

        private void Surface(SurfaceFrame frame, float sin, float cos, float size, float offset)
        {
            Vector3 center = frame.center, side = frame.side;
            float width = frame.width;
            float radius = .335f * size * width;
            float height = .235f * size * width;
            center += Vector3.up * (.255f * size * width);
            // Include the rising and falling profile in the lighting, so lumps read as round apples.
            Vector3 tangent = frame.forward + size * frame.slope *
                (side * (.335f * sin) + Vector3.up * (.255f + .235f * cos));
            Vector3 around = side * (.335f * cos) - Vector3.up * (.235f * sin);
            Vector3 normal = Vector3.Cross(tangent, around).normalized;
            Vector3 vertex = center + side * (sin * radius) + Vector3.up * (cos * height) + normal * offset * width;
            vertices.Add(worldToLocal.MultiplyPoint3x4(vertex));
            normals.Add(inverseRotation * normal);
        }

        private Vector3 Center(float u)
        {
            int i = Mathf.Min(count - 2, Mathf.FloorToInt(u));
            float t = u - i;
            Vector3 a = points[Mathf.Max(0, i - 1)], b = points[i], c = points[i + 1], d = points[Mathf.Min(count - 1, i + 2)];
            // Restrained Hermite tangents round corners without swinging into adjacent cells.
            Vector3 m0 = (c - a) * .4f, m1 = (d - b) * .4f;
            // A new tail cell initially shares its neighbor's old position. Prevent spline
            // tangents from folding that short span back through the skin during growth.
            float span = Vector3.Distance(b, c);
            m0 = Vector3.ClampMagnitude(m0, span);
            m1 = Vector3.ClampMagnitude(m1, span);
            return (2 * t * t * t - 3 * t * t + 1) * b + (t * t * t - 2 * t * t + t) * m0 +
                (-2 * t * t * t + 3 * t * t) * c + (t * t * t - t * t) * m1;
        }

        private void OnDestroy() { if (mesh != null) Destroy(mesh); }
    }
}
