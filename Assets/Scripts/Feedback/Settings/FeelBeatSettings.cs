using UnityEngine;

namespace GardenSnake
{
    /// <summary>
    /// How hard each beat is allowed to land: the intensity handed to its Feel player and put on
    /// the garden's channel for the scenery to lean with. Edit outside Play Mode.
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/Feel Beats")]
    public sealed class FeelBeatSettings : ScriptableObject
    {
        [Header("Pickup")]
        [Tooltip("Intensity of the first apple of a run, and of a fully escalated one.")]
        public Vector2 pickupIntensity = new(.75f, 1.5f);
        [Min(1), Tooltip("The score at which a pickup lands as hard as pickups ever land.")]
        public int fullPickupIntensityScore = 14;

        [Header("Everything else")]
        [Min(0)] public float deathIntensity = 1;
        [Min(0)] public float runStartIntensity = 1;
        [Min(0)] public float newBestIntensity = 1;
        [Min(0), Tooltip("Each apple past a record already broken this run.")]
        public float recordAppleIntensity = 1;
    }
}
