using GardenSnake.Gameplay;
using UnityEngine;

namespace GardenSnake.Presentation
{
    /// <summary>
    /// Bounded, additive visual waves through the board's patches. Cell coordinates and collision
    /// never move; only the drawn grass does.
    /// <para>
    /// It knows nothing about the run: whoever triggers a wave also tells it when the garden
    /// should hold still, so the board can be exercised on its own.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class GridCellWaves : MonoBehaviour
    {
        // The authored board, laid out row by row from the bottom left. Not a setting: the cells
        // below are that grid, and nothing rebuilds it.
        private const int Columns = GameLoopManager.Columns;
        private const int Rows = GameLoopManager.Rows;

        [Header("Board")]
        [SerializeField, Tooltip("Every patch of the board, row by row from the bottom left.")]
        private Transform[] cells;
        [Header("Tuning")]
        [SerializeField] private BoardWaveSettings waves;
        [SerializeField, Range(0f, 2f), Tooltip("How high the grass lifts when a wave passes. 0 holds the board flat.")]
        private float waveHeight = 1f;

        private Wave[] live;
        private Vector3[] rest;
        private int waveCount;
        private int CellCount => cells == null ? 0 : cells.Length;

        /// <summary>While set, live waves keep their age and the board stops moving.</summary>
        public bool Frozen { get; set; }

        /// <summary>How far the drawn board has risen under a world position, for anything riding it.</summary>
        public float HeightAt(Vector3 world)
        {
            if (rest == null || CellCount < Columns) return 0;
            var local = transform.InverseTransformPoint(world);
            var x = local.x + (Columns - 1) * .5f;
            var y = local.z + (Rows - 1) * .5f;
            var x0 = Mathf.Clamp(Mathf.FloorToInt(x), 0, Columns - 1);
            var y0 = Mathf.Clamp(Mathf.FloorToInt(y), 0, Rows - 1);
            int x1 = Mathf.Min(x0 + 1, Columns - 1), y1 = Mathf.Min(y0 + 1, Rows - 1);
            float Lift(int column, int row)
            {
                var i = row * Columns + column;
                return i < rest.Length && cells[i] != null ? cells[i].localPosition.y - rest[i].y : 0;
            }
            var acrossX = Mathf.Clamp01(x - x0);
            return Mathf.Lerp(Mathf.Lerp(Lift(x0, y0), Lift(x1, y0), acrossX),
                Mathf.Lerp(Lift(x0, y1), Lift(x1, y1), acrossX), Mathf.Clamp01(y - y0));
        }

        public void Play(Pattern pattern, Cell source, float strength = 1)
        {
            if (!isActiveAndEnabled || live == null || CellCount == 0) return;
            var preset = pattern switch
            {
                Pattern.Bloom => waves.bloom,
                Pattern.Sweep => waves.sweep,
                Pattern.CheckerHop => waves.checkerHop,
                _ => waves.ripple
            };
            preset.amplitude = Mathf.Max(0, preset.amplitude)
                * Mathf.Clamp(strength, 0, waves.maximumStrength) * Mathf.Max(0, waveHeight);
            preset.speed = Mathf.Max(.1f, preset.speed);
            preset.duration = Mathf.Max(.1f, preset.duration);
            preset.width = Mathf.Max(.1f, preset.width);
            // The buffer is full: drop the oldest wave so the newest beat is never the one lost.
            if (waveCount == live.Length)
            {
                for (var i = 1; i < waveCount; i++) live[i - 1] = live[i];
                waveCount--;
            }
            live[waveCount++] = new Wave
            {
                Pattern = pattern,
                Preset = preset,
                Source = new Vector2(source.X, source.Y)
            };
        }

        public void Clear()
        {
            waveCount = 0;
            if (rest == null) return;
            for (var i = 0; i < rest.Length; i++) if (cells[i] != null) cells[i].localPosition = rest[i];
        }

        private void Awake()
        {
            waves = Tuning.Or(waves);
            live = new Wave[Mathf.Clamp(waves.maximumConcurrentWaves, 1, 64)];
            rest = new Vector3[CellCount];
            for (var i = 0; i < rest.Length; i++) if (cells[i] != null) rest[i] = cells[i].localPosition;
        }

        private void Update()
        {
            if (waveCount == 0 || rest == null || Frozen) return;
            // The furthest a wave can have to travel: one corner of the board to the other.
            const float maxDistance = Columns + Rows;
            var step = Mathf.Min(Time.unscaledDeltaTime, Mathf.Max(.001f, waves.maximumFrameStep));
            for (var i = waveCount - 1; i >= 0; i--)
            {
                // Preserve the source beat after a stalled frame instead of jumping past it.
                live[i].Age += step;
                var preset = live[i].Preset;
                var lifetime = maxDistance / preset.speed + preset.duration + preset.width / preset.speed;
                if (live[i].Age > lifetime) live[i] = live[--waveCount];
            }
            var lift = Mathf.Max(0, waves.maximumLift);
            var dip = Mathf.Max(0, waves.maximumDip);
            for (var i = 0; i < rest.Length; i++)
            {
                if (cells[i] == null) continue;
                float height = 0;
                var cell = new Vector2(i % Columns, i / Columns);
                for (var w = 0; w < waveCount; w++)
                    height += Sample(live[w], cell - live[w].Source);
                cells[i].localPosition = rest[i] + Vector3.up * Mathf.Clamp(height, -dip, lift);
            }
        }

        /// <summary>How far one wave lifts a patch that far from its source, at the wave's current age.</summary>
        private float Sample(in Wave wave, Vector2 offset)
        {
            var preset = wave.Preset;
            var distance = wave.Pattern == Pattern.Sweep
                ? Mathf.Abs(Vector2.Dot(offset, waves.sweepDirection.normalized))
                : offset.magnitude;
            var local = wave.Age - distance / preset.speed;
            var duration = preset.duration + preset.width / preset.speed;
            if (local <= 0 || local >= duration) return 0;
            var t = local / duration;
            var envelope = Mathf.Sin(t * Mathf.PI);
            if (wave.Pattern == Pattern.Ripple)
                envelope *= 1 - waves.rippleModulation
                    + waves.rippleModulation * Mathf.Cos(t * Mathf.PI * 2 * waves.rippleCycles);
            if (wave.Pattern == Pattern.CheckerHop)
                envelope *= ((Mathf.RoundToInt(offset.x + offset.y) & 1) == 0) ? 1 : waves.checkerAlternateHeight;
            var falloff = wave.Pattern == Pattern.Bloom
                ? 1 / (1 + distance * Mathf.Max(0, waves.bloomFalloff)) : 1;
            return preset.amplitude * Mathf.Pow(Mathf.Abs(envelope), Mathf.Max(.1f, waves.envelopePower)) * falloff;
        }

        private void OnDisable() => Clear();

        public enum Pattern { Ripple, Bloom, Sweep, CheckerHop }

        private struct Wave
        {
            public Pattern Pattern;
            public BoardWaveSettings.Preset Preset;
            public Vector2 Source;
            public float Age;
        }
    }
}
