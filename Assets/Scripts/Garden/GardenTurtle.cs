using UnityEngine;

namespace GardenSnake.Garden
{
    /// <summary>
    /// Walks diagonal pairs of flippers, rolling gently as it goes, and is the one animal that
    /// answers a death by pulling head and legs into its shell instead of running.
    /// </summary>
    public sealed class GardenTurtle : GardenCrawler
    {
        private bool Hiding => State == Activity.Hide;

        private TurtleSettings Tuning => (TurtleSettings)Species;

        protected override AnimalSpeciesSettings DefaultSpecies() => ScriptableObject.CreateInstance<TurtleSettings>();

        /// <summary>Diagonal pairs: front left with rear right, and the other two against them.</summary>
        protected override float StrideOffset(int index) => index == 0 || index == 3 ? 0 : Mathf.PI;

        protected override float BodyRoll(in FramePhase frame) => frame.Moving
            ? Mathf.Sin(frame.Clock * Tuning.rollFrequency) * Tuning.roll
            : 0;

        protected override bool Startle(Beat beat, float strength)
        {
            if (beat != Beat.Death) return false;
            EnterActivity(Activity.Hide, Tuning.hideSeconds);
            return true;
        }

        protected override float HeadTuck => Hiding ? Tuning.hiddenHeadScale : base.HeadTuck;

        protected override Vector3 HeadTuckOffset => Hiding ? Tuning.hiddenHeadOffset : base.HeadTuckOffset;

        protected override void AdjustLimbPosition(ref Vector3 local)
        {
            if (Hiding) local.x *= Tuning.hiddenLimbSpread;
        }
    }
}
