using UnityEngine;

namespace GardenSnake
{
    /// <summary>
    /// Never really lands and never stops beating its wings. It wanders sideways across every short
    /// trip instead of holding the straight line, and it is already in the air when the garden loads.
    /// </summary>
    public sealed class GardenButterfly : GardenFlier
    {
        [Header("Drift")]
        [SerializeField, Min(0f)] private float swayCycles = 2f;
        [SerializeField, Min(0f)] private float swayDistance = .15f;

        [Header("Wings")]
        [SerializeField, Min(0f)] private float wingSpeedMultiplier = 1.25f;
        [SerializeField] private float restWingAngle = 28f;
        [SerializeField, Min(0f)] private float restWingFrequency = 3f;
        [SerializeField] private float restWingAmplitude = 18f;

        protected override bool StartsTravelling => true;

        protected override float WingSpeedMultiplier => wingSpeedMultiplier;

        protected override Vector3 RouteOffset(in FramePhase frame, float arc) =>
            base.RouteOffset(frame, arc) +
            Vector3.right * (Mathf.Sin(frame.T * Mathf.PI * 2 * swayCycles + Phase) * arc * swayDistance);

        protected override float WingAngle(in FramePhase frame) => State == Activity.Fly
            ? Flap
            : restWingAngle + Mathf.Sin(frame.Clock * restWingFrequency) * restWingAmplitude;
    }
}
