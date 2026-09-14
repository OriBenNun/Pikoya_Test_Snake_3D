using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GardenSnake
{
    /// <summary>
    /// The input layer, and the top of the chain: it reads the keyboard and the pointer and
    /// announces what the player asked for. It holds no game state, reads none, and never calls
    /// down into the rest of the game - everything below subscribes to these events instead.
    /// </summary>
    [DefaultExecutionOrder(-300)]
    public sealed class PlayerController : MonoBehaviour
    {
        /// <summary>Space, Enter, or the card's main button: start, restart, or resume.</summary>
        public event Action PrimaryRequested;
        public event Action PauseRequested;
        public event Action MuteRequested;
        public event Action<Direction> TurnRequested;

        [Header("Swipe")]
        [SerializeField, Min(1)] private float swipeMinimumPixels = 24;
        [SerializeField, Range(0, 1)] private float swipeScreenHeightFraction = .035f;

        private Vector2 pointerStart;
        private bool trackingSwipe;

        private void Update()
        {
            ReadKeyboard();
            ReadPointer();
        }

        private void ReadKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) PrimaryRequested?.Invoke();
            if (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame) Pause();
            if (keyboard.mKey.wasPressedThisFrame) MuteRequested?.Invoke();
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) TurnRequested?.Invoke(Direction.Up);
            else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) TurnRequested?.Invoke(Direction.Right);
            else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) TurnRequested?.Invoke(Direction.Down);
            else if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) TurnRequested?.Invoke(Direction.Left);
        }

        private void ReadPointer()
        {
            var pointer = Pointer.current;
            if (pointer == null) return;
            if (pointer.press.wasPressedThisFrame)
            {
                // A press that starts on the HUD belongs to the HUD, not to the board.
                trackingSwipe = EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject();
                pointerStart = pointer.position.ReadValue();
            }
            if (trackingSwipe && pointer.press.isPressed)
            {
                Vector2 delta = pointer.position.ReadValue() - pointerStart;
                if (delta.magnitude >= Mathf.Max(swipeMinimumPixels, Screen.height * swipeScreenHeightFraction))
                {
                    TurnRequested?.Invoke(Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                        ? delta.x > 0 ? Direction.Right : Direction.Left
                        : delta.y > 0 ? Direction.Up : Direction.Down);
                    pointerStart = pointer.position.ReadValue();
                }
            }
            if (pointer.press.wasReleasedThisFrame) trackingSwipe = false;
        }

        /// <summary>Buttons on the results card and the corner controls call straight in here.</summary>
        public void RequestPrimary() => PrimaryRequested?.Invoke();

        public void RequestPause() => Pause();

        public void RequestMute() => MuteRequested?.Invoke();

        /// <summary>A pause always abandons the swipe in progress, whoever asked for it.</summary>
        private void Pause()
        {
            trackingSwipe = false;
            PauseRequested?.Invoke();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) trackingSwipe = false;
        }

        private void OnDisable() => trackingSwipe = false;
    }
}
