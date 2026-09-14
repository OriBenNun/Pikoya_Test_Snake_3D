namespace GardenSnake.Core
{
    public enum RecordBeat { None, Broken, Extended }

    /// <summary>Compares apples against the record that existed before this run.</summary>
    public sealed class RunRecord
    {
        private int highestScored;

        public int PreviousBest { get; private set; }
        public bool Eligible { get; private set; }
        public bool Broken { get; private set; }

        public void Begin(int previousBest, bool hasPlayed)
        {
            PreviousBest = previousBest;
            Eligible = hasPlayed;
            Broken = false;
            highestScored = 0;
        }

        /// <summary>Reports the score after an apple and says whether it is worth celebrating.</summary>
        public RecordBeat Register(int score)
        {
            if (score <= highestScored) return RecordBeat.None;
            highestScored = score;
            if (!Eligible || score <= PreviousBest) return RecordBeat.None;
            if (Broken) return RecordBeat.Extended;
            Broken = true;
            return RecordBeat.Broken;
        }
    }
}
