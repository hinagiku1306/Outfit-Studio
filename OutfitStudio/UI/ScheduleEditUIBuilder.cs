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

        // Priority inline text (click to open dropdown)
        public ClickableComponent PriorityClickArea { get; private set; } = null!;
        public Rectangle PriorityPanelAnchor { get; private set; }

        // Rotation inline text (click to open dropdown)
        public ClickableComponent RotationClickArea { get; private set; } = null!;
        public Rectangle RotationPanelAnchor { get; private set; }

        // Outfits label + [+] toggle button (on Behavior header row)
        public ClickableComponent OutfitsAddButton { get; private set; } = null!;

        // Bottom buttons
        public ClickableComponent SaveButton { get; private set; } = null!;
        public ClickableComponent CancelButton { get; private set; } = null!;

        public Rectangle ContentBoxBounds { get; private set; }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public int RightColBarX { get; private set; }

        private int contentX;
        private int contentWidth;

        private int totalOutfitsRowY;
        private int nameRowY;
        private int conditionsHeaderY;
        private int specialEventsHeaderY;
        private int behaviorHeaderY;
        private int priorityRowY;
        private int rotationRowY;
        private int textRowHeight;

        public bool IsEditing { get; set; }

        public ScheduleEditUIBuilder()
        {
            Width = ScheduleEditWidth;
        }

        public void Recalculate()
        {
            textRowHeight = (int)Game1.smallFont.MeasureString("X").Y;

            int contentInner = ScheduleEditTopPadding
                + textRowHeight + ScheduleEditInfoRowGap   // Total outfits row
                + textRowHeight + ScheduleEditInfoRowGap   // Name row
                + ScheduleEditSectionHeaderHeight          // "Conditions"
                + ScheduleEditHeaderToContentGap
                + TabAndButtonHeight                       // Season|Weather|Area bars
                + ScheduleEditBarRowGap
                + TabAndButtonHeight                       // Location bar
                + ScheduleEditConditionsToEventsGap
                + ScheduleEditSectionHeaderHeight          // "Special Events"
                + ScheduleEditSpecialEventsHeaderGap
                + TabAndButtonHeight                       // Festival + Wedding row
                + ScheduleEditSectionGap
                + ScheduleEditSectionHeaderHeight          // "Behavior"
                + ScheduleEditBehaviorHeaderGap
                + textRowHeight                            // Priority
                + ScheduleEditPriorityRotationGap
                + textRowHeight                            // Rotation
                + ScheduleEditSectionGap
                + TabAndButtonHeight                       // buttons row
                + ScheduleEditBottomPadding;

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

            int currentY = ContentBoxBounds.Y + ScheduleEditTopPadding;

            // Total outfits row (always shown)
            totalOutfitsRowY = currentY;
            currentY += textRowHeight + ScheduleEditInfoRowGap;

            // Name row
            nameRowY = currentY;
            currentY += textRowHeight + ScheduleEditInfoRowGap;

            // Conditions header
            conditionsHeaderY = currentY;
            currentY += ScheduleEditSectionHeaderHeight + ScheduleEditHeaderToContentGap;

            int triggerX = contentX + ScheduleEditTriggerIndent;
            int triggerAvail = contentWidth - ScheduleEditTriggerIndent - ScheduleEditTriggerRightPad;

            // Row 1: Season | Weather | Area (3 columns)
            int col3Width = (triggerAvail - ScheduleEditColumnGap * 2) / 3;
            int col2X = triggerX + col3Width + ScheduleEditColumnGap;
            int col3X = col2X + col3Width + ScheduleEditColumnGap;

            SeasonsDropdownBar = new ClickableComponent(
                new Rectangle(triggerX, currentY, col3Width, TabAndButtonHeight),
                "SeasonsDropdown");
            SeasonsClearButton = CreateClearButton(SeasonsDropdownBar);

            WeatherDropdownBar = new ClickableComponent(
                new Rectangle(col2X, currentY, col3Width, TabAndButtonHeight),
                "WeatherDropdown");
            WeatherClearButton = CreateClearButton(WeatherDropdownBar);

            AreasDropdownBar = new ClickableComponent(
                new Rectangle(col3X, currentY, col3Width, TabAndButtonHeight),
                "AreasDropdown");
            AreasClearButton = CreateClearButton(AreasDropdownBar);
            currentY += TabAndButtonHeight + ScheduleEditBarRowGap;

            // Row 2: Location (full width)
            LocationsDropdownBar = new ClickableComponent(
                new Rectangle(triggerX, currentY, triggerAvail, TabAndButtonHeight),
                "LocationsDropdown");
            LocationsClearButton = CreateClearButton(LocationsDropdownBar);
            currentY += TabAndButtonHeight + ScheduleEditConditionsToEventsGap;

            // Special Events header
            specialEventsHeaderY = currentY;
            currentY += ScheduleEditSectionHeaderHeight + ScheduleEditSpecialEventsHeaderGap;

            // Festival + Wedding row (same line)
            {
                string weddingLabel = TranslationCache.ScheduleEditWedding;
                int weddingLabelW = (int)Game1.smallFont.MeasureString(weddingLabel).X;
                int weddingTotalW = ScheduleCheckboxSize + ScheduleCheckboxGap + weddingLabelW;
                int festivalBarW = Width / 2;

                FestivalsDropdownBar = new ClickableComponent(
                    new Rectangle(triggerX, currentY, festivalBarW, TabAndButtonHeight),
                    "FestivalsDropdown");
                FestivalsClearButton = CreateClearButton(FestivalsDropdownBar);

                int weddingX = FestivalsDropdownBar.bounds.Right + ScheduleEditFestivalToWeddingGap;
                int weddingY = currentY + (TabAndButtonHeight - ScheduleCheckboxSize) / 2;
                WeddingCheckbox = new ClickableComponent(
                    new Rectangle(weddingX, weddingY, ScheduleCheckboxSize, ScheduleCheckboxSize),
                    "Wedding");
                RightColBarX = weddingX;
            }
            currentY += TabAndButtonHeight + ScheduleEditSectionGap;

            // Behavior header + Outfits text + [+] toggle button
            behaviorHeaderY = currentY;
            {
                int addBtnW = UIHelpers.GetToggleButtonWidth();
                int outfitsTextX = RightColBarX;
                int outfitsTextY = currentY + (int)((ScheduleEditSectionHeaderHeight - Game1.smallFont.MeasureString("A").Y) / 2);
                int addBtnX = outfitsTextX + (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditOutfits).X + 8;
                int addBtnY = currentY + (ScheduleEditSectionHeaderHeight - SmallButtonHeight) / 2;
                OutfitsAddButton = new ClickableComponent(
                    new Rectangle(addBtnX, addBtnY, addBtnW, SmallButtonHeight),
                    "OutfitsAdd");
            }
            currentY += ScheduleEditSectionHeaderHeight + ScheduleEditBehaviorHeaderGap;

            // Priority row (inline text)
            priorityRowY = currentY;
            currentY += textRowHeight + ScheduleEditPriorityRotationGap;

            // Rotation row (inline text)
            rotationRowY = currentY;
            currentY += textRowHeight + ScheduleEditSectionGap;

            CalculateBehaviorLayout();

            // Bottom buttons (Save + Cancel) — same width
            int maxButtonWidth = (contentWidth - ScheduleBottomButtonGap) / 2;
            int saveWidth = UIHelpers.CalculateButtonWidth(TranslationCache.CommonSave, maxButtonWidth);
            int cancelWidth = UIHelpers.CalculateButtonWidth(TranslationCache.CommonCancel, maxButtonWidth);
            int buttonWidth = Math.Max(saveWidth, cancelWidth);
            int totalBtnWidth = buttonWidth + ScheduleBottomButtonGap + buttonWidth;
            int btnStartX = X + (Width - totalBtnWidth) / 2;

            SaveButton = new ClickableComponent(
                new Rectangle(btnStartX, currentY, buttonWidth, TabAndButtonHeight),
                "Save");
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

        private void CalculateBehaviorLayout()
        {
            int indent = contentX + ScheduleEditBehaviorIndent;

            // Priority: "Priority: <value>"
            int priorityLabelW = (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditPriority + ": ").X;
            int priorityValueMaxW = Math.Max(
                Math.Max(
                    Math.Max(
                        (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditPriorityHigh).X,
                        (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditPriorityMedium).X),
                    (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditPriorityLow).X),
                (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditPrioritySpecial).X);
            int priorityTotalW = priorityLabelW + priorityValueMaxW;
            PriorityClickArea = new ClickableComponent(
                new Rectangle(indent, priorityRowY, priorityTotalW, textRowHeight),
                "PriorityClick");
            int priorityPanelW = priorityValueMaxW + 40;
            int priorityPanelX = indent + priorityLabelW + priorityValueMaxW / 2 - priorityPanelW / 2;
            PriorityPanelAnchor = new Rectangle(priorityPanelX, priorityRowY, priorityPanelW, textRowHeight);

            // Rotation: "Rotation: <value>"
            int rotationLabelW = (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditRotation + ": ").X;
            int rotationValueMaxW = Math.Max(
                (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditRotateOnceADay).X,
                (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleEditRotateOnLocationChange).X);
            int rotationTotalW = rotationLabelW + rotationValueMaxW;
            RotationClickArea = new ClickableComponent(
                new Rectangle(indent, rotationRowY, rotationTotalW, textRowHeight),
                "RotationClick");
            int rotationPanelW = rotationValueMaxW + 40;
            int rotationPanelX = indent + rotationLabelW + rotationValueMaxW / 2 - rotationPanelW / 2;
            RotationPanelAnchor = new Rectangle(rotationPanelX, rotationRowY, rotationPanelW, textRowHeight);
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

        public void DrawTotalOutfitsRow(SpriteBatch b, int count, int remaining, bool showRemaining)
        {
            string label = TranslationCache.ScheduleEditTotalOutfits;
            string value = $": {count}";
            if (showRemaining)
                value += $"   \u00b7   {remaining} {TranslationCache.ScheduleEditRemainingBeforeReset}";

            Vector2 pos = new Vector2(contentX, totalOutfitsRowY);
            Utility.drawTextWithShadow(b, label, Game1.smallFont, pos, Game1.textColor);
            Utility.drawTextWithShadow(b, label, Game1.smallFont, pos + new Vector2(1, 0), Game1.textColor);

            float labelWidth = Game1.smallFont.MeasureString(label).X;
            Utility.drawTextWithShadow(b, value, Game1.smallFont, pos + new Vector2(labelWidth, 0), Game1.textColor);
        }

        public string? DrawNameRow(SpriteBatch b, string name)
        {
            string label = TranslationCache.ScheduleEditNameLabel;
            float labelWidth = Game1.smallFont.MeasureString(label).X;
            float colonWidth = Game1.smallFont.MeasureString(": ").X;
            int maxNameWidth = contentWidth - (int)labelWidth - (int)colonWidth;
            string displayName = UIHelpers.TruncateText(name, maxNameWidth);
            string value = $": {displayName}";

            Vector2 pos = new Vector2(contentX, nameRowY);
            Utility.drawTextWithShadow(b, label, Game1.smallFont, pos, Game1.textColor);
            Utility.drawTextWithShadow(b, label, Game1.smallFont, pos + new Vector2(1, 0), Game1.textColor);

            Utility.drawTextWithShadow(b, value, Game1.smallFont, pos + new Vector2(labelWidth, 0), Game1.textColor);

            if (displayName != name)
            {
                int mx = Game1.getMouseX(), my = Game1.getMouseY();
                int rowHeight = (int)Game1.smallFont.MeasureString("X").Y;
                if (mx >= contentX && mx <= contentX + contentWidth
                    && my >= nameRowY && my <= nameRowY + rowHeight)
                    return name;
            }
            return null;
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
            int textY = WeddingCheckbox.bounds.Y + (int)((ScheduleCheckboxSize - textHeight) / 2);
            Vector2 textPos = new Vector2(textX, textY);

            int labelWidth = (int)Game1.smallFont.MeasureString(label).X;
            Rectangle hitArea = new Rectangle(RightColBarX, WeddingCheckbox.bounds.Y,
                (WeddingCheckbox.bounds.Right + ScheduleCheckboxGap + labelWidth) - RightColBarX, ScheduleCheckboxSize);
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

        public void DrawBehaviorHeader(SpriteBatch b)
        {
            string text = TranslationCache.ScheduleEditBehavior;
            float textHeight = Game1.smallFont.MeasureString(text).Y;
            int textY = behaviorHeaderY + (int)((ScheduleEditSectionHeaderHeight - textHeight) / 2);
            Vector2 pos = new Vector2(contentX, textY);
            Utility.drawTextWithShadow(b, text, Game1.smallFont, pos, Game1.textColor);
            Utility.drawTextWithShadow(b, text, Game1.smallFont, pos + new Vector2(1, 0), Game1.textColor);

            string outfitsText = TranslationCache.ScheduleEditOutfits;
            int outfitsTextX = RightColBarX;
            int outfitsTextY = behaviorHeaderY + (int)((ScheduleEditSectionHeaderHeight - textHeight) / 2);
            Vector2 outfitsPos = new Vector2(outfitsTextX, outfitsTextY);
            Utility.drawTextWithShadow(b, outfitsText, Game1.smallFont, outfitsPos, Game1.textColor);
            Utility.drawTextWithShadow(b, outfitsText, Game1.smallFont, outfitsPos + new Vector2(1, 0), Game1.textColor);

            UIHelpers.DrawToggleButton(b, OutfitsAddButton, false);
        }

        public void DrawPriorityRow(SpriteBatch b, string currentValue, bool isOpen, bool isSpecial = false)
        {
            string label = TranslationCache.ScheduleEditPriority + ": ";
            int textY = priorityRowY;
            int textX = PriorityClickArea.bounds.X;

            Utility.drawTextWithShadow(b, label, Game1.smallFont,
                new Vector2(textX, textY), Game1.textColor);

            int valueX = textX + (int)Game1.smallFont.MeasureString(label).X;
            Vector2 valuePos = new Vector2(valueX, textY);
            Color valueColor = isSpecial ? Color.Orange : Game1.textColor;
            bool isHovered = !isSpecial && !isOpen && !UIHelpers.SuppressHover
                && PriorityClickArea.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            if (isHovered)
            {
                Utility.drawTextWithShadow(b, currentValue, Game1.smallFont, valuePos + new Vector2(-1, 0), valueColor * 0.8f);
                Utility.drawTextWithShadow(b, currentValue, Game1.smallFont, valuePos, valueColor);
            }
            else
            {
                Utility.drawTextWithShadow(b, currentValue, Game1.smallFont, valuePos, valueColor);
            }
        }

        public void DrawRotationRow(SpriteBatch b, string currentValue, bool isOpen)
        {
            string label = TranslationCache.ScheduleEditRotation + ": ";
            int textY = rotationRowY;
            int textX = RotationClickArea.bounds.X;

            Utility.drawTextWithShadow(b, label, Game1.smallFont,
                new Vector2(textX, textY), Game1.textColor);

            int valueX = textX + (int)Game1.smallFont.MeasureString(label).X;
            Vector2 valuePos = new Vector2(valueX, textY);
            bool isHovered = !isOpen && !UIHelpers.SuppressHover
                && RotationClickArea.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            if (isHovered)
            {
                Utility.drawTextWithShadow(b, currentValue, Game1.smallFont, valuePos + new Vector2(-1, 0), Game1.textColor * 0.8f);
                Utility.drawTextWithShadow(b, currentValue, Game1.smallFont, valuePos, Game1.textColor);
            }
            else
            {
                Utility.drawTextWithShadow(b, currentValue, Game1.smallFont, valuePos, Game1.textColor);
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
                    new Rectangle(PriorityPanelAnchor.X,
                        PriorityPanelAnchor.Bottom + ScheduleEditDropdownPanelPadding + i * ScheduleEditDropdownOptionHeight,
                        PriorityPanelAnchor.Width, ScheduleEditDropdownOptionHeight),
                    texts[i]));
            }
        }

        public Rectangle GetPriorityDropdownPanelBounds(int optionCount)
        {
            int dropdownHeight = optionCount * ScheduleEditDropdownOptionHeight + ScheduleEditDropdownPanelPadding * 2;
            return new Rectangle(
                PriorityPanelAnchor.X - 4,
                PriorityPanelAnchor.Bottom - 4,
                PriorityPanelAnchor.Width + 8,
                dropdownHeight + 8);
        }

        public void BuildRotationOptions(List<ClickableComponent> outOptions)
        {
            outOptions.Clear();
            string[] texts = {
                TranslationCache.ScheduleEditRotateOnLocationChange,
                TranslationCache.ScheduleEditRotateOnceADay
            };

            for (int i = 0; i < texts.Length; i++)
            {
                outOptions.Add(new ClickableComponent(
                    new Rectangle(RotationPanelAnchor.X,
                        RotationPanelAnchor.Bottom + ScheduleEditDropdownPanelPadding + i * ScheduleEditDropdownOptionHeight,
                        RotationPanelAnchor.Width, ScheduleEditDropdownOptionHeight),
                    texts[i]));
            }
        }

        public Rectangle GetRotationDropdownPanelBounds(int optionCount)
        {
            int dropdownHeight = optionCount * ScheduleEditDropdownOptionHeight + ScheduleEditDropdownPanelPadding * 2;
            return new Rectangle(
                RotationPanelAnchor.X - 4,
                RotationPanelAnchor.Bottom - 4,
                RotationPanelAnchor.Width + 8,
                dropdownHeight + 8);
        }

        public void DrawBottomButtons(SpriteBatch b)
        {
            UIHelpers.DrawTextButton(b, SaveButton, TranslationCache.CommonSave);
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
