using UnityEngine;

namespace GardenSnake
{
    /// <summary>Decorative animation only. Routes stay in authored garden patches, outside the snake board.</summary>
    public sealed class GardenAnimal : MonoBehaviour
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
        public Activity State { get; private set; }
        public int FlightCount { get; private set; }
        public int RestCount { get; private set; }
        public int ReactionCount { get; private set; }
        private SnakeFeel feel;
        private Vector3 from, to, bodyScale, headScale;
        private Vector3 headPosition;
        private Quaternion headRotation;
        private Quaternion[] limbRest;
        private Quaternion[] earRest;
        private Vector3[] limbPositions;
        private Vector3 bodyPosition;
        private Quaternion bodyRotation;
        private int hops;
        private float flapPhase, wingAngle;
        private float elapsed, duration, excitement, pendingAt = -1;
        private float pendingStrength, curiosity, tempo;
        private System.Random random;
        private SnakeFeel.Beat pendingBeat;
        private bool destinationB, atPerch;

        private void Awake()
        {
            random = new System.Random(7919 + Mathf.RoundToInt(phase * 104729));
            tempo = Range(.72f, 1.3f);
            bodyScale = body.localScale;
            bodyPosition = body.localPosition;
            bodyRotation = body.localRotation;
            headScale = head.localScale;
            headPosition = head.localPosition;
            headRotation = head.localRotation;
            limbRest = new Quaternion[limbs.Length];
            limbPositions = new Vector3[limbs.Length];
            for (int i = 0; i < limbs.Length; i++) { limbRest[i] = limbs[i].localRotation; limbPositions[i] = limbs[i].localPosition; }
            earRest = new Quaternion[ears.Length];
            for (int i = 0; i < ears.Length; i++) earRest[i] = ears[i].localRotation;
            State = species == Species.Butterfly ? Activity.Fly : Activity.Rest;
            duration = Range(1f, restSeconds * 2);
            from = to = transform.position;
            if (species == Species.Butterfly) BeginTravel();
            else RestCount++;
        }

        private void OnEnable()
        {
            feel = FindFirstObjectByType<SnakeFeel>();
            if (feel != null) feel.Reacted += React;
        }

        private void OnDisable() { if (feel != null) feel.Reacted -= React; }

