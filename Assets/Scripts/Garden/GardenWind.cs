using System;
using System.Collections.Generic;
using UnityEngine;

namespace GardenSnake
{
    /// <summary>Position-phased wind and travelling feedback gusts, with rooted vegetation pivots.</summary>
    public sealed class GardenWind : GardenDweller
    {
        [Header("Vegetation (collected on entering Play Mode)")]
        [SerializeField] private Transform[] stems;
        [SerializeField] private bool discoverPlants = true;
        [SerializeField] private string[] plantNames = { "Tree", "Bush", "Tulip", "Daisy", "Lavender", "Flower", "Grass tuft" };
        [SerializeField] private string sproutPrefix = "Sprout";
        [SerializeField, Min(0)] private float treeWeight = .22f;
        [SerializeField, Min(0)] private float bushWeight = .45f;
        [SerializeField, Min(0)] private float plantWeight = 1f;
        [Header("Wind (live tuning)")]
        [SerializeField, Range(0f, 20f)] private float amplitude = 7f;
        [SerializeField, Range(0f, 3f)] private float frequency = .9f;
        [SerializeField, Range(0f, 1f)] private float spatialScale = .2f;
        [SerializeField, Range(0f, 1f)] private float gustDepth = .45f;
        [SerializeField] private Vector2 phaseDirection = new(1, .6f);
        [SerializeField] private Vector2 leanDirection = new(1, .55f);
        [SerializeField, Min(0)] private float gustFrequency = .23f;
        [SerializeField, Range(0, 1)] private float frequencyVariation = .16f;
        [SerializeField] private float variationSpatialScale = 3.71f;
        [SerializeField, Min(0)] private float turbulenceFrequency = .3f;
        [SerializeField, Min(0)] private float turbulenceStrength = 1.2f;
        [SerializeField] private float noiseOffset = 40f;
        [SerializeField, Min(0)] private float primarySway = .65f;
        [SerializeField, Min(0)] private float secondarySway = .2f;
        [SerializeField, Min(0)] private float secondaryFrequency = 1.73f;
        [SerializeField] private float secondarySpatialScale = 1.3f;
        [Header("Feedback gusts")]
        [SerializeField] private bool reactToFeedback = true;
        [SerializeField, Min(.1f)] private float propagationSpeed = 24f;
        [SerializeField, Min(0)] private float distanceFalloff = .025f;
        [SerializeField] private float deathStrength = -1.5f;
        [SerializeField, Min(0)] private float pulseDuration = 2f;
        [SerializeField, Min(0)] private float pulseFrequency = 11f;
        [SerializeField, Min(0)] private float pulseDecay = 3f;
        [SerializeField, Min(0)] private float pulseAmplitude = 12f;
        private Quaternion[] rest;
        private float[] pulseAt;
        private float[] pulseStrength;
        private float[] weight;
        public int MovingCount => stems == null ? 0 : stems.Length;

        private void Awake()
        {
            var plants = new HashSet<Transform>();
            if (stems != null) foreach (var stem in stems) if (stem != null) plants.Add(stem);
            if (discoverPlants) foreach (var candidate in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (candidate.gameObject.scene != gameObject.scene) continue;
                string plant = candidate.name;
                if ((plantNames != null && Array.IndexOf(plantNames, plant) >= 0) ||
                    (!string.IsNullOrEmpty(sproutPrefix) && plant.StartsWith(sproutPrefix))) plants.Add(candidate);
            }
            stems = new Transform[plants.Count];
            plants.CopyTo(stems);
            rest = new Quaternion[stems.Length];
            pulseAt = new float[stems.Length];
            pulseStrength = new float[stems.Length];
            weight = new float[stems.Length];
            for (int i = 0; i < stems.Length; i++)
            {
                rest[i] = stems[i].localRotation;
                // Reading Transform.name marshals a fresh string out of the engine every time.
                // A trunk never becomes a flower, so weigh the whole meadow once and keep it.
                string plant = stems[i].name;
                weight[i] = plant == "Tree" ? treeWeight : plant == "Bush" ? bushWeight : plantWeight;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (rest == null) return;
            for (int i = 0; i < stems.Length; i++) if (stems[i] != null) stems[i].localRotation = rest[i];
        }

        protected override void React(Beat beat, Vector3 at, float strength)
        {
            if (!reactToFeedback) return;
            // A death pulls the meadow the other way, so the whole garden recoils with the snake.
            float signed = strength * (beat == Beat.Death ? deathStrength : 1f);
            for (int i = 0; i < stems.Length; i++)
            {
                if (stems[i] == null) continue;
                float distance = Vector3.Distance(stems[i].position, at);
                pulseAt[i] = Time.time + TravelTime(distance, propagationSpeed);
                pulseStrength[i] = Carried(signed, distance, distanceFalloff);
            }
        }

        private void Update()
        {
            float clock = Time.time * frequency;
            float gust = 1 - gustDepth + gustDepth * (.5f + .5f * Mathf.Sin(Time.time * gustFrequency));
            for (int i = 0; i < stems.Length; i++)
            {
                if (stems[i] == null) continue;
                Vector3 position = stems[i].position;
                // Phase by where a plant stands, so the gust travels across the meadow.
                float phase = (position.x * phaseDirection.x + position.z * phaseDirection.y) * spatialScale;
                float localClock = clock * (1f + frequencyVariation * Mathf.Sin(phase * variationSpatialScale));
                float turbulence = (Mathf.PerlinNoise(phase + noiseOffset, Time.time * turbulenceFrequency) - .5f)
                    * turbulenceStrength;
                float sway = Mathf.Sin(localClock + phase) * primarySway + turbulence
                    + secondarySway * Mathf.Sin(localClock * secondaryFrequency + phase * secondarySpatialScale);
                float age = Time.time - pulseAt[i];
                bool pulsing = reactToFeedback && age >= 0 && age < pulseDuration;
                float pulse = pulsing
                    ? Mathf.Sin(age * pulseFrequency) * Mathf.Exp(-age * pulseDecay) * pulseStrength[i] * pulseAmplitude
                    : 0;
                float lean = (sway * amplitude * gust + pulse) * weight[i];
                stems[i].localRotation = rest[i] * Quaternion.Euler(lean * leanDirection.x, 0, lean * leanDirection.y);
            }
        }
    }
}
