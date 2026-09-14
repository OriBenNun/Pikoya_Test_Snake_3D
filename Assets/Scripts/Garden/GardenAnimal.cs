using GardenSnake.Gameplay;
using UnityEngine;

namespace GardenSnake.Garden
{
    /// <summary>
    /// Decorative animation only. Routes stay in authored garden patches, outside the snake board.
    /// <para>
    /// This base holds what every animal does the same way: choose a spot in its patch, travel
    /// there, settle, breathe, glance about, and startle when the garden is struck. A species
    /// subclass supplies its gait and whatever it alone does — a bunny's hop, a turtle's shell,
    /// a bird's perch — by overriding the hooks below rather than by testing a species field.
    /// </para>
    /// </summary>
    public abstract class GardenAnimal : GardenDweller
    {
        public enum Activity { Rest, Fly, Hop, Graze, Crawl, Hide }
        [Header("Rig")]
        [SerializeField] private Transform body, head;
        [SerializeField] private Transform[] limbs = System.Array.Empty<Transform>();
        [Header("Route")]
        [SerializeField, Tooltip("One end of the patch this animal keeps to.")] private Vector3 groundA;
        [SerializeField, Tooltip("And the other end.")] private Vector3 groundB;
        [SerializeField, Tooltip("This animal's own offset, so no two of a species move alike.")]
        private float phase;
        [Header("Tuning")]
        [SerializeField] private AnimalMotionSettings motion;
        [SerializeField, Tooltip("The asset for this animal's own kind.")]
        private AnimalSpeciesSettings species;

        public Activity State { get; private set; }
        public int FlightCount { get; private set; }
        public int RestCount { get; private set; }
        /// <summary>Where this frame sits in whatever the animal is currently doing.</summary>
        protected readonly struct FramePhase
        {
            /// <summary>0 to 1 through the current activity.</summary>
            public readonly float T;
            /// <summary>The animal's own tempo-scaled clock, offset so no two move in step.</summary>
            public readonly float Clock;
            public readonly bool Moving;

            public FramePhase(float t, float clock, bool moving)
            {
                T = t;
                Clock = clock;
                Moving = moving;
            }
        }

        private Vector3 bodyScale;
        private Vector3 headScale;
        private Vector3 headPosition;
        private Quaternion headRotation;
        private Quaternion[] limbRest;
        private Vector3[] limbPositions;
        private Vector3 bodyPosition;
        private Quaternion bodyRotation;
        private float elapsed;
        private float duration;
        private float excitement;
        private float pendingAt = -1;
        private float pendingStrength;
        private float curiosity;
        private float tempo;
        private System.Random random;
        private Beat pendingBeat;
        private bool destinationB;

        /// <summary>The animal is going somewhere, however it happens to get there.</summary>
        private bool Moving => State is Activity.Fly or Activity.Hop or Activity.Crawl;

        /// <summary>Where the current trip started.</summary>
        protected Vector3 From { get; private set; }

        /// <summary>Where it is headed. A species whose destination can move rewrites this in flight.</summary>
        protected Vector3 Destination { get; set; }

        /// <summary>How stirred up the animal is after a recent beat, decaying back to zero.</summary>
        protected float Excitement => excitement;

        /// <summary>This animal's authored offset, so no two of a species move alike.</summary>
        protected float Phase => phase;

        /// <summary>What every animal shares.</summary>
        protected AnimalMotionSettings Motion => motion;

        /// <summary>This animal's own kind. A species reads its own asset through this.</summary>
        protected AnimalSpeciesSettings Species => species;

        /// <summary>The asset this species falls back to when its slot is empty.</summary>
        protected abstract AnimalSpeciesSettings DefaultSpecies();

        // Species hooks. Everything a single kind of animal does is reached through one of these.

        /// <summary>What this species is doing while it travels.</summary>
        protected abstract Activity TravelActivity { get; }

        /// <summary>What it does between trips.</summary>
        protected virtual Activity RestActivity => Activity.Rest;

        /// <summary>Whether it sets off as soon as the garden loads instead of resting first.</summary>
        protected virtual bool StartsTravelling => false;

        /// <summary>Cache any species-specific rest pose. Runs before the first activity is chosen.</summary>
        protected virtual void OnAwake() { }

        /// <summary>Picks a point along the patch; a species with somewhere better to be overrides it.</summary>
        protected virtual Vector3 ChooseDestination(bool towardsB) =>
            Vector3.Lerp(groundA, groundB, towardsB
                ? Range(motion.destinationBRange.x, motion.destinationBRange.y)
                : Range(motion.destinationARange.x, motion.destinationARange.y));

        /// <summary>Reshapes the trip length once the destination is known; a hopper counts hops.</summary>
        protected virtual float TravelDuration(float seconds) => seconds;

        /// <summary>Where the animal stands once the trip is over.</summary>
        protected virtual Vector3 ArrivalPosition => Destination;

        /// <summary>A beat arrived. Return true to keep this response instead of the usual bolt.</summary>
        protected virtual bool Startle(Beat beat, float strength) => false;

