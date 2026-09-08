using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using GardenSnake.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace GardenSnake.Editor
{
    /// <summary>
    /// Unattended playtest driver. Steers the real game with real Input System events, restarts
    /// after every death, and writes a screenshot filmstrip plus a run report so a session can be
    /// reviewed frame by frame from the command line.
    /// </summary>
    public static class GardenPlaytest
    {
        private const string ShotFolder = "Artifacts/shots";
        private const string ReportPath = "Artifacts/playtest.txt";

        private static SnakeController controller;
        private static Keyboard keyboard;
        private static InputSettings.BackgroundBehavior backgroundBehavior;
        private static double endsAt;
        private static double nextShotAt;
        private static double shotInterval;
        private static double restartAt;
        private static int releaseAt;
        private static int shotIndex;
        private static int runs;
        private static int bestScore;
        private static string label;
        private static readonly List<string> notes = new List<string>();

        /// <summary>Steers for <paramref name="seconds"/>, saving a frame every <paramref name="everySeconds"/>.</summary>
        public static string Run(string runLabel, float seconds, float everySeconds)
        {
            if (!EditorApplication.isPlaying) return "not-playing";
            controller = UnityEngine.Object.FindAnyObjectByType<SnakeController>();
            if (controller == null) return "no-controller";
            Directory.CreateDirectory(ShotFolder);
            label = runLabel;
            shotInterval = everySeconds;
            endsAt = EditorApplication.timeSinceStartup + seconds;
            nextShotAt = EditorApplication.timeSinceStartup;
            restartAt = 0;
            shotIndex = 0;
            runs = 0;
            bestScore = 0;
            releaseAt = -1;
            notes.Clear();
            Application.runInBackground = true;
            backgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            if (keyboard == null || !keyboard.added) keyboard = InputSystem.AddDevice<Keyboard>("PlaytestKeyboard");
            File.WriteAllText(ReportPath, "running");
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            return "started";
        }

        public static string Status() => File.Exists(ReportPath) ? File.ReadAllText(ReportPath) : "missing";

        private static void Tick()
        {
            if (!EditorApplication.isPlaying || controller == null) { Finish("play mode ended"); return; }
            if (releaseAt >= 0 && Time.frameCount >= releaseAt)
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                releaseAt = -1;
            }
            double now = EditorApplication.timeSinceStartup;
            if (now >= nextShotAt)
            {
                nextShotAt = now + shotInterval;
                ScreenCapture.CaptureScreenshot($"{ShotFolder}/{label}-{shotIndex:00}.png");
                shotIndex++;
            }
            var game = controller.Game;
            bestScore = Mathf.Max(bestScore, game.Score);
            switch (game.State)
            {
                case RunState.Ready:
                    if (restartAt == 0) restartAt = now + .35;
                    if (now >= restartAt) { restartAt = 0; runs++; Press(Key.Space); }
                    break;
                case RunState.Lost:
                case RunState.Won:
                    if (restartAt == 0)
                    {
                        restartAt = now + 1.4;
                        notes.Add($"run {runs}: {game.Score} apples, {game.EndReason}");
                    }
                    if (now >= restartAt) { restartAt = 0; runs++; Press(Key.Space); }
                    break;
                case RunState.Playing:
                    restartAt = 0;
                    Steer(game);
                    break;
            }
            if (now >= endsAt) Finish(null);
        }

        /// <summary>Greedy chase that refuses moves into a wall or an occupied cell.</summary>
        private static void Steer(SnakeGame game)
        {
            Cell head = game.Body[0];
            Cell food = game.Food;
            var wishes = new List<Direction>();
            if (food.X != head.X) wishes.Add(food.X > head.X ? Direction.Right : Direction.Left);
            if (food.Y != head.Y) wishes.Add(food.Y > head.Y ? Direction.Up : Direction.Down);
            foreach (Direction fallback in new[] { game.Heading, Direction.Up, Direction.Right, Direction.Down, Direction.Left })
                if (!wishes.Contains(fallback)) wishes.Add(fallback);
            foreach (Direction wish in wishes)
            {
                if (!Safe(game, wish)) continue;
                if (wish == game.Heading) return;
                Press(KeyFor(wish));
                return;
            }
        }

        private static bool Safe(SnakeGame game, Direction direction)
        {
            Cell next = game.Body[0] + SnakeGame.Offset(direction);
            if (next.X < 0 || next.Y < 0 || next.X >= game.Width || next.Y >= game.Height) return false;
            for (int i = 0; i < game.Body.Count - 1; i++)
                if (game.Body[i] == next) return false;
            // one-step lookahead keeps the pilot out of pockets it cannot leave
            int exits = 0;
            foreach (Direction option in new[] { Direction.Up, Direction.Right, Direction.Down, Direction.Left })
            {
                Cell after = next + SnakeGame.Offset(option);
                if (after.X < 0 || after.Y < 0 || after.X >= game.Width || after.Y >= game.Height) continue;
                bool blocked = false;
                for (int i = 0; i < game.Body.Count - 2; i++)
                    if (game.Body[i] == after) blocked = true;
                if (!blocked) exits++;
            }
            return exits > 0;
        }

        private static Key KeyFor(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return Key.W;
                case Direction.Right: return Key.D;
                case Direction.Down: return Key.S;
                default: return Key.A;
            }
        }

        private static void Press(Key key)
        {
            if (releaseAt >= 0) return;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            releaseAt = Time.frameCount + 2;
        }

        private static void Finish(string error)
        {
            EditorApplication.update -= Tick;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = backgroundBehavior;
            var report = new List<string>
            {
                "done" + (error == null ? "" : " (" + error + ")"),
                "runs=" + runs + " best=" + bestScore + " frames=" + shotIndex,
                "label=" + label
            };
            report.AddRange(notes);
            File.WriteAllLines(ReportPath, report);
            Debug.Log("[GardenSnake] Playtest finished: best " + bestScore.ToString(CultureInfo.InvariantCulture));
        }
    }
}
