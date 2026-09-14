using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GardenSnake
{
    /// <summary>A continuous surface over the controller's visual poses; game cells remain authoritative.</summary>
    public sealed class SnakeSkin : MonoBehaviour
    {
        private static readonly Unity.Profiling.ProfilerMarker DrawMarker = new Unity.Profiling.ProfilerMarker("GardenSnake.Skin");
        [SerializeField] private SnakeSkinSettings settings;
        private bool ownsSettings;
        private int Sides, SamplesPerCell;
        private struct SurfaceFrame
        {
            public Vector3 center, forward, side;
            public float width, slope;
        }
        private SurfaceFrame[] surfaceFrames;
        private Vector2[] circle;
        private readonly Vector3[] spotSamples = new Vector3[165];
        private float cachedSpotLength = -1, cachedSpotArc = -1;
        private Matrix4x4 worldToLocal;
        private Quaternion inverseRotation;
        private bool localIsWorld;
        private float shapeRadius, shapeHeight, shapeCenterHeight;
        private Vector3 boundsMin, boundsMax;
        private int topologyCount;
        private int topologyRings;
        private float topologyBellyArc = -1;
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
        private IReadOnlyList<Vector3> digestionAnchors;
        private bool finishingDigestion;
        private float digestionBlend;
        private float digestionVisibility;

        public void Initialize(GameObject source, int capacity, SnakeSkinSettings tuning = null)
        {
            if (tuning != null) settings = tuning;
            if (settings == null) { settings = ScriptableObject.CreateInstance<SnakeSkinSettings>(); ownsSettings = true; }
            Sides = Mathf.Clamp(settings.Sides, 8, 40);
            SamplesPerCell = Mathf.Clamp(settings.SamplesPerCell, 3, 24);
            circle = new Vector2[Sides + 1];
            points = new Vector3[capacity + 1]; widths = new float[capacity + 1];
            surfaceFrames = new SurfaceFrame[capacity * SamplesPerCell + 1];
            for (int s = 0; s <= Sides; s++)
            {
                float angle = s * Mathf.PI * 2 / Sides;
                circle[s] = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
            }
            CacheSpotSamples();
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
            skinRenderer.sharedMaterials = new[] { settings.SkinMaterial != null ? settings.SkinMaterial : skin, settings.BellyMaterial != null ? settings.BellyMaterial : cream, settings.SpotMaterial != null ? settings.SpotMaterial : spot };
        }

        public void Draw(IReadOnlyList<Transform> poses, Transform tail, int bodyCount, float bodyScale, float headScale, float tailScale,
            IReadOnlyList<float> swallowedApples, IReadOnlyList<Vector3> appleAnchors, float stepBlend, float bulgeVisibility, bool finishingApple = false)
        {
            using var profileScope = DrawMarker.Auto();
            CacheSpotSamples();
            digestion = swallowedApples;
            digestionAnchors = appleAnchors;
            finishingDigestion = finishingApple;
            digestionBlend = stepBlend;
            digestionVisibility = bulgeVisibility;
            count = bodyCount + 1;
            worldToLocal = transform.worldToLocalMatrix;
            inverseRotation = Quaternion.Inverse(transform.rotation);
            // Read the profile once. Surface() runs tens of thousands of times a frame, and a
            // property call per vertex is a property call forty thousand times over.
            shapeRadius = settings.Radius;
            shapeHeight = settings.Height;
            shapeCenterHeight = settings.CenterHeight;
            // The skin object sits at the origin unturned, so the usual case is no transform
            // at all. Checking once beats a matrix multiply and a rotation per vertex.
            localIsWorld = worldToLocal.isIdentity;
            bool rebuildTopology = topologyCount != bodyCount || !Mathf.Approximately(topologyBellyArc, settings.BellyArc);
            for (int i = 0; i < bodyCount; i++)
            {
                Transform pose = i == bodyCount - 1 ? tail : poses[i];
                points[i] = pose.position;
                widths[i] = pose.localScale.x / (i == bodyCount - 1 ? tailScale : i == 0 ? headScale : bodyScale);
            }
            Vector3 end = points[bodyCount - 1] - points[bodyCount - 2];
            float tailSpan = end.magnitude;
            if (bodyCount > 2 && tailSpan < .5f)
            {
                Vector3 previousEnd = points[bodyCount - 1] - points[bodyCount - 3];
                end = Vector3.Lerp(previousEnd.normalized, end.normalized, Mathf.SmoothStep(0, 1, tailSpan / .5f));
            }
            // Grow the new shoulder over the whole movement step. Expanding it before
            // this span reaches full length makes a steep ridge near the tail tip.
            if (bodyCount > 2 && tailSpan < 1)
                widths[bodyCount - 2] *= Mathf.Lerp(settings.TailWidth, 1, Mathf.SmoothStep(0, 1, tailSpan));
            if (end.sqrMagnitude < .0001f) end = -tail.forward;
            points[bodyCount] = points[bodyCount - 1] + end.normalized * settings.TailTipLength;
            widths[bodyCount] = 0;
            // Hide the neck's open end inside the head, and taper the final cell to a single tip.
            widths[0] *= settings.NeckWidth;
            widths[bodyCount - 1] *= settings.TailWidth;
            bool visible = widths[0] > .001f || widths[bodyCount - 1] > .001f;
            if (skinRenderer.enabled != visible) skinRenderer.enabled = visible;
            vertices.Clear(); normals.Clear();
            int rings = (count - 1) * SamplesPerCell + 1;
            // A ring's triangles are the same whatever else the body is doing, so the tube's
            // indices only ever need extending. Rebuilding all of them on every apple was the
            // spike the player felt when the snake grew.
            bool rebuildTube = rebuildTopology && (rings < topologyRings || !Mathf.Approximately(topologyBellyArc, settings.BellyArc));
            if (rebuildTube) { top.Clear(); belly.Clear(); topologyRings = 0; }
            if (rebuildTopology) markings.Clear();
            int firstNewRing = Mathf.Max(1, topologyRings);
            // Centerline and digestion cost scales with rings, not rings multiplied by 21 vertices.
            // The bounds come from the same pass: every vertex sits within its ring's profile,
            // so a box around the centreline padded by that profile contains the whole skin.
            boundsMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            boundsMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            float reach = Mathf.Max(shapeRadius, shapeCenterHeight + shapeHeight) * bodyScale + settings.SpotSurfaceOffset;
            for (int ring = 0; ring < rings; ring++)
            {
                SurfaceFrame frame = EvaluateFrame(ring / (float)SamplesPerCell);
                surfaceFrames[ring] = frame;
                float pad = reach * Mathf.Max(1f, frame.width);
                Vector3 c = frame.center;
                if (c.x - pad < boundsMin.x) boundsMin.x = c.x - pad;
                if (c.y - pad < boundsMin.y) boundsMin.y = c.y - pad;
                if (c.z - pad < boundsMin.z) boundsMin.z = c.z - pad;
                if (c.x + pad > boundsMax.x) boundsMax.x = c.x + pad;
                if (c.y + pad > boundsMax.y) boundsMax.y = c.y + pad;
                if (c.z + pad > boundsMax.z) boundsMax.z = c.z + pad;
            }
            for (int ring = 0; ring < rings; ring++)
            {
                for (int s = 0; s <= Sides; s++)
                    Surface(surfaceFrames[ring], circle[s].x, circle[s].y, bodyScale, 0);
                if (ring == 0 || ring < firstNewRing) continue;
                for (int s = 0; s < Sides; s++)
                {
                    int a = (ring - 1) * (Sides + 1) + s, b = a + Sides + 1;
                    var triangles = Mathf.Abs((s + .5f) / Sides - .5f) < settings.BellyArc * .5f ? belly : top;
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
                        Surface(InterpolateFrame(i + sample.x), sample.y, sample.z, bodyScale, settings.SpotSurfaceOffset);
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
                topologyRings = rings;
                topologyBellyArc = settings.BellyArc;
            }
            // Walking every vertex again just to find the box is the single most expensive
            // thing left in the draw, and the ring pass already knows the answer.
            Vector3 centre = (boundsMin + boundsMax) * .5f;
            if (!localIsWorld) centre = worldToLocal.MultiplyPoint3x4(centre);
            mesh.bounds = new Bounds(centre, boundsMax - boundsMin);
        }

        private void CacheSpotSamples()
        {
            if (Mathf.Approximately(cachedSpotLength, settings.SpotLength) && Mathf.Approximately(cachedSpotArc, settings.SpotArc)) return;
            cachedSpotLength = settings.SpotLength;
            cachedSpotArc = settings.SpotArc;
            for (int ring = 0; ring <= 4; ring++)
            for (int s = 0; s <= 32; s++)
            {
                float radius = Mathf.Max(.001f, ring / 4f);
                float angle = s * Mathf.PI * 2 / 32;
                float theta = Mathf.Sin(angle) * settings.SpotArc * radius;
                spotSamples[ring * 33 + s] = new Vector3(Mathf.Cos(angle) * settings.SpotLength * radius, Mathf.Sin(theta), Mathf.Cos(theta));
            }
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
            // Small rounded shoulders distinguish each body part without breaking the skin.
            float bumpEnvelope = Mathf.SmoothStep(0, 1, u);
            float bumpEnvelopeSlope = u < 1 ? 6 * u * (1 - u) : 0;
            float bumpWave = .5f + .5f * Mathf.Cos(u * Mathf.PI * 2);
            float bump = settings.SegmentBump * bumpEnvelope * bumpWave;
            float bumpSlope = settings.SegmentBump * (bumpEnvelopeSlope * bumpWave - bumpEnvelope * Mathf.PI * Mathf.Sin(u * Mathf.PI * 2));
            widthSlope = widthSlope * (1 + bump) + width * bumpSlope;
            width *= 1 + bump;
            // Food defines a stationary field in board space. Each passing body part
            // swells at that same location, including when the centerline rounds a turn.
            float bulge = 0, bulgeSlope = 0;
            for (int apple = 0; apple < digestion.Count; apple++)
            {
                float travel = Mathf.Max(0, digestion[apple] + digestionBlend - 1);
                // A nearby parallel stretch must not inherit another stretch's apple.
                if (Mathf.Abs(u - travel) > 2) continue;
                Vector3 offset = center - digestionAnchors[apple];
                offset.y = 0;
                float halfLength = Mathf.Max(.05f, settings.BulgeHalfLength);
                float distance = offset.magnitude / halfLength;
                if (distance >= 1) continue;
                float entrance = Mathf.SmoothStep(0, 1, travel / Mathf.Max(.05f, settings.BulgeEntranceLength));
                if (finishingDigestion && apple == digestion.Count - 1)
                    entrance *= 1 - Mathf.SmoothStep(0, 1, digestionBlend);
                float round = (.5f + .5f * Mathf.Cos(distance * Mathf.PI)) * entrance;
                if (round <= bulge) continue;
                bulge = round;
                bulgeSlope = -.5f * Mathf.PI / halfLength * Mathf.Sin(distance * Mathf.PI) * Vector3.Dot(offset.normalized, forward) * entrance;
            }
            widthSlope = widthSlope * (1 + bulge * settings.BellyBulge * digestionVisibility) + width * bulgeSlope * settings.BellyBulge * digestionVisibility;
            width *= 1 + bulge * settings.BellyBulge * digestionVisibility;
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

        /// <summary>
        /// One vertex of the tube, and its normal. This is the whole cost of the skin: it runs
        /// once per side per ring, plus once per sample of every marking, so it is written out in
        /// floats rather than Vector3 operators. The maths is unchanged.
        /// <para>The centreline's forward and side both lie flat, so their Y is always zero.</para>
        /// </summary>
        private void Surface(in SurfaceFrame frame, float sin, float cos, float size, float offset)
        {
            float width = frame.width;
            float scaled = size * width;
            float radius = shapeRadius * scaled;
            float height = shapeHeight * scaled;
            float sideX = frame.side.x, sideZ = frame.side.z;
            float lean = size * frame.slope;

            // Include the rising and falling profile in the lighting, so lumps read as round apples.
            float radialSin = shapeRadius * sin;
            float tangentX = frame.forward.x + lean * sideX * radialSin;
            float tangentY = lean * (shapeCenterHeight + shapeHeight * cos);
            float tangentZ = frame.forward.z + lean * sideZ * radialSin;

            float radialCos = shapeRadius * cos;
            float aroundX = sideX * radialCos;
            float aroundY = -shapeHeight * sin;
            float aroundZ = sideZ * radialCos;

            float nx = tangentY * aroundZ - tangentZ * aroundY;
            float ny = tangentZ * aroundX - tangentX * aroundZ;
            float nz = tangentX * aroundY - tangentY * aroundX;
            // Vector3.normalized collapses to zero below this magnitude; match it exactly.
            float length = Mathf.Sqrt(nx * nx + ny * ny + nz * nz);
            if (length > 1e-5f) { float inv = 1f / length; nx *= inv; ny *= inv; nz *= inv; }
            else { nx = ny = nz = 0f; }

            float push = offset * width;
            float vx = frame.center.x + sideX * (sin * radius) + nx * push;
            float vy = frame.center.y + shapeCenterHeight * scaled + cos * height + ny * push;
            float vz = frame.center.z + sideZ * (sin * radius) + nz * push;

            var vertex = new Vector3(vx, vy, vz);
            var normal = new Vector3(nx, ny, nz);
            vertices.Add(localIsWorld ? vertex : worldToLocal.MultiplyPoint3x4(vertex));
            normals.Add(localIsWorld ? normal : inverseRotation * normal);
        }

        private Vector3 Center(float u)
        {
            Vector3 center = RawCenter(u);
            for (int apple = 0; apple < digestion.Count; apple++)
            {
                // The growing tail already rests on the pickup. Pulling its collapsing
                // neighbor back toward it would fold the newly forming shoulder.
                if (finishingDigestion && apple == digestion.Count - 1) continue;
                float travel = Mathf.Clamp(digestion[apple] + digestionBlend - 1, 0, count - 1);
                float distance = Mathf.Abs(u - travel) / Mathf.Max(.05f, settings.BulgeHalfLength);
                if (distance >= 1) continue;
                // Rounded corners cut inside the grid path. Keep the swallowed apple
                // pinned while the surrounding skin bends smoothly around its position.
                float pin = Mathf.SmoothStep(0, 1, travel) * (.5f + .5f * Mathf.Cos(distance * Mathf.PI));
                Vector3 correction = digestionAnchors[apple] - RawCenter(travel);
                correction.y = 0;
                center += correction * pin * digestionVisibility;
            }
            return center;
        }

        private Vector3 RawCenter(float u)
        {
            int i = Mathf.Min(count - 2, Mathf.FloorToInt(u));
            float t = u - i;
            Vector3 a = points[Mathf.Max(0, i - 1)], b = points[i], c = points[i + 1], d = points[Mathf.Min(count - 1, i + 2)];
            // Restrained Hermite tangents round corners without swinging into adjacent cells.
            Vector3 m0 = (c - a) * settings.CurveTension, m1 = (d - b) * settings.CurveTension;
            // A new tail cell initially shares its neighbor's old position. Prevent spline
            // tangents from folding that short span back through the skin during growth.
            float span = Vector3.Distance(b, c);
            m0 = Vector3.ClampMagnitude(m0, span);
            m1 = Vector3.ClampMagnitude(m1, span);
            return (2 * t * t * t - 3 * t * t + 1) * b + (t * t * t - 2 * t * t + t) * m0 +
                (-2 * t * t * t + 3 * t * t) * c + (t * t * t - t * t) * m1;
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (ownsSettings && settings != null) Destroy(settings);
        }
    }
}
