using UnityEngine;

namespace GardenSnake.Presentation.Hud
{
    /// <summary>
    /// The words the HUD throws up over the board: the number that pops where an apple was eaten,
    /// and the banner a milestone earns. Edit outside Play Mode.
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/HUD Toast")]
    public sealed class HudToastSettings : ScriptableObject
    {
        [Header("Pickup toast")]
        [Min(1)] public float fontSize = 40;
        [Min(1), Tooltip("Size of the quieter toast an extended record gets.")]
        public float quietFontSize = 28;
        [Min(.01f)] public float duration = .95f;
        [Min(.01f)] public float quietDuration = .7f;
        [Min(0)] public float fadeSpeed = 3.2f;
        [Range(0, 1), Tooltip("Opacity of the glow behind the number.")]
        public float haloOpacity = .75f;
        [Range(0, 1)] public float quietHaloOpacity = .22f;
        [Tooltip("Pixels above the apple the number starts.")]
        public float startHeight = 64;
        [Tooltip("Pixels it drifts up as it fades.")]
        public float rise = 54;
        [Min(0), Tooltip("Pixels of headroom kept below the top of the canvas.")]
        public float topPadding = 70;
        [Tooltip("X: the size it pops in at. Y: the size it settles to.")]
        public Vector2 scale = new(1.3f, .95f);
        [Min(0)] public float scaleSpeed = 3;

        [Header("Banner")]
        [Min(.01f)] public float bannerDuration = 2;
        [Min(0)] public float bannerFadeSpeed = 2.4f;
        [Min(0)] public float bannerRiseSpeed = 6;
        [Range(0, 1)] public float bannerFillOpacity = .96f;
        [Min(0)] public float bannerStartScale = .82f;
        [Min(.01f), Tooltip("Shapes the banner's arrival. Higher is snappier.")]
        public float bannerEasePower = 3;
    }
}
