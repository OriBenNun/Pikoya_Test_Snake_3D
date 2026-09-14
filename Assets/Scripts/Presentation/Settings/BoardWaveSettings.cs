using System;
using UnityEngine;

namespace GardenSnake.Presentation
{
    /// <summary>
    /// The waves that travel through the board's patches: one preset per pattern, how each one is
    /// shaped as it passes, and what the board will never do however many beats land at once.
    /// Edit outside Play Mode.
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/Board Waves")]
    public sealed class BoardWaveSettings : ScriptableObject
    {
        /// <summary>One wave: how tall it stands, how fast it travels, how long and wide it is.</summary>
        [Serializable]
        public struct Preset
        {
            [Min(0), Tooltip("How far the grass lifts at the crest.")]
            public float amplitude;
            [Min(.1f), Tooltip("Cells per second the wave travels outward.")]
            public float speed;
            [Min(.1f), Tooltip("Seconds one patch spends rising and falling.")]
            public float duration;
            [Min(.1f), Tooltip("How many cells of the board the crest covers at once.")]
            public float width;

            public Preset(float amplitude, float speed, float duration, float width)
            {
                this.amplitude = amplitude;
                this.speed = speed;
                this.duration = duration;
                this.width = width;
            }
        }

        [Header("Presets")]
        [Tooltip("A ring travelling out from a cell: the everyday wave.")]
        public Preset ripple = new(.25f, 13, .6f, 1.5f);
        [Tooltip("A slower, taller bloom for a record.")]
        public Preset bloom = new(.44f, 15, .85f, 2);
        [Tooltip("A diagonal sweep across the whole board as a run starts.")]
        public Preset sweep = new(.25f, 15, .6f, 2);
        [Tooltip("Alternating patches hopping, for a milestone.")]
        public Preset checkerHop = new(.22f, 12, .65f, 2);

        [Header("Shape")]
        [Range(0, 1), Tooltip("How much a ripple's crest is rippled in turn.")]
        public float rippleModulation = .35f;
        [Min(0)] public float rippleCycles = 2f;
        [Range(0, 1), Tooltip("Height the off-beat patches of a checker hop reach.")]
        public float checkerAlternateHeight = .3f;
        [Min(0), Tooltip("How quickly a bloom fades with distance from its source.")]
        public float bloomFalloff = .035f;
        [Min(.1f), Tooltip("Shapes the rise and fall of every wave. Higher is snappier.")]
        public float envelopePower = 2f;
        [Tooltip("Sweep direction in board coordinates; normalized when sampled.")]
        public Vector2 sweepDirection = Vector2.one;

        [Header("Limits")]
        [Range(1, 64), Tooltip("Waves that can travel at once. The oldest is dropped over this.")]
        public int maximumConcurrentWaves = 8;
        [Min(0), Tooltip("Highest any patch is ever lifted.")]
        public float maximumLift = .48f;
        [Min(0), Tooltip("Deepest any patch is ever pressed.")]
        public float maximumDip = .08f;
        [Min(0), Tooltip("Strongest a single beat may make its wave.")]
        public float maximumStrength = 2f;
        [Range(.016f, .1f), Tooltip("Longest frame a wave advances by, so a stall never teleports it across the board.")]
        public float maximumFrameStep = .05f;
    }
}
