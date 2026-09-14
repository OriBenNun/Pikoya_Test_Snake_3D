using UnityEngine;

namespace GardenSnake
{
    /// <summary>Scurries its six legs in alternating tripods over a short patch of lawn.</summary>
    public sealed class GardenLadybug : GardenCrawler
    {
        [Header("Stride")]
        [SerializeField, Min(0f)] private float strideCycles = 9f;
        [SerializeField] private float strideAngle = 24f;

        protected override float StrideCycles => strideCycles;

        protected override float StrideAngle => strideAngle;

        /// <summary>Alternating tripods: each side's legs step against their neighbours'.</summary>
        protected override float StrideOffset(int index) => (index % 2 + index / 2) * Mathf.PI;
    }
}
