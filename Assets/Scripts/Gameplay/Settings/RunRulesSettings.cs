using UnityEngine;

namespace GardenSnake.Gameplay
{
    /// <summary>
    /// The rules of a run. Read when the game starts; edit outside Play Mode.
    /// <para>
    /// The size of the board is not here: the scene carries one authored grid of patches and
    /// nothing rebuilds it, so it lives with the code that assumes it - see
    /// <see cref="GameLoopManager.Columns"/>.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/Run Rules")]
    public sealed class RunRulesSettings : ScriptableObject
    {
        [Header("Rules")]
        [Min(2), Tooltip("Body parts the snake starts a run with.")]
        public int initialLength = 3;
        [Range(1, 8), Tooltip("Turns the game remembers while the snake finishes crossing a cell.")]
        public int turnBufferSize = 2;
        [Min(1), Tooltip("Cells straight ahead the first apple of a run sits, so the first swipe is obvious.")]
        public int firstAppleDistance = 2;

        [Header("Frame")]
        [Min(1)] public int targetFrameRate = 60;
        [Min(0), Tooltip("Seconds after a run ends before pressing again restarts it, so the card lands first.")]
        public float restartDelay = .35f;
    }
}
