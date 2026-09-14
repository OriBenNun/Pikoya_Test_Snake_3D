using System;
using UnityEngine;

namespace GardenSnake
{
    /// <summary>The moments a player can feel. Everything decorative listens for these.</summary>
    public enum Beat { Pickup, Death, RunStart, NewBest, RecordApple }

    /// <summary>
    /// One broadcast channel for reaction beats. The feedback layer strikes them; the garden's
    /// scenery listens. Neither side holds a reference to the other, so a beat can reach a
    /// flower or a bird without anything in the game knowing that either exists.
    /// </summary>
    public static class GardenBeats
    {
        /// <summary>The beat, where in the world it happened, and how hard it landed.</summary>
        public static event Action<Beat, Vector3, float> Struck;

        public static void Strike(Beat beat, Vector3 at, float strength) => Struck?.Invoke(beat, at, strength);

        // Static events outlive Play Mode when the editor keeps the domain loaded, which would
        // leave every listener from the previous session attached to the next one.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Clear() => Struck = null;
    }
}
