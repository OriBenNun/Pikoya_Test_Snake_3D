using UnityEngine;

namespace GardenSnake
{
    /// <summary>Decorative animation only. Routes stay in authored garden patches, outside the snake board.</summary>
    public sealed class GardenAnimal : GardenDweller
    {
        public enum Species { Bird, Bunny, Turtle, Butterfly, Ladybug }
        public enum Activity { Rest, Fly, Hop, Graze, Crawl, Hide }
        [SerializeField] private Species species;
        [SerializeField] private Transform body, head, leftWing, rightWing;
        [SerializeField] private Transform[] limbs;
        [SerializeField] private Transform[] ears = System.Array.Empty<Transform>();
        [SerializeField] private Transform perch;
        [SerializeField] private Vector3 groundA, groundB;
        [SerializeField, Min(.1f)] private float travelSeconds = 3f;
        [SerializeField, Min(.1f)] private float restSeconds = 4f;
        [SerializeField, Range(0f, 6f)] private float flightHeight = 2f;
        [SerializeField] private float phase;
        [Header("Motion")]
        [SerializeField, Range(.1f, .6f)] private float hopHeight = .30f;
        [SerializeField, Range(.3f, 1f)] private float hopLength = .65f;
        [SerializeField, Range(2f, 12f)] private float wingBeatsPerSecond = 5.5f;

        [Header("Variation and Travel")]
        [SerializeField] private int randomSeed = 7919;
        [SerializeField] private int phaseSeedMultiplier = 104729;
        [SerializeField, Tooltip("Minimum and maximum animation speed multipliers.")] private Vector2 tempoRange = new Vector2(.72f, 1.3f);
        [SerializeField, Min(0f)] private float initialRestMinimum = 1f;
        [SerializeField, Min(0f)] private float initialRestMaximumMultiplier = 2f;
        [SerializeField, Tooltip("Travel destination ranges along the ground A-to-B segment.")] private Vector2 destinationARange = new Vector2(0f, .35f);
        [SerializeField] private Vector2 destinationBRange = new Vector2(.65f, 1f);
        [SerializeField] private Vector2 travelDurationRange = new Vector2(.75f, 1.5f);
        [SerializeField] private Vector2 restDurationRange = new Vector2(.65f, 2.1f);
        [SerializeField, Min(0f)] private float travelExcitementSpeed = .3f;
        [SerializeField, Min(0f)] private float turnSpeed = 7f;
        [SerializeField, Tooltip("Birds use their perch on alternate trips when enabled.")] private bool usePerch = true;

        [Header("Reactions")]
        [SerializeField, Min(.001f), Tooltip("Reaction propagation speed in world units per second.")] private float reactionPropagationSpeed = 30f;
        [SerializeField, Tooltip("Additional random reaction delay in seconds.")] private Vector2 reactionDelayRange = new Vector2(.03f, .65f);
        [SerializeField, Min(0f)] private float reactionDistanceAttenuation = .06f;
        [SerializeField, Min(0f)] private float excitementDecay = .35f;
        [SerializeField, Min(0f)] private float curiosityDecay = .8f;
        [SerializeField, Min(0f)] private float reactionCuriosity = 1f;
        [SerializeField, Min(0f)] private float reactionTravelChance = .35f;
        [SerializeField, Min(.001f)] private float hideSeconds = 2.5f;

        [Header("Hopping and Flight Path")]
        [SerializeField, Tooltip("Minimum and maximum seconds per hop.")] private Vector2 hopDurationRange = new Vector2(.62f, .82f);
        [SerializeField, Min(0f)] private float hopExcitementSpeed = .2f;
        [SerializeField, Min(0f)] private float hopExcitementHeight = .10f;
        [SerializeField, Range(.001f, .499f), Tooltip("Fraction of each hop spent crouching at each end.")] private float hopCrouchFraction = .18f;
        [SerializeField, Min(0f)] private float butterflySwayCycles = 2f;
        [SerializeField, Min(0f)] private float butterflySwayDistance = .15f;