        /// <summary>Runs before anything is posed, for a phase a species works out for itself.</summary>
        protected virtual void PrepareFrame(in FramePhase frame) { }

        /// <summary>Each frame at rest, for an animal whose resting place can move under it.</summary>
        protected virtual void Settle() { }

        /// <summary>Each frame in transit, in case the destination has moved since it set off.</summary>
        protected virtual void TrackDestination() { }

        /// <summary>0 to 1 along the route; a gait that lands between steps reshapes it.</summary>
        protected virtual float RouteProgress(in FramePhase frame)
        {
            float t = frame.T;
            return t * t * (3 - 2 * t);
        }

        /// <summary>Lift and wander away from the straight line, given the trip's 0-to-1-to-0 arc.</summary>
        protected virtual Vector3 RouteOffset(in FramePhase frame, float arc) => Vector3.zero;

        /// <summary>Swelling of the body: breathing, unless a gait squashes and stretches it.</summary>
        protected virtual float Bounce(in FramePhase frame, float breath) => breath;

        protected virtual float BodyPitch(in FramePhase frame) => 0;

        protected virtual float BodyRoll(in FramePhase frame) => 0;

        /// <summary>How far the head is drawn in, 1 being fully out.</summary>
        protected virtual float HeadTuck => 1f;

        protected virtual Vector3 HeadTuckOffset => Vector3.zero;

        /// <summary>Head pitch from feeding or idling.</summary>
        protected virtual float HeadNod(in FramePhase frame) =>
            Mathf.Sin(frame.Clock * motion.idleNodFrequency) * motion.idleNodAmplitude;

        /// <summary>The gait's pose for one limb: rotation about its rest, and how far it lifts.</summary>
        protected virtual void PoseLimb(int index, in FramePhase frame, out float angle, out float sweep, out float lift)
        {
            angle = 0;
            sweep = 0;
            lift = 0;
        }

        /// <summary>Last chance to move a limb's rest position, such as tucking it under a shell.</summary>
        protected virtual void AdjustLimbPosition(ref Vector3 local) { }

        /// <summary>Ears, wings, and anything else only one species carries.</summary>
        protected virtual void AnimateAppendages(in FramePhase frame, float nod, float delta) { }

        // Shared behaviour.

        private void Awake()
        {
            motion = Tuning.Or(motion);
            species = species != null ? species : DefaultSpecies();
            random = new System.Random(motion.randomSeed + Mathf.RoundToInt(phase * motion.phaseSeedMultiplier));
            tempo = Range(motion.tempoRange.x, motion.tempoRange.y);
            bodyScale = body.localScale;
            bodyPosition = body.localPosition;
            bodyRotation = body.localRotation;
            headScale = head.localScale;
            headPosition = head.localPosition;
            headRotation = head.localRotation;
            limbRest = new Quaternion[limbs.Length];
            limbPositions = new Vector3[limbs.Length];
            for (int i = 0; i < limbs.Length; i++)
            {
                limbRest[i] = limbs[i].localRotation;
                limbPositions[i] = limbs[i].localPosition;
            }
            OnAwake();
            State = Activity.Rest;
            duration = Mathf.Max(.001f, Range(motion.initialRestMinimum, species.restSeconds * motion.initialRestMaximumMultiplier));
            From = Destination = transform.position;
            if (StartsTravelling) BeginTravel();
            else RestCount++;
        }

        protected override void React(Beat beat, Vector3 at, float strength)
        {
            pendingBeat = beat;
            float distance = Vector3.Distance(transform.position, at);
            pendingAt = Time.time + TravelTime(distance, motion.propagationSpeed) +
                Range(motion.delayRange.x, motion.delayRange.y);
            pendingStrength = Carried(strength, distance, motion.distanceAttenuation);
        }

        protected float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

        /// <summary>Drops whatever was under way and holds a new activity for a fixed spell.</summary>
        protected void EnterActivity(Activity activity, float seconds)
        {
            State = activity;
            elapsed = 0;
            duration = Mathf.Max(.001f, seconds);
        }

        private void BeginTravel()
        {
            From = transform.position;
            destinationB = !destinationB;
            Destination = ChooseDestination(destinationB);
            elapsed = 0;
            // An excited animal covers the same ground in less time.
            duration = species.travelSeconds * Range(motion.travelDurationRange.x, motion.travelDurationRange.y)
                / Mathf.Max(.001f, 1 + excitement * motion.travelExcitementSpeed);
            duration = Mathf.Max(.001f, TravelDuration(duration));
            State = TravelActivity;
            if (State == Activity.Fly) FlightCount++;
        }

