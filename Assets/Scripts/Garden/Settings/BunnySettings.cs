using UnityEngine;

namespace GardenSnake.Garden
{
    /// <summary>
    /// The bunny: it crosses its patch in discrete hops, gathering and landing at each end of one,
    /// grazes where it lands, and its ears drag behind the hop and twitch between times.
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/Wildlife/Bunny")]
    public sealed class BunnySettings : AnimalSpeciesSettings
    {
        [Header("Hopping")]
        [Range(.1f, .6f)] public float hopHeight = .30f;
        [Range(.3f, 1f), Tooltip("World units one hop covers; a trip is however many it takes.")]
        public float hopLength = .65f;
        [Tooltip("Minimum and maximum seconds per hop.")]
        public Vector2 hopDurationRange = new(.62f, .82f);
        [Min(0f)] public float hopExcitementSpeed = .2f;
        [Min(0f), Tooltip("How much higher an excited bunny hops.")]
        public float hopExcitementHeight = .10f;
        [Range(.001f, .499f), Tooltip("Fraction of each hop spent crouching at each end.")]
        public float hopCrouchFraction = .18f;

        [Header("Body")]
        [Tooltip("How far the body stretches at the top of a hop.")]
        public float hopBodyStretch = .055f;
        [Tooltip("And squashes as it gathers and lands.")]
        public float crouchBodySquash = .15f;
        public float hopPitch = -9f;

        [Header("Grazing")]
        [Min(0f), Tooltip("How often it puts its head down to the grass.")]
        public float nibbleFrequency = .7f;
        [Tooltip("Degrees the head goes down to graze.")]
        public float grazeNod = 18f;
        [Min(0f)] public float grazeNodFrequency = 4f;
        [Tooltip("Degrees of nibbling on top of that.")]
        public float grazeNodAmplitude = 12f;

        [Header("Limbs")]
        [Tooltip("Degrees the hind legs drive through.")]
        public float rearHopLimbAngle = -38f;
        [Tooltip("Degrees the forelegs reach out.")]
        public float frontHopLimbAngle = -52f;
        [Tooltip("And how far they gather back under the body.")]
        public float frontHopLimbRecovery = 30f;

        [Header("Ears")]
        [Tooltip("How far the ears lag behind the hop.")]
        public float earDragPhase = .65f;
        [Tooltip("And how far the second ear lags behind the first.")]
        public float earDragPhaseSpacing = .18f;
        public float earDragAngle = 22f;
        [Min(0f), Tooltip("How often an ear twitches on its own.")]
        public float earTwitchFrequency = .83f;
        public float earTwitchPhaseSpacing = 2.6f;
        [Min(.001f), Tooltip("How sharp each twitch is. Higher is flickier.")]
        public float earTwitchPower = 18f;
        public float earTwitchAngle = 13f;
        [Tooltip("How much of a head nod the ears take back out again.")]
        public float earNodCompensation = -.35f;
        [Min(0f)] public float earRotationResponse = 13f;
    }
}
