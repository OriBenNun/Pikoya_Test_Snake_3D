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

        /// <summary>Pickups escalate: the tenth apple should land harder than the first.</summary>
        public void Pickup(Vector3 at, int score) => Play(Beat.Pickup, pickup, at, Mathf.Lerp(.75f, 1.5f, Mathf.Clamp01(score / 14f)));

        public void Death(Vector3 at) => Play(Beat.Death, death, at, 1f);

        public void RunStart(Vector3 at) => Play(Beat.RunStart, runStart, at, 1f);

        public void NewBest(Vector3 at) => Play(Beat.NewBest, newBest, at, 1f);

        public void RecordApple(Vector3 at) => Play(Beat.RecordApple, recordApple, at, 1f);

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
