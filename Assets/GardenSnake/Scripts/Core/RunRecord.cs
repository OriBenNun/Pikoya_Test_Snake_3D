namespace GardenSnake.Core
{
    public enum RecordBeat { None, Broken, Extended }

    /// <summary>Compares apples against the record that existed before this run.</summary>
    public sealed class RunRecord
    {
        public int PreviousBest { get; private set; }
        public bool Eligible { get; private set; }
        public bool Broken { get; private set; }
        private int lastScore;

        public void Begin(int previousBest, bool hasPlayed)
        {
            PreviousBest = previousBest;
            Eligible = hasPlayed;
            Broken = false;
            lastScore = 0;
        }

        public RecordBeat Apple(int score)
        {
            if (score <= lastScore) return RecordBeat.None;
            lastScore = score;
            if (!Eligible || score <= PreviousBest) return RecordBeat.None;
            if (Broken) return RecordBeat.Extended;
            Broken = true;
            return RecordBeat.Broken;
        }
    }
}