        [Header("Body Motion")]
        [SerializeField, Min(0f), Tooltip("Breathing angular frequency in radians per animation second.")] private float breathFrequency = 2.4f;
        [SerializeField, Min(0f)] private float breathAmplitude = .008f;
        [SerializeField, Min(0f)] private float curiosityBreath = .02f;
        [SerializeField] private float hopBodyStretch = .055f;
        [SerializeField] private float crouchBodySquash = .15f;
        [SerializeField] private float bodyWidthCompensation = .4f;
        [SerializeField] private float hopPitch = -9f;
        [SerializeField] private float birdFlightPitch = -12f;
        [SerializeField, Min(0f)] private float turtleRollFrequency = 5f;
        [SerializeField] private float turtleRoll = 2f;
        [SerializeField, Min(0f)] private float bodyRotationResponse = 14f;

        [Header("Head Motion")]
        [SerializeField, Range(0f, 1f)] private float hiddenHeadScale = .75f;
        [SerializeField] private Vector3 hiddenHeadOffset = new Vector3(0f, -.06f, -.36f);
        [SerializeField, Min(0f)] private float headScaleResponse = 10f;
        [SerializeField, Min(0f)] private float headPositionResponse = 10f;
        [SerializeField, Min(0f)] private float nibbleFrequency = .7f;
        [SerializeField] private float grazeNod = 18f;
        [SerializeField, Min(0f)] private float grazeNodFrequency = 4f;
        [SerializeField] private float grazeNodAmplitude = 12f;
        [SerializeField, Min(0f)] private float idleNodFrequency = 1.3f;
        [SerializeField] private float idleNodAmplitude = 3f;
        [SerializeField, Min(0f)] private float lookFrequency = .55f;
        [SerializeField, Min(0f)] private float lookEnvelopeFrequency = .37f;
        [SerializeField, Min(.001f)] private float lookEnvelopePower = 4f;
        [SerializeField] private float lookAngle = 24f;
        [SerializeField] private float curiosityHeadTilt = 16f;
        [SerializeField] private float headPitchCompensation = .45f;

        [Header("Limbs")]
        [SerializeField] private float rearHopLimbAngle = -38f;
        [SerializeField] private float frontHopLimbAngle = -52f;
        [SerializeField] private float frontHopLimbRecovery = 30f;
        [SerializeField] private float birdFlightLimbAngle = -65f;
        [SerializeField, Min(0f)] private float turtleStrideCycles = 5f;
        [SerializeField, Min(0f)] private float insectStrideCycles = 9f;
        [SerializeField] private float turtleStrideAngle = 20f;
        [SerializeField] private float insectStrideAngle = 24f;
        [SerializeField, Min(0f)] private float strideLift = .035f;
        [SerializeField, Min(0f)] private float limbRotationResponse = 22f;
        [SerializeField, Min(0f)] private float limbPositionResponse = 12f;
        [SerializeField, Range(0f, 1f)] private float hiddenLimbSpread = .6f;

        [Header("Ears")]
        [SerializeField] private float earDragPhase = .65f;
        [SerializeField] private float earDragPhaseSpacing = .18f;
        [SerializeField] private float earDragAngle = 22f;
        [SerializeField, Min(0f)] private float earTwitchFrequency = .83f;
        [SerializeField] private float earTwitchPhaseSpacing = 2.6f;
        [SerializeField, Min(.001f)] private float earTwitchPower = 18f;
        [SerializeField] private float earTwitchAngle = 13f;
        [SerializeField] private float earNodCompensation = -.35f;
        [SerializeField, Min(0f)] private float earRotationResponse = 13f;

