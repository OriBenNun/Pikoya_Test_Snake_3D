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
        [SerializeField] private RectTransform cardInstructions;
        [Header("Controls")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Image pauseGlyph;
        [SerializeField] private Sprite pauseSprite;
        [SerializeField] private Sprite resumeSprite;
        [SerializeField] private Button muteButton;
        [SerializeField] private Image muteGlyph;
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;

        [Header("Chrome appearance")]
        [SerializeField] private Color controlColor = new Color(.118f, .227f, .165f);
        [SerializeField, Range(0, 1)] private float mutedOpacity = .35f;
        [SerializeField, Range(0, 1)] private float soundOnOpacity = .85f;
        [SerializeField, Range(0, 1)] private float playingBrandOpacity = .85f;
        [SerializeField, Min(0)] private float chromeFadeSpeed = 1.6f;
        [SerializeField] private Color bestColor = new Color(.118f, .227f, .165f, .72f);
        [SerializeField] private Color bestFlashColor = new Color(.89f, .38f, .15f);
        [SerializeField, Min(.01f)] private float bestFlashDuration = .6f;
        [SerializeField, Min(0)] private float bestFlashScale = .1f;
        [Header("Pickup toast")]
        [SerializeField, Min(1)] private float pickupFontSize = 40;
        [SerializeField, Min(1)] private float quietPickupFontSize = 28;
        [SerializeField, Min(.01f)] private float pickupDuration = .95f;
        [SerializeField, Min(.01f)] private float quietPickupDuration = .7f;
        [SerializeField, Min(0)] private float toastFadeSpeed = 3.2f;
        [SerializeField, Range(0, 1)] private float toastHaloOpacity = .75f;
        [SerializeField, Range(0, 1)] private float quietToastHaloOpacity = .22f;
        [SerializeField] private float toastStartHeight = 64;
        [SerializeField] private float toastRise = 54;
        [SerializeField, Min(0)] private float toastTopPadding = 70;
        [SerializeField] private Vector2 toastScale = new Vector2(1.3f, .95f);
        [SerializeField, Min(0)] private float toastScaleSpeed = 3;
        [Header("Banner")]
        [SerializeField, Min(.01f)] private float bannerDuration = 2;
        [SerializeField, Min(0)] private float bannerFadeSpeed = 2.4f;
        [SerializeField, Min(0)] private float bannerRiseSpeed = 6;
        [SerializeField, Range(0, 1)] private float bannerFillOpacity = .96f;
        [SerializeField, Min(0)] private float bannerStartScale = .82f;
        [SerializeField, Min(.01f)] private float bannerEasePower = 3;
        [Header("Card animation")]
        [SerializeField, Min(0)] private float resultsDelay = .72f;
        [SerializeField, Min(.01f)] private float cardFadeDuration = .22f;
        [SerializeField, Min(.01f)] private float cardEasePower = 3;
        [SerializeField, Range(0, 1)] private float scrimOpacity = .85f;
        [SerializeField, Min(0)] private float scrimFadeInSpeed = 4;
        [SerializeField, Min(0)] private float scrimFadeOutSpeed = 6;
        [SerializeField, Min(0)] private float cardStartScale = .94f;
        [SerializeField] private float cardStartY = -22;
        [Header("Card layout")]
        [SerializeField, Min(1)] private float cardWidth = 660;
        [SerializeField, Tooltip("X: ready/paused height. Y: results height.")] private Vector2 cardHeights = new Vector2(520, 448);
        [SerializeField, Tooltip("X: ready/paused Y position. Y: results Y position.")] private Vector2 eyebrowY = new Vector2(202, 168);
        [SerializeField] private Vector2 titleY = new Vector2(150, 116);
        [SerializeField] private float tallyY = 40;
        [SerializeField] private Vector2 bodyY = new Vector2(80, -34);
        [SerializeField] private Vector2 primaryButtonY = new Vector2(-158, -130);
        [SerializeField] private Vector2 keyHintY = new Vector2(-213, -186);
        [SerializeField] private float instructionsY = -32;
        [Header("Copy")]
        [SerializeField] private string bestPrefix = "BEST ";
        [SerializeField] private string bestResultPrefix = "Best so far: ";
        [SerializeField] private string readyEyebrow = "A SMALL GARDEN, A BIG APPETITE";
        [SerializeField] private string readyTitle = "Room to grow";
        [SerializeField, TextArea] private string readyBody = "Eat apples to grow longer.\nStay off the edges and your own tail.";
        [SerializeField] private string playLabel = "PLAY";
        [SerializeField] private string pausedEyebrow = "TAKE A BREATHER";
        [SerializeField] private string pausedTitle = "On a leaf break";
        [SerializeField, TextArea] private string pausedBody = "The garden will be right here\nwhenever you are ready.";
        [SerializeField] private string resumeLabel = "RESUME";
        [SerializeField] private string wonEyebrow = "WHAT A HARVEST";
        [SerializeField] private string recordEyebrow = "A NEW PERSONAL BEST";
        [SerializeField] private string lostEyebrow = "ONE MORE LITTLE GO?";
        [SerializeField] private string wonTitle = "Garden complete";
        [SerializeField] private string replayLabel = "PLAY AGAIN";
        [SerializeField] private string startingVerdict = "Off to a start";
        [SerializeField] private string decentVerdict = "A decent little run";
        [SerializeField] private string goodVerdict = "That was a good one";
        [SerializeField] private string magnificentVerdict = "A magnificent run";
        [SerializeField, Min(0)] private int decentScore = 5;
        [SerializeField, Min(0)] private int goodScore = 12;
        [SerializeField, Min(0)] private int magnificentScore = 25;

        /// <summary>Pre-rendered numerals: the score changes every apple and never allocates.</summary>
        private static readonly string[] Numerals = BuildNumerals();

        private SnakeController controller;
        private Camera view;
        private RectTransform canvasRect;
        private Vector2 toastAnchor;
        private float toastTime;
        private float toastDuration = .95f;
        private bool toastQuiet;
        private float bannerTime;
        private float cardTime;
        private float whisperTime;
        private RunState? shownState;
        private int shownScore = -1;
        private int shownBest = -1;

        public Transform ScoreTransform => scoreText != null ? scoreText.transform : null;

        public void Bind(SnakeController game, Camera gameCamera)
        {
            controller = game;
            view = gameCamera;
            canvasRect = (RectTransform)transform;
            // This visual dimmer must not swallow corner controls or board swipes.
            scrimGroup.blocksRaycasts = false;
            primaryButton.onClick.AddListener(controller.PrimaryAction);
            pauseButton.onClick.AddListener(controller.TogglePause);
            muteButton.onClick.AddListener(controller.ToggleMute);
            foreach (Button button in new[] { primaryButton, pauseButton, muteButton })
                button.onClick.AddListener(controller.Click);
            toast.alpha = 0;
            toastHalo.color = WithAlpha(toastHalo.color, 0);
            banner.alpha = 0;
            bannerFill.color = WithAlpha(bannerFill.color, 0);
            Refresh();
        }

        public void Refresh()
        {
            SnakeGame game = controller.Game;
            if (shownScore != game.Score)
            {
                shownScore = game.Score;
                scoreText.text = Numeral(game.Score);
            }
            if (shownBest != controller.Best)
            {
                shownBest = controller.Best;
                bestText.text = bestPrefix + Numeral(controller.Best);
            }
            muteGlyph.sprite = controller.Muted ? soundOffSprite : soundOnSprite;
            muteGlyph.color = WithAlpha(controlColor, controller.Muted ? mutedOpacity : soundOnOpacity);
            pauseGlyph.sprite = game.State == RunState.Paused ? resumeSprite : pauseSprite;
            pauseButton.interactable = game.State is RunState.Playing or RunState.Paused;

            if (shownState != game.State)
            {
                if (game.State == RunState.Playing && shownState != RunState.Paused)
                    toastTime = bannerTime = whisperTime = 0;
                cardTime = 0;
                shownState = game.State;
            }
            bool showCard = game.State != RunState.Playing;
            card.SetActive(showCard);
            bool ended = game.State is RunState.Lost or RunState.Won;
            cardTally.SetActive(ended);
            if (ended) cardTallyValue.text = Numeral(game.Score);
            LayoutCard(ended);
            if (showCard) WriteCard(game);
        }

        private void WriteCard(SnakeGame game)
        {
            (cardEyebrow.text, cardTitle.text, cardBody.text, primaryLabel.text) = game.State switch
            {
                RunState.Ready => (readyEyebrow, readyTitle, readyBody, playLabel),
                RunState.Paused => (pausedEyebrow, pausedTitle, pausedBody, resumeLabel),
                RunState.Won => (wonEyebrow, wonTitle, EndSummary(game), replayLabel),
                _ => (controller.Record.Broken ? recordEyebrow : lostEyebrow,
                      Verdict(game.Score), EndSummary(game), replayLabel)
            };
        }

        private string EndSummary(SnakeGame game) => game.EndReason + "\n" + bestResultPrefix + controller.Best;

        /// <summary>The card is only as tall as the state needs, so it never shows an empty gap.</summary>
        private void LayoutCard(bool ended)
        {
            var rect = (RectTransform)card.transform;
            rect.sizeDelta = new Vector2(cardWidth, ended ? cardHeights.y : cardHeights.x);
            cardEyebrow.rectTransform.anchoredPosition = new Vector2(0, ended ? eyebrowY.y : eyebrowY.x);
            cardTitle.rectTransform.anchoredPosition = new Vector2(0, ended ? titleY.y : titleY.x);
            ((RectTransform)cardTally.transform).anchoredPosition = new Vector2(0, tallyY);
            cardBody.rectTransform.anchoredPosition = new Vector2(0, ended ? bodyY.y : bodyY.x);
            ((RectTransform)primaryButton.transform).anchoredPosition = new Vector2(0, ended ? primaryButtonY.y : primaryButtonY.x);
            cardKeyHint.anchoredPosition = new Vector2(0, ended ? keyHintY.y : keyHintY.x);
            if (cardInstructions == null) return;
            cardInstructions.gameObject.SetActive(!ended);
            cardInstructions.anchoredPosition = new Vector2(0, instructionsY);
        }

        private string Verdict(int score)
        {
            if (score >= magnificentScore) return magnificentVerdict;
            if (score >= goodScore) return goodVerdict;
            if (score >= decentScore) return decentVerdict;
            return startingVerdict;
        }

        /// <summary>A number that pops where the apple was, then drifts up and fades.</summary>
        public void ShowPickup(string message, Vector3 worldPosition, bool quiet = false)
        {
            toast.text = message;
            toastQuiet = quiet;
            toast.fontSize = quiet ? quietPickupFontSize : pickupFontSize;
            toastDuration = quiet ? quietPickupDuration : pickupDuration;
            toastTime = toastDuration;
            toastAnchor = ScreenAnchor(worldPosition);
            toastRoot.anchoredPosition = toastAnchor;
        }

        public void WhisperBest() => whisperTime = bestFlashDuration;

        public void ShowBanner(string message)
        {
            banner.text = message;
            bannerTime = bannerDuration;
        }

        private void Update()
        {
            // The controller wires the HUD up in its Awake; until then there is nothing to draw.
            if (controller == null) return;
            float delta = Time.unscaledDeltaTime;
            AnimateBestFlash(delta);
            AnimateToast(delta);
            AnimateBanner(delta);
            brandGroup.alpha = Mathf.MoveTowards(brandGroup.alpha,
                controller.Game.State == RunState.Playing ? playingBrandOpacity : 1f, delta * chromeFadeSpeed);
            AnimateCard(delta);
        }

        private void AnimateBestFlash(float delta)
        {
            whisperTime = Mathf.Max(0, whisperTime - delta);
            float whisper = Mathf.Sin(whisperTime / Mathf.Max(.01f, bestFlashDuration) * Mathf.PI);
            bestText.transform.localScale = Vector3.one * (1 + whisper * bestFlashScale);
            bestText.color = Color.Lerp(bestColor, bestFlashColor, whisper);
        }

        private void AnimateToast(float delta)
        {
            toastTime = Mathf.Max(0, toastTime - delta);
            float progress = 1 - toastTime / Mathf.Max(.01f, toastDuration);
            float fade = Mathf.Min(1, toastTime * toastFadeSpeed);
            toast.alpha = fade;
            toastHalo.color = WithAlpha(toastHalo.color, fade * (toastQuiet ? quietToastHaloOpacity : toastHaloOpacity));
            Vector2 lift = toastAnchor + new Vector2(0, toastStartHeight + progress * toastRise);
            toastRoot.anchoredPosition = new Vector2(lift.x, Mathf.Min(lift.y, canvasRect.rect.height * .5f - toastTopPadding));
            toastRoot.localScale = Vector3.one * Mathf.Lerp(toastScale.x, toastScale.y, Mathf.Clamp01(progress * toastScaleSpeed));
        }

        private void AnimateBanner(float delta)
        {
            bannerTime = Mathf.Max(0, bannerTime - delta);
            float fade = Mathf.Min(1, bannerTime * bannerFadeSpeed);
            float rise = Mathf.Clamp01((bannerDuration - bannerTime) * bannerRiseSpeed);
            banner.alpha = fade;
            bannerFill.color = WithAlpha(bannerFill.color, fade * bannerFillOpacity);
            bannerRoot.localScale = Vector3.one * Mathf.Lerp(bannerStartScale, 1f, 1 - Mathf.Pow(1 - rise, bannerEasePower));
        }

        private void AnimateCard(float delta)
        {
            if (!card.activeSelf)
            {
                scrimGroup.alpha = Mathf.MoveTowards(scrimGroup.alpha, 0, delta * scrimFadeOutSpeed);
                return;
            }
            cardTime += delta;
            bool ended = shownState is RunState.Lost or RunState.Won;
            // A short beat after a death lets the collision land before the card interrupts.
            float t = Mathf.Clamp01((cardTime - (ended ? resultsDelay : 0)) / Mathf.Max(.01f, cardFadeDuration));
            float eased = 1 - Mathf.Pow(1 - t, cardEasePower);
            cardGroup.alpha = t;
            cardGroup.interactable = t >= 1;
            cardGroup.blocksRaycasts = t > 0;
            scrimGroup.alpha = Mathf.MoveTowards(scrimGroup.alpha, t * scrimOpacity, delta * scrimFadeInSpeed);
            card.transform.localScale = Vector3.one * Mathf.Lerp(cardStartScale, 1, eased);
            ((RectTransform)card.transform).anchoredPosition = new Vector2(0, Mathf.Lerp(cardStartY, 0, eased));
        }

        private Vector2 ScreenAnchor(Vector3 worldPosition)
        {
            Vector2 screen = view.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
            return local;
        }

        private static Color WithAlpha(Color color, float alpha) => new(color.r, color.g, color.b, alpha);

        private static string[] BuildNumerals()
        {
            var numerals = new string[400];
            for (int i = 0; i < numerals.Length; i++) numerals[i] = i.ToString();
            return numerals;
        }

        private static string Numeral(int value) =>
            value >= 0 && value < Numerals.Length ? Numerals[value] : value.ToString();
    }
}
