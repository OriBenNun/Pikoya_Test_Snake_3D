using UnityEngine;

namespace GardenSnake
{
    /// <summary>
    /// Frames the whole garden between the two HUD bands at whatever aspect the window happens to
    /// be, then eases towards that frame: pushed in while a run is going, sitting back on the
    /// menus and after a death.
    /// </summary>
    [DefaultExecutionOrder(-110)]
    public sealed class GameCameraRig : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private Camera view;
        [SerializeField] private GameLoopManager loop;
        [Header("Framing")]
        [SerializeField, Range(.5f, 4f)] private float edgePadding = 1f;
        [SerializeField, Range(0f, .3f)] private float hudBandTop = .105f;
        [SerializeField, Range(0f, .3f)] private float hudBandBottom = .15f;
        [SerializeField, Range(1f, 1.4f)] private float restingZoom = 1.13f;
        [SerializeField, Range(.5f, 8f)] private float zoomSpeed = 3.4f;

        private Vector3 home;
        private float lastAspect;
        private float fitSize;
        private float viewSize;

        private void Start()
        {
            home = view.transform.localPosition;
            FrameBoard();
        }

        private void Update()
        {
            if (!Mathf.Approximately(lastAspect, view.aspect)) FrameBoard();
            float target = fitSize * (loop.State == RunState.Playing ? 1f : restingZoom);
            viewSize = Mathf.Lerp(viewSize, target, 1 - Mathf.Exp(-zoomSpeed * Time.unscaledDeltaTime));
            view.orthographicSize = viewSize;
            // Slide the view so the leftover space splits into the two bands we asked for.
            float shift = (hudBandTop - hudBandBottom) * viewSize;
            view.transform.localPosition = home + view.transform.localRotation * (Vector3.up * shift);
        }

        /// <summary>
        /// Works out the orthographic size that fits the whole garden between the HUD bands at
        /// the current aspect. The result is a target; the camera eases towards it.
        /// </summary>
        private void FrameBoard()
        {
            lastAspect = view.aspect;
            float halfX = loop.BoardWidth * .5f + edgePadding;
            float halfZ = loop.BoardHeight * .5f + edgePadding;
            float maxX = 0;
            float maxY = 0;
            for (int corner = 0; corner < 4; corner++)
            {
                var point = new Vector3(corner < 2 ? -halfX : halfX, 0, corner % 2 == 0 ? -halfZ : halfZ);
                Vector3 local = view.transform.InverseTransformPoint(point);
                maxX = Mathf.Max(maxX, Mathf.Abs(local.x));
                maxY = Mathf.Max(maxY, Mathf.Abs(local.y));
            }
            float band = Mathf.Clamp01(1 - hudBandTop - hudBandBottom);
            fitSize = Mathf.Max(maxY / band, maxX / Mathf.Max(.1f, view.aspect));
            if (viewSize <= 0) viewSize = fitSize * restingZoom;
        }
    }
}
