using UnityEngine;

namespace GardenSnake
{
    /// <summary>
    /// Crosses its patch in discrete hops, gathering and landing at each end of one, and grazes
    /// where it lands. Its ears drag behind the hop and twitch on their own between times.
    /// </summary>
    public sealed class GardenBunny : GardenAnimal
    {
        [SerializeField] private Transform[] ears = System.Array.Empty<Transform>();

        [Header("Hopping")]
        [SerializeField, Range(.1f, .6f)] private float hopHeight = .30f;
        [SerializeField, Range(.3f, 1f)] private float hopLength = .65f;
        [SerializeField, Tooltip("Minimum and maximum seconds per hop.")] private Vector2 hopDurationRange = new Vector2(.62f, .82f);
        [SerializeField, Min(0f)] private float hopExcitementSpeed = .2f;
        [SerializeField, Min(0f)] private float hopExcitementHeight = .10f;
        [SerializeField, Range(.001f, .499f), Tooltip("Fraction of each hop spent crouching at each end.")] private float hopCrouchFraction = .18f;

        [Header("Body Motion")]
        [SerializeField] private float hopBodyStretch = .055f;
        [SerializeField] private float crouchBodySquash = .15f;
        [SerializeField] private float hopPitch = -9f;

        [Header("Grazing")]
        [SerializeField, Min(0f)] private float nibbleFrequency = .7f;
        [SerializeField] private float grazeNod = 18f;
        [SerializeField, Min(0f)] private float grazeNodFrequency = 4f;
        [SerializeField] private float grazeNodAmplitude = 12f;

        [Header("Limbs")]
        [SerializeField] private float rearHopLimbAngle = -38f;
        [SerializeField] private float frontHopLimbAngle = -52f;
        [SerializeField] private float frontHopLimbRecovery = 30f;

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

        private Quaternion[] earRest;
        private int hops = 1;
        /// <summary>0 to 1 through the airborne part of a single hop.</summary>
        private float flight;
        /// <summary>The height curve of one hop: 0 at each end, 1 at the top.</summary>
        private float hopArc;
        /// <summary>Compression at each end of a hop, as the bunny gathers and lands.</summary>
        private float crouch;

        protected override Activity TravelActivity => Activity.Hop;

        protected override Activity RestActivity => Activity.Graze;

        protected override void OnAwake()
        {
            earRest = new Quaternion[ears.Length];
            for (int i = 0; i < ears.Length; i++) earRest[i] = ears[i].localRotation;
        }

        /// <summary>A trip is however many hops it takes to cover the ground, not a fixed spell.</summary>
        protected override float TravelDuration(float seconds)
        {
            hops = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(From, Destination) / Mathf.Max(.001f, hopLength)));
            return hops * Range(hopDurationRange.x, hopDurationRange.y)
                / Mathf.Max(.001f, 1 + Excitement * hopExcitementSpeed);
        }

        protected override void PrepareFrame(in FramePhase frame)
        {
            float hopCycle = Mathf.Repeat(frame.T * hops, 1f);
            float crouchFraction = Mathf.Clamp(hopCrouchFraction, .001f, .499f);
            flight = Mathf.Clamp01((hopCycle - crouchFraction) / Mathf.Max(.001f, 1 - 2 * crouchFraction));
            hopArc = 4 * flight * (1 - flight);
            crouch = hopCycle < crouchFraction
                ? Mathf.Sin(hopCycle / crouchFraction * Mathf.PI)
                : hopCycle > 1 - crouchFraction
                    ? Mathf.Sin((hopCycle - (1 - crouchFraction)) / crouchFraction * Mathf.PI)
                    : 0;
        }

        /// <summary>Ground is covered one hop at a time, with the bunny still between them.</summary>
        protected override float RouteProgress(in FramePhase frame) =>
            (Mathf.Min(hops - 1, Mathf.FloorToInt(frame.T * hops)) + Mathf.SmoothStep(0, 1, flight)) / hops;

        protected override Vector3 RouteOffset(in FramePhase frame, float arc) =>
            Vector3.up * (hopArc * (hopHeight + Excitement * hopExcitementHeight));

        protected override float Bounce(in FramePhase frame, float breath) => State == Activity.Hop
            ? hopArc * hopBodyStretch - crouch * crouchBodySquash
            : breath;

        protected override float BodyPitch(in FramePhase frame) => State == Activity.Hop
            ? Mathf.Sin(flight * Mathf.PI * 2) * hopPitch
            : 0;

        protected override float HeadNod(in FramePhase frame)
        {
            if (State != Activity.Graze) return base.HeadNod(frame);
            float nibble = Mathf.Max(0, Mathf.Sin(frame.Clock * nibbleFrequency));
            return nibble * (grazeNod + Mathf.Sin(frame.Clock * grazeNodFrequency) * grazeNodAmplitude);
        }

        /// <summary>Hind legs drive the hop; forelegs reach out and gather back under the body.</summary>
        protected override void PoseLimb(int index, in FramePhase frame, out float angle, out float sweep, out float lift)
        {
            angle = 0;
            sweep = 0;
            lift = 0;
            if (State != Activity.Hop) return;
            bool rear = index % 2 == 0;
            angle = rear
                ? Mathf.Sin(flight * Mathf.PI * 2) * rearHopLimbAngle
                : hopArc * frontHopLimbAngle
                  + Mathf.Sin(flight * Mathf.PI) * flight * frontHopLimbRecovery;
        }

        /// <summary>Ears drag behind a hop and twitch on their own between times.</summary>
        protected override void AnimateAppendages(in FramePhase frame, float nod, float delta)
        {
            for (int i = 0; i < ears.Length; i++)
            {
                float drag = State == Activity.Hop
                    ? Mathf.Sin(flight * Mathf.PI * 2 - earDragPhase - i * earDragPhaseSpacing)
                      * hopArc * earDragAngle
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
    }
}
