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
        [SerializeField] private Transform perch;
        [SerializeField] private Vector3 groundA, groundB;
        [SerializeField, Min(.1f)] private float travelSeconds = 3f;
        [SerializeField, Min(.1f)] private float restSeconds = 4f;
        [SerializeField, Range(0f, 6f)] private float flightHeight = 2f;
        [SerializeField] private float phase;
        public Activity State { get; private set; }
        public int FlightCount { get; private set; }
        public int RestCount { get; private set; }
        public int ReactionCount { get; private set; }
        private SnakeFeel feel;
        private Vector3 from, to, bodyScale, headScale;
        private Vector3 headPosition;
        private Quaternion headRotation;
        private Quaternion[] limbRest;
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
            headScale = head.localScale;
            headPosition = head.localPosition;
            headRotation = head.localRotation;
            limbRest = new Quaternion[limbs.Length];
            for (int i = 0; i < limbs.Length; i++) limbRest[i] = limbs[i].localRotation;
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
            if (moving)
            {
                if (atPerch && perch != null) to = perch.position;
                float smooth = t * t * (3 - 2 * t);
                Vector3 position = Vector3.Lerp(from, to, smooth);
                float arc = Mathf.Sin(t * Mathf.PI);
                if (State == Activity.Fly) position.y += arc * flightHeight;
                if (State == Activity.Hop) position.y += Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3)) * (.32f + excitement * .2f);
                if (species == Species.Butterfly) position.x += Mathf.Sin(t * Mathf.PI * 4 + phase) * arc * .15f;
                transform.position = position;
                Vector3 heading = to - from; heading.y = 0;
                if (heading.sqrMagnitude > .001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(heading), dt * 7);
            }
            else if (atPerch && perch != null) transform.position = perch.position;

            float breath = Mathf.Sin(clock * 2.4f) * .015f + curiosity * .055f;
            float bounce = State == Activity.Hop ? Mathf.Sin(t * Mathf.PI * 6) * .09f : breath;
            body.localScale = Vector3.Scale(bodyScale, new Vector3(1 - bounce * .4f, 1 + bounce, 1 - bounce * .4f));
            float tuck = State == Activity.Hide ? .12f : 1f;
            head.localScale = Vector3.Lerp(head.localScale, headScale * tuck, dt * 10);
            head.localPosition = Vector3.Lerp(head.localPosition, headPosition + (State == Activity.Hide ? Vector3.back * .3f : Vector3.zero), dt * 10);
            float nibble = Mathf.Max(0, Mathf.Sin(clock * .7f));
            float nod = State == Activity.Graze ? nibble * (18 + Mathf.Sin(clock * 4) * 12) : Mathf.Sin(clock * 1.3f) * 3;
            head.localRotation = headRotation * Quaternion.Euler(nod - curiosity * 16, Mathf.Sin(clock * .8f) * (8 + curiosity * 18), 0);
            for (int i = 0; i < limbs.Length; i++)
            {
                float wiggle = moving ? Mathf.Sin(clock * (species == Species.Turtle ? 7 : 15) + i * Mathf.PI) * 23 : Mathf.Sin(clock * 2 + i) * 5;
                limbs[i].localRotation = limbRest[i] * Quaternion.Euler(wiggle, 0, species == Species.Bunny ? wiggle * .25f : 0);
                if (species == Species.Turtle) limbs[i].localScale = Vector3.Lerp(limbs[i].localScale, Vector3.one * tuck, dt * 10);
            }
            if (leftWing != null && rightWing != null)
            {
                bool glide = species == Species.Bird && t > .2f && t < .8f && Mathf.Sin(clock * 1.8f) > .25f;
                float flap = State == Activity.Fly ? (glide ? 8 : Mathf.Sin(clock * (species == Species.Butterfly ? 28 : 23)) * 58) : -68 + Mathf.Sin(clock * 2) * 2;
                leftWing.localRotation = Quaternion.Euler(0, 0, flap);
                rightWing.localRotation = Quaternion.Euler(0, 0, -flap);
            }
        }
    }
}
