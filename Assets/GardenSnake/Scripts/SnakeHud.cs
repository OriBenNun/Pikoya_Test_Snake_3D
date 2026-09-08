using GardenSnake.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GardenSnake
{
    /// <summary>
    /// Presentation only. The HUD never touches the simulation; it reads the controller and decides
    /// how loud to be, fading its own chrome out of the way as soon as a run is actually going.
    /// </summary>
    public sealed class SnakeHud : MonoBehaviour
    {
        [Header("Persistent")]
        [SerializeField] private CanvasGroup brandGroup;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text bestText;
        [SerializeField] private CanvasGroup hintGroup;
        [SerializeField] private TMP_Text toast;
        [SerializeField] private TMP_Text banner;
        [Header("Card")]
        [SerializeField] private CanvasGroup scrimGroup;
        [SerializeField] private GameObject card;
        [SerializeField] private CanvasGroup cardGroup;
        [SerializeField] private TMP_Text cardEyebrow;
        [SerializeField] private TMP_Text cardTitle;
        [SerializeField] private TMP_Text cardBody;
        [SerializeField] private Button primaryButton;
        [SerializeField] private TMP_Text primaryLabel;
        [Header("Controls")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Image pauseGlyph;
        [SerializeField] private Sprite pauseSprite;
        [SerializeField] private Sprite resumeSprite;
        [SerializeField] private Button muteButton;
        [SerializeField] private Image muteGlyph;
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;
        [SerializeField] private GameObject touchPad;
        [SerializeField] private Button[] directionButtons;

        private SnakeController controller;
        private Camera view;
        private RectTransform canvasRect;
        private Vector2 toastAnchor;
        private float toastTime;
        private float bannerTime;
        private float cardTime;
        private float hintTarget = 1;
        private RunState lastState = (RunState)(-1);

        public Transform ScoreTransform => scoreText != null ? scoreText.transform : null;

        public void Bind(SnakeController game, Camera gameCamera)
        {
            controller = game;
            view = gameCamera;
            canvasRect = (RectTransform)transform;
            primaryButton.onClick.AddListener(controller.PrimaryAction);
            pauseButton.onClick.AddListener(controller.TogglePause);
            muteButton.onClick.AddListener(controller.ToggleMute);
            for (int i = 0; i < directionButtons.Length; i++)
            {
                int direction = i;
                directionButtons[i].onClick.AddListener(() => controller.Turn(direction));
            }
            // Steering buttons are clutter on a machine with a keyboard, and essential without one.
            touchPad.SetActive(Application.isMobilePlatform || Input.touchSupported);
            toast.alpha = 0;
            banner.alpha = 0;
            Refresh();
        }

        public void Refresh()
        {
            SnakeGame game = controller.Game;
            scoreText.text = game.Score.ToString();
            bestText.text = "BEST " + controller.Best;
            muteGlyph.sprite = controller.Muted ? soundOffSprite : soundOnSprite;
            muteGlyph.color = new Color(1, .973f, .906f, controller.Muted ? .45f : .9f);
            pauseGlyph.sprite = game.State == RunState.Paused ? resumeSprite : pauseSprite;
            pauseButton.interactable = game.State == RunState.Playing || game.State == RunState.Paused;

            if (lastState != game.State) { cardTime = 0; lastState = game.State; }
            bool showCard = game.State != RunState.Playing;
            card.SetActive(showCard);
            hintTarget = game.State == RunState.Playing && game.Score > 0 ? 0 : 1;
            switch (game.State)
            {
                case RunState.Ready:
                    cardEyebrow.text = "A SMALL GARDEN, A BIG APPETITE";
                    cardTitle.text = "Room to grow";
                    cardBody.text = "Eat apples to grow longer.\nStay off the edges and your own tail.";
                    primaryLabel.text = "PLAY";
                    break;
                case RunState.Paused:
                    cardEyebrow.text = "TAKE A BREATHER";
                    cardTitle.text = "On a leaf break";
                    cardBody.text = "The garden will be right here\nwhenever you are ready.";
                    primaryLabel.text = "RESUME";
                    break;
                case RunState.Lost:
                case RunState.Won:
                    bool won = game.State == RunState.Won;
                    cardEyebrow.text = won ? "WHAT A HARVEST" : game.Score >= controller.Best && game.Score > 0
                        ? "A NEW PERSONAL BEST"
                        : "ONE MORE LITTLE GO?";
                    cardTitle.text = won ? "Garden complete" : Verdict(game.Score);
                    cardBody.text = game.EndReason + "\n" + game.Score + " picked   ·   best " + controller.Best;
                    primaryLabel.text = "PLAY AGAIN";
                    break;
            }
        }

        private static string Verdict(int score)
        {
            if (score >= 25) return "A magnificent run";
            if (score >= 12) return "That was a good one";
            if (score >= 5) return "A decent little run";
            return "Off to a start";
        }

        /// <summary>A number that pops where the apple was, then drifts up and fades.</summary>
        public void ShowPickup(string message, Vector3 worldPosition)
        {
            toast.text = message;
            toastTime = .95f;
            toastAnchor = ScreenAnchor(worldPosition);
        }

        public void ShowBanner(string message)
        {
            banner.text = message;
            bannerTime = 2f;
        }

        private Vector2 ScreenAnchor(Vector3 worldPosition)
        {
            Vector2 screen = view.WorldToScreenPoint(worldPosition);
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out local);
            return local;
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;

            toastTime = Mathf.Max(0, toastTime - delta);
            float toastProgress = 1 - toastTime / .95f;
            toast.alpha = Mathf.Min(1, toastTime * 3.2f);
            toast.rectTransform.anchoredPosition = toastAnchor + new Vector2(0, 26 + toastProgress * 54);
            toast.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.25f, .95f, Mathf.Clamp01(toastProgress * 3f));

            bannerTime = Mathf.Max(0, bannerTime - delta);
            banner.alpha = Mathf.Min(1, bannerTime * 2.4f);

            hintGroup.alpha = Mathf.MoveTowards(hintGroup.alpha, hintTarget, delta * 1.6f);
            brandGroup.alpha = Mathf.MoveTowards(brandGroup.alpha,
                controller != null && controller.Game.State == RunState.Playing ? .38f : 1f, delta * 1.6f);

            if (!card.activeSelf) { scrimGroup.alpha = Mathf.MoveTowards(scrimGroup.alpha, 0, delta * 6f); return; }
            cardTime += delta;
            bool ended = lastState == RunState.Lost || lastState == RunState.Won;
            // A short beat after a death lets the collision land before the card interrupts.
            float t = Mathf.Clamp01((cardTime - (ended ? .55f : 0)) / .22f);
            float eased = 1 - Mathf.Pow(1 - t, 3);
            cardGroup.alpha = t;
            cardGroup.interactable = t >= 1;
            cardGroup.blocksRaycasts = t > 0;
            scrimGroup.alpha = Mathf.MoveTowards(scrimGroup.alpha, t * .85f, delta * 4f);
            card.transform.localScale = Vector3.one * Mathf.Lerp(.94f, 1, eased);
            ((RectTransform)card.transform).anchoredPosition = new Vector2(0, Mathf.Lerp(-22, 0, eased));
        }
    }
}
