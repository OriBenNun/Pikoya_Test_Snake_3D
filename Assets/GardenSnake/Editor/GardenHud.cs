using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static GardenSnake.Editor.GardenPalette;

namespace GardenSnake.Editor
{
    /// <summary>
    /// Builds the interface: a quiet wordmark, a score cluster, two icon buttons and one card.
    /// Everything the player needs while playing is on the board itself, so the HUD keeps to the
    /// corners and fades itself down once a run is under way.
    /// </summary>
    public static class GardenHud
    {
        private static TMP_FontAsset font;

        public static void PrepareFont()
        {
            Directory.CreateDirectory(GardenBuilder.Root + "/UI/Resources");
            AssetDatabase.Refresh();
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<TMP_Settings>(),
                    GardenBuilder.Root + "/UI/Resources/TMP Settings.asset");
            string path = GardenBuilder.Root + "/UI/GardenFont.asset";
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null) { AdoptAsDefault(); return; }
            const string sourcePath = GardenBuilder.Root + "/UI/LiberationSans.ttf";
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (sourceFont == null) throw new InvalidOperationException("The included Liberation Sans font is missing.");
            font = TMP_FontAsset.CreateFontAsset(sourceFont, 64, 8,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
            font.name = "GardenFont";
            font.TryAddCharacters(new string(Enumerable.Range(32, 95).Select(c => (char)c).ToArray()) + "·");
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font, path);
            foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
            AssetDatabase.AddObjectToAsset(font.material, font);
            AssetDatabase.SaveAssets();
            AdoptAsDefault();
        }

        /// <summary>
        /// Point TMP Settings at the generated font. Without this every label warns about a
        /// missing default font in the moment between AddComponent and the font assignment.
        /// </summary>
        private static void AdoptAsDefault()
        {
            var settings = Resources.Load<TMP_Settings>("TMP Settings");
            if (settings == null) return;
            var bound = new SerializedObject(settings);
            var property = bound.FindProperty("m_defaultFontAsset");
            if (property == null || property.objectReferenceValue == font) return;
            property.objectReferenceValue = font;
            bound.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        public static SnakeHud Create()
        {
            Sprite panel = GardenSprites.RoundedRect("Panel", 96, 28);
            Sprite pill = GardenSprites.RoundedRect("Pill", 64, 30);
            Sprite circle = GardenSprites.Circle("Circle", 96);
            Sprite glow = GardenSprites.Glow("Glow", 128, 2.4f);
            Sprite edge = GardenSprites.RoundedOutline("PanelEdge", 96, 28, 2f);
            Sprite pauseIcon = GardenSprites.PauseIcon("IconPause", 64);
            Sprite playIcon = GardenSprites.PlayIcon("IconPlay", 64);
            Sprite soundOnIcon = GardenSprites.SoundIcon("IconSoundOn", 64, true);
            Sprite soundOffIcon = GardenSprites.SoundIcon("IconSoundOff", 64, false);

            var root = new GameObject("Garden HUD", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(SnakeHud));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = .5f;
            var hud = root.GetComponent<SnakeHud>();
            var bound = new SerializedObject(hud);

            // ---- wordmark -------------------------------------------------
            var brand = Anchored("Brand", root.transform, TopLeft, new Vector2(30, -20), new Vector2(330, 58));
            Fill(brand, pill, Paper.With(.94f)).type = Image.Type.Sliced;
            var brandGroup = brand.gameObject.AddComponent<CanvasGroup>();
            var wordmark = Label("Wordmark", brand, "GARDEN SNAKE", 21, TextStrong.With(.9f),
                new Vector2(18, -12), new Vector2(300, 28), TextAlignmentOptions.TopLeft, TopLeft);
            wordmark.fontStyle = FontStyles.Bold;
            wordmark.characterSpacing = 12;
            var rule = Anchored("Rule", brand, TopLeft, new Vector2(18, -44), new Vector2(40, 3));
            Fill(rule, pill, AccentDeep.With(.95f));

            // ---- score ----------------------------------------------------
            var scoreRoot = Anchored("Score", root.transform, TopRight, new Vector2(-30, -20), new Vector2(126, 90));
            Fill(scoreRoot, pill, Paper.With(.94f)).type = Image.Type.Sliced;
            var score = Label("Apples", scoreRoot, "0", 44, TextStrong,
                new Vector2(-16, -5), new Vector2(100, 54), TextAlignmentOptions.TopRight, TopRight);
            score.fontStyle = FontStyles.Bold;
            var best = Label("Best", scoreRoot, "BEST 0", 12, TextSoft,
                new Vector2(-16, -60), new Vector2(100, 20), TextAlignmentOptions.TopRight, TopRight);
            best.characterSpacing = 10;

            var gaugeRect = Anchored("Speed gauge", root.transform, new Vector2(.5f, 0), new Vector2(0, 26), new Vector2(124, 124));
            gaugeRect.pivot = new Vector2(.5f, 0);
            gaugeRect.gameObject.AddComponent<CanvasRenderer>();
            var gauge = gaugeRect.gameObject.AddComponent<SpeedGauge>();
            gauge.raycastTarget = false;
            var speedReadout = Label("Cells per second", gaugeRect, "4.0", 19, TextStrong, new Vector2(0, -19), new Vector2(64, 25));
            speedReadout.fontStyle = FontStyles.Bold;
            Label("Units", gaugeRect, "CELLS / SEC", 6.5f, TextSoft, new Vector2(0, -35), new Vector2(70, 10));
            var speedCaption = Label("Pace", gaugeRect, "WIGGLE PACE", 10, TextStrong, new Vector2(0, -70), new Vector2(150, 17));
            speedCaption.fontStyle = FontStyles.Bold;
            var gaugeSettings = new SerializedObject(gauge);
            GardenBuilder.Set(gaugeSettings, "readout", speedReadout);
            GardenBuilder.Set(gaugeSettings, "caption", speedCaption);
            gaugeSettings.ApplyModifiedPropertiesWithoutUndo();

            // ---- floating pickup number and headline ----------------------
            var toastRoot = Anchored("Pickup", root.transform, Middle, Vector2.zero, new Vector2(220, 90));
            var toastGlow = Anchored("Halo", toastRoot, Middle, Vector2.zero, new Vector2(220, 110));
            Fill(toastGlow, glow, Paper.With(.9f));
            var toast = Label("Amount", toastRoot, "", 40, AccentDeep, Vector2.zero, new Vector2(220, 60));
            toast.fontStyle = FontStyles.Bold;

            var bannerRoot = Anchored("Banner", root.transform, new Vector2(.5f, 1), new Vector2(0, -26), new Vector2(300, 46));
            var bannerFill = Fill(bannerRoot, pill, Paper);
            bannerFill.type = Image.Type.Sliced;
            bannerFill.pixelsPerUnitMultiplier = 2.4f;
            var banner = Label("Text", bannerRoot, "", 18, AccentDeep, Vector2.zero, new Vector2(300, 46));
            banner.fontStyle = FontStyles.Bold;
            banner.characterSpacing = 16;

            // ---- hint line ------------------------------------------------
            var hint = Anchored("Hints", root.transform, BottomLeft, new Vector2(46, 36), new Vector2(640, 24));
            var hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
            var hintPaper = Anchored("Paper", hint, Middle, Vector2.zero, new Vector2(668, 38));
            Fill(hintPaper, pill, Paper.With(.94f)).type = Image.Type.Sliced;
            var hintLabel = Label("Line", hint, "WASD / ARROWS OR SWIPE TO STEER   ·   P PAUSE   ·   M SOUND", 12,
                TextStrong.With(.85f), Vector2.zero, new Vector2(640, 24), TextAlignmentOptions.Left, BottomLeft);
            hintLabel.characterSpacing = 8;

            // ---- corner buttons -------------------------------------------
            var pauseButton = IconButton("Pause", root.transform, circle, pauseIcon,
                BottomRight, new Vector2(-46, 40), 44, out Image pauseGlyph);
            var muteButton = IconButton("Sound", root.transform, circle, soundOnIcon,
                BottomRight, new Vector2(-104, 40), 44, out Image muteGlyph);

            // ---- card -----------------------------------------------------
            var scrim = FullScreen("Scrim", root.transform);
            scrim.gameObject.AddComponent<Image>().color = Scrim;
            var scrimGroup = scrim.gameObject.AddComponent<CanvasGroup>();
            scrimGroup.blocksRaycasts = false;

            var card = Anchored("Run card", root.transform, Middle, Vector2.zero, new Vector2(660, 430));
            var cardShadow = Anchored("Shadow", card, Middle, new Vector2(0, -14), new Vector2(920, 640));
            Fill(cardShadow, glow, Ink.With(.34f));
            var cardFill = Fill(FullScreen("Surface", card), panel, Paper);
            cardFill.type = Image.Type.Sliced;
            cardFill.pixelsPerUnitMultiplier = 1.6f;
            cardFill.raycastTarget = true;
            var cardEdge = FullScreen("Edge", card);
            var edgeImage = Fill(cardEdge, edge, PaperEdge);
            edgeImage.type = Image.Type.Sliced;
            edgeImage.pixelsPerUnitMultiplier = 1.6f;
            var cardGroup = card.gameObject.AddComponent<CanvasGroup>();

            var eyebrow = Label("Eyebrow", card, "", 12, AccentDeep, new Vector2(0, 158), new Vector2(600, 26));
            eyebrow.characterSpacing = 14;
            eyebrow.fontStyle = FontStyles.Bold;
            var title = Label("Title", card, "", 42, TextStrong, new Vector2(0, 106), new Vector2(620, 64));
            title.fontStyle = FontStyles.Bold;

            var tally = Anchored("Tally", card, Middle, new Vector2(0, 36), new Vector2(400, 76));
            var tallyValue = Label("Value", tally, "0", 50, AccentDeep, new Vector2(0, 6), new Vector2(400, 62));
            tallyValue.fontStyle = FontStyles.Bold;
            var tallyCaption = Label("Caption", tally, "APPLES PICKED", 11, TextStrong.With(.8f),
                new Vector2(0, -30), new Vector2(400, 20));
            tallyCaption.characterSpacing = 14;

            var body = Label("Description", card, "", 18, TextStrong.With(.85f), new Vector2(0, -30), new Vector2(560, 66));
            body.textWrappingMode = TextWrappingModes.Normal;
            body.lineSpacing = 14;

            var primary = TextButton("Primary", card, pill, "PLAY", new Vector2(0, -122), new Vector2(300, 62),
                AccentDeep, Paper, out TMP_Text primaryLabel);
            var keyHint = Label("Key hint", card, "OR PRESS SPACE", 11, TextStrong.With(.7f),
                new Vector2(0, -182), new Vector2(400, 22));
            keyHint.characterSpacing = 12;

            // ---- full screen flash (driven by Feel) -----------------------
            var flash = FullScreen("Flash", root.transform);
            var flashImage = flash.gameObject.AddComponent<Image>();
            flashImage.color = new Color(1, 1, 1, 0);
            flashImage.raycastTarget = false;
            flash.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            flash.gameObject.AddComponent<MoreMountains.Feedbacks.MMFlash>();

            // ---- wiring ---------------------------------------------------
            GardenBuilder.Set(bound, "brandGroup", brandGroup);
            GardenBuilder.Set(bound, "scoreText", score);
            GardenBuilder.Set(bound, "bestText", best);
            GardenBuilder.Set(bound, "hintGroup", hintGroup);
            GardenBuilder.Set(bound, "toastRoot", toastRoot);
            GardenBuilder.Set(bound, "toast", toast);
            GardenBuilder.Set(bound, "toastHalo", toastGlow.GetComponent<Image>());
            GardenBuilder.Set(bound, "banner", banner);
            GardenBuilder.Set(bound, "bannerRoot", bannerRoot);
            GardenBuilder.Set(bound, "bannerFill", bannerFill);
            GardenBuilder.Set(bound, "scrimGroup", scrimGroup);
            GardenBuilder.Set(bound, "card", card.gameObject);
            GardenBuilder.Set(bound, "cardGroup", cardGroup);
            GardenBuilder.Set(bound, "cardEyebrow", eyebrow);
            GardenBuilder.Set(bound, "cardTitle", title);
            GardenBuilder.Set(bound, "cardBody", body);
            GardenBuilder.Set(bound, "cardTally", tally.gameObject);
            GardenBuilder.Set(bound, "cardTallyValue", tallyValue);
            GardenBuilder.Set(bound, "primaryButton", primary);
            GardenBuilder.Set(bound, "primaryLabel", primaryLabel);
            GardenBuilder.Set(bound, "cardKeyHint", keyHint.rectTransform);
            GardenBuilder.Set(bound, "pauseButton", pauseButton);
            GardenBuilder.Set(bound, "pauseGlyph", pauseGlyph);
            GardenBuilder.Set(bound, "pauseSprite", pauseIcon);
            GardenBuilder.Set(bound, "resumeSprite", playIcon);
            GardenBuilder.Set(bound, "muteButton", muteButton);
            GardenBuilder.Set(bound, "muteGlyph", muteGlyph);
            GardenBuilder.Set(bound, "soundOnSprite", soundOnIcon);
            GardenBuilder.Set(bound, "soundOffSprite", soundOffIcon);
            bound.ApplyModifiedPropertiesWithoutUndo();

            new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            return hud;
        }

        // ---------------------------------------------------------------- primitives

        private static readonly Vector2 TopLeft = new Vector2(0, 1);
        private static readonly Vector2 TopRight = new Vector2(1, 1);
        private static readonly Vector2 BottomLeft = new Vector2(0, 0);
        private static readonly Vector2 BottomRight = new Vector2(1, 0);
        private static readonly Vector2 Middle = new Vector2(.5f, .5f);

        private static RectTransform Anchored(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static RectTransform FullScreen(string name, Transform parent)
        {
            var rect = Anchored(name, parent, Middle, Vector2.zero, Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static Image Fill(RectTransform rect, Sprite sprite, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text Label(string name, Transform parent, string value, float size, Color color,
            Vector2 position, Vector2 dimensions,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center, Vector2? anchor = null)
        {
            var rect = Anchored(name, parent, anchor ?? Middle, position, dimensions);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = value;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        private static Button IconButton(string name, Transform parent, Sprite background, Sprite icon,
            Vector2 anchor, Vector2 position, float diameter, out Image glyph)
        {
            var rect = Anchored(name, parent, anchor, position, new Vector2(diameter, diameter));
            var fill = rect.gameObject.AddComponent<Image>();
            fill.sprite = background;
            fill.color = Paper.With(.94f);
            var button = rect.gameObject.AddComponent<Button>();
            Style(button, fill);
            var glyphRect = Anchored("Glyph", rect, Middle, Vector2.zero, new Vector2(diameter, diameter));
            glyph = Fill(glyphRect, icon, TextStrong.With(.85f));
            return button;
        }

        private static Button TextButton(string name, Transform parent, Sprite background, string caption,
            Vector2 position, Vector2 size, Color fillColor, Color textColor, out TMP_Text label)
        {
            var rect = Anchored(name, parent, Middle, position, size);
            var fill = rect.gameObject.AddComponent<Image>();
            fill.sprite = background;
            fill.type = Image.Type.Sliced;
            fill.pixelsPerUnitMultiplier = 2.2f;
            fill.color = fillColor;
            var button = rect.gameObject.AddComponent<Button>();
            Style(button, fill);
            label = Label("Label", rect, caption, 17, textColor, Vector2.zero, size);
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 10;
            return button;
        }

        private static void Style(Button button, Image target)
        {
            button.targetGraphic = target;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.14f, 1.14f, 1.14f, 1f);
            colors.pressedColor = new Color(.8f, .8f, .8f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = .09f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
        }
    }
}
