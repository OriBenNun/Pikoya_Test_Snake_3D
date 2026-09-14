using UnityEngine;

namespace GardenSnake.Presentation.Hud
{
    /// <summary>How every button in the game answers a pointer. Edit outside Play Mode.</summary>
    [CreateAssetMenu(menuName = "Garden Snake/Button Feel")]
    public sealed class ButtonFeelSettings : ScriptableObject
    {
        [Range(1, 1.15f), Tooltip("How far a button swells while the pointer is over it.")]
        public float hoverScale = 1.035f;
        [Range(.8f, 1), Tooltip("How far it presses in under the pointer.")]
        public float pressScale = .94f;
        [Min(.01f), Tooltip("Seconds the spring takes to settle.")]
        public float settleTime = .065f;
        [Min(0), Tooltip("How hard releasing it kicks back past rest.")]
        public float releaseKick = 1.2f;
    }
}
