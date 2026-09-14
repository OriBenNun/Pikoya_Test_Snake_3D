using UnityEngine;

namespace GardenSnake.Garden
{
    /// <summary>
    /// Crosses its patch in discrete hops, gathering and landing at each end of one, and grazes
    /// where it lands. Its ears drag behind the hop and twitch on their own between times.
    /// </summary>
    public sealed class GardenBunny : GardenAnimal
    {
        [SerializeField, Tooltip("The two ears, in order; the first leads the twitch.")]
        private Transform[] ears = System.Array.Empty<Transform>();

        private Quaternion[] earRest;
        private int hops = 1;
        /// <summary>0 to 1 through the airborne part of a single hop.</summary>
        private float flight;
        /// <summary>The height curve of one hop: 0 at each end, 1 at the top.</summary>
        private float hopArc;
        /// <summary>Compression at each end of a hop, as the bunny gathers and lands.</summary>
        private float crouch;

        private BunnySettings Tuning => (BunnySettings)Species;

        protected override AnimalSpeciesSettings DefaultSpecies() => ScriptableObject.CreateInstance<BunnySettings>();

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
            hops = Mathf.Max(1, Mathf.CeilToInt(
                Vector3.Distance(From, Destination) / Mathf.Max(.001f, Tuning.hopLength)));
            return hops * Range(Tuning.hopDurationRange.x, Tuning.hopDurationRange.y)
                / Mathf.Max(.001f, 1 + Excitement * Tuning.hopExcitementSpeed);
        }

        protected override void PrepareFrame(in FramePhase frame)
        {
            float hopCycle = Mathf.Repeat(frame.T * hops, 1f);
            float crouchFraction = Mathf.Clamp(Tuning.hopCrouchFraction, .001f, .499f);
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
            Vector3.up * (hopArc * (Tuning.hopHeight + Excitement * Tuning.hopExcitementHeight));

        protected override float Bounce(in FramePhase frame, float breath) => State == Activity.Hop
            ? hopArc * Tuning.hopBodyStretch - crouch * Tuning.crouchBodySquash
            : breath;

        protected override float BodyPitch(in FramePhase frame) => State == Activity.Hop
            ? Mathf.Sin(flight * Mathf.PI * 2) * Tuning.hopPitch
            : 0;

        protected override float HeadNod(in FramePhase frame)
        {
            if (State != Activity.Graze) return base.HeadNod(frame);
            float nibble = Mathf.Max(0, Mathf.Sin(frame.Clock * Tuning.nibbleFrequency));
            return nibble * (Tuning.grazeNod
                + Mathf.Sin(frame.Clock * Tuning.grazeNodFrequency) * Tuning.grazeNodAmplitude);
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
                ? Mathf.Sin(flight * Mathf.PI * 2) * Tuning.rearHopLimbAngle
                : hopArc * Tuning.frontHopLimbAngle
                  + Mathf.Sin(flight * Mathf.PI) * flight * Tuning.frontHopLimbRecovery;
        }

        /// <summary>Ears drag behind a hop and twitch on their own between times.</summary>
        protected override void AnimateAppendages(in FramePhase frame, float nod, float delta)
        {
            for (int i = 0; i < ears.Length; i++)
            {
                float drag = State == Activity.Hop
                    ? Mathf.Sin(flight * Mathf.PI * 2 - Tuning.earDragPhase - i * Tuning.earDragPhaseSpacing)
                      * hopArc * Tuning.earDragAngle
                    : 0;
                float twitch = Mathf.Pow(
                    Mathf.Max(0, Mathf.Sin(frame.Clock * Tuning.earTwitchFrequency
                        + i * Tuning.earTwitchPhaseSpacing)),
                    Mathf.Max(.001f, Tuning.earTwitchPower)) * Tuning.earTwitchAngle;
                var target = earRest[i] * Quaternion.Euler(
                    drag + nod * Tuning.earNodCompensation, 0, twitch * (i == 0 ? -1 : 1));
                ears[i].localRotation = Quaternion.Slerp(ears[i].localRotation, target,
                    1 - Mathf.Exp(-delta * Tuning.earRotationResponse));
            }
        }
    }
}
