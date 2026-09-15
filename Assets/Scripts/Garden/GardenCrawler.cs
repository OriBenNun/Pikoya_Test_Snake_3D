using UnityEngine;

namespace GardenSnake.Garden
{
    /// <summary>
    /// An animal that keeps its feet on the ground and walks its patch. The stride is one sine wave
    /// per limb; the species decides which limbs move together.
    /// </summary>
    public abstract class GardenCrawler : GardenAnimal
    {
        /// <summary>This crawler's own asset.</summary>
        protected CrawlerSettings Gait => (CrawlerSettings)Species;

        protected override Activity TravelActivity => Activity.Crawl;

        protected override AnimalSpeciesSettings DefaultSpecies() => ScriptableObject.CreateInstance<CrawlerSettings>();

        /// <summary>Where a limb sits in the gait cycle, in radians.</summary>
        protected abstract float StrideOffset(int index);

        protected override void PoseLimb(int index, in FramePhase frame, out float angle, out float sweep, out float lift)
        {
            angle = 0;
            sweep = 0;
            lift = 0;
            if (!frame.Moving) return;
            float stride = Mathf.Sin(frame.T * Mathf.PI * 2 * Gait.strideCycles + StrideOffset(index));
            sweep = stride * Gait.strideAngle;
            lift = Mathf.Max(0, stride) * Gait.strideLift;
        }
    }
}
