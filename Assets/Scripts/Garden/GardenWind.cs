using System;
using System.Collections.Generic;
using GardenSnake.Gameplay;
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

        [Header("Tuning")]
        [SerializeField] private WindSettings wind;

        // Which objects in the scene count as vegetation. Not settings: these are the names the
        // garden was authored with, and the wind finds nothing if they stop matching.
        private static readonly string[] PlantNames =
            { "Tree", "Bush", "Tulip", "Daisy", "Lavender", "Flower", "Grass tuft" };
        private const string SproutPrefix = "Sprout";

        private Quaternion[] rest;
        private float[] pulseAt;
        private float[] pulseStrength;
        private float[] weight;
        public int MovingCount => stems == null ? 0 : stems.Length;

        private void Awake()
        {
            wind = Tuning.Or(wind);
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
                weight[i] = plant == "Tree" ? wind.treeWeight : plant == "Bush" ? wind.bushWeight : wind.plantWeight;
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
            float signed = strength * (beat == Beat.Death ? wind.deathStrength : 1f);
            for (int i = 0; i < stems.Length; i++)
            {
                if (stems[i] == null) continue;
                float distance = Vector3.Distance(stems[i].position, at);
                pulseAt[i] = Time.time + TravelTime(distance, wind.propagationSpeed);
                pulseStrength[i] = Carried(signed, distance, wind.distanceFalloff);
            }
        }

        private void Update()
        {
            float clock = Time.time * wind.frequency;
            float gust = 1 - wind.gustDepth + wind.gustDepth * (.5f + .5f * Mathf.Sin(Time.time * wind.gustFrequency));
            for (int i = 0; i < stems.Length; i++)
            {
                if (stems[i] == null) continue;
                Vector3 position = stems[i].position;
                // Phase by where a plant stands, so the gust travels across the meadow.
                float phase = (position.x * wind.phaseDirection.x + position.z * wind.phaseDirection.y) * wind.spatialScale;
                float localClock = clock * (1f + wind.frequencyVariation * Mathf.Sin(phase * wind.variationSpatialScale));
                float turbulence = (Mathf.PerlinNoise(phase + wind.noiseOffset, Time.time * wind.turbulenceFrequency) - .5f)
                    * wind.turbulenceStrength;
                float sway = Mathf.Sin(localClock + phase) * wind.primarySway + turbulence
                    + wind.secondarySway * Mathf.Sin(localClock * wind.secondaryFrequency + phase * wind.secondarySpatialScale);
                float age = Time.time - pulseAt[i];
                float pulse = age >= 0 && age < wind.pulseDuration
                    ? Mathf.Sin(age * wind.pulseFrequency) * Mathf.Exp(-age * wind.pulseDecay) * pulseStrength[i] * pulseAmplitude
                    : 0;
                float lean = (sway * amplitude * gust + pulse) * weight[i];
                stems[i].localRotation = rest[i] * Quaternion.Euler(lean * wind.leanDirection.x, 0, lean * wind.leanDirection.y);
            }
        }
    }
}
