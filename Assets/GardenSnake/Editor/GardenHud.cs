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
    /// Everything else the player needs is on the board itself, so the HUD keeps to the corners
    /// and leaves the garden unobstructed while a run is going.
    /// </summary>
    public static class GardenHud
    {
        private const int Reference = 1600;
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
            if (font != null) return;
            const string sourcePath = GardenBuilder.Root + "/UI/LiberationSans.ttf";
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (sourceFont == null) throw new InvalidOperationException("The included Liberation Sans font is missing.");
            font = TMP_FontAsset.CreateFontAsset(sourceFont, 64, 8,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
            font.name = "GardenFont";
            font.TryAddCharacters(new string(Enumerable.Range(32, 95).Select(c => (char)c).ToArray()) + "·—");
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font, path);
            foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
            AssetDatabase.AddObjectToAsset(font.material, font);
            AssetDatabase.SaveAssets();
        }

        public static SnakeHud Create()
        {
            Sprite panel = GardenSprites.RoundedRect("Panel", 96, 28);
            Sprite pill = GardenSprites.RoundedRect("Pill", 64, 30);
            Sprite circle = GardenSprites.Circle("Circle", 96);
            Sprite glow = GardenSprites.Glow("Glow", 128, 2.4f);
            Sprite pauseIcon = GardenSprites.PauseIcon("IconPause", 64);
            Sprite playIcon = GardenSprites.PlayIcon("IconPlay", 64);
            Sprite soundOnIcon = GardenSprites.SoundIcon("IconSoundOn", 64, true);
            Sprite soundOffIcon = GardenSprites.SoundIcon("IconSoundOff", 64, false);
            Sprite chevron = GardenSprites.ChevronIcon("IconChevron", 64);

            var root = new GameObject("Garden HUD", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(SnakeHud));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Reference, 900);
            scaler.matchWidthOrHeight = .5f;
            var hud = root.GetComponent<SnakeHud>();
            var bound = new SerializedObject(hud);

            // ---- wordmark ------------------------------------------------
            var brand = Anchored("Brand", root.transform, new Vector2(0, 1), new Vector2(44, -44), new Vector2(420, 68));
            var brandGroup = brand.gameObject.AddComponent<CanvasGroup>();
            var wordmark = Label("Wordmark", brand, "GARDEN SNAKE", 21, Cream.With(.92f),
                new Vector2(0, 0), new Vector2(420, 30), TextAlignmentOptions.TopLeft);
            wordmark.fontStyle = FontStyles.Bold;
            wordmark.characterSpacing = 12;
            var rule = Anchored("Rule", brand, new Vector2(0, 1), new Vector2(2, -34), new Vector2(38, 3));
            rule.pivot = new Vector2(0, 1);
            rule.anchoredPosition = new Vector2(2, -34);
            Fill(rule, pill, Accent.With(.9f));

            // ---- score ---------------------------------------------------
            var scoreRoot = Anchored("Score", root.transform, new Vector2(1, 1), new Vector2(-44, -40), new Vector2(320, 110));
            var appleDot = Anchored("Apple dot", scoreRoot, new Vector2(1, 1), new Vector2(-124, -22), new Vector2(20, 20));
            Fill(appleDot, circle, AppleSkin);
            var score = Label("Apples", scoreRoot, "0", 56, Cream,
                new Vector2(0, -6), new Vector2(300, 68), TextAlignmentOptions.TopRight);
            score.fontStyle = FontStyles.Bold;
            var best = Label("Best", scoreRoot, "BEST 0", 13, Cream.With(.5f),
                new Vector2(0, -62), new Vector2(300, 24), TextAlignmentOptions.TopRight);
            best.characterSpacing = 9;

            // ---- floating pickup number and headline ---------------------
            var toast = Label("Pickup", root.transform, "", 30, Accent,
                Vector2.zero, new Vector2(260, 48), TextAlignmentOptions.Center, new Vector2(.5f, .5f));
            toast.fontStyle = FontStyles.Bold;
            var banner = Label("Banner", root.transform, "", 22, Cream,
                new Vector2(0, -46), new Vector2(700, 40), TextAlignmentOptions.Center, new Vector2(.5f, 1));
            banner.fontStyle = FontStyles.Bold;
            banner.characterSpacing = 14;

            // ---- hint line -----------------------------------------------
            var hint = Anchored("Hints", root.transform, new Vector2(0, 0), new Vector2(44, 40), new Vector2(620, 26));
            var hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
            var hintLabel = Label("Line", hint, "WASD / ARROWS TO STEER   ·   P PAUSE   ·   M SOUND", 13,
                Cream.With(.45f), Vector2.zero, new Vector2(620, 26), TextAlignmentOptions.Left);
            hintLabel.characterSpacing = 7;

            // ---- corner buttons -------------------------------------------
            var pauseButton = IconButton("Pause", root.transform, circle, pauseIcon,
                new Vector2(1, 0), new Vector2(-46, 46), 46, out Image pauseGlyph);
            var muteButton = IconButton("Sound", root.transform, circle, soundOnIcon,
                new Vector2(1, 0), new Vector2(-106, 46), 46, out Image muteGlyph);

            // ---- touch steering ------------------------------------------
            var pad = Anchored("Direction pad", root.transform, new Vector2(1, 0), new Vector2(-140, 210), new Vector2(240, 240));
            var padButtons = new[]
            {
                PadButton("Up", pad, circle, chevron, new Vector2(0, 74), 0),
                PadButton("Right", pad, circle, chevron, new Vector2(74, 0), -90),
                PadButton("Down", pad, circle, chevron, new Vector2(0, -74), 180),
                PadButton("Left", pad, circle, chevron, new Vector2(-74, 0), 90)
            };

            // ---- card ------------------------------------------------------
            var scrim = Anchored("Scrim", root.transform, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            scrim.anchorMin = Vector2.zero;
            scrim.anchorMax = Vector2.one;
            scrim.offsetMin = scrim.offsetMax = Vector2.zero;
            var scrimImage = scrim.gameObject.AddComponent<Image>();
            scrimImage.color = Scrim;
            var scrimGroup = scrim.gameObject.AddComponent<CanvasGroup>();

            var card = Anchored("Run card", root.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(660, 388));
            var cardGlow = Anchored("Card glow", card, new Vector2(.5f, .5f), new Vector2(0, -8), new Vector2(860, 580));
            Fill(cardGlow, glow, new Color(0, 0, 0, .5f));
            cardGlow.SetAsFirstSibling();
            var cardFill = Fill(card, panel, Panel);
            cardFill.type = Image.Type.Sliced;
            cardFill.pixelsPerUnitMultiplier = 1.6f;
            var cardEdge = Anchored("Edge", card, new Vector2(.5f, .5f), Vector2.zero, new Vector2(660, 388));
            var edgeImage = Fill(cardEdge, GardenSprites.RoundedOutline("PanelEdge", 96, 28, 2f), PanelEdge);
            edgeImage.type = Image.Type.Sliced;
            edgeImage.pixelsPerUnitMultiplier = 1.6f;
            edgeImage.raycastTarget = false;
            var cardGroup = card.gameObject.AddComponent<CanvasGroup>();

            var eyebrow = Label("Eyebrow", card, "", 12, Accent, new Vector2(0, 138), new Vector2(600, 26));
            eyebrow.characterSpacing = 14;
            eyebrow.fontStyle = FontStyles.Bold;
            var title = Label("Title", card, "", 44, Cream, new Vector2(0, 84), new Vector2(620, 70));
            title.fontStyle = FontStyles.Bold;
            var body = Label("Description", card, "", 18, Cream.With(.62f), new Vector2(0, 12), new Vector2(560, 70));
            body.textWrappingMode = TextWrappingModes.Normal;
            body.lineSpacing = 12;
            var primary = TextButton("Primary", card, pill, "PLAY", new Vector2(0, -78), new Vector2(300, 62),
                Accent, Ink, out TMP_Text primaryLabel);
            var keyHint = Label("Key hint", card, "OR PRESS SPACE", 11, Cream.With(.35f),
                new Vector2(0, -136), new Vector2(400, 22));
            keyHint.characterSpacing = 12;

            // ---- wiring ----------------------------------------------------
            GardenBuilder.Set(bound, "brandGroup", brandGroup);
            GardenBuilder.Set(bound, "scoreText", score);
            GardenBuilder.Set(bound, "bestText", best);
            GardenBuilder.Set(bound, "hintGroup", hintGroup);
            GardenBuilder.Set(bound, "toast", toast);
            GardenBuilder.Set(bound, "banner", banner);
            GardenBuilder.Set(bound, "scrimGroup", scrimGroup);
            GardenBuilder.Set(bound, "card", card.gameObject);
            GardenBuilder.Set(bound, "cardGroup", cardGroup);
            GardenBuilder.Set(bound, "cardEyebrow", eyebrow);
            GardenBuilder.Set(bound, "cardTitle", title);
            GardenBuilder.Set(bound, "cardBody", body);
            GardenBuilder.Set(bound, "primaryButton", primary);
            GardenBuilder.Set(bound, "primaryLabel", primaryLabel);
            GardenBuilder.Set(bound, "pauseButton", pauseButton);
            GardenBuilder.Set(bound, "pauseGlyph", pauseGlyph);
            GardenBuilder.Set(bound, "pauseSprite", pauseIcon);
            GardenBuilder.Set(bound, "resumeSprite", playIcon);
            GardenBuilder.Set(bound, "muteButton", muteButton);
            GardenBuilder.Set(bound, "muteGlyph", muteGlyph);
            GardenBuilder.Set(bound, "soundOnSprite", soundOnIcon);
            GardenBuilder.Set(bound, "soundOffSprite", soundOffIcon);
            GardenBuilder.Set(bound, "touchPad", pad.gameObject);
            var array = bound.FindProperty("directionButtons");
            array.arraySize = padButtons.Length;
            for (int i = 0; i < padButtons.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = padButtons[i];
            bound.ApplyModifiedPropertiesWithoutUndo();

            // Full-screen flash, driven by Feel. Last child so it covers the card too.
            var flash = Anchored("Flash", root.transform, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            flash.anchorMin = Vector2.zero;
            flash.anchorMax = Vector2.one;
            flash.offsetMin = flash.offsetMax = Vector2.zero;
            var flashImage = flash.gameObject.AddComponent<Image>();
            flashImage.color = new Color(1, 1, 1, 0);
            flashImage.raycastTarget = false;
            flash.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            flash.gameObject.AddComponent<MoreMountains.Feedbacks.MMFlash>();

            new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            return hud;
        }

        // ---------------------------------------------------------------- primitives

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
            var rect = Anchored(name, parent, anchor ?? new Vector2(.5f, .5f), position, dimensions);
            rect.pivot = new Vector2(.5f, .5f);
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
            fill.color = Panel.With(.85f);
            var button = rect.gameObject.AddComponent<Button>();
            Style(button, fill);
            var glyphRect = Anchored("Glyph", rect, new Vector2(.5f, .5f), Vector2.zero, new Vector2(diameter, diameter));
            glyph = Fill(glyphRect, icon, Cream.With(.9f));
            return button;
        }

        private static Button PadButton(string name, Transform parent, Sprite background, Sprite icon,
            Vector2 position, float rotation)
        {
            var button = IconButton(name, parent, background, icon, new Vector2(.5f, .5f), position, 66, out Image glyph);
            button.GetComponent<Image>().color = Panel.With(.55f);
            glyph.transform.localRotation = Quaternion.Euler(0, 0, rotation);
            return button;
        }

        private static Button TextButton(string name, Transform parent, Sprite background, string caption,
            Vector2 position, Vector2 size, Color fillColor, Color textColor, out TMP_Text label)
        {
            var rect = Anchored(name, parent, new Vector2(.5f, .5f), position, size);
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
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(.82f, .82f, .82f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = .09f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
        }
    }
}
