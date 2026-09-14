using UnityEngine;

namespace GardenSnake.Presentation
{
    /// <summary>Persistent Inspector tuning shared by runtime-created snake presentation components.</summary>
    [CreateAssetMenu(menuName = "Garden Snake/Snake Skin Settings")]
    public sealed class SnakeSkinSettings : ScriptableObject
    {
        [Header("Mesh quality (applied on entering Play Mode)")]
        [SerializeField, Range(8, 40)] private int sides = 20;
        public int Sides => sides;
        [SerializeField, Range(3, 24)] private int samplesPerCell = 12;
        public int SamplesPerCell => samplesPerCell;
        [Header("Materials (optional overrides, applied on entering Play Mode)")]
        [SerializeField] private Material skinMaterial = null;
        public Material SkinMaterial => skinMaterial;
        [SerializeField] private Material bellyMaterial = null;
        public Material BellyMaterial => bellyMaterial;
        [SerializeField] private Material spotMaterial = null;
        public Material SpotMaterial => spotMaterial;
        [Header("Body shape (live tuning)")]
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
        [Header("Markings (live tuning)")]
        [SerializeField, Range(0, .5f)] private float spotLength = .25f;
        public float SpotLength => spotLength;
        [SerializeField, Range(0, 1.5f)] private float spotArc = .39f;
        public float SpotArc => spotArc;
        [SerializeField, Min(0)] private float spotSurfaceOffset = .018f;
        public float SpotSurfaceOffset => spotSurfaceOffset;
        [Header("Digestion (live tuning)")]
        [SerializeField, Range(0, .6f)] private float bellyBulge = .3f;
        public float BellyBulge => bellyBulge;
        [SerializeField, Min(.05f)] private float bulgeHalfLength = .9f;
        public float BulgeHalfLength => bulgeHalfLength;
        [SerializeField, Min(.05f)] private float bulgeEntranceLength = .4f;
        public float BulgeEntranceLength => bulgeEntranceLength;
    }
}
