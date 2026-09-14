using UnityEngine;

namespace GardenSnake
{
    /// <summary>
    /// Flies between the lawn and a perch in the tree, flapping out of each end of the trip and
    /// gliding the middle of it. On the ground it folds its wings away and tucks its legs up.
    /// </summary>
    public sealed class GardenBird : GardenFlier
    {
        [SerializeField] private Transform perch;
        [SerializeField, Tooltip("Birds use their perch on alternate trips when enabled.")] private bool usePerch = true;
        [SerializeField] private float flightPitch = -12f;
        [SerializeField] private float flightLimbAngle = -65f;

        [Header("Wings")]
        [SerializeField, Tooltip("Normalized flight progress at which the bird starts and stops gliding.")] private Vector2 glideProgressRange = new Vector2(.25f, .7f);
        [SerializeField] private float glideWingAngle = 10f;
        [SerializeField] private float restWingAngle = -12f;
        [SerializeField] private float foldedWingAngle = 65f;
        [SerializeField, Min(0f)] private float foldedWingScale = .68f;

        private bool atPerch;

        private bool Perched => atPerch && perch != null;

        /// <summary>Every other trip goes up to the perch rather than to another spot on the lawn.</summary>
        protected override Vector3 ChooseDestination(bool towardsB)
        {
            atPerch = usePerch && towardsB && perch != null;
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
            ? Mathf.Sin(frame.T * Mathf.PI * 2) * flightPitch
            : 0;

        protected override void PoseLimb(int index, in FramePhase frame, out float angle, out float sweep, out float lift)
        {
            angle = frame.Moving ? Mathf.Sin(frame.T * Mathf.PI) * flightLimbAngle : 0;
            sweep = 0;
            lift = 0;
        }

        protected override float FoldAngle => State == Activity.Fly ? 0 : foldedWingAngle;

        protected override float FoldScale => State == Activity.Fly ? 1f : foldedWingScale;

        protected override float WingAngle(in FramePhase frame)
        {
            if (State != Activity.Fly) return restWingAngle;
            bool gliding = frame.T > glideProgressRange.x && frame.T < glideProgressRange.y;
            return gliding ? glideWingAngle : Flap;
        }
    }
}
