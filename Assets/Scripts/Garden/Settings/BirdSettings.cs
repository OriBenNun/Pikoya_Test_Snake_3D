using UnityEngine;

namespace GardenSnake.Garden
{
    /// <summary>
    /// The bird: how it flies between the lawn and its perch, and how it folds its wings away once
    /// it is standing on something.
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/Wildlife/Bird")]
    public sealed class BirdSettings : FlierSettings
    {
        [Header("Perch")]
        [Tooltip("Birds use their perch on alternate trips when enabled.")]
        public bool usePerch = true;

        [Header("Flight")]
        [Tooltip("Degrees the body pitches through a flight.")]
        public float flightPitch = -12f;
        [Tooltip("Degrees the legs tuck up while airborne.")]
        public float flightLimbAngle = -65f;

        [Header("Wings")]
        [Tooltip("Normalized flight progress at which the bird starts and stops gliding.")]
        public Vector2 glideProgressRange = new(.25f, .7f);
        public float glideWingAngle = 10f;
        [Tooltip("Where the wings sit on the ground, before they fold.")]
        public float restWingAngle = -12f;
        [Tooltip("Degrees the wings sweep back when they are folded away.")]
        public float foldedWingAngle = 65f;
        [Min(0f)] public float foldedWingScale = .68f;
    }
}
