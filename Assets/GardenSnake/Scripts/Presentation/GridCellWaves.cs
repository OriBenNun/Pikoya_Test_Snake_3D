using GardenSnake.Core;
using UnityEngine;

namespace GardenSnake
{
    /// <summary>
    /// Bounded, additive visual waves. Cell coordinates and collision never move.
    /// <para>
    /// It knows nothing about the run: whoever triggers a wave also tells it when the garden
    /// should hold still, so the board can be exercised on its own.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class GridCellWaves : MonoBehaviour
    {
        public enum Pattern { Ripple, Bloom, Sweep, CheckerHop }

        [System.Serializable]
        public struct Preset
        {
            public Pattern pattern;
            [Min(0)] public float amplitude;
            [Min(.1f)] public float speed;
            [Min(.1f)] public float duration;
            [Min(.1f)] public float width;
            public Preset(Pattern pattern, float amplitude, float speed, float duration, float width)
            { this.pattern = pattern; this.amplitude = amplitude; this.speed = speed; this.duration = duration; this.width = width; }
        }

        [System.Serializable]
        public sealed class Shape
        {
            [Range(0, 1)] public float rippleModulation = .35f;
            [Min(0)] public float rippleCycles = 2f;
            [Range(0, 1)] public float checkerAlternateHeight = .3f;
            [Min(0)] public float bloomFalloff = .035f;
            [Min(.1f)] public float envelopePower = 2f;
            [Tooltip("Sweep direction in board coordinates; normalized when sampled.")]
            public Vector2 sweepDirection = Vector2.one;
        }

        [Header("Board references (set before Play Mode)")]
        [SerializeField] private Transform[] cells;
        [SerializeField, Min(1)] private int columns = 21;
        [SerializeField, Range(1, 64), Tooltip("Maximum simultaneous waves. Applied on entering Play Mode.")]
        private int maximumConcurrentWaves = 8;
        [Header("Wave limits")]
        [SerializeField, Min(0)] private float maximumHeight = .48f;
        [SerializeField, Min(0)] private float maximumDepth = .08f;
        [SerializeField, Min(0)] private float maximumStrength = 2f;
        [SerializeField, Range(.016f, .1f)] private float maximumFrameStep = .05f;
        [Header("Presets (live tuning; new waves use current values)")]
        [SerializeField] private Preset ripple = new Preset(Pattern.Ripple, .25f, 13, .6f, 1.5f);
        [SerializeField] private Preset celebration = new Preset(Pattern.Bloom, .44f, 15, .85f, 2);
        [SerializeField] private Preset sweep = new Preset(Pattern.Sweep, .25f, 15, .6f, 2);
        [SerializeField] private Preset checker = new Preset(Pattern.CheckerHop, .22f, 12, .65f, 2);
        [SerializeField] private Shape shape = new Shape();
        private struct Wave { public Preset preset; public Vector2 source; public float age; }
        private Wave[] active;
        private static readonly Shape DefaultShape = new Shape();
        private Vector3[] rest;
        private int count;
        public int ActiveCount => count;
        public int CellCount => cells == null ? 0 : cells.Length;

        /// <summary>While set, live waves keep their age and the board stops moving.</summary>
        public bool Frozen { get; set; }

        private void Awake()
        {
            columns = Mathf.Max(1, columns);
            active = new Wave[Mathf.Clamp(maximumConcurrentWaves, 1, 64)];
            rest = new Vector3[CellCount];
            for (int i = 0; i < rest.Length; i++) if (cells[i] != null) rest[i] = cells[i].localPosition;
        }

        public void Play(Pattern pattern, Cell source, float strength = 1)
        {
            var preset = pattern == Pattern.Bloom ? celebration : pattern == Pattern.Sweep ? sweep
                : pattern == Pattern.CheckerHop ? checker : ripple;
            preset.amplitude *= Mathf.Clamp(strength, 0, maximumStrength);
            Play(preset, source);
        }

        public void Play(Preset preset, Cell source)
        {
            if (!isActiveAndEnabled || active == null || CellCount == 0) return;
            preset.amplitude = Mathf.Max(0, preset.amplitude);
            preset.speed = Mathf.Max(.1f, preset.speed);
            preset.duration = Mathf.Max(.1f, preset.duration);
            preset.width = Mathf.Max(.1f, preset.width);
            if (count == active.Length)
            {
                for (int i = 1; i < count; i++) active[i - 1] = active[i];
                count--;
            }
            active[count++] = new Wave { preset = preset, source = new Vector2(source.X, source.Y) };
        }

        [ContextMenu("Preview ripple from center")]
        private void PreviewRipple() => Play(Pattern.Ripple, new Cell(columns / 2, CellCount / columns / 2));
        [ContextMenu("Preview bloom from center")]
        private void PreviewBloom() => Play(Pattern.Bloom, new Cell(columns / 2, CellCount / columns / 2));
        [ContextMenu("Preview sweep from corner")]
        private void PreviewSweep() => Play(Pattern.Sweep, new Cell(0, 0));
        [ContextMenu("Preview checker hop")]
        private void PreviewChecker() => Play(Pattern.CheckerHop, new Cell(columns / 2, CellCount / columns / 2));

        public void Clear()
        {
            count = 0;
            if (rest == null) return;
            for (int i = 0; i < rest.Length; i++) if (cells[i] != null) cells[i].localPosition = rest[i];
        }

        private void Update()
        {
            if (count == 0 || rest == null || Frozen) return;
            for (int i = count - 1; i >= 0; i--)
            {
                // Preserve the source beat after a stalled frame instead of jumping past it.
                active[i].age += Mathf.Min(Time.unscaledDeltaTime, Mathf.Max(.001f, maximumFrameStep));
                float maxDistance = columns + Mathf.CeilToInt(CellCount / (float)columns);
                if (active[i].age > maxDistance / active[i].preset.speed + active[i].preset.duration + active[i].preset.width / active[i].preset.speed)
                    active[i] = active[--count];
            }
            for (int i = 0; i < rest.Length; i++)
            {
                if (cells[i] == null) continue;
                float height = 0;
                var cell = new Vector2(i % columns, i / columns);
                for (int w = 0; w < count; w++) height += Sample(active[w].preset, cell - active[w].source, active[w].age, shape);
                cells[i].localPosition = rest[i] + Vector3.up * Mathf.Clamp(height, -Mathf.Max(0, maximumDepth), Mathf.Max(0, maximumHeight));
            }
        }

        public static float Sample(Preset preset, Vector2 offset, float age, Shape shape = null)
        {
            shape ??= DefaultShape;
            float distance = preset.pattern == Pattern.Sweep ? Mathf.Abs(Vector2.Dot(offset, shape.sweepDirection.normalized)) : offset.magnitude;
            float local = age - distance / Mathf.Max(.1f, preset.speed);
            float duration = Mathf.Max(.1f, preset.duration) + Mathf.Max(.1f, preset.width) / Mathf.Max(.1f, preset.speed);
            if (local <= 0 || local >= duration) return 0;
            float t = local / duration;
            float envelope = Mathf.Sin(t * Mathf.PI);
            if (preset.pattern == Pattern.Ripple) envelope *= 1 - shape.rippleModulation + shape.rippleModulation * Mathf.Cos(t * Mathf.PI * 2 * shape.rippleCycles);
            if (preset.pattern == Pattern.CheckerHop)
                envelope *= ((Mathf.RoundToInt(offset.x + offset.y) & 1) == 0) ? 1 : shape.checkerAlternateHeight;
            float falloff = preset.pattern == Pattern.Bloom ? 1 / (1 + distance * Mathf.Max(0, shape.bloomFalloff)) : 1;
            return preset.amplitude * Mathf.Pow(Mathf.Abs(envelope), Mathf.Max(.1f, shape.envelopePower)) * falloff;
        }

        public float HeightAt(Vector3 world)
        {
            if (rest == null || columns < 1 || CellCount < columns) return 0;
            Vector3 local = transform.InverseTransformPoint(world);
            float x = local.x + (columns - 1) * .5f;
            int rows = CellCount / columns;
            float y = local.z + (rows - 1) * .5f;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(x), 0, columns - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(y), 0, rows - 1);
            int x1 = Mathf.Min(x0 + 1, columns - 1), y1 = Mathf.Min(y0 + 1, rows - 1);
            float H(int a, int b) { int i = b * columns + a; return cells[i] != null ? cells[i].localPosition.y - rest[i].y : 0; }
            return Mathf.Lerp(Mathf.Lerp(H(x0, y0), H(x1, y0), Mathf.Clamp01(x - x0)),
                Mathf.Lerp(H(x0, y1), H(x1, y1), Mathf.Clamp01(x - x0)), Mathf.Clamp01(y - y0));
        }

        private void OnDisable() => Clear();
    }
}
