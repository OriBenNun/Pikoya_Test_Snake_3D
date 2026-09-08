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
        [SerializeField] private MMF_Player pickup;
        [SerializeField] private MMF_Player death;
        [SerializeField] private MMF_Player runStart;
        [SerializeField] private MMF_Player newBest;
        [SerializeField] private MMF_Player turn;

        /// <summary>Pickups escalate: the tenth apple should land harder than the first.</summary>
        public void Pickup(Vector3 at, int score) => Play(pickup, at, Mathf.Lerp(.75f, 1.5f, Mathf.Clamp01(score / 14f)));

        public void Death(Vector3 at) => Play(death, at, 1f);

        public void RunStart(Vector3 at) => Play(runStart, at, 1f);

        public void NewBest(Vector3 at) => Play(newBest, at, 1f);

        public void Turn(Vector3 at) => Play(turn, at, 1f);

        public void StopAll()
        {
            foreach (MMF_Player player in new[] { pickup, death, runStart, newBest, turn })
                if (player != null) player.StopFeedbacks();
        }

        private static void Play(MMF_Player player, Vector3 at, float intensity)
        {
            if (player == null) return;
            player.PlayFeedbacks(at, intensity);
        }
    }
}
