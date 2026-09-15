using GardenSnake.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GardenSnake.Presentation.Hud
{
    /// <summary>
    /// Presentation only. The HUD never touches the simulation; it reads the game loop and decides
    /// how loud to be, fading its own chrome out of the way as soon as a run is actually going.
    /// The toasts and banners it throws up are asked for by the feedback layer.
    /// </summary>
    public sealed class SnakeHud : MonoBehaviour
    {
        /// <summary>Pre-rendered numerals: the score changes every apple and never allocates.</summary>
        private static readonly string[] Numerals = BuildNumerals();

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
        [Header("Readouts")]
        [SerializeField] private SpeedGauge gauge;
        [Header("Controls")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Image pauseGlyph;
        [SerializeField] private Sprite pauseSprite;
        [SerializeField] private Sprite resumeSprite;
        [SerializeField] private Button muteButton;
        [SerializeField] private Image muteGlyph;
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;

        [Header("Tuning")]
        [SerializeField] private HudChromeSettings chrome;
        [SerializeField] private HudToastSettings toastStyle;
        [SerializeField] private HudCardSettings cardStyle;
        [SerializeField] private HudCopySettings copy;
        [SerializeField, Min(0), Tooltip("Beat after a run ends before the results card arrives, so the death can land.")]
        private float resultsDelay = .72f;
        [Header("Scene")]
        [SerializeField] private GameLoopManager loop;
        [SerializeField] private PlayerController input;
        [SerializeField] private Camera view;

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

        private void Refresh()
        {
            if (shownScore != loop.Score)
            {
                shownScore = loop.Score;
                scoreText.text = Numeral(loop.Score);
            }
            if (shownBest != loop.Best)
            {
                shownBest = loop.Best;
                bestText.text = copy.bestPrefix + Numeral(loop.Best);
            }
            muteGlyph.sprite = loop.Muted ? soundOffSprite : soundOnSprite;
            muteGlyph.color = WithAlpha(chrome.controlColor, loop.Muted ? chrome.mutedOpacity : chrome.soundOnOpacity);
            pauseGlyph.sprite = loop.State == RunState.Paused ? resumeSprite : pauseSprite;
            pauseButton.interactable = loop.State is RunState.Playing or RunState.Paused;

            if (shownState != loop.State)
            {
                if (loop.State == RunState.Playing && shownState != RunState.Paused)
                    toastTime = bannerTime = whisperTime = 0;
                cardTime = 0;
                shownState = loop.State;
            }
            var showCard = loop.State != RunState.Playing;
            card.SetActive(showCard);
            var ended = loop.State is RunState.Lost or RunState.Won;
            cardTally.SetActive(ended);
            if (ended) cardTallyValue.text = Numeral(loop.Score);
            LayoutCard(ended);
            if (showCard) WriteCard();
        }

        /// <summary>A number that pops where the apple was, then drifts up and fades.</summary>
        public void ShowPickup(string message, Vector3 worldPosition, bool quiet = false)
        {
            toast.text = message;
            toastQuiet = quiet;
            toast.fontSize = quiet ? toastStyle.quietFontSize : toastStyle.fontSize;
            toastDuration = quiet ? toastStyle.quietDuration : toastStyle.duration;
            toastTime = toastDuration;
            toastAnchor = ScreenAnchor(worldPosition);
            toastRoot.anchoredPosition = toastAnchor;
        }

        public void WhisperBest() => whisperTime = chrome.bestFlashDuration;

        public void ShowBanner(string message)
        {
            banner.text = message;
            bannerTime = toastStyle.bannerDuration;
        }

        private void Start()
        {
            chrome = Tuning.Or(chrome);
            toastStyle = Tuning.Or(toastStyle);
            cardStyle = Tuning.Or(cardStyle);
            copy = Tuning.Or(copy);
            canvasRect = (RectTransform)transform;
            // This visual dimmer must not swallow corner controls or board swipes.
            scrimGroup.blocksRaycasts = false;
            // Buttons are input, so they ask the input layer rather than the game directly.
            primaryButton.onClick.AddListener(input.RequestPrimary);
            pauseButton.onClick.AddListener(input.RequestPause);
            muteButton.onClick.AddListener(input.RequestMute);
            foreach (var button in new[] { primaryButton, pauseButton, muteButton })
                button.onClick.AddListener(loop.Click);
            if (gauge != null) gauge.Bind(loop);
            toast.alpha = 0;
            toastHalo.color = WithAlpha(toastHalo.color, 0);
            banner.alpha = 0;
            bannerFill.color = WithAlpha(bannerFill.color, 0);
            loop.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (loop != null) loop.Changed -= Refresh;
        }

        private void WriteCard()
        {
            (cardEyebrow.text, cardTitle.text, cardBody.text, primaryLabel.text) = loop.State switch
            {
                RunState.Ready => (copy.readyEyebrow, copy.readyTitle, copy.readyBody, copy.playLabel),
                RunState.Paused => (copy.pausedEyebrow, copy.pausedTitle, copy.pausedBody, copy.resumeLabel),
                RunState.Won => (copy.wonEyebrow, copy.wonTitle, EndSummary(), copy.replayLabel),
                _ => (loop.RecordBroken ? copy.recordEyebrow : copy.lostEyebrow,
                      Verdict(loop.Score), EndSummary(), copy.replayLabel)
            };
        }

        private string EndSummary() => loop.EndReason + "\n" + copy.bestResultPrefix + loop.Best;

        /// <summary>The card is only as tall as the state needs, so it never shows an empty gap.</summary>
        private void LayoutCard(bool ended)
        {
            var rect = (RectTransform)card.transform;
            rect.sizeDelta = new Vector2(cardStyle.width, ended ? cardStyle.heights.y : cardStyle.heights.x);
            cardEyebrow.rectTransform.anchoredPosition = new Vector2(0, ended ? cardStyle.eyebrowY.y : cardStyle.eyebrowY.x);
            cardTitle.rectTransform.anchoredPosition = new Vector2(0, ended ? cardStyle.titleY.y : cardStyle.titleY.x);
            ((RectTransform)cardTally.transform).anchoredPosition = new Vector2(0, cardStyle.tallyY);
            cardBody.rectTransform.anchoredPosition = new Vector2(0, ended ? cardStyle.bodyY.y : cardStyle.bodyY.x);
            ((RectTransform)primaryButton.transform).anchoredPosition = new Vector2(0, ended ? cardStyle.primaryButtonY.y : cardStyle.primaryButtonY.x);
            cardKeyHint.anchoredPosition = new Vector2(0, ended ? cardStyle.keyHintY.y : cardStyle.keyHintY.x);
            if (cardInstructions == null) return;
            cardInstructions.gameObject.SetActive(!ended);
            cardInstructions.anchoredPosition = new Vector2(0, cardStyle.instructionsY);
        }

        private string Verdict(int score)
        {
            if (score >= copy.magnificentScore) return copy.magnificentVerdict;
            if (score >= copy.goodScore) return copy.goodVerdict;
            if (score >= copy.decentScore) return copy.decentVerdict;
            return copy.startingVerdict;
        }

        private void Update()
        {
            // Start binds the HUD; until then there is nothing to draw.
            if (canvasRect == null) return;
            var delta = Time.unscaledDeltaTime;
            AnimateBestFlash(delta);
            AnimateToast(delta);
            AnimateBanner(delta);
            AnimateChrome(delta);
            AnimateCard(delta);
        }

        private void AnimateBestFlash(float delta)
        {
            whisperTime = Mathf.Max(0, whisperTime - delta);
            var whisper = Mathf.Sin(whisperTime / Mathf.Max(.01f, chrome.bestFlashDuration) * Mathf.PI);
            bestText.transform.localScale = Vector3.one * (1 + whisper * chrome.bestFlashScale);
            bestText.color = Color.Lerp(chrome.bestColor, chrome.bestFlashColor, whisper);
        }

        private void AnimateToast(float delta)
        {
            toastTime = Mathf.Max(0, toastTime - delta);
            var progress = 1 - toastTime / Mathf.Max(.01f, toastDuration);
            var fade = Mathf.Min(1, toastTime * toastStyle.fadeSpeed);
            toast.alpha = fade;
            toastHalo.color = WithAlpha(toastHalo.color, fade * (toastQuiet ? toastStyle.quietHaloOpacity : toastStyle.haloOpacity));
            var lift = toastAnchor + new Vector2(0, toastStyle.startHeight + progress * toastStyle.rise);
            toastRoot.anchoredPosition = new Vector2(lift.x, Mathf.Min(lift.y, canvasRect.rect.height * .5f - toastStyle.topPadding));
            toastRoot.localScale = Vector3.one * Mathf.Lerp(toastStyle.scale.x, toastStyle.scale.y, Mathf.Clamp01(progress * toastStyle.scaleSpeed));
        }

        private void AnimateBanner(float delta)
        {
            bannerTime = Mathf.Max(0, bannerTime - delta);
            var bannerFade = Mathf.Min(1, bannerTime * toastStyle.bannerFadeSpeed);
            var bannerRise = Mathf.Clamp01((toastStyle.bannerDuration - bannerTime) * toastStyle.bannerRiseSpeed);
            banner.alpha = bannerFade;
            bannerFill.color = WithAlpha(bannerFill.color, bannerFade * toastStyle.bannerFillOpacity);
            bannerRoot.localScale = Vector3.one * Mathf.Lerp(toastStyle.bannerStartScale, 1f, 1 - Mathf.Pow(1 - bannerRise, toastStyle.bannerEasePower));

        }

        private void AnimateChrome(float delta)
        {
            brandGroup.alpha = Mathf.MoveTowards(brandGroup.alpha,
                loop.State == RunState.Playing ? chrome.playingBrandOpacity : 1f, delta * chrome.chromeFadeSpeed);
        }

        private void AnimateCard(float delta)
        {
            if (!card.activeSelf)
            {
                scrimGroup.alpha = Mathf.MoveTowards(scrimGroup.alpha, 0, delta * cardStyle.fadeOutSpeed);
                return;
            }
            cardTime += delta;
            var ended = shownState is RunState.Lost or RunState.Won;
            // A short beat after a death lets the collision land before the card interrupts.
            var t = Mathf.Clamp01((cardTime - (ended ? resultsDelay : 0)) / Mathf.Max(.01f, cardStyle.fadeDuration));
            var eased = 1 - Mathf.Pow(1 - t, cardStyle.easePower);
            cardGroup.alpha = t;
            cardGroup.interactable = t >= 1;
            cardGroup.blocksRaycasts = t > 0;
            scrimGroup.alpha = Mathf.MoveTowards(scrimGroup.alpha, t * cardStyle.opacity, delta * cardStyle.fadeInSpeed);
            card.transform.localScale = Vector3.one * Mathf.Lerp(cardStyle.startScale, 1, eased);
            ((RectTransform)card.transform).anchoredPosition = new Vector2(0, Mathf.Lerp(cardStyle.startY, 0, eased));
        }

        private Vector2 ScreenAnchor(Vector3 worldPosition)
        {
            Vector2 screen = view.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local);
            return local;
        }

        private static Color WithAlpha(Color color, float alpha) => new(color.r, color.g, color.b, alpha);

        private static string[] BuildNumerals()
        {
            var numerals = new string[400];
            for (var i = 0; i < numerals.Length; i++) numerals[i] = i.ToString();
            return numerals;
        }

        private static string Numeral(int value) =>
            value >= 0 && value < Numerals.Length ? Numerals[value] : value.ToString();
    }
}
