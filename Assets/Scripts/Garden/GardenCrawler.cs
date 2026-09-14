using UnityEngine;

namespace GardenSnake
{
    /// <summary>
    /// An animal that keeps its feet on the ground and walks its patch. The stride is one sine wave
    /// per limb; the species decides how fast it cycles, how far each limb swings, and which limbs
    /// move together.
    /// </summary>
    public abstract class GardenCrawler : GardenAnimal
    {
        [Header("Stride")]
        [SerializeField, Min(0f)] private float strideLift = .035f;

        protected override Activity TravelActivity => Activity.Crawl;

        /// <summary>Stride cycles per trip.</summary>
        protected abstract float StrideCycles { get; }

        /// <summary>How far a limb swings fore and aft, in degrees.</summary>
        protected abstract float StrideAngle { get; }

        /// <summary>Where a limb sits in the gait cycle, in radians.</summary>
        protected abstract float StrideOffset(int index);

        protected override void PoseLimb(int index, in FramePhase frame, out float angle, out float sweep, out float lift)
        {
            angle = 0;
            sweep = 0;
            lift = 0;
            if (!frame.Moving) return;
            float stride = Mathf.Sin(frame.T * Mathf.PI * 2 * StrideCycles + StrideOffset(index));
            sweep = stride * StrideAngle;
            lift = Mathf.Max(0, stride) * strideLift;
        }
    }
}
