using UnityEngine;

namespace GardenSnake.Gameplay
{
    /// <summary>
    /// What counts as a swipe. A flick has to clear both thresholds, so the same gesture reads the
    /// same way on a phone and in a desktop window.
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/Swipe")]
    public sealed class SwipeSettings : ScriptableObject
    {
        [Min(1), Tooltip("Shortest swipe that turns the snake, in pixels.")]
        public float minimumPixels = 24f;
        [Range(0, 1), Tooltip("The same threshold as a fraction of screen height; whichever is larger wins.")]
        public float screenHeightFraction = .035f;
    }
}
