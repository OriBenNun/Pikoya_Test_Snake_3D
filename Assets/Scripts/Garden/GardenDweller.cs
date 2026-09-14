using UnityEngine;

namespace GardenSnake.Garden
{
    /// <summary>
    /// Anything decorative that leans when the game does. It owns the one thing the meadow and the
    /// wildlife were each doing for themselves: attaching to the garden's beat channel, counting
    /// what arrives, and turning a distant beat into a delay and a diminished strength.
    /// <para>
    /// A dweller never looks the game up, and nothing in the game looks a dweller up. Switch the
    /// whole garden off and the run is unchanged.
    /// </para>
    /// </summary>
    public abstract class GardenDweller : MonoBehaviour
    {
        /// <summary>How many beats have reached this object; the audit harness reads it.</summary>
        public int ReactionCount { get; private set; }

        protected virtual void OnEnable() => GardenBeats.Struck += Receive;

        protected virtual void OnDisable() => GardenBeats.Struck -= Receive;

        private void Receive(Beat beat, Vector3 at, float strength)
        {
            ReactionCount++;
            React(beat, at, strength);
        }

        /// <summary>A beat struck somewhere in the garden. Decide what, if anything, it does here.</summary>
        protected abstract void React(Beat beat, Vector3 at, float strength);

        /// <summary>Seconds a beat takes to cross <paramref name="distance"/> at <paramref name="speed"/>.</summary>
        protected static float TravelTime(float distance, float speed) => distance / Mathf.Max(.001f, speed);

        /// <summary>What is left of a beat's strength once it has crossed that distance.</summary>
        protected static float Carried(float strength, float distance, float falloff) =>
            strength / Mathf.Max(.001f, 1 + distance * Mathf.Max(0, falloff));
    }
}
