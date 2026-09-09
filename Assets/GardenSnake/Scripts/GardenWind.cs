using System.Collections.Generic;
using UnityEngine;

namespace GardenSnake
{
    /// <summary>Position-phased wind and travelling feedback gusts, with rooted vegetation pivots.</summary>
    public sealed class GardenWind : MonoBehaviour
    {
        [SerializeField] private Transform[] stems;
        [SerializeField, Range(0f, 20f)] private float amplitude = 7f;
        [SerializeField, Range(0f, 3f)] private float frequency = .9f;
        [SerializeField, Range(0f, 1f)] private float spatialScale = .2f;
        [SerializeField, Range(0f, 1f)] private float gustDepth = .45f;
        private Quaternion[] rest;
        private float[] phases, weights, pulseAt, pulseStrength;
        private SnakeFeel feel;
        public int MovingCount => stems == null ? 0 : stems.Length;
        public int ReactionCount { get; private set; }

        private void Awake()
        {
            var plants = new HashSet<Transform>();
            if (stems != null) foreach (var stem in stems) if (stem != null) plants.Add(stem);
            foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.gameObject.scene != gameObject.scene) continue;
                string n = t.name;
                if (n == "Tree" || n == "Bush" || n == "Tulip" || n == "Daisy" ||
                    n == "Lavender" || n == "Flower" || n == "Grass tuft" || n.StartsWith("Sprout")) plants.Add(t);
            }
            stems = new Transform[plants.Count];
            plants.CopyTo(stems);
            rest = new Quaternion[stems.Length];
            phases = new float[stems.Length]; weights = new float[stems.Length];
            pulseAt = new float[stems.Length]; pulseStrength = new float[stems.Length];
            for (int i = 0; i < stems.Length; i++)
            {
                rest[i] = stems[i].localRotation;
                Vector3 p = stems[i].position;
                phases[i] = (p.x + p.z * .6f) * spatialScale;
                weights[i] = stems[i].name == "Tree" ? .22f : stems[i].name == "Bush" ? .45f : 1f;
            }
        }

        private void OnEnable()
        {
            feel = FindFirstObjectByType<SnakeFeel>();
            if (feel != null) feel.Reacted += React;
        }

        private void OnDisable()
        {
            if (feel != null) feel.Reacted -= React;
            if (rest == null) return;
            for (int i = 0; i < stems.Length; i++) if (stems[i] != null) stems[i].localRotation = rest[i];
        }

        private void React(SnakeFeel.Beat beat, Vector3 at, float strength)
        {
            ReactionCount++;
            for (int i = 0; i < stems.Length; i++)
            {
                if (stems[i] == null) continue;
                float distance = Vector3.Distance(stems[i].position, at);
                pulseAt[i] = Time.time + distance / 24f;
                pulseStrength[i] = strength * (beat == SnakeFeel.Beat.Death ? -1.5f : 1f) / (1f + distance * .025f);
            }
        }

        private void Update()
        {
            float clock = Time.time * frequency;
            float gust = 1 - gustDepth + gustDepth * (.5f + .5f * Mathf.Sin(Time.time * .23f));
            for (int i = 0; i < stems.Length; i++)
            {
                if (stems[i] == null) continue;
                float phase = phases[i];
                float localClock = clock * (1f + .16f * Mathf.Sin(phase * 3.71f));
                float turbulence = (Mathf.PerlinNoise(phase + 40, Time.time * .3f) - .5f) * 1.2f;
                float sway = Mathf.Sin(localClock + phase) * .65f + turbulence + .2f * Mathf.Sin(localClock * 1.73f + phase * 1.3f);
                float age = Time.time - pulseAt[i];
                float pulse = age >= 0 && age < 2 ? Mathf.Sin(age * 11) * Mathf.Exp(-age * 3) * pulseStrength[i] * 12 : 0;
                float lean = (sway * amplitude * gust + pulse) * weights[i];
                stems[i].localRotation = rest[i] * Quaternion.Euler(lean, 0, lean * .55f);
            }
        }
    }
}
