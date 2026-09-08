using System.Collections.Generic;
using GardenSnake.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace GardenSnake
{
    /// <summary>
    /// Drives one run: reads input, ticks the simulation on a fixed step, and presents the result.
    /// Nothing here decides rules; it owns timing, motion and the noises the garden makes.
    /// </summary>
    public sealed class SnakeController : MonoBehaviour
    {
        [Header("Board")]
        [SerializeField, Min(6)] private int boardWidth = 21;
        [SerializeField, Min(6)] private int boardHeight = 12;
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
        [SerializeField, Range(.5f, 4f)] private float edgePadding = 1f;
        [SerializeField, Range(0f, .3f)] private float hudBandTop = .105f;
        [SerializeField, Range(0f, .3f)] private float hudBandBottom = .095f;
        [SerializeField, Range(1f, 1.4f)] private float restingZoom = 1.13f;
        [SerializeField, Range(.5f, 8f)] private float zoomSpeed = 3.4f;
        [SerializeField] private Volume paceVolume;
        [SerializeField] private Transform appleMarker;
        [SerializeField] private Transform burstRing;
        [SerializeField] private ParticleSystem pickupParticles;
        [SerializeField] private ParticleSystem trailParticles;
        [SerializeField] private SnakeFeel feel;
        [SerializeField] private SnakeHud hud;
        [Header("Snake proportions")]
        [SerializeField, Range(.8f, 1.6f)] private float headScale = 1.22f;
        [SerializeField, Range(.8f, 1.6f)] private float bodyScale = 1.3f;
        [SerializeField, Range(.8f, 1.6f)] private float tailScale = 1.24f;
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private AudioClip turnSound;
        [SerializeField] private AudioClip loseSound;
        [SerializeField] private AudioClip startSound;
        [SerializeField] private AudioClip bestSound;
        [SerializeField] private AudioClip clickSound;

        private const float DeathBeat = .26f;

        private readonly List<Transform> segments = new List<Transform>();
        private readonly List<Vector3> previous = new List<Vector3>();
        private Transform tail;
        private Transform apple;
        private float elapsed;
        private float currentStep;
        private float pulse;
        private float slither;
        private float bank;
        private float appleAge;
        private float burstAge = 99;
        private Material burstMaterial;
        private float deathAge;
        private float endTime;
        private float lastAspect;
        private float fitSize;
        private float viewSize;
        private int capturedCount;
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
            Application.runInBackground = true;
            Game = new SnakeGame(boardWidth, boardHeight, System.Environment.TickCount);
            best = PlayerPrefs.GetInt("GardenSnake.Best", 0);
            muted = PlayerPrefs.GetInt("GardenSnake.Muted", 0) == 1;
            ApplyMute();
            cameraHome = gameCamera.transform.localPosition;
            burstMaterial = burstRing.GetComponent<Renderer>().material;
            segments.Add(Instantiate(headPrefab, transform).transform);
            tail = Instantiate(tailPrefab, transform).transform;
            apple = Instantiate(applePrefab, transform).transform;
            Prewarm();
            FrameBoard();
            ResetVisuals();
            hud.Bind(this, gameCamera);
            musicSource.Play();
        }

        // ---------------------------------------------------------------- player actions

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
            Play(startSound, 1f, .5f);
            feel.RunStart(World(Game.Body[0]));
            hud.Refresh();
        }

        public void Turn(int direction)
        {
            if (Game.State == RunState.Ready) PrimaryAction();
            Direction wish = (Direction)direction;
            int turnSign = TurnSign(Game.Heading, wish);
            if (!Game.QueueTurn(wish)) return;
            bank = turnSign;
            Play(turnSound, Random.Range(.96f, 1.06f), .16f);
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
            ApplyMute();
            PlayerPrefs.SetInt("GardenSnake.Muted", muted ? 1 : 0);
            PlayerPrefs.Save();
            hud.Refresh();
        }

        /// <summary>The small confirmation every button press gets.</summary>
        public void Click() => Play(clickSound, 1f, .35f);

        private void ApplyMute()
        {
            audioSource.mute = muted;
            musicSource.mute = muted;
        }

        // ---------------------------------------------------------------- loop

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
                appleAge = 0;
                bool record = Game.Score > best;
                UpdateBest();
                apple.position = World(Game.Food);
                feel.Pickup(eatenAt, Game.Score);
                burstAge = 0;
                burstRing.position = eatenAt + Vector3.up * .05f;
                Play(pickupSound, 1f + Game.Score % 6 * .045f, .6f);
                hud.ShowPickup("+1", eatenAt);
                if (record && Game.Score > 1)
                {
                    feel.NewBest(eatenAt);
                    Play(bestSound, 1f, .45f);
                    hud.ShowBanner("NEW BEST");
                }
                else if (Game.Score % 10 == 0) hud.ShowBanner(Game.Score + " APPLES");
            }
            if (result == StepResult.Lost || result == StepResult.Won)
            {
                endTime = Time.unscaledTime;
                deathAge = 0;
                if (result == StepResult.Lost)
                {
                    Play(loseSound, 1, .55f);
                    feel.Death(World(Game.Body[0]));
                    pickupParticles.transform.position = World(Game.Body[0]) + Vector3.up * .3f;
                    pickupParticles.Emit(18);
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

        // ---------------------------------------------------------------- presentation

        /// <summary>
        /// Every body segment a full board could ever need is built once, before the first run.
        /// Growing then costs one SetActive instead of an Instantiate, so eating an apple never
        /// stalls the frame it lands on.
        /// </summary>
        private void Prewarm()
        {
            int capacity = boardWidth * boardHeight;
            segments.Capacity = capacity;
            previous.Capacity = capacity;
            for (int i = segments.Count; i < capacity - 1; i++)
            {
                Transform part = Instantiate(bodyPrefab, transform).transform;
                part.gameObject.SetActive(false);
                segments.Add(part);
            }
            while (previous.Count < capacity) previous.Add(Vector3.zero);
        }

        private void EnsureSegments()
        {
            // A new segment starts where the one in front of it was, not at the world origin.
            while (capturedCount < Game.Body.Count)
            {
                previous[capturedCount] = previous[Mathf.Max(0, capturedCount - 1)];
                capturedCount++;
            }
            int live = Game.Body.Count - 1;
            for (int i = 0; i < segments.Count; i++)
            {
                bool wanted = i < live;
                if (segments[i].gameObject.activeSelf != wanted) segments[i].gameObject.SetActive(wanted);
            }
            if (apple.gameObject.activeSelf != (Game.State != RunState.Won))
                apple.gameObject.SetActive(Game.State != RunState.Won);
        }

        private void ResetVisuals()
        {
            CapturePrevious();
            EnsureSegments();
            elapsed = 0;
            currentStep = StepSeconds;
            pulse = 0;
            bank = 0;
            appleAge = 0;
            deathAge = 0;
            burstAge = 99;
            for (int i = 0; i < Game.Body.Count - 1; i++)
            {
                segments[i].position = World(Game.Body[i]);
                segments[i].localScale = Vector3.one * (i == 0 ? headScale : bodyScale);
            }
            segments[0].rotation = Rotation(Game.Heading);
            tail.position = World(Game.Body[Game.Body.Count - 1]);
            tail.localScale = Vector3.one * tailScale;
            apple.position = World(Game.Food);
            pickupParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void CapturePrevious()
        {
            // Indexed, not foreach: enumerating the read-only body through its interface boxes.
            for (int i = 0; i < Game.Body.Count; i++) previous[i] = World(Game.Body[i]);
            capturedCount = Game.Body.Count;
        }

        /// <summary>
        /// Works out the orthographic size that fits the whole garden between the HUD bands at
        /// the current aspect. The result is a target; the camera eases towards it so a run can
        /// push in and the menus can sit back.
        /// </summary>
        private void FrameBoard()
        {
            lastAspect = gameCamera.aspect;
            float halfX = boardWidth * .5f + edgePadding;
            float halfZ = boardHeight * .5f + edgePadding;
            float maxX = 0;
            float maxY = 0;
            for (int corner = 0; corner < 4; corner++)
            {
                var point = new Vector3(corner < 2 ? -halfX : halfX, 0, corner % 2 == 0 ? -halfZ : halfZ);
                Vector3 local = gameCamera.transform.InverseTransformPoint(point);
                maxX = Mathf.Max(maxX, Mathf.Abs(local.x));
                maxY = Mathf.Max(maxY, Mathf.Abs(local.y));
            }
            float band = Mathf.Clamp01(1 - hudBandTop - hudBandBottom);
            fitSize = Mathf.Max(maxY / band, maxX / Mathf.Max(.1f, gameCamera.aspect));
            if (viewSize <= 0) viewSize = fitSize * restingZoom;
        }

        /// <summary>Push in while a run is going, sit back on the menus and after a death.</summary>
        private void ApplyZoom()
        {
            float target = fitSize * (Game.State == RunState.Playing ? 1f : restingZoom);
            viewSize = Mathf.Lerp(viewSize, target, 1 - Mathf.Exp(-zoomSpeed * Time.unscaledDeltaTime));
            gameCamera.orthographicSize = viewSize;
            // Slide the view so the leftover space splits into the two bands we asked for.
            float shift = (hudBandTop - hudBandBottom) * viewSize;
            gameCamera.transform.localPosition = cameraHome + gameCamera.transform.localRotation * (Vector3.up * shift);
        }

        private void AnimateVisuals()
        {
            if (!Mathf.Approximately(lastAspect, gameCamera.aspect)) FrameBoard();
            ApplyZoom();
            float delta = Time.unscaledDeltaTime;
            bool moving = Game.State == RunState.Playing;
            bool dying = Game.State == RunState.Lost;
            deathAge = dying ? deathAge + delta : 0;
            pulse = Mathf.MoveTowards(pulse, 0, Time.deltaTime * 3.5f);
            bank = Mathf.MoveTowards(bank, 0, delta * 3.4f);
            appleAge += delta;
            if (moving) slither += Time.deltaTime / currentStep;

            AnimateSnake(moving, dying);
            AnimateTrail(moving);
            AnimateApple();
            AnimateBurst(delta);
            float pace = Mathf.InverseLerp(initialStepSeconds, fastestStepSeconds, currentStep);
            float wanted = Game.State == RunState.Playing ? pace : 0;
            paceVolume.weight = Mathf.MoveTowards(paceVolume.weight, wanted, delta * 1.2f);
        }

        private void AnimateSnake(bool moving, bool dying)
        {
            float t = Game.State == RunState.Playing || Game.State == RunState.Paused
                ? Mathf.Clamp01(elapsed / currentStep)
                : 1;
            for (int i = 0; i < Game.Body.Count; i++)
            {
                Transform part = i == Game.Body.Count - 1 ? tail : segments[i];
                Vector3 target = World(Game.Body[i]);
                Vector3 from = previous[i];
                Vector3 position = Vector3.Lerp(from, target, t);
                // A shallow sideways wave sells "alive" without ever leaving the cell.
                Vector3 along = target - from;
                if (along.sqrMagnitude > .01f && !dying)
                {
                    Vector3 side = Vector3.Cross(Vector3.up, along.normalized);
                    position += side * (Mathf.Sin((slither - i * .42f) * 2.1f) * .055f);
                }
                float basis = i == 0 ? headScale : i == Game.Body.Count - 1 ? tailScale : bodyScale;
                float bump = pulse * Mathf.Max(0, Mathf.Sin((1 - pulse) * 9 - i * .55f));
                Vector3 scale = new Vector3(1 + bump * .18f, 1 + bump * .3f, 1 + bump * .18f);
                if (Game.State == RunState.Ready)
                {
                    float breath = Mathf.Sin(Time.unscaledTime * 2.4f - i * .5f) * .035f;
                    scale += new Vector3(-breath, breath * 2.2f, -breath);
                    position += Vector3.up * (breath * .5f);
                }
                if (dying)
                {
                    // Each piece swells and pops out of existence, head first, so the board is
                    // clear by the time the results card arrives.
                    float stagger = Mathf.Min(.05f, .34f / Mathf.Max(1, Game.Body.Count));
                    float pop = Mathf.Clamp01((deathAge - i * stagger) / DeathBeat);
                    float swell = Mathf.Sin(pop * Mathf.PI) * .35f;
                    float shrink = pop < .55f ? 1 : 1 - (pop - .55f) / .45f;
                    scale = Vector3.one * ((1 + swell) * shrink * shrink);
                    position += Vector3.up * (pop * .22f);
                }
                part.position = position + Vector3.up * (bump * .07f);
                part.localScale = basis * scale;
                if (i == 0)
                {
                    Quaternion facing = Rotation(Game.Heading) * Quaternion.Euler(0, 0, bank * -16f);
                    part.rotation = Quaternion.Slerp(part.rotation, facing, Time.unscaledDeltaTime * 22);
                }
                else
                {
                    Vector3 toward = World(Game.Body[i - 1]) - target;
                    if (toward.sqrMagnitude > .01f) part.rotation = Quaternion.LookRotation(toward);
                }
            }
        }

        /// <summary>One expanding ring per apple: the pickup gets a shape, not just particles.</summary>
        private void AnimateBurst(float delta)
        {
            const float life = .42f;
            if (burstAge >= life)
            {
                if (burstRing.gameObject.activeSelf) burstRing.gameObject.SetActive(false);
                return;
            }
            burstAge += delta;
            if (!burstRing.gameObject.activeSelf) burstRing.gameObject.SetActive(true);
            float t = Mathf.Clamp01(burstAge / life);
            float eased = 1 - Mathf.Pow(1 - t, 2.6f);
            burstRing.localScale = Vector3.one * Mathf.Lerp(.6f, 2.5f, eased);
            burstMaterial.SetFloat("_Alpha", (1 - t) * (1 - t) * .55f);
        }

        private void AnimateTrail(bool moving)
        {
            var emission = trailParticles.emission;
            // The faster the run gets, the more the snake leaves behind it.
            emission.rateOverTime = Mathf.Lerp(10, 30, Mathf.InverseLerp(initialStepSeconds, fastestStepSeconds, currentStep));
            if (moving && !trailParticles.isPlaying) trailParticles.Play();
            if (!moving && trailParticles.isPlaying) trailParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            trailParticles.transform.position = segments[0].position + Vector3.up * .12f;
        }

        private void AnimateApple()
        {
            float breathe = Mathf.Sin(Time.unscaledTime * 3.2f);
            // A fresh apple drops in with a little overshoot rather than blinking into place.
            float arrival = Mathf.Clamp01(appleAge / .34f);
            float pop = arrival >= 1 ? 1 : 1 - Mathf.Pow(1 - arrival, 3) * Mathf.Cos(arrival * 9f) * .55f;
            apple.position = World(Game.Food) + Vector3.up * (.14f + breathe * .07f + (1 - arrival) * .5f);
            apple.rotation = Quaternion.Euler(0, Time.unscaledTime * 34, Mathf.Sin(Time.unscaledTime * 2.1f) * 6f);
            apple.localScale = Vector3.one * ((1 + breathe * .04f) * pop);
            appleMarker.gameObject.SetActive(apple.gameObject.activeSelf);
            appleMarker.position = World(Game.Food) + Vector3.up * .04f;
            appleMarker.localScale = Vector3.one * ((2.1f + breathe * .18f) * arrival);
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
            new Vector3(cell.X - (boardWidth - 1) * .5f, 0, cell.Y - (boardHeight - 1) * .5f);

        private static Quaternion Rotation(Direction direction) => Quaternion.Euler(0, (int)direction * 90, 0);

        /// <summary>-1 for a left turn, 1 for a right turn, 0 when the heading does not change.</summary>
        private static int TurnSign(Direction from, Direction to)
        {
            int difference = ((int)to - (int)from + 4) % 4;
            return difference == 1 ? 1 : difference == 3 ? -1 : 0;
        }
    }
}
