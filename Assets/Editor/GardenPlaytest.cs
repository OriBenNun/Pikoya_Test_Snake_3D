using System.Collections.Generic;
using System.Globalization;
using System.IO;
using GardenSnake.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace GardenSnake.Editor.Editor
{
    /// <summary>
    /// Unattended playtest driver. Steers the real game with real Input System events, restarts
    /// after every death, and writes a screenshot filmstrip plus a run report so a session can be
    /// reviewed frame by frame from the command line.
    /// </summary>
    public static class GardenPlaytest
    {
        private const string ShotFolder = "Artifacts/shots";
        private const string DriverKeyboard = "PlaytestKeyboard";
        private const string ReportPath = "Artifacts/playtest.txt";

        private static GameLoopManager controller;
        private static Keyboard keyboard;
        private static InputSettings.BackgroundBehavior backgroundBehavior;
        private static double endsAt;
        private static double nextShotAt;
        private static double shotInterval;
        private static double restartAt;
        private static int releaseAt;
        private static int shotIndex;
        private static int runs;
        private static int lastScore;
        /// <summary>Every direction, in turn order, reused so steering allocates nothing.</summary>
        private static readonly Direction[] AllDirections =
            { Direction.Up, Direction.Right, Direction.Down, Direction.Left };
        private static readonly List<Direction> wishes = new List<Direction>(8);
        private static readonly List<double> pending = new List<double>();
        private static int bestScore;
        private static string label;
        private static readonly List<string> notes = new List<string>();

        /// <summary>Steers for <paramref name="seconds"/>, saving a frame every <paramref name="everySeconds"/>.</summary>
        public static string Run(string runLabel, float seconds, float everySeconds)
        {
            if (!EditorApplication.isPlaying) return "not-playing";
            controller = UnityEngine.Object.FindAnyObjectByType<GameLoopManager>();
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
            lastScore = 0;
            pending.Clear();
            releaseAt = -1;
            notes.Clear();
            Application.runInBackground = true;
            backgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            // A domain reload forgets this field but not the device, so a run that ends badly
            // leaves a keyboard behind. Left to accumulate, one of the orphans becomes
            // Keyboard.current and quietly swallows every key the driver sends.
            for (int i = InputSystem.devices.Count - 1; i >= 0; i--)
            {
                InputDevice device = InputSystem.devices[i];
                if (device != keyboard && device.name.StartsWith(DriverKeyboard)) InputSystem.RemoveDevice(device);
            }
            if (keyboard == null || !keyboard.added) keyboard = InputSystem.AddDevice<Keyboard>(DriverKeyboard);
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
                Capture();
            }
            // Moments worth a frame of their own: the bite, the wilt, and the results card.
            for (int i = pending.Count - 1; i >= 0; i--)
                if (now >= pending[i]) { pending.RemoveAt(i); Capture(); }
            if (controller.Score != lastScore)
            {
                if (controller.Score > lastScore) pending.Add(now + .12);
                lastScore = controller.Score;
            }
            bestScore = Mathf.Max(bestScore, controller.Score);
            switch (controller.State)
            {
                case RunState.Ready:
                    if (restartAt == 0) restartAt = now + .35;
                    if (now >= restartAt) { restartAt = 0; runs++; Press(Key.Space); }
                    break;
                case RunState.Lost:
                case RunState.Won:
                    if (restartAt == 0)
                    {
                        restartAt = now + 2.4;
                        pending.Add(now + .2);
                        pending.Add(now + 1.3);
                        notes.Add($"run {runs}: {controller.Score} apples, {controller.EndReason}");
                    }
                    if (now >= restartAt) { restartAt = 0; runs++; Press(Key.Space); }
                    break;
                case RunState.Paused:
                    // The Editor pauses the game whenever it loses focus, which it always has
                    // while the harness drives it from the command line.
                    Press(Key.P);
                    break;
                case RunState.Playing:
                    restartAt = 0;
                    Steer();
                    break;
            }
            if (now >= endsAt) Finish(null);
        }

        private static void Capture()
        {
            ScreenCapture.CaptureScreenshot($"{ShotFolder}/{label}-{shotIndex:00}.png");
            shotIndex++;
        }

        /// <summary>Greedy chase that refuses moves into a wall or an occupied cell.</summary>
        private static void Steer()
        {
            Cell head = controller.Body[0];
            Cell food = controller.Food;
            wishes.Clear();
            if (food.X != head.X) wishes.Add(food.X > head.X ? Direction.Right : Direction.Left);
            if (food.Y != head.Y) wishes.Add(food.Y > head.Y ? Direction.Up : Direction.Down);
            // Straight on first, then anything legal, so a blocked chase still keeps moving.
            if (!wishes.Contains(controller.Heading)) wishes.Add(controller.Heading);
            foreach (Direction fallback in AllDirections)
                if (!wishes.Contains(fallback)) wishes.Add(fallback);
            foreach (Direction wish in wishes)
            {
                if (!Safe(wish)) continue;
                if (wish == controller.Heading) return;
                Press(KeyFor(wish));
                return;
            }
        }

        private static bool Safe(Direction direction)
        {
            Cell next = controller.Body[0] + GameLoopManager.Offset(direction);
            if (!controller.InBounds(next)) return false;
            for (int i = 0; i < controller.Body.Count - 1; i++)
                if (controller.Body[i] == next) return false;
            // One-step lookahead keeps the pilot out of pockets it cannot leave.
            int exits = 0;
            foreach (Direction option in AllDirections)
            {
                Cell after = next + GameLoopManager.Offset(option);
                if (!controller.InBounds(after)) continue;
                bool blocked = false;
                for (int i = 0; i < controller.Body.Count - 2; i++)
                    if (controller.Body[i] == after) blocked = true;
                if (!blocked) exits++;
            }
            return exits > 0;
        }

        private static Key KeyFor(Direction direction) => direction switch
        {
            Direction.Up => Key.W,
            Direction.Right => Key.D,
            Direction.Down => Key.S,
            _ => Key.A
        };

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
