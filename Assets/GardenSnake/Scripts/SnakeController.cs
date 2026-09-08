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
        [SerializeField, Min(6)] private int boardSize = 15;
        [SerializeField, Range(.1f, .5f)] private float initialStepSeconds = .25f;
        [SerializeField, Range(.06f, .3f)] private float fastestStepSeconds = .115f;
        [SerializeField, Range(0f, .02f)] private float speedGainPerApple = .0055f;
        [SerializeField, Range(0f, 1f)] private float openingBeat = .45f;
        [Header("Blender models")]
        [SerializeField] private GameObject headPrefab;
        [SerializeField] private GameObject bodyPrefab;
        [SerializeField] private GameObject tailPrefab;
        [SerializeField] private GameObject applePrefab;
        [Header("Presentation")]
        [SerializeField] private Camera gameCamera;
        [SerializeField] private Transform cameraRig;
        [SerializeField, Range(.5f, 4f)] private float edgePadding = 1.55f;
        [SerializeField] private ParticleSystem pickupParticles;
        [SerializeField] private ParticleSystem trailParticles;
        [SerializeField] private SnakeFeel feel;
        [SerializeField] private SnakeHud hud;
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private AudioClip turnSound;
        [SerializeField] private AudioClip loseSound;
        [SerializeField] private AudioClip startSound;

        private readonly List<Transform> segments = new List<Transform>();
        private readonly List<Vector3> previous = new List<Vector3>();
        private Transform tail;
        private Transform apple;
        private float elapsed;
        private float currentStep;
        private float pulse;
        private float slither;
        private float endTime;
        private float lastAspect;
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
            Application.runInBackground = true;
            Game = new SnakeGame(boardSize, boardSize, System.Environment.TickCount);
            best = PlayerPrefs.GetInt("GardenSnake.Best", 0);
            muted = PlayerPrefs.GetInt("GardenSnake.Muted", 0) == 1;
            audioSource.mute = muted;
            segments.Add(Instantiate(headPrefab, transform).transform);
            tail = Instantiate(tailPrefab, transform).transform;
            apple = Instantiate(applePrefab, transform).transform;
            FrameBoard();
            ResetVisuals();
            hud.Bind(this, gameCamera);
        }

        public void PrimaryAction()
        {
            if (Game.State == RunState.Paused) { TogglePause(); return; }
            if (Game.State == RunState.Playing) return;
            if ((Game.State == RunState.Lost || Game.State == RunState.Won) && Time.unscaledTime - endTime < .35f) return;
            Game.Reset();
            Game.Start();
            ResetVisuals();
            // A short beat before the first step gives the player time to read the board.
            elapsed = -openingBeat;
            Play(startSound, 1f, .55f);
            feel.RunStart(World(Game.Body[0]));
            hud.Refresh();
        }

        public void Turn(int direction)
        {
            if (Game.State == RunState.Ready) PrimaryAction();
            if (!Game.QueueTurn((Direction)direction)) return;
            Play(turnSound, 1f, .14f);
            feel.Turn(World(Game.Body[0]));
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
                if (elapsed >= currentStep) Advance();
            }
            AnimateVisuals();
        }

        private void Advance()
        {
            elapsed -= currentStep;
            CapturePrevious();
            Vector3 eatenAt = World(Game.Food);
            StepResult result = Game.Step();
            currentStep = StepSeconds;
            EnsureSegments();
            if (result == StepResult.Ate || result == StepResult.Won)
            {
                pulse = 1;
                bool record = Game.Score > best;
                UpdateBest();
                apple.position = World(Game.Food);
                feel.Pickup(eatenAt, Game.Score);
                Play(pickupSound, 1f + Game.Score % 6 * .05f, .65f);
                hud.ShowPickup("+1", eatenAt);
                if (record && Game.Score > 1)
                {
                    feel.NewBest(eatenAt);
                    hud.ShowBanner("NEW BEST");
                }
                else if (Game.Score % 10 == 0) hud.ShowBanner(Game.Score + " APPLES");
            }
            if (result == StepResult.Lost || result == StepResult.Won)
            {
                endTime = Time.unscaledTime;
                if (result == StepResult.Lost)
                {
                    Play(loseSound, 1, .6f);
                    feel.Death(World(Game.Body[0]));
                }
                else hud.ShowBanner("GARDEN COMPLETE");
            }
            hud.Refresh();
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
            pulse = 0;
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

        /// <summary>Fits the whole garden on screen at any aspect, with room for the planter rim.</summary>
        private void FrameBoard()
        {
            lastAspect = gameCamera.aspect;
            float half = boardSize * .5f + edgePadding;
            float maxX = 0;
            float maxY = 0;
            for (int corner = 0; corner < 4; corner++)
            {
                var point = new Vector3(corner < 2 ? -half : half, 0, corner % 2 == 0 ? -half : half);
                Vector3 local = gameCamera.transform.InverseTransformPoint(point);
                maxX = Mathf.Max(maxX, Mathf.Abs(local.x));
                maxY = Mathf.Max(maxY, Mathf.Abs(local.y));
            }
            gameCamera.orthographicSize = Mathf.Max(maxY, maxX / Mathf.Max(.1f, gameCamera.aspect));
        }

        private void AnimateVisuals()
        {
            if (!Mathf.Approximately(lastAspect, gameCamera.aspect)) FrameBoard();
            bool moving = Game.State == RunState.Playing;
            float t = Game.State == RunState.Playing || Game.State == RunState.Paused
                ? Mathf.Clamp01(elapsed / currentStep)
                : 1;
            pulse = Mathf.MoveTowards(pulse, 0, Time.deltaTime * 3.5f);
            if (moving) slither += Time.deltaTime / currentStep;

            Vector3 headingOffset = Vector3.zero;
            for (int i = 0; i < Game.Body.Count; i++)
            {
                Transform part = i == Game.Body.Count - 1 ? tail : segments[i];
                Vector3 target = World(Game.Body[i]);
                Vector3 from = previous[Mathf.Min(i, previous.Count - 1)];
                Vector3 position = Vector3.Lerp(from, target, t);
                // A shallow sideways wave sells "alive" without ever leaving the cell.
                Vector3 along = target - from;
                if (along.sqrMagnitude > .01f)
                {
                    Vector3 side = Vector3.Cross(Vector3.up, along.normalized);
                    position += side * (Mathf.Sin((slither - i * .42f) * 2.1f) * .055f);
                }
                if (i == 0) headingOffset = position;
                float bump = pulse * Mathf.Max(0, Mathf.Sin((1 - pulse) * 9 - i * .55f));
                part.position = position + Vector3.up * (bump * .07f);
                part.localScale = new Vector3(1 + bump * .18f, 1 + bump * .3f, 1 + bump * .18f);
                if (i == 0) part.rotation = Quaternion.Slerp(part.rotation, Rotation(Game.Heading), Time.deltaTime * 22);
                else
                {
                    Vector3 toward = World(Game.Body[i - 1]) - target;
                    if (toward.sqrMagnitude > .01f) part.rotation = Quaternion.LookRotation(toward);
                }
            }

            var emission = trailParticles.emission;
            emission.enabled = moving;
            if (moving && !trailParticles.isPlaying) trailParticles.Play();
            if (!moving && trailParticles.isPlaying) trailParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            trailParticles.transform.position = headingOffset + Vector3.up * .1f;

            apple.position = World(Game.Food) + Vector3.up * (.14f + Mathf.Sin(Time.time * 3.2f) * .07f);
            apple.rotation = Quaternion.Euler(0, Time.time * 34, Mathf.Sin(Time.time * 2.1f) * 6f);
            apple.localScale = Vector3.one * (1 + Mathf.Sin(Time.time * 3.2f) * .04f);
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

        public Vector3 World(Cell cell) =>
            new Vector3(cell.X - (boardSize - 1) * .5f, 0, cell.Y - (boardSize - 1) * .5f);

        private static Quaternion Rotation(Direction direction) => Quaternion.Euler(0, (int)direction * 90, 0);
    }
}
