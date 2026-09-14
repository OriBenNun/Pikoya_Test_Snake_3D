using UnityEngine;

namespace GardenSnake.Garden
{
    /// <summary>
    /// Never really lands and never stops beating its wings. It wanders sideways across every short
    /// trip instead of holding the straight line, and it is already in the air when the garden loads.
    /// </summary>
    public sealed class GardenButterfly : GardenFlier
    {
        private ButterflySettings Tuning => (ButterflySettings)Species;

        protected override AnimalSpeciesSettings DefaultSpecies() => ScriptableObject.CreateInstance<ButterflySettings>();

        protected override bool StartsTravelling => true;

        protected override float WingSpeedMultiplier => Tuning.wingSpeedMultiplier;

        protected override Vector3 RouteOffset(in FramePhase frame, float arc) =>
            base.RouteOffset(frame, arc) + Vector3.right *
            (Mathf.Sin(frame.T * Mathf.PI * 2 * Tuning.swayCycles + Phase) * arc * Tuning.swayDistance);

        protected override float WingAngle(in FramePhase frame) => State == Activity.Fly
            ? Flap
            : Tuning.restWingAngle + Mathf.Sin(frame.Clock * Tuning.restWingFrequency) * Tuning.restWingAmplitude;
    }
}
