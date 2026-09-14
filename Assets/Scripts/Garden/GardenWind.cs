using System;
using System.Collections.Generic;
using UnityEngine;

namespace GardenSnake.Garden
{
    /// <summary>Position-phased wind and travelling feedback gusts, with rooted vegetation pivots.</summary>
    public sealed class GardenWind : GardenDweller
    {
        [Header("Vegetation")]
        [SerializeField, Tooltip("The plants that sway. Anything the garden names as a plant is added to these on start.")]
        private Transform[] stems;
        [Header("Wind")]
        [SerializeField, Range(0f, 20f), Tooltip("How far the meadow leans in the wind. 0 stills it completely.")]
        private float amplitude = 7f;
        [SerializeField, Min(0f), Tooltip("How hard the meadow snaps when a beat reaches it. 0 ignores the game.")]
        private float pulseAmplitude = 12f;

        // Which objects in the scene count as vegetation. A trunk never becomes a flower, so the
        // meadow is collected and weighed once.
        private static readonly string[] PlantNames =
            { "Tree", "Bush", "Tulip", "Daisy", "Lavender", "Flower", "Grass tuft" };
        private const string SproutPrefix = "Sprout";
        private const float TreeWeight = .22f;
        private const float BushWeight = .45f;
        private const float PlantWeight = 1f;

        // The shape of the wind: one slow travelling wave, a second faster one across it, and
        // Perlin turbulence over the top, all gated by a gust that comes and goes.
        private const float Frequency = .9f;
        private const float SpatialScale = .2f;
        private const float GustDepth = .45f;
        private const float GustFrequency = .23f;
        private const float FrequencyVariation = .16f;
        private const float VariationSpatialScale = 3.71f;
        private const float TurbulenceFrequency = .3f;
        private const float TurbulenceStrength = 1.2f;
        private const float NoiseOffset = 40f;
        private const float PrimarySway = .65f;
        private const float SecondarySway = .2f;
        private const float SecondaryFrequency = 1.73f;
        private const float SecondarySpatialScale = 1.3f;
        private static readonly Vector2 PhaseDirection = new(1, .6f);
        private static readonly Vector2 LeanDirection = new(1, .55f);

        // How a beat travels out through the meadow and dies away.
        private const float PropagationSpeed = 30f;
        private const float DistanceFalloff = .025f;
        /// <summary>A death pulls the meadow the other way, so the garden recoils with the snake.</summary>
        private const float DeathStrength = -1.5f;
        private const float PulseDuration = 2f;
        private const float PulseFrequency = 11f;
        private const float PulseDecay = 3f;

        private Quaternion[] rest;
        private float[] pulseAt;
        private float[] pulseStrength;
        private float[] weight;
        public int MovingCount => stems == null ? 0 : stems.Length;

        private void Awake()
        {
            var plants = new HashSet<Transform>();
            if (stems != null) foreach (var stem in stems) if (stem != null) plants.Add(stem);
            foreach (var candidate in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (candidate.gameObject.scene != gameObject.scene) continue;
                string plant = candidate.name;
                if (Array.IndexOf(PlantNames, plant) >= 0 || plant.StartsWith(SproutPrefix)) plants.Add(candidate);
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
                string plant = stems[i].name;
                weight[i] = plant == "Tree" ? TreeWeight : plant == "Bush" ? BushWeight : PlantWeight;
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
            if (pulseAmplitude <= 0) return;
            float signed = strength * (beat == Beat.Death ? DeathStrength : 1f);
            for (int i = 0; i < stems.Length; i++)
            {
                if (stems[i] == null) continue;
                float distance = Vector3.Distance(stems[i].position, at);
                pulseAt[i] = Time.time + TravelTime(distance, PropagationSpeed);
                pulseStrength[i] = Carried(signed, distance, DistanceFalloff);
            }
        }

        private void Update()
        {
            float clock = Time.time * Frequency;
            float gust = 1 - GustDepth + GustDepth * (.5f + .5f * Mathf.Sin(Time.time * GustFrequency));
            for (int i = 0; i < stems.Length; i++)
            {
                if (stems[i] == null) continue;
                Vector3 position = stems[i].position;
                // Phase by where a plant stands, so the gust travels across the meadow.
                float phase = (position.x * PhaseDirection.x + position.z * PhaseDirection.y) * SpatialScale;
                float localClock = clock * (1f + FrequencyVariation * Mathf.Sin(phase * VariationSpatialScale));
                float turbulence = (Mathf.PerlinNoise(phase + NoiseOffset, Time.time * TurbulenceFrequency) - .5f)
                    * TurbulenceStrength;
                float sway = Mathf.Sin(localClock + phase) * PrimarySway + turbulence
                    + SecondarySway * Mathf.Sin(localClock * SecondaryFrequency + phase * SecondarySpatialScale);
                float age = Time.time - pulseAt[i];
                float pulse = age >= 0 && age < PulseDuration
                    ? Mathf.Sin(age * PulseFrequency) * Mathf.Exp(-age * PulseDecay) * pulseStrength[i] * pulseAmplitude
                    : 0;
                float lean = (sway * amplitude * gust + pulse) * weight[i];
                stems[i].localRotation = rest[i] * Quaternion.Euler(lean * LeanDirection.x, 0, lean * LeanDirection.y);
            }
        }
    }
}
