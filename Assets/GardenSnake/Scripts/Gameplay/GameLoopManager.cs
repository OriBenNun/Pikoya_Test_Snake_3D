using System;
using GardenSnake.Core;
using UnityEngine;

namespace GardenSnake
{
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
    /// The rules and the meta loop. It owns the simulation, the fixed movement step, the record
    /// that survives between runs, and the pause and sound preferences. It decides <em>what</em>
    /// happened and announces it; it never decides how any of it should look or sound.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class GameLoopManager : MonoBehaviour
    {
        [Header("Board")]
        [SerializeField, Min(6)] private int boardWidth = 21;
        [SerializeField, Min(6)] private int boardHeight = 12;
        [Header("Pace")]
        [SerializeField, Range(.1f, .5f)] private float initialStepSeconds = .25f;
        [SerializeField, Range(.06f, .3f)] private float fastestStepSeconds = .115f;
        [SerializeField, Range(0f, .02f)] private float speedGainPerApple = .0055f;
        [SerializeField, Range(0f, 1f)] private float openingBeat = .45f;
        [Header("Rules (applied on entering Play Mode)")]
        [SerializeField, Min(2)] private int initialLength = 3;
        [SerializeField, Range(1, 8)] private int turnBufferSize = 2;
        [SerializeField, Min(1)] private int firstAppleDistance = 2;
        [Header("Runtime")]
        [SerializeField] private PlayerController input;
        [SerializeField, Min(1)] private int targetFrameRate = 60;
        [SerializeField] private bool runInBackground = true;
        [SerializeField, Min(0)] private float restartDelay = .35f;

        private const string BestKey = "GardenSnake.Best";
        private const string PlayedKey = "GardenSnake.HasPlayed";
        private const string MutedKey = "GardenSnake.Muted";
        // One stalled browser frame must not advance the snake through several cells unseen.
        private const float MaximumCatchUp = .1f;

        /// <summary>Anything a readout could be showing has changed.</summary>
        public event Action Changed;
        /// <summary>Raised immediately before the simulation advances one cell.</summary>
        public event Action Stepping;
        /// <summary>Raised immediately after it advances, with what the step turned out to be.</summary>
        public event Action<StepResult> Stepped;
        /// <summary>A fresh run just began, with the head's world position.</summary>
        public event Action<Vector3> RunStarted;
        public event Action<AppleBeat> AppleEaten;
        /// <summary>The run is over: <see cref="StepResult.Lost"/> or <see cref="StepResult.Won"/>.</summary>
        public event Action<StepResult> RunEnded;
        /// <summary>A turn the simulation accepted: -1 to the left, 1 to the right.</summary>
        public event Action<int> TurnAccepted;
        public event Action<bool> MuteChanged;
        /// <summary>The small confirmation every button press earns.</summary>
        public event Action Clicked;

        private readonly RunRecord record = new RunRecord();
        private float elapsed;
        private float currentStep;
        private float endTime;
        private int best;
        private bool bestUnsaved;
        private bool hasPlayed;
        private bool muted;

        public SnakeGame Game { get; private set; }
        public RunRecord Record => record;
        public int RecordCelebrations { get; private set; }
        public int RecordWhispers { get; private set; }
        public int Best => best;
        public bool Muted => muted;
        public int BoardWidth => boardWidth;
        public int BoardHeight => boardHeight;

        /// <summary>Seconds per cell at the current score; the step in force may still be the last one.</summary>
        public float StepSeconds => Mathf.Max(fastestStepSeconds, initialStepSeconds - Game.Score * speedGainPerApple);
        /// <summary>The step the snake is actually moving on right now.</summary>
        public float CurrentStep => currentStep;
        /// <summary>How far through the current step the snake is, 0 to 1.</summary>
        public float StepProgress => Game.State == RunState.Playing || Game.State == RunState.Paused
            ? Mathf.Clamp01(elapsed / currentStep) : 1;
        /// <summary>Pace at the current score, for readouts.</summary>
        public float Pace => Mathf.InverseLerp(initialStepSeconds, fastestStepSeconds, StepSeconds);
        /// <summary>Pace of the step in force, for anything that has to move with the snake.</summary>
        public float CurrentPace => Mathf.InverseLerp(initialStepSeconds, fastestStepSeconds, currentStep);

        private void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            Application.runInBackground = runInBackground;
            Game = new SnakeGame(boardWidth, boardHeight, Environment.TickCount,
                initialLength, turnBufferSize, firstAppleDistance);
            best = PlayerPrefs.GetInt(BestKey, 0);
            hasPlayed = PlayerPrefs.GetInt(PlayedKey, 0) == 1 || best > 0;
            muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
            currentStep = StepSeconds;
        }

