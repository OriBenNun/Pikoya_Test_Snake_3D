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

        private const string BestKey = "GardenSnake.Best";
        private const string HasPlayedKey = "GardenSnake.HasPlayed";
        private const string MutedKey = "GardenSnake.Muted";
        /// <summary>Age given to a one-shot animation that is over, so it never replays on its own.</summary>
        private const float Finished = 99f;
        /// <summary>Longest catch-up a single frame may simulate, so a browser stall cannot kill unseen.</summary>
        private const float MaximumCatchUp = .1f;
        private static readonly int BurstAlphaId = Shader.PropertyToID("_Alpha");

        private readonly List<Transform> segments = new();
        private readonly List<Vector3> previousPositions = new();
        private readonly List<float> visualDigestion = new();
        private readonly List<Vector3> digestionAnchors = new();
        private readonly RunRecord record = new();

        private Transform tail;
        private SnakeSkin skin;
        private SnakeMouth mouth;
        private Transform apple;
        private Material burstMaterial;
        private Transform[] eyeParts;
        private Vector3[] eyePartRestScales;

        private float elapsed;
        private float currentStep;
        private float slither;
        private float bank;
        private float appleAge;
        private float burstAge = Finished;
        private float blinkAge;
        private float nextBlink;
        private int grownIndex = -1;
        private float grownAge = Finished;
        private float deathAge;
        private float endTime;
        private bool finishingDigestion;
        private Vector3 finishingAnchor;
        private int capturedCount;

        private float lastAspect;
        private float fitSize;
        private float viewSize;
        private Vector3 cameraHome;

        private Vector2 pointerStart;
        private bool trackingSwipe;
        private int best;
        private bool bestUnsaved;
        private bool hasPlayed;
        private bool muted;

        public SnakeGame Game { get; private set; }
        public RunRecord Record => record;
        public int Best => best;
        public bool Muted => muted;
        public int RecordCelebrations { get; private set; }
        public int RecordWhispers { get; private set; }
        public float StepSeconds => Mathf.Max(fastestStepSeconds, initialStepSeconds - Game.Score * speedGainPerApple);
        public float Pace => Mathf.InverseLerp(initialStepSeconds, fastestStepSeconds, StepSeconds);
        /// <summary>The pace actually on screen, which lags <see cref="Pace"/> by the step in flight.</summary>
        private float VisiblePace => Mathf.InverseLerp(initialStepSeconds, fastestStepSeconds, currentStep);
        private bool RunEnded => Game.State is RunState.Lost or RunState.Won;

        private void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            Application.runInBackground = runInBackground;
            nextBlink = firstBlinkDelay;
            Game = new SnakeGame(boardWidth, boardHeight, System.Environment.TickCount,
                initialLength, turnBufferSize, firstAppleDistance);
            best = PlayerPrefs.GetInt(BestKey, 0);
            hasPlayed = PlayerPrefs.GetInt(HasPlayedKey, 0) == 1 || best > 0;
            muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
            ApplyMute();
            cameraHome = gameCamera.transform.localPosition;
            burstMaterial = burstRing.GetComponent<Renderer>().material;
            segments.Add(Instantiate(headPrefab, transform).transform);
            mouth = segments[0].gameObject.AddComponent<SnakeMouth>();
            mouth.Initialize(applePrefab, mouthSettings);
            CollectEyeParts();
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

        // Player actions

        public void PrimaryAction()
        {
            if (Game.State == RunState.Paused) { TogglePause(); return; }
            if (Game.State == RunState.Playing) return;
            if (RunEnded && Time.unscaledTime - endTime < restartDelay) return;
            record.Begin(best, hasPlayed);
            RecordCelebrations = RecordWhispers = 0;
            hasPlayed = true;
            PlayerPrefs.SetInt(HasPlayedKey, 1);
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

        public void Turn(int direction) => Turn((Direction)direction);

        public void Turn(Direction wish)
        {
            if (Game.State == RunState.Ready) PrimaryAction();
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
            PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
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

        // Loop

        private void Update()
        {
            ReadKeyboard();
            ReadPointer();
            if (Game.State == RunState.Playing)
            {
                elapsed += Mathf.Min(Time.deltaTime, MaximumCatchUp);
                if (elapsed >= currentStep) Advance();
            }
            AnimateVisuals();
        }

        private void Advance()
        {
            elapsed -= currentStep;
            CapturePrevious();
            Vector3 pickedAt = World(Game.Food);
            int lengthBefore = Game.Body.Count;
            Vector3 tailBefore = World(Game.Body[lengthBefore - 1]);
            StepResult result = Game.Step();
            finishingDigestion = Game.Body.Count > lengthBefore;
            finishingAnchor = tailBefore;
            currentStep = StepSeconds;
            EnsureSegments();
            if (finishingDigestion)
            {
                grownIndex = Game.Body.Count - 2;
                grownAge = 0;
            }
            if (result == StepResult.Ate) CelebrateApple(pickedAt);
            if (result is StepResult.Lost or StepResult.Won) EndRun(result);
            hud.Refresh();
        }

        private void CelebrateApple(Vector3 pickedAt)
        {
            mouth.Swallow(apple, currentStep);
            appleAge = 0;
            RecordBeat beat = record.Register(Game.Score);
            UpdateBest();
            apple.position = World(Game.Food);
            feel.Pickup(pickedAt, Game.Score);
            burstAge = 0;
            burstRing.position = pickedAt + Vector3.up * burstHeight;
            Play(pickupSound, pickupPitch + Game.Score % Mathf.Max(1, pickupPitchCycle) * pickupPitchIncrement, pickupVolume);
            bool extending = beat == RecordBeat.Extended;
            hud.ShowPickup(extending ? "+1 <size=55%>best</size>" : "+1", pickedAt, extending);
            switch (beat)
            {
                case RecordBeat.Broken:
                    RecordCelebrations++;
                    cellWaves.Play(bestWave, Game.Body[0], bestWaveIntensity);
                    feel.NewBest(pickedAt);
                    Play(bestSound, bestPitch, bestVolume);
                    hud.ShowBanner("NEW BEST");
                    break;
                case RecordBeat.Extended:
                    RecordWhispers++;
                    feel.RecordApple(pickedAt);
                    Play(bestSound, recordPitch, recordVolume);
                    hud.WhisperBest();
                    break;
                default:
                    if (Game.Score % Mathf.Max(1, milestoneAppleInterval) != 0) break;
                    cellWaves.Play(milestoneWave, Game.Body[0], milestoneWaveIntensity);
                    hud.ShowBanner(Game.Score + " APPLES");
                    break;
            }
        }

        private void EndRun(StepResult result)
        {
            endTime = Time.unscaledTime;
            deathAge = 0;
            SaveBest();
            if (result == StepResult.Won)
            {
                cellWaves.Play(victoryWave, Game.Body[0], victoryWaveIntensity);
                hud.ShowBanner("GARDEN COMPLETE");
                return;
            }
            Vector3 head = World(Game.Body[0]);
            Play(loseSound, losePitch, loseVolume);
            feel.Death(head);
            cellWaves.Play(deathWave, Game.Body[0], deathWaveIntensity);
            pickupParticles.transform.position = head + Vector3.up * deathParticleHeight;
            pickupParticles.Emit(deathParticleCount);
        }

        private void ReadKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) PrimaryAction();
            if (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame) TogglePause();
            if (keyboard.mKey.wasPressedThisFrame) ToggleMute();
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) Turn(Direction.Up);
            else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) Turn(Direction.Right);
            else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) Turn(Direction.Down);
            else if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) Turn(Direction.Left);
        }

        /// <summary>A swipe that clears the threshold turns, then re-arms from where it ended.</summary>
        private void ReadPointer()
        {
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
                    Turn(Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                        ? delta.x > 0 ? Direction.Right : Direction.Left
                        : delta.y > 0 ? Direction.Up : Direction.Down);
                    pointerStart = pointer.position.ReadValue();
                }
            }
            if (pointer.press.wasReleasedThisFrame) trackingSwipe = false;
        }

        // Presentation

        /// <summary>
        /// Every body segment a full board could ever need is built once, before the first run.
        /// Growing then costs one SetActive instead of an Instantiate, so eating an apple never
        /// stalls the frame it lands on.
        /// </summary>
        private void Prewarm()
        {
            int capacity = boardWidth * boardHeight;
            segments.Capacity = capacity;
            previousPositions.Capacity = capacity;
            for (int i = segments.Count; i < capacity - 1; i++)
            {
                Transform part = new GameObject("Body pose " + i).transform;
                part.SetParent(transform, false);
                part.gameObject.SetActive(false);
                segments.Add(part);
            }
            while (previousPositions.Count < capacity) previousPositions.Add(Vector3.zero);
        }

        private void EnsureSegments()
        {
            // A new segment starts where the one in front of it was, not at the world origin.
            while (capturedCount < Game.Body.Count)
            {
                previousPositions[capturedCount] = previousPositions[Mathf.Max(0, capturedCount - 1)];
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
            burstAge = Finished;
            grownIndex = -1;
            grownAge = Finished;
            finishingDigestion = false;
            visualDigestion.Clear();
            digestionAnchors.Clear();
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
        private void CollectEyeParts()
        {
            var found = new List<Transform>();
            foreach (Transform part in segments[0].GetComponentsInChildren<Transform>())
                if (part.name.StartsWith("Eye white") || part.name.StartsWith("Pupil") ||
                    part.name.StartsWith("Eye glint"))
                    found.Add(part);
            eyeParts = found.ToArray();
            eyePartRestScales = new Vector3[eyeParts.Length];
            for (int i = 0; i < eyeParts.Length; i++) eyePartRestScales[i] = eyeParts[i].localScale;
        }

        private void Blink(float delta)
        {
            if (eyeParts.Length == 0) return;
            blinkAge += delta;
            if (blinkAge > nextBlink + blinkDuration)
            {
                blinkAge = 0;
                nextBlink = Random.Range(blinkInterval.x, blinkInterval.y);
            }
            float open = blinkAge < nextBlink
                ? 1
                : 1 - Mathf.Sin(Mathf.Clamp01((blinkAge - nextBlink) / Mathf.Max(.01f, blinkDuration)) * Mathf.PI) * blinkClosure;
            // Food gets wide-eyed attention; do not blink away the anticipation pose.
            open = Mathf.Lerp(open, 1, mouth.Openness);
            for (int i = 0; i < eyeParts.Length; i++)
            {
                Vector3 rest = eyePartRestScales[i];
                eyeParts[i].localScale = new Vector3(rest.x, rest.y, rest.z * open);
            }
        }

        private void CapturePrevious()
        {
            // Indexed, not foreach: enumerating the read-only body through its interface boxes.
            for (int i = 0; i < Game.Body.Count; i++) previousPositions[i] = World(Game.Body[i]);
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
            bool paused = Game.State == RunState.Paused;
            bool moving = Game.State == RunState.Playing;
            bool dying = Game.State == RunState.Lost;
            deathAge = dying ? deathAge + delta : 0;
            if (!paused) grownAge += delta;
            Blink(paused ? 0 : delta);
            bank = Mathf.MoveTowards(bank, 0, delta * bankRecoverySpeed);
            appleAge += delta;
            if (moving) slither += Time.deltaTime / currentStep;

            AnimateSnake(dying);
            AnimateTrail(moving);
            AnimateApple();
            mouth.Animate(Game, apple.position, paused ? 0 : delta);
            AnimateBurst(delta);
            float wanted = moving ? VisiblePace : 0;
            paceVolume.weight = Mathf.MoveTowards(paceVolume.weight, wanted, delta * paceVolumeBlendSpeed);
        }

        private void AnimateSnake(bool dying)
        {
            float stepBlend = Game.State is RunState.Playing or RunState.Paused
                ? Mathf.Clamp01(elapsed / currentStep)
                : 1;
            int last = Game.Body.Count - 1;
            for (int i = 0; i <= last; i++)
            {
                Transform part = i == last ? tail : segments[i];
                SegmentPose pose = EvaluateSegment(i, stepBlend, dying);
                part.position = pose.Position + Vector3.up * cellWaves.HeightAt(pose.Position);
                part.localScale = SegmentBasis(i) * pose.Scale;
                AimSegment(i, part);
            }
            UpdateDigestion();
            skin.Draw(segments, tail, Game.Body.Count, new SnakeSkin.SegmentScales(headScale, bodyScale, tailScale),
                new SnakeSkin.Digestion(visualDigestion, digestionAnchors, stepBlend, dying ? 0 : 1, finishingDigestion));
        }

        private readonly struct SegmentPose
        {
            public readonly Vector3 Position;
            public readonly Vector3 Scale;
            public SegmentPose(Vector3 position, Vector3 scale) { Position = position; Scale = scale; }
        }

        private float SegmentBasis(int index) =>
            index == 0 ? headScale : index == Game.Body.Count - 1 ? tailScale : bodyScale;

        private SegmentPose EvaluateSegment(int index, float stepBlend, bool dying)
        {
            Vector3 target = World(Game.Body[index]);
            Vector3 from = previousPositions[index];
            Vector3 position = Vector3.Lerp(from, target, stepBlend);
            Vector3 scale = Vector3.one;

            // A shallow sideways wave sells "alive" without ever leaving the cell.
            Vector3 along = target - from;
            if (along.sqrMagnitude > .01f && !dying)
            {
                Vector3 side = Vector3.Cross(Vector3.up, along.normalized);
                position += side * (Mathf.Sin((slither - index * slitherSegmentPhase) * slitherFrequency) * slitherAmplitude);
            }
            if (index == grownIndex && grownAge < growthDuration)
            {
                float birth = grownAge / Mathf.Max(.01f, growthDuration);
                scale *= 1 + Mathf.Sin(birth * Mathf.PI) * growthSwell;
            }
            if (Game.State == RunState.Ready)
            {
                float breath = Mathf.Sin(Time.unscaledTime * idleBreathFrequency - index * idleBreathSegmentPhase) * idleBreathAmplitude;
                scale += new Vector3(-breath, breath * idleBreathStretch, -breath);
                position += Vector3.up * (breath * idleBreathLift);
            }
            if (!dying) return new SegmentPose(position, scale);

            // The head shoves into whatever stopped it before the snake gives up.
            if (index == 0 && deathAge < deathRecoilDuration)
            {
                float recoil = Mathf.Sin(deathAge / Mathf.Max(.01f, deathRecoilDuration) * Mathf.PI) * deathRecoilDistance;
                position += (World(Game.Body[0]) - World(Game.Body[1])).normalized * recoil;
            }
            // Each piece swells and pops out of existence, head first, so the board is clear by
            // the time the results card arrives.
            float stagger = Mathf.Min(deathSegmentDelay, deathTotalStagger / Mathf.Max(1, Game.Body.Count));
            float pop = Mathf.Clamp01((deathAge - index * stagger) / Mathf.Max(.01f, deathBeat));
            float swell = Mathf.Sin(pop * Mathf.PI) * deathSwell;
            float shrink = pop < deathShrinkStart ? 1 : 1 - (pop - deathShrinkStart) / Mathf.Max(.01f, 1 - deathShrinkStart);
            return new SegmentPose(position + Vector3.up * (pop * deathLift),
                Vector3.one * ((1 + swell) * shrink * shrink));
        }

        private void AimSegment(int index, Transform part)
        {
            if (index == 0)
            {
                Quaternion facing = Rotation(Game.Heading) * Quaternion.Euler(0, 0, bank * bankAngle);
                part.rotation = Quaternion.Slerp(part.rotation, facing, Time.unscaledDeltaTime * headTurnSpeed);
                return;
            }
            Vector3 toward = segments[index - 1].position - part.position;
            if (toward.sqrMagnitude > .01f) part.rotation = Quaternion.LookRotation(toward);
        }

        private void UpdateDigestion()
        {
            visualDigestion.Clear();
            digestionAnchors.Clear();
            foreach (float appleProgress in Game.Digestion)
            {
                visualDigestion.Add(appleProgress);
                digestionAnchors.Add(World(Game.Body[Mathf.Clamp((int)appleProgress, 0, Game.Body.Count - 1)]));
            }
            // The simulation grows at the start of a step. Let the last lump settle into
            // the tail over that step instead of disappearing between two rendered frames.
            if (!finishingDigestion) return;
            visualDigestion.Add(Game.Body.Count - 1);
            digestionAnchors.Add(finishingAnchor);
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
            burstMaterial.SetFloat(BurstAlphaId, (1 - t) * (1 - t) * burstOpacity);
        }

        private void AnimateTrail(bool moving)
        {
            var emission = trailParticles.emission;
            // The faster the run gets, the more the snake leaves behind it.
            emission.rateOverTime = Mathf.Lerp(trailEmission.x, trailEmission.y, VisiblePace);
            if (moving && !trailParticles.isPlaying) trailParticles.Play();
            if (!moving && trailParticles.isPlaying) trailParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            trailParticles.transform.position = segments[0].position + Vector3.up * trailHeight;
        }

        private void AnimateApple()
        {
            Vector3 cell = World(Game.Food);
            float ground = cellWaves.HeightAt(cell);
            float breathe = Mathf.Sin(Time.unscaledTime * appleBreathFrequency);
            // A fresh apple drops in with a little overshoot rather than blinking into place.
            float arrival = Mathf.Clamp01(appleAge / Mathf.Max(.01f, appleArrivalDuration));
            float pop = arrival >= 1
                ? 1
                : 1 - Mathf.Pow(1 - arrival, appleArrivalEasePower) * Mathf.Cos(arrival * appleArrivalOscillation) * appleArrivalSwell;
            apple.position = cell + Vector3.up *
                (ground + appleHoverHeight + breathe * appleBobAmplitude + (1 - arrival) * appleDropHeight);
            apple.rotation = Quaternion.Euler(0, Time.unscaledTime * appleSpinSpeed,
                Mathf.Sin(Time.unscaledTime * appleRockFrequency) * appleRockAngle);
            apple.localScale = Vector3.one * (appleScale * (1 + breathe * appleBreathScale) * pop);
            appleMarker.gameObject.SetActive(apple.gameObject.activeSelf);
            appleMarker.position = cell + Vector3.up * (appleMarkerHeight + ground);
            appleMarker.localScale = Vector3.one * ((appleMarkerScale + breathe * appleMarkerPulse) * arrival);
        }

        // Persistence

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
            PlayerPrefs.SetInt(BestKey, best);
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
            new(cell.X - (boardWidth - 1) * .5f, 0, cell.Y - (boardHeight - 1) * .5f);

        private static Quaternion Rotation(Direction direction) => Quaternion.Euler(0, (int)direction * 90, 0);

        /// <summary>-1 for a left turn, 1 for a right turn, 0 when the heading does not change.</summary>
        private static int TurnSign(Direction from, Direction to)
        {
            int difference = ((int)to - (int)from + 4) % 4;
            return difference == 1 ? 1 : difference == 3 ? -1 : 0;
        }
    }
}
