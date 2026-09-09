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
        [Header("Rules (applied on entering Play Mode)")]
        [SerializeField, Range(.05f, 1f), Tooltip("Cells traveled by swallowed food per movement step.")]
        private float digestionPerStep = SnakeGame.DigestionPerStep;
        [SerializeField, Min(2)] private int initialLength = 3;
        [SerializeField, Range(1, 8)] private int turnBufferSize = 2;
        [SerializeField, Min(1)] private int firstAppleDistance = 2;
        [Header("Shared snake tuning")]
        [SerializeField] private SnakeSkinSettings skinSettings;
        [SerializeField] private SnakeMouthSettings mouthSettings;
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
        [SerializeField] private GridCellWaves cellWaves;
        [Header("Snake proportions")]
        [SerializeField, Range(.8f, 1.6f)] private float headScale = 1.22f;
        [SerializeField, Range(.8f, 1.6f)] private float bodyScale = 1.3f;
        [SerializeField, Range(.8f, 1.6f)] private float tailScale = 1.24f;
        [SerializeField, Range(.8f, 1.6f)] private float appleScale = 1.16f;
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private AudioClip turnSound;
        [SerializeField] private AudioClip loseSound;
        [SerializeField] private AudioClip startSound;
        [SerializeField] private AudioClip bestSound;
        [SerializeField] private AudioClip clickSound;

        [Header("Input and runtime")]
        [SerializeField, Min(1)] private int targetFrameRate = 60;
        [SerializeField] private bool runInBackground = true;
        [SerializeField, Min(0)] private float restartDelay = .35f;
        [SerializeField, Min(1)] private float swipeMinimumPixels = 24;
        [SerializeField, Range(0, 1)] private float swipeScreenHeightFraction = .035f;
        [Header("Snake motion")]
        [SerializeField, Min(0)] private float bankRecoverySpeed = 3.4f;
        [SerializeField] private float bankAngle = -16;
        [SerializeField, Min(0)] private float headTurnSpeed = 22;
        [SerializeField, Min(0)] private float slitherAmplitude = .055f;
        [SerializeField, Min(0)] private float slitherFrequency = 2.1f;
        [SerializeField] private float slitherSegmentPhase = .42f;
        [SerializeField, Min(.01f)] private float growthDuration = .2f;
        [SerializeField, Min(0)] private float growthSwell = .2f;
        [SerializeField, Min(0)] private float idleBreathFrequency = 2.4f;
        [SerializeField] private float idleBreathSegmentPhase = .5f;
        [SerializeField, Range(0, .5f)] private float idleBreathAmplitude = .035f;
        [SerializeField, Min(0)] private float idleBreathStretch = 2.2f;
        [SerializeField] private float idleBreathLift = .5f;
        [Header("Blink")]
        [SerializeField, Min(0)] private float firstBlinkDelay = 2.5f;
        [SerializeField, Tooltip("Minimum and maximum seconds between blinks.")] private Vector2 blinkInterval = new Vector2(2.2f, 5.5f);
        [SerializeField, Min(.01f)] private float blinkDuration = .16f;
        [SerializeField, Range(0, 1)] private float blinkClosure = .92f;
        [Header("Death animation")]
        [SerializeField, Min(.01f)] private float deathBeat = .26f;
        [SerializeField, Min(.01f)] private float deathRecoilDuration = .1f;
        [SerializeField, Min(0)] private float deathRecoilDistance = .18f;
        [SerializeField, Min(0)] private float deathSegmentDelay = .05f;
        [SerializeField, Min(0)] private float deathTotalStagger = .34f;
        [SerializeField, Min(0)] private float deathSwell = .35f;
        [SerializeField, Range(0, .99f)] private float deathShrinkStart = .55f;
        [SerializeField] private float deathLift = .22f;
        [SerializeField, Min(0)] private int deathParticleCount = 18;
        [SerializeField] private float deathParticleHeight = .3f;
        [Header("Apple animation")]
        [SerializeField, Min(0)] private float appleBreathFrequency = 3.2f;
        [SerializeField, Min(.01f)] private float appleArrivalDuration = .34f;
        [SerializeField, Min(.01f)] private float appleArrivalEasePower = 3;
        [SerializeField, Min(0)] private float appleArrivalOscillation = 9;
        [SerializeField, Range(0, 1)] private float appleArrivalSwell = .55f;
        [SerializeField] private float appleHoverHeight = .14f;
        [SerializeField, Min(0)] private float appleBobAmplitude = .07f;
        [SerializeField] private float appleDropHeight = .5f;
        [SerializeField] private float appleSpinSpeed = 34;
        [SerializeField, Min(0)] private float appleRockFrequency = 2.1f;
        [SerializeField] private float appleRockAngle = 6;
        [SerializeField, Range(0, 1)] private float appleBreathScale = .04f;
        [SerializeField] private float appleMarkerHeight = .04f;
        [SerializeField, Min(0)] private float appleMarkerScale = 2.1f;
        [SerializeField, Min(0)] private float appleMarkerPulse = .18f;
        [Header("Pickup ring and trail")]
        [SerializeField, Min(.01f)] private float burstDuration = .42f;
        [SerializeField, Min(.01f)] private float burstEasePower = 2.6f;
        [SerializeField] private Vector2 burstScale = new Vector2(.6f, 2.5f);
        [SerializeField, Range(0, 1)] private float burstOpacity = .55f;
        [SerializeField] private float burstHeight = .05f;
        [SerializeField, Tooltip("Particles per second at starting and maximum pace.")] private Vector2 trailEmission = new Vector2(10, 30);
        [SerializeField] private float trailHeight = .12f;
        [Header("Pace and celebrations")]
        [SerializeField, Min(0)] private float paceVolumeBlendSpeed = 1.2f;
        [SerializeField, Min(1)] private int milestoneAppleInterval = 10;
        [SerializeField, Min(0)] private float victoryWaveIntensity = 1.5f;
        [SerializeField] private GridCellWaves.Pattern startWave = GridCellWaves.Pattern.Sweep;
        [SerializeField] private GridCellWaves.Pattern deathWave = GridCellWaves.Pattern.Ripple;
        [SerializeField] private GridCellWaves.Pattern bestWave = GridCellWaves.Pattern.Bloom;
        [SerializeField] private GridCellWaves.Pattern milestoneWave = GridCellWaves.Pattern.CheckerHop;
        [SerializeField] private GridCellWaves.Pattern victoryWave = GridCellWaves.Pattern.Bloom;
        [SerializeField, Min(0)] private float startWaveIntensity = 1;
        [SerializeField, Min(0)] private float deathWaveIntensity = 1;
        [SerializeField, Min(0)] private float bestWaveIntensity = 1;
        [SerializeField, Min(0)] private float milestoneWaveIntensity = 1;
        [Header("Sound tuning")]
        [SerializeField, Range(.1f, 3)] private float startPitch = 1;
        [SerializeField, Range(0, 1)] private float startVolume = .5f;
        [SerializeField] private Vector2 turnPitch = new Vector2(.96f, 1.06f);
        [SerializeField, Range(0, 1)] private float turnVolume = .16f;
        [SerializeField, Range(.1f, 3)] private float clickPitch = 1;
        [SerializeField, Range(0, 1)] private float clickVolume = .35f;
        [SerializeField, Range(.1f, 3)] private float pickupPitch = 1;
        [SerializeField, Min(1)] private int pickupPitchCycle = 6;
        [SerializeField, Min(0)] private float pickupPitchIncrement = .045f;
        [SerializeField, Range(0, 1)] private float pickupVolume = .6f;
        [SerializeField, Range(.1f, 3)] private float bestPitch = 1;
        [SerializeField, Range(0, 1)] private float bestVolume = .45f;
        [SerializeField, Range(.1f, 3)] private float recordPitch = 1.35f;
        [SerializeField, Range(0, 1)] private float recordVolume = .1f;
        [SerializeField, Range(.1f, 3)] private float losePitch = 1;
        [SerializeField, Range(0, 1)] private float loseVolume = .55f;

        private readonly List<Transform> segments = new List<Transform>();
        private readonly List<Vector3> previous = new List<Vector3>();
        private Transform tail;
        private SnakeSkin skin;
        private SnakeMouth mouth;
        private Transform apple;
        private float elapsed;
        private float currentStep;
        private float slither;
        private float bank;
        private float appleAge;
        private float burstAge = 99;
        private Material burstMaterial;
        private Transform[] eyelids;
        private Vector3[] eyelidRest;
        private float blinkAge;
        private float nextBlink;
        private int grownIndex = -1;
        private float grownAge = 99;
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
        private bool hasPlayed;
        private readonly RunRecord record = new RunRecord();
        public RunRecord Record => record;
        public int RecordCelebrations { get; private set; }
        public int RecordWhispers { get; private set; }
        public float Pace => Mathf.InverseLerp(initialStepSeconds, fastestStepSeconds, StepSeconds);
        private bool bestUnsaved;
        private bool muted;
        public SnakeGame Game { get; private set; }
        public int Best => best;
        public bool Muted => muted;
        public float StepSeconds => Mathf.Max(fastestStepSeconds, initialStepSeconds - Game.Score * speedGainPerApple);

        private void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            Application.runInBackground = runInBackground;
            nextBlink = firstBlinkDelay;
            Game = new SnakeGame(boardWidth, boardHeight, System.Environment.TickCount,
                digestionPerStep, initialLength, turnBufferSize, firstAppleDistance);
            best = PlayerPrefs.GetInt("GardenSnake.Best", 0);
            hasPlayed = PlayerPrefs.GetInt("GardenSnake.HasPlayed", 0) == 1 || best > 0;
            muted = PlayerPrefs.GetInt("GardenSnake.Muted", 0) == 1;
            ApplyMute();
            cameraHome = gameCamera.transform.localPosition;
            burstMaterial = burstRing.GetComponent<Renderer>().material;
            segments.Add(Instantiate(headPrefab, transform).transform);
            mouth = segments[0].gameObject.AddComponent<SnakeMouth>();
            mouth.Initialize(applePrefab, mouthSettings);
            CollectEyelids();
            tail = new GameObject("Tail pose").transform;
            tail.SetParent(transform, false);
            var skinObject = new GameObject("Snake skin");
            skinObject.transform.SetParent(transform, false);
            skin = skinObject.AddComponent<SnakeSkin>();
            skin.Initialize(bodyPrefab, boardWidth * boardHeight, skinSettings);
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
            if ((Game.State == RunState.Lost || Game.State == RunState.Won) && Time.unscaledTime - endTime < restartDelay) return;
            record.Begin(best, hasPlayed);
            RecordCelebrations = RecordWhispers = 0;
            hasPlayed = true;
            PlayerPrefs.SetInt("GardenSnake.HasPlayed", 1);
            PlayerPrefs.Save();
            Game.Reset();
            Game.Start();
            cellWaves.Clear();
            ResetVisuals();
            // A short beat before the first step gives the player time to read the board.
            elapsed = -openingBeat;
            Play(startSound, startPitch, startVolume);
            feel.RunStart(World(Game.Body[0]));
            cellWaves.Play(startWave, Game.Body[0], startWaveIntensity);
            hud.Refresh();
        }

        public void Turn(int direction)
        {
            if (Game.State == RunState.Ready) PrimaryAction();
            Direction wish = (Direction)direction;
            int turnSign = TurnSign(Game.Heading, wish);
            if (!Game.QueueTurn(wish)) return;
            bank = turnSign;
            Play(turnSound, Random.Range(turnPitch.x, turnPitch.y), turnVolume);
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
        public void Click() => Play(clickSound, clickPitch, clickVolume);

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
            int oldLength = Game.Body.Count;
            StepResult result = Game.Step();
            currentStep = StepSeconds;
            EnsureSegments();
            if (Game.Body.Count > oldLength)
            {
                grownIndex = Game.Body.Count - 2;
                grownAge = 0;
            }
            if (result == StepResult.Ate)
            {
                mouth.Swallow(apple, currentStep);
                appleAge = 0;
                RecordBeat recordBeat = record.Apple(Game.Score);
                UpdateBest();
                apple.position = World(Game.Food);
                feel.Pickup(eatenAt, Game.Score);
                burstAge = 0;
                burstRing.position = eatenAt + Vector3.up * burstHeight;
                Play(pickupSound, pickupPitch + Game.Score % Mathf.Max(1, pickupPitchCycle) * pickupPitchIncrement, pickupVolume);
                hud.ShowPickup(recordBeat == RecordBeat.Extended ? "+1 <size=55%>best</size>" : "+1", eatenAt, recordBeat == RecordBeat.Extended);
                if (recordBeat == RecordBeat.Broken)
                {
                    RecordCelebrations++;
                    cellWaves.Play(bestWave, Game.Body[0], bestWaveIntensity);
                    feel.NewBest(eatenAt);
                    Play(bestSound, bestPitch, bestVolume);
                    hud.ShowBanner("NEW BEST");
                }
                else if (recordBeat == RecordBeat.Extended)
                {
                    RecordWhispers++;
                    feel.RecordApple(eatenAt);
                    Play(bestSound, recordPitch, recordVolume);
                    hud.WhisperBest();
                }
                else if (Game.Score % Mathf.Max(1, milestoneAppleInterval) == 0)
                {
                    cellWaves.Play(milestoneWave, Game.Body[0], milestoneWaveIntensity);
                    hud.ShowBanner(Game.Score + " APPLES");
                }
            }
            if (result == StepResult.Lost || result == StepResult.Won)
            {
                endTime = Time.unscaledTime;
                deathAge = 0;
                SaveBest();
                if (result == StepResult.Lost)
                {
                    Play(loseSound, losePitch, loseVolume);
                    feel.Death(World(Game.Body[0]));
                    cellWaves.Play(deathWave, Game.Body[0], deathWaveIntensity);
                    pickupParticles.transform.position = World(Game.Body[0]) + Vector3.up * deathParticleHeight;
                    pickupParticles.Emit(deathParticleCount);
                }
                else
                {
                    cellWaves.Play(victoryWave, Game.Body[0], victoryWaveIntensity);
                    hud.ShowBanner("GARDEN COMPLETE");
                }
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
                if (delta.magnitude >= Mathf.Max(swipeMinimumPixels, Screen.height * swipeScreenHeightFraction))
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
                Transform part = new GameObject("Body pose " + i).transform;
                part.SetParent(transform, false);
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
            bool showApple = Game.HasFood && Game.State != RunState.Won;
            if (apple.gameObject.activeSelf != showApple) apple.gameObject.SetActive(showApple);
        }

        private void ResetVisuals()
        {
            CapturePrevious();
            EnsureSegments();
            elapsed = 0;
            currentStep = StepSeconds;
            bank = 0;
            appleAge = 0;
            deathAge = 0;
            burstAge = 99;
            grownIndex = -1;
            grownAge = 99;
            mouth.ResetPose();
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

        /// <summary>
        /// The head model carries its eyes as separate pieces, so a blink is just a squash on
        /// those. Collected once; the names come straight from the Blender source.
        /// </summary>
        private void CollectEyelids()
        {
            var found = new List<Transform>();
            foreach (Transform part in segments[0].GetComponentsInChildren<Transform>())
                if (part.name.StartsWith("Eye white") || part.name.StartsWith("Pupil") ||
                    part.name.StartsWith("Eye glint"))
                    found.Add(part);
            eyelids = found.ToArray();
            eyelidRest = new Vector3[eyelids.Length];
            for (int i = 0; i < eyelids.Length; i++) eyelidRest[i] = eyelids[i].localScale;
        }

        private void Blink(float delta)
        {
            if (eyelids.Length == 0) return;
            blinkAge += delta;
            if (blinkAge > nextBlink + blinkDuration)
            {
                blinkAge = 0;
                nextBlink = Random.Range(blinkInterval.x, blinkInterval.y);
            }
            float open = blinkAge < nextBlink
                ? 1
                : 1 - Mathf.Sin(Mathf.Clamp01((blinkAge - nextBlink) / Mathf.Max(.01f, blinkDuration)) * Mathf.PI) * blinkClosure;
            for (int i = 0; i < eyelids.Length; i++)
            {
                Vector3 rest = eyelidRest[i];
                eyelids[i].localScale = new Vector3(rest.x, rest.y * open, rest.z);
            }
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
            if (Game.State != RunState.Paused) grownAge += delta;
            Blink(delta);
            bank = Mathf.MoveTowards(bank, 0, delta * bankRecoverySpeed);
            appleAge += delta;
            if (moving) slither += Time.deltaTime / currentStep;

            AnimateSnake(moving, dying);
            AnimateTrail(moving);
            AnimateApple();
            mouth.Animate(Game, apple.position, Game.State == RunState.Paused ? 0 : delta);
            AnimateBurst(delta);
            float pace = Mathf.InverseLerp(initialStepSeconds, fastestStepSeconds, currentStep);
            float wanted = Game.State == RunState.Playing ? pace : 0;
            paceVolume.weight = Mathf.MoveTowards(paceVolume.weight, wanted, delta * paceVolumeBlendSpeed);
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
                    position += side * (Mathf.Sin((slither - i * slitherSegmentPhase) * slitherFrequency) * slitherAmplitude);
                }
                float basis = i == 0 ? headScale : i == Game.Body.Count - 1 ? tailScale : bodyScale;
                Vector3 scale = Vector3.one;
                if (i == grownIndex && grownAge < growthDuration)
                {
                    float birth = grownAge / Mathf.Max(.01f, growthDuration);
                    scale *= 1 + Mathf.Sin(birth * Mathf.PI) * growthSwell;
                }
                if (Game.State == RunState.Ready)
                {
                    float breath = Mathf.Sin(Time.unscaledTime * idleBreathFrequency - i * idleBreathSegmentPhase) * idleBreathAmplitude;
                    scale += new Vector3(-breath, breath * idleBreathStretch, -breath);
                    position += Vector3.up * (breath * idleBreathLift);
                }
                if (dying && i == 0 && deathAge < deathRecoilDuration)
                {
                    // The head shoves into whatever stopped it before the snake gives up.
                    float recoil = Mathf.Sin(deathAge / Mathf.Max(.01f, deathRecoilDuration) * Mathf.PI) * deathRecoilDistance;
                    position += (World(Game.Body[0]) - World(Game.Body[1])).normalized * recoil;
                }
                if (dying)
                {
                    // Each piece swells and pops out of existence, head first, so the board is
                    // clear by the time the results card arrives.
                    float stagger = Mathf.Min(deathSegmentDelay, deathTotalStagger / Mathf.Max(1, Game.Body.Count));
                    float pop = Mathf.Clamp01((deathAge - i * stagger) / Mathf.Max(.01f, deathBeat));
                    float swell = Mathf.Sin(pop * Mathf.PI) * deathSwell;
                    float shrink = pop < deathShrinkStart ? 1 : 1 - (pop - deathShrinkStart) / Mathf.Max(.01f, 1 - deathShrinkStart);
                    scale = Vector3.one * ((1 + swell) * shrink * shrink);
                    position += Vector3.up * (pop * deathLift);
                }
                part.position = position + Vector3.up * cellWaves.HeightAt(position);
                part.localScale = basis * scale;
                if (i == 0)
                {
                    Quaternion facing = Rotation(Game.Heading) * Quaternion.Euler(0, 0, bank * bankAngle);
                    part.rotation = Quaternion.Slerp(part.rotation, facing, Time.unscaledDeltaTime * headTurnSpeed);
                }
                else
                {
                    Vector3 toward = segments[i - 1].position - part.position;
                    if (toward.sqrMagnitude > .01f) part.rotation = Quaternion.LookRotation(toward);
                }
            }
            skin.Draw(segments, tail, Game.Body.Count, bodyScale, headScale, tailScale,
                Game.Digestion, t, dying ? 0 : 1, Game.DigestionSpeed);
        }

        /// <summary>One expanding ring per apple: the pickup gets a shape, not just particles.</summary>
        private void AnimateBurst(float delta)
        {
            if (burstAge >= burstDuration)
            {
                if (burstRing.gameObject.activeSelf) burstRing.gameObject.SetActive(false);
                return;
            }
            burstAge += delta;
            if (!burstRing.gameObject.activeSelf) burstRing.gameObject.SetActive(true);
            float t = Mathf.Clamp01(burstAge / Mathf.Max(.01f, burstDuration));
            float eased = 1 - Mathf.Pow(1 - t, burstEasePower);
            burstRing.localScale = Vector3.one * Mathf.Lerp(burstScale.x, burstScale.y, eased);
            burstMaterial.SetFloat("_Alpha", (1 - t) * (1 - t) * burstOpacity);
        }

        private void AnimateTrail(bool moving)
        {
            var emission = trailParticles.emission;
            // The faster the run gets, the more the snake leaves behind it.
            emission.rateOverTime = Mathf.Lerp(trailEmission.x, trailEmission.y, Mathf.InverseLerp(initialStepSeconds, fastestStepSeconds, currentStep));
            if (moving && !trailParticles.isPlaying) trailParticles.Play();
            if (!moving && trailParticles.isPlaying) trailParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            trailParticles.transform.position = segments[0].position + Vector3.up * trailHeight;
        }

        private void AnimateApple()
        {
            float breathe = Mathf.Sin(Time.unscaledTime * appleBreathFrequency);
            // A fresh apple drops in with a little overshoot rather than blinking into place.
            float arrival = Mathf.Clamp01(appleAge / Mathf.Max(.01f, appleArrivalDuration));
            float pop = arrival >= 1 ? 1 : 1 - Mathf.Pow(1 - arrival, appleArrivalEasePower) * Mathf.Cos(arrival * appleArrivalOscillation) * appleArrivalSwell;
            apple.position = World(Game.Food) + Vector3.up * (cellWaves.HeightAt(World(Game.Food)) + appleHoverHeight + breathe * appleBobAmplitude + (1 - arrival) * appleDropHeight);
            apple.rotation = Quaternion.Euler(0, Time.unscaledTime * appleSpinSpeed, Mathf.Sin(Time.unscaledTime * appleRockFrequency) * appleRockAngle);
            apple.localScale = Vector3.one * (appleScale * (1 + breathe * appleBreathScale) * pop);
            appleMarker.gameObject.SetActive(apple.gameObject.activeSelf);
            appleMarker.position = World(Game.Food) + Vector3.up * (appleMarkerHeight + cellWaves.HeightAt(World(Game.Food)));
            appleMarker.localScale = Vector3.one * ((appleMarkerScale + breathe * appleMarkerPulse) * arrival);
        }

        /// <summary>Tracks the record in memory; the disk write waits for the run to end.</summary>
        private void UpdateBest()
        {
            if (Game.Score <= best) return;
            best = Game.Score;
            bestUnsaved = true;
        }

        private void SaveBest()
        {
            if (!bestUnsaved) return;
            bestUnsaved = false;
            PlayerPrefs.SetInt("GardenSnake.Best", best);
            PlayerPrefs.Save();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused) return;
            SaveBest();
            if (Game != null && Game.State == RunState.Playing) TogglePause();
        }

        private void OnApplicationQuit() => SaveBest();

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