        private void Update()
        {
            float delta = Time.deltaTime;
            if (delta <= 0) return;
            elapsed += delta;
            excitement = Mathf.MoveTowards(excitement, 0, delta * motion.excitementDecay);
            curiosity = Mathf.MoveTowards(curiosity, 0, delta * motion.curiosityDecay);
            ReactWhenTheBeatArrives();
            AdvanceActivity();

            FramePhase frame = MeasurePhase();
            PrepareFrame(frame);
            MoveAlongRoute(frame, delta);
            float pitch = AnimateBody(frame, delta);
            AnimateHead(frame, pitch, delta, out float nod);
            AnimateLimbs(frame, delta);
            AnimateAppendages(frame, nod, delta);
        }

        /// <summary>A beat struck a moment ago has now travelled far enough to reach this animal.</summary>
        private void ReactWhenTheBeatArrives()
        {
            if (pendingAt < 0 || Time.time < pendingAt) return;
            pendingAt = -1;
            excitement = Mathf.Max(excitement, pendingStrength);
            curiosity = motion.curiosity;
            if (Startle(pendingBeat, pendingStrength)) return;
            // Something already under way is left to finish; only a settled animal bolts.
            if (State is Activity.Fly or Activity.Hop) return;
            if (pendingBeat is Beat.Death or Beat.NewBest ||
                Range(0, 1) < pendingStrength * motion.travelChance) BeginTravel();
        }

        /// <summary>Travel becomes rest, and rest becomes the next trip.</summary>
        private void AdvanceActivity()
        {
            if (elapsed < duration) return;
            if (!Moving) { BeginTravel(); return; }
            transform.position = ArrivalPosition;
            State = RestActivity;
            RestCount++;
            elapsed = 0;
            duration = Mathf.Max(.001f, species.restSeconds * Range(motion.restDurationRange.x, motion.restDurationRange.y));
        }

        private FramePhase MeasurePhase() => new FramePhase(
            Mathf.Clamp01(elapsed / Mathf.Max(.001f, duration)), Time.time * tempo + phase, Moving);

        private void MoveAlongRoute(in FramePhase frame, float delta)
        {
            if (!frame.Moving)
            {
                Settle();
                return;
            }
            TrackDestination();
            transform.position = Vector3.Lerp(From, Destination, RouteProgress(frame))
                + RouteOffset(frame, Mathf.Sin(frame.T * Mathf.PI));
            Vector3 heading = Destination - From;
            heading.y = 0;
            if (heading.sqrMagnitude > .001f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(heading), delta * motion.turnSpeed);
        }

        /// <summary>Breathing, the gait's squash and stretch, and the lean of a body in motion.</summary>
        private float AnimateBody(in FramePhase frame, float delta)
        {
            float breath = Mathf.Sin(frame.Clock * motion.breathFrequency) * motion.breathAmplitude + curiosity * motion.curiosityBreath;
            float bounce = Bounce(frame, breath);
            body.localScale = Vector3.Scale(bodyScale,
                new Vector3(1 - bounce * motion.widthCompensation, 1 + bounce, 1 - bounce * motion.widthCompensation));
            float pitch = BodyPitch(frame);
            body.localRotation = Quaternion.Slerp(body.localRotation,
                bodyRotation * Quaternion.Euler(pitch, 0, BodyRoll(frame)),
                1 - Mathf.Exp(-delta * motion.rotationResponse));
            body.localPosition = bodyPosition;
            return pitch;
        }

        /// <summary>Feeding nods, idle glances, and a head drawn in out of harm's way.</summary>
        private void AnimateHead(in FramePhase frame, float pitch, float delta, out float nod)
        {
            head.localScale = Vector3.Lerp(head.localScale, headScale * HeadTuck, delta * motion.headScaleResponse);
            head.localPosition = Vector3.Lerp(head.localPosition, headPosition + HeadTuckOffset,
                1 - Mathf.Exp(-delta * motion.headPositionResponse));
            nod = HeadNod(frame);
            // Glances come in bursts: a slow envelope gates a faster side-to-side look.
            float look = Mathf.Sin(frame.Clock * motion.lookFrequency)
                * Mathf.Pow(Mathf.Max(0, Mathf.Sin(frame.Clock * motion.lookEnvelopeFrequency)),
                    Mathf.Max(.001f, motion.lookEnvelopePower)) * motion.lookAngle;
            head.localRotation = headRotation * Quaternion.Euler(
                nod - curiosity * motion.curiosityHeadTilt - pitch * motion.headPitchCompensation, look, 0);
        }

        private void AnimateLimbs(in FramePhase frame, float delta)
        {
            for (int i = 0; i < limbs.Length; i++)
            {
                PoseLimb(i, frame, out float angle, out float sweep, out float lift);
                limbs[i].localRotation = Quaternion.Slerp(limbs[i].localRotation,
                    limbRest[i] * Quaternion.Euler(angle, sweep, 0), 1 - Mathf.Exp(-delta * motion.limbRotationResponse));
                Vector3 local = limbPositions[i] + Vector3.up * lift;
                AdjustLimbPosition(ref local);
                limbs[i].localPosition = Vector3.Lerp(limbs[i].localPosition, local,
                    1 - Mathf.Exp(-delta * motion.limbPositionResponse));
            }
        }
    }
}
