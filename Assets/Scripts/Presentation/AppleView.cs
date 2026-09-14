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
        [Header("Scene")]
        [SerializeField] private GameLoopManager loop;
        [SerializeField] private GridCellWaves board;
        [Header("Proportions")]
        [SerializeField, Range(.8f, 1.6f)] private float appleScale = 1.16f;

        // The fruit's character, not settings.
        private const float BreathFrequency = 3.2f;
        private const float BreathScale = .04f;
        private const float ArrivalDuration = .34f;
        private const float ArrivalEasePower = 3f;
        private const float ArrivalOscillation = 9f;
        private const float ArrivalSwell = .55f;
        private const float HoverHeight = .14f;
        private const float BobAmplitude = .07f;
        private const float DropHeight = .5f;
        private const float SpinSpeed = 34f;
        private const float RockFrequency = 2.1f;
        private const float RockAngle = 6f;
        private const float MarkerHeight = .04f;
        private const float MarkerScale = 2.1f;
        private const float MarkerPulse = .18f;

        private Transform apple;
        private float age;
        private bool shown = true;
        private bool markerShown = true;

        /// <summary>The apple currently on the board.</summary>
        public Transform Apple => apple;

        /// <summary>The model a swallowed copy is made from.</summary>
        public GameObject Prefab => applePrefab;

        private void Awake()
        {
            apple = Instantiate(applePrefab, transform).transform;
        }

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
            Vector3 cell = loop.World(loop.Food);
            float lift = board.HeightAt(cell);
            float breathe = Mathf.Sin(Time.unscaledTime * BreathFrequency);
            // A fresh apple drops in with a little overshoot rather than blinking into place.
            float arrival = Mathf.Clamp01(age / ArrivalDuration);
            float pop = arrival >= 1
                ? 1
                : 1 - Mathf.Pow(1 - arrival, ArrivalEasePower) * Mathf.Cos(arrival * ArrivalOscillation) * ArrivalSwell;
            apple.position = cell + Vector3.up *
                (lift + HoverHeight + breathe * BobAmplitude + (1 - arrival) * DropHeight);
            apple.rotation = Quaternion.Euler(0, Time.unscaledTime * SpinSpeed,
                Mathf.Sin(Time.unscaledTime * RockFrequency) * RockAngle);
            apple.localScale = Vector3.one * (appleScale * (1 + breathe * BreathScale) * pop);
            if (markerShown != shown)
            {
                markerShown = shown;
                marker.gameObject.SetActive(shown);
            }
            marker.position = cell + Vector3.up * (MarkerHeight + lift);
            marker.localScale = Vector3.one * ((MarkerScale + breathe * MarkerPulse) * arrival);
        }
    }
}
