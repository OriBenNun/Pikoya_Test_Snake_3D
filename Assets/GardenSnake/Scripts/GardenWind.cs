using UnityEngine;

namespace GardenSnake
{
    /// <summary>
    /// Sways every flower in the meadow from one clock. The phase comes from world position, so
    /// gusts travel across the garden instead of every stem nodding in unison.
    /// </summary>
    public sealed class GardenWind : MonoBehaviour
    {
        [SerializeField] private Transform[] stems;
        [SerializeField, Range(0f, 20f)] private float amplitude = 7f;
        [SerializeField, Range(0f, 3f)] private float frequency = .9f;
        [SerializeField, Range(0f, 1f)] private float spatialScale = .2f;
        [SerializeField, Range(0f, 1f)] private float gustDepth = .45f;

        private Quaternion[] rest;
        private float[] phases;

        private void Awake()
        {
            rest = new Quaternion[stems.Length];
            phases = new float[stems.Length];
            for (int i = 0; i < stems.Length; i++)
            {
                rest[i] = stems[i].localRotation;
                Vector3 position = stems[i].position;
                phases[i] = (position.x + position.z * .6f) * spatialScale;
            }
        }

        private void Update()
        {
            float clock = Time.time * frequency;
            float gust = 1 - gustDepth + gustDepth * (.5f + .5f * Mathf.Sin(Time.time * .23f));
            for (int i = 0; i < stems.Length; i++)
            {
                float phase = phases[i];
                float sway = Mathf.Sin(clock + phase) + .34f * Mathf.Sin(clock * 1.73f + phase * 1.3f);
                float lean = sway * amplitude * gust;
                stems[i].localRotation = rest[i] * Quaternion.Euler(lean, 0, lean * .55f);
            }
        }
    }
}
