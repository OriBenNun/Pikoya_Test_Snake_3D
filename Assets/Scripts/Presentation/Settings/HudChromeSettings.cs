using UnityEngine;

namespace GardenSnake.Presentation.Hud
{
    /// <summary>
    /// The HUD that is always there: the two corner buttons, the wordmark, the score and the
    /// record, and how they step back once a run is under way. Edit outside Play Mode.
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/HUD Chrome")]
    public sealed class HudChromeSettings : ScriptableObject
    {
        [Header("Corner controls")]
        public Color controlColor = new(.118f, .227f, .165f);
        [Range(0, 1), Tooltip("Opacity of the sound button while the game is muted.")]
        public float mutedOpacity = .35f;
        [Range(0, 1)] public float soundOnOpacity = .85f;

        [Header("Wordmark")]
        [Range(0, 1), Tooltip("Opacity the wordmark fades to while a run is going.")]
        public float playingBrandOpacity = .85f;
        [Min(0)] public float chromeFadeSpeed = 1.6f;

        [Header("Record flash")]
        public Color bestColor = new(.118f, .227f, .165f, .72f);
        [Tooltip("Colour the record flashes as it is extended.")]
        public Color bestFlashColor = new(.89f, .38f, .15f);
        [Min(.01f)] public float bestFlashDuration = .6f;
        [Min(0), Tooltip("How far the record swells as it flashes.")]
        public float bestFlashScale = .1f;
    }
}
