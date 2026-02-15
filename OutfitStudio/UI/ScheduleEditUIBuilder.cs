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
        public ClickableTextureComponent CloseButton { get; private set; } = null!;

        // Condition dropdown bars
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

        // Priority dropdown
        public ClickableComponent PriorityDropdownBar { get; private set; } = null!;

        // Rotate dropdown
        public ClickableComponent RotateDropdownBar { get; private set; } = null!;

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

        public int OptionsLabelRightX { get; private set; }
        public int RightColBarX { get; private set; }

        private int contentX;
        private int contentWidth;

        private int totalOutfitsRowY;
        private int conditionsHeaderY;
        private int specialEventsHeaderY;
        private int tagsRowY;
        private int setsRowY;
        private int priorityRowY;
        private int rotateRowY;

        public ScheduleEditUIBuilder()
        {
            Width = ScheduleEditWidth;
        }

        public void Recalculate()
        {
            int contentInner = ScheduleBorderPadding
                + ScheduleEditRowHeight                    // total outfits
                + ScheduleEditSectionGap
                + ScheduleEditSectionHeaderHeight          // "Conditions"
                + TabAndButtonHeight                       // Season|Weather bars
                + ScheduleEditBarRowGap
                + TabAndButtonHeight                       // Area|Location bars
                + ScheduleEditSectionGap
                + ScheduleEditSectionHeaderHeight          // "Special Events"
                + TabAndButtonHeight                       // Festival|Wedding row
                + ScheduleEditBarRowGap
                + ScheduleEditRowHeight                    // Tags row
                + ScheduleEditOptionRowGap
                + ScheduleEditRowHeight                    // Sets row
                + ScheduleEditOptionRowGap
                + TabAndButtonHeight                       // Priority bar
                + ScheduleEditOptionRowGap
                + TabAndButtonHeight                       // Rotate bar
                + ScheduleEditSectionGap
                + TabAndButtonHeight                       // buttons row
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

            // Total outfits row
            totalOutfitsRowY = currentY;
            currentY += ScheduleEditRowHeight + ScheduleEditSectionGap;

            // Conditions header
            conditionsHeaderY = currentY;
            currentY += ScheduleEditSectionHeaderHeight;

            // 2-column condition layout (full width, no labels)
            int triggerX = contentX + ScheduleEditTriggerIndent;
            int triggerAvail = contentWidth - ScheduleEditTriggerIndent;
            int colWidth = (triggerAvail - ScheduleEditColumnGap) / 2;
            int rightColX = triggerX + colWidth + ScheduleEditColumnGap;
            RightColBarX = rightColX;

            // Row 1: Season (left) | Weather (right)
            SeasonsDropdownBar = new ClickableComponent(
                new Rectangle(triggerX, currentY, colWidth, TabAndButtonHeight),
                "SeasonsDropdown");
            SeasonsClearButton = CreateClearButton(SeasonsDropdownBar);

            WeatherDropdownBar = new ClickableComponent(
                new Rectangle(rightColX, currentY, colWidth, TabAndButtonHeight),
                "WeatherDropdown");
            WeatherClearButton = CreateClearButton(WeatherDropdownBar);
            currentY += TabAndButtonHeight + ScheduleEditBarRowGap;

            // Row 2: Area (left) | Location (right)
            AreasDropdownBar = new ClickableComponent(
                new Rectangle(triggerX, currentY, colWidth, TabAndButtonHeight),
                "AreasDropdown");
            AreasClearButton = CreateClearButton(AreasDropdownBar);

            LocationsDropdownBar = new ClickableComponent(
                new Rectangle(rightColX, currentY, colWidth, TabAndButtonHeight),
                "LocationsDropdown");
            LocationsClearButton = CreateClearButton(LocationsDropdownBar);
            currentY += TabAndButtonHeight + ScheduleEditSectionGap;

            // Special Events header
            specialEventsHeaderY = currentY;
            currentY += ScheduleEditSectionHeaderHeight;

            // Row 3: Festival (left) | Wedding checkbox (right)
            FestivalsDropdownBar = new ClickableComponent(
                new Rectangle(triggerX, currentY, colWidth, TabAndButtonHeight),
                "FestivalsDropdown");
            FestivalsClearButton = CreateClearButton(FestivalsDropdownBar);

            int weddingCheckY = currentY + (TabAndButtonHeight - ScheduleCheckboxSize) / 2;
            WeddingCheckbox = new ClickableComponent(
                new Rectangle(rightColX, weddingCheckY, ScheduleCheckboxSize, ScheduleCheckboxSize),
                "Wedding");
            currentY += TabAndButtonHeight + ScheduleEditBarRowGap;

            // Tags row (center-aligned block)
            tagsRowY = currentY;
            currentY += ScheduleEditRowHeight + ScheduleEditOptionRowGap;

            // Sets row
            setsRowY = currentY;
            currentY += ScheduleEditRowHeight + ScheduleEditOptionRowGap;

            CalculateTagsSetsCenteredLayout();

            // Priority dropdown bar
            priorityRowY = currentY;
            currentY += TabAndButtonHeight + ScheduleEditOptionRowGap;

            // Rotate dropdown bar
            rotateRowY = currentY;
            currentY += TabAndButtonHeight + ScheduleEditSectionGap;

            CalculateOptionsLayout();

            // Bottom buttons (Preview + Cancel) — same width
            int maxButtonWidth = (contentWidth - ScheduleBottomButtonGap) / 2;
            int previewWidth = UIHelpers.CalculateButtonWidth(TranslationCache.ScheduleEditPreview, maxButtonWidth);
            int cancelWidth = UIHelpers.CalculateButtonWidth(TranslationCache.CommonCancel, maxButtonWidth);
            int buttonWidth = Math.Max(previewWidth, cancelWidth);
            int totalBtnWidth = buttonWidth + ScheduleBottomButtonGap + buttonWidth;
            int btnStartX = X + (Width - totalBtnWidth) / 2;

            PreviewButton = new ClickableComponent(
                new Rectangle(btnStartX, currentY, buttonWidth, TabAndButtonHeight),
                "Preview");
            CancelButton = new ClickableComponent(
                new Rectangle(btnStartX + buttonWidth + ScheduleBottomButtonGap, currentY, buttonWidth, TabAndButtonHeight),
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
            string tagsLabel = TranslationCache.SaveSetTagsLabel;
            string setsLabel = TranslationCache.ScheduleEditSets;

            int tagsLabelW = (int)Game1.smallFont.MeasureString(tagsLabel).X;
            int setsLabelW = (int)Game1.smallFont.MeasureString(setsLabel).X;
            int maxLabelW = Math.Max(tagsLabelW, setsLabelW);

            int addBtnWidth = UIHelpers.GetToggleButtonWidth();
            int gap = 8;
            int blockWidth = maxLabelW + gap + addBtnWidth;
            int blockX = ContentBoxBounds.X + (ContentBoxBounds.Width - blockWidth) / 2;
            int addBtnX = blockX + maxLabelW + gap;

            int tagsAddBtnY = tagsRowY + (ScheduleEditRowHeight - SmallButtonHeight) / 2;
            TagsAddButton = new ClickableComponent(
                new Rectangle(addBtnX, tagsAddBtnY, addBtnWidth, SmallButtonHeight),
                "TagsAdd");

            int setsAddBtnY = setsRowY + (ScheduleEditRowHeight - SmallButtonHeight) / 2;
            SetsAddButton = new ClickableComponent(
                new Rectangle(addBtnX, setsAddBtnY, addBtnWidth, SmallButtonHeight),
                "SetsAdd");
        }

        private void CalculateOptionsLayout()
        {
            int priorityLabelW = (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditPriority).X;
            int rotateLabelW = (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditRotate).X;
            int maxLabelW = Math.Max(priorityLabelW, rotateLabelW);

            int priorityTextWidth = Math.Max(
                Math.Max(
                    (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditPriorityHigh).X,
                    (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditPriorityMedium).X),
                (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditPriorityLow).X);
            int priorityBarWidth = priorityTextWidth + 40;

            int rotateTextWidth = Math.Max(
                (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditRotateOnceADay).X,
                (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditRotateOnLocationChange).X);
            int rotateBarWidth = Math.Min(rotateTextWidth, 200) + 40;

            int maxControlWidth = Math.Max(priorityBarWidth, rotateBarWidth);
            int groupWidth = maxLabelW + ScheduleEditOptionsLabelToBarGap + maxControlWidth;
            int groupX = ContentBoxBounds.X + (ContentBoxBounds.Width - groupWidth) / 2;
            OptionsLabelRightX = groupX + maxLabelW;
            int controlX = OptionsLabelRightX + ScheduleEditOptionsLabelToBarGap;

            PriorityDropdownBar = new ClickableComponent(
                new Rectangle(controlX, priorityRowY, priorityBarWidth, TabAndButtonHeight),
                "PriorityDropdown");

            RotateDropdownBar = new ClickableComponent(
                new Rectangle(controlX, rotateRowY, rotateBarWidth, TabAndButtonHeight),
                "RotateDropdown");
        }

        private ClickableComponent CreateClearButton(ClickableComponent bar)
        {
            int clearX = bar.bounds.Right - ClearButtonRightMargin - ClearButtonSize - ScheduleEditClearButtonLeftShift;
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

        public void DrawConditionsHeader(SpriteBatch b, float opacity = 1f)
        {
            string text = TranslationCache.ScheduleEditConditions;
            float textHeight = Game1.smallFont.MeasureString(text).Y;
            int textY = conditionsHeaderY + (int)((ScheduleEditSectionHeaderHeight - textHeight) / 2);
            Vector2 pos = new Vector2(contentX, textY);
            Color color = Game1.textColor * opacity;
            Utility.drawTextWithShadow(b, text, Game1.smallFont, pos, color);
            Utility.drawTextWithShadow(b, text, Game1.smallFont, pos + new Vector2(1, 0), color);
        }

        public void DrawSpecialEventsHeader(SpriteBatch b)
        {
            string text = TranslationCache.ScheduleEditSpecialEvents;
            float textHeight = Game1.smallFont.MeasureString(text).Y;
            int textY = specialEventsHeaderY + (int)((ScheduleEditSectionHeaderHeight - textHeight) / 2);
            Vector2 pos = new Vector2(contentX, textY);
            Utility.drawTextWithShadow(b, text, Game1.smallFont, pos, Game1.textColor);
            Utility.drawTextWithShadow(b, text, Game1.smallFont, pos + new Vector2(1, 0), Game1.textColor);
        }

        public void DrawWeddingRow(SpriteBatch b, bool isChecked)
        {
            Rectangle srcRect = isChecked ? UIHelpers.CheckedSourceRect : UIHelpers.UncheckedSourceRect;
            b.Draw(Game1.mouseCursors, new Vector2(WeddingCheckbox.bounds.X, WeddingCheckbox.bounds.Y),
                srcRect, Color.White, 0f, Vector2.Zero, ScheduleCheckboxScale, SpriteEffects.None, 1f);

            string label = TranslationCache.ScheduleEditWedding;
            float textHeight = Game1.smallFont.MeasureString(label).Y;
            int textX = WeddingCheckbox.bounds.Right + ScheduleCheckboxGap;
            int rowY = WeddingCheckbox.bounds.Y - (TabAndButtonHeight - ScheduleCheckboxSize) / 2;
            int textY = rowY + (int)((TabAndButtonHeight - textHeight) / 2);
            Vector2 textPos = new Vector2(textX, textY);

            int labelWidth = (int)Game1.smallFont.MeasureString(label).X;
            Rectangle hitArea = new Rectangle(RightColBarX, rowY,
                (WeddingCheckbox.bounds.Right + ScheduleCheckboxGap + labelWidth) - RightColBarX, TabAndButtonHeight);
            bool isHovered = !UIHelpers.SuppressHover && hitArea.Contains(Game1.getMouseX(), Game1.getMouseY());

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

        public void DrawTagsRow(SpriteBatch b, int tagCount, bool isTagsOpen)
        {
            string label = TranslationCache.SaveSetTagsLabel;
            float textHeight = Game1.smallFont.MeasureString(label).Y;
            int textY = tagsRowY + (int)((ScheduleEditRowHeight - textHeight) / 2);

            string displayText = tagCount > 0 ? $"{label} ({tagCount})" : label;
            int labelX = TagsAddButton.bounds.X - 8 - (int)Game1.smallFont.MeasureString(displayText).X;

            Utility.drawTextWithShadow(b, displayText, Game1.smallFont,
                new Vector2(labelX, textY), Game1.textColor);

            UIHelpers.DrawToggleButton(b, TagsAddButton, isTagsOpen);
        }

        public void DrawSetsRow(SpriteBatch b, int includedCount, bool isSetsOpen)
        {
            string label = TranslationCache.ScheduleEditSets;
            float textHeight = Game1.smallFont.MeasureString(label).Y;
            int textY = setsRowY + (int)((ScheduleEditRowHeight - textHeight) / 2);

            string displayText = includedCount > 0 ? $"{label} ({includedCount})" : label;
            int labelX = SetsAddButton.bounds.X - 8 - (int)Game1.smallFont.MeasureString(displayText).X;

            Utility.drawTextWithShadow(b, displayText, Game1.smallFont,
                new Vector2(labelX, textY), Game1.textColor);

            UIHelpers.DrawToggleButton(b, SetsAddButton, isSetsOpen);
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

        public void BuildRotateOptions(List<ClickableComponent> outOptions)
        {
            outOptions.Clear();
            string[] texts = {
                TranslationCache.ScheduleEditRotateOnLocationChange,
                TranslationCache.ScheduleEditRotateOnceADay
            };

            for (int i = 0; i < texts.Length; i++)
            {
                outOptions.Add(new ClickableComponent(
                    new Rectangle(RotateDropdownBar.bounds.X,
                        RotateDropdownBar.bounds.Bottom + ScheduleEditDropdownPanelPadding + i * ScheduleEditDropdownOptionHeight,
                        RotateDropdownBar.bounds.Width, ScheduleEditDropdownOptionHeight),
                    texts[i]));
            }
        }

        public Rectangle GetRotateDropdownPanelBounds(int optionCount)
        {
            int dropdownHeight = optionCount * ScheduleEditDropdownOptionHeight + ScheduleEditDropdownPanelPadding * 2;
            return new Rectangle(
                RotateDropdownBar.bounds.X - 4,
                RotateDropdownBar.bounds.Bottom - 4,
                RotateDropdownBar.bounds.Width + 8,
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
