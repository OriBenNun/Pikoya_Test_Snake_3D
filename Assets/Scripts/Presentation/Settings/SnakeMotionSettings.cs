using UnityEngine;

namespace GardenSnake.Presentation
{
    /// <summary>
    /// How the animal carries itself: the lean into a corner, the slither that sells "alive", the
    /// swell of a new body part, the breathing while it waits, the blink, and the way it gives up.
    /// Edit outside Play Mode.
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/Snake Motion")]
    public sealed class SnakeMotionSettings : ScriptableObject
    {
        [Header("Banking")]
        [Min(0), Tooltip("How quickly the head comes back up out of a corner.")]
        public float bankRecoverySpeed = 3.4f;
        [Tooltip("Degrees the head rolls into a turn.")]
        public float bankAngle = -16f;
        [Min(0), Tooltip("How sharply the head snaps onto a new heading.")]
        public float headTurnSpeed = 22f;

        [Header("Slither")]
        [Min(0), Tooltip("How far the body waves sideways within its cell.")]
        public float amplitude = .055f;
        [Min(0), Tooltip("Waves per movement step.")]
        public float frequency = 2.1f;
        [Tooltip("How far the wave lags from one body part to the next.")]
        public float segmentPhase = .42f;

        [Header("Growth")]
        [Min(.01f), Tooltip("Seconds a new body part takes to swell into place.")]
        public float duration = .2f;
        [Min(0), Tooltip("How far it overshoots on the way.")]
        public float swell = .2f;

        [Header("Idle breathing")]
        [Min(0)] public float breathFrequency = 2.4f;
        [Tooltip("How far the breath lags from one body part to the next.")]
        public float breathSegmentPhase = .5f;
        [Min(0)] public float breathAmplitude = .035f;
        [Min(0), Tooltip("How much of the breath goes into stretching upward rather than widening.")]
        public float breathStretch = 2.2f;
        [Min(0), Tooltip("How far the breath lifts each part off the grass.")]
        public float breathLift = .5f;

        [Header("Blinking")]
        [Min(0), Tooltip("Seconds before the first blink of a run.")]
        public float firstBlinkDelay = 2.5f;
        [Min(0)] public float minimumBlinkGap = 2.2f;
        [Min(.01f)] public float maximumBlinkGap = 5.5f;
        [Min(.01f)] public float blinkDuration = .16f;
        [Range(0, 1), Tooltip("How far the eyes close at the bottom of a blink.")]
        public float blinkClosure = .92f;

        [Header("Death")]
        [Min(.01f), Tooltip("Seconds one body part takes to swell and pop out of existence.")]
        public float beat = .26f;
        [Min(.01f), Tooltip("Seconds the head spends shoving into whatever stopped it.")]
        public float recoilDuration = .1f;
        [Min(0)] public float recoilDistance = .18f;
        [Min(0), Tooltip("Seconds between one part popping and the next.")]
        public float segmentDelay = .05f;
        [Min(0), Tooltip("Longest the whole body may take, however long it is.")]
        public float totalStagger = .34f;
        [Min(0)] public float deathSwell = .35f;
        [Range(0, 1), Tooltip("How far through a part's pop it starts shrinking.")]
        public float shrinkStart = .55f;
        [Min(0), Tooltip("How far each part rises as it goes.")]
        public float lift = .22f;
    }
}
