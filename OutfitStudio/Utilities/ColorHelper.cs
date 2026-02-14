using Microsoft.Xna.Framework;
using StardewValley.Objects;

namespace OutfitStudio
{
    public static class ColorHelper
    {
        public static void ApplyColor(Clothing item, string? colorString)
        {
            var color = ParseColor(colorString);
            if (color.HasValue)
                item.clothesColor.Set(color.Value);
        }

        public static string ToColorString(Color color)
        {
            return $"{color.R},{color.G},{color.B},{color.A}";
        }

        public static Color? ParseColor(string? colorString)
        {
            if (string.IsNullOrEmpty(colorString))
                return null;

            string[] parts = colorString.Split(',');
            if (parts.Length < 3)
                return null;

            if (!byte.TryParse(parts[0], out byte r) ||
                !byte.TryParse(parts[1], out byte g) ||
                !byte.TryParse(parts[2], out byte b))
                return null;

            byte a = 255;
            if (parts.Length >= 4)
                byte.TryParse(parts[3], out a);

            return new Color(r, g, b, a);
        }
    }
}
