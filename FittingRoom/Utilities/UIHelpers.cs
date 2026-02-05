using System;
using System.Collections.Generic;
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
        /// Returns the position to draw content so it appears visually centered in a texture box.
        /// Compensates for the asymmetric border of the standard menu texture (bottom is 4px thicker).
        /// </summary>
        /// <param name="bounds">The bounds of the texture box.</param>
        /// <param name="contentSize">The size of the content to center.</param>
        /// <returns>The top-left position to draw the content.</returns>
        public static Vector2 GetVisualCenter(Rectangle bounds, Vector2 contentSize)
        {
            return new Vector2(
                bounds.X + (bounds.Width - contentSize.X) / 2,
                bounds.Y + (bounds.Height - contentSize.Y) / 2 + TextureBoxVisualOffsetY
            );
        }

        /// <summary>
        /// Calculates the width needed for a button based on its text content.
        /// Uses fixed TabAndButtonWidth unless text requires more space.
        /// </summary>
        public static int CalculateButtonWidth(string text)
        {
            Vector2 textSize = Game1.smallFont.MeasureString(text);
            int calculatedWidth = (int)textSize.X + TextPadding * 2;
            return Math.Max(TabAndButtonWidth, calculatedWidth);
        }

        /// <summary>
        /// Draws a text button with label and bold text on hover.
        /// </summary>
        /// <param name="shadowOffset">Shadow offset (0 = none, 4 = default, 8 = vanilla)</param>
        /// <param name="shadowOpacity">Shadow opacity (0 = none, 0.4 = default/vanilla)</param>
        public static void DrawTextButton(SpriteBatch b, ClickableComponent button, string label, int shadowOffset = 4, float shadowOpacity = 0.4f)
        {
            bool isHovered = button.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            DrawTextureBox(b, button.bounds.X, button.bounds.Y,
                button.bounds.Width, button.bounds.Height, Color.White,
                scale: 1f, shadowOffset: shadowOffset, shadowOpacity: shadowOpacity);

            Vector2 textSize = Game1.smallFont.MeasureString(label);
            Vector2 textPos = GetVisualCenter(button.bounds, textSize);

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

        private static readonly Dictionary<string, string> truncatedTextCache = new();
        private static int lastTruncationWidth = -1;

        /// <summary>
        /// Draws a texture box with customizable shadow. Compatible with interface recolor mods.
        /// Default shadow: 4px offset, 40% opacity.
        /// </summary>
        /// <param name="shadowOffset">Shadow offset in pixels (0 = no shadow, 4 = default, 8 = vanilla)</param>
        /// <param name="shadowOpacity">Shadow opacity (0 = invisible, 0.4 = default/vanilla)</param>
        public static void DrawTextureBox(SpriteBatch b, int x, int y, int width, int height, Color color, float scale = 1f, int shadowOffset = 4, float shadowOpacity = 0.4f)
        {
            if (shadowOffset > 0 && shadowOpacity > 0f)
            {
                IClickableMenu.drawTextureBox(b, Game1.menuTexture, MenuBoxSourceRect,
                    x - shadowOffset, y + shadowOffset, width, height,
                    Color.Black * shadowOpacity, scale, drawShadow: false);
            }

            IClickableMenu.drawTextureBox(b, Game1.menuTexture, MenuBoxSourceRect,
                x, y, width, height, color, scale, drawShadow: false);
        }

        /// <summary>
        /// Draws a texture box without shadow. Convenience wrapper for DrawTextureBox.
        /// </summary>
        public static void DrawTextureBoxNoShadow(SpriteBatch b, int x, int y, int width, int height, Color color, float scale = 1f)
        {
            DrawTextureBox(b, x, y, width, height, color, scale, shadowOffset: 0, shadowOpacity: 0f);
        }

        /// <summary>
        /// Draws a dropdown button with text, optional label, optional arrow, and optional clear button.
        /// Centralizes dropdown button rendering for consistent appearance.
        /// </summary>
        public static void DrawDropdownButton(
            SpriteBatch b,
            Rectangle bounds,
            string displayText,
            bool isOpen,
            bool showArrow = true,
            string? label = null,
            int labelX = 0,
            ClickableComponent? clearButton = null,
            bool hasValue = false,
            Action<SpriteBatch, ClickableComponent>? drawClearButton = null)
        {
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            bool isHovered = bounds.Contains(mouseX, mouseY) && !isOpen;

            // Draw label if provided
            if (!string.IsNullOrEmpty(label))
            {
                float textHeight = Game1.smallFont.MeasureString("A").Y;
                int labelY = bounds.Y + (int)((bounds.Height - textHeight) / 2);
                Utility.drawTextWithShadow(b, label, Game1.smallFont,
                    new Vector2(labelX, labelY), Game1.textColor);
            }

            // Draw texture box
            DrawTextureBox(b, bounds.X, bounds.Y, bounds.Width, bounds.Height,
                isOpen ? Color.Wheat : Color.White);

            // Calculate max text width based on whether we have clear button and/or arrow
            int reservedRight = 20; // Base right padding
            if (showArrow) reservedRight += 28;
            if (hasValue && clearButton != null) reservedRight = ClearButtonSize + ClearButtonRightMargin + 28;
            int maxTextWidth = bounds.Width - reservedRight - 20;

            // Truncate text if needed
            string truncatedText = TruncateText(displayText, maxTextWidth);

            // Calculate text position
            Vector2 textSize = Game1.smallFont.MeasureString(truncatedText);
            Vector2 textPos = new Vector2(
                bounds.X + 20,
                bounds.Y + (bounds.Height - textSize.Y) / 2
            );

            // Draw text with hover effect
            if (isHovered)
            {
                Utility.drawTextWithShadow(b, truncatedText, Game1.smallFont, textPos + new Vector2(-1, 0), Game1.textColor * 0.8f);
                Utility.drawTextWithShadow(b, truncatedText, Game1.smallFont, textPos, Game1.textColor);
            }
            else
            {
                Utility.drawTextWithShadow(b, truncatedText, Game1.smallFont, textPos, Game1.textColor);
            }

            // Draw arrow if requested
            if (showArrow)
            {
                Rectangle sourceRect = new Rectangle(421, 472, 11, 12);
                float scale = 2f;
                Vector2 arrowPos = new Vector2(
                    bounds.Right - 24,
                    bounds.Y + (bounds.Height - 12 * scale) / 2
                );
                b.Draw(Game1.mouseCursors, arrowPos, sourceRect, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
            }

            // Draw clear button if value exists
            if (hasValue && clearButton != null && drawClearButton != null)
            {
                drawClearButton(b, clearButton);
            }
        }

        /// <summary>
        /// Draws dropdown options with texture box background, hover effects, scroll indicators, and text truncation.
        /// Returns the full text of any hovered truncated option (for tooltip display).
        /// </summary>
        public static string? DrawDropdownOptions(
            SpriteBatch b,
            Rectangle anchorBounds,
            List<ClickableComponent> options,
            int firstVisibleIndex,
            int maxVisibleItems,
            Func<ClickableComponent, bool>? isSelected = null,
            bool enableTruncation = true)
        {
            if (options.Count == 0)
                return null;

            string? hoveredTruncatedText = null;
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            int visibleCount = 0;
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].visible)
                    visibleCount++;
            }
            if (visibleCount == 0)
                return null;

            int optionHeight = options[0].bounds.Height;
            int dropdownHeight = Math.Min(visibleCount, maxVisibleItems) * optionHeight;

            DrawTextureBoxNoShadow(b,
                anchorBounds.X - 4,
                anchorBounds.Bottom - 4,
                anchorBounds.Width + 8,
                dropdownHeight + 8,
                Color.White);

            int maxTextWidth = anchorBounds.Width - FilterTextPadding * 2;
            if (lastTruncationWidth != maxTextWidth)
            {
                truncatedTextCache.Clear();
                lastTruncationWidth = maxTextWidth;
            }

            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (!option.visible)
                    continue;

                bool optionSelected = isSelected?.Invoke(option) ?? false;
                bool optionHovered = option.containsPoint(mouseX, mouseY);

                if (optionSelected)
                {
                    b.Draw(Game1.staminaRect, option.bounds, Color.LightBlue * 0.3f);
                }
                else if (optionHovered)
                {
                    b.Draw(Game1.staminaRect, option.bounds, Color.Wheat * 0.6f);
                }

                string fullText = option.name;
                string displayText;
                bool isTruncated = false;

                if (enableTruncation)
                {
                    if (truncatedTextCache.TryGetValue(fullText, out var cached))
                    {
                        displayText = cached;
                        isTruncated = displayText != fullText;
                    }
                    else
                    {
                        displayText = fullText;
                        Vector2 textSize = Game1.smallFont.MeasureString(displayText);

                        if (textSize.X > maxTextWidth)
                        {
                            isTruncated = true;
                            while (textSize.X > maxTextWidth && displayText.Length > 3)
                            {
                                displayText = displayText.Substring(0, displayText.Length - 1);
                                textSize = Game1.smallFont.MeasureString(displayText + "...");
                            }
                            displayText += "...";
                        }
                        truncatedTextCache[fullText] = displayText;
                    }
                }
                else
                {
                    displayText = fullText;
                }

                Vector2 finalTextSize = Game1.smallFont.MeasureString(displayText);
                Vector2 textPos = new Vector2(
                    option.bounds.X + 16,
                    option.bounds.Y + (option.bounds.Height - finalTextSize.Y) / 2
                );

                Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPos, Game1.textColor);

                if (optionHovered && isTruncated)
                {
                    hoveredTruncatedText = fullText;
                }
            }

            bool canScrollUp = firstVisibleIndex > 0;
            bool canScrollDown = firstVisibleIndex + maxVisibleItems < options.Count;

            if (canScrollUp)
            {
                Rectangle upArrowSource = new Rectangle(421, 459, 11, 12);
                Vector2 upArrowPos = new Vector2(anchorBounds.Right - 22, anchorBounds.Bottom + 6);
                b.Draw(Game1.mouseCursors, upArrowPos, upArrowSource, Color.White, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 1f);
            }

            if (canScrollDown)
            {
                Rectangle downArrowSource = new Rectangle(421, 472, 11, 12);
                Vector2 downArrowPos = new Vector2(anchorBounds.Right - 22, anchorBounds.Bottom + dropdownHeight - 22);
                b.Draw(Game1.mouseCursors, downArrowPos, downArrowSource, Color.White, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 1f);
            }

            return hoveredTruncatedText;
        }
    }
}
