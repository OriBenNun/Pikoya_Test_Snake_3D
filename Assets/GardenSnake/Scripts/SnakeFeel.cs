using MoreMountains.Feedbacks;
using UnityEngine;

namespace GardenSnake
{
    /// <summary>
    /// The game's reaction layer. Every beat the player can feel - a pickup, a death, a new best -
    /// is one Feel player, so the simulation only says what happened and never how loud it looks.
    /// </summary>
    public sealed class SnakeFeel : MonoBehaviour
    {
        public enum Beat { Pickup, Death, RunStart, NewBest, RecordApple }
        public event System.Action<Beat, Vector3, float> Reacted;
        [SerializeField] private MMF_Player pickup;
        [SerializeField] private MMF_Player death;
        [SerializeField] private MMF_Player runStart;
        [SerializeField] private MMF_Player newBest;
        [SerializeField] private MMF_Player recordApple;
        [Header("Beat intensity")]
        [SerializeField, Tooltip("Intensity of the first pickup and a fully escalated pickup.")]
        private Vector2 pickupIntensity = new Vector2(.75f, 1.5f);
        [SerializeField, Min(1)] private int fullPickupIntensityScore = 14;
        [SerializeField, Min(0)] private float deathIntensity = 1;
        [SerializeField, Min(0)] private float runStartIntensity = 1;
        [SerializeField, Min(0)] private float newBestIntensity = 1;
        [SerializeField, Min(0)] private float recordAppleIntensity = 1;

        /// <summary>Pickups escalate: the tenth apple should land harder than the first.</summary>
        public void Pickup(Vector3 at, int score) => Play(Beat.Pickup, pickup, at,
            Mathf.Lerp(pickupIntensity.x, pickupIntensity.y, Mathf.Clamp01(score / (float)Mathf.Max(1, fullPickupIntensityScore))));

        public void Death(Vector3 at) => Play(Beat.Death, death, at, deathIntensity);

        public void RunStart(Vector3 at) => Play(Beat.RunStart, runStart, at, runStartIntensity);

        public void NewBest(Vector3 at) => Play(Beat.NewBest, newBest, at, newBestIntensity);

        public void RecordApple(Vector3 at) => Play(Beat.RecordApple, recordApple, at, recordAppleIntensity);

        public void StopAll()
        {
            foreach (MMF_Player player in new[] { pickup, death, runStart, newBest, recordApple })
                if (player != null) player.StopFeedbacks();
        }

        private void Play(Beat beat, MMF_Player player, Vector3 at, float intensity)
        {
            Reacted?.Invoke(beat, at, intensity);
            if (player == null) return;
            player.PlayFeedbacks(at, intensity);
        }
    }
}
