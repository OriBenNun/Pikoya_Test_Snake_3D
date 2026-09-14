using UnityEngine;

namespace GardenSnake.Garden
{
    /// <summary>
    /// The turtle: it rolls gently as it walks, and it is the one animal that answers a death by
    /// pulling head and legs into its shell instead of running.
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/Wildlife/Turtle")]
    public sealed class TurtleSettings : CrawlerSettings
    {
        [Header("Roll")]
        [Min(0f)] public float rollFrequency = 5f;
        [Tooltip("Degrees the shell rocks side to side as it walks.")]
        public float roll = 2f;

        [Header("Hiding")]
        [Min(.001f), Tooltip("Seconds it stays in its shell after a death.")]
        public float hideSeconds = 2.5f;
        [Range(0f, 1f), Tooltip("How far the head shrinks as it is drawn in.")]
        public float hiddenHeadScale = .75f;
        public Vector3 hiddenHeadOffset = new(0f, -.06f, -.36f);
        [Range(0f, 1f), Tooltip("How far the legs draw in under the shell.")]
        public float hiddenLimbSpread = .6f;
    }
}
