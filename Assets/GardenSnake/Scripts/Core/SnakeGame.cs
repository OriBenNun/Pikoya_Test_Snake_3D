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
        public static Cell operator +(Cell a, Cell b) => new Cell(a.X + b.X, a.Y + b.Y);
    }

    /// <summary>Deterministic grid simulation. No Unity, timing, rendering, or input dependencies.</summary>
    public sealed class SnakeGame
    {
        private readonly List<Cell> body = new List<Cell>();
        private readonly Queue<Direction> turns = new Queue<Direction>(2);
        private readonly Random random;
        private readonly IReadOnlyList<Cell> readOnlyBody;
        private readonly List<float> digestion = new List<float>();
        private readonly IReadOnlyList<float> readOnlyDigestion;
        public const float DigestionPerStep = .5f;
        public IReadOnlyList<Cell> Body => readOnlyBody;
        public IReadOnlyList<float> Digestion => readOnlyDigestion;
        public bool HasFood => Food.X >= 0;
        public int Width { get; }
        public int Height { get; }
        public int Score { get; private set; }
        public Cell Food { get; private set; }
        public Direction Heading { get; private set; }
        public RunState State { get; private set; }
        public string EndReason { get; private set; }

        public SnakeGame(int width = 12, int height = 12, int seed = 1)
        {
            if (width < 6 || height < 6) throw new ArgumentOutOfRangeException(nameof(width));
            Width = width;
            Height = height;
            random = new Random(seed);
            readOnlyBody = body.AsReadOnly();
            readOnlyDigestion = digestion.AsReadOnly();
            Reset();
        }

        public void Reset()
        {
            body.Clear();
            turns.Clear();
            digestion.Clear();
            int x = Width / 2;
            int y = Height / 2;
            for (int i = 0; i < 3; i++) body.Add(new Cell(x - i, y));
            Heading = Direction.Right;
            Score = 0;
            EndReason = string.Empty;
            State = RunState.Ready;
            // The first apple teaches movement immediately; later apples use free-cell sampling.
            Food = new Cell(x + 2, y);
        }

        public void Start() { if (State == RunState.Ready) State = RunState.Playing; }
        public void TogglePause()
        {
            if (State == RunState.Playing) { State = RunState.Paused; turns.Clear(); }
            else if (State == RunState.Paused) State = RunState.Playing;
        }

        public bool QueueTurn(Direction direction)
        {
            if (State != RunState.Playing || turns.Count >= 2) return false;
            Direction last = Heading;
            foreach (Direction queued in turns) last = queued;
            if (direction == last || ((int)direction + 2) % 4 == (int)last) return false;
            turns.Enqueue(direction);
            return true;
        }

        public StepResult Step()
        {
            if (State != RunState.Playing) return StepResult.None;
            if (turns.Count > 0) Heading = turns.Dequeue();
            Cell next = body[0] + Offset(Heading);
            bool eat = next == Food;
            if (next.X < 0 || next.Y < 0 || next.X >= Width || next.Y >= Height)
                return Lose("You reached the garden edge.");
            // Growth happens only when the oldest swallowed apple reaches the tail.
            bool grow = digestion.Count > 0 && digestion[0] + DigestionPerStep >= body.Count - 1;
            int occupied = body.Count - (grow ? 0 : 1);
            for (int i = 0; i < occupied; i++)
                if (body[i] == next) return Lose("You crossed your own tail.");
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
            if (eat)
            {
                // Pending growth reserves the remaining cells, including the final apple.
                if (body.Count + digestion.Count == Width * Height) Food = new Cell(-1, -1);
                else SpawnFood();
            }
            return eat ? StepResult.Ate : StepResult.Moved;
        }

        private StepResult Lose(string reason)
        {
            State = RunState.Lost;
            EndReason = reason;
            turns.Clear();
            return StepResult.Lost;
        }

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

        public static Cell Offset(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return new Cell(0, 1);
                case Direction.Right: return new Cell(1, 0);
                case Direction.Down: return new Cell(0, -1);
                default: return new Cell(-1, 0);
            }
        }
    }
}
