using UnityEngine;

namespace GardenSnake.Presentation.Hud
{
    /// <summary>
    /// The pace dial: how the needle behaves, what it calls each stretch of the pace, and the face
    /// it is drawn on, in canvas units. Edit outside Play Mode.
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/Speed Gauge")]
    public sealed class SpeedGaugeSettings : ScriptableObject
    {
        [Header("Kick")]
        [Min(0), Tooltip("Pace change that counts as a step up and kicks the dial.")]
        public float kickThreshold = .005f;
        [Tooltip("How hard that step throws the needle.")]
        public float kickVelocity = .45f;
        [Min(0), Tooltip("How far past each end of the dial the needle may swing.")]
        public float needleOvershoot = .025f;
        [Min(0)] public float kickDecay = 3;
        [Tooltip("How far the whole dial stretches on a kick.")]
        public Vector2 kickStretch = new(.045f, .085f);

        [Header("Pace bands")]
        [Range(0, 1)] public float cruisingThreshold = .22f;
        [Range(0, 1)] public float zippyThreshold = .62f;
        [Range(0, 1)] public float zoomiesThreshold = .97f;
        public string idleCaption = "WIGGLE PACE";
        public string cruisingCaption = "CRUISING";
        public string zippyCaption = "ZIPPY";
        public string zoomiesCaption = "ZOOMIES!";
        public string pausedCaption = "PAUSED";

        [Header("Face colours")]
        public Color ink = new(.1f, .23f, .16f);
        public Color paper = new(1, .97f, .85f);
        public Color mint = new(.39f, .72f, .35f);
        public Color gold = new(1, .72f, .22f);
        public Color coral = new(.95f, .31f, .17f);
        public Color shadowColor = new(.07f, .2f, .12f, .2f);
        public Color rimColor = new(.78f, .88f, .59f);
        public Color highlightColor = Color.white;
        [Range(0, 1), Tooltip("How far the arcs beyond the needle wash out.")]
        public float unfilledFade = .7f;

        [Header("Face geometry")]
        public Vector2 faceOffset = new(0, 1);
        public Vector2 shadowOffset = new(0, -5);
        public Vector2 insetOffset = new(0, 2);
        [Min(0)] public float outerRadius = 58;
        [Min(0)] public float rimRadius = 54;
        [Min(0)] public float faceRadius = 50;
        [Tooltip("Degrees the needle sweeps between, empty to full.")]
        public Vector2 sweepAngles = new(210, -30);
        [Min(2)] public int paceArcCount = 30;
        public Vector2 paceArcRadii = new(39, 46);
        public float paceArcSweep = -6.6f;
        [Min(0), Tooltip("How far ahead of the needle the arcs stay lit.")]
        public float litArcLead = .025f;
        [Min(1)] public int tickIntervals = 8;
        [Min(0)] public float majorTickInnerRadius = 29;
        [Min(0)] public float minorTickInnerRadius = 32;
        [Min(0)] public float tickOuterRadius = 35;
        public float tickSweep = -2.3f;
        [Min(0)] public float needleRearLength = 9;
        [Min(0)] public float needleHalfWidth = 3.5f;
        [Min(0)] public float needleLength = 37;
        [Min(0)] public float hubRadius = 7;
        public Vector2 hubHighlightOffset = new(-1, 1);
        [Min(0)] public float hubHighlightRadius = 3;
        [Min(0), Tooltip("How far out the two screw heads sit.")]
        public float screwDistance = 48;
        [Min(0)] public float screwRadius = 2;
        public Vector2 highlightRadii = new(51, 53);
        public float highlightAngle = 60;
        public float highlightSweep = 80;

        [Header("Mesh quality")]
        [Tooltip("Sides on each circle. Raise this when the dial is scaled up, or the rim reads as a polygon.")]
        [Range(8, 128)] public int discSegments = 96;
        [Tooltip("Degrees per arc slice. Lower is smoother.")]
        [Range(1, 30)] public float arcSegmentDegrees = 2;
    }
}
