using GardenSnake.Gameplay;
using UnityEngine;

namespace GardenSnake.Garden
{
    /// <summary>
    /// An animal that leaves the ground: it arcs up over the middle of every trip and beats a pair
    /// of wings the whole way. What the wings do at each moment is left to the species.
    /// </summary>
    public abstract class GardenFlier : GardenAnimal
    {
        [SerializeField] private Transform leftWing, rightWing;

        private float flapPhase;
        private float wingAngle;

        /// <summary>This flier's own asset.</summary>
        protected FlierSettings Flight => (FlierSettings)Species;

        protected override Activity TravelActivity => Activity.Fly;

        /// <summary>The wing angle mid-beat, for whenever this species is actually beating them.</summary>
        protected float Flap => Mathf.Sin(flapPhase) * Flight.flapWingAngle + Flight.flapWingOffset;

        /// <summary>How fast this species beats, relative to the shared rate.</summary>
        protected virtual float WingSpeedMultiplier => 1f;

        /// <summary>How far the wings are swept back when they are not carrying the animal.</summary>
        protected virtual float FoldAngle => 0;

        /// <summary>How far the wings shrink when folded away.</summary>
        protected virtual float FoldScale => 1f;

        /// <summary>The angle the wings are heading towards this frame.</summary>
        protected abstract float WingAngle(in FramePhase frame);

        protected override Vector3 RouteOffset(in FramePhase frame, float arc) =>
            Vector3.up * (arc * Flight.flightHeight);

        protected override void AnimateAppendages(in FramePhase frame, float nod, float delta)
        {
            if (leftWing == null || rightWing == null) return;
            flapPhase += delta * Flight.wingBeatsPerSecond * WingSpeedMultiplier * Mathf.PI * 2;
            wingAngle = Mathf.Lerp(wingAngle, WingAngle(frame),
                1 - Mathf.Exp(-delta * Flight.wingRotationResponse));
            float fold = FoldAngle;
            leftWing.localScale = Vector3.Lerp(leftWing.localScale, Vector3.one * FoldScale,
                1 - Mathf.Exp(-delta * Flight.wingScaleResponse));
            rightWing.localScale = leftWing.localScale;
            leftWing.localRotation = Quaternion.Euler(0, -fold, -wingAngle);
            rightWing.localRotation = Quaternion.Euler(0, fold, wingAngle);
        }
    }
}
