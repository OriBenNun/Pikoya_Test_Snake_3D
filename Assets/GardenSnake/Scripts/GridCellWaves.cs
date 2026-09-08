using GardenSnake.Core;
using UnityEngine;

namespace GardenSnake
{
    /// <summary>Bounded, additive visual waves. Cell coordinates and collision never move.</summary>
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

        [SerializeField] private Transform[] cells;
        [SerializeField] private int columns = 21;
        [SerializeField] private float maximumHeight = .48f;
        [SerializeField] private Preset ripple = new Preset(Pattern.Ripple, .25f, 13, .6f, 1.5f);
        [SerializeField] private Preset celebration = new Preset(Pattern.Bloom, .44f, 15, .85f, 2);
        [SerializeField] private Preset sweep = new Preset(Pattern.Sweep, .25f, 15, .6f, 2);
        [SerializeField] private Preset checker = new Preset(Pattern.CheckerHop, .22f, 12, .65f, 2);
        private struct Wave { public Preset preset; public Vector2 source; public float age; }
        private readonly Wave[] active = new Wave[8];
        private Vector3[] rest;
        private int count;
        private SnakeController controller;
        public int ActiveCount => count;
        public int CellCount => cells == null ? 0 : cells.Length;

        private void Awake()
        {
            rest = new Vector3[CellCount];
            for (int i = 0; i < rest.Length; i++) rest[i] = cells[i].localPosition;
            controller = FindFirstObjectByType<SnakeController>();
        }

        public void Play(Pattern pattern, Cell source, float strength = 1)
        {
            var preset = pattern == Pattern.Bloom ? celebration : pattern == Pattern.Sweep ? sweep
                : pattern == Pattern.CheckerHop ? checker : ripple;
            preset.amplitude *= Mathf.Clamp(strength, 0, 2);
            Play(preset, source);
        }

        public void Play(Preset preset, Cell source)
        {
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
            for (int i = 0; i < rest.Length; i++) cells[i].localPosition = rest[i];
        }

        private void Update()
        {
            if (count == 0 || rest == null || (controller != null && controller.Game.State == RunState.Paused)) return;
            for (int i = count - 1; i >= 0; i--)
            {
                active[i].age += Time.unscaledDeltaTime;
                float maxDistance = columns + Mathf.CeilToInt(CellCount / (float)columns);
                if (active[i].age > maxDistance / active[i].preset.speed + active[i].preset.duration + active[i].preset.width / active[i].preset.speed)
                    active[i] = active[--count];
            }
            for (int i = 0; i < rest.Length; i++)
            {
                float height = 0;
                var cell = new Vector2(i % columns, i / columns);
                for (int w = 0; w < count; w++) height += Sample(active[w].preset, cell - active[w].source, active[w].age);
                cells[i].localPosition = rest[i] + Vector3.up * Mathf.Clamp(height, -.08f, maximumHeight);
            }
        }

        public static float Sample(Preset preset, Vector2 offset, float age)
        {
            float distance = preset.pattern == Pattern.Sweep ? Mathf.Abs(offset.x + offset.y) * .7071f : offset.magnitude;
            float local = age - distance / Mathf.Max(.1f, preset.speed);
            float duration = Mathf.Max(.1f, preset.duration) + Mathf.Max(.1f, preset.width) / Mathf.Max(.1f, preset.speed);
            if (local <= 0 || local >= duration) return 0;
            float t = local / duration;
            float envelope = Mathf.Sin(t * Mathf.PI);
            if (preset.pattern == Pattern.Ripple) envelope *= .65f + .35f * Mathf.Cos(t * Mathf.PI * 4);
            if (preset.pattern == Pattern.CheckerHop)
                envelope *= ((Mathf.RoundToInt(offset.x + offset.y) & 1) == 0) ? 1 : .3f;
            float falloff = preset.pattern == Pattern.Bloom ? 1 / (1 + distance * .035f) : 1;
            return preset.amplitude * envelope * envelope * falloff;
        }

        public float HeightAt(Vector3 world)
        {
            if (rest == null) return 0;
            Vector3 local = transform.InverseTransformPoint(world);
            float x = local.x + (columns - 1) * .5f;
            int rows = CellCount / columns;
            float y = local.z + (rows - 1) * .5f;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(x), 0, columns - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(y), 0, rows - 1);
            int x1 = Mathf.Min(x0 + 1, columns - 1), y1 = Mathf.Min(y0 + 1, rows - 1);
            float H(int a, int b) { int i = b * columns + a; return cells[i].localPosition.y - rest[i].y; }
            return Mathf.Lerp(Mathf.Lerp(H(x0, y0), H(x1, y0), Mathf.Clamp01(x - x0)),
                Mathf.Lerp(H(x0, y1), H(x1, y1), Mathf.Clamp01(x - x0)), Mathf.Clamp01(y - y0));
        }

        private void OnDisable() => Clear();
    }
}
