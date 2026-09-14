using UnityEngine;

namespace GardenSnake.Garden
{
    /// <summary>
    /// Flies between the lawn and a perch in the tree, flapping out of each end of the trip and
    /// gliding the middle of it. On the ground it folds its wings away and tucks its legs up.
    /// </summary>
    public sealed class GardenBird : GardenFlier
    {
        [SerializeField, Tooltip("Where in the tree this bird sits between trips.")]
        private Transform perch;

        private bool atPerch;

        private bool Perched => atPerch && perch != null;

        private BirdSettings Tuning => (BirdSettings)Species;

        protected override AnimalSpeciesSettings DefaultSpecies() => ScriptableObject.CreateInstance<BirdSettings>();

        /// <summary>Every other trip goes up to the perch rather than to another spot on the lawn.</summary>
        protected override Vector3 ChooseDestination(bool towardsB)
        {
            atPerch = Tuning.usePerch && towardsB && perch != null;
            return atPerch ? perch.position : base.ChooseDestination(towardsB);
        }

        protected override Vector3 ArrivalPosition => Perched ? perch.position : base.ArrivalPosition;

        protected override void Settle()
        {
            if (Perched) transform.position = perch.position;
        }

        /// <summary>A perch sways in the wind, so aim at where it is now rather than where it was.</summary>
        protected override void TrackDestination()
        {
            if (Perched) Destination = perch.position;
        }

        protected override float BodyPitch(in FramePhase frame) => frame.Moving
            ? Mathf.Sin(frame.T * Mathf.PI * 2) * Tuning.flightPitch
            : 0;

        protected override void PoseLimb(int index, in FramePhase frame, out float angle, out float sweep, out float lift)
        {
            angle = frame.Moving ? Mathf.Sin(frame.T * Mathf.PI) * Tuning.flightLimbAngle : 0;
            sweep = 0;
            lift = 0;
        }

        protected override float FoldAngle => State == Activity.Fly ? 0 : Tuning.foldedWingAngle;

        protected override float FoldScale => State == Activity.Fly ? 1f : Tuning.foldedWingScale;

        protected override float WingAngle(in FramePhase frame)
        {
            if (State != Activity.Fly) return Tuning.restWingAngle;
            bool gliding = frame.T > Tuning.glideProgressRange.x && frame.T < Tuning.glideProgressRange.y;
            return gliding ? Tuning.glideWingAngle : Flap;
        }
    }
}
