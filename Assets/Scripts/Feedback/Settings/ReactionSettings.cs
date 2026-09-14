using UnityEngine;

namespace GardenSnake
{
    /// <summary>
    /// The reactions the feedback layer draws itself: the ring a picked apple throws, the sparkle
    /// behind the head, the vignette that closes in with the pace, and the puff of a death.
    /// Edit outside Play Mode.
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/Reactions")]
    public sealed class ReactionSettings : ScriptableObject
    {
        [Header("Pickup ring")]
        [Min(.01f)] public float burstDuration = .42f;
        [Min(.1f), Tooltip("Shapes the expansion. Higher throws the ring out faster and eases harder.")]
        public float burstEasePower = 2.6f;
        [Min(0)] public float burstStartScale = .6f;
        [Min(0)] public float burstEndScale = 2.5f;
        [Range(0, 1)] public float burstOpacity = .55f;
        [Min(0), Tooltip("How far above the grass the ring is drawn.")]
        public float burstHeight = .05f;

        [Header("Head trail")]
        [Min(0), Tooltip("Particles per second at the opening pace.")]
        public float trailEmissionSlow = 10f;
        [Min(0), Tooltip("Particles per second at full pace.")]
        public float trailEmissionFast = 30f;
        [Min(0)] public float trailHeight = .12f;

        [Header("Pace vignette")]
        [Min(0), Tooltip("How quickly the vignette follows the pace in and out.")]
        public float paceVolumeBlendSpeed = 1.2f;

        [Header("Death")]
        [Min(0)] public int deathParticleCount = 18;
        [Min(0)] public float deathParticleHeight = .3f;
    }
}