        [Header("Wings")]
        [SerializeField, Tooltip("Normalized flight progress at which birds start and stop gliding.")] private Vector2 glideProgressRange = new Vector2(.25f, .7f);
        [SerializeField, Min(0f)] private float butterflyWingSpeedMultiplier = 1.25f;
        [SerializeField] private float glideWingAngle = 10f;
        [SerializeField] private float flapWingAngle = 52f;
        [SerializeField] private float flapWingOffset = 8f;
        [SerializeField] private float butterflyRestWingAngle = 28f;
        [SerializeField, Min(0f)] private float butterflyRestWingFrequency = 3f;
        [SerializeField] private float butterflyRestWingAmplitude = 18f;
        [SerializeField] private float birdRestWingAngle = -12f;
        [SerializeField, Min(0f)] private float wingRotationResponse = 35f;
        [SerializeField] private float foldedWingAngle = 65f;
        [SerializeField, Min(0f)] private float foldedWingScale = .68f;
        [SerializeField, Min(0f)] private float wingScaleResponse = 12f;
        public Activity State { get; private set; }
        public int FlightCount { get; private set; }
        public int RestCount { get; private set; }
        /// <summary>Where this frame sits in whatever the animal is currently doing.</summary>
        private readonly struct FramePhase
        {
            /// <summary>0 to 1 through the current activity.</summary>
            public readonly float T;
            /// <summary>The animal's own tempo-scaled clock, offset so no two move in step.</summary>
            public readonly float Clock;
            /// <summary>0 to 1 through the airborne part of a single hop.</summary>
            public readonly float Flight;
            /// <summary>The height curve of one hop: 0 at each end, 1 at the top.</summary>
            public readonly float HopArc;
            /// <summary>Compression at each end of a hop, as the animal gathers and lands.</summary>
            public readonly float Crouch;
            public readonly bool Moving;

            public FramePhase(float t, float clock, float flight, float hopArc, float crouch, bool moving)
            {
                T = t;
                Clock = clock;
                Flight = flight;
                HopArc = hopArc;
                Crouch = crouch;
                Moving = moving;
            }
        }

        private Vector3 from;
        private Vector3 to;
        private Vector3 bodyScale;
        private Vector3 headScale;
        private Vector3 headPosition;
        private Quaternion headRotation;
        private Quaternion[] limbRest;
        private Quaternion[] earRest;
        private Vector3[] limbPositions;
        private Vector3 bodyPosition;
        private Quaternion bodyRotation;
        private int hops;
        private float flapPhase;
        private float wingAngle;
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
        private bool atPerch;

        /// <summary>The animal is going somewhere, however it happens to get there.</summary>
        private bool Moving => State is Activity.Fly or Activity.Hop or Activity.Crawl;

        private void Awake()
        {
            random = new System.Random(randomSeed + Mathf.RoundToInt(phase * phaseSeedMultiplier));
            tempo = Range(tempoRange.x, tempoRange.y);
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
            earRest = new Quaternion[ears.Length];
            for (int i = 0; i < ears.Length; i++) earRest[i] = ears[i].localRotation;
            State = species == Species.Butterfly ? Activity.Fly : Activity.Rest;
            duration = Mathf.Max(.001f, Range(initialRestMinimum, restSeconds * initialRestMaximumMultiplier));
            from = to = transform.position;
            if (species == Species.Butterfly) BeginTravel();
            else RestCount++;
        }

