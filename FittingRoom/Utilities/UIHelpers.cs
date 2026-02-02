using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using static FittingRoom.OutfitLayoutConstants;

namespace FittingRoom
{
    /// <summary>
    /// Static utility methods for UI drawing and text manipulation.
    /// </summary>
    public static class UIHelpers
    {
        /// <summary>
        /// Truncates text with ellipsis if it exceeds maxWidth.
        /// </summary>
        public static string TruncateText(string text, int maxWidth, SpriteFont? font = null)
        {
            font ??= Game1.smallFont;

            if (font.MeasureString(text).X <= maxWidth)
                return text;

            while (text.Length > 0 && font.MeasureString(text + "...").X > maxWidth)
            {
                text = text.Substring(0, text.Length - 1);
            }

            return text + "...";
        }

        /// <summary>
        /// Calculates the width needed for a button based on its text content.
        /// Uses fixed TabAndButtonWidth unless text requires more space.
        /// </summary>
        public static int CalculateButtonWidth(string text)
        {
            Vector2 textSize = Game1.smallFont.MeasureString(text);
            int calculatedWidth = (int)textSize.X + TextPadding * 2;

            // Use fixed width unless calculated width exceeds it
            return Math.Max(TabAndButtonWidth, calculatedWidth);
        }

        /// <summary>
        /// Draws a text button with label and bold text on hover.
        /// </summary>
        public static void DrawTextButton(SpriteBatch b, ClickableComponent button, string label)
        {
            bool isHovered = button.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            IClickableMenu.drawTextureBox(b, button.bounds.X, button.bounds.Y,
                button.bounds.Width, button.bounds.Height, Color.White);

            Vector2 textSize = Game1.smallFont.MeasureString(label);
            Vector2 textPos = new Vector2(
                button.bounds.X + (button.bounds.Width - textSize.X) / 2,
                button.bounds.Y + (button.bounds.Height - textSize.Y) / 2
            );

            if (isHovered)
            {
                Utility.drawTextWithShadow(b, label, Game1.smallFont, textPos + new Vector2(-1, 0), Game1.textColor * 0.8f);
                Utility.drawTextWithShadow(b, label, Game1.smallFont, textPos, Game1.textColor);
            }
            else
            {
                Utility.drawTextWithShadow(b, label, Game1.smallFont, textPos, Game1.textColor);
            }
        }

        /// <summary>
        /// Draws a texture button with hover scale effect (1.2x on hover).
        /// </summary>
        public static void DrawTextureButton(SpriteBatch b, ClickableTextureComponent button)
        {
            bool isHovered = button.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            if (isHovered)
            {
                float originalScale = button.scale;
                button.scale = originalScale * ButtonHoveringScale;
                button.draw(b);
                button.scale = originalScale;
            }
            else
            {
                button.draw(b);
            }
        }
    }
}
