using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    public static class UIHelpers
    {
        public static string TruncateText(string text, int maxWidth, SpriteFont? font = null)
        {
            font ??= Game1.smallFont;
            return TruncateText(text, maxWidth, s => font.MeasureString(s).X);
        }

        internal static string TruncateText(string text, int maxWidth, Func<string, float> measureWidth)
        {
            if (measureWidth(text) <= maxWidth)
                return text;

            while (text.Length > 0 && measureWidth(text + "...") > maxWidth)
            {
                text = text.Substring(0, text.Length - 1);
            }

            return text + "...";
        }

        public static int ClampScrollOffset(int current, int totalItems, int visibleItems)
        {
            int maxOffset = Math.Max(0, totalItems - visibleItems);
            return Math.Clamp(current, 0, maxOffset);
        }

        internal static string FormatTagsWithCount(
            IReadOnlyList<string> tags,
            string prefix,
            int maxWidth,
            Func<string, float> measureWidth,
            string noneText)
        {
            if (tags.Count == 0)
                return prefix + noneText;

            string fullText = prefix + string.Join(", ", tags);
            if (measureWidth(fullText) <= maxWidth)
                return fullText;

            for (int shown = tags.Count - 1; shown >= 1; shown--)
            {
                int remaining = tags.Count - shown;
                string partial = prefix;
                for (int i = 0; i < shown; i++)
                {
                    if (i > 0) partial += ", ";
                    partial += tags[i];
                }
                partial += $" (+{remaining})";

                if (measureWidth(partial) <= maxWidth)
                    return partial;
            }

            return prefix + $"(+{tags.Count})";
        }

        private const int TotalSeasons = 4;
        private const int TotalWeatherTypes = 2;
        private const int TotalAreaTypes = 2;

        internal static string GenerateRuleName(
            IEnumerable<string> seasons,
            IEnumerable<string> weatherDisplayNames,
            IEnumerable<string> areaDisplayNames,
            IEnumerable<string> locations,
            IEnumerable<string> festivalDisplayNames,
            bool isWeddingDay,
            string weddingLabel)
        {
            var parts = new List<string>();

            var seasonList = seasons.ToList();
            if (seasonList.Count > 0 && seasonList.Count < TotalSeasons)
                parts.Add(string.Join(", ", seasonList));

            var weatherList = weatherDisplayNames.ToList();
            if (weatherList.Count > 0 && weatherList.Count < TotalWeatherTypes)
                parts.Add(string.Join(", ", weatherList));

            var areaList = areaDisplayNames.ToList();
            if (areaList.Count > 0 && areaList.Count < TotalAreaTypes)
                parts.Add(string.Join(", ", areaList));

            var locationList = locations.ToList();
            if (locationList.Count > 0)
                parts.Add(string.Join(", ", locationList));

            var festivalList = festivalDisplayNames.ToList();
            if (festivalList.Count > 0)
                parts.Add(string.Join(", ", festivalList));

            if (isWeddingDay)
                parts.Add(weddingLabel);

            if (parts.Count > 0)
                return string.Join(" | ", parts);

            string always = TranslationCache.ScheduleAlways;
            return string.IsNullOrEmpty(always) ? "Always" : always;
        }

        // Compensates for the asymmetric border of the standard menu texture (bottom is 4px thicker)
        public static Vector2 GetVisualCenter(Rectangle bounds, Vector2 contentSize)
        {
            return new Vector2(
                bounds.X + (bounds.Width - contentSize.X) / 2,
                bounds.Y + (bounds.Height - contentSize.Y) / 2 + TextureBoxVisualOffsetY
            );
        }

        public static int CalculateButtonWidth(string text)
        {
            Vector2 textSize = Game1.smallFont.MeasureString(text);
            int calculatedWidth = (int)textSize.X + TextPadding * 2;
            return Math.Max(TabAndButtonWidth, calculatedWidth);
        }

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

        public static void DrawTextureBoxNoShadow(SpriteBatch b, int x, int y, int width, int height, Color color, float scale = 1f)
        {
            DrawTextureBox(b, x, y, width, height, color, scale, shadowOffset: 0, shadowOpacity: 0f);
        }

        public static void DrawDropdownButton(
            SpriteBatch b,
            Rectangle bounds,
            string displayText,
            bool isOpen,
            string? label = null,
            int labelX = 0,
            ClickableComponent? clearButton = null,
            bool hasValue = false,
            Action<SpriteBatch, ClickableComponent>? drawClearButton = null,
            float opacity = 1f)
        {
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            bool isHovered = opacity >= 1f && bounds.Contains(mouseX, mouseY) && !isOpen;

            if (!string.IsNullOrEmpty(label))
            {
                float textHeight = Game1.smallFont.MeasureString("A").Y;
                int labelY = bounds.Y + (int)((bounds.Height - textHeight) / 2);
                Utility.drawTextWithShadow(b, label, Game1.smallFont,
                    new Vector2(labelX, labelY), Game1.textColor * opacity);
            }

            DrawTextureBox(b, bounds.X, bounds.Y, bounds.Width, bounds.Height,
                isOpen ? Color.Wheat : Color.White * opacity);

            int reservedRight = 20; // Base right padding
            if (hasValue && clearButton != null) reservedRight = ClearButtonSize + ClearButtonRightMargin;
            int maxTextWidth = bounds.Width - reservedRight - 20;

            string truncatedText = TruncateText(displayText, maxTextWidth);

            Vector2 textSize = Game1.smallFont.MeasureString(truncatedText);
            Vector2 textPos = new Vector2(
                bounds.X + 20,
                bounds.Y + (bounds.Height - textSize.Y) / 2
            );

            if (isHovered)
            {
                Utility.drawTextWithShadow(b, truncatedText, Game1.smallFont, textPos + new Vector2(-1, 0), Game1.textColor * 0.8f);
                Utility.drawTextWithShadow(b, truncatedText, Game1.smallFont, textPos, Game1.textColor);
            }
            else
            {
                Utility.drawTextWithShadow(b, truncatedText, Game1.smallFont, textPos, Game1.textColor * opacity);
            }

            if (hasValue && clearButton != null && drawClearButton != null)
            {
                drawClearButton(b, clearButton);
            }
        }

        public static void DrawClearButton(SpriteBatch b, ClickableComponent button)
        {
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            bool isHovered = button.containsPoint(mouseX, mouseY);

            Rectangle sourceRect = new Rectangle(337, 494, 12, 12);
            float scale = isHovered ? 2.2f : 2f;
            Vector2 center = new Vector2(
                button.bounds.X + button.bounds.Width / 2 - 2,
                button.bounds.Y + button.bounds.Height / 2
            );
            Vector2 origin = new Vector2(6, 6);

            b.Draw(
                Game1.mouseCursors,
                center,
                sourceRect,
                Color.White,
                0f,
                origin,
                scale,
                SpriteEffects.None,
                1f
            );
        }

        public static void DrawTabWithText(SpriteBatch b, ClickableComponent tab, string label, bool isActive, float opacity = 1f)
        {
            Color boxColor = isActive ? Color.White : Color.White * 0.8f;
            Color textColor = isActive ? Game1.textColor : Game1.textColor * TabOpacity;
            boxColor *= opacity;
            textColor *= opacity;
            bool isHovered = opacity >= 1f && tab.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            DrawTextureBox(b, tab.bounds.X, tab.bounds.Y,
                tab.bounds.Width, tab.bounds.Height, boxColor);

            Vector2 labelSize = Game1.smallFont.MeasureString(label);
            Vector2 textPos = new Vector2(
                tab.bounds.X + (tab.bounds.Width - labelSize.X) / 2,
                tab.bounds.Y + (tab.bounds.Height - labelSize.Y) / 2);

            if (isHovered)
            {
                Utility.drawTextWithShadow(b, label, Game1.smallFont, textPos + new Vector2(-1, 0), textColor * 0.8f);
                Utility.drawTextWithShadow(b, label, Game1.smallFont, textPos, textColor);
            }
            else
            {
                Utility.drawTextWithShadow(b, label, Game1.smallFont, textPos, textColor);
            }
        }

        public static string? DrawDropdownOptions(
            SpriteBatch b,
            Rectangle anchorBounds,
            List<ClickableComponent> options,
            int firstVisibleIndex,
            int maxVisibleItems,
            Func<ClickableComponent, bool>? isSelected = null,
            bool enableTruncation = true,
            int panelPaddingV = 0)
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
                dropdownHeight + 8 + panelPaddingV * 2,
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
                    b.Draw(Game1.staminaRect, option.bounds, Color.Wheat * 0.6f);
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
                Vector2 upArrowPos = new Vector2(anchorBounds.Right - 24, anchorBounds.Bottom + 8);
                b.Draw(Game1.mouseCursors, upArrowPos, upArrowSource, Color.White, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 1f);
            }

            if (canScrollDown)
            {
                Rectangle downArrowSource = new Rectangle(421, 472, 11, 12);
                Vector2 downArrowPos = new Vector2(anchorBounds.Right - 24, anchorBounds.Bottom + dropdownHeight - 26);
                b.Draw(Game1.mouseCursors, downArrowPos, downArrowSource, Color.White, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 1f);
            }

            return hoveredTruncatedText;
        }
    }
}
