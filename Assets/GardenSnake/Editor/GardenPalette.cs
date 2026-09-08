using UnityEngine;

namespace GardenSnake.Editor
{
    /// <summary>
    /// One place for every colour in the game. The garden is a bright afternoon lawn: a sunny
    /// yellow-green surround with light pooling in the middle, a deeper green board raised inside
    /// a terracotta planter, and a pale butter snake that stays legible against the grass at any
    /// size. Warm orange is the single accent, shared by the apple glow, the confetti and the UI.
    /// </summary>
    public static class GardenPalette
    {
        // Surround
        public static readonly Color Surround = Hex("7FAF57");
        public static readonly Color SurroundGlow = Hex("A9CE77");

        // Board
        public static readonly Color GrassLight = Hex("3E9440");
        public static readonly Color GrassDark = Hex("368539");
        public static readonly Color PlanterRim = Hex("C96F3B");
        public static readonly Color PlanterSoil = Hex("245C29");

        // Snake
        public static readonly Color SnakeBody = Hex("F7EFA8");
        public static readonly Color SnakeSpot = Hex("8ED04A");
        public static readonly Color SnakeCream = Hex("FFFFFA");
        public static readonly Color Ink = Hex("1E3A2A");

        // Props
        public static readonly Color AppleSkin = Hex("E8402F");
        public static readonly Color AppleLeaf = Hex("5FBF3F");
        public static readonly Color Stem = Hex("7A5236");
        public static readonly Color Petal = Hex("FFFDF2");
        public static readonly Color Pollen = Hex("FFC93C");
        public static readonly Color Stone = Hex("96A38C");

        // Interface
        public static readonly Color Paper = Hex("FFF9EC");
        public static readonly Color PaperEdge = new Color(.118f, .227f, .165f, .1f);
        public static readonly Color TextStrong = Hex("1E3A2A");
        public static readonly Color TextSoft = new Color(.118f, .227f, .165f, .62f);
        public static readonly Color Accent = Hex("F2913D");
        public static readonly Color AccentDeep = Hex("E2622E");
        public static readonly Color Scrim = new Color(.078f, .18f, .098f, .72f);
        public static readonly Color Danger = Hex("E8402F");

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            return color;
        }

        public static Color With(this Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
    }
}
