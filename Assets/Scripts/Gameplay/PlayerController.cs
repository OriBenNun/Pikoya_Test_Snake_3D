using System;
using System.Collections.Generic;
using GardenSnake.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GardenSnake
{
    /// <summary>
    /// The input layer, and the top of the chain: it announces what the player asked for. It holds
    /// no game state, reads none, and never calls down into the rest of the game - everything below
    /// subscribes to these events instead.
    /// <para>
    /// Every action is callback driven, so a frame in which the player does nothing costs nothing.
    /// The pointer is the reason that matters: its position is only watched between a press and its
    /// release, and on a keyboard-only run it is never watched at all.
    /// </para>
    /// <para>
    /// The actions themselves live in the project's input actions asset, in the Garden Snake map;
    /// the slots below say which action answers which event, so the bindings can be rebound there
    /// without touching this.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-300)]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Actions")]
        [SerializeField, Tooltip("Start, restart, or resume.")] private InputActionReference primary;
        [SerializeField] private InputActionReference pause;
        [SerializeField] private InputActionReference mute;
        [SerializeField] private InputActionReference turnUp;
        [SerializeField] private InputActionReference turnRight;
        [SerializeField] private InputActionReference turnDown;
        [SerializeField] private InputActionReference turnLeft;
        [SerializeField, Tooltip("Held down for the length of a swipe.")]
        private InputActionReference pointerPress;
        [SerializeField, Tooltip("Only watched while the pointer is held down.")]
        private InputActionReference pointerPosition;
        [Header("Tuning")]
        [SerializeField] private SwipeSettings swipe;

        /// <summary>Space, Enter, or the card's main button: start, restart, or resume.</summary>
        public event Action PrimaryRequested;
        public event Action PauseRequested;
        public event Action MuteRequested;
        public event Action<Direction> TurnRequested;

        private readonly List<RaycastResult> hits = new();
        private Vector2 pointerStart;
        private bool trackingSwipe;

        /// <summary>Buttons on the results card and the corner controls call straight in here.</summary>
        public void RequestPrimary() => PrimaryRequested?.Invoke();

        public void RequestPause() => Pause();

        public void RequestMute() => MuteRequested?.Invoke();

        private void Awake() => swipe = Tuning.Or(swipe);

        private void OnEnable()
        {
            Listen(primary, OnPrimary);
            Listen(pause, OnPause);
            Listen(mute, OnMute);
            Listen(turnUp, OnTurnUp);
            Listen(turnRight, OnTurnRight);
            Listen(turnDown, OnTurnDown);
            Listen(turnLeft, OnTurnLeft);
            if (pointerPress == null || pointerPress.action == null) return;
            pointerPress.action.started += OnPointerPressed;
            pointerPress.action.canceled += OnPointerReleased;
            pointerPress.action.Enable();
            if (pointerPosition == null || pointerPosition.action == null) return;
            pointerPosition.action.performed += OnPointerMoved;
            // The asset is the project-wide one, so its maps come up enabled. The pointer's
            // position is the one action worth switching off until a press actually needs it.
            pointerPosition.action.Disable();
        }

        private void OnDisable()
        {
            Silence(primary, OnPrimary);
            Silence(pause, OnPause);
            Silence(mute, OnMute);
            Silence(turnUp, OnTurnUp);
            Silence(turnRight, OnTurnRight);
            Silence(turnDown, OnTurnDown);
            Silence(turnLeft, OnTurnLeft);
            if (pointerPress && pointerPress.action != null)
            {
                pointerPress.action.started -= OnPointerPressed;
                pointerPress.action.canceled -= OnPointerReleased;
                pointerPress.action.Disable();
            }
            if (pointerPosition && pointerPosition.action != null)
                pointerPosition.action.performed -= OnPointerMoved;
            EndSwipe();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) EndSwipe();
        }

        private void OnPrimary(InputAction.CallbackContext context) => PrimaryRequested?.Invoke();

        private void OnPause(InputAction.CallbackContext context) => Pause();

        private void OnMute(InputAction.CallbackContext context) => MuteRequested?.Invoke();

        private void OnTurnUp(InputAction.CallbackContext context) => TurnRequested?.Invoke(Direction.Up);

        private void OnTurnRight(InputAction.CallbackContext context) => TurnRequested?.Invoke(Direction.Right);

        private void OnTurnDown(InputAction.CallbackContext context) => TurnRequested?.Invoke(Direction.Down);

        private void OnTurnLeft(InputAction.CallbackContext context) => TurnRequested?.Invoke(Direction.Left);

        private void OnPointerPressed(InputAction.CallbackContext context)
        {
            pointerStart = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
            // A press that starts on the HUD belongs to the HUD, not to the board.
            trackingSwipe = !OverHud(pointerStart);
            if (trackingSwipe) pointerPosition?.action?.Enable();
        }

        /// <summary>The pointer moved while held down; a long enough flick is a turn.</summary>
        private void OnPointerMoved(InputAction.CallbackContext context)
        {
            if (!trackingSwipe) return;
            Vector2 position = context.ReadValue<Vector2>();
            Vector2 delta = position - pointerStart;
            if (delta.magnitude < Mathf.Max(swipe.minimumPixels, Screen.height * swipe.screenHeightFraction)) return;
            TurnRequested?.Invoke(Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                ? delta.x > 0 ? Direction.Right : Direction.Left
                : delta.y > 0 ? Direction.Up : Direction.Down);
            pointerStart = position;
        }

        private void OnPointerReleased(InputAction.CallbackContext context) => EndSwipe();

        /// <summary>
        /// Whether a press at this point landed on the HUD. Raycast rather than
        /// EventSystem.IsPointerOverGameObject, which answers with the previous frame's hover state
        /// when it is asked from inside an input callback - and a fresh touch had no hover at all.
        /// </summary>
        private bool OverHud(Vector2 position)
        {
            if (EventSystem.current == null) return false;
            hits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
            return hits.Count > 0;
        }

        private void EndSwipe()
        {
            trackingSwipe = false;
            pointerPosition?.action?.Disable();
        }

        /// <summary>A pause always abandons the swipe in progress, whoever asked for it.</summary>
        private void Pause()
        {
            EndSwipe();
            PauseRequested?.Invoke();
        }

        private static void Listen(InputActionReference reference, Action<InputAction.CallbackContext> answer)
        {
            if (!reference || reference.action == null) return;
            reference.action.performed += answer;
            reference.action.Enable();
        }

        private static void Silence(InputActionReference reference, Action<InputAction.CallbackContext> answer)
        {
            if (!reference || reference.action == null) return;
            reference.action.performed -= answer;
            reference.action.Disable();
        }
    }
}
