using UnityEngine;

namespace GardenSnake.Garden
{
    /// <summary>
    /// What every animal in the garden does the same way: pick a spot in its patch, travel there,
    /// settle, breathe, glance about, and startle when the garden is struck. One asset, shared by
    /// the whole menagerie. Edit outside Play Mode.
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/Animal Motion")]
    public sealed class AnimalMotionSettings : ScriptableObject
    {
        [Header("Variation")]
        [Tooltip("Seed every animal draws its own variation from.")]
        public int randomSeed = 7919;
        [Tooltip("Mixed with each animal's authored phase so no two draw the same numbers.")]
        public int phaseSeedMultiplier = 104729;
        [Tooltip("Slowest and fastest an animal's own clock may run.")]
        public Vector2 tempoRange = new(.72f, 1.3f);
        [Min(0f), Tooltip("Shortest wait before an animal first sets off.")]
        public float initialRestMinimum = 1f;
        [Min(0f)] public float initialRestMaximumMultiplier = 2f;

        [Header("Travel")]
        [Tooltip("Where along the patch a trip towards the A end may finish, 0 to 1.")]
        public Vector2 destinationARange = new(0f, .35f);
        [Tooltip("And towards the B end.")]
        public Vector2 destinationBRange = new(.65f, 1f);
        [Tooltip("How much a trip's length may vary against the species' own travel time.")]
        public Vector2 travelDurationRange = new(.75f, 1.5f);
        [Tooltip("And a rest against its rest time.")]
        public Vector2 restDurationRange = new(.65f, 2.1f);
        [Min(0f), Tooltip("How much faster an excited animal covers the same ground.")]
        public float travelExcitementSpeed = .3f;
        [Min(0f), Tooltip("How quickly it turns to face where it is going.")]
        public float turnSpeed = 7f;

        [Header("Reactions")]
        [Min(.001f), Tooltip("World units per second a beat travels out to the wildlife.")]
        public float propagationSpeed = 30f;
        [Tooltip("Extra seconds each animal dithers before it reacts, so they never startle in unison.")]
        public Vector2 delayRange = new(.03f, .65f);
        [Min(0f), Tooltip("How much of a beat is lost per unit of distance.")]
        public float distanceAttenuation = .06f;
        [Min(0f), Tooltip("How quickly the stirred-up feeling fades.")]
        public float excitementDecay = .35f;
        [Min(0f), Tooltip("How quickly a curious animal loses interest.")]
        public float curiosityDecay = .8f;
        [Min(0f), Tooltip("How curious a beat leaves it.")]
        public float curiosity = 1f;
        [Min(0f), Tooltip("Chance per unit of strength that a settled animal bolts.")]
        public float travelChance = .35f;

        [Header("Body")]
        [Min(0f), Tooltip("Breaths per animation second, in radians.")]
        public float breathFrequency = 2.4f;
        [Min(0f)] public float breathAmplitude = .008f;
        [Min(0f), Tooltip("How much deeper a curious animal breathes.")]
        public float curiosityBreath = .02f;
        [Tooltip("How much of a breath's swelling comes out of the body's width.")]
        public float widthCompensation = .4f;
        [Min(0f)] public float rotationResponse = 14f;

        [Header("Head")]
        [Min(0f)] public float headScaleResponse = 10f;
        [Min(0f)] public float headPositionResponse = 10f;
        [Min(0f)] public float idleNodFrequency = 1.3f;
        public float idleNodAmplitude = 3f;
        [Min(0f), Tooltip("Glances come in bursts: a slow envelope gates a faster side-to-side look.")]
        public float lookFrequency = .55f;
        [Min(0f)] public float lookEnvelopeFrequency = .37f;
        [Min(.001f)] public float lookEnvelopePower = 4f;
        public float lookAngle = 24f;
        [Tooltip("Degrees a curious animal tips its head.")]
        public float curiosityHeadTilt = 16f;
        [Tooltip("How much of the body's pitch the head takes back out again.")]
        public float headPitchCompensation = .45f;

        [Header("Limbs")]
        [Min(0f)] public float limbRotationResponse = 22f;
        [Min(0f)] public float limbPositionResponse = 12f;
    }
}
