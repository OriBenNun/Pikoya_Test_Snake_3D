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
        [Min(1), Tooltip("Pickups climb a short scale, then start it again.")]
        public int pickupPitchCycle = 6;
        [Min(0), Tooltip("How far up that scale each apple steps.")]
        public float pickupPitchIncrement = .045f;
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
    }
}
