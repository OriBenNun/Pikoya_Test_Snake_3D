using UnityEngine;

namespace GardenSnake.Garden
{
    /// <summary>Scurries its six legs in alternating tripods over a short patch of lawn.</summary>
    public sealed class GardenLadybug : GardenCrawler
    {
        /// <summary>Alternating tripods: each side's legs step against their neighbours'.</summary>
        protected override float StrideOffset(int index) => (index % 2 + index / 2) * Mathf.PI;
    }
}
