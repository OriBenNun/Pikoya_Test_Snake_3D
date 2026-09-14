using UnityEngine;

namespace GardenSnake.Garden
{
    /// <summary>
    /// The shape of the wind: one slow travelling wave, a second faster one across it, Perlin
    /// turbulence over the top, all gated by a gust that comes and goes - and what a beat from the
    /// game does as it crosses the meadow. Edit outside Play Mode.
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/Wind")]
    public sealed class WindSettings : ScriptableObject
    {
        [Header("Sway")]
        [Range(0f, 3f), Tooltip("Sways per second.")]
        public float frequency = .9f;
        [Range(0f, 1f), Tooltip("How far the wave is phased by where a plant stands, so a gust travels across the meadow.")]
        public float spatialScale = .2f;
        [Min(0)] public float primarySway = .65f;
        [Min(0), Tooltip("A second, faster wave laid across the first.")]
        public float secondarySway = .2f;
        [Min(0)] public float secondaryFrequency = 1.73f;
        public float secondarySpatialScale = 1.3f;
        [Tooltip("Which way the phase travels across the garden, in world XZ.")]
        public Vector2 phaseDirection = new(1, .6f);
        [Tooltip("Which way a leaning plant tips, in world XZ.")]
        public Vector2 leanDirection = new(1, .55f);

        [Header("Gusts and turbulence")]
        [Range(0f, 1f), Tooltip("How far the wind drops between gusts.")]
        public float gustDepth = .45f;
        [Min(0)] public float gustFrequency = .23f;
        [Range(0, 1), Tooltip("How much neighbouring plants disagree about the tempo.")]
        public float frequencyVariation = .16f;
        public float variationSpatialScale = 3.71f;
        [Min(0)] public float turbulenceFrequency = .3f;
        [Min(0)] public float turbulenceStrength = 1.2f;
        [Tooltip("Where in the noise field the garden sits. Change it for a different meadow.")]
        public float noiseOffset = 40f;

        [Header("Plant weights")]
        [Min(0), Tooltip("How much of the wind a tree takes.")]
        public float treeWeight = .22f;
        [Min(0)] public float bushWeight = .45f;
        [Min(0), Tooltip("Everything smaller: flowers, grass and sprouts.")]
        public float plantWeight = 1f;

        [Header("Beats")]
        [Min(.1f), Tooltip("World units per second a beat travels out through the meadow.")]
        public float propagationSpeed = 30f;
        [Min(0), Tooltip("How much of a beat is lost per unit of distance.")]
        public float distanceFalloff = .025f;
        [Tooltip("A death pulls the meadow the other way, so the garden recoils with the snake.")]
        public float deathStrength = -1.5f;
        [Min(0)] public float pulseDuration = 2f;
        [Min(0)] public float pulseFrequency = 11f;
        [Min(0)] public float pulseDecay = 3f;
    }
}
