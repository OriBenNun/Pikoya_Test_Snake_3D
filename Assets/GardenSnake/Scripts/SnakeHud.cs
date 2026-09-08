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
        [SerializeField] private RectTransform toastRoot;
        [SerializeField] private TMP_Text toast;
        [SerializeField] private Image toastHalo;
        [SerializeField] private RectTransform bannerRoot;
        [SerializeField] private TMP_Text banner;
        [SerializeField] private Image bannerFill;
        [Header("Card")]
        [SerializeField] private CanvasGroup scrimGroup;
        [SerializeField] private GameObject card;
        [SerializeField] private CanvasGroup cardGroup;
        [SerializeField] private TMP_Text cardEyebrow;
        [SerializeField] private TMP_Text cardTitle;
        [SerializeField] private TMP_Text cardBody;
        [SerializeField] private GameObject cardTally;
        [SerializeField] private TMP_Text cardTallyValue;
        [SerializeField] private Button primaryButton;
        [SerializeField] private TMP_Text primaryLabel;
        [SerializeField] private RectTransform cardKeyHint;
        [Header("Controls")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Image pauseGlyph;
        [SerializeField] private Sprite pauseSprite;
        [SerializeField] private Sprite resumeSprite;
        [SerializeField] private Button muteButton;
        [SerializeField] private Image muteGlyph;
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;

        private SnakeController controller;
        private Camera view;
        private RectTransform canvasRect;
        private Vector2 toastAnchor;
        private float toastTime;
        private float bannerTime;
        private float cardTime;
        private float hintTarget = 1;
        private RunState lastState = (RunState)(-1);
        private int shownScore = -1;
        private int shownBest = -1;
        private static readonly string[] Counts = BuildCounts();

        /// <summary>Pre-rendered numerals: the score changes every apple and never allocates.</summary>
        private static string[] BuildCounts()
        {
            var counts = new string[400];
            for (int i = 0; i < counts.Length; i++) counts[i] = i.ToString();
            return counts;
        }

        private static string Count(int value) =>
            value >= 0 && value < Counts.Length ? Counts[value] : value.ToString();

        public Transform ScoreTransform => scoreText != null ? scoreText.transform : null;

        public void Bind(SnakeController game, Camera gameCamera)
        {
            controller = game;
            view = gameCamera;
            canvasRect = (RectTransform)transform;
            primaryButton.onClick.AddListener(controller.PrimaryAction);
            pauseButton.onClick.AddListener(controller.TogglePause);
            muteButton.onClick.AddListener(controller.ToggleMute);
            foreach (Button button in new[] { primaryButton, pauseButton, muteButton })
                button.onClick.AddListener(controller.Click);
            toast.alpha = 0;
            toastHalo.color = FadeTo(toastHalo.color, 0);
            banner.alpha = 0;
            bannerFill.color = FadeTo(bannerFill.color, 0);
            Refresh();
        }

        public void Refresh()
        {
            SnakeGame game = controller.Game;
            if (shownScore != game.Score)
            {
                shownScore = game.Score;
                scoreText.text = Count(game.Score);
            }
            if (shownBest != controller.Best)
            {
                shownBest = controller.Best;
                bestText.text = "BEST " + Count(controller.Best);
            }
            muteGlyph.sprite = controller.Muted ? soundOffSprite : soundOnSprite;
            muteGlyph.color = new Color(.118f, .227f, .165f, controller.Muted ? .35f : .85f);
            pauseGlyph.sprite = game.State == RunState.Paused ? resumeSprite : pauseSprite;
            pauseButton.interactable = game.State == RunState.Playing || game.State == RunState.Paused;

            if (lastState != game.State) { cardTime = 0; lastState = game.State; }
            bool showCard = game.State != RunState.Playing;
            card.SetActive(showCard);
            bool ended = game.State == RunState.Lost || game.State == RunState.Won;
            cardTally.SetActive(ended);
            if (ended) cardTallyValue.text = Count(game.Score);
            LayoutCard(ended);
            hintTarget = game.State == RunState.Playing && game.Score > 0 ? 0 : 1;
            if (!showCard) return;
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
                    cardEyebrow.text = won ? "WHAT A HARVEST"
                        : game.Score >= controller.Best && game.Score > 0 ? "A NEW PERSONAL BEST"
                        : "ONE MORE LITTLE GO?";
                    cardTitle.text = won ? "Garden complete" : Verdict(game.Score);
                    cardBody.text = game.EndReason + "\nBest so far: " + controller.Best;
                    primaryLabel.text = "PLAY AGAIN";
                    break;
            }
        }

        /// <summary>The card is only as tall as the state needs, so it never shows an empty gap.</summary>
        private void LayoutCard(bool ended)
        {
            var rect = (RectTransform)card.transform;
            rect.sizeDelta = new Vector2(660, ended ? 448 : 372);
            cardEyebrow.rectTransform.anchoredPosition = new Vector2(0, ended ? 168 : 128);
            cardTitle.rectTransform.anchoredPosition = new Vector2(0, ended ? 116 : 74);
            ((RectTransform)cardTally.transform).anchoredPosition = new Vector2(0, 40);
            cardBody.rectTransform.anchoredPosition = new Vector2(0, ended ? -34 : 4);
            ((RectTransform)primaryButton.transform).anchoredPosition = new Vector2(0, ended ? -130 : -92);
            cardKeyHint.anchoredPosition = new Vector2(0, ended ? -186 : -146);
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
            toastRoot.anchoredPosition = toastAnchor;
        }

        public void ShowBanner(string message)
        {
            banner.text = message;
            bannerTime = 2f;
        }

        private static Color FadeTo(Color color, float alpha) =>
            new Color(color.r, color.g, color.b, alpha);

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
            float toastFade = Mathf.Min(1, toastTime * 3.2f);
            toast.alpha = toastFade;
            toastHalo.color = FadeTo(toastHalo.color, toastFade * .75f);
            Vector2 lift = toastAnchor + new Vector2(0, 64 + toastProgress * 54);
            toastRoot.anchoredPosition = new Vector2(lift.x, Mathf.Min(lift.y, canvasRect.rect.height * .5f - 70));
            toastRoot.localScale = Vector3.one * Mathf.Lerp(1.3f, .95f, Mathf.Clamp01(toastProgress * 3f));

            bannerTime = Mathf.Max(0, bannerTime - delta);
            float bannerFade = Mathf.Min(1, bannerTime * 2.4f);
            float bannerRise = Mathf.Clamp01((2f - bannerTime) * 6f);
            banner.alpha = bannerFade;
            bannerFill.color = FadeTo(bannerFill.color, bannerFade * .96f);
            bannerRoot.localScale = Vector3.one * Mathf.Lerp(.82f, 1f, 1 - Mathf.Pow(1 - bannerRise, 3));

            hintGroup.alpha = Mathf.MoveTowards(hintGroup.alpha, hintTarget, delta * 1.6f);
            brandGroup.alpha = Mathf.MoveTowards(brandGroup.alpha,
                controller != null && controller.Game.State == RunState.Playing ? .38f : 1f, delta * 1.6f);

            if (!card.activeSelf) { scrimGroup.alpha = Mathf.MoveTowards(scrimGroup.alpha, 0, delta * 6f); return; }
            cardTime += delta;
            bool ended = lastState == RunState.Lost || lastState == RunState.Won;
            // A short beat after a death lets the collision land before the card interrupts.
            float t = Mathf.Clamp01((cardTime - (ended ? .72f : 0)) / .22f);
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
