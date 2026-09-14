using UnityEngine;

namespace GardenSnake.Garden
{
    /// <summary>An animal that leaves the ground: how high it arcs, and how it beats its wings.</summary>
    public abstract class FlierSettings : AnimalSpeciesSettings
    {
        [Header("Flight")]
        [Range(0f, 6f), Tooltip("How high the middle of a trip arcs above its ends.")]
        public float flightHeight = 1.5f;

        [Header("Wings")]
        [Range(2f, 12f)] public float wingBeatsPerSecond = 5.5f;
        [Tooltip("Degrees the wings sweep through a beat.")]
        public float flapWingAngle = 52f;
        [Tooltip("Degrees the whole beat is offset by, so the wings sit above level.")]
        public float flapWingOffset = 8f;
        [Min(0f)] public float wingRotationResponse = 35f;
        [Min(0f)] public float wingScaleResponse = 12f;
    }
}
