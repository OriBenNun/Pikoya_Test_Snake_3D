using System;
using GardenSnake.Gameplay;
using UnityEngine;

namespace GardenSnake.Presentation
{
    /// <summary>
    /// Keeps the whole board in frame whatever shape the window is. The scene is authored for a
    /// wide desktop window; anything squarer - a resized browser, a phone held in landscape with
    /// the address bar up - would crop the fence off the sides, so the camera pulls back by
    /// exactly as much as the window is missing and never sits tighter than the authored framing.
    /// <para>
    /// It owns the framing, not the zoom. <c>FeedbackManager</c> eases between playing and resting
    /// sizes on top of <see cref="Size"/>; when nothing is listening this applies the size itself,
    /// so the framing survives the reaction layer being switched off.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-300)]
    [RequireComponent(typeof(Camera))]
    public sealed class GardenCamera : MonoBehaviour
    {
        [SerializeField, Range(0f, 6f), Tooltip("Cells of headroom kept around the board, so the fence and its shadow stay in frame.")]
        private float margin = 1.5f;

        private Camera view;
        /// <summary>The framing the scene was authored at. The window may loosen it, never tighten it.</summary>
        private float authoredSize;
        private int width;
        private int height;

        /// <summary>The orthographic size a run should be played at, once the window has had its say.</summary>
        public float Size { get; private set; }

        /// <summary>Raised when the window changed shape and <see cref="Size"/> moved with it.</summary>
        public event Action SizeChanged;

        private void Awake()
        {
            view = GetComponent<Camera>();
            authoredSize = view.orthographicSize;
            Fit();
            view.orthographicSize = Size;
        }

        private void Update()
        {
            if (Screen.width == width && Screen.height == height) return;
            float was = Size;
            Fit();
            if (Mathf.Approximately(was, Size)) return;
            if (SizeChanged != null) SizeChanged();
            else view.orthographicSize = Size;
        }

        /// <summary>
        /// The board's four corners, measured from where the camera stands. Measuring beats
        /// trigonometry on the tilt: whatever the camera is doing, this is what it can see.
        /// </summary>
        private void Fit()
        {
            width = Screen.width;
            height = Screen.height;
            float halfX = (GameLoopManager.Columns + margin) * .5f;
            float halfZ = (GameLoopManager.Rows + margin) * .5f;
            float needX = 0f;
            float needY = 0f;
            for (int corner = 0; corner < 4; corner++)
            {
                var point = new Vector3(corner < 2 ? -halfX : halfX, 0f, corner % 2 == 0 ? -halfZ : halfZ);
                var seen = transform.InverseTransformPoint(point);
                needX = Mathf.Max(needX, Mathf.Abs(seen.x));
                needY = Mathf.Max(needY, Mathf.Abs(seen.y));
            }
            float aspect = height > 0 ? (float)width / height : view.aspect;
            Size = Mathf.Max(authoredSize, needY, needX / Mathf.Max(aspect, .1f));
        }
    }
}
