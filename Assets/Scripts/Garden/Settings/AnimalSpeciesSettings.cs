using UnityEngine;

namespace GardenSnake.Garden
{
    /// <summary>
    /// What one kind of animal does that the others do not. Every species has an asset of its own,
    /// shared by each of that animal in the garden; only its route and its phase are authored per
    /// animal in the scene. Edit outside Play Mode.
    /// </summary>
    public abstract class AnimalSpeciesSettings : ScriptableObject
    {
        [Header("Pace")]
        [Min(.1f), Tooltip("Seconds a trip across the patch takes, before variation.")]
        public float travelSeconds = 3f;
        [Min(.1f), Tooltip("Seconds it stays put between trips, before variation.")]
        public float restSeconds = 6f;
    }
}
