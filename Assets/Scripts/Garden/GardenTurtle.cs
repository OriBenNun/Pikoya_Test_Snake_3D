using UnityEngine;

namespace GardenSnake
{
    /// <summary>
    /// Walks diagonal pairs of flippers, rolling gently as it goes, and is the one animal that
    /// answers a death by pulling head and legs into its shell instead of running.
    /// </summary>
    public sealed class GardenTurtle : GardenCrawler
    {
        [Header("Stride")]
        [SerializeField, Min(0f)] private float strideCycles = 5f;
        [SerializeField] private float strideAngle = 20f;

        [Header("Body Motion")]
        [SerializeField, Min(0f)] private float rollFrequency = 5f;
        [SerializeField] private float roll = 2f;

        [Header("Hiding")]
        [SerializeField, Min(.001f)] private float hideSeconds = 2.5f;
        [SerializeField, Range(0f, 1f)] private float hiddenHeadScale = .75f;
        [SerializeField] private Vector3 hiddenHeadOffset = new Vector3(0f, -.06f, -.36f);
        [SerializeField, Range(0f, 1f)] private float hiddenLimbSpread = .6f;

        private bool Hiding => State == Activity.Hide;

        protected override float StrideCycles => strideCycles;

        protected override float StrideAngle => strideAngle;

        /// <summary>Diagonal pairs: front left with rear right, and the other two against them.</summary>
        protected override float StrideOffset(int index) => index == 0 || index == 3 ? 0 : Mathf.PI;

        protected override float BodyRoll(in FramePhase frame) => frame.Moving
            ? Mathf.Sin(frame.Clock * rollFrequency) * roll
            : 0;

        protected override bool Startle(Beat beat, float strength)
        {
            if (beat != Beat.Death) return false;
            EnterActivity(Activity.Hide, hideSeconds);
            return true;
        }

        protected override float HeadTuck => Hiding ? hiddenHeadScale : base.HeadTuck;

        protected override Vector3 HeadTuckOffset => Hiding ? hiddenHeadOffset : base.HeadTuckOffset;

        protected override void AdjustLimbPosition(ref Vector3 local)
        {
            if (Hiding) local.x *= hiddenLimbSpread;
        }
    }
}
