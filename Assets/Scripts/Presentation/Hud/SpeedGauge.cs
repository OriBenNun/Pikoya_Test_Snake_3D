using GardenSnake.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GardenSnake.Presentation.Hud
{
    /// <summary>A layered toy speedometer drawn as a small UI mesh, with a damped spring needle.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SpeedGauge : MaskableGraphic
    {
        [SerializeField] private TMP_Text readout;
        [SerializeField] private TMP_Text caption;
        [SerializeField, Range(5, 25)] private float spring = 14;
        [Header("Needle and kick")]
        [SerializeField, Min(0)] private float damping = 1.35f;
        [SerializeField, Min(0)] private float kickThreshold = .005f;
        [SerializeField] private float kickVelocity = .45f;
        [SerializeField, Min(0)] private float needleOvershoot = .025f;
        [SerializeField, Min(0)] private float kickDecay = 3;
        [SerializeField] private Vector2 kickStretch = new Vector2(.045f, .085f);
        [Header("Pace bands")]
        [SerializeField, Range(0, 1)] private float cruisingThreshold = .22f;
        [SerializeField, Range(0, 1)] private float zippyThreshold = .62f;
        [SerializeField, Range(0, 1)] private float zoomiesThreshold = .97f;
        [SerializeField] private string idleCaption = "WIGGLE PACE";
        [SerializeField] private string cruisingCaption = "CRUISING";
        [SerializeField] private string zippyCaption = "ZIPPY";
        [SerializeField] private string zoomiesCaption = "ZOOMIES!";
        [SerializeField] private string pausedCaption = "PAUSED";
        [Header("Face colors")]
        [SerializeField] private Color ink = new Color(.1f, .23f, .16f);
        [SerializeField] private Color paper = new Color(1, .97f, .85f);
        [SerializeField] private Color mint = new Color(.39f, .72f, .35f);
        [SerializeField] private Color gold = new Color(1, .72f, .22f);
        [SerializeField] private Color coral = new Color(.95f, .31f, .17f);
        [SerializeField] private Color shadowColor = new Color(.07f, .2f, .12f, .2f);
        [SerializeField] private Color rimColor = new Color(.78f, .88f, .59f);
        [SerializeField] private Color highlightColor = Color.white;
        [SerializeField, Range(0, 1)] private float unfilledFade = .7f;
        [Header("Face geometry (canvas units)")]
        [SerializeField] private Vector2 faceOffset = new Vector2(0, 1);
        [SerializeField] private Vector2 shadowOffset = new Vector2(0, -5);
        [SerializeField] private Vector2 insetOffset = new Vector2(0, 2);
        [SerializeField, Min(0)] private float outerRadius = 58;
        [SerializeField, Min(0)] private float rimRadius = 54;
        [SerializeField, Min(0)] private float faceRadius = 50;
        [SerializeField] private Vector2 sweepAngles = new Vector2(210, -30);
        [SerializeField, Min(2)] private int paceArcCount = 30;
        [SerializeField] private Vector2 paceArcRadii = new Vector2(39, 46);
        [SerializeField] private float paceArcSweep = -6.6f;
        [SerializeField, Min(0)] private float litArcLead = .025f;
        [SerializeField, Min(1)] private int tickIntervals = 8;
        [SerializeField, Min(0)] private float majorTickInnerRadius = 29;
        [SerializeField, Min(0)] private float minorTickInnerRadius = 32;
        [SerializeField, Min(0)] private float tickOuterRadius = 35;
        [SerializeField] private float tickSweep = -2.3f;
        [SerializeField, Min(0)] private float needleRearLength = 9;
        [SerializeField, Min(0)] private float needleHalfWidth = 3.5f;
        [SerializeField, Min(0)] private float needleLength = 37;
        [SerializeField, Min(0)] private float hubRadius = 7;
        [SerializeField] private Vector2 hubHighlightOffset = new Vector2(-1, 1);
        [SerializeField, Min(0)] private float hubHighlightRadius = 3;
        [SerializeField, Min(0)] private float screwDistance = 48;
        [SerializeField, Min(0)] private float screwRadius = 2;
        [SerializeField] private Vector2 highlightRadii = new Vector2(51, 53);
        [SerializeField] private float highlightAngle = 60;
        [SerializeField] private float highlightSweep = 80;
        [Header("Mesh quality")]
        [SerializeField, Range(8, 128)] private int discSegments = 40;
        [SerializeField, Range(1, 30)] private float arcSegmentDegrees = 5;
        /// <summary>The caption bands, slowest first; Paused overrides whatever the pace is.</summary>
        private enum PaceBand { Idle, Cruising, Zippy, Zoomies, Paused }

        private GameLoopManager loop;
        private float displayed;
        private float velocity;
        private float kick;
        private float previousPace = -1;
        private float drawnDisplayed = float.NaN;
        private float drawnKick = float.NaN;
        private int shownSpeed = -1;
        private PaceBand? shownBand;
        public float DisplayedPace => displayed;
        public float TargetPace => loop == null ? 0 : loop.Pace;

        /// <summary>The HUD hands the gauge the loop it should read; it looks nothing up itself.</summary>
        public void Bind(GameLoopManager gameLoop) => loop = gameLoop;

        protected override void Start()
        {
            base.Start();
            raycastTarget = false;
        }

        private void Update()
        {
            if (loop == null) return;
            float target = loop.Pace;
            bool paused = loop.State == RunState.Paused;
            float dt = paused ? 0 : Mathf.Min(Time.unscaledDeltaTime, .033f);
            if (target > previousPace + kickThreshold && previousPace >= 0) { kick = 1; velocity += kickVelocity; }
            previousPace = target;
            velocity += ((target - displayed) * spring * spring - damping * spring * velocity) * dt;
            displayed = Mathf.Clamp(displayed + velocity * dt, -needleOvershoot, 1 + needleOvershoot);
            kick = Mathf.MoveTowards(kick, 0, dt * kickDecay);
            rectTransform.localScale = new Vector3(1 + kick * kickStretch.x, 1 + kick * kickStretch.y, 1);
            int speed = Mathf.RoundToInt(10 / loop.StepSeconds);
            if (shownSpeed != speed)
            {
                shownSpeed = speed;
                readout.SetText("{0:1}", speed / 10f);
            }
            PaceBand band = BandFor(target, paused);
            if (shownBand != band)
            {
                shownBand = band;
                caption.text = CaptionFor(band);
            }
            // The face is a full procedural mesh, so rebuilding it costs an allocation every time.
            // A settled needle draws the same face as last frame; leave it alone until it moves.
            if (dt <= 0) return;
            if (Mathf.Abs(displayed - drawnDisplayed) < .0002f && Mathf.Abs(kick - drawnKick) < .0002f) return;
            drawnDisplayed = displayed;
            drawnKick = kick;
            SetVerticesDirty();
        }

        private PaceBand BandFor(float pace, bool paused) =>
            paused ? PaceBand.Paused
            : pace > zoomiesThreshold ? PaceBand.Zoomies
            : pace > zippyThreshold ? PaceBand.Zippy
            : pace > cruisingThreshold ? PaceBand.Cruising
            : PaceBand.Idle;

        private string CaptionFor(PaceBand band) => band switch
        {
            PaceBand.Paused => pausedCaption,
            PaceBand.Zoomies => zoomiesCaption,
            PaceBand.Zippy => zippyCaption,
            PaceBand.Cruising => cruisingCaption,
            _ => idleCaption
        };

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Vector2 center = rectTransform.rect.center + faceOffset;
            Disc(vh, center + shadowOffset, outerRadius, shadowColor);
            Disc(vh, center, outerRadius, ink);
            Disc(vh, center + insetOffset, rimRadius, rimColor);
            Disc(vh, center + insetOffset, faceRadius, paper);
            for (int i = 0; i < Mathf.Max(2, paceArcCount); i++)
            {
                float t = i / (float)Mathf.Max(1, paceArcCount - 1);
                Color color = t < .5f ? Color.Lerp(mint, gold, t * 2) : Color.Lerp(gold, coral, (t - .5f) * 2);
                if (t > displayed + litArcLead) color = Color.Lerp(color, paper, unfilledFade);
                Arc(vh, center, paceArcRadii.x, paceArcRadii.y, Mathf.Lerp(sweepAngles.x, sweepAngles.y, t), paceArcSweep, color);
            }
            for (int i = 0; i <= Mathf.Max(1, tickIntervals); i++)
            {
                float a = Mathf.Lerp(sweepAngles.x, sweepAngles.y, i / (float)Mathf.Max(1, tickIntervals));
                Arc(vh, center, i % 2 == 0 ? majorTickInnerRadius : minorTickInnerRadius, tickOuterRadius, a, tickSweep, ink);
            }
            float angle = Mathf.Lerp(sweepAngles.x, sweepAngles.y, displayed) * Mathf.Deg2Rad;
            Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 side = new(-direction.y, direction.x);
            Triangle(vh, center - direction * needleRearLength + side * needleHalfWidth, center - direction * needleRearLength - side * needleHalfWidth, center + direction * needleLength, coral);
            Disc(vh, center, hubRadius, ink);
            Disc(vh, center + hubHighlightOffset, hubHighlightRadius, gold);
            // Two tiny screw heads and a specular highlight make the face read as a toy object.
            Disc(vh, center + new Vector2(-screwDistance, 0), screwRadius, paper);
            Disc(vh, center + new Vector2(screwDistance, 0), screwRadius, paper);
            Arc(vh, center + insetOffset, highlightRadii.x, highlightRadii.y, highlightAngle, highlightSweep, highlightColor);
        }

        private static void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int i = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero); vh.AddVert(b, color, Vector2.zero); vh.AddVert(c, color, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
        }

        private void Disc(VertexHelper vh, Vector2 center, float radius, Color color)
        {
            int segments = Mathf.Max(3, discSegments);
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments;
                float b = (i + 1) * Mathf.PI * 2 / segments;
                Triangle(vh, center, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                    center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, color);
            }
        }

        private void Arc(VertexHelper vh, Vector2 center, float inner, float outer, float angle, float sweep, Color color)
        {
            int pieces = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(sweep) / Mathf.Max(1, arcSegmentDegrees)));
            for (int s = 0; s < pieces; s++)
            {
                float a = (angle + sweep * s / pieces) * Mathf.Deg2Rad;
                float b = (angle + sweep * (s + 1) / pieces) * Mathf.Deg2Rad;
                Vector2 av = new(Mathf.Cos(a), Mathf.Sin(a)), bv = new(Mathf.Cos(b), Mathf.Sin(b));
                Triangle(vh, center + av * inner, center + av * outer, center + bv * outer, color);
                Triangle(vh, center + av * inner, center + bv * outer, center + bv * inner, color);
            }
        }
    }
}