        protected override void React(Beat beat, Vector3 at, float strength)
        {
            pendingBeat = beat;
            float distance = Vector3.Distance(transform.position, at);
            pendingAt = Time.time + TravelTime(distance, reactionPropagationSpeed) +
                Range(reactionDelayRange.x, reactionDelayRange.y);
            pendingStrength = Carried(strength, distance, reactionDistanceAttenuation);
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

        private void BeginTravel()
        {
            from = transform.position;
            destinationB = !destinationB;
            atPerch = usePerch && species == Species.Bird && destinationB && perch != null;
            to = atPerch
                ? perch.position
                : Vector3.Lerp(groundA, groundB, destinationB
                    ? Range(destinationBRange.x, destinationBRange.y)
                    : Range(destinationARange.x, destinationARange.y));
            elapsed = 0;
            // An excited animal covers the same ground in less time.
            duration = travelSeconds * Range(travelDurationRange.x, travelDurationRange.y)
                / Mathf.Max(.001f, 1 + excitement * travelExcitementSpeed);
            hops = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to) / Mathf.Max(.001f, hopLength)));
            if (species == Species.Bunny)
                duration = hops * Range(hopDurationRange.x, hopDurationRange.y)
                    / Mathf.Max(.001f, 1 + excitement * hopExcitementSpeed);
            duration = Mathf.Max(.001f, duration);
            State = species is Species.Bird or Species.Butterfly ? Activity.Fly
                : species == Species.Bunny ? Activity.Hop
                : Activity.Crawl;
            if (State == Activity.Fly) FlightCount++;
        }

        private void Update()
        {
            float delta = Time.deltaTime;
            if (delta <= 0) return;
            elapsed += delta;
            excitement = Mathf.MoveTowards(excitement, 0, delta * excitementDecay);
            curiosity = Mathf.MoveTowards(curiosity, 0, delta * curiosityDecay);
            ReactWhenTheBeatArrives();
            AdvanceActivity();

            FramePhase frame = MeasurePhase();
            MoveAlongRoute(frame, delta);
            float pitch = AnimateBody(frame, delta);
            AnimateHead(frame, pitch, delta, out float nod);
            AnimateLimbs(frame, delta);
            AnimateEars(frame, nod, delta);
            AnimateWings(frame, delta);
        }

        /// <summary>A beat struck a moment ago has now travelled far enough to reach this animal.</summary>
        private void ReactWhenTheBeatArrives()
        {
            if (pendingAt < 0 || Time.time < pendingAt) return;
            pendingAt = -1;
            excitement = Mathf.Max(excitement, pendingStrength);
            curiosity = reactionCuriosity;
            if (species == Species.Turtle && pendingBeat == Beat.Death)
            {
                State = Activity.Hide;
                elapsed = 0;
                duration = Mathf.Max(.001f, hideSeconds);
                return;
            }
            // Something already under way is left to finish; only a settled animal bolts.
            if (State is Activity.Fly or Activity.Hop) return;
            if (pendingBeat is Beat.Death or Beat.NewBest ||
                Range(0, 1) < pendingStrength * reactionTravelChance) BeginTravel();
        }

        /// <summary>Travel becomes rest, and rest becomes the next trip.</summary>
        private void AdvanceActivity()
        {
            if (elapsed < duration) return;
            if (!Moving) { BeginTravel(); return; }
            transform.position = atPerch && perch != null ? perch.position : to;
            State = species == Species.Bunny ? Activity.Graze : Activity.Rest;
            RestCount++;
            elapsed = 0;
            duration = Mathf.Max(.001f, restSeconds * Range(restDurationRange.x, restDurationRange.y));
        }

        private FramePhase MeasurePhase()
        {
            float t = Mathf.Clamp01(elapsed / Mathf.Max(.001f, duration));
            float clock = Time.time * tempo + phase;
            float hopCycle = Mathf.Repeat(t * hops, 1f);
            float crouchFraction = Mathf.Clamp(hopCrouchFraction, .001f, .499f);
            float flight = Mathf.Clamp01((hopCycle - crouchFraction) / Mathf.Max(.001f, 1 - 2 * crouchFraction));
            float hopArc = 4 * flight * (1 - flight);
            float crouch = hopCycle < crouchFraction
                ? Mathf.Sin(hopCycle / crouchFraction * Mathf.PI)
                : hopCycle > 1 - crouchFraction
                    ? Mathf.Sin((hopCycle - (1 - crouchFraction)) / crouchFraction * Mathf.PI)
                    : 0;
            return new FramePhase(t, clock, flight, hopArc, crouch, Moving);
        }

        private void MoveAlongRoute(in FramePhase frame, float delta)
        {
            if (!frame.Moving)
            {
                if (atPerch && perch != null) transform.position = perch.position;
                return;
            }
            // A perch can sway in the wind, so re-read it rather than aiming at where it was.
            if (atPerch && perch != null) to = perch.position;
            float t = frame.T;
            float smooth = t * t * (3 - 2 * t);
            if (State == Activity.Hop)
                smooth = (Mathf.Min(hops - 1, Mathf.FloorToInt(t * hops)) + Mathf.SmoothStep(0, 1, frame.Flight)) / hops;
            Vector3 position = Vector3.Lerp(from, to, smooth);
            float arc = Mathf.Sin(t * Mathf.PI);
            if (State == Activity.Fly) position.y += arc * flightHeight;
            if (State == Activity.Hop) position.y += frame.HopArc * (hopHeight + excitement * hopExcitementHeight);
            if (species == Species.Butterfly)
                position.x += Mathf.Sin(t * Mathf.PI * 2 * butterflySwayCycles + phase) * arc * butterflySwayDistance;
            transform.position = position;
            Vector3 heading = to - from;
            heading.y = 0;
            if (heading.sqrMagnitude > .001f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(heading), delta * turnSpeed);
        }

        /// <summary>Breathing, the hop's squash and stretch, and the lean of a body in motion.</summary>
        private float AnimateBody(in FramePhase frame, float delta)
        {
            float breath = Mathf.Sin(frame.Clock * breathFrequency) * breathAmplitude + curiosity * curiosityBreath;
            float bounce = State == Activity.Hop
                ? frame.HopArc * hopBodyStretch - frame.Crouch * crouchBodySquash
                : breath;
            body.localScale = Vector3.Scale(bodyScale,
                new Vector3(1 - bounce * bodyWidthCompensation, 1 + bounce, 1 - bounce * bodyWidthCompensation));
            float pitch = State == Activity.Hop ? Mathf.Sin(frame.Flight * Mathf.PI * 2) * hopPitch
                : species == Species.Bird && frame.Moving ? Mathf.Sin(frame.T * Mathf.PI * 2) * birdFlightPitch
                : 0;
            float roll = species == Species.Turtle && frame.Moving
                ? Mathf.Sin(frame.Clock * turtleRollFrequency) * turtleRoll
                : 0;
            body.localRotation = Quaternion.Slerp(body.localRotation,
                bodyRotation * Quaternion.Euler(pitch, 0, roll), 1 - Mathf.Exp(-delta * bodyRotationResponse));
            body.localPosition = bodyPosition;
            return pitch;
        }

        /// <summary>Grazing nods, idle glances, and a turtle pulling its head in.</summary>
        private void AnimateHead(in FramePhase frame, float pitch, float delta, out float nod)
        {
            float tuck = State == Activity.Hide ? hiddenHeadScale : 1f;
            head.localScale = Vector3.Lerp(head.localScale, headScale * tuck, delta * headScaleResponse);
            head.localPosition = Vector3.Lerp(head.localPosition,
                headPosition + (State == Activity.Hide ? hiddenHeadOffset : Vector3.zero),
                1 - Mathf.Exp(-delta * headPositionResponse));
            float nibble = Mathf.Max(0, Mathf.Sin(frame.Clock * nibbleFrequency));
            nod = State == Activity.Graze
                ? nibble * (grazeNod + Mathf.Sin(frame.Clock * grazeNodFrequency) * grazeNodAmplitude)
                : Mathf.Sin(frame.Clock * idleNodFrequency) * idleNodAmplitude;
            // Glances come in bursts: a slow envelope gates a faster side-to-side look.
            float look = Mathf.Sin(frame.Clock * lookFrequency)
                * Mathf.Pow(Mathf.Max(0, Mathf.Sin(frame.Clock * lookEnvelopeFrequency)),
                    Mathf.Max(.001f, lookEnvelopePower)) * lookAngle;
            head.localRotation = headRotation * Quaternion.Euler(
                nod - curiosity * curiosityHeadTilt - pitch * headPitchCompensation, look, 0);
        }

        private void AnimateLimbs(in FramePhase frame, float delta)
        {
            for (int i = 0; i < limbs.Length; i++)
            {
                float angle = 0, lift = 0, sweep = 0;
                if (species == Species.Bunny && State == Activity.Hop)
                {
                    bool rear = i % 2 == 0;
                    angle = rear
                        ? Mathf.Sin(frame.Flight * Mathf.PI * 2) * rearHopLimbAngle
                        : frame.HopArc * frontHopLimbAngle
                          + Mathf.Sin(frame.Flight * Mathf.PI) * frame.Flight * frontHopLimbRecovery;
                }
                else if (species == Species.Bird)
                {
                    angle = frame.Moving ? Mathf.Sin(frame.T * Mathf.PI) * birdFlightLimbAngle : 0;
                }
                else if (frame.Moving)
                {
                    // Diagonal pairs on turtles; alternating tripods on insects.
                    bool turtle = species == Species.Turtle;
                    float offset = turtle ? (i == 0 || i == 3 ? 0 : Mathf.PI) : (i % 2 + i / 2) * Mathf.PI;
                    float cycles = turtle ? turtleStrideCycles : insectStrideCycles;
                    float stride = Mathf.Sin(frame.T * Mathf.PI * 2 * cycles + offset);
                    sweep = stride * (turtle ? turtleStrideAngle : insectStrideAngle);
                    lift = Mathf.Max(0, stride) * strideLift;
                }
                limbs[i].localRotation = Quaternion.Slerp(limbs[i].localRotation,
                    limbRest[i] * Quaternion.Euler(angle, sweep, 0), 1 - Mathf.Exp(-delta * limbRotationResponse));
                Vector3 local = limbPositions[i] + Vector3.up * lift;
                if (species == Species.Turtle && State == Activity.Hide) local.x *= hiddenLimbSpread;
                limbs[i].localPosition = Vector3.Lerp(limbs[i].localPosition, local,
                    1 - Mathf.Exp(-delta * limbPositionResponse));
            }
        }

        /// <summary>Ears drag behind a hop and twitch on their own between times.</summary>
        private void AnimateEars(in FramePhase frame, float nod, float delta)
        {
            for (int i = 0; i < ears.Length; i++)
            {
                float drag = State == Activity.Hop
                    ? Mathf.Sin(frame.Flight * Mathf.PI * 2 - earDragPhase - i * earDragPhaseSpacing)
                      * frame.HopArc * earDragAngle
                    : 0;
                float twitch = Mathf.Pow(
                    Mathf.Max(0, Mathf.Sin(frame.Clock * earTwitchFrequency + i * earTwitchPhaseSpacing)),
                    Mathf.Max(.001f, earTwitchPower)) * earTwitchAngle;
                var target = earRest[i] * Quaternion.Euler(
                    drag + nod * earNodCompensation, 0, twitch * (i == 0 ? -1 : 1));
                ears[i].localRotation = Quaternion.Slerp(ears[i].localRotation, target,
                    1 - Mathf.Exp(-delta * earRotationResponse));
            }
        }

        /// <summary>Birds flap, then glide the middle of a flight; butterflies never stop.</summary>
        private void AnimateWings(in FramePhase frame, float delta)
        {
            if (leftWing == null || rightWing == null) return;
            bool butterfly = species == Species.Butterfly;
            bool glide = !butterfly && frame.T > glideProgressRange.x && frame.T < glideProgressRange.y;
            flapPhase += delta * wingBeatsPerSecond * (butterfly ? butterflyWingSpeedMultiplier : 1) * Mathf.PI * 2;
            float flap = State == Activity.Fly
                ? glide ? glideWingAngle : Mathf.Sin(flapPhase) * flapWingAngle + flapWingOffset
                : butterfly
                    ? butterflyRestWingAngle + Mathf.Sin(frame.Clock * butterflyRestWingFrequency) * butterflyRestWingAmplitude
                    : birdRestWingAngle;
            wingAngle = Mathf.Lerp(wingAngle, flap, 1 - Mathf.Exp(-delta * wingRotationResponse));
            bool folded = !butterfly && State != Activity.Fly;
            float fold = folded ? foldedWingAngle : 0;
            float span = folded ? foldedWingScale : 1;
            leftWing.localScale = Vector3.Lerp(leftWing.localScale, Vector3.one * span,
                1 - Mathf.Exp(-delta * wingScaleResponse));
            rightWing.localScale = leftWing.localScale;
            leftWing.localRotation = Quaternion.Euler(0, -fold, -wingAngle);
            rightWing.localRotation = Quaternion.Euler(0, fold, wingAngle);
        }
    }
}