        private void React(SnakeFeel.Beat beat, Vector3 at, float strength)
        {
            ReactionCount++;
            pendingBeat = beat;
            float distance = Vector3.Distance(transform.position, at);
            pendingAt = Time.time + distance / 30f + Range(.03f, .65f);
            pendingStrength = strength / (1 + distance * .06f);
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

        private void BeginTravel()
        {
            from = transform.position;
            destinationB = !destinationB;
            atPerch = species == Species.Bird && destinationB && perch != null;
            to = atPerch ? perch.position : Vector3.Lerp(groundA, groundB, destinationB ? Range(.65f, 1f) : Range(0f, .35f));
            elapsed = 0;
            duration = travelSeconds * Range(.75f, 1.5f) / (1 + excitement * .3f);
            hops = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to) / hopLength));
            if (species == Species.Bunny) duration = hops * Range(.62f, .82f) / (1 + excitement * .2f);
            State = species == Species.Bird || species == Species.Butterfly ? Activity.Fly :
                species == Species.Bunny ? Activity.Hop : Activity.Crawl;
            if (State == Activity.Fly) FlightCount++;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            elapsed += dt;
            excitement = Mathf.MoveTowards(excitement, 0, dt * .35f);
            curiosity = Mathf.MoveTowards(curiosity, 0, dt * .8f);
            if (pendingAt >= 0 && Time.time >= pendingAt)
            {
                pendingAt = -1;
                excitement = Mathf.Max(excitement, pendingStrength);
                curiosity = 1;
                if (species == Species.Turtle && pendingBeat == SnakeFeel.Beat.Death)
                {
                    State = Activity.Hide; elapsed = 0; duration = 2.5f;
                }
                else if (State != Activity.Fly && State != Activity.Hop &&
                    (pendingBeat == SnakeFeel.Beat.Death || pendingBeat == SnakeFeel.Beat.NewBest ||
                     Range(0, 1) < pendingStrength * .35f)) BeginTravel();
            }
            bool moving = State == Activity.Fly || State == Activity.Hop || State == Activity.Crawl;
            if (elapsed >= duration)
            {
                if (moving)
                {
                    transform.position = atPerch && perch != null ? perch.position : to;
                    State = species == Species.Bunny ? Activity.Graze : Activity.Rest;
                    RestCount++;
                    elapsed = 0;
                    duration = restSeconds * Range(.65f, 2.1f);
                }
                else BeginTravel();
                moving = State == Activity.Fly || State == Activity.Hop || State == Activity.Crawl;
            }
            float t = Mathf.Clamp01(elapsed / duration);
            float clock = Time.time * tempo + phase;
            float hopCycle = Mathf.Repeat(t * hops, 1f);
            float flight = Mathf.Clamp01((hopCycle - .18f) / .64f);
            float hopArc = 4 * flight * (1 - flight);
            float crouch = hopCycle < .18f ? Mathf.Sin(hopCycle / .18f * Mathf.PI) :
                hopCycle > .82f ? Mathf.Sin((hopCycle - .82f) / .18f * Mathf.PI) : 0;
            if (moving)
            {
                if (atPerch && perch != null) to = perch.position;
                float smooth = t * t * (3 - 2 * t);
                if (State == Activity.Hop)
                    smooth = (Mathf.Min(hops - 1, Mathf.FloorToInt(t * hops)) + Mathf.SmoothStep(0, 1, flight)) / hops;
                Vector3 position = Vector3.Lerp(from, to, smooth);
                float arc = Mathf.Sin(t * Mathf.PI);
                if (State == Activity.Fly) position.y += arc * flightHeight;
                if (State == Activity.Hop) position.y += hopArc * (hopHeight + excitement * .10f);
                if (species == Species.Butterfly) position.x += Mathf.Sin(t * Mathf.PI * 4 + phase) * arc * .15f;
                transform.position = position;
                Vector3 heading = to - from; heading.y = 0;
                if (heading.sqrMagnitude > .001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(heading), dt * 7);
            }
            else if (atPerch && perch != null) transform.position = perch.position;

            float breath = Mathf.Sin(clock * 2.4f) * .008f + curiosity * .02f;
            float bounce = State == Activity.Hop ? hopArc * .055f - crouch * .15f : breath;
            body.localScale = Vector3.Scale(bodyScale, new Vector3(1 - bounce * .4f, 1 + bounce, 1 - bounce * .4f));
            float pitch = State == Activity.Hop ? Mathf.Sin(flight * Mathf.PI * 2) * -9 :
                species == Species.Bird && moving ? -Mathf.Sin(t * Mathf.PI * 2) * 12 : 0;
            float roll = species == Species.Turtle && moving ? Mathf.Sin(clock * 5) * 2 : 0;
            body.localRotation = Quaternion.Slerp(body.localRotation, bodyRotation * Quaternion.Euler(pitch, 0, roll), 1 - Mathf.Exp(-dt * 14));
            body.localPosition = bodyPosition;
            float tuck = State == Activity.Hide ? .75f : 1f;
            head.localScale = Vector3.Lerp(head.localScale, headScale * tuck, dt * 10);
            head.localPosition = Vector3.Lerp(head.localPosition, headPosition + (State == Activity.Hide ? new Vector3(0, -.06f, -.36f) : Vector3.zero), 1 - Mathf.Exp(-dt * 10));
            float nibble = Mathf.Max(0, Mathf.Sin(clock * .7f));
            float nod = State == Activity.Graze ? nibble * (18 + Mathf.Sin(clock * 4) * 12) : Mathf.Sin(clock * 1.3f) * 3;
            float look = Mathf.Sin(clock * .55f) * Mathf.Pow(Mathf.Max(0, Mathf.Sin(clock * .37f)), 4) * 24;
            head.localRotation = headRotation * Quaternion.Euler(nod - curiosity * 16 - pitch * .45f, look, 0);
            for (int i = 0; i < limbs.Length; i++)
            {
                float angle = 0, lift = 0, sweep = 0;
                if (species == Species.Bunny && State == Activity.Hop)
                {
                    bool rear = i % 2 == 0;
                    angle = rear ? -Mathf.Sin(flight * Mathf.PI * 2) * 38 : -hopArc * 52 + Mathf.Sin(flight * Mathf.PI) * flight * 30;
                }
                else if (species == Species.Bird) angle = moving ? Mathf.Sin(t * Mathf.PI) * -65 : 0;
                else if (moving)
                {
                    // Diagonal pairs on turtles; alternating tripods on insects.
                    float offset = species == Species.Turtle ? (i == 0 || i == 3 ? 0 : Mathf.PI) : (i % 2 + i / 2) * Mathf.PI;
                    float stride = Mathf.Sin(t * Mathf.PI * 2 * (species == Species.Turtle ? 5 : 9) + offset);
                    sweep = stride * (species == Species.Turtle ? 20 : 24);
                    lift = Mathf.Max(0, stride) * .035f;
                }
                limbs[i].localRotation = Quaternion.Slerp(limbs[i].localRotation, limbRest[i] * Quaternion.Euler(angle, sweep, 0), 1 - Mathf.Exp(-dt * 22));
                Vector3 local = limbPositions[i] + Vector3.up * lift;
                if (species == Species.Turtle && State == Activity.Hide) local.x *= .6f;
                limbs[i].localPosition = Vector3.Lerp(limbs[i].localPosition, local, 1 - Mathf.Exp(-dt * 12));
            }
            for (int i = 0; i < ears.Length; i++)
            {
                float drag = State == Activity.Hop ? Mathf.Sin(flight * Mathf.PI * 2 - .65f - i * .18f) * hopArc * 22 : 0;
                float twitch = Mathf.Pow(Mathf.Max(0, Mathf.Sin(clock * .83f + i * 2.6f)), 18) * 13;
                var target = earRest[i] * Quaternion.Euler(drag + nod * -.35f, 0, twitch * (i == 0 ? -1 : 1));
                ears[i].localRotation = Quaternion.Slerp(ears[i].localRotation, target, 1 - Mathf.Exp(-dt * 13));
            }
            if (leftWing != null && rightWing != null)
            {
                bool butterfly = species == Species.Butterfly;
                bool glide = !butterfly && t > .25f && t < .7f;
                flapPhase += dt * wingBeatsPerSecond * (butterfly ? 1.25f : 1) * Mathf.PI * 2;
                float flap = State == Activity.Fly ? (glide ? 10 : Mathf.Sin(flapPhase) * 52 + 8) :
                    butterfly ? 28 + Mathf.Sin(clock * 3) * 18 : -12;
                wingAngle = Mathf.Lerp(wingAngle, flap, 1 - Mathf.Exp(-dt * 35));
                float fold = !butterfly && State != Activity.Fly ? 65 : 0;
                float span = !butterfly && State != Activity.Fly ? .68f : 1;
                leftWing.localScale = Vector3.Lerp(leftWing.localScale, Vector3.one * span, 1 - Mathf.Exp(-dt * 12));
                rightWing.localScale = leftWing.localScale;
                leftWing.localRotation = Quaternion.Euler(0, -fold, -wingAngle);
                rightWing.localRotation = Quaternion.Euler(0, fold, wingAngle);
            }
        }
    }
}
