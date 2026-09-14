using GardenSnake.Presentation;
using UnityEngine;

namespace GardenSnake
{
    /// <summary>
    /// Which wave the board runs for each moment of a run, and how far each one carries.
    /// Edit outside Play Mode.
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/Wave Cues")]
    public sealed class WaveCueSettings : ScriptableObject
    {
        [Header("Patterns")]
        public GridCellWaves.Pattern start = GridCellWaves.Pattern.Sweep;
        public GridCellWaves.Pattern death = GridCellWaves.Pattern.Ripple;
        public GridCellWaves.Pattern best = GridCellWaves.Pattern.Bloom;
        public GridCellWaves.Pattern milestone = GridCellWaves.Pattern.CheckerHop;
        public GridCellWaves.Pattern victory = GridCellWaves.Pattern.Bloom;

        [Header("Strength")]
        [Min(0)] public float startIntensity = 1;
        [Min(0)] public float deathIntensity = 1;
        [Min(0)] public float bestIntensity = 1;
        [Min(0)] public float milestoneIntensity = 1;
        [Min(0)] public float victoryIntensity = 1.5f;

        [Header("Milestones")]
        [Min(1), Tooltip("Every nth apple is worth a word and a wave of its own.")]
        public int appleInterval = 10;
    }
}
