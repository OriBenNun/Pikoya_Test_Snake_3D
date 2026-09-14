using System;
using System.Collections.Generic;
using UnityEngine;

namespace GardenSnake
{
    /// <summary>
    /// The snake's visual layer. It owns every moving piece of the animal - the body poses, the
    /// continuous skin, the face, the blink, the death - and rebuilds them from whatever the game
    /// loop currently says is true. It never decides a rule and never makes a sound.
    /// <para>
    /// Only the pieces an art director would actually reach for are exposed: the three models,
    /// the two shared tuning assets, and the proportions of head, body and tail. Everything else
    /// is the animal's character rather than a setting, so it is baked into this class.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class SnakeManager : MonoBehaviour
    {
        [Header("Models")]
        [SerializeField] private GameObject headPrefab;
        [SerializeField] private GameObject bodyPrefab;
        [Header("Shared tuning")]
        [SerializeField] private SnakeSkinSettings skinSettings;
        [SerializeField] private SnakeMouthSettings mouthSettings;
        [Header("Proportions")]
        [SerializeField, Range(.8f, 1.6f)] private float headScale = 1.22f;
        [SerializeField, Range(.8f, 1.6f)] private float bodyScale = 1.3f;
        [SerializeField, Range(.8f, 1.6f)] private float tailScale = 1.24f;
        [Header("Scene")]
        [SerializeField] private GameLoopManager loop;
        [SerializeField] private AppleView appleView;
        [SerializeField] private GridCellWaves board;

        // The animal's character, not settings.
        // Banking into a corner.
        private const float BankRecoverySpeed = 3.4f;
        private const float BankAngle = -16f;
        private const float HeadTurnSpeed = 22f;
        // The shallow sideways wave that sells "alive" without ever leaving the cell.
        private const float SlitherAmplitude = .055f;
        private const float SlitherFrequency = 2.1f;
        private const float SlitherSegmentPhase = .42f;
        // A new segment swells as it appears.
        private const float GrowthDuration = .2f;
        private const float GrowthSwell = .2f;
        // Breathing while the board is still waiting for the player.
        private const float IdleBreathFrequency = 2.4f;
        private const float IdleBreathSegmentPhase = .5f;
        private const float IdleBreathAmplitude = .035f;
        private const float IdleBreathStretch = 2.2f;
        private const float IdleBreathLift = .5f;
        // Blinking.
        private const float FirstBlinkDelay = 2.5f;
        private const float MinimumBlinkGap = 2.2f;
        private const float MaximumBlinkGap = 5.5f;
        private const float BlinkDuration = .16f;
        private const float BlinkClosure = .92f;
        // The death: a shove into whatever stopped it, then head-first out of existence.
        private const float DeathBeat = .26f;
        private const float DeathRecoilDuration = .1f;
        private const float DeathRecoilDistance = .18f;
        private const float DeathSegmentDelay = .05f;
        private const float DeathTotalStagger = .34f;
        private const float DeathSwell = .35f;
        private const float DeathShrinkStart = .55f;
        private const float DeathLift = .22f;
        /// <summary>Age given to a one-shot animation that is over, so it never replays on its own.</summary>
        private const float Finished = 99f;

        /// <summary>The snake has been re-posed for a fresh run; anything trailing it should reset.</summary>
        public event Action VisualsReset;

        private readonly List<Transform> segments = new();
        private readonly List<Vector3> previousPositions = new();
        private readonly List<float> visualDigestion = new();
        private readonly List<Vector3> digestionAnchors = new();
        private Transform tail;
        private SnakeSkin skin;
        private SnakeMouth mouth;
        private Transform[] eyeParts;
        private Vector3[] eyePartRestScales;
        private bool finishingDigestion;
        private Vector3 finishingAnchor;
        private float slither;
        private float bank;
        private float blinkAge;
        private float nextBlink = FirstBlinkDelay;
        private int grownIndex = -1;
        private float grownAge = Finished;
        private float deathAge;
        private int capturedCount;
        private int previousLength;

        /// <summary>Where the head is being drawn this frame.</summary>
        public Vector3 HeadPosition => segments.Count > 0 ? segments[0].position : transform.position;
        public float MouthOpenness => mouth != null ? mouth.Openness : 0;

        private void Start()
        {
            segments.Add(Instantiate(headPrefab, transform).transform);
            mouth = segments[0].gameObject.AddComponent<SnakeMouth>();
            mouth.Initialize(appleView.Prefab, mouthSettings);
            CollectEyeParts();
            tail = new GameObject("Tail pose").transform;
            tail.SetParent(transform, false);
            var skinObject = new GameObject("Snake skin");
            skinObject.transform.SetParent(transform, false);
            skin = skinObject.AddComponent<SnakeSkin>();
            skin.Initialize(bodyPrefab, loop.BoardWidth * loop.BoardHeight, skinSettings);
            Prewarm();
            ResetVisuals();

            loop.Stepping += CapturePrevious;
            loop.Stepped += OnStepped;
            loop.AppleEaten += OnAppleEaten;
            loop.RunStarted += OnRunStarted;
            loop.RunEnded += OnRunEnded;
            loop.TurnAccepted += OnTurnAccepted;
        }

        private void OnDestroy()
        {
            if (loop == null) return;
            loop.Stepping -= CapturePrevious;
            loop.Stepped -= OnStepped;
            loop.AppleEaten -= OnAppleEaten;
            loop.RunStarted -= OnRunStarted;
            loop.RunEnded -= OnRunEnded;
            loop.TurnAccepted -= OnTurnAccepted;
        }

        // Reacting to the loop

        private void OnRunStarted(Vector3 head) => ResetVisuals();

        private void OnStepped(StepResult result)
        {
            // The simulation grows at the start of a step. Let the last lump settle into the
            // tail over that step instead of disappearing between two rendered frames.
            finishingDigestion = loop.Body.Count > previousLength;
            EnsureSegments();
            if (!finishingDigestion) return;
            grownIndex = loop.Body.Count - 2;
            grownAge = 0;
        }

        private void OnAppleEaten(AppleBeat apple)
        {
            // The mouth takes hold of the apple where it was eaten, before it is respawned.
            mouth.Swallow(appleView.Apple, loop.CurrentStep);
            appleView.Respawn();
        }

        /// <summary>The collision has just landed; start the wilt from this frame.</summary>
        private void OnRunEnded(StepResult result) => deathAge = 0;

        /// <summary>A turn the simulation took: lean into it, then come back up.</summary>
        private void OnTurnAccepted(int turnSign) => bank = turnSign;

        // The frame

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;
            bool paused = loop.State == RunState.Paused;
            bool dying = loop.State == RunState.Lost;
            deathAge = dying ? deathAge + delta : 0;
            if (!paused) grownAge += delta;
            Blink(paused ? 0 : delta);
            bank = Mathf.MoveTowards(bank, 0, delta * BankRecoverySpeed);
            if (loop.State == RunState.Playing) slither += Time.deltaTime / loop.CurrentStep;

            AnimateBody(dying);
            appleView.Animate();
            mouth.Animate(loop, appleView.Apple.position, paused ? 0 : delta);
        }

