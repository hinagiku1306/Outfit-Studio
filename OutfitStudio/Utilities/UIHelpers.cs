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
        internal static readonly Rectangle CheckedSourceRect = new(236, 425, 9, 9);
        internal static readonly Rectangle UncheckedSourceRect = new(227, 425, 9, 9);
        internal static readonly Rectangle UpScrollArrowSourceRect = new(421, 459, 11, 12);
        internal static readonly Rectangle DownScrollArrowSourceRect = new(421, 472, 11, 12);

        public static bool SuppressHover { get; set; }

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

        internal static string TrimTextFromStart(string text, float maxWidth, Func<string, float> measure)
        {
            if (string.IsNullOrEmpty(text))
                return text ?? "";
            if (measure(text) <= maxWidth)
                return text;
            while (text.Length > 1 && measure(text) > maxWidth)
                text = text.Substring(1);
            return text;
        }

        internal static int CalculateInputBarMaxTextWidth(int boundsWidth, bool hasClearButton)
        {
            int rightReserved = hasClearButton
                ? ClearButtonSize + ClearButtonRightMargin
                : InputBarTextStartX;
            return boundsWidth - InputBarTextStartX - rightReserved;
        }

        internal static int CalculateDropdownArrowX(int anchorRight)
        {
            int arrowW = (int)(UpScrollArrowSourceRect.Width * DropdownArrowScale);
            return anchorRight - arrowW - DropdownArrowPad;
        }

        internal static int CalculateDropdownUpArrowY(int anchorBottom, int panelPaddingV = 0)
        {
            return anchorBottom + panelPaddingV + DropdownArrowPad;
        }

        internal static int CalculateDropdownDownArrowY(int anchorBottom, int dropdownHeight, int panelPaddingV = 0)
        {
            int arrowH = (int)(UpScrollArrowSourceRect.Height * DropdownArrowScale);
            return anchorBottom + panelPaddingV + dropdownHeight - arrowH - DropdownArrowPad;
        }

        internal static int CalculateDropdownButtonMaxTextWidth(int boundsWidth, bool hasClearButton)
        {
            int reservedRight = hasClearButton ? ClearButtonSize + ClearButtonRightMargin : 20;
            return boundsWidth - reservedRight - 20;
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

        public static int CalculateButtonWidth(string text, int maxWidth = int.MaxValue)
        {
            return CalculateButtonWidth(text, maxWidth, s => Game1.smallFont.MeasureString(s).X);
        }

        internal static int CalculateButtonWidth(string text, int maxWidth, Func<string, float> measureWidth)
        {
            int calculatedWidth = (int)measureWidth(text) + TextPadding * 2;
            return Math.Min(Math.Max(TabAndButtonWidth, calculatedWidth), maxWidth);
        }

        public static void DrawTextButton(SpriteBatch b, ClickableComponent button, string label, int shadowOffset = 4, float shadowOpacity = 0.4f)
        {
            bool isHovered = !SuppressHover && button.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            DrawTextureBox(b, button.bounds.X, button.bounds.Y,
                button.bounds.Width, button.bounds.Height, Color.White,
                scale: 1f, shadowOffset: shadowOffset, shadowOpacity: shadowOpacity);

            int maxTextWidth = button.bounds.Width - TextPadding * 2;
            string displayLabel = TruncateText(label, maxTextWidth);

            Vector2 textSize = Game1.smallFont.MeasureString(displayLabel);
            Vector2 textPos = GetVisualCenter(button.bounds, textSize);

            if (isHovered)
            {
                Utility.drawTextWithShadow(b, displayLabel, Game1.smallFont, textPos + new Vector2(-1, 0), Game1.textColor * 0.8f);
                Utility.drawTextWithShadow(b, displayLabel, Game1.smallFont, textPos, Game1.textColor);
            }
            else
            {
                Utility.drawTextWithShadow(b, displayLabel, Game1.smallFont, textPos, Game1.textColor);
            }
        }

        public static int GetToggleButtonWidth()
        {
            return (int)Game1.smallFont.MeasureString("+").X + TextPadding * 2;
        }

        public static void DrawToggleButton(SpriteBatch b, ClickableComponent button, bool isOpen)
        {
            bool isHovered = !SuppressHover && button.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            DrawTextureBox(b, button.bounds.X, button.bounds.Y,
                button.bounds.Width, button.bounds.Height, Color.White,
                shadowOffset: 2, shadowOpacity: 0.3f);

            if (isHovered)
                b.Draw(Game1.staminaRect, button.bounds, HoverEffectColor);

            string text = isOpen ? "-" : "+";
            Vector2 textSize = Game1.smallFont.MeasureString(text);
            Vector2 textPos = GetVisualCenter(button.bounds, textSize);
            Utility.drawTextWithShadow(b, text, Game1.smallFont, textPos, Game1.textColor);
        }

        public static void DrawTextureButton(SpriteBatch b, ClickableTextureComponent button)
        {
            bool isHovered = !SuppressHover && button.containsPoint(Game1.getMouseX(), Game1.getMouseY());

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
            int? labelY = null,
            ClickableComponent? clearButton = null,
            bool hasValue = false,
            Action<SpriteBatch, ClickableComponent>? drawClearButton = null,
            float opacity = 1f,
            string? placeholder = null)
        {
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            bool isHovered = !SuppressHover && opacity >= 1f && bounds.Contains(mouseX, mouseY) && !isOpen;

            if (!string.IsNullOrEmpty(label))
            {
                float textHeight = Game1.smallFont.MeasureString("A").Y;
                int effectiveLabelY = labelY ?? bounds.Y + (int)((bounds.Height - textHeight) / 2);
                Utility.drawTextWithShadow(b, label, Game1.smallFont,
                    new Vector2(labelX, effectiveLabelY), Game1.textColor * opacity);
            }

            DrawTextureBox(b, bounds.X, bounds.Y, bounds.Width, bounds.Height,
                isOpen ? Color.Wheat : Color.White * opacity);

            int maxTextWidth = CalculateDropdownButtonMaxTextWidth(bounds.Width, hasValue && clearButton != null);

            bool isEmpty = string.IsNullOrEmpty(displayText);
            if (isEmpty && !string.IsNullOrEmpty(placeholder))
            {
                string placeholderTrunc = TruncateText(placeholder, maxTextWidth);
                Vector2 placeholderSize = Game1.smallFont.MeasureString(placeholderTrunc);
                Vector2 placeholderPos = new Vector2(bounds.X + 20, bounds.Y + (bounds.Height - placeholderSize.Y) / 2);
                Utility.drawTextWithShadow(b, placeholderTrunc, Game1.smallFont, placeholderPos, Color.Gray * opacity);
            }
            else
            {
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
            }

            if (hasValue && clearButton != null)
            {
                if (drawClearButton != null)
                    drawClearButton(b, clearButton);
                else
                    DrawClearButton(b, clearButton);
            }
        }

        public static void DrawClearButton(SpriteBatch b, ClickableComponent button)
        {
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            bool isHovered = !SuppressHover && button.containsPoint(mouseX, mouseY);

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

        public static void DrawInputBar(
            SpriteBatch b,
            Rectangle bounds,
            string text,
            bool isFocused,
            string? placeholder = null,
            ClickableComponent? clearButton = null,
            float opacity = 1f,
            int xOffset = 0,
            Color? focusedBoxColor = null)
        {
            bool hasText = !string.IsNullOrEmpty(text);
            bool hasClearButton = clearButton != null && hasText;
            Color boxColor = (isFocused && focusedBoxColor.HasValue) ? focusedBoxColor.Value : Color.White;

            DrawTextureBox(b, bounds.X + xOffset, bounds.Y, bounds.Width, bounds.Height, boxColor * opacity);

            float textHeight = Game1.smallFont.MeasureString("A").Y;
            int maxTextWidth = CalculateInputBarMaxTextWidth(bounds.Width, hasClearButton);
            Vector2 textPos = new Vector2(
                bounds.X + InputBarTextStartX + xOffset,
                bounds.Y + (bounds.Height - textHeight) / 2);

            string displayText = "";

            if (!hasText)
            {
                if (!string.IsNullOrEmpty(placeholder))
                    Utility.drawTextWithShadow(b, placeholder, Game1.smallFont, textPos, Color.Gray * opacity);
            }
            else
            {
                displayText = isFocused
                    ? TrimTextFromStart(text, maxTextWidth, s => Game1.smallFont.MeasureString(s).X)
                    : TruncateText(text, maxTextWidth);

                Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPos, Game1.textColor * opacity);

                if (clearButton != null)
                    DrawClearButton(b, clearButton);
            }

            if (isFocused)
            {
                float caretX = hasText
                    ? textPos.X + Game1.smallFont.MeasureString(displayText).X
                    : textPos.X;

                if ((int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 500) % 2 == 0)
                {
                    b.Draw(Game1.staminaRect,
                        new Rectangle((int)caretX, (int)textPos.Y, InputBarCaretWidth, (int)textHeight),
                        Game1.textColor * opacity);
                }
            }
        }

        public static void DrawTabWithText(SpriteBatch b, ClickableComponent tab, string label, bool isActive, float opacity = 1f)
        {
            Color boxColor = isActive ? Color.White : Color.White * 0.8f;
            Color textColor = isActive ? Game1.textColor : Game1.textColor * TabOpacity;
            boxColor *= opacity;
            textColor *= opacity;
            bool isHovered = !SuppressHover && opacity >= 1f && tab.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            DrawTextureBox(b, tab.bounds.X, tab.bounds.Y,
                tab.bounds.Width, tab.bounds.Height, boxColor);

            int maxTextWidth = tab.bounds.Width - TextPadding * 2;
            string displayLabel = TruncateText(label, maxTextWidth);

            Vector2 labelSize = Game1.smallFont.MeasureString(displayLabel);
            Vector2 textPos = new Vector2(
                tab.bounds.X + (tab.bounds.Width - labelSize.X) / 2,
                tab.bounds.Y + (tab.bounds.Height - labelSize.Y) / 2);

            if (isHovered)
            {
                Utility.drawTextWithShadow(b, displayLabel, Game1.smallFont, textPos + new Vector2(-1, 0), textColor * 0.8f);
                Utility.drawTextWithShadow(b, displayLabel, Game1.smallFont, textPos, textColor);
            }
            else
            {
                Utility.drawTextWithShadow(b, displayLabel, Game1.smallFont, textPos, textColor);
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

            int arrowX = CalculateDropdownArrowX(anchorBounds.Right);

            if (canScrollUp)
            {
                int upArrowY = CalculateDropdownUpArrowY(anchorBounds.Bottom);
                b.Draw(Game1.mouseCursors, new Vector2(arrowX, upArrowY), UpScrollArrowSourceRect, Color.White, 0f, Vector2.Zero, DropdownArrowScale, SpriteEffects.None, 1f);
            }

            if (canScrollDown)
            {
                int downArrowY = CalculateDropdownDownArrowY(anchorBounds.Bottom, dropdownHeight);
                b.Draw(Game1.mouseCursors, new Vector2(arrowX, downArrowY), DownScrollArrowSourceRect, Color.White, 0f, Vector2.Zero, DropdownArrowScale, SpriteEffects.None, 1f);
            }

            return hoveredTruncatedText;
        }

        public static string? DrawMultiSelectDropdownOptions(
            SpriteBatch b,
            Rectangle anchorBounds,
            List<ClickableComponent> options,
            int firstVisibleIndex,
            int maxVisibleItems,
            Func<ClickableComponent, bool> isChecked,
            float checkboxScale = 3.2f,
            int checkboxSize = 29,
            int panelPaddingV = 0)
        {
            if (options.Count == 0) return null;

            string? hoveredTruncatedText = null;
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            int visibleCount = 0;
            for (int i = 0; i < options.Count; i++)
                if (options[i].visible) visibleCount++;
            if (visibleCount == 0) return null;

            int optionHeight = options[0].bounds.Height;
            int dropdownHeight = Math.Min(visibleCount, maxVisibleItems) * optionHeight;

            DrawTextureBoxNoShadow(b,
                anchorBounds.X - 4, anchorBounds.Bottom - 4,
                anchorBounds.Width + 8, dropdownHeight + 8 + panelPaddingV * 2,
                Color.White);

            int checkLeftPad = 12;
            int checkToTextGap = 8;
            int maxTextWidth = anchorBounds.Width - 20 - checkboxSize - checkToTextGap - 20;

            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (!option.visible) continue;

                bool optChecked = isChecked(option);
                bool isHovered = option.containsPoint(mouseX, mouseY);

                if (isHovered)
                    b.Draw(Game1.staminaRect, option.bounds, Color.Wheat * 0.6f);

                Rectangle srcRect = optChecked ? CheckedSourceRect : UncheckedSourceRect;
                int checkX = option.bounds.X + checkLeftPad;
                int checkY = option.bounds.Y + (option.bounds.Height - checkboxSize) / 2;
                b.Draw(Game1.mouseCursors, new Vector2(checkX, checkY),
                    srcRect, Color.White, 0f, Vector2.Zero, checkboxScale, SpriteEffects.None, 1f);

                string fullText = option.name;
                string displayText = TruncateText(fullText, maxTextWidth);
                bool isTruncated = displayText != fullText;

                Vector2 textSize = Game1.smallFont.MeasureString(displayText);
                Vector2 textPos = new Vector2(
                    checkX + checkboxSize + checkToTextGap,
                    option.bounds.Y + (option.bounds.Height - textSize.Y) / 2);
                Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPos, Game1.textColor);

                if (isHovered && isTruncated)
                    hoveredTruncatedText = fullText;
            }

            bool canScrollUp = firstVisibleIndex > 0;
            bool canScrollDown = firstVisibleIndex + maxVisibleItems < options.Count;

            int arrowX = CalculateDropdownArrowX(anchorBounds.Right);

            if (canScrollUp)
            {
                int upArrowY = CalculateDropdownUpArrowY(anchorBounds.Bottom, panelPaddingV);
                b.Draw(Game1.mouseCursors, new Vector2(arrowX, upArrowY), UpScrollArrowSourceRect,
                    Color.White, 0f, Vector2.Zero, DropdownArrowScale, SpriteEffects.None, 1f);
            }
            if (canScrollDown)
            {
                int downArrowY = CalculateDropdownDownArrowY(anchorBounds.Bottom, dropdownHeight, panelPaddingV);
                b.Draw(Game1.mouseCursors, new Vector2(arrowX, downArrowY), DownScrollArrowSourceRect,
                    Color.White, 0f, Vector2.Zero, DropdownArrowScale, SpriteEffects.None, 1f);
            }

            return hoveredTruncatedText;
        }

        internal static (Rectangle dialogBounds, Rectangle yesButton, Rectangle noButton)
            CalculateDeleteDialogLayout(string questionText, string yesLabel, string noLabel,
                int viewportWidth, int viewportHeight,
                Func<string, float> measureQuestion, Func<string, float> measureButton)
        {
            float questionWidth = measureQuestion(questionText);
            float questionHeight = measureQuestion("M");
            int yesWidth = CalculateButtonWidth(yesLabel, int.MaxValue, measureButton);
            int noWidth = CalculateButtonWidth(noLabel, int.MaxValue, measureButton);
            int buttonSpacing = 20;
            int totalButtonsWidth = yesWidth + buttonSpacing + noWidth;
            int topPadding = 28;
            int questionToButtons = 30;
            int bottomPadding = 24;
            int dialogWidth = Math.Max((int)questionWidth + 60, totalButtonsWidth + 60);
            int dialogHeight = topPadding + (int)questionHeight + questionToButtons + TabAndButtonHeight + bottomPadding;
            int dialogX = (viewportWidth - dialogWidth) / 2;
            int dialogY = (viewportHeight - dialogHeight) / 2;
            var dialogBounds = new Rectangle(dialogX, dialogY, dialogWidth, dialogHeight);
            int buttonsY = dialogY + topPadding + (int)questionHeight + questionToButtons;
            int buttonsStartX = dialogX + (dialogWidth - totalButtonsWidth) / 2;
            var yes = new Rectangle(buttonsStartX, buttonsY, yesWidth, TabAndButtonHeight);
            var no = new Rectangle(buttonsStartX + yesWidth + buttonSpacing, buttonsY, noWidth, TabAndButtonHeight);
            return (dialogBounds, yes, no);
        }

        public static (Rectangle dialogBounds, ClickableComponent yesButton, ClickableComponent noButton)
            CalculateDeleteDialogLayout(string questionText, string yesLabel, string noLabel)
        {
            Vector2 questionSize = Game1.dialogueFont.MeasureString(questionText);
            int yesWidth = CalculateButtonWidth(yesLabel);
            int noWidth = CalculateButtonWidth(noLabel);
            int buttonSpacing = 20;
            int totalButtonsWidth = yesWidth + buttonSpacing + noWidth;
            int topPadding = 28;
            int questionToButtons = 30;
            int bottomPadding = 24;
            int dialogWidth = Math.Max((int)questionSize.X + 60, totalButtonsWidth + 60);
            int dialogHeight = topPadding + (int)questionSize.Y + questionToButtons + TabAndButtonHeight + bottomPadding;
            int dialogX = (Game1.uiViewport.Width - dialogWidth) / 2;
            int dialogY = (Game1.uiViewport.Height - dialogHeight) / 2;
            var dialogBounds = new Rectangle(dialogX, dialogY, dialogWidth, dialogHeight);
            int buttonsY = dialogY + topPadding + (int)questionSize.Y + questionToButtons;
            int buttonsStartX = dialogX + (dialogWidth - totalButtonsWidth) / 2;
            var yes = new ClickableComponent(new Rectangle(buttonsStartX, buttonsY, yesWidth, TabAndButtonHeight), "yes");
            var no = new ClickableComponent(new Rectangle(buttonsStartX + yesWidth + buttonSpacing, buttonsY, noWidth, TabAndButtonHeight), "no");
            return (dialogBounds, yes, no);
        }

        public static void DrawDeleteConfirmationDialog(SpriteBatch b, Rectangle dialogBounds,
            string questionText, ClickableComponent yesButton, string yesLabel,
            ClickableComponent noButton, string noLabel)
        {
            b.Draw(Game1.fadeToBlackRect,
                new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
                Color.Black * 0.2f);
            DrawTextureBox(b, dialogBounds.X, dialogBounds.Y, dialogBounds.Width, dialogBounds.Height, Color.White);
            Vector2 textSize = Game1.dialogueFont.MeasureString(questionText);
            Vector2 textPos = new Vector2(
                dialogBounds.X + (dialogBounds.Width - textSize.X) / 2,
                dialogBounds.Y + 28);
            Utility.drawTextWithShadow(b, questionText, Game1.dialogueFont, textPos, Game1.textColor);
            DrawDeleteDialogButton(b, yesButton, yesLabel, Color.LightGreen);
            DrawDeleteDialogButton(b, noButton, noLabel, Color.Red);
        }

        public static void DrawWrappedTooltip(SpriteBatch b, string text, int wrapWidth = 300)
        {
            string wrapped = text.Contains(' ')
                ? Game1.parseText(text, Game1.smallFont, wrapWidth)
                : text;

            string[] lines = wrapped.Split('\n');
            int lineHeight = Game1.smallFont.LineSpacing;
            int lineGap = 1;
            int totalTextHeight = lines.Length * lineHeight + (lines.Length - 1) * lineGap;

            float maxLineWidth = 0;
            foreach (string line in lines)
                maxLineWidth = Math.Max(maxLineWidth, Game1.smallFont.MeasureString(line).X);

            int width = (int)maxLineWidth + 32;
            int height = Math.Max(60, totalTextHeight + 32 + 4);

            int x = Game1.getOldMouseX() + 32;
            int y = Game1.getOldMouseY() + 32;

            Rectangle safeArea = Utility.getSafeArea();
            if (x + width > safeArea.Right)
                x = safeArea.Right - width;
            if (y + height > safeArea.Bottom)
                y = safeArea.Bottom - height;
            if (x < safeArea.Left)
                x = safeArea.Left;
            if (y < safeArea.Top)
                y = safeArea.Top;

            IClickableMenu.drawTextureBox(b, Game1.menuTexture, MenuBoxSourceRect,
                x, y, width, height, Color.White, drawShadow: true);

            int textY = y + 16;
            foreach (string line in lines)
            {
                Utility.drawTextWithShadow(b, line, Game1.smallFont,
                    new Vector2(x + 20, textY), Game1.textColor);
                textY += lineHeight + lineGap;
            }
        }

        public static void DrawItemInSlot(SpriteBatch b, Rectangle slot, Item item, float transparency = 1f)
        {
            Vector2 drawPos = new Vector2(
                slot.X + (slot.Width - DrawnItemSize) / 2,
                slot.Y + (slot.Height - DrawnItemSize) / 2);
            item.drawInMenu(b, drawPos, 1f, transparency, 0.9f, StackDrawType.Hide);
        }

        public static void SafeDispose(ref RenderTarget2D? renderTarget, ref SpriteBatch? spriteBatch)
        {
            if (renderTarget != null && !renderTarget.IsDisposed)
            {
                renderTarget.Dispose();
                renderTarget = null;
            }
            if (spriteBatch != null && !spriteBatch.IsDisposed)
            {
                spriteBatch.Dispose();
                spriteBatch = null;
            }
        }

        public static void DrawDeleteDialogButton(SpriteBatch b, ClickableComponent button,
            string label, Color hoverColor)
        {
            bool isHovered = button.containsPoint(Game1.getMouseX(), Game1.getMouseY());
            DrawTextureBox(b, button.bounds.X, button.bounds.Y, button.bounds.Width, button.bounds.Height, Color.White);
            if (isHovered)
                b.Draw(Game1.staminaRect, button.bounds, hoverColor * 0.2f);
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
    }
}
