using UnityEngine;

namespace GardenSnake.Presentation.Hud
{
    /// <summary>Everything the game says, and the scores that decide which verdict a run gets.</summary>
    [CreateAssetMenu(menuName = "Garden Snake/HUD Copy")]
    public sealed class HudCopySettings : ScriptableObject
    {
        [Header("Readouts")]
        public string bestPrefix = "BEST ";
        public string bestResultPrefix = "Best so far: ";

        [Header("Ready")]
        public string readyEyebrow = "A SMALL GARDEN, A BIG APPETITE";
        public string readyTitle = "Room to grow";
        [TextArea] public string readyBody = "Eat apples to grow longer.\nStay off the edges and your own tail.";
        public string playLabel = "PLAY";

        [Header("Paused")]
        public string pausedEyebrow = "TAKE A BREATHER";
        public string pausedTitle = "On a leaf break";
        [TextArea] public string pausedBody = "The garden will be right here\nwhenever you are ready.";
        public string resumeLabel = "RESUME";

        [Header("Results")]
        public string wonEyebrow = "WHAT A HARVEST";
        public string recordEyebrow = "A NEW PERSONAL BEST";
        public string lostEyebrow = "ONE MORE LITTLE GO?";
        public string wonTitle = "Garden complete";
        public string replayLabel = "PLAY AGAIN";

        [Header("Verdicts")]
        public string startingVerdict = "Off to a start";
        public string decentVerdict = "A decent little run";
        public string goodVerdict = "That was a good one";
        public string magnificentVerdict = "A magnificent run";
        [Min(0)] public int decentScore = 5;
        [Min(0)] public int goodScore = 12;
        [Min(0)] public int magnificentScore = 25;
    }
}