        private void AnimateBody(bool dying)
        {
            float stepBlend = loop.StepProgress;
            int count = loop.Body.Count;
            for (int i = 0; i < count; i++)
            {
                Transform part = i == count - 1 ? tail : segments[i];
                SegmentPose pose = EvaluateSegment(i, count, stepBlend, dying);
                part.position = pose.Position + Vector3.up * board.HeightAt(pose.Position);
                part.localScale = SegmentBasis(i, count) * pose.Scale;
                AimSegment(i, part);
            }
            CollectDigestion();
            skin.Draw(segments, tail, count, new SnakeSkin.SegmentScales(headScale, bodyScale, tailScale),
                new SnakeSkin.Digestion(visualDigestion, digestionAnchors, stepBlend, dying ? 0 : 1, finishingDigestion));
        }

        /// <summary>Where one segment sits and how big it is drawn, before the board's wave lifts it.</summary>
        private readonly struct SegmentPose
        {
            public readonly Vector3 Position;
            public readonly Vector3 Scale;

            public SegmentPose(Vector3 position, Vector3 scale)
            {
                Position = position;
                Scale = scale;
            }
        }

        private float SegmentBasis(int index, int count) =>
            index == 0 ? headScale : index == count - 1 ? tailScale : bodyScale;

        private SegmentPose EvaluateSegment(int index, int count, float stepBlend, bool dying)
        {
            Vector3 target = loop.World(loop.Body[index]);
            Vector3 from = previousPositions[index];
            Vector3 position = Vector3.Lerp(from, target, stepBlend);
            Vector3 scale = Vector3.one;

            // A shallow sideways wave sells "alive" without ever leaving the cell.
            Vector3 along = target - from;
            if (along.sqrMagnitude > .01f && !dying)
            {
                Vector3 side = Vector3.Cross(Vector3.up, along.normalized);
                position += side * (Mathf.Sin((slither - index * SlitherSegmentPhase) * SlitherFrequency) * SlitherAmplitude);
            }
            if (index == grownIndex && grownAge < GrowthDuration)
            {
                float birth = grownAge / GrowthDuration;
                scale *= 1 + Mathf.Sin(birth * Mathf.PI) * GrowthSwell;
            }
            if (loop.State == RunState.Ready)
            {
                float breath = Mathf.Sin(Time.unscaledTime * IdleBreathFrequency - index * IdleBreathSegmentPhase) * IdleBreathAmplitude;
                scale += new Vector3(-breath, breath * IdleBreathStretch, -breath);
                position += Vector3.up * (breath * IdleBreathLift);
            }
            if (!dying) return new SegmentPose(position, scale);

            // The head shoves into whatever stopped it before the snake gives up.
            if (index == 0 && deathAge < DeathRecoilDuration)
            {
                float recoil = Mathf.Sin(deathAge / DeathRecoilDuration * Mathf.PI) * DeathRecoilDistance;
                position += (loop.World(loop.Body[0]) - loop.World(loop.Body[1])).normalized * recoil;
            }
            // Each piece swells and pops out of existence, head first, so the board is clear by
            // the time the results card arrives.
            float stagger = Mathf.Min(DeathSegmentDelay, DeathTotalStagger / count);
            float pop = Mathf.Clamp01((deathAge - index * stagger) / DeathBeat);
            float swell = Mathf.Sin(pop * Mathf.PI) * DeathSwell;
            float shrink = pop < DeathShrinkStart ? 1 : 1 - (pop - DeathShrinkStart) / (1 - DeathShrinkStart);
            return new SegmentPose(position + Vector3.up * (pop * DeathLift),
                Vector3.one * ((1 + swell) * shrink * shrink));
        }

