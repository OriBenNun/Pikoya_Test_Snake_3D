using System.Collections.Generic;
using GardenSnake.Core;
using NUnit.Framework;

namespace GardenSnake.Tests
{
    public sealed class SnakeGameTests
    {
        [Test]
        public void ReadyAndPausedStatesDoNotAdvance()
        {
            var game = new SnakeGame();
            var head = game.Body[0];
            Assert.That(game.Step(), Is.EqualTo(StepResult.None));
            game.Start();
            game.TogglePause();
            Assert.That(game.Step(), Is.EqualTo(StepResult.None));
            Assert.That(game.Body[0], Is.EqualTo(head));
            Assert.That(game.QueueTurn(Direction.Up), Is.False);
            game.TogglePause();
            Assert.That(game.Step(), Is.EqualTo(StepResult.Moved));
        }

        [Test]
        public void ReverseAndDuplicateInputsAreIgnored()
        {
            var game = Started();
            Assert.That(game.QueueTurn(Direction.Left), Is.False);
            Assert.That(game.QueueTurn(Direction.Right), Is.False);
            game.Step();
            Assert.That(game.Heading, Is.EqualTo(Direction.Right));
        }

        [Test]
        public void TwoBufferedCornersExecuteOnSeparateTicks()
        {
            var game = Started();
            Assert.That(game.QueueTurn(Direction.Up), Is.True);
            Assert.That(game.QueueTurn(Direction.Down), Is.False);
            Assert.That(game.QueueTurn(Direction.Left), Is.True);
            Assert.That(game.QueueTurn(Direction.Down), Is.False, "Buffer must stay bounded.");
            game.Step();
            Assert.That(game.Heading, Is.EqualTo(Direction.Up));
            game.Step();
            Assert.That(game.Heading, Is.EqualTo(Direction.Left));
            Assert.That(game.State, Is.EqualTo(RunState.Playing));
        }

        [Test]
        public void AppleGrowsExactlyOneCellAndSpawnsOutsideSnake()
        {
            var game = Started();
            Assert.That(game.Step(), Is.EqualTo(StepResult.Moved));
            Assert.That(game.Step(), Is.EqualTo(StepResult.Ate));
            Assert.That(game.Score, Is.EqualTo(1));
            Assert.That(game.Body.Count, Is.EqualTo(4));
            Assert.That(game.Body, Has.No.Member(game.Food));
        }

        [Test]
        public void WallCollisionDoesNotMoveBodyOutOfBounds()
        {
            var game = Started();
            for (int i = 0; i < 6; i++) game.Step();
            Assert.That(game.State, Is.EqualTo(RunState.Lost));
            Assert.That(game.Body[0].X, Is.EqualTo(11));
            Assert.That(game.EndReason, Does.Contain("edge"));
            Assert.That(game.Step(), Is.EqualTo(StepResult.None));
        }

        [Test]
        public void TailCellCanBeEnteredWhenItVacates()
        {
            var game = Started();
            game.Step(); game.Step();
            game.QueueTurn(Direction.Up); game.Step();
            game.QueueTurn(Direction.Left); game.Step();
            var tailCell = game.Body[game.Body.Count - 1];
            game.QueueTurn(Direction.Down);
            Assert.That(game.Step(), Is.EqualTo(StepResult.Moved));
            Assert.That(game.Body[0], Is.EqualTo(tailCell));
        }

        [Test]
        public void ResetClearsScoreAndQueuedInput()
        {
            var game = Started();
            game.Step(); game.Step(); game.QueueTurn(Direction.Up);
            game.Reset();
            Assert.That(game.Score, Is.Zero);
            Assert.That(game.Body.Count, Is.EqualTo(3));
            Assert.That(game.State, Is.EqualTo(RunState.Ready));
            game.Start(); game.Step();
            Assert.That(game.Heading, Is.EqualTo(Direction.Right));
        }

        [Test]
        public void SameSeedAndInputProduceSameFoodSequence()
        {
            var a = Started(); var b = Started();
            for (int i = 0; i < 4; i++) { a.Step(); b.Step(); Assert.That(a.Food, Is.EqualTo(b.Food)); }
        }

        [Test]
        public void HamiltonianRunFillsBoardWithoutFoodOverlapOrSpawnHang()
        {
            var game = new SnakeGame(6, 6, 21);
            game.Start();
            var cycle = MakeCycle();
            for (int tick = 0; tick < 10000 && game.State == RunState.Playing; tick++)
            {
                FollowCycle(game, cycle);
                game.Step();
                if (game.State == RunState.Playing) Assert.That(game.Body, Has.No.Member(game.Food));
            }
            Assert.That(game.State, Is.EqualTo(RunState.Won));
            Assert.That(game.Body.Count, Is.EqualTo(36));
            Assert.That(game.Score, Is.EqualTo(33));
        }

        [Test]
        public void TurningIntoOccupiedBodyLoses()
        {
            var game = new SnakeGame(6, 6, 21);
            game.Start();
            var cycle = MakeCycle();
            for (int tick = 0; tick < 10000 && game.State == RunState.Playing; tick++)
            {
                for (int d = 0; d < 4; d++)
                {
                    if ((d + 2) % 4 == (int)game.Heading) continue;
                    Cell target = game.Body[0] + SnakeGame.Offset((Direction)d);
                    for (int i = 1; i < game.Body.Count - 1; i++)
                    {
                        if (game.Body[i] != target) continue;
                        game.QueueTurn((Direction)d);
                        Assert.That(game.Step(), Is.EqualTo(StepResult.Lost));
                        Assert.That(game.EndReason, Does.Contain("tail"));
                        return;
                    }
                }
                FollowCycle(game, cycle);
                game.Step();
            }
            Assert.Fail("Test route never exposed a self-collision opportunity.");
        }

        private static SnakeGame Started() { var game = new SnakeGame(12, 12, 1); game.Start(); return game; }

        private static List<Cell> MakeCycle()
        {
            var cycle = new List<Cell>();
            for (int y = 0; y < 6; y++) cycle.Add(new Cell(0, y));
            for (int y = 5; y >= 0; y--)
                for (int i = 1; i < 6; i++) cycle.Add(new Cell(y % 2 == 1 ? i : 6 - i, y));
            return cycle;
        }

        private static void FollowCycle(SnakeGame game, List<Cell> cycle)
        {
            int index = cycle.IndexOf(game.Body[0]);
            Cell next = cycle[(index + 1) % cycle.Count];
            for (int d = 0; d < 4; d++)
                if (game.Body[0] + SnakeGame.Offset((Direction)d) == next) game.QueueTurn((Direction)d);
        }
    }
}
