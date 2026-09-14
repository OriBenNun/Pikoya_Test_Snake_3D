using UnityEngine;

namespace GardenSnake.Presentation
{
    /// <summary>
    /// How the snake is shaped and drawn: the one asset an art director works in. Edit it outside
    /// Play Mode; the game reads it when it starts.
    /// </summary>
    [CreateAssetMenu(menuName = "Garden Snake/Snake Skin Settings")]
    public sealed class SnakeSkinSettings : ScriptableObject
    {
        [Header("Proportions")]
        [SerializeField, Range(.8f, 1.6f), Tooltip("How big the head is drawn against the body.")]
        private float headScale = 1.22f;
        public float HeadScale => headScale;
        [SerializeField, Range(.8f, 1.6f), Tooltip("How thick the body reads along its length.")]
        private float bodyScale = 1.3f;
        public float BodyScale => bodyScale;
        [SerializeField, Range(.8f, 1.6f), Tooltip("How much of that thickness the tail tip keeps.")]
        private float tailScale = 1.24f;
        public float TailScale => tailScale;
        [Header("Mesh quality")]
        [SerializeField, Range(8, 40)] private int sides = 20;
        public int Sides => sides;
        [SerializeField, Range(3, 24)] private int samplesPerCell = 12;
        public int SamplesPerCell => samplesPerCell;
        [Header("Materials (optional overrides)")]
        [SerializeField] private Material skinMaterial = null;
        public Material SkinMaterial => skinMaterial;
        [SerializeField] private Material bellyMaterial = null;
        public Material BellyMaterial => bellyMaterial;
        [SerializeField] private Material spotMaterial = null;
        public Material SpotMaterial => spotMaterial;
        [Header("Body shape")]
        [SerializeField, Min(.01f)] private float radius = .335f;
        public float Radius => radius;
        [SerializeField, Min(.01f)] private float height = .235f;
        public float Height => height;
        [SerializeField, Min(0)] private float centerHeight = .255f;
        public float CenterHeight => centerHeight;
        [SerializeField, Min(0)] private float tailTipLength = .42f;
        public float TailTipLength => tailTipLength;
        [SerializeField, Range(0, 1)] private float neckWidth = .83f;
        public float NeckWidth => neckWidth;
        [SerializeField, Range(0, 1)] private float tailWidth = .58f;
        public float TailWidth => tailWidth;
        [SerializeField, Range(0, .5f)] private float curveTension = .4f;
        public float CurveTension => curveTension;
        [SerializeField, Range(0, 1)] private float bellyArc = .3f;
        public float BellyArc => bellyArc;
        [SerializeField, Range(0, .2f), Tooltip("Soft rounded bump on each body segment.")]
        private float segmentBump = .075f;
        public float SegmentBump => segmentBump;
        [Header("Markings")]
        [SerializeField, Range(0, .5f)] private float spotLength = .25f;
        public float SpotLength => spotLength;
        [SerializeField, Range(0, 1.5f)] private float spotArc = .39f;
        public float SpotArc => spotArc;
        [SerializeField, Min(0)] private float spotSurfaceOffset = .018f;
        public float SpotSurfaceOffset => spotSurfaceOffset;
        [Header("Digestion")]
        [SerializeField, Range(0, .6f)] private float bellyBulge = .3f;
        public float BellyBulge => bellyBulge;
        [SerializeField, Min(.05f)] private float bulgeHalfLength = .9f;
        public float BulgeHalfLength => bulgeHalfLength;
        [SerializeField, Min(.05f)] private float bulgeEntranceLength = .4f;
        public float BulgeEntranceLength => bulgeEntranceLength;
    }
}