        private void OnEnable()
        {
            if (input == null) return;
            input.PrimaryRequested += PrimaryAction;
            input.PauseRequested += TogglePause;
            input.MuteRequested += ToggleMute;
            input.TurnRequested += Turn;
        }

        private void OnDisable()
        {
            if (input == null) return;
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
            if (Game.State != RunState.Playing) return;
            elapsed += Mathf.Min(Time.deltaTime, MaximumCatchUp);
            if (elapsed >= currentStep) Advance();
        }

        // ---------------------------------------------------------------- player actions

        public void PrimaryAction()
        {
            if (Game.State == RunState.Paused) { TogglePause(); return; }
            if (Game.State == RunState.Playing) return;
            if ((Game.State == RunState.Lost || Game.State == RunState.Won) &&
                Time.unscaledTime - endTime < restartDelay) return;
            record.Begin(best, hasPlayed);
            RecordCelebrations = RecordWhispers = 0;
            hasPlayed = true;
            PlayerPrefs.SetInt(PlayedKey, 1);
            PlayerPrefs.Save();
            Game.Reset();
            Game.Start();
            currentStep = StepSeconds;
            // A short beat before the first step gives the player time to read the board.
            elapsed = -openingBeat;
            RunStarted?.Invoke(World(Game.Body[0]));
            Changed?.Invoke();
        }

        public void Turn(Direction wish)
        {
            if (Game.State == RunState.Ready) PrimaryAction();
            int turnSign = TurnSign(Game.Heading, wish);
            if (!Game.QueueTurn(wish)) return;
            TurnAccepted?.Invoke(turnSign);
        }

        /// <summary>Kept for callers that hand a raw <see cref="Direction"/> value across a boundary.</summary>
        public void Turn(int direction) => Turn((Direction)direction);

        public void TogglePause()
        {
            Game.TogglePause();
            Changed?.Invoke();
        }

        public void ToggleMute()
        {
            muted = !muted;
            PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
            PlayerPrefs.Save();
            MuteChanged?.Invoke(muted);
            Changed?.Invoke();
        }

        public void Click() => Clicked?.Invoke();

        // ---------------------------------------------------------------- the step

        private void Advance()
        {
            elapsed -= currentStep;
            Vector3 eatenAt = World(Game.Food);
            Stepping?.Invoke();
            StepResult result = Game.Step();
            currentStep = StepSeconds;
            Stepped?.Invoke(result);
            if (result == StepResult.Ate)
            {
                RecordBeat beat = record.Apple(Game.Score);
                if (beat == RecordBeat.Broken) RecordCelebrations++;
                else if (beat == RecordBeat.Extended) RecordWhispers++;
                UpdateBest();
                AppleEaten?.Invoke(new AppleBeat(eatenAt, Game.Score, beat));
            }
            if (result == StepResult.Lost || result == StepResult.Won)
            {
                endTime = Time.unscaledTime;
                SaveBest();
                RunEnded?.Invoke(result);
            }
            Changed?.Invoke();
        }

        // ---------------------------------------------------------------- the record

        /// <summary>Tracks the record in memory; the disk write waits for the run to end.</summary>
        private void UpdateBest()
        {
            if (Game.Score <= best) return;
            best = Game.Score;
            bestUnsaved = true;
        }

        private void SaveBest()
        {
            if (!bestUnsaved) return;
            bestUnsaved = false;
            PlayerPrefs.SetInt(BestKey, best);
            PlayerPrefs.Save();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused) return;
            SaveBest();
            if (Game != null && Game.State == RunState.Playing) TogglePause();
        }

        private void OnApplicationQuit() => SaveBest();

        // ---------------------------------------------------------------- board space

        /// <summary>Where a cell sits in the world. The one place board coordinates become metres.</summary>
        public Vector3 World(Cell cell) =>
            new Vector3(cell.X - (boardWidth - 1) * .5f, 0, cell.Y - (boardHeight - 1) * .5f);

        /// <summary>-1 for a left turn, 1 for a right turn, 0 when the heading does not change.</summary>
        private static int TurnSign(Direction from, Direction to)
        {
            int difference = ((int)to - (int)from + 4) % 4;
            return difference == 1 ? 1 : difference == 3 ? -1 : 0;
        }
    }
}
