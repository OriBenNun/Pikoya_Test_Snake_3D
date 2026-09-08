using UnityEngine;

namespace GardenSnake.Editor
{
    /// <summary>
    /// One place for every colour in the game. The garden is lit like late afternoon: a deep,
    /// desaturated surround so the warm board reads as the only lit thing on screen, a pale
    /// butter-lime snake that separates from the grass at any size, and a single warm accent
    /// (pollen gold) shared by the apple shine, the confetti and the UI highlights.
    /// </summary>
    public static class GardenPalette
    {
        // Surround
        public static readonly Color Surround = Hex("16211B");
        public static readonly Color SurroundFloor = Hex("101A15");

        // Board
        public static readonly Color GrassLight = Hex("4E9147");
        public static readonly Color GrassDark = Hex("447F40");
        public static readonly Color PlanterRim = Hex("8C5638");
        public static readonly Color PlanterSoil = Hex("3A2A20");

        // Snake
        public static readonly Color SnakeBody = Hex("E4F2AE");
        public static readonly Color SnakeSpot = Hex("BCDD72");
        public static readonly Color SnakeCream = Hex("FFFDF0");
        public static readonly Color Ink = Hex("14241C");

        // Props
        public static readonly Color AppleSkin = Hex("E4443A");
        public static readonly Color AppleLeaf = Hex("6FBF4A");
        public static readonly Color Stem = Hex("6B4A32");
        public static readonly Color Petal = Hex("FFF6DC");
        public static readonly Color Pollen = Hex("FFC55C");
        public static readonly Color Stone = Hex("6E7A6C");

        // Interface
        public static readonly Color Cream = Hex("FFF8E7");
        public static readonly Color CreamDim = new Color(1f, .973f, .906f, .55f);
        public static readonly Color Panel = Hex("1B2A22");
        public static readonly Color PanelEdge = new Color(1f, 1f, 1f, .09f);
        public static readonly Color Scrim = new Color(.043f, .086f, .063f, .72f);
        public static readonly Color Accent = Hex("FFC55C");
        public static readonly Color Danger = Hex("E4443A");

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            return color;
        }

        public static Color With(this Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
    }
}
