using System;
using System.Collections.Generic;
using System.IO;
using GardenSnake.Core;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace GardenSnake.Editor
{
    /// <summary>Runs the real controls and UI in Play mode; never changes simulation state directly.</summary>
    public static class GardenPlaythrough
    {
        private static SnakeController controller;
        private static int stage;
        private static double stageAt;
        private static int releaseAt;
        private static Cell pausedCell;
        private static readonly List<string> results = new List<string>();
        private static bool originalMuted;
        private static Keyboard keyboard;
        private static Mouse mouse;
        private static InputSettings.BackgroundBehavior backgroundBehavior;
        private static Vector2 pointerPosition;

        [MenuItem("Garden Snake/Verify controls in Play mode")]
        public static void Start()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
            controller = UnityEngine.Object.FindFirstObjectByType<SnakeController>();
            if (controller == null || controller.Game.State != RunState.Ready)
                throw new InvalidOperationException("Run verification from a fresh Ready screen.");
            var gameView = EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
            gameView.Focus();
            originalMuted = controller.Muted;
            backgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard = InputSystem.AddDevice<Keyboard>("GardenVerificationKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("GardenVerificationMouse");
            pointerPosition = Vector2.zero;
            results.Clear();
            stage = 0;
            stageAt = EditorApplication.timeSinceStartup;
            releaseAt = -1;
            Directory.CreateDirectory("Artifacts");
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            try
            {
                if (!EditorApplication.isPlaying || controller == null) throw new Exception("Play mode stopped during verification.");
                if (releaseAt >= 0 && Time.frameCount >= releaseAt)
                {
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = pointerPosition });
                    releaseAt = -1;
                }
                if (EditorApplication.timeSinceStartup - stageAt > 8) throw new Exception("Timed out at stage " + stage);
                switch (stage)
                {
                    case 0:
                        RequireText("Room to grow.");
                        Capture("ready");
                        KeyPress(Key.Space);
                        Next("Ready screen and keyboard start");
                        break;
                    case 1:
                        if (controller.Game.Score < 1) return;
                        if (controller.Game.Body.Count != 4) throw new Exception("Pickup did not grow the snake.");
                        Capture("pickup");
                        KeyPress(Key.UpArrow);
                        Next("Apple pickup, score, growth");
                        break;
                    case 2:
                        if (controller.Game.Heading != Direction.Up || controller.Game.Body[0].Y <= 6) return;
                        Capture("playing");
                        KeyPress(Key.P);
                        Next("Arrow input turns upward");
                        break;
                    case 3:
                        if (controller.Game.State != RunState.Paused) return;
                        pausedCell = controller.Game.Body[0];
                        RequireText("On a leaf break.");
                        Next("Pause input and pause card");
                        break;
                    case 4:
                        if (EditorApplication.timeSinceStartup - stageAt < .6) return;
                        if (controller.Game.Body[0] != pausedCell) throw new Exception("Snake moved while paused.");
                        Click("Sound");
                        Next("Paused simulation remains fixed");
                        break;
                    case 5:
                        if (controller.Muted == originalMuted) return;
                        Click("Primary");
                        Next("Sound button toggles audio");
                        break;
                    case 6:
                        if (controller.Game.State != RunState.Playing) return;
                        KeyPress(Key.M);
                        Next("Resume button resumes play");
                        break;
                    case 7:
                        if (controller.Game.State != RunState.Lost) return;
                        RequireText("A good little run.");
                        Next("Wall collision and results card");
                        break;
                    case 8:
                        if (EditorApplication.timeSinceStartup - stageAt < .9) return;
                        Capture("results");
                        Click("Primary");
                        Next("Results restart button clicked");
                        break;
                    case 9:
                        if (controller.Game.State != RunState.Playing) return;
                        if (controller.Game.Score != 0 || controller.Game.Body.Count != 3) throw new Exception("Restart did not reset run.");
                        KeyPress(Key.P);
                        Next("Restart resets score and length");
                        break;
                    case 10:
                        if (controller.Game.State != RunState.Paused) return;
                        Finish(null);
                        break;
                }
            }
            catch (Exception error) { Finish(error.Message); }
        }

        private static void Next(string message)
        {
            results.Add("PASS: " + message);
            stage++;
            stageAt = EditorApplication.timeSinceStartup;
        }

        private static void KeyPress(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            releaseAt = Time.frameCount + 2;
        }

        private static void Click(string name)
        {
            foreach (var button in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
            {
                if (button.name != name || !button.interactable) continue;
                var rect = (RectTransform)button.transform;
                Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
                pointerPosition = point;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
                releaseAt = Time.frameCount + 2;
                return;
            }
            throw new Exception("No interactable button: " + name);
        }

        private static void RequireText(string expected)
        {
            foreach (var text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                if (text.text == expected) return;
            throw new Exception("Missing visible prompt: " + expected);
        }

        private static void Capture(string name) => ScreenCapture.CaptureScreenshot("Artifacts/" + name + ".png");

        private static void Finish(string error)
        {
            EditorApplication.update -= Tick;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = backgroundBehavior;
            if (error != null) results.Add("FAIL: " + error);
            File.WriteAllLines("Artifacts/playthrough.txt", results);
            if (error == null) Debug.Log("[GardenSnake] Playthrough: " + results.Count + " checks passed.");
            else Debug.LogError("[GardenSnake] Playthrough: " + error);
        }
    }
}
