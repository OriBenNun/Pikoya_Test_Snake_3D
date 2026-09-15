using GardenSnake.Gameplay;
using UnityEngine;

namespace GardenSnake.Presentation
{
    /// <summary>
    /// The apple and the pool of light under it. It drops in with a little overshoot, breathes,
    /// spins and rocks, and rides whatever the board is doing beneath it. The cell it belongs to
    /// is the simulation's business; this only draws it.
    /// </summary>
    public sealed class AppleView : MonoBehaviour
    {
        [Header("Models")]
        [SerializeField] private GameObject applePrefab;
        [SerializeField] private Transform marker;
        [Header("Tuning")]
        [SerializeField] private AppleMotionSettings motion;
        [Header("Scene")]
        [SerializeField] private GameLoopManager loop;
        [SerializeField] private GridCellWaves board;

        private Transform apple;
        private float age;
        private bool shown = true;
        private bool markerShown = true;

        public Transform Apple => apple;

        /// <summary>The model a swallowed copy is made from.</summary>
        public GameObject Prefab => applePrefab;

        /// <summary>A fresh apple has been placed; move to its cell and drop in from above.</summary>
        public void Respawn()
        {
            age = 0;
            apple.position = loop.World(loop.Food);
        }

        /// <summary>The first apple of a run. It arrives the same way any other one does.</summary>
        public void Settle() => Respawn();

        public void Show(bool visible)
        {
            if (shown == visible) return;
            shown = visible;
            apple.gameObject.SetActive(visible);
        }

        /// <summary>Driven by <see cref="SnakeManager"/> so the mouth always reads a settled apple.</summary>
        public void Animate()
        {
            age += Time.unscaledDeltaTime;
            var cell = loop.World(loop.Food);
            var lift = board.HeightAt(cell);
            var breathe = Mathf.Sin(Time.unscaledTime * motion.breathFrequency);
            // A fresh apple drops in with a little overshoot rather than blinking into place.
            var arrival = Mathf.Clamp01(age / Mathf.Max(.01f, motion.duration));
            var pop = arrival >= 1
                ? 1
                : 1 - Mathf.Pow(1 - arrival, motion.easePower) * Mathf.Cos(arrival * motion.oscillation) * motion.swell;
            apple.position = cell + Vector3.up *
                (lift + motion.hoverHeight + breathe * motion.bobAmplitude + (1 - arrival) * motion.dropHeight);
            apple.rotation = Quaternion.Euler(0, Time.unscaledTime * motion.spinSpeed,
                Mathf.Sin(Time.unscaledTime * motion.rockFrequency) * motion.rockAngle);
            apple.localScale = Vector3.one * (motion.scale * (1 + breathe * motion.breathScale) * pop);
            if (markerShown != shown)
            {
                markerShown = shown;
                marker.gameObject.SetActive(shown);
            }
            marker.position = cell + Vector3.up * (motion.height + lift);
            marker.localScale = Vector3.one * ((motion.markerScale + breathe * motion.pulse) * arrival);
        }

        private void Awake()
        {
            motion = Tuning.Or(motion);
            apple = Instantiate(applePrefab, transform).transform;
        }
    }
}
