using UnityEngine;

namespace GardenSnake.Presentation
{
    /// <summary>
    /// How the apple behaves once the rules have decided where it is: the drop in, the breathing,
    /// the spin, and the pool of light it sits in. Edit outside Play Mode.
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/Apple Motion")]
    public sealed class AppleMotionSettings : ScriptableObject
    {
        [Header("Size")]
        [Range(.8f, 1.6f), Tooltip("How big the apple is drawn against a patch of the board.")]
        public float scale = 1.16f;
        [Min(0)] public float breathFrequency = 3.2f;
        [Min(0), Tooltip("How much of its size the apple breathes in and out.")]
        public float breathScale = .04f;

        [Header("Arrival")]
        [Min(.01f), Tooltip("Seconds a fresh apple takes to settle into its cell.")]
        public float duration = .34f;
        [Min(.1f)] public float easePower = 3f;
        [Min(0), Tooltip("How much the arrival wobbles before it settles.")]
        public float oscillation = 9f;
        [Min(0), Tooltip("How far it overshoots its size on the way in.")]
        public float swell = .55f;
        [Min(0), Tooltip("How far above the cell it drops in from.")]
        public float dropHeight = .5f;

        [Header("Hover")]
        [Min(0), Tooltip("How far the apple floats above the grass.")]
        public float hoverHeight = .14f;
        [Min(0), Tooltip("How far it bobs as it breathes.")]
        public float bobAmplitude = .07f;
        [Tooltip("Degrees per second it turns on the spot.")]
        public float spinSpeed = 34f;
        [Min(0)] public float rockFrequency = 2.1f;
        [Tooltip("Degrees it rocks side to side.")]
        public float rockAngle = 6f;

        [Header("Marker")]
        [Min(0), Tooltip("How far above the grass the pool of light is drawn.")]
        public float height = .04f;
        [Min(0)] public float markerScale = 2.1f;
        [Min(0), Tooltip("How much the pool of light pulses with the apple.")]
        public float pulse = .18f;
    }
}
