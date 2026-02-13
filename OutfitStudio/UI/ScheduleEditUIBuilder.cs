using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    public class ScheduleEditUIBuilder
    {
        private static readonly Rectangle CheckedSourceRect = new Rectangle(236, 425, 9, 9);
        private static readonly Rectangle UncheckedSourceRect = new Rectangle(227, 425, 9, 9);
        private static readonly Rectangle UpScrollArrowSourceRect = new Rectangle(421, 459, 11, 12);
        private static readonly Rectangle DownScrollArrowSourceRect = new Rectangle(421, 472, 11, 12);

        public ClickableTextureComponent CloseButton { get; private set; } = null!;

        // Trigger dropdown bars
        public ClickableComponent SeasonsDropdownBar { get; private set; } = null!;
        public ClickableComponent WeatherDropdownBar { get; private set; } = null!;
        public ClickableComponent AreasDropdownBar { get; private set; } = null!;
        public ClickableComponent FestivalsDropdownBar { get; private set; } = null!;
        public ClickableComponent LocationsDropdownBar { get; private set; } = null!;

        // Clear buttons
        public ClickableComponent SeasonsClearButton { get; private set; } = null!;
        public ClickableComponent WeatherClearButton { get; private set; } = null!;
        public ClickableComponent AreasClearButton { get; private set; } = null!;
        public ClickableComponent FestivalsClearButton { get; private set; } = null!;
        public ClickableComponent LocationsClearButton { get; private set; } = null!;

        // Wedding checkbox
        public ClickableComponent WeddingCheckbox { get; private set; } = null!;

        // Name input
        public Rectangle NameInputBounds { get; private set; }
        public ClickableComponent NameClearButton { get; private set; } = null!;

        // Priority dropdown
        public ClickableComponent PriorityDropdownBar { get; private set; } = null!;

        // Advance queue dropdown
        public ClickableComponent AdvanceQueueDropdownBar { get; private set; } = null!;

        // Tags/Sets buttons
        public ClickableComponent TagsAddButton { get; private set; } = null!;
        public ClickableComponent SetsAddButton { get; private set; } = null!;

        // Bottom buttons
        public ClickableComponent PreviewButton { get; private set; } = null!;
        public ClickableComponent CancelButton { get; private set; } = null!;

        public Rectangle ContentBoxBounds { get; private set; }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public int LeftColLabelX { get; private set; }
        public int RightColLabelX { get; private set; }

        private int contentX;
        private int contentWidth;

        private int nameRowY;
        private int totalOutfitsRowY;
        private int triggersHeaderY;
        private int tagsRowY;
        private int setsRowY;
        private int priorityRowY;
        private int advanceQueueRowY;
        private int optionsLabelX;
        private int weddingRowY;

        public ScheduleEditUIBuilder()
        {
            Width = ScheduleEditWidth;
        }

        public void Recalculate()
        {
            int contentInner = ScheduleBorderPadding
                + ScheduleEditRowHeight              // name input
                + ScheduleEditRowHeight              // total outfits
                + ScheduleEditSectionGap
                + ConfigSectionHeaderHeight          // "Triggers"
                + ScheduleEditRowHeight * 3          // 3 trigger rows
                + ScheduleEditTagRowPaddingV         // padding above Tags row
                + ScheduleEditRowHeight              // Tags row
                + ScheduleEditRowHeight              // Sets row
                + ScheduleEditTagRowPaddingV         // padding below Sets row
                + ScheduleEditRowHeight              // priority dropdown
                + ScheduleEditRowHeight              // advance queue
                + ScheduleEditSectionGap
                + TabAndButtonHeight                 // buttons row
                + ScheduleBorderPadding;

            Height = contentInner;
            X = (Game1.uiViewport.Width - Width) / 2;
            Y = (Game1.uiViewport.Height - Height) / 2;

            ContentBoxBounds = new Rectangle(X, Y, Width, Height);

            CalculateLayout();
        }

        private void CalculateLayout()
        {
            contentX = ContentBoxBounds.X + ScheduleBorderPadding;
            contentWidth = ContentBoxBounds.Width - ScheduleBorderPadding * 2;

            int currentY = ContentBoxBounds.Y + ScheduleBorderPadding;

            // Name input row
            nameRowY = currentY;
            int nameInputWidth = contentWidth - 60;
            int nameInputX = contentX + (contentWidth - nameInputWidth) / 2;
            int nameInputY = nameRowY + (ScheduleEditRowHeight - TabAndButtonHeight) / 2;
            NameInputBounds = new Rectangle(nameInputX, nameInputY, nameInputWidth, TabAndButtonHeight);
            NameClearButton = new ClickableComponent(
                new Rectangle(NameInputBounds.Right - ClearButtonSize - 4,
                    NameInputBounds.Y + (NameInputBounds.Height - ClearButtonSize) / 2,
                    ClearButtonSize, ClearButtonSize),
                "NameClear");
            currentY += ScheduleEditRowHeight;

            // Total outfits row
            totalOutfitsRowY = currentY;
            currentY += ScheduleEditRowHeight + ScheduleEditSectionGap;

            // Triggers header
            triggersHeaderY = currentY;
            currentY += ConfigSectionHeaderHeight;

            // 2-column trigger layout
            int triggerX = contentX + ScheduleEditTriggerIndent;
            int triggerAvail = contentWidth - ScheduleEditTriggerIndent;
            int leftColWidth = (triggerAvail - ScheduleEditColumnGap) / 2;
            int rightColStartX = triggerX + leftColWidth + ScheduleEditColumnGap;

            LeftColLabelX = triggerX;
            RightColLabelX = rightColStartX;

            int labelW = ScheduleEditTriggerLabelWidth;
            int leftDropdownX = triggerX + labelW;
            int leftDropdownW = leftColWidth - labelW;
            int rightDropdownX = rightColStartX + labelW;
            int rightDropdownW = leftColWidth - labelW;

            int barVOffset = (ScheduleEditRowHeight - TabAndButtonHeight) / 2;

            // Row 1: Season (left) | Weather (right)
            SeasonsDropdownBar = new ClickableComponent(
                new Rectangle(leftDropdownX, currentY + barVOffset, leftDropdownW, TabAndButtonHeight),
                "SeasonsDropdown");
            SeasonsClearButton = CreateClearButton(SeasonsDropdownBar);

            WeatherDropdownBar = new ClickableComponent(
                new Rectangle(rightDropdownX, currentY + barVOffset, rightDropdownW, TabAndButtonHeight),
                "WeatherDropdown");
            WeatherClearButton = CreateClearButton(WeatherDropdownBar);
            currentY += ScheduleEditRowHeight;

            // Row 2: Area (left) | Festival (right)
            AreasDropdownBar = new ClickableComponent(
                new Rectangle(leftDropdownX, currentY + barVOffset, leftDropdownW, TabAndButtonHeight),
                "AreasDropdown");
            AreasClearButton = CreateClearButton(AreasDropdownBar);

            FestivalsDropdownBar = new ClickableComponent(
                new Rectangle(rightDropdownX, currentY + barVOffset, rightDropdownW, TabAndButtonHeight),
                "FestivalsDropdown");
            FestivalsClearButton = CreateClearButton(FestivalsDropdownBar);
            currentY += ScheduleEditRowHeight;

            // Row 3: Location (left) | Wedding checkbox (right)
            weddingRowY = currentY;
            LocationsDropdownBar = new ClickableComponent(
                new Rectangle(leftDropdownX, currentY + barVOffset, leftDropdownW, TabAndButtonHeight),
                "LocationsDropdown");
            LocationsClearButton = CreateClearButton(LocationsDropdownBar);

            int weddingCheckY = currentY + (ScheduleEditRowHeight - ScheduleCheckboxSize) / 2;
            WeddingCheckbox = new ClickableComponent(
                new Rectangle(rightColStartX, weddingCheckY, ScheduleCheckboxSize, ScheduleCheckboxSize),
                "Wedding");
            currentY += ScheduleEditRowHeight + ScheduleEditTagRowPaddingV;

            // Tags row (center-aligned block)
            tagsRowY = currentY;
            currentY += ScheduleEditRowHeight;

            // Sets row
            setsRowY = currentY;
            currentY += ScheduleEditRowHeight + ScheduleEditTagRowPaddingV;

            CalculateTagsSetsCenteredLayout();

            // Priority dropdown row
            priorityRowY = currentY;
            currentY += ScheduleEditRowHeight;

            // Advance queue row
            advanceQueueRowY = currentY;
            currentY += ScheduleEditRowHeight + ScheduleEditSectionGap;

            CalculateOptionsLayout();

            // Bottom buttons (Preview + Cancel)
            int previewWidth = UIHelpers.CalculateButtonWidth(TranslationCache.ScheduleEditPreview);
            int cancelWidth = UIHelpers.CalculateButtonWidth(TranslationCache.CommonCancel);
            int totalBtnWidth = previewWidth + ScheduleBottomButtonGap + cancelWidth;
            int btnStartX = X + (Width - totalBtnWidth) / 2;

            PreviewButton = new ClickableComponent(
                new Rectangle(btnStartX, currentY, previewWidth, TabAndButtonHeight),
                "Preview");
            CancelButton = new ClickableComponent(
                new Rectangle(btnStartX + previewWidth + ScheduleBottomButtonGap, currentY, cancelWidth, TabAndButtonHeight),
                "Cancel");

            // Close X button
            CloseButton = new ClickableTextureComponent(
                new Rectangle(X + Width - CloseButtonSize - CloseButtonEdgeMargin,
                    Y + CloseButtonEdgeMargin, CloseButtonSize, CloseButtonSize),
                Game1.mouseCursors,
                new Rectangle(337, 494, 12, 12),
                4f);
        }

        private void CalculateTagsSetsCenteredLayout()
        {
            string tagsLabel = TranslationCache.ScheduleEditTags;
            string setsLabel = TranslationCache.ScheduleEditSets;

            int tagsLabelW = (int)Game1.smallFont.MeasureString(tagsLabel).X;
            int setsLabelW = (int)Game1.smallFont.MeasureString(setsLabel).X;
            int maxLabelW = Math.Max(tagsLabelW, setsLabelW);

            int addBtnSize = SmallButtonHeight;
            int gap = 8;
            int blockWidth = maxLabelW + gap + addBtnSize;
            int blockX = ContentBoxBounds.X + (ContentBoxBounds.Width - blockWidth) / 2;
            int addBtnX = blockX + maxLabelW + gap;

            int tagsAddBtnY = tagsRowY + (ScheduleEditRowHeight - addBtnSize) / 2;
            TagsAddButton = new ClickableComponent(
                new Rectangle(addBtnX, tagsAddBtnY, addBtnSize, addBtnSize),
                "TagsAdd");

            int setsAddBtnY = setsRowY + (ScheduleEditRowHeight - addBtnSize) / 2;
            SetsAddButton = new ClickableComponent(
                new Rectangle(addBtnX, setsAddBtnY, addBtnSize, addBtnSize),
                "SetsAdd");
        }

        private void CalculateOptionsLayout()
        {
            int priorityLabelW = (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditPriority).X;
            int advanceLabelW = (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditAdvanceQueue).X;

            int maxLabelW = Math.Max(priorityLabelW, advanceLabelW);
            const int labelToControlGap = 12;

            // Control widths
            int priorityTextWidth = Math.Max(
                Math.Max(
                    (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditPriorityHigh).X,
                    (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditPriorityMedium).X),
                (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditPriorityLow).X);
            int priorityBarWidth = priorityTextWidth + 40;

            int advanceTextWidth = Math.Max(
                (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditAdvanceQueueOnceADay).X,
                (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditAdvanceQueueOnLocationChange).X);
            int advanceBarWidth = Math.Min(advanceTextWidth, 200) + 40;

            // Find widest row, center the group
            int maxControlWidth = Math.Max(priorityBarWidth, advanceBarWidth);
            int groupWidth = maxLabelW + labelToControlGap + maxControlWidth;
            int groupX = ContentBoxBounds.X + (ContentBoxBounds.Width - groupWidth) / 2;
            optionsLabelX = groupX;
            int controlX = groupX + maxLabelW + labelToControlGap;

            // Priority dropdown
            int priorityBarY = priorityRowY + (ScheduleEditRowHeight - TabAndButtonHeight) / 2;
            PriorityDropdownBar = new ClickableComponent(
                new Rectangle(controlX, priorityBarY, priorityBarWidth, TabAndButtonHeight),
                "PriorityDropdown");

            // Advance queue dropdown
            int advanceBarY = advanceQueueRowY + (ScheduleEditRowHeight - TabAndButtonHeight) / 2;
            AdvanceQueueDropdownBar = new ClickableComponent(
                new Rectangle(controlX, advanceBarY, advanceBarWidth, TabAndButtonHeight),
                "AdvanceQueueDropdown");
        }

        private ClickableComponent CreateClearButton(ClickableComponent bar)
        {
            int clearX = bar.bounds.Right - ClearButtonSize - 4;
            int clearY = bar.bounds.Y + (bar.bounds.Height - ClearButtonSize) / 2;
            return new ClickableComponent(
                new Rectangle(clearX, clearY, ClearButtonSize, ClearButtonSize),
                bar.name + "Clear");
        }

        public void BuildDropdownOptions(ClickableComponent bar, List<string> displayNames, int scrollIndex, List<ClickableComponent> outOptions)
        {
            outOptions.Clear();
            int maxVisible = ScheduleEditDropdownMaxVisible;
            int count = Math.Min(displayNames.Count - scrollIndex, maxVisible);

            for (int i = 0; i < count; i++)
            {
                int dataIndex = scrollIndex + i;
                outOptions.Add(new ClickableComponent(
                    new Rectangle(bar.bounds.X,
                        bar.bounds.Bottom + ScheduleEditDropdownPanelPadding + i * ScheduleEditDropdownOptionHeight,
                        bar.bounds.Width, ScheduleEditDropdownOptionHeight),
                    displayNames[dataIndex]
                ));
            }
        }

        public void DrawTotalOutfitsRow(SpriteBatch b, int count, int remaining)
        {
            string text = $"{TranslationCache.ScheduleEditTotalOutfits}: {count}";
            text += $"   \u00b7   {remaining} {TranslationCache.ScheduleEditRemainingBeforeReset}";

            float textHeight = Game1.smallFont.MeasureString(text).Y;
            int textY = totalOutfitsRowY + (int)((ScheduleEditRowHeight - textHeight) / 2);
            Utility.drawTextWithShadow(b, text, Game1.smallFont,
                new Vector2(contentX, textY), Game1.textColor);
        }

        public void DrawTriggersHeader(SpriteBatch b)
        {
            string text = TranslationCache.ScheduleEditTriggers;
            float textHeight = Game1.smallFont.MeasureString(text).Y;
            int textY = triggersHeaderY + (int)((ConfigSectionHeaderHeight - textHeight) / 2);
            Vector2 pos = new Vector2(contentX, textY);
            Utility.drawTextWithShadow(b, text, Game1.smallFont, pos, Game1.textColor);
            Utility.drawTextWithShadow(b, text, Game1.smallFont, pos + new Vector2(1, 0), Game1.textColor);
        }

        public void DrawDropdownBar(SpriteBatch b, ClickableComponent bar, string label, string displayText,
            bool isOpen, int labelX, bool hasValue = false, ClickableComponent? clearButton = null)
        {
            // Label
            int rowY = bar.bounds.Y - (ScheduleEditRowHeight - TabAndButtonHeight) / 2;
            float labelHeight = Game1.smallFont.MeasureString(label).Y;
            int labelY = rowY + (int)((ScheduleEditRowHeight - labelHeight) / 2);
            Utility.drawTextWithShadow(b, label, Game1.smallFont,
                new Vector2(labelX, labelY), Game1.textColor);

            // Box
            UIHelpers.DrawTextureBox(b, bar.bounds.X, bar.bounds.Y,
                bar.bounds.Width, bar.bounds.Height,
                isOpen ? Color.Wheat : Color.White);

            // Text (reserve space for clear button when value exists)
            int reservedRight = 20;
            if (hasValue && clearButton != null)
                reservedRight = ClearButtonSize + 8;
            int maxTextWidth = bar.bounds.Width - reservedRight - 20;
            string truncated = UIHelpers.TruncateText(displayText, maxTextWidth);

            Vector2 textSize = Game1.smallFont.MeasureString(truncated);
            Vector2 textPos = new Vector2(
                bar.bounds.X + 20,
                bar.bounds.Y + (bar.bounds.Height - textSize.Y) / 2
            );

            bool isHovered = bar.containsPoint(Game1.getMouseX(), Game1.getMouseY()) && !isOpen;
            if (isHovered)
            {
                Utility.drawTextWithShadow(b, truncated, Game1.smallFont, textPos + new Vector2(-1, 0), Game1.textColor * 0.8f);
                Utility.drawTextWithShadow(b, truncated, Game1.smallFont, textPos, Game1.textColor);
            }
            else
            {
                Utility.drawTextWithShadow(b, truncated, Game1.smallFont, textPos, Game1.textColor);
            }

            // Clear button
            if (hasValue && clearButton != null)
                UIHelpers.DrawClearButton(b, clearButton);
        }

        public string? DrawMultiSelectDropdown(
            SpriteBatch b,
            ClickableComponent bar,
            List<ClickableComponent> visibleOptions,
            int totalCount,
            int scrollIndex,
            Func<int, bool> isChecked)
        {
            if (visibleOptions.Count == 0)
                return null;

            string? hoveredTruncatedText = null;
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            int optionsHeight = visibleOptions.Count * ScheduleEditDropdownOptionHeight;
            int dropdownHeight = optionsHeight + ScheduleEditDropdownPanelPadding * 2;

            UIHelpers.DrawTextureBoxNoShadow(b,
                bar.bounds.X - 4,
                bar.bounds.Bottom - 4,
                bar.bounds.Width + 8,
                dropdownHeight + 8,
                Color.White);

            int maxTextWidth = bar.bounds.Width - 20 - ScheduleCheckboxSize - 8 - 20;

            for (int i = 0; i < visibleOptions.Count; i++)
            {
                var option = visibleOptions[i];
                int dataIndex = scrollIndex + i;
                bool isCheckedVal = isChecked(dataIndex);
                bool isHovered = option.containsPoint(mouseX, mouseY);

                if (isHovered)
                    b.Draw(Game1.staminaRect, option.bounds, Color.Wheat * 0.6f);

                Rectangle srcRect = isCheckedVal ? CheckedSourceRect : UncheckedSourceRect;
                int checkX = option.bounds.X + 12;
                int checkY = option.bounds.Y + (option.bounds.Height - ScheduleCheckboxSize) / 2;
                b.Draw(Game1.mouseCursors, new Vector2(checkX, checkY),
                    srcRect, Color.White, 0f, Vector2.Zero, ScheduleCheckboxScale, SpriteEffects.None, 1f);

                string fullText = option.name;
                string displayText = UIHelpers.TruncateText(fullText, maxTextWidth);
                bool isTruncated = displayText != fullText;

                Vector2 textSize = Game1.smallFont.MeasureString(displayText);
                Vector2 textPos = new Vector2(
                    checkX + ScheduleCheckboxSize + 8,
                    option.bounds.Y + (option.bounds.Height - textSize.Y) / 2
                );
                Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPos, Game1.textColor);

                if (isHovered && isTruncated)
                    hoveredTruncatedText = fullText;
            }

            // Scroll arrows
            if (scrollIndex > 0)
            {
                Vector2 arrowPos = new Vector2(bar.bounds.Right - 24,
                    bar.bounds.Bottom + ScheduleEditDropdownPanelPadding + 8);
                b.Draw(Game1.mouseCursors, arrowPos, UpScrollArrowSourceRect,
                    Color.White, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 1f);
            }
            if (scrollIndex + ScheduleEditDropdownMaxVisible < totalCount)
            {
                Vector2 arrowPos = new Vector2(bar.bounds.Right - 24,
                    bar.bounds.Bottom + ScheduleEditDropdownPanelPadding + optionsHeight - 26);
                b.Draw(Game1.mouseCursors, arrowPos, DownScrollArrowSourceRect,
                    Color.White, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 1f);
            }

            return hoveredTruncatedText;
        }

        public void DrawWeddingRow(SpriteBatch b, bool isChecked)
        {
            Rectangle srcRect = isChecked ? CheckedSourceRect : UncheckedSourceRect;
            b.Draw(Game1.mouseCursors, new Vector2(WeddingCheckbox.bounds.X, WeddingCheckbox.bounds.Y),
                srcRect, Color.White, 0f, Vector2.Zero, ScheduleCheckboxScale, SpriteEffects.None, 1f);

            string label = TranslationCache.ScheduleEditWedding;
            float textHeight = Game1.smallFont.MeasureString(label).Y;
            int textX = WeddingCheckbox.bounds.Right + ScheduleCheckboxGap;
            int textY = weddingRowY + (int)((ScheduleEditRowHeight - textHeight) / 2);
            Utility.drawTextWithShadow(b, label, Game1.smallFont,
                new Vector2(textX, textY), Game1.textColor);
        }

        public void DrawTagsRow(SpriteBatch b, int tagCount)
        {
            string label = TranslationCache.ScheduleEditTags;
            int labelX = TagsAddButton.bounds.X - 8 - (int)Game1.smallFont.MeasureString(label).X;
            float textHeight = Game1.smallFont.MeasureString(label).Y;
            int textY = tagsRowY + (int)((ScheduleEditRowHeight - textHeight) / 2);

            string displayText = tagCount > 0 ? $"{label} ({tagCount})" : label;
            labelX = TagsAddButton.bounds.X - 8 - (int)Game1.smallFont.MeasureString(displayText).X;

            Utility.drawTextWithShadow(b, displayText, Game1.smallFont,
                new Vector2(labelX, textY), Game1.textColor);

            DrawAddButton(b, TagsAddButton);
        }

        public void DrawSetsRow(SpriteBatch b, int includedCount)
        {
            string label = TranslationCache.ScheduleEditSets;
            float textHeight = Game1.smallFont.MeasureString(label).Y;
            int textY = setsRowY + (int)((ScheduleEditRowHeight - textHeight) / 2);

            string displayText = includedCount > 0 ? $"{label} ({includedCount})" : label;
            int labelX = SetsAddButton.bounds.X - 8 - (int)Game1.smallFont.MeasureString(displayText).X;

            Utility.drawTextWithShadow(b, displayText, Game1.smallFont,
                new Vector2(labelX, textY), Game1.textColor);

            DrawAddButton(b, SetsAddButton);
        }

        private void DrawAddButton(SpriteBatch b, ClickableComponent button)
        {
            bool isHovered = button.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            UIHelpers.DrawTextureBox(b, button.bounds.X, button.bounds.Y,
                button.bounds.Width, button.bounds.Height, Color.White,
                shadowOffset: 2, shadowOpacity: 0.3f);

            if (isHovered)
                b.Draw(Game1.staminaRect, button.bounds, HoverEffectColor);

            Vector2 plusSize = Game1.smallFont.MeasureString("+");
            Vector2 plusPos = UIHelpers.GetVisualCenter(button.bounds, plusSize);
            Utility.drawTextWithShadow(b, "+", Game1.smallFont, plusPos, Game1.textColor);
        }

        public void DrawNameInput(SpriteBatch b, string text, bool hasFocus)
        {
            UIHelpers.DrawTextureBox(b, NameInputBounds.X, NameInputBounds.Y,
                NameInputBounds.Width, NameInputBounds.Height,
                hasFocus ? Color.Wheat : Color.White);

            bool showPlaceholder = string.IsNullOrEmpty(text);
            string displayText = showPlaceholder ? TranslationCache.ScheduleEditNamePlaceholder : text;
            int maxTextWidth = NameInputBounds.Width - 20 - ClearButtonSize - 8;
            displayText = UIHelpers.TruncateText(displayText, maxTextWidth);
            Color textColor = showPlaceholder ? Color.Gray : Game1.textColor;
            float textHeight = Game1.smallFont.MeasureString("A").Y;
            Vector2 textPosition = new Vector2(NameInputBounds.X + 20, NameInputBounds.Y + (NameInputBounds.Height - textHeight) / 2);
            Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPosition, textColor);

            if (!string.IsNullOrEmpty(text))
                UIHelpers.DrawClearButton(b, NameClearButton);
        }

        public void DrawNameCursor(SpriteBatch b, string currentText, bool isSelected)
        {
            if (!isSelected)
                return;

            Vector2 textSize = string.IsNullOrEmpty(currentText)
                ? Vector2.Zero
                : Game1.smallFont.MeasureString(currentText);

            Vector2 textPosition = new Vector2(NameInputBounds.X + 20, NameInputBounds.Y + (NameInputBounds.Height - 32) / 2);
            Vector2 cursorPosition = new Vector2(textPosition.X + textSize.X, textPosition.Y);

            bool showCursor = Game1.currentGameTime != null
                && (int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 500) % 2 == 0;

            if (showCursor)
                b.Draw(Game1.staminaRect, new Rectangle((int)cursorPosition.X, (int)cursorPosition.Y, 4, 32), Game1.textColor);
        }

        public void DrawPriorityRow(SpriteBatch b, string selectedText, bool isOpen)
        {
            string label = TranslationCache.ScheduleEditPriority;
            float labelHeight = Game1.smallFont.MeasureString(label).Y;
            int labelY = priorityRowY + (int)((ScheduleEditRowHeight - labelHeight) / 2);
            Utility.drawTextWithShadow(b, label, Game1.smallFont,
                new Vector2(optionsLabelX, labelY), Game1.textColor);

            UIHelpers.DrawTextureBox(b, PriorityDropdownBar.bounds.X, PriorityDropdownBar.bounds.Y,
                PriorityDropdownBar.bounds.Width, PriorityDropdownBar.bounds.Height,
                isOpen ? Color.Wheat : Color.White);

            string truncated = UIHelpers.TruncateText(selectedText, PriorityDropdownBar.bounds.Width - 40);
            Vector2 textSize = Game1.smallFont.MeasureString(truncated);
            Vector2 textPos = new Vector2(
                PriorityDropdownBar.bounds.X + 20,
                PriorityDropdownBar.bounds.Y + (PriorityDropdownBar.bounds.Height - textSize.Y) / 2);

            bool isHovered = PriorityDropdownBar.containsPoint(Game1.getMouseX(), Game1.getMouseY()) && !isOpen;
            if (isHovered)
            {
                Utility.drawTextWithShadow(b, truncated, Game1.smallFont, textPos + new Vector2(-1, 0), Game1.textColor * 0.8f);
                Utility.drawTextWithShadow(b, truncated, Game1.smallFont, textPos, Game1.textColor);
            }
            else
            {
                Utility.drawTextWithShadow(b, truncated, Game1.smallFont, textPos, Game1.textColor);
            }
        }

        public void DrawAdvanceQueueRow(SpriteBatch b, string selectedText, bool isOpen)
        {
            string label = TranslationCache.ScheduleEditAdvanceQueue;
            float labelHeight = Game1.smallFont.MeasureString(label).Y;
            int labelY = advanceQueueRowY + (int)((ScheduleEditRowHeight - labelHeight) / 2);
            Utility.drawTextWithShadow(b, label, Game1.smallFont,
                new Vector2(optionsLabelX, labelY), Game1.textColor);

            // Dropdown bar
            UIHelpers.DrawTextureBox(b, AdvanceQueueDropdownBar.bounds.X, AdvanceQueueDropdownBar.bounds.Y,
                AdvanceQueueDropdownBar.bounds.Width, AdvanceQueueDropdownBar.bounds.Height,
                isOpen ? Color.Wheat : Color.White);

            string truncated = UIHelpers.TruncateText(selectedText, AdvanceQueueDropdownBar.bounds.Width - 40);
            Vector2 textSize = Game1.smallFont.MeasureString(truncated);
            Vector2 textPos = new Vector2(
                AdvanceQueueDropdownBar.bounds.X + 20,
                AdvanceQueueDropdownBar.bounds.Y + (AdvanceQueueDropdownBar.bounds.Height - textSize.Y) / 2);

            bool isHovered = AdvanceQueueDropdownBar.containsPoint(Game1.getMouseX(), Game1.getMouseY()) && !isOpen;
            if (isHovered)
            {
                Utility.drawTextWithShadow(b, truncated, Game1.smallFont, textPos + new Vector2(-1, 0), Game1.textColor * 0.8f);
                Utility.drawTextWithShadow(b, truncated, Game1.smallFont, textPos, Game1.textColor);
            }
            else
            {
                Utility.drawTextWithShadow(b, truncated, Game1.smallFont, textPos, Game1.textColor);
            }
        }

        public void DrawSingleSelectDropdownPanel(SpriteBatch b, ClickableComponent bar, List<ClickableComponent> options)
        {
            if (options.Count == 0)
                return;

            int optionsHeight = options.Count * ScheduleEditDropdownOptionHeight;
            int dropdownHeight = optionsHeight + ScheduleEditDropdownPanelPadding * 2;

            UIHelpers.DrawTextureBoxNoShadow(b,
                bar.bounds.X - 4,
                bar.bounds.Bottom - 4,
                bar.bounds.Width + 8,
                dropdownHeight + 8,
                Color.White);

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                bool isHovered = option.containsPoint(mouseX, mouseY);

                if (isHovered)
                    b.Draw(Game1.staminaRect, option.bounds, Color.Wheat * 0.6f);

                string text = UIHelpers.TruncateText(option.name, option.bounds.Width - 40);
                Vector2 textSize = Game1.smallFont.MeasureString(text);
                Vector2 textPos = new Vector2(
                    option.bounds.X + 20,
                    option.bounds.Y + (option.bounds.Height - textSize.Y) / 2);
                Utility.drawTextWithShadow(b, text, Game1.smallFont, textPos, Game1.textColor);
            }
        }

        public void BuildPriorityOptions(List<ClickableComponent> outOptions)
        {
            outOptions.Clear();
            string[] texts = {
                TranslationCache.ScheduleEditPriorityHigh,
                TranslationCache.ScheduleEditPriorityMedium,
                TranslationCache.ScheduleEditPriorityLow
            };

            for (int i = 0; i < texts.Length; i++)
            {
                outOptions.Add(new ClickableComponent(
                    new Rectangle(PriorityDropdownBar.bounds.X,
                        PriorityDropdownBar.bounds.Bottom + ScheduleEditDropdownPanelPadding + i * ScheduleEditDropdownOptionHeight,
                        PriorityDropdownBar.bounds.Width, ScheduleEditDropdownOptionHeight),
                    texts[i]));
            }
        }

        public Rectangle GetPriorityDropdownPanelBounds(int optionCount)
        {
            int dropdownHeight = optionCount * ScheduleEditDropdownOptionHeight + ScheduleEditDropdownPanelPadding * 2;
            return new Rectangle(
                PriorityDropdownBar.bounds.X - 4,
                PriorityDropdownBar.bounds.Bottom - 4,
                PriorityDropdownBar.bounds.Width + 8,
                dropdownHeight + 8);
        }

        public void BuildAdvanceQueueOptions(List<ClickableComponent> outOptions)
        {
            outOptions.Clear();
            string[] texts = {
                TranslationCache.ScheduleEditAdvanceQueueOnLocationChange,
                TranslationCache.ScheduleEditAdvanceQueueOnceADay
            };

            for (int i = 0; i < texts.Length; i++)
            {
                outOptions.Add(new ClickableComponent(
                    new Rectangle(AdvanceQueueDropdownBar.bounds.X,
                        AdvanceQueueDropdownBar.bounds.Bottom + ScheduleEditDropdownPanelPadding + i * ScheduleEditDropdownOptionHeight,
                        AdvanceQueueDropdownBar.bounds.Width, ScheduleEditDropdownOptionHeight),
                    texts[i]));
            }
        }

        public Rectangle GetAdvanceQueueDropdownPanelBounds(int optionCount)
        {
            int dropdownHeight = optionCount * ScheduleEditDropdownOptionHeight + ScheduleEditDropdownPanelPadding * 2;
            return new Rectangle(
                AdvanceQueueDropdownBar.bounds.X - 4,
                AdvanceQueueDropdownBar.bounds.Bottom - 4,
                AdvanceQueueDropdownBar.bounds.Width + 8,
                dropdownHeight + 8);
        }

        public void DrawBottomButtons(SpriteBatch b)
        {
            UIHelpers.DrawTextButton(b, PreviewButton, TranslationCache.ScheduleEditPreview);
            UIHelpers.DrawTextButton(b, CancelButton, TranslationCache.CommonCancel);
        }

        public void DrawCloseButton(SpriteBatch b)
        {
            UIHelpers.DrawTextureButton(b, CloseButton);
        }

        public Rectangle GetDropdownPanelBounds(ClickableComponent bar, int visibleCount)
        {
            int dropdownHeight = visibleCount * ScheduleEditDropdownOptionHeight + ScheduleEditDropdownPanelPadding * 2;
            return new Rectangle(
                bar.bounds.X - 4,
                bar.bounds.Bottom - 4,
                bar.bounds.Width + 8,
                dropdownHeight + 8
            );
        }
    }
}
