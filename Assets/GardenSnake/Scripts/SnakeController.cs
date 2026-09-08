using System.Collections.Generic;
using GardenSnake.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GardenSnake
{
    public sealed class SnakeController : MonoBehaviour
    {
        [Header("Board")]
        [SerializeField, Min(6)] private int boardSize = 12;
        [SerializeField, Range(.1f, .5f)] private float initialStepSeconds = .25f;
        [SerializeField, Range(.06f, .3f)] private float fastestStepSeconds = .12f;
        [SerializeField, Range(0f, .02f)] private float speedGainPerApple = .006f;
        [Header("Blender models")]
        [SerializeField] private GameObject headPrefab;
        [SerializeField] private GameObject bodyPrefab;
        [SerializeField] private GameObject tailPrefab;
        [SerializeField] private GameObject applePrefab;
        [Header("Feedback")]
        [SerializeField] private Camera gameCamera;
        [SerializeField] private ParticleSystem pickupParticles;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private AudioClip turnSound;
        [SerializeField] private AudioClip loseSound;
        [SerializeField] private AudioClip startSound;
        [SerializeField, Range(0f, .4f)] private float cameraKick = .11f;
        [SerializeField] private SnakeHud hud;

        private readonly List<Transform> segments = new List<Transform>();
        private readonly List<Vector3> previous = new List<Vector3>();
        private Transform tail;
        private Transform apple;
        private float elapsed;
        private float currentStep;
        private float pulse;
        private float shake;
        private float endTime;
        private Vector3 cameraHome;
        private Vector2 pointerStart;
        private bool trackingSwipe;
        private int best;
        private bool muted;
        public SnakeGame Game { get; private set; }
        public int Best => best;
        public bool Muted => muted;
        public float StepSeconds => Mathf.Max(fastestStepSeconds, initialStepSeconds - Game.Score * speedGainPerApple);

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Game = new SnakeGame(boardSize, boardSize, System.Environment.TickCount);
            best = PlayerPrefs.GetInt("GardenSnake.Best", 0);
            muted = PlayerPrefs.GetInt("GardenSnake.Muted", 0) == 1;
            audioSource.mute = muted;
            cameraHome = gameCamera.transform.position;
            segments.Add(Instantiate(headPrefab, transform).transform);
            tail = Instantiate(tailPrefab, transform).transform;
            apple = Instantiate(applePrefab, transform).transform;
            ResetVisuals();
            hud.Bind(this);
        }

        public void PrimaryAction()
        {
            if (Game.State == RunState.Paused) { TogglePause(); return; }
            if (Game.State == RunState.Playing) return;
            if ((Game.State == RunState.Lost || Game.State == RunState.Won) && Time.unscaledTime - endTime < .35f) return;
            Game.Reset();
            Game.Start();
            elapsed = 0;
            currentStep = StepSeconds;
            ResetVisuals();
            Play(startSound, 1f, .55f);
            hud.Refresh();
        }

        public void Turn(int direction)
        {
            if (Game.State == RunState.Ready) PrimaryAction();
            if (Game.QueueTurn((Direction)direction)) Play(turnSound, 1f, .16f);
        }

        public void TogglePause()
        {
            Game.TogglePause();
            trackingSwipe = false;
            hud.Refresh();
        }

        public void ToggleMute()
        {
            muted = !muted;
            audioSource.mute = muted;
            PlayerPrefs.SetInt("GardenSnake.Muted", muted ? 1 : 0);
            PlayerPrefs.Save();
            hud.Refresh();
        }

        private void Update()
        {
            ReadInput();
            if (Game.State == RunState.Playing)
            {
                // Limit catch-up after a stalled browser frame: no unseen multi-cell deaths.
                elapsed += Mathf.Min(Time.deltaTime, .1f);
                if (elapsed >= currentStep)
                {
                    elapsed -= currentStep;
                    CapturePrevious();
                    var eatenAt = World(Game.Food);
                    StepResult result = Game.Step();
                    currentStep = StepSeconds;
                    EnsureSegments();
                    if (result == StepResult.Ate || result == StepResult.Won)
                    {
                        pulse = 1;
                        shake = cameraKick * .35f;
                        pickupParticles.transform.position = eatenAt + Vector3.up * .4f;
                        pickupParticles.Play();
                        Play(pickupSound, 1f + Game.Score % 5 * .055f, .65f);
                        UpdateBest();
                        apple.position = World(Game.Food);
                        hud.ShowToast(Game.Score % 5 == 0 ? "NICE GROWING!" : "+1 APPLE");
                    }
                    if (result == StepResult.Lost || result == StepResult.Won)
                    {
                        endTime = Time.unscaledTime;
                        shake = cameraKick;
                        if (result == StepResult.Lost) Play(loseSound, 1, .6f);
                        hud.ShowToast(result == StepResult.Won ? "GARDEN COMPLETE!" : "OOPS!");
                    }
                    hud.Refresh();
                }
            }
            AnimateVisuals();
        }

        private void ReadInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) PrimaryAction();
                if (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame) TogglePause();
                if (keyboard.mKey.wasPressedThisFrame) ToggleMute();
                if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) Turn((int)Direction.Up);
                else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) Turn((int)Direction.Right);
                else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) Turn((int)Direction.Down);
                else if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) Turn((int)Direction.Left);
            }
            var pointer = Pointer.current;
            if (pointer == null) return;
            if (pointer.press.wasPressedThisFrame)
            {
                trackingSwipe = EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject();
                pointerStart = pointer.position.ReadValue();
            }
            if (trackingSwipe && pointer.press.isPressed)
            {
                Vector2 delta = pointer.position.ReadValue() - pointerStart;
                if (delta.magnitude >= Mathf.Max(24, Screen.height * .035f))
                {
                    Turn((int)(Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                        ? delta.x > 0 ? Direction.Right : Direction.Left
                        : delta.y > 0 ? Direction.Up : Direction.Down));
                    pointerStart = pointer.position.ReadValue();
                }
            }
            if (pointer.press.wasReleasedThisFrame) trackingSwipe = false;
        }

        private void EnsureSegments()
        {
            while (segments.Count < Game.Body.Count - 1)
                segments.Add(Instantiate(bodyPrefab, transform).transform);
            for (int i = 0; i < segments.Count; i++) segments[i].gameObject.SetActive(i < Game.Body.Count - 1);
            while (previous.Count < Game.Body.Count) previous.Add(previous[previous.Count - 1]);
            apple.gameObject.SetActive(Game.State != RunState.Won);
        }

        private void ResetVisuals()
        {
            CapturePrevious();
            EnsureSegments();
            elapsed = 0;
            currentStep = StepSeconds;
            pulse = shake = 0;
            for (int i = 0; i < Game.Body.Count - 1; i++) segments[i].position = World(Game.Body[i]);
            segments[0].rotation = Rotation(Game.Heading);
            tail.position = World(Game.Body[Game.Body.Count - 1]);
            apple.position = World(Game.Food);
            pickupParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void CapturePrevious()
        {
            previous.Clear();
            foreach (Cell cell in Game.Body) previous.Add(World(cell));
        }

        private void AnimateVisuals()
        {
            bool moving = Game.State == RunState.Playing || Game.State == RunState.Paused;
            float t = moving ? Mathf.Clamp01(elapsed / currentStep) : 1;
            pulse = Mathf.MoveTowards(pulse, 0, Time.deltaTime * 3.5f);
            for (int i = 0; i < Game.Body.Count; i++)
            {
                Transform part = i == Game.Body.Count - 1 ? tail : segments[i];
                part.position = Vector3.Lerp(previous[Mathf.Min(i, previous.Count - 1)], World(Game.Body[i]), t);
                float bump = pulse * Mathf.Max(0, Mathf.Sin((1 - pulse) * 9 - i * .55f));
                part.localScale = new Vector3(1 + bump * .16f, 1 + bump * .25f, 1 + bump * .16f);
                if (i == 0) part.rotation = Quaternion.Slerp(part.rotation, Rotation(Game.Heading), Time.deltaTime * 22);
                else
                {
                    Vector3 toward = World(Game.Body[i - 1]) - World(Game.Body[i]);
                    if (toward.sqrMagnitude > .01f) part.rotation = Quaternion.LookRotation(toward);
                }
            }
            apple.position = World(Game.Food) + Vector3.up * (.12f + Mathf.Sin(Time.time * 3.5f) * .065f);
            apple.rotation = Quaternion.Euler(0, Time.time * 38, 0);
            float appleScale = 1 + Mathf.Sin(Time.time * 3.5f) * .035f;
            apple.localScale = Vector3.one * appleScale;
            shake = Mathf.MoveTowards(shake, 0, Time.deltaTime * .5f);
            gameCamera.transform.position = cameraHome + new Vector3(Mathf.Sin(Time.time * 63), Mathf.Cos(Time.time * 51), 0) * shake;
            gameCamera.orthographicSize = Mathf.Max(8.7f, 7.3f / gameCamera.aspect);
        }

        private void UpdateBest()
        {
            if (Game.Score <= best) return;
            best = Game.Score;
            PlayerPrefs.SetInt("GardenSnake.Best", best);
            PlayerPrefs.Save();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && Game != null && Game.State == RunState.Playing) TogglePause();
        }

        private void Play(AudioClip clip, float pitch, float volume)
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, volume);
        }

        public Vector3 World(Cell cell) => new Vector3(cell.X - (boardSize - 1) * .5f, 0, cell.Y - (boardSize - 1) * .5f);
        private static Quaternion Rotation(Direction direction) => Quaternion.Euler(0, (int)direction * 90, 0);
    }
}
