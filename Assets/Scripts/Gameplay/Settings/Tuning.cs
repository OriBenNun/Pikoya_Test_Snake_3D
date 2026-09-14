using UnityEngine;

namespace GardenSnake.Gameplay
{
    /// <summary>
    /// How every component reaches its tuning asset. A missing asset is never an error: the
    /// component gets a throwaway carrying the authored defaults, so the game still runs and plays
    /// correctly with an empty slot.
    /// </summary>
    public static class Tuning
    {
        public static T Or<T>(T settings) where T : ScriptableObject =>
            settings != null ? settings : ScriptableObject.CreateInstance<T>();
    }
}
