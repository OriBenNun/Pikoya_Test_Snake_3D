using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GardenSnake.Presentation.Hud
{
    /// <summary>Small, unscaled hover and press reactions that leave Button click handling intact.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class ButtonFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField, Range(1, 1.15f)] private float hoverScale = 1.035f;
        [SerializeField, Range(.8f, 1)] private float pressScale = .94f;
        [SerializeField, Min(.01f)] private float settleTime = .065f;
        [SerializeField, Min(0)] private float releaseKick = 1.2f;

        private Button button;
        private Vector3 restScale;
        private float scale = 1;
        private float velocity;
        private bool hovered;
        private bool pressed;

        private void Awake()
        {
            button = GetComponent<Button>();
            restScale = transform.localScale;
        }

        private void OnDisable()
        {
            hovered = pressed = false;
            scale = 1;
            velocity = 0;
            transform.localScale = restScale;
        }

        private void Update()
        {
            // Losing interactivity clears both flags, so rest is already the fallthrough.
            if (!button.IsInteractable()) hovered = pressed = false;
            float target = pressed ? pressScale : hovered ? hoverScale : 1;
            scale = Mathf.SmoothDamp(scale, target, ref velocity, settleTime,
                Mathf.Infinity, Mathf.Min(Time.unscaledDeltaTime, .05f));
            transform.localScale = restScale * scale;
        }

        public void OnPointerEnter(PointerEventData eventData) => hovered = true;

        public void OnPointerExit(PointerEventData eventData) => hovered = pressed = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && button.IsInteractable())
                pressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (pressed && hovered && button.IsInteractable()) velocity = releaseKick;
            pressed = false;
        }
    }
}
