using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    public class ScheduleMenuUIBuilder
    {
        private static readonly Rectangle GearIconSourceRect = new Rectangle(30, 428, 10, 10);
        private static readonly Rectangle ClearIconSourceRect = new Rectangle(337, 494, 12, 12);
        private static readonly Rectangle SpeedIconSourceRect = new Rectangle(130, 428, 10, 10);
        private const int RuleCheckboxToNameGap = 12;
        private const int WarpIconScale = 3;
        private const int WarpIconSize = 10 * WarpIconScale; // 30px
        private const int NameToWarpIconGap = 8;
        private const int TopPadding = 38;
        private const int BottomPadding = 20;
        private const int ListToButtonGap = 20;
        private const int HoverExtend = 10;

        public ClickableTextureComponent CloseButton { get; private set; } = null!;
        public ClickableComponent MasterStatusButton { get; private set; } = null!;
        public List<ClickableComponent> RuleCheckboxes { get; private set; } = new();
        public List<ClickableComponent> RuleNameAreas { get; private set; } = new();
        public List<ClickableComponent> RuleEditButtons { get; private set; } = new();
        public List<ClickableComponent> RuleDeleteButtons { get; private set; } = new();
        public ClickableComponent NewButton { get; private set; } = null!;
        public ClickableComponent CloseMenuButton { get; private set; } = null!;
        // Priority dropdown
        public ClickableComponent PriorityDropdown { get; private set; } = null!;
        public ClickableComponent PriorityClearButton { get; private set; } = null!;
        public List<ClickableComponent> PriorityDropdownOptions { get; private set; } = new();

        // Search bar
        public ClickableComponent SearchBar { get; private set; } = null!;
        public ClickableComponent SearchClearButton { get; private set; } = null!;

        // Divider
        public Rectangle DividerBounds { get; private set; }

        public Rectangle TitleBoxBounds { get; private set; }
        public Rectangle ContentBoxBounds { get; private set; }
        public Rectangle RuleListClipRect { get; private set; }

        public int ScrollOffset { get; set; }
        public int MaxScrollOffset { get; private set; }
        public bool IsScrollable => MaxScrollOffset > 0;

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        private int contentX;
        private int contentWidth;
        private int ruleListY;
        private int ruleListHeight;
        private int ruleCount;
        private int masterToggleHeight;


        public ScheduleMenuUIBuilder()
        {
            Width = ScheduleMenuWidth;
            Recalculate(0);
        }

        public void Recalculate(int ruleCount, int totalRuleCount = 0)
        {
            this.ruleCount = ruleCount;
            masterToggleHeight = (int)Game1.smallFont.MeasureString("T").Y;

            int visibleRules = Math.Min(ruleCount, ScheduleMaxVisibleRules);
            int heightRows = Math.Max(ScheduleMinVisibleRules, visibleRules);
            int ruleAreaHeight = heightRows * ScheduleRuleRowHeight + (heightRows - 1) * ScheduleItemGap;

            int contentInner = TopPadding
                + masterToggleHeight
                + ScheduleSectionGap                    // gap to filter row
                + ScheduleSearchBarHeight               // priority dropdown + search bar
                + ScheduleSearchDividerGap              // 8
                + ScheduleDividerHeight                 // divider
                + ScheduleSearchDividerGap              // 8
                + ruleAreaHeight                        // rule list
                + ScheduleRuleListBottomPad             // 10
                + ListToButtonGap                       // gap to buttons
                + TabAndButtonHeight                    // buttons
                + BottomPadding;                        // bottom pad

            // Title box
            Vector2 titleSize = Game1.dialogueFont.MeasureString(TranslationCache.ScheduleTitle);
            int titleBoxHeight = ScheduleTitleTopPadding + (int)titleSize.Y + ScheduleTitleBottomPadding;

            int totalHeight = titleBoxHeight + contentInner;

            Height = totalHeight;
            X = (Game1.uiViewport.Width - Width) / 2;
            Y = (Game1.uiViewport.Height - Height) / 2 - 10;

            int currentY = Y;

            // Title box
            int titleBoxWidth = (int)titleSize.X + ScheduleTitleSidePadding * 2;
            int titleBoxX = X + (Width - titleBoxWidth) / 2;
            TitleBoxBounds = new Rectangle(titleBoxX, currentY, titleBoxWidth, titleBoxHeight);
            currentY += titleBoxHeight;

            // Content box
            ContentBoxBounds = new Rectangle(X, currentY, Width, contentInner);

            contentX = ContentBoxBounds.X + ScheduleBorderPadding;
            contentWidth = ContentBoxBounds.Width - ScheduleBorderPadding * 2 - ScheduleScrollArrowRightPadding;

            // Scroll math
            int totalRuleHeight = ruleCount > 0
                ? ruleCount * ScheduleRuleRowHeight + (ruleCount - 1) * ScheduleItemGap
                : 0;
            MaxScrollOffset = Math.Max(0, totalRuleHeight - ruleAreaHeight);
            ClampScrollOffset();

            CalculateLayout(ruleAreaHeight);
        }

        public void ClampScrollOffset()
        {
            ScrollOffset = Math.Clamp(ScrollOffset, 0, MaxScrollOffset);
        }

        private void CalculateLayout(int ruleAreaHeight)
        {
            int currentY = ContentBoxBounds.Y + TopPadding;

            // 1. Master toggle
            int masterRowY = currentY;
            string enabledText = TranslationCache.ScheduleMasterEnabled;
            string disabledText = TranslationCache.ScheduleMasterDisabled;
            float maxStatusWidth = Math.Max(
                Game1.smallFont.MeasureString(enabledText).X,
                Game1.smallFont.MeasureString(disabledText).X);
            int statusX = contentX + contentWidth - (int)maxStatusWidth;
            MasterStatusButton = new ClickableComponent(
                new Rectangle(statusX, masterRowY, (int)maxStatusWidth, masterToggleHeight), "MasterStatus");
            currentY += masterToggleHeight + ScheduleSectionGap;

            // 2. Priority dropdown + search bar on same row
            PositionPriorityAndSearch(currentY);
            currentY += ScheduleSearchBarHeight + ScheduleSearchDividerGap;

            // 3. Divider
            DividerBounds = new Rectangle(contentX, currentY, contentWidth, ScheduleDividerHeight);
            currentY += ScheduleDividerHeight + ScheduleSearchDividerGap;

            // 4. Rule list area
            ruleListY = currentY;
            ruleListHeight = ruleAreaHeight;
            RuleListClipRect = new Rectangle(ContentBoxBounds.X, ruleListY, ContentBoxBounds.Width, ruleListHeight);

            BuildRuleComponents();

            // 5. Bottom buttons (inside content box) — New + Close only
            int buttonsY = ruleListY + ruleListHeight + ScheduleRuleListBottomPad + ListToButtonGap;

            int maxButtonWidth = (contentWidth - ScheduleBottomButtonGap) / 2;
            int newWidth = UIHelpers.CalculateButtonWidth(TranslationCache.ScheduleNew, maxButtonWidth);
            int closeWidth = UIHelpers.CalculateButtonWidth(TranslationCache.ScheduleClose, maxButtonWidth);

            int totalBtnWidth = newWidth + ScheduleBottomButtonGap + closeWidth;
            int btnStartX = X + (Width - totalBtnWidth) / 2;

            NewButton = new ClickableComponent(
                new Rectangle(btnStartX, buttonsY, newWidth, TabAndButtonHeight), "New");
            CloseMenuButton = new ClickableComponent(
                new Rectangle(btnStartX + newWidth + ScheduleBottomButtonGap, buttonsY, closeWidth, TabAndButtonHeight), "Close");

            // Close X button
            CloseButton = new ClickableTextureComponent(
                new Rectangle(X + Width - CloseButtonSize - CloseButtonEdgeMargin,
                    Y + CloseButtonEdgeMargin, CloseButtonSize, CloseButtonSize),
                Game1.mouseCursors, new Rectangle(337, 494, 12, 12), 4f);

        }

        private void PositionPriorityAndSearch(int rowY)
        {
            // Compute dropdown width from widest possible text + clear button space
            string[] priorityTexts =
            {
                TranslationCache.SchedulePriorityPlaceholder,
                TranslationCache.ScheduleEditPriorityHigh,
                TranslationCache.ScheduleEditPriorityMedium,
                TranslationCache.ScheduleEditPriorityLow
            };
            float maxTextWidth = 0;
            foreach (var t in priorityTexts)
                maxTextWidth = Math.Max(maxTextWidth, Game1.smallFont.MeasureString(t).X);
            int dropdownWidth = 28 + (int)maxTextWidth + ClearButtonSize + ClearButtonRightMargin;

            PriorityDropdown = new ClickableComponent(
                new Rectangle(contentX, rowY, dropdownWidth, ScheduleSearchBarHeight), "Priority");
            PriorityClearButton = CreateClearButton(PriorityDropdown);
            BuildPriorityOptions();

            // Search bar fills the remaining width
            int searchX = contentX + dropdownWidth + SchedulePriorityDropdownGap;
            int searchWidth = contentWidth - dropdownWidth - SchedulePriorityDropdownGap;
            SearchBar = new ClickableComponent(
                new Rectangle(searchX, rowY, searchWidth, ScheduleSearchBarHeight), "Search");
            SearchClearButton = CreateClearButton(SearchBar);
        }

        public void BuildPriorityOptions()
        {
            PriorityDropdownOptions.Clear();
            string[] labels =
            {
                TranslationCache.ScheduleEditPriorityHigh,
                TranslationCache.ScheduleEditPriorityMedium,
                TranslationCache.ScheduleEditPriorityLow
            };
            for (int i = 0; i < labels.Length; i++)
            {
                PriorityDropdownOptions.Add(new ClickableComponent(
                    new Rectangle(PriorityDropdown.bounds.X,
                        PriorityDropdown.bounds.Bottom + 4 + i * SchedulePriorityOptionHeight,
                        PriorityDropdown.bounds.Width, SchedulePriorityOptionHeight),
                    labels[i]));
            }
        }

        private static ClickableComponent CreateClearButton(ClickableComponent bar)
        {
            int clearX = bar.bounds.Right - ClearButtonRightMargin - ClearButtonSize;
            int clearY = bar.bounds.Y + (bar.bounds.Height - ClearButtonSize) / 2;
            return new ClickableComponent(
                new Rectangle(clearX, clearY, ClearButtonSize, ClearButtonSize), bar.name + "Clear");
        }

        private void BuildRuleComponents()
        {
            RuleCheckboxes.Clear();
            RuleNameAreas.Clear();
            RuleEditButtons.Clear();
            RuleDeleteButtons.Clear();

            int indentedX = contentX + ScheduleNameIndent;
            int checkStartX = indentedX;
            int buttonsRight = contentX + contentWidth;
            int stride = ScheduleRuleRowHeight + ScheduleItemGap;

            for (int i = 0; i < ruleCount; i++)
            {
                int rowY = ruleListY + (i * stride) - ScrollOffset;

                int checkY = rowY + (ScheduleRuleRowHeight - ScheduleCheckboxSize) / 2;
                RuleCheckboxes.Add(new ClickableComponent(
                    new Rectangle(checkStartX, checkY, ScheduleCheckboxSize, ScheduleCheckboxSize),
                    i.ToString()
                ));

                // Delete button (clear button style)
                int deleteX = buttonsRight - ClearButtonSize - 10;
                int deleteY = rowY + (ScheduleRuleRowHeight - ClearButtonSize) / 2;
                RuleDeleteButtons.Add(new ClickableComponent(
                    new Rectangle(deleteX, deleteY, ClearButtonSize, ClearButtonSize),
                    i.ToString()
                ));

                // Edit button (gear icon, no background)
                int editX = deleteX - ScheduleNameToInfoGap - ScheduleInfoButtonSize;
                int editY = rowY + (ScheduleRuleRowHeight - ScheduleInfoButtonSize) / 2;
                RuleEditButtons.Add(new ClickableComponent(
                    new Rectangle(editX, editY, ScheduleInfoButtonSize, ScheduleInfoButtonSize),
                    i.ToString()
                ));

                // Name area (checkbox + gap to edit button)
                int nameX = checkStartX + ScheduleCheckboxSize + RuleCheckboxToNameGap;
                int nameWidth = editX - ScheduleNameToInfoGap - nameX;
                RuleNameAreas.Add(new ClickableComponent(
                    new Rectangle(nameX, rowY, nameWidth, ScheduleRuleRowHeight),
                    i.ToString()
                ));
            }
        }

        public void DrawTitle(SpriteBatch b)
        {
            string title = TranslationCache.ScheduleTitle;
            Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
            Utility.drawTextWithShadow(b, title, Game1.dialogueFont,
                new Vector2(TitleBoxBounds.X + (TitleBoxBounds.Width - titleSize.X) / 2,
                    TitleBoxBounds.Y + ScheduleTitleTopPadding),
                Game1.textColor);
        }

        public void DrawMasterToggle(SpriteBatch b, bool isEnabled)
        {
            int rowY = MasterStatusButton.bounds.Y;

            // Label (bold)
            string label = TranslationCache.ScheduleMasterToggle;
            float textHeight = Game1.smallFont.MeasureString(label).Y;
            int textY = rowY + (int)((masterToggleHeight - textHeight) / 2);
            Vector2 labelPos = new Vector2(contentX, textY);
            Utility.drawTextWithShadow(b, label, Game1.smallFont,
                labelPos + new Vector2(-1, 0), Game1.textColor * 0.8f);
            Utility.drawTextWithShadow(b, label, Game1.smallFont,
                labelPos, Game1.textColor);

            // Status text (clickable, bold on hover)
            string statusText = isEnabled
                ? TranslationCache.ScheduleMasterEnabled
                : TranslationCache.ScheduleMasterDisabled;
            bool isHovered = !UIHelpers.SuppressHover && MasterStatusButton.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            float statusTextHeight = Game1.smallFont.MeasureString(statusText).Y;
            Vector2 statusPos = new Vector2(
                MasterStatusButton.bounds.X,
                rowY + (masterToggleHeight - statusTextHeight) / 2);

            if (isHovered)
            {
                Utility.drawTextWithShadow(b, statusText, Game1.smallFont,
                    statusPos + new Vector2(-1, 0), Game1.textColor * 0.8f);
                Utility.drawTextWithShadow(b, statusText, Game1.smallFont,
                    statusPos, Game1.textColor);
            }
            else
            {
                Utility.drawTextWithShadow(b, statusText, Game1.smallFont,
                    statusPos, Game1.textColor);
            }
        }

        public void DrawPriorityDropdown(SpriteBatch b, string? selectedPriorityLabel, bool isOpen, float opacity)
        {
            string displayText = selectedPriorityLabel ?? TranslationCache.SchedulePriorityPlaceholder;
            bool hasValue = selectedPriorityLabel != null;
            bool showClear = hasValue && opacity >= 1f;

            var bounds = PriorityDropdown.bounds;
            UIHelpers.DrawDropdownButton(b, bounds, displayText, isOpen,
                clearButton: showClear ? PriorityClearButton : null,
                hasValue: showClear,
                opacity: opacity);
        }

        public string? DrawPriorityDropdownOptions(SpriteBatch b, string? selectedPriorityLabel)
        {
            return UIHelpers.DrawDropdownOptions(b, PriorityDropdown.bounds,
                PriorityDropdownOptions, 0, 3,
                isSelected: option => option.name == selectedPriorityLabel,
                panelPaddingV: 4);
        }

        public void DrawDivider(SpriteBatch b)
        {
            b.Draw(Game1.staminaRect, DividerBounds, Color.Gray * 0.3f);
        }

        public string? DrawRuleRow(SpriteBatch b, int index, string name, bool isChecked, bool isHovered, bool masterEnabled, bool advanceOnWarp)
        {
            if (index >= RuleCheckboxes.Count)
                return null;

            string? hoveredTruncated = null;
            var checkbox = RuleCheckboxes[index];
            var nameArea = RuleNameAreas[index];
            var editBtn = RuleEditButtons[index];
            var deleteBtn = RuleDeleteButtons[index];

            float opacity = masterEnabled ? (isChecked ? 1f : DisabledSectionOpacity) : DisabledSectionOpacity;

            if (isHovered)
            {
                int indentedX = contentX + ScheduleNameIndent;
                int highlightWidth = contentWidth - ScheduleNameIndent;
                b.Draw(Game1.staminaRect,
                    new Rectangle(indentedX - HoverExtend, nameArea.bounds.Y, highlightWidth + HoverExtend, ScheduleRuleRowHeight),
                    Color.Wheat * 0.3f);
            }

            // Single-line layout
            int nameStartX = nameArea.bounds.X;
            int maxNameWidth = nameArea.bounds.Width;

            if (advanceOnWarp)
                maxNameWidth -= NameToWarpIconGap + WarpIconSize;

            string displayName = UIHelpers.TruncateText(name, maxNameWidth);
            bool isTruncated = displayName != name;

            float nameHeight = Game1.smallFont.MeasureString(displayName).Y;
            int nameY = nameArea.bounds.Y + (int)((ScheduleRuleRowHeight - nameHeight) / 2);

            // Checkbox first
            Rectangle sourceRect = isChecked ? UIHelpers.CheckedSourceRect : UIHelpers.UncheckedSourceRect;
            int checkDrawY = nameY + (int)((nameHeight - ScheduleCheckboxSize) / 2);
            b.Draw(Game1.mouseCursors,
                new Vector2(checkbox.bounds.X, checkDrawY),
                sourceRect, Color.White * opacity, 0f, Vector2.Zero, ScheduleCheckboxScale, SpriteEffects.None, 1f);

            // Draw name
            Utility.drawTextWithShadow(b, displayName, Game1.smallFont,
                new Vector2(nameStartX, nameY), Game1.textColor * opacity);

            // Draw speed icon after name
            if (advanceOnWarp)
            {
                int textWidth = (int)Game1.smallFont.MeasureString(displayName).X;
                int iconX = nameStartX + textWidth + NameToWarpIconGap + 2;
                int iconY = nameArea.bounds.Y + (ScheduleRuleRowHeight - WarpIconSize) / 2 - 2;
                b.Draw(Game1.mouseCursors, new Vector2(iconX, iconY),
                    SpeedIconSourceRect, Color.White * opacity, 0f, Vector2.Zero, WarpIconScale, SpriteEffects.FlipHorizontally, 1f);
            }

            if (isTruncated && isHovered)
                hoveredTruncated = name;

            DrawEditButton(b, editBtn, opacity, masterEnabled);
            DrawDeleteButton(b, deleteBtn, opacity, masterEnabled);

            return hoveredTruncated;
        }

        private void DrawEditButton(SpriteBatch b, ClickableComponent btn, float opacity, bool allowHover)
        {
            bool btnHovered = !UIHelpers.SuppressHover && allowHover && btn.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            float baseScale = 2.0f;
            float gearScale = btnHovered ? baseScale * 1.1f : baseScale;
            Vector2 gearOrigin = new Vector2(5, 5);
            Vector2 gearCenter = new Vector2(
                btn.bounds.X + btn.bounds.Width / 2f,
                btn.bounds.Y + btn.bounds.Height / 2f);
            b.Draw(Game1.mouseCursors, gearCenter, GearIconSourceRect,
                Color.White * opacity, 0f, gearOrigin, gearScale, SpriteEffects.None, 1f);
        }

        private void DrawDeleteButton(SpriteBatch b, ClickableComponent btn, float opacity, bool allowHover)
        {
            bool btnHovered = !UIHelpers.SuppressHover && allowHover && btn.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            float scale = btnHovered ? 2.2f : 2f;
            Vector2 center = new Vector2(
                btn.bounds.X + btn.bounds.Width / 2 - 2,
                btn.bounds.Y + btn.bounds.Height / 2);
            Vector2 origin = new Vector2(6, 6);
            b.Draw(Game1.mouseCursors, center, ClearIconSourceRect,
                Color.White * opacity, 0f, origin, scale, SpriteEffects.None, 1f);
        }

        public void DrawNoRulesText(SpriteBatch b, bool masterEnabled)
        {
            float opacity = masterEnabled ? 0.5f : DisabledSectionOpacity * 0.5f;
            string text = TranslationCache.ScheduleNoRules;
            Vector2 textSize = Game1.smallFont.MeasureString(text);
            float textX = contentX + (contentWidth - textSize.X) / 2;
            float textY = ruleListY + (ruleListHeight - textSize.Y) / 2;
            Utility.drawTextWithShadow(b, text, Game1.smallFont,
                new Vector2(textX, textY), Game1.textColor * opacity);
        }

        public void DrawButtons(SpriteBatch b, bool masterEnabled)
        {
            if (masterEnabled)
            {
                UIHelpers.DrawTextButton(b, NewButton, TranslationCache.ScheduleNew);
            }
            else
            {
                UIHelpers.DrawTextureBox(b, NewButton.bounds.X, NewButton.bounds.Y,
                    NewButton.bounds.Width, NewButton.bounds.Height,
                    Color.White * DisabledSectionOpacity);

                Vector2 textSize = Game1.smallFont.MeasureString(TranslationCache.ScheduleNew);
                Vector2 textPos = UIHelpers.GetVisualCenter(NewButton.bounds, textSize);
                Utility.drawTextWithShadow(b, TranslationCache.ScheduleNew, Game1.smallFont,
                    textPos, Game1.textColor * DisabledSectionOpacity);
            }

            UIHelpers.DrawTextButton(b, CloseMenuButton, TranslationCache.ScheduleClose);
        }

        public void DrawCloseButton(SpriteBatch b)
        {
            UIHelpers.DrawTextureButton(b, CloseButton);
        }

        public void DrawScrollIndicators(SpriteBatch b)
        {
            if (!IsScrollable) return;

            int arrowW = (int)(UIHelpers.UpScrollArrowSourceRect.Width * ScheduleScrollArrowScale);
            int arrowH = (int)(UIHelpers.UpScrollArrowSourceRect.Height * ScheduleScrollArrowScale);
            int rightPaddingStart = contentX + contentWidth;
            int rightPaddingWidth = ScheduleBorderPadding + ScheduleScrollArrowRightPadding;
            int arrowX = rightPaddingStart + (rightPaddingWidth - arrowW) / 2 - 10;

            if (ScrollOffset > 0)
            {
                int upY = ruleListY + (ScheduleRuleRowHeight - arrowH) / 2 - 4;
                b.Draw(Game1.mouseCursors, new Vector2(arrowX, upY),
                    UIHelpers.UpScrollArrowSourceRect, Color.White, 0f, Vector2.Zero,
                    ScheduleScrollArrowScale, SpriteEffects.None, 1f);
            }

            if (ScrollOffset < MaxScrollOffset)
            {
                int downY = ruleListY + ruleListHeight - ScheduleRuleRowHeight + (ScheduleRuleRowHeight - arrowH) / 2 - 4;
                b.Draw(Game1.mouseCursors, new Vector2(arrowX, downY),
                    UIHelpers.DownScrollArrowSourceRect, Color.White, 0f, Vector2.Zero,
                    ScheduleScrollArrowScale, SpriteEffects.None, 1f);
            }
        }

        public int GetRuleIndexAtPoint(int x, int y)
        {
            if (!RuleListClipRect.Contains(x, y))
                return -1;

            for (int i = 0; i < RuleNameAreas.Count; i++)
            {
                var row = RuleNameAreas[i];
                if (y >= row.bounds.Y && y < row.bounds.Y + ScheduleRuleRowHeight)
                    return i;
            }
            return -1;
        }
    }
}
