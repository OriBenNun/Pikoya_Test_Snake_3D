using System;
using System.Collections.Generic;
using System.Linq;
using GardenSnake.Gameplay;
using UnityEngine;

namespace GardenSnake.Presentation
{
    /// <summary>
    /// The snake's visual layer. It owns every moving piece of the animal - the body poses, the
    /// continuous skin, the face, the blink, the death - and rebuilds them from whatever the game
    /// loop currently says is true. It never decides a rule and never makes a sound.
    /// <para>
    /// Nothing here is a setting: the component is pure wiring, and everything the animal does is
    /// either its character - baked into the constants below - or one of the two tuning assets.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class SnakeManager : MonoBehaviour
    {
        /// <summary>Age given to a one-shot animation that is over, so it never replays on its own.</summary>
        private const float Finished = 99f;

        [Header("Models")] [SerializeField] private GameObject headPrefab;
        [SerializeField] private GameObject bodyPrefab;
        [Header("Tuning")] [SerializeField] private SnakeSkinSettings skinSettings;
        [SerializeField] private SnakeMouthSettings mouthSettings;
        [SerializeField] private SnakeMotionSettings motion;
        [Header("Scene")] [SerializeField] private GameLoopManager loop;
        [SerializeField] private AppleView appleView;
        [SerializeField] private GridCellWaves board;

        /// <summary>The snake has been re-posed for a fresh run; anything trailing it should reset.</summary>
        public event Action VisualsReset;

        private readonly List<Transform> segments = new();
        private readonly List<Vector3> previousPositions = new();
        private readonly List<float> visualDigestion = new();
        private readonly List<Vector3> digestionAnchors = new();
        private Transform tail;
        private float headScale, bodyScale, tailScale;
        private SnakeSkin skin;
        private SnakeMouth mouth;
        private Transform[] eyeParts;
        private Vector3[] eyePartRestScales;
        private bool finishingDigestion;
        private Vector3 finishingAnchor;
        private float slither;
        private float bank;
        private float blinkAge;
        private float nextBlink;
        private int grownIndex = -1;
        private float grownAge = Finished;
        private float deathAge;
        private int capturedCount;
        private int previousLength;

        /// <summary>Where the head is being drawn this frame.</summary>
        public Vector3 HeadPosition => segments.Count > 0 ? segments[0].position : transform.position;


        private void Start()
        {
            skinSettings = Tuning.Or(skinSettings);
            motion = Tuning.Or(motion);
            nextBlink = motion.firstBlinkDelay;
            headScale = skinSettings.HeadScale;
            bodyScale = skinSettings.BodyScale;
            tailScale = skinSettings.TailScale;
            segments.Add(Instantiate(headPrefab, transform).transform);
            mouth = segments[0].gameObject.AddComponent<SnakeMouth>();
            mouth.Initialize(appleView.Prefab, mouthSettings);
            CollectEyeParts();
            tail = new GameObject("Tail pose").transform;
            tail.SetParent(transform, false);
            var skinObject = new GameObject("Snake skin");
            skinObject.transform.SetParent(transform, false);
            skin = skinObject.AddComponent<SnakeSkin>();
            skin.Initialize(bodyPrefab, GameLoopManager.Columns * GameLoopManager.Rows, skinSettings);
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
            var delta = Time.unscaledDeltaTime;
            var paused = loop.State == RunState.Paused;
            var dying = loop.State == RunState.Lost;
            deathAge = dying ? deathAge + delta : 0;
            if (!paused) grownAge += delta;
            Blink(paused ? 0 : delta);
            bank = Mathf.MoveTowards(bank, 0, delta * motion.bankRecoverySpeed);
            if (loop.State == RunState.Playing) slither += Time.deltaTime / loop.CurrentStep;

            AnimateBody(dying);
            appleView.Animate();
            mouth.Animate(loop, appleView.Apple.position, paused ? 0 : delta);
        }

        private void AnimateBody(bool dying)
        {
            var stepBlend = loop.StepProgress;
            var count = loop.Body.Count;
            for (var i = 0; i < count; i++)
            {
                var part = i == count - 1 ? tail : segments[i];
                var pose = EvaluateSegment(i, count, stepBlend, dying);
                part.position = pose.Position + Vector3.up * board.HeightAt(pose.Position);
                part.localScale = SegmentBasis(i, count) * pose.Scale;
                AimSegment(i, part);
            }

            CollectDigestion();
            skin.Draw(segments, tail, count, new SnakeSkin.SegmentScales(headScale, bodyScale, tailScale),
                new SnakeSkin.Digestion(visualDigestion, digestionAnchors, stepBlend, dying ? 0 : 1,
                    finishingDigestion));
        }

        private float SegmentBasis(int index, int count) =>
            index == 0 ? headScale : index == count - 1 ? tailScale : bodyScale;

        private SegmentPose EvaluateSegment(int index, int count, float stepBlend, bool dying)
        {
            var target = loop.World(loop.Body[index]);
            var from = previousPositions[index];
            var position = Vector3.Lerp(from, target, stepBlend);
            var scale = Vector3.one;

            // A shallow sideways wave sells "alive" without ever leaving the cell.
            var along = target - from;
            if (along.sqrMagnitude > .01f && !dying)
            {
                var side = Vector3.Cross(Vector3.up, along.normalized);
                position += side * (Mathf.Sin((slither - index * motion.segmentPhase) * motion.frequency) *
                                    motion.amplitude);
            }

            if (index == grownIndex && grownAge < motion.duration)
            {
                var birth = grownAge / Mathf.Max(.01f, motion.duration);
                scale *= 1 + Mathf.Sin(birth * Mathf.PI) * motion.swell;
            }

            if (loop.State == RunState.Ready)
            {
                var breath = Mathf.Sin(Time.unscaledTime * motion.breathFrequency - index * motion.breathSegmentPhase) *
                             motion.breathAmplitude;
                scale += new Vector3(-breath, breath * motion.breathStretch, -breath);
                position += Vector3.up * (breath * motion.breathLift);
            }

            if (!dying) return new SegmentPose(position, scale);

            // The head shoves into whatever stopped it before the snake gives up.
            if (index == 0 && deathAge < motion.recoilDuration)
            {
                var recoil = Mathf.Sin(deathAge / Mathf.Max(.01f, motion.recoilDuration) * Mathf.PI) *
                             motion.recoilDistance;
                position += (loop.World(loop.Body[0]) - loop.World(loop.Body[1])).normalized * recoil;
            }

            // Each piece swells and pops out of existence, head first, so the board is clear by
            // the time the results card arrives.
            var stagger = Mathf.Min(motion.segmentDelay, motion.totalStagger / count);
            var pop = Mathf.Clamp01((deathAge - index * stagger) / Mathf.Max(.01f, motion.beat));
            var swell = Mathf.Sin(pop * Mathf.PI) * motion.deathSwell;
            var shrink = pop < motion.shrinkStart
                ? 1
                : 1 - (pop - motion.shrinkStart) / Mathf.Max(.001f, 1 - motion.shrinkStart);
            return new SegmentPose(position + Vector3.up * (pop * motion.lift),
                Vector3.one * ((1 + swell) * shrink * shrink));
        }

        /// <summary>The head follows the heading and leans into corners; every other part looks ahead.</summary>
        private void AimSegment(int index, Transform part)
        {
            if (index == 0)
            {
                var facing = Rotation(loop.Heading) * Quaternion.Euler(0, 0, bank * motion.bankAngle);
                part.rotation = Quaternion.Slerp(part.rotation, facing, Time.unscaledDeltaTime * motion.headTurnSpeed);
                return;
            }

            var toward = segments[index - 1].position - part.position;
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
            var pending = loop.Digestion;
            foreach (var progress in pending)
            {
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
            var capacity = GameLoopManager.Columns * GameLoopManager.Rows;
            segments.Capacity = capacity;
            previousPositions.Capacity = capacity;
            for (var i = segments.Count; i < capacity - 1; i++)
            {
                var part = new GameObject("Body pose " + i).transform;
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
            var body = loop.Body;
            for (var i = 0; i < previousLength; i++) previousPositions[i] = loop.World(body[i]);
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

            var live = loop.Body.Count - 1;
            for (var i = 0; i < segments.Count; i++)
            {
                var wanted = i < live;
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
            for (var i = 0; i < loop.Body.Count - 1; i++)
            {
                segments[i].position = loop.World(loop.Body[i]);
                segments[i].localScale = Vector3.one * (i == 0 ? headScale : bodyScale);
            }

            segments[0].rotation = Rotation(loop.Heading);
            tail.position = loop.World(loop.Body[^1]);
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
            eyeParts = segments[0].GetComponentsInChildren<Transform>().Where(part =>
                    part.name.StartsWith("Eye white") || part.name.StartsWith("Pupil") ||
                    part.name.StartsWith("Eye glint"))
                .ToArray();
            eyePartRestScales = new Vector3[eyeParts.Length];
            for (var i = 0; i < eyeParts.Length; i++) eyePartRestScales[i] = eyeParts[i].localScale;
        }

        private void Blink(float delta)
        {
            if (eyeParts.Length == 0) return;
            blinkAge += delta;
            if (blinkAge > nextBlink + motion.blinkDuration)
            {
                blinkAge = 0;
                nextBlink = UnityEngine.Random.Range(motion.minimumBlinkGap, motion.maximumBlinkGap);
            }

            var open = blinkAge < nextBlink
                ? 1
                : 1 - Mathf.Sin(Mathf.Clamp01((blinkAge - nextBlink) / Mathf.Max(.01f, motion.blinkDuration))
                                * Mathf.PI) * motion.blinkClosure;
            // Food gets wide-eyed attention; do not blink away the anticipation pose.
            open = Mathf.Lerp(open, 1, mouth.Openness);
            for (var i = 0; i < eyeParts.Length; i++)
            {
                var rest = eyePartRestScales[i];
                eyeParts[i].localScale = new Vector3(rest.x, rest.y, rest.z * open);
            }
        }

        private static Quaternion Rotation(Direction direction) => Quaternion.Euler(0, (int)direction * 90, 0);

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
    }
}