        /// <summary>The head follows the heading and leans into corners; every other part looks ahead.</summary>
        private void AimSegment(int index, Transform part)
        {
            if (index == 0)
            {
                Quaternion facing = Rotation(loop.Heading) * Quaternion.Euler(0, 0, bank * BankAngle);
                part.rotation = Quaternion.Slerp(part.rotation, facing, Time.unscaledDeltaTime * HeadTurnSpeed);
                return;
            }
            Vector3 toward = segments[index - 1].position - part.position;
            if (toward.sqrMagnitude > .01f) part.rotation = Quaternion.LookRotation(toward);
        }

        /// <summary>
        /// Swallowed apples stay pinned to the cell they were picked up on. Indexed, not foreach:
        /// enumerating the simulation's read-only list through its interface allocates every frame.
        /// </summary>
        private void CollectDigestion()
        {
            visualDigestion.Clear();
            digestionAnchors.Clear();
            IReadOnlyList<float> pending = loop.Digestion;
            for (int i = 0; i < pending.Count; i++)
            {
                float progress = pending[i];
                visualDigestion.Add(progress);
                digestionAnchors.Add(loop.World(loop.Body[Mathf.Clamp((int)progress, 0, loop.Body.Count - 1)]));
            }
            if (!finishingDigestion) return;
            visualDigestion.Add(loop.Body.Count - 1);
            digestionAnchors.Add(finishingAnchor);
        }

        // Poses

        /// <summary>
        /// Every body segment a full board could ever need is built once, before the first run.
        /// Growing then costs one SetActive instead of an Instantiate, so eating an apple never
        /// stalls the frame it lands on.
        /// </summary>
        private void Prewarm()
        {
            int capacity = loop.BoardWidth * loop.BoardHeight;
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

        private void CapturePrevious()
        {
            previousLength = loop.Body.Count;
            finishingAnchor = loop.World(loop.Body[previousLength - 1]);
            IReadOnlyList<Cell> body = loop.Body;
            for (int i = 0; i < previousLength; i++) previousPositions[i] = loop.World(body[i]);
            capturedCount = previousLength;
        }

        private void EnsureSegments()
        {
            // A new segment starts where the one in front of it was, not at the world origin.
            while (capturedCount < loop.Body.Count)
            {
                previousPositions[capturedCount] = previousPositions[Mathf.Max(0, capturedCount - 1)];
                capturedCount++;
            }
            int live = loop.Body.Count - 1;
            for (int i = 0; i < segments.Count; i++)
            {
                bool wanted = i < live;
                if (segments[i].gameObject.activeSelf != wanted) segments[i].gameObject.SetActive(wanted);
            }
            appleView.Show(loop.HasFood && loop.State != RunState.Won);
        }

        private void ResetVisuals()
        {
            CapturePrevious();
            finishingDigestion = false;
            EnsureSegments();
            bank = 0;
            deathAge = 0;
            grownIndex = -1;
            grownAge = Finished;
            visualDigestion.Clear();
            digestionAnchors.Clear();
            mouth.ResetPose();
            for (int i = 0; i < loop.Body.Count - 1; i++)
            {
                segments[i].position = loop.World(loop.Body[i]);
                segments[i].localScale = Vector3.one * (i == 0 ? headScale : bodyScale);
            }
            segments[0].rotation = Rotation(loop.Heading);
            tail.position = loop.World(loop.Body[loop.Body.Count - 1]);
            tail.localScale = Vector3.one * tailScale;
            appleView.Settle();
            VisualsReset?.Invoke();
        }

        // The face

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
            if (blinkAge > nextBlink + BlinkDuration)
            {
                blinkAge = 0;
                nextBlink = UnityEngine.Random.Range(MinimumBlinkGap, MaximumBlinkGap);
            }
            float open = blinkAge < nextBlink
                ? 1
                : 1 - Mathf.Sin(Mathf.Clamp01((blinkAge - nextBlink) / BlinkDuration) * Mathf.PI) * BlinkClosure;
            // Food gets wide-eyed attention; do not blink away the anticipation pose.
            open = Mathf.Lerp(open, 1, mouth.Openness);
            for (int i = 0; i < eyeParts.Length; i++)
            {
                Vector3 rest = eyePartRestScales[i];
                eyeParts[i].localScale = new Vector3(rest.x, rest.y, rest.z * open);
            }
        }

        private static Quaternion Rotation(Direction direction) => Quaternion.Euler(0, (int)direction * 90, 0);
    }
}
