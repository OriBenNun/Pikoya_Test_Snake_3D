using System;
using System.Collections.Generic;

namespace GardenSnake.Core
{
    public enum Direction { Up, Right, Down, Left }
    public enum RunState { Ready, Playing, Paused, Lost, Won }
    public enum StepResult { None, Moved, Ate, Lost, Won }

    public readonly struct Cell : IEquatable<Cell>
    {
        public readonly int X;
        public readonly int Y;

        public Cell(int x, int y) { X = x; Y = y; }

        public bool Equals(Cell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Cell other && Equals(other);
        public override int GetHashCode() => unchecked(X * 397 ^ Y);
        public static bool operator ==(Cell a, Cell b) => a.Equals(b);
        public static bool operator !=(Cell a, Cell b) => !a.Equals(b);
        public static Cell operator +(Cell a, Cell b) => new(a.X + b.X, a.Y + b.Y);
    }

    /// <summary>Deterministic grid simulation. No Unity, timing, rendering, or input dependencies.</summary>
    public sealed class SnakeGame
    {
        /// <summary>
        /// A swallowed apple travels one body index per move, which keeps it on the cell it was
        /// picked up from while the snake slides over it.
        /// </summary>
        public const float DigestionPerStep = 1f;

        private const int MinimumBoardSide = 6;
        private const int MaximumTurnBuffer = 8;
        private static readonly Cell NoFood = new(-1, -1);

        private readonly List<Cell> body = new();
        private readonly List<float> digestion = new();
        private readonly Queue<Direction> turns;
        private readonly Random random;
        private readonly int firstAppleDistance;

        public SnakeGame(int width = 12, int height = 12, int seed = 1, int initialLength = 3,
            int turnBufferSize = 2, int firstAppleDistance = 2)
        {
            if (width < MinimumBoardSide || height < MinimumBoardSide)
                throw new ArgumentOutOfRangeException(nameof(width),
                    "The board must be at least " + MinimumBoardSide + " cells on each side.");
            Width = width;
            Height = height;
            InitialLength = Math.Max(2, Math.Min(width / 2 + 1, initialLength));
            TurnBufferSize = Math.Max(1, Math.Min(MaximumTurnBuffer, turnBufferSize));
            this.firstAppleDistance = Math.Max(1, Math.Min(width - width / 2 - 1, firstAppleDistance));
            random = new Random(seed);
            turns = new Queue<Direction>(TurnBufferSize);
            Body = body.AsReadOnly();
            Digestion = digestion.AsReadOnly();
            Reset();
        }

        public int Width { get; }
        public int Height { get; }
        public int InitialLength { get; }
        public int TurnBufferSize { get; }
        public float DigestionSpeed => DigestionPerStep;
        public IReadOnlyList<Cell> Body { get; }
        public IReadOnlyList<float> Digestion { get; }
        public bool HasFood => Food.X >= 0;
        public int Score { get; private set; }
        public Cell Food { get; private set; }
        public Direction Heading { get; private set; }
        public RunState State { get; private set; }
        public string EndReason { get; private set; }

        public void Reset()
        {
            body.Clear();
            turns.Clear();
            digestion.Clear();
            int startX = Width / 2;
            int startY = Height / 2;
            for (int i = 0; i < InitialLength; i++) body.Add(new Cell(startX - i, startY));
            Heading = Direction.Right;
            Score = 0;
            EndReason = string.Empty;
            State = RunState.Ready;
            // The first apple teaches movement immediately; later apples use free-cell sampling.
            Food = new Cell(startX + firstAppleDistance, startY);
        }

        public void Start()
        {
            if (State == RunState.Ready) State = RunState.Playing;
        }

        public void TogglePause()
        {
            if (State == RunState.Playing) { State = RunState.Paused; turns.Clear(); }
            else if (State == RunState.Paused) State = RunState.Playing;
        }

        public bool InBounds(Cell cell) => cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;

        public bool QueueTurn(Direction direction)
        {
            if (State != RunState.Playing || turns.Count >= TurnBufferSize) return false;
            Direction last = Heading;
            foreach (Direction queued in turns) last = queued;
            if (direction == last || IsOpposite(direction, last)) return false;
            turns.Enqueue(direction);
            return true;
        }

        public StepResult Step()
        {
            if (State != RunState.Playing) return StepResult.None;
            if (turns.Count > 0) Heading = turns.Dequeue();
            Cell next = body[0] + Offset(Heading);
            if (!InBounds(next)) return Lose("You reached the garden edge.");

            // Growth happens only when the oldest swallowed apple reaches the tail, so until then
            // the cell the tail is about to vacate is free to enter.
            bool grow = digestion.Count > 0 && digestion[0] + DigestionPerStep >= body.Count;
            int occupied = body.Count - (grow ? 0 : 1);
            for (int i = 0; i < occupied; i++)
                if (body[i] == next) return Lose("You crossed your own tail.");

            bool eat = next == Food;
            body.Insert(0, next);
            if (!grow) body.RemoveAt(body.Count - 1);
            for (int i = 0; i < digestion.Count; i++) digestion[i] += DigestionPerStep;
            if (grow) digestion.RemoveAt(0);
            if (eat) { Score++; digestion.Add(0); }

            if (body.Count == Width * Height)
            {
                State = RunState.Won;
                EndReason = "Every patch of the garden is yours!";
                return StepResult.Won;
            }
            if (!eat) return StepResult.Moved;
            // Pending growth reserves the remaining cells, including the final apple.
            if (body.Count + digestion.Count == Width * Height) Food = NoFood;
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

        /// <summary>Picks the nth free cell, so spawning never retries and never hangs on a full board.</summary>
        private void SpawnFood()
        {
            int index = random.Next(Width * Height - body.Count);
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                var cell = new Cell(x, y);
                if (body.Contains(cell)) continue;
                if (index-- == 0) { Food = cell; return; }
            }
            throw new InvalidOperationException("No free food cell.");
        }

        public static Cell Offset(Direction direction) => direction switch
        {
            Direction.Up => new Cell(0, 1),
            Direction.Right => new Cell(1, 0),
            Direction.Down => new Cell(0, -1),
            _ => new Cell(-1, 0)
        };

        private static bool IsOpposite(Direction a, Direction b) => ((int)a + 2) % 4 == (int)b;
    }
}
