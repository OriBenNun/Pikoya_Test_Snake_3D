using UnityEngine;

namespace GardenSnake.Garden
{
    /// <summary>
    /// The butterfly: it never really lands and never stops beating its wings, and it wanders
    /// sideways across every short trip instead of holding the straight line.
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/Wildlife/Butterfly")]
    public sealed class ButterflySettings : FlierSettings
    {
        [Header("Drift")]
        [Min(0f), Tooltip("Wanders from side to side this many times per trip.")]
        public float swayCycles = 2f;
        [Min(0f), Tooltip("How far it wanders off the straight line.")]
        public float swayDistance = .15f;

        [Header("Wings")]
        [Min(0f), Tooltip("How much faster it beats than the shared rate.")]
        public float wingSpeedMultiplier = 1.25f;
        [Tooltip("Where the wings rest when it is not flying.")]
        public float restWingAngle = 28f;
        [Min(0f)] public float restWingFrequency = 3f;
        [Tooltip("How far the resting wings still open and close.")]
        public float restWingAmplitude = 18f;
    }
}
