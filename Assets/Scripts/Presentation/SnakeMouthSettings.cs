using UnityEngine;

namespace GardenSnake.Presentation
{
    /// <summary>
    /// The face: how the mouth opens, what it does with a swallowed apple, and where every soft
    /// piece of the head sits. Edit it outside Play Mode.
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/Snake Mouth Settings")]
    public sealed class SnakeMouthSettings : ScriptableObject
    {
        [Header("Anticipation")]
        [SerializeField, Min(0)] private float anticipationDistance = 3.2f;
        public float AnticipationDistance => anticipationDistance;
        [SerializeField, Min(.01f)] private float anticipationRamp = 1.7f;
        public float AnticipationRamp => anticipationRamp;
        [SerializeField, Min(0)] private float anticipationHalfWidth = .65f;
        public float AnticipationHalfWidth => anticipationHalfWidth;
        [SerializeField, Min(0)] private float jawSpeed = 12f;
        public float JawSpeed => jawSpeed;
        [Header("Swallow")]
        [SerializeField, Range(.05f, 1)] private float swallowStepFraction = .9f;
        public float SwallowStepFraction => swallowStepFraction;
        [SerializeField] private Vector3 swallowDestination = new Vector3(0, .3f, .35f);
        public Vector3 SwallowDestination => swallowDestination;
        [SerializeField] private float appleSpin = 350f;
        public float AppleSpin => appleSpin;
        [SerializeField, Min(.1f)] private float appleShrinkPower = 2f;
        public float AppleShrinkPower => appleShrinkPower;
        [Header("Face")]
        [SerializeField, Min(0)] private float upperFaceLift = .65f;
        public float UpperFaceLift => upperFaceLift;
        [SerializeField, Range(0, 1)] private float muzzleRetraction = .28f;
        public float MuzzleRetraction => muzzleRetraction;
        [SerializeField, Min(0)] private float muzzleWidening = .3f;
        public float MuzzleWidening => muzzleWidening;
        [Header("Excited eyes")]
        [SerializeField] private Vector3 excitedEyeScale = new Vector3(1.65f, 1.8f, 1.5f);
        public Vector3 ExcitedEyeScale => excitedEyeScale;
        [SerializeField, Min(0)] private float eyeLift = .18f;
        public float EyeLift => eyeLift;
        [SerializeField, Min(0)] private float eyeSpread = .14f;
        public float EyeSpread => eyeSpread;
        [SerializeField, Min(0)] private float eyeBounce = .035f;
        public float EyeBounce => eyeBounce;
        [SerializeField, Min(0)] private float eyeBounceFrequency = 5f;
        public float EyeBounceFrequency => eyeBounceFrequency;
        [Header("Mouth interior")]
        [SerializeField] private Vector3 cavityPosition = new Vector3(0, .235f, .545f);
        public Vector3 CavityPosition => cavityPosition;
        [SerializeField] private Vector3 cavityOpeningOffset = new Vector3(0, .385f, .12f);
        public Vector3 CavityOpeningOffset => cavityOpeningOffset;
        [SerializeField] private Vector3 cavityScale = new Vector3(.48f, .027f, .065f);
        public Vector3 CavityScale => cavityScale;
        [SerializeField] private Vector3 cavityOpeningScale = new Vector3(1.32f, .923f, .715f);
        public Vector3 CavityOpeningScale => cavityOpeningScale;
        [SerializeField] private Vector3 cavityRotation = new Vector3(-18, 0, 0);
        public Vector3 CavityRotation => cavityRotation;
        [SerializeField] private Vector3 lipThickness = new Vector3(.12f, .12f, .02f);
        public Vector3 LipThickness => lipThickness;
        [SerializeField, Min(0)] private float lipInset = .065f;
        public float LipInset => lipInset;
        [Header("Lower jaw")]
        [SerializeField] private Vector3 jawPosition = new Vector3(0, .17f, .41f);
        public Vector3 JawPosition => jawPosition;
        [SerializeField] private Vector3 jawOpeningOffset = new Vector3(0, .04f, .32f);
        public Vector3 JawOpeningOffset => jawOpeningOffset;
        [SerializeField] private Vector3 jawScale = new Vector3(.64f, .12f, .32f);
        public Vector3 JawScale => jawScale;
        [SerializeField] private Vector3 jawOpeningScale = new Vector3(1.12f, .1f, .53f);
        public Vector3 JawOpeningScale => jawOpeningScale;
        [Header("Tongue")]
        [SerializeField] private Vector3 tonguePosition = new Vector3(0, .205f, .555f);
        public Vector3 TonguePosition => tonguePosition;
        [SerializeField] private Vector3 tongueOpeningOffset = new Vector3(0, .06f, .29f);
        public Vector3 TongueOpeningOffset => tongueOpeningOffset;
        [SerializeField] private Vector3 tongueScale = new Vector3(.21f, .035f, .12f);
        public Vector3 TongueScale => tongueScale;
        [SerializeField] private Vector3 tongueOpeningScale = new Vector3(.21f, .055f, .24f);
        public Vector3 TongueOpeningScale => tongueOpeningScale;
        [SerializeField, Range(0, 1)] private float tongueThreshold = .15f;
        public float TongueThreshold => tongueThreshold;
    }
}
