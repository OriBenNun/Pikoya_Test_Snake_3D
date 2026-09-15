using UnityEngine;

namespace GardenSnake
{
    /// <summary>
    /// The pitch and level of every one-shot, kept in one asset so the mix reads as a mix.
    /// Edit outside Play Mode.
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/Sound Mix")]
    public sealed class SoundMixSettings : ScriptableObject
    {
        [Header("Run start")]
        [Range(.1f, 3)] public float startPitch = 1;
        [Range(0, 1)] public float startVolume = .5f;

        [Header("Turn")]
        [Tooltip("Each turn picks a pitch between these, so a run of them never sounds mechanical.")]
        public Vector2 turnPitch = new(.96f, 1.06f);
        [Range(0, 1)] public float turnVolume = .16f;

        [Header("Button")]
        [Range(.1f, 3)] public float clickPitch = 1;
        [Range(0, 1)] public float clickVolume = .35f;

        [Header("Pickup")]
        [Range(.1f, 3)] public float pickupPitch = 1;
        [Min(1), Tooltip("How many pickups the scale climbs before it starts again.")]
        public int pickupPitchCycle = 20;
        [Min(0), Tooltip("How far up that scale each apple steps.")]
        public float pickupPitchIncrement = .03f;
        [Range(0, 1)] public float pickupVolume = .6f;

        [Header("Record")]
        [Range(.1f, 3)] public float bestPitch = 1;
        [Range(0, 1)] public float bestVolume = .45f;
        [Range(.1f, 3), Tooltip("Pitch of the quieter chime an already-broken record gets.")]
        public float recordPitch = 1.35f;
        [Range(0, 1)] public float recordVolume = .1f;

        [Header("Death")]
        [Range(.1f, 3)] public float losePitch = 1;
        [Range(0, 1)] public float loseVolume = .55f;

        /// <summary>
        /// The pitch the apple at this score plays at: one step per apple up a scale
        /// <see cref="pickupPitchCycle"/> steps long, clamped to what an AudioSource can play.
        /// </summary>
        public float PickupPitch(int score) =>
            Mathf.Clamp(
                pickupPitch + Mathf.Max(0, score) % Mathf.Max(1, pickupPitchCycle) * pickupPitchIncrement,
                MinPitch, MaxPitch);

        /// <summary>AudioSource plays no slower or faster than this, so the scale stops here too.</summary>
        private const float MinPitch = .1f, MaxPitch = 3f;
    }
}
