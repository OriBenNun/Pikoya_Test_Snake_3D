using UnityEngine;

namespace GardenSnake.Presentation.Hud
{
    /// <summary>
    /// The one card the game has, on the menus, on a pause and on a result: how it arrives, and
    /// where everything on it sits. Every pair is X for the ready and paused layout, Y for the
    /// results layout, so the card is only ever as tall as the state needs. Edit outside Play Mode.
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/HUD Card")]
    public sealed class HudCardSettings : ScriptableObject
    {
        [Header("Arrival")]
        [Min(.01f)] public float fadeDuration = .22f;
        [Min(.01f), Tooltip("Shapes the arrival. Higher is snappier.")]
        public float easePower = 3;
        [Min(0)] public float startScale = .94f;
        [Tooltip("Pixels below its resting place the card rises from.")]
        public float startY = -22;

        [Header("Scrim")]
        [Range(0, 1), Tooltip("How far the board is dimmed behind the card.")]
        public float opacity = .85f;
        [Min(0)] public float fadeInSpeed = 4;
        [Min(0)] public float fadeOutSpeed = 6;

        [Header("Layout")]
        [Min(1)] public float width = 660;
        public Vector2 heights = new(520, 448);
        public Vector2 eyebrowY = new(202, 168);
        public Vector2 titleY = new(150, 116);
        public float tallyY = 40;
        public Vector2 bodyY = new(80, -34);
        public Vector2 primaryButtonY = new(-158, -130);
        public Vector2 keyHintY = new(-213, -186);
        public float instructionsY = -32;
    }
}
