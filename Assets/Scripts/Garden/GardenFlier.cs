using UnityEngine;

namespace GardenSnake
{
    /// <summary>
    /// An animal that leaves the ground: it arcs up over the middle of every trip and beats a pair
    /// of wings the whole way. What the wings do at each moment is left to the species.
    /// </summary>
    public abstract class GardenFlier : GardenAnimal
    {
        [SerializeField] private Transform leftWing, rightWing;
        [SerializeField, Range(0f, 6f)] private float flightHeight = 2f;

        [Header("Wings")]
        [SerializeField, Range(2f, 12f)] private float wingBeatsPerSecond = 5.5f;
        [SerializeField] private float flapWingAngle = 52f;
        [SerializeField] private float flapWingOffset = 8f;
        [SerializeField, Min(0f)] private float wingRotationResponse = 35f;
        [SerializeField, Min(0f)] private float wingScaleResponse = 12f;

        private float flapPhase;
        private float wingAngle;

        protected override Activity TravelActivity => Activity.Fly;

        /// <summary>The wing angle mid-beat, for whenever this species is actually beating them.</summary>
        protected float Flap => Mathf.Sin(flapPhase) * flapWingAngle + flapWingOffset;

        /// <summary>How fast this species beats, relative to the shared rate.</summary>
        protected virtual float WingSpeedMultiplier => 1f;

        /// <summary>How far the wings are swept back when they are not carrying the animal.</summary>
        protected virtual float FoldAngle => 0;

        /// <summary>How far the wings shrink when folded away.</summary>
        protected virtual float FoldScale => 1f;

        /// <summary>The angle the wings are heading towards this frame.</summary>
        protected abstract float WingAngle(in FramePhase frame);

        protected override Vector3 RouteOffset(in FramePhase frame, float arc) => Vector3.up * (arc * flightHeight);

        protected override void AnimateAppendages(in FramePhase frame, float nod, float delta)
        {
            if (leftWing == null || rightWing == null) return;
            flapPhase += delta * wingBeatsPerSecond * WingSpeedMultiplier * Mathf.PI * 2;
            wingAngle = Mathf.Lerp(wingAngle, WingAngle(frame), 1 - Mathf.Exp(-delta * wingRotationResponse));
            float fold = FoldAngle;
            leftWing.localScale = Vector3.Lerp(leftWing.localScale, Vector3.one * FoldScale,
                1 - Mathf.Exp(-delta * wingScaleResponse));
            rightWing.localScale = leftWing.localScale;
            leftWing.localRotation = Quaternion.Euler(0, -fold, -wingAngle);
            rightWing.localRotation = Quaternion.Euler(0, fold, wingAngle);
        }
    }
}
