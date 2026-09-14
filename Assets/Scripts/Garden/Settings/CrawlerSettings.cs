using UnityEngine;

namespace GardenSnake.Garden
{
    /// <summary>
    /// An animal that keeps its feet on the ground and walks its patch. The stride is one sine wave
    /// per limb; which limbs move together is the species' own business, in code.
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/Wildlife/Crawler")]
    public class CrawlerSettings : AnimalSpeciesSettings
    {
        [Header("Stride")]
        [Min(0f), Tooltip("Stride cycles per trip.")]
        public float strideCycles = 9f;
        [Tooltip("How far a limb swings fore and aft, in degrees.")]
        public float strideAngle = 24f;
        [Min(0f), Tooltip("How far a limb lifts off the ground on its forward swing.")]
        public float strideLift = .035f;
    }
}
