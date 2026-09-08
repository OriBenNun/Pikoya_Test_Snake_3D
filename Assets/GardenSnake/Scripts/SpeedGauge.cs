using GardenSnake.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GardenSnake
{
    /// <summary>A layered toy speedometer drawn as a small UI mesh, with a damped spring needle.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SpeedGauge : MaskableGraphic
    {
        [SerializeField] private TMP_Text readout;
        [SerializeField] private TMP_Text caption;
        [SerializeField, Range(5, 25)] private float spring = 14;
        private SnakeController controller;
        private float displayed, velocity, kick, previousPace = -1;
        private int shownSpeed = -1, shownBand = -1;
        public float DisplayedPace => displayed;
        public float TargetPace => controller == null ? 0 : controller.Pace;
        private static readonly Color Ink = new Color(.1f, .23f, .16f);
        private static readonly Color Paper = new Color(1, .97f, .85f);
        private static readonly Color Mint = new Color(.39f, .72f, .35f);
        private static readonly Color Gold = new Color(1, .72f, .22f);
        private static readonly Color Coral = new Color(.95f, .31f, .17f);

        protected override void Start()
        {
            base.Start();
            raycastTarget = false;
            controller = FindFirstObjectByType<SnakeController>();
        }

        private void Update()
        {
            if (controller == null || controller.Game == null) return;
            float target = controller.Pace;
            bool paused = controller.Game.State == RunState.Paused;
            float dt = paused ? 0 : Mathf.Min(Time.unscaledDeltaTime, .033f);
            if (target > previousPace + .005f && previousPace >= 0) { kick = 1; velocity += .45f; }
            previousPace = target;
            velocity += ((target - displayed) * spring * spring - 1.35f * spring * velocity) * dt;
            displayed = Mathf.Clamp(displayed + velocity * dt, -.025f, 1.025f);
            kick = Mathf.MoveTowards(kick, 0, dt * 3);
            rectTransform.localScale = new Vector3(1 + kick * .045f, 1 + kick * .085f, 1);
            int speed = Mathf.RoundToInt(10 / controller.StepSeconds);
            if (shownSpeed != speed)
            {
                shownSpeed = speed;
                readout.SetText("{0:1}", speed / 10f);
            }
            int band = paused ? 4 : target > .97f ? 3 : target > .62f ? 2 : target > .22f ? 1 : 0;
            if (shownBand != band)
            {
                shownBand = band;
                caption.text = band == 4 ? "PAUSED" : band == 3 ? "ZOOMIES!" : band == 2 ? "ZIPPY" : band == 1 ? "CRUISING" : "WIGGLE PACE";
            }
            if (dt > 0) SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Vector2 center = rectTransform.rect.center + new Vector2(0, 1);
            Disc(vh, center + new Vector2(0, -5), 58, new Color(.07f, .2f, .12f, .2f));
            Disc(vh, center, 58, Ink);
            Disc(vh, center + new Vector2(0, 2), 54, new Color(.78f, .88f, .59f));
            Disc(vh, center + new Vector2(0, 2), 50, Paper);
            for (int i = 0; i < 30; i++)
            {
                float t = i / 29f;
                Color color = t < .5f ? Color.Lerp(Mint, Gold, t * 2) : Color.Lerp(Gold, Coral, (t - .5f) * 2);
                if (t > displayed + .025f) color = Color.Lerp(color, Paper, .7f);
                Arc(vh, center, 39, 46, Mathf.Lerp(210, -30, t), -6.6f, color);
            }
            for (int i = 0; i <= 8; i++)
            {
                float a = Mathf.Lerp(210, -30, i / 8f);
                Arc(vh, center, i % 2 == 0 ? 29 : 32, 35, a, -2.3f, Ink);
            }
            float angle = Mathf.Lerp(210, -30, displayed) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 side = new Vector2(-direction.y, direction.x);
            Triangle(vh, center - direction * 9 + side * 3.5f, center - direction * 9 - side * 3.5f, center + direction * 37, Coral);
            Disc(vh, center, 7, Ink);
            Disc(vh, center + new Vector2(-1, 1), 3, Gold);
            // Two tiny screw heads and a specular highlight make the face read as a toy object.
            Disc(vh, center + new Vector2(-48, 0), 2, Paper);
            Disc(vh, center + new Vector2(48, 0), 2, Paper);
            Arc(vh, center + new Vector2(0, 2), 51, 53, 60, 80, Color.white);
        }

        private static void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int i = vh.currentVertCount;
            vh.AddVert(a, color, Vector2.zero); vh.AddVert(b, color, Vector2.zero); vh.AddVert(c, color, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
        }

        private static void Disc(VertexHelper vh, Vector2 center, float radius, Color color)
        {
            for (int i = 0; i < 40; i++)
            {
                float a = i * Mathf.PI * 2 / 40, b = (i + 1) * Mathf.PI * 2 / 40;
                Triangle(vh, center, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                    center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, color);
            }
        }

        private static void Arc(VertexHelper vh, Vector2 center, float inner, float outer, float angle, float sweep, Color color)
        {
            int pieces = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(sweep) / 5));
            for (int s = 0; s < pieces; s++)
            {
                float a = (angle + sweep * s / pieces) * Mathf.Deg2Rad;
                float b = (angle + sweep * (s + 1) / pieces) * Mathf.Deg2Rad;
                Vector2 av = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), bv = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                Triangle(vh, center + av * inner, center + av * outer, center + bv * outer, color);
                Triangle(vh, center + av * inner, center + bv * outer, center + bv * inner, color);
            }
        }
    }
}
