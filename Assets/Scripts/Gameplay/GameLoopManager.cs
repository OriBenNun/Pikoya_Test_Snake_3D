using System;
using System.Collections.Generic;
using UnityEngine;

namespace GardenSnake.Gameplay
{
    /// <summary>A patch of the board. Two ints and the handful of operators a grid needs.</summary>
    public readonly struct Cell : IEquatable<Cell>
    {
        public readonly int X;
        public readonly int Y;
        public Cell(int x, int y) { X = x; Y = y; }
        public bool Equals(Cell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Cell other && Equals(other);
        public override int GetHashCode() => unchecked(X * 397 ^ Y);
        public override string ToString() => X + "," + Y;
        public static bool operator ==(Cell a, Cell b) => a.Equals(b);
        public static bool operator !=(Cell a, Cell b) => !a.Equals(b);
        public static Cell operator +(Cell a, Cell b) => new(a.X + b.X, a.Y + b.Y);
    }

    public enum Direction { Up, Right, Down, Left }
    public enum RunState { Ready, Playing, Paused, Lost, Won }
    public enum StepResult { None, Moved, Ate, Lost, Won }

    /// <summary>What an apple was worth against the record this run started with.</summary>
    public enum RecordBeat { None, Broken, Extended }

    /// <summary>What an eaten apple was worth, and where it was eaten.</summary>
    public readonly struct AppleBeat
    {
        /// <summary>The cell the apple was standing on, in world space.</summary>
        public readonly Vector3 At;
        public readonly int Score;
        public readonly RecordBeat Record;

        public AppleBeat(Vector3 at, int score, RecordBeat record)
        {
            At = at;
            Score = score;
            Record = record;
        }
    }

    /// <summary>
    /// The rules and the meta loop. It owns the snake, the board, the fixed movement step, the
    /// record that survives between runs, and the pause and sound preferences. It decides
    /// <em>what</em> happened and announces it; it never decides how any of it should look or
    /// sound.
    /// <para>
    /// Cells are <see cref="Cell"/> in board space, where (0,0) is the bottom left patch.
    /// <see cref="World"/> is the one place those become metres.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class GameLoopManager : MonoBehaviour
    {
        /// <summary>
        /// The authored board. The scene carries exactly one grid of patches, laid out row by row
        /// from the bottom left, and nothing rebuilds it - so this is the game's shape rather than
        /// a setting. Changing it here would leave the rules playing on a board the garden does
        /// not have.
        /// </summary>
        public const int Columns = 21;
        public const int Rows = 12;
        private const int Cells = Columns * Rows;
        /// <summary>One stalled browser frame must not advance the snake through several cells unseen.</summary>
        private const float MaximumCatchUp = .1f;
        private const string BestKey = "GardenSnake.Best";
        private const string PlayedKey = "GardenSnake.HasPlayed";
        private const string MutedKey = "GardenSnake.Muted";
        /// <summary>Food keeps its pickup cell as the snake slides over it: one body index per move.</summary>
        private const float DigestionPerStep = 1f;

        /// <summary>The board has no food on it at all.</summary>
        private static readonly Cell NoFood = new(-1, -1);

        [Header("Pace")]
        [SerializeField, Range(.1f, .5f), Tooltip("Seconds per cell at the start of a run. Lower is faster.")]
        private float initialStepSeconds = .25f;
        [SerializeField, Range(.06f, .3f), Tooltip("Fastest the snake ever gets, however many apples it eats.")]
        private float fastestStepSeconds = .115f;
        [SerializeField, Range(0f, .02f), Tooltip("Seconds every apple shaves off the step.")]
        private float speedGainPerApple = .0055f;
        [SerializeField, Range(0f, 1f), Tooltip("Still beat after PLAY before the first step, so the board can be read.")]
        private float openingBeat = .45f;
        [Header("Tuning")]
        [SerializeField] private RunRulesSettings rules;
        [Header("Scene")]
        [SerializeField] private PlayerController input;

        /// <summary>Anything a readout could be showing has changed.</summary>
        public event Action Changed;
        /// <summary>Raised immediately before the snake advances one cell.</summary>
        public event Action Stepping;
        /// <summary>Raised immediately after it advances, with what the step turned out to be.</summary>
        public event Action<StepResult> Stepped;
        /// <summary>A fresh run just began, with the head's world position.</summary>
        public event Action<Vector3> RunStarted;
        public event Action<AppleBeat> AppleEaten;
        /// <summary>The run is over: <see cref="StepResult.Lost"/> or <see cref="StepResult.Won"/>.</summary>
        public event Action<StepResult> RunEnded;
        /// <summary>A turn the rules accepted: -1 to the left, 1 to the right.</summary>
        public event Action<int> TurnAccepted;
        public event Action<bool> MuteChanged;
        /// <summary>The small confirmation every button press earns.</summary>
        public event Action Clicked;

        private readonly List<Cell> body = new();
        private readonly List<float> digestion = new();
        private readonly Queue<Direction> turns = new(3);
        private System.Random random;
        private float elapsed;
        private float currentStep;
        private float endTime;
        private int best;
        private int scoreAtLastApple;
        private bool bestUnsaved;
        private bool hasPlayed;
        private bool muted;

        /// <summary>Head first, tail last.</summary>
        public IReadOnlyList<Cell> Body => body;
        /// <summary>How far each swallowed apple has traveled down the body, in body indices.</summary>
        public IReadOnlyList<float> Digestion => digestion;
        public Cell Food { get; private set; }
        public bool HasFood => Food.X >= 0;
        public Direction Heading { get; private set; }
        public RunState State { get; private set; }
        public int Score { get; private set; }
        public string EndReason { get; private set; } = string.Empty;
        public int Best => best;
        public bool Muted => muted;
        /// <summary>This run has already passed the record it started with.</summary>
        public bool RecordBroken { get; private set; }
        /// <summary>Seconds per cell at the current score; the step in force may still be the last one.</summary>
        public float StepSeconds => Mathf.Max(fastestStepSeconds, initialStepSeconds - Score * speedGainPerApple);
        /// <summary>The step the snake is actually moving on right now.</summary>
        public float CurrentStep => currentStep;
        /// <summary>How far through the current step the snake is, 0 to 1.</summary>
        public float StepProgress => State is RunState.Playing or RunState.Paused
            ? Mathf.Clamp01(elapsed / currentStep) : 1;
        /// <summary>Pace at the current score, for readouts.</summary>
        public float Pace => Mathf.InverseLerp(initialStepSeconds, fastestStepSeconds, StepSeconds);
        /// <summary>Pace of the step in force, for anything that has to move with the snake.</summary>
        public float CurrentPace => Mathf.InverseLerp(initialStepSeconds, fastestStepSeconds, currentStep);

        /// <summary>The run has finished, won or lost, and is waiting on the results card.</summary>
        private bool RunOver => State is RunState.Lost or RunState.Won;
        /// <summary>The record this run is measured against, captured when it started.</summary>
        private int PreviousBest { get; set; }
        /// <summary>False on a player's very first run, when there is no record to beat yet.</summary>
        private bool RecordEligible { get; set; }

        public void Click() => Clicked?.Invoke();

        /// <summary>Where a cell sits in the world. The one place board coordinates become metres.</summary>
        public Vector3 World(Cell cell) =>
            new (cell.X - (Columns - 1) * .5f, 0, cell.Y - (Rows - 1) * .5f);

        // Lifecycle

        private void Awake()
        {
            rules = Tuning.Or(rules);
            Application.targetFrameRate = Mathf.Max(1, rules.targetFrameRate);
            Application.runInBackground = true;
            random = new System.Random(Environment.TickCount);
            body.Capacity = Cells;
            ResetRun();
            best = PlayerPrefs.GetInt(BestKey, 0);
            hasPlayed = PlayerPrefs.GetInt(PlayedKey, 0) == 1 || best > 0;
            muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
            currentStep = StepSeconds;
        }

        private void OnEnable()
        {
            if (!input) return;
            input.PrimaryRequested += PrimaryAction;
            input.PauseRequested += TogglePause;
            input.MuteRequested += ToggleMute;
            input.TurnRequested += Turn;
        }

        private void OnDisable()
        {
            if (!input) return;
            input.PrimaryRequested -= PrimaryAction;
            input.PauseRequested -= TogglePause;
            input.MuteRequested -= ToggleMute;
            input.TurnRequested -= Turn;
        }

        private void Start()
        {
            // Every layer below has bound its listeners by now; tell them where the game stands.
            MuteChanged?.Invoke(muted);
            Changed?.Invoke();
        }

        private void Update()
        {
            if (State != RunState.Playing) return;
            elapsed += Mathf.Min(Time.deltaTime, MaximumCatchUp);
            if (elapsed >= currentStep) Advance();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused) return;
            SaveBest();
            if (State == RunState.Playing) TogglePause();
        }

        private void OnApplicationQuit() => SaveBest();

        // Player actions

        private void PrimaryAction()
        {
            switch (State)
            {
                case RunState.Paused:
                    TogglePause(); return;
                case RunState.Playing:
                    return;
            }

            if (RunOver && Time.unscaledTime - endTime < rules.restartDelay) return;
            PreviousBest = best;
            RecordEligible = hasPlayed;
            RecordBroken = false;
            scoreAtLastApple = 0;
            hasPlayed = true;
            PlayerPrefs.SetInt(PlayedKey, 1);
            PlayerPrefs.Save();
            ResetRun();
            State = RunState.Playing;
            currentStep = StepSeconds;
            // A short beat before the first step gives the player time to read the board.
            elapsed = -openingBeat;
            RunStarted?.Invoke(World(body[0]));
            Changed?.Invoke();
        }

        private void Turn(Direction wish)
        {
            if (State == RunState.Ready) PrimaryAction();
            if (State != RunState.Playing || turns.Count >= rules.turnBufferSize) return;
            var last = Heading;
            foreach (var queued in turns) last = queued;
            // Reversing into the neck is not a move, and neither is turning the way you already face.
            if (wish == last || IsOpposite(wish, last)) return;
            turns.Enqueue(wish);
            // The lean is measured against the heading the snake is on now, not the one it will
            // be on by the time this turn comes out of the buffer.
            TurnAccepted?.Invoke(TurnSign(Heading, wish));
        }

        private void TogglePause()
        {
            switch (State)
            {
                case RunState.Playing:
                    State = RunState.Paused; turns.Clear();
                    break;
                case RunState.Paused:
                    State = RunState.Playing;
                    break;
            }

            Changed?.Invoke();
        }

        private void ToggleMute()
        {
            muted = !muted;
            PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
            PlayerPrefs.Save();
            MuteChanged?.Invoke(muted);
            Changed?.Invoke();
        }

        // The step

        private void Advance()
        {
            elapsed -= currentStep;
            var eatenAt = World(Food);
            Stepping?.Invoke();
            var result = Step();
            currentStep = StepSeconds;
            Stepped?.Invoke(result);
            switch (result)
            {
                case StepResult.Ate:
                {
                    RecordBeat beat = ScoreRecord();
                    UpdateBest();
                    AppleEaten?.Invoke(new AppleBeat(eatenAt, Score, beat));
                    break;
                }
                case StepResult.Lost or StepResult.Won:
                    endTime = Time.unscaledTime;
                    SaveBest();
                    RunEnded?.Invoke(result);
                    break;
            }

            Changed?.Invoke();
        }

        /// <summary>Moves the snake one cell and resolves what that did. The whole of the rules.</summary>
        private StepResult Step()
        {
            if (State != RunState.Playing) return StepResult.None;
            if (turns.Count > 0) Heading = turns.Dequeue();
            var next = body[0] + Offset(Heading);
            if (!InBounds(next)) return Lose("You reached the garden edge.");
            // Growth happens only when the oldest swallowed apple reaches the tail, so until then
            // the cell the tail is about to vacate is free to enter.
            var grow = digestion.Count > 0 && digestion[0] + DigestionPerStep >= body.Count;
            var occupied = body.Count - (grow ? 0 : 1);
            for (var i = 0; i < occupied; i++)
                if (body[i] == next) return Lose("You crossed your own tail.");
            var eat = next == Food;
            body.Insert(0, next);
            if (!grow) body.RemoveAt(body.Count - 1);
            for (var i = 0; i < digestion.Count; i++) digestion[i] += DigestionPerStep;
            if (grow) digestion.RemoveAt(0);
            if (eat) { Score++; digestion.Add(0); }
            if (body.Count == Cells)
            {
                State = RunState.Won;
                EndReason = "Every patch of the garden is yours!";
                return StepResult.Won;
            }

            if (!eat) return StepResult.Moved;
            // Pending growth reserves the remaining cells, including the final apple.
            if (body.Count + digestion.Count == Cells) Food = NoFood;
            else SpawnFood();
            return StepResult.Ate;
        }

        private StepResult Lose(string reason)
        {
            State = RunState.Lost;
            EndReason = reason;
            turns.Clear();
            return StepResult.Lost;
        }

        private void ResetRun()
        {
            body.Clear();
            turns.Clear();
            digestion.Clear();
            var startX = Columns / 2;
            var startY = Rows / 2;
            var length = Mathf.Clamp(rules.initialLength, 2, startX + 1);
            for (var i = 0; i < length; i++) body.Add(new Cell(startX - i, startY));
            Heading = Direction.Right;
            Score = 0;
            EndReason = string.Empty;
            State = RunState.Ready;
            // The first apple teaches movement immediately; later apples use free-cell sampling.
            Food = new Cell(startX + Mathf.Clamp(rules.firstAppleDistance, 1, Columns - startX - 1), startY);
        }

        /// <summary>Picks the nth free cell, so spawning never retries and never hangs on a full board.</summary>
        private void SpawnFood()
        {
            var index = random.Next(Cells - body.Count);
            for (var y = 0; y < Rows; y++)
            for (var x = 0; x < Columns; x++)
            {
                var cell = new Cell(x, y);
                if (body.Contains(cell)) continue;
                if (index-- == 0) { Food = cell; return; }
            }
            Debug.LogError("No free food cell.");
            ResetRun();
        }

        private bool InBounds(Cell cell) =>
            cell is { X: >= 0, Y: >= 0 } and { X: < Columns, Y: < Rows };

        // The record

        /// <summary>Compares this apple against the record the run started with.</summary>
        private RecordBeat ScoreRecord()
        {
            if (Score <= scoreAtLastApple) return RecordBeat.None;
            scoreAtLastApple = Score;
            if (!RecordEligible || Score <= PreviousBest) return RecordBeat.None;
            if (RecordBroken) return RecordBeat.Extended;
            RecordBroken = true;
            return RecordBeat.Broken;
        }

        /// <summary>Tracks the record in memory; the disk write waits for the run to end.</summary>
        private void UpdateBest()
        {
            if (Score <= best) return;
            best = Score;
            bestUnsaved = true;
        }

        private void SaveBest()
        {
            if (!bestUnsaved) return;
            bestUnsaved = false;
            PlayerPrefs.SetInt(BestKey, best);
            PlayerPrefs.Save();
        }

        // Board space

        private static Cell Offset(Direction direction) => direction switch
        {
            Direction.Up => new Cell(0, 1),
            Direction.Right => new Cell(1, 0),
            Direction.Down => new Cell(0, -1),
            _ => new Cell(-1, 0)
        };

        private static bool IsOpposite(Direction a, Direction b) => ((int)a + 2) % 4 == (int)b;

        /// <summary>-1 for a left turn, 1 for a right turn, 0 when the heading does not change.</summary>
        private static int TurnSign(Direction from, Direction to)
        {
            var difference = ((int)to - (int)from + 4) % 4;
            return difference == 1 ? 1 : difference == 3 ? -1 : 0;
        }
    }
}
