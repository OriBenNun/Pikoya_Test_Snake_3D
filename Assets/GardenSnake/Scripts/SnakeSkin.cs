using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace GardenSnake
{
    /// <summary>A continuous surface over the controller's visual poses; game cells remain authoritative.</summary>
    public sealed class SnakeSkin : MonoBehaviour
    {
        /// <summary>How much bigger each part is drawn than the shared centerline width of 1.</summary>
        public readonly struct SegmentScales
        {
            public readonly float Head;
            public readonly float Body;
            public readonly float Tail;

            public SegmentScales(float head, float body, float tail)
            {
                Head = head;
                Body = body;
                Tail = tail;
            }
        }

        /// <summary>The swallowed apples the skin should bulge around this frame.</summary>
        public readonly struct Digestion
        {
            /// <summary>Each apple's travel along the body, measured in body indices.</summary>
            public readonly IReadOnlyList<float> Progress;
            /// <summary>Where each apple was swallowed, in world space, so the lump stays put.</summary>
            public readonly IReadOnlyList<Vector3> Anchors;
            /// <summary>How far into the current movement step the snake is, 0 to 1.</summary>
            public readonly float StepBlend;
            /// <summary>Fades every lump out together, so a dying snake stops digesting.</summary>
            public readonly float Visibility;
            /// <summary>The last entry is a lump settling into the tail cell that grew this step.</summary>
            public readonly bool Finishing;

            public Digestion(IReadOnlyList<float> progress, IReadOnlyList<Vector3> anchors,
                float stepBlend, float visibility, bool finishing)
            {
                Progress = progress;
                Anchors = anchors;
                StepBlend = stepBlend;
                Visibility = visibility;
                Finishing = finishing;
            }
        }

        /// <summary>One ring of the extruded tube: where it sits, which way it points, how wide it is.</summary>
        private struct SurfaceFrame
        {
            public Vector3 center;
            public Vector3 forward;
            public Vector3 side;
            public float width;
            public float slope;
        }

        private static readonly ProfilerMarker DrawMarker = new("GardenSnake.Skin");

        // An oval marking is a disc of SpotRings rings, each sampled SpotSteps times around.
        private const int SpotRings = 4;
        private const int SpotSteps = 32;
        private const int SpotSamplesPerRing = SpotSteps + 1;
        private const int SpotSampleCount = (SpotRings + 1) * SpotSamplesPerRing;

        [SerializeField] private SnakeSkinSettings settings;
        private bool ownsSettings;

        private int sides;
        private int samplesPerCell;
        private Vector2[] ringProfile;
        private SurfaceFrame[] surfaceFrames;
        private readonly Vector3[] spotSamples = new Vector3[SpotSampleCount];
        private float cachedSpotLength = -1;
        private float cachedSpotArc = -1;

        private Matrix4x4 worldToLocal;
        private Quaternion inverseRotation;
        private int topologyCount;
        private float topologyBellyArc = -1;
        private readonly List<Vector3> vertices = new();
        private readonly List<Vector3> normals = new();
        private readonly List<int> topTriangles = new();
        private readonly List<int> bellyTriangles = new();
        private readonly List<int> spotTriangles = new();
        private Mesh mesh;
        private MeshRenderer skinRenderer;

        private Vector3[] centerline;
        private float[] centerlineWidths;
        private int pointCount;
        private IReadOnlyList<float> digestion;
        private IReadOnlyList<Vector3> digestionAnchors;
        private bool finishingDigestion;
        private float digestionBlend;
        private float digestionVisibility;

        public void Initialize(GameObject source, int capacity, SnakeSkinSettings tuning = null)
        {
            if (tuning != null) settings = tuning;
            if (settings == null) { settings = ScriptableObject.CreateInstance<SnakeSkinSettings>(); ownsSettings = true; }
            sides = Mathf.Clamp(settings.Sides, 8, 40);
            samplesPerCell = Mathf.Clamp(settings.SamplesPerCell, 3, 24);
            ringProfile = new Vector2[sides + 1];
            centerline = new Vector3[capacity + 1];
            centerlineWidths = new float[capacity + 1];
            surfaceFrames = new SurfaceFrame[capacity * samplesPerCell + 1];
            for (int s = 0; s <= sides; s++)
            {
                float angle = s * Mathf.PI * 2 / sides;
                ringProfile[s] = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
            }
            CacheSpotSamples();
            int maxVertices = (capacity * samplesPerCell + 1) * (sides + 1) + capacity * SpotSampleCount;
            vertices.Capacity = normals.Capacity = maxVertices;
            topTriangles.Capacity = bellyTriangles.Capacity = maxVertices * 3;
            spotTriangles.Capacity = capacity * SpotRings * SpotSteps * 6;

            Material sourceBody = null, sourceBelly = null, sourceSpot = null;
            foreach (var renderer in source.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.name == "Body") sourceBody = renderer.sharedMaterial;
                if (renderer.name == "Belly") sourceBelly = renderer.sharedMaterial;
                if (renderer.name == "Dorsal spot") sourceSpot = renderer.sharedMaterial;
            }
            mesh = new Mesh { name = "Continuous snake skin", indexFormat = IndexFormat.UInt32 };
            mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            skinRenderer = gameObject.AddComponent<MeshRenderer>();
            skinRenderer.sharedMaterials = new[]
            {
                settings.SkinMaterial != null ? settings.SkinMaterial : sourceBody,
                settings.BellyMaterial != null ? settings.BellyMaterial : sourceBelly,
                settings.SpotMaterial != null ? settings.SpotMaterial : sourceSpot
            };
        }

        public void Draw(IReadOnlyList<Transform> poses, Transform tail, int bodyCount,
            SegmentScales scales, Digestion state)
        {
            using var profileScope = DrawMarker.Auto();
            CacheSpotSamples();
            digestion = state.Progress;
            digestionAnchors = state.Anchors;
            finishingDigestion = state.Finishing;
            digestionBlend = state.StepBlend;
            digestionVisibility = state.Visibility;
            pointCount = bodyCount + 1;
            worldToLocal = transform.worldToLocalMatrix;
            inverseRotation = Quaternion.Inverse(transform.rotation);
            bool rebuildTopology = topologyCount != bodyCount || !Mathf.Approximately(topologyBellyArc, settings.BellyArc);

            SampleCenterline(poses, tail, bodyCount, scales);
            BuildTube(rebuildTopology, scales.Body);
            BuildMarkings(rebuildTopology, bodyCount, scales.Body);
            UploadMesh(rebuildTopology, bodyCount);
        }

        /// <summary>Reads this frame's poses into a centerline of world points and relative widths.</summary>
        private void SampleCenterline(IReadOnlyList<Transform> poses, Transform tail, int bodyCount, SegmentScales scales)
        {
            for (int i = 0; i < bodyCount; i++)
            {
                Transform pose = i == bodyCount - 1 ? tail : poses[i];
                centerline[i] = pose.position;
                centerlineWidths[i] = pose.localScale.x /
                    (i == bodyCount - 1 ? scales.Tail : i == 0 ? scales.Head : scales.Body);
            }
            Vector3 tipDirection = centerline[bodyCount - 1] - centerline[bodyCount - 2];
            float tailSpan = tipDirection.magnitude;
            if (bodyCount > 2 && tailSpan < .5f)
            {
                Vector3 previousDirection = centerline[bodyCount - 1] - centerline[bodyCount - 3];
                tipDirection = Vector3.Lerp(previousDirection.normalized, tipDirection.normalized,
                    Mathf.SmoothStep(0, 1, tailSpan / .5f));
            }
            // Grow the new shoulder over the whole movement step. Expanding it before
            // this span reaches full length makes a steep ridge near the tail tip.
            if (bodyCount > 2 && tailSpan < 1)
                centerlineWidths[bodyCount - 2] *= Mathf.Lerp(settings.TailWidth, 1, Mathf.SmoothStep(0, 1, tailSpan));
            if (tipDirection.sqrMagnitude < .0001f) tipDirection = -tail.forward;
            centerline[bodyCount] = centerline[bodyCount - 1] + tipDirection.normalized * settings.TailTipLength;
            centerlineWidths[bodyCount] = 0;
            // Hide the neck's open end inside the head, and taper the final cell to a single tip.
            centerlineWidths[0] *= settings.NeckWidth;
            centerlineWidths[bodyCount - 1] *= settings.TailWidth;
            skinRenderer.enabled = centerlineWidths[0] > .001f || centerlineWidths[bodyCount - 1] > .001f;
        }

        private void BuildTube(bool rebuildTopology, float bodyScale)
        {
            vertices.Clear();
            normals.Clear();
            if (rebuildTopology) { topTriangles.Clear(); bellyTriangles.Clear(); spotTriangles.Clear(); }
            int rings = (pointCount - 1) * samplesPerCell + 1;
            // Centerline and digestion cost scales with rings, not rings multiplied by 21 vertices.
            for (int ring = 0; ring < rings; ring++)
                surfaceFrames[ring] = EvaluateFrame(ring / (float)samplesPerCell);
            for (int ring = 0; ring < rings; ring++)
            {
                for (int s = 0; s <= sides; s++)
                    Surface(surfaceFrames[ring], ringProfile[s].x, ringProfile[s].y, bodyScale, 0);
                if (ring == 0 || !rebuildTopology) continue;
                for (int s = 0; s < sides; s++)
                {
                    int a = (ring - 1) * (sides + 1) + s, b = a + sides + 1;
                    var triangles = Mathf.Abs((s + .5f) / sides - .5f) < settings.BellyArc * .5f
                        ? bellyTriangles
                        : topTriangles;
                    triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                    triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
                }
            }
        }

        /// <summary>Surface-following oval markings retain the character's original palette.</summary>
        private void BuildMarkings(bool rebuildTopology, int bodyCount, float bodyScale)
        {
            for (int i = 1; i < bodyCount; i++)
            {
                int start = vertices.Count;
                for (int ring = 0; ring <= SpotRings; ring++)
                for (int s = 0; s <= SpotSteps; s++)
                {
                    Vector3 sample = spotSamples[ring * SpotSamplesPerRing + s];
                    Surface(InterpolateFrame(i + sample.x), sample.y, sample.z, bodyScale, settings.SpotSurfaceOffset);
                    if (ring == 0 || s == SpotSteps || !rebuildTopology) continue;
                    int a = start + (ring - 1) * SpotSamplesPerRing + s, b = a + SpotSamplesPerRing;
                    spotTriangles.Add(a); spotTriangles.Add(b); spotTriangles.Add(b + 1);
                    spotTriangles.Add(a); spotTriangles.Add(b + 1); spotTriangles.Add(a + 1);
                }
            }
        }

        private void UploadMesh(bool rebuildTopology, int bodyCount)
        {
            if (rebuildTopology) mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            if (rebuildTopology)
            {
                mesh.subMeshCount = 3;
                mesh.SetTriangles(topTriangles, 0, false);
                mesh.SetTriangles(bellyTriangles, 1, false);
                mesh.SetTriangles(spotTriangles, 2, false);
                topologyCount = bodyCount;
                topologyBellyArc = settings.BellyArc;
            }
            mesh.RecalculateBounds();
        }

        private void CacheSpotSamples()
        {
            if (Mathf.Approximately(cachedSpotLength, settings.SpotLength) &&
                Mathf.Approximately(cachedSpotArc, settings.SpotArc)) return;
            cachedSpotLength = settings.SpotLength;
            cachedSpotArc = settings.SpotArc;
            for (int ring = 0; ring <= SpotRings; ring++)
            for (int s = 0; s <= SpotSteps; s++)
            {
                float radius = Mathf.Max(.001f, ring / (float)SpotRings);
                float angle = s * Mathf.PI * 2 / SpotSteps;
                float theta = Mathf.Sin(angle) * settings.SpotArc * radius;
                spotSamples[ring * SpotSamplesPerRing + s] =
                    new Vector3(Mathf.Cos(angle) * settings.SpotLength * radius, Mathf.Sin(theta), Mathf.Cos(theta));
            }
        }

        /// <summary>Builds the ring at centerline parameter <paramref name="u"/>, measured in body indices.</summary>
        private SurfaceFrame EvaluateFrame(float u)
        {
            u = Mathf.Clamp(u, 0, pointCount - 1);
            int i = Mathf.Min(pointCount - 2, Mathf.FloorToInt(u));
            float t = u - i;
            Vector3 center = Center(u);
            float before = Mathf.Max(0, u - .025f), after = Mathf.Min(pointCount - 1, u + .025f);
            Vector3 forward = (Center(after) - Center(before)) / Mathf.Max(.001f, after - before);
            forward.y = 0;
            if (forward.sqrMagnitude < .0001f) forward = Vector3.back;
            Vector3 side = Vector3.Cross(Vector3.up, forward.normalized);
            float width = Mathf.Lerp(centerlineWidths[i], centerlineWidths[i + 1], Mathf.SmoothStep(0, 1, t));
            float widthSlope = (centerlineWidths[i + 1] - centerlineWidths[i]) * 6 * t * (1 - t);
            // Small rounded shoulders distinguish each body part without breaking the skin.
            float bumpEnvelope = Mathf.SmoothStep(0, 1, u);
            float bumpEnvelopeSlope = u < 1 ? 6 * u * (1 - u) : 0;
            float bumpWave = .5f + .5f * Mathf.Cos(u * Mathf.PI * 2);
            float bump = settings.SegmentBump * bumpEnvelope * bumpWave;
            float bumpSlope = settings.SegmentBump *
                (bumpEnvelopeSlope * bumpWave - bumpEnvelope * Mathf.PI * Mathf.Sin(u * Mathf.PI * 2));
            widthSlope = widthSlope * (1 + bump) + width * bumpSlope;
            width *= 1 + bump;

            float bulge = 0, bulgeSlope = 0;
            EvaluateBulge(u, center, forward, ref bulge, ref bulgeSlope);
            float strength = settings.BellyBulge * digestionVisibility;
            widthSlope = widthSlope * (1 + bulge * strength) + width * bulgeSlope * strength;
            width *= 1 + bulge * strength;
            return new SurfaceFrame { center = center, forward = forward, side = side, width = width, slope = widthSlope };
        }

        /// <summary>
        /// Food defines a stationary field in board space. Each passing body part swells at that
        /// same location, including when the centerline rounds a turn.
        /// </summary>
        private void EvaluateBulge(float u, Vector3 center, Vector3 forward, ref float bulge, ref float bulgeSlope)
        {
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
                bulgeSlope = -.5f * Mathf.PI / halfLength * Mathf.Sin(distance * Mathf.PI) *
                    Vector3.Dot(offset.normalized, forward) * entrance;
            }
        }

        private SurfaceFrame InterpolateFrame(float u)
        {
            float sample = Mathf.Clamp(u, 0, pointCount - 1) * samplesPerCell;
            int i = Mathf.Min((pointCount - 1) * samplesPerCell - 1, Mathf.FloorToInt(sample));
            float t = sample - i;
            SurfaceFrame a = surfaceFrames[i], b = surfaceFrames[i + 1];
            return new SurfaceFrame
            {
                center = Vector3.LerpUnclamped(a.center, b.center, t),
                forward = Vector3.LerpUnclamped(a.forward, b.forward, t),
                side = Vector3.LerpUnclamped(a.side, b.side, t),
                width = Mathf.LerpUnclamped(a.width, b.width, t),
                slope = Mathf.LerpUnclamped(a.slope, b.slope, t)
            };
        }

        /// <summary>Emits one vertex of the elliptical cross section, lifted clear of the surface by an offset.</summary>
        private void Surface(SurfaceFrame frame, float profileSin, float profileCos, float scale, float surfaceOffset)
        {
            Vector3 center = frame.center, side = frame.side;
            float width = frame.width;
            float radius = settings.Radius * scale * width;
            float height = settings.Height * scale * width;
            center += Vector3.up * (settings.CenterHeight * scale * width);
            // Include the rising and falling profile in the lighting, so lumps read as round apples.
            Vector3 tangent = frame.forward + scale * frame.slope *
                (side * (settings.Radius * profileSin) + Vector3.up * (settings.CenterHeight + settings.Height * profileCos));
            Vector3 around = side * (settings.Radius * profileCos) - Vector3.up * (settings.Height * profileSin);
            Vector3 normal = Vector3.Cross(tangent, around).normalized;
            Vector3 vertex = center + side * (profileSin * radius) + Vector3.up * (profileCos * height) +
                normal * surfaceOffset * width;
            vertices.Add(worldToLocal.MultiplyPoint3x4(vertex));
            normals.Add(inverseRotation * normal);
        }

        private Vector3 Center(float u)
        {
            Vector3 center = RawCenter(u);
            for (int apple = 0; apple < digestion.Count; apple++)
            {
                // The growing tail already rests on the pickup. Pulling its collapsing
                // neighbor back toward it would fold the newly forming shoulder.
                if (finishingDigestion && apple == digestion.Count - 1) continue;
                float travel = Mathf.Clamp(digestion[apple] + digestionBlend - 1, 0, pointCount - 1);
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
            int i = Mathf.Min(pointCount - 2, Mathf.FloorToInt(u));
            float t = u - i;
            Vector3 a = centerline[Mathf.Max(0, i - 1)], b = centerline[i];
            Vector3 c = centerline[i + 1], d = centerline[Mathf.Min(pointCount - 1, i + 2)];
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
