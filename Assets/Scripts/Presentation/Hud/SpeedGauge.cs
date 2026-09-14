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
        [SerializeField, Range(5, 25), Tooltip("How eagerly the needle chases the pace.")]
        private float spring = 14;
        [SerializeField, Min(0), Tooltip("How much the needle overshoots and wobbles. 0 lets it ring.")]
        private float damping = 1.35f;

        [Header("Tuning")]
        [SerializeField] private SpeedGaugeSettings dial;

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

        protected override void Awake()
        {
            base.Awake();
            // The face is populated during layout, which can come before Start.
            dial = Tuning.Or(dial);
        }

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
            if (target > previousPace + dial.kickThreshold && previousPace >= 0) { kick = 1; velocity += dial.kickVelocity; }
            previousPace = target;
            velocity += ((target - displayed) * spring * spring - damping * spring * velocity) * dt;
            displayed = Mathf.Clamp(displayed + velocity * dt, -dial.needleOvershoot, 1 + dial.needleOvershoot);
            kick = Mathf.MoveTowards(kick, 0, dt * dial.kickDecay);
            rectTransform.localScale = new Vector3(1 + kick * dial.kickStretch.x, 1 + kick * dial.kickStretch.y, 1);
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
            : pace > dial.zoomiesThreshold ? PaceBand.Zoomies
            : pace > dial.zippyThreshold ? PaceBand.Zippy
            : pace > dial.cruisingThreshold ? PaceBand.Cruising
            : PaceBand.Idle;

        private string CaptionFor(PaceBand band) => band switch
        {
            PaceBand.Paused => dial.pausedCaption,
            PaceBand.Zoomies => dial.zoomiesCaption,
            PaceBand.Zippy => dial.zippyCaption,
            PaceBand.Cruising => dial.cruisingCaption,
            _ => dial.idleCaption
        };

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Vector2 center = rectTransform.rect.center + dial.faceOffset;
            Disc(vh, center + dial.shadowOffset, dial.outerRadius, dial.shadowColor);
            Disc(vh, center, dial.outerRadius, dial.ink);
            Disc(vh, center + dial.insetOffset, dial.rimRadius, dial.rimColor);
            Disc(vh, center + dial.insetOffset, dial.faceRadius, dial.paper);
            for (int i = 0; i < dial.paceArcCount; i++)
            {
                float t = i / (float)Mathf.Max(1, dial.paceArcCount - 1);
                Color color = t < .5f ? Color.Lerp(dial.mint, dial.gold, t * 2) : Color.Lerp(dial.gold, dial.coral, (t - .5f) * 2);
                if (t > displayed + dial.litArcLead) color = Color.Lerp(color, dial.paper, dial.unfilledFade);
                Arc(vh, center, dial.paceArcRadii.x, dial.paceArcRadii.y, Mathf.Lerp(dial.sweepAngles.x, dial.sweepAngles.y, t), dial.paceArcSweep, color);
            }
            for (int i = 0; i <= dial.tickIntervals; i++)
            {
                float a = Mathf.Lerp(dial.sweepAngles.x, dial.sweepAngles.y, i / (float)dial.tickIntervals);
                Arc(vh, center, i % 2 == 0 ? dial.majorTickInnerRadius : dial.minorTickInnerRadius, dial.tickOuterRadius, a, dial.tickSweep, dial.ink);
            }
            float angle = Mathf.Lerp(dial.sweepAngles.x, dial.sweepAngles.y, displayed) * Mathf.Deg2Rad;
            Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 side = new(-direction.y, direction.x);
            Triangle(vh, center - direction * dial.needleRearLength + side * dial.needleHalfWidth, center - direction * dial.needleRearLength - side * dial.needleHalfWidth, center + direction * dial.needleLength, dial.coral);
            Disc(vh, center, dial.hubRadius, dial.ink);
            Disc(vh, center + dial.hubHighlightOffset, dial.hubHighlightRadius, dial.gold);
            // Two tiny screw heads and a specular highlight make the face read as a toy object.
            Disc(vh, center + new Vector2(-dial.screwDistance, 0), dial.screwRadius, dial.paper);
            Disc(vh, center + new Vector2(dial.screwDistance, 0), dial.screwRadius, dial.paper);
            Arc(vh, center + dial.insetOffset, dial.highlightRadii.x, dial.highlightRadii.y, dial.highlightAngle, dial.highlightSweep, dial.highlightColor);
        }

        private static void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int i = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero); vh.AddVert(b, color, Vector2.zero); vh.AddVert(c, color, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
        }

        private void Disc(VertexHelper vh, Vector2 center, float radius, Color color)
        {
            int segments = Mathf.Max(3, dial.discSegments);
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
            int pieces = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(sweep) / dial.arcSegmentDegrees));
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
