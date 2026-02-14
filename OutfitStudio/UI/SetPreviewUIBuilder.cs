using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    public class SetPreviewUIBuilder
    {
        private static readonly Rectangle LeftArrowSourceRect = new Rectangle(352, 495, 12, 11);
        private static readonly Rectangle RightArrowSourceRect = new Rectangle(365, 495, 12, 11);

        public ClickableTextureComponent CloseXButton { get; private set; } = null!;
        public ClickableComponent SaveButton { get; private set; } = null!;
        public ClickableComponent CancelButton { get; private set; } = null!;
        public ClickableComponent LeftArrowButton { get; private set; } = null!;
        public ClickableComponent RightArrowButton { get; private set; } = null!;

        public Rectangle ContentBoxBounds { get; private set; }
        public Rectangle ButtonBoxBounds { get; private set; }
        public Rectangle PreviewBox { get; private set; }
        public List<ClickableComponent> SetListItems { get; private set; } = new();
        public List<ClickableComponent> ExcludeButtons { get; private set; } = new();

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        private readonly List<string> triggerLines;

        private int triggerPanelX;
        private int rightPanelX;
        private int rightPanelWidth;
        private int headerY;
        private int dividerY;
        private int listY;
        private int triggerHeaderY;
        private int triggerLinesStartY;

        public SetPreviewUIBuilder(List<string> triggerLines)
        {
            this.triggerLines = triggerLines;
            Width = SetPreviewWidth;
        }

        public void Recalculate()
        {
            int arrowH = (int)(ArrowNativeHeight * WardrobeArrowScale);
            int centerContentHeight = SaveSetPreviewHeight + ArrowGap + arrowH;
            int rightContentHeight = SetPreviewHeaderHeight + SetPreviewDividerPadding + 2 + SetPreviewDividerPadding
                + SetPreviewMaxVisibleSets * SetPreviewRowHeight;
            int innerHeight = Math.Max(rightContentHeight, centerContentHeight);

            int contentBoxHeight = ScheduleBorderPadding + innerHeight + ScheduleBorderPadding;
            int totalHeight = contentBoxHeight + ScheduleBoxGap + ScheduleButtonBoxHeight;

            Height = totalHeight;
            X = (Game1.uiViewport.Width - Width) / 2;
            Y = (Game1.uiViewport.Height - Height) / 2;

            int currentY = Y;
            ContentBoxBounds = new Rectangle(X, currentY, Width, contentBoxHeight);
            currentY += contentBoxHeight + ScheduleBoxGap;
            ButtonBoxBounds = new Rectangle(X, currentY, Width, ScheduleButtonBoxHeight);

            int contentX = ContentBoxBounds.X + ScheduleBorderPadding;
            int contentWidth = ContentBoxBounds.Width - ScheduleBorderPadding * 2;

            triggerPanelX = contentX;
            int centerPanelX = contentX + SetPreviewTriggerPanelWidth + SetPreviewPanelGap;
            rightPanelX = centerPanelX + SaveSetPreviewWidth + SetPreviewPanelGap;
            rightPanelWidth = contentX + contentWidth - rightPanelX;

            int contentStartY = ContentBoxBounds.Y + ScheduleBorderPadding;

            // Preview centered vertically
            int previewOffsetY = (innerHeight - centerContentHeight) / 2;
            PreviewBox = new Rectangle(centerPanelX, contentStartY + previewOffsetY, SaveSetPreviewWidth, SaveSetPreviewHeight);

            // Arrow buttons below preview
            int arrowW = (int)(ArrowNativeWidth * WardrobeArrowScale);
            int totalArrowWidth = arrowW * 2 + ArrowGap;
            int arrowsY = PreviewBox.Bottom + ArrowGap;
            int arrowsCenterX = PreviewBox.X + PreviewBox.Width / 2;

            LeftArrowButton = new ClickableComponent(
                new Rectangle(arrowsCenterX - totalArrowWidth / 2, arrowsY, arrowW, arrowH),
                "LeftArrow");
            RightArrowButton = new ClickableComponent(
                new Rectangle(arrowsCenterX + totalArrowWidth / 2 - arrowW, arrowsY, arrowW, arrowH),
                "RightArrow");

            // Right panel
            headerY = contentStartY;
            dividerY = headerY + SetPreviewHeaderHeight + SetPreviewDividerPadding;
            listY = dividerY + 2 + SetPreviewDividerPadding;

            // Trigger panel (top-aligned)
            triggerHeaderY = contentStartY;
            float headerTextHeight = Game1.smallFont.MeasureString("A").Y;
            triggerLinesStartY = triggerHeaderY + (int)headerTextHeight + 8;

            CalculateButtons();
        }

        private void CalculateButtons()
        {
            CloseXButton = new ClickableTextureComponent(
                new Rectangle(X + Width - CloseButtonSize - CloseButtonEdgeMargin,
                    Y + CloseButtonEdgeMargin, CloseButtonSize, CloseButtonSize),
                Game1.mouseCursors,
                new Rectangle(337, 494, 12, 12),
                4f);

            int maxButtonWidth = (ButtonBoxBounds.Width - ScheduleBottomButtonGap) / 2;
            int saveWidth = UIHelpers.CalculateButtonWidth(TranslationCache.CommonSave, maxButtonWidth);
            int cancelWidth = UIHelpers.CalculateButtonWidth(TranslationCache.CommonCancel, maxButtonWidth);
            int totalBtnWidth = saveWidth + ScheduleBottomButtonGap + cancelWidth;
            int btnStartX = ButtonBoxBounds.X + (ButtonBoxBounds.Width - totalBtnWidth) / 2;
            int buttonY = ButtonBoxBounds.Y + (ScheduleButtonBoxHeight - TabAndButtonHeight) / 2;

            SaveButton = new ClickableComponent(
                new Rectangle(btnStartX, buttonY, saveWidth, TabAndButtonHeight),
                "Save");
            CancelButton = new ClickableComponent(
                new Rectangle(btnStartX + saveWidth + ScheduleBottomButtonGap, buttonY, cancelWidth, TabAndButtonHeight),
                "Cancel");
        }

        public void BuildSetListItems(List<string> setNames, int scrollOffset)
        {
            SetListItems.Clear();
            ExcludeButtons.Clear();

            int maxVisible = SetPreviewMaxVisibleSets;
            int count = Math.Min(setNames.Count - scrollOffset, maxVisible);

            for (int i = 0; i < count; i++)
            {
                int dataIndex = scrollOffset + i;
                int rowY = listY + i * SetPreviewRowHeight;

                SetListItems.Add(new ClickableComponent(
                    new Rectangle(rightPanelX, rowY, rightPanelWidth, SetPreviewRowHeight),
                    setNames[dataIndex]
                ));

                int btnY = rowY + (SetPreviewRowHeight - ClearButtonSize) / 2;
                ExcludeButtons.Add(new ClickableComponent(
                    new Rectangle(rightPanelX + rightPanelWidth - ClearButtonSize - 8, btnY,
                        ClearButtonSize, ClearButtonSize),
                    "exclude_" + dataIndex
                ));
            }
        }

        public void DrawContentBox(SpriteBatch b)
        {
            UIHelpers.DrawTextureBox(b, ContentBoxBounds.X, ContentBoxBounds.Y,
                ContentBoxBounds.Width, ContentBoxBounds.Height, Color.White);
        }

        public void DrawButtonBox(SpriteBatch b)
        {
            UIHelpers.DrawTextureBox(b, ButtonBoxBounds.X, ButtonBoxBounds.Y,
                ButtonBoxBounds.Width, ButtonBoxBounds.Height, Color.White);
        }

        public void DrawPreview(SpriteBatch b, Texture2D? previewTexture)
        {
            b.Draw(Game1.daybg, PreviewBox, Color.White);
            if (previewTexture != null)
                b.Draw(previewTexture, PreviewBox, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
        }

        public void DrawArrowButtons(SpriteBatch b)
        {
            bool leftHovered = !UIHelpers.SuppressHover && LeftArrowButton.containsPoint(Game1.getMouseX(), Game1.getMouseY());
            bool rightHovered = !UIHelpers.SuppressHover && RightArrowButton.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            b.Draw(Game1.mouseCursors,
                new Vector2(LeftArrowButton.bounds.X, LeftArrowButton.bounds.Y),
                LeftArrowSourceRect, leftHovered ? Color.White : Color.White * 0.8f,
                0f, Vector2.Zero, WardrobeArrowScale, SpriteEffects.None, 1f);

            b.Draw(Game1.mouseCursors,
                new Vector2(RightArrowButton.bounds.X, RightArrowButton.bounds.Y),
                RightArrowSourceRect, rightHovered ? Color.White : Color.White * 0.8f,
                0f, Vector2.Zero, WardrobeArrowScale, SpriteEffects.None, 1f);
        }

        public void DrawTriggerPanel(SpriteBatch b)
        {
            string header = TranslationCache.SetPreviewTriggersHeader;
            Utility.drawTextWithShadow(b, header, Game1.smallFont,
                new Vector2(triggerPanelX, triggerHeaderY), Game1.textColor);

            int maxLineWidth = SetPreviewTriggerPanelWidth - SetPreviewTriggerIndent;
            int lineX = triggerPanelX + SetPreviewTriggerIndent;

            for (int i = 0; i < triggerLines.Count; i++)
            {
                int lineY = triggerLinesStartY + i * SetPreviewTriggerLineHeight;
                string text = UIHelpers.TruncateText(triggerLines[i], maxLineWidth);
                Utility.drawTextWithShadow(b, text, Game1.smallFont,
                    new Vector2(lineX, lineY), Game1.textColor);
            }
        }

        public void DrawHeader(SpriteBatch b, int activeCount, int totalCount)
        {
            string headerText = $"{TranslationCache.SetPreviewActiveSets} {activeCount} / {totalCount}";
            float textHeight = Game1.smallFont.MeasureString(headerText).Y;
            int textY = headerY + (int)((SetPreviewHeaderHeight - textHeight) / 2);
            Vector2 pos = new Vector2(rightPanelX, textY);
            Utility.drawTextWithShadow(b, headerText, Game1.smallFont, pos, Game1.textColor);
            Utility.drawTextWithShadow(b, headerText, Game1.smallFont, pos + new Vector2(1, 0), Game1.textColor);
        }

        public void DrawDivider(SpriteBatch b)
        {
            b.Draw(Game1.staminaRect,
                new Rectangle(rightPanelX, dividerY, rightPanelWidth, 2),
                Game1.textColor * 0.3f);
        }

        public string? DrawSetList(
            SpriteBatch b,
            Func<int, bool> isExcluded,
            Func<int, bool> isSelected,
            int scrollOffset,
            int totalCount)
        {
            string? hoveredTruncatedText = null;
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            int maxNameWidth = rightPanelWidth - ClearButtonSize - SetPreviewNameToButtonGap - 16;

            for (int i = 0; i < SetListItems.Count; i++)
            {
                var item = SetListItems[i];
                int dataIndex = scrollOffset + i;
                bool excluded = isExcluded(dataIndex);
                bool selected = isSelected(dataIndex);
                bool isHovered = item.containsPoint(mouseX, mouseY);

                float opacity = excluded ? MarkedForDeletionOpacity : 1f;

                if (selected)
                    b.Draw(Game1.staminaRect, item.bounds, Color.Wheat);
                else if (isHovered)
                    b.Draw(Game1.staminaRect, item.bounds, Color.Wheat * 0.6f);

                string fullName = item.name;
                string displayName = UIHelpers.TruncateText(fullName, maxNameWidth);
                bool isTruncated = displayName != fullName;

                Vector2 textSize = Game1.smallFont.MeasureString(displayName);
                Vector2 textPos = new Vector2(
                    item.bounds.X + 8,
                    item.bounds.Y + (item.bounds.Height - textSize.Y) / 2
                );
                Utility.drawTextWithShadow(b, displayName, Game1.smallFont, textPos, Game1.textColor * opacity);

                if (i < ExcludeButtons.Count)
                    DrawExcludeButton(b, ExcludeButtons[i], excluded);

                if (isHovered && isTruncated)
                    hoveredTruncatedText = fullName;
            }

            if (scrollOffset > 0)
            {
                Vector2 arrowPos = new Vector2(rightPanelX + rightPanelWidth - 24, listY - 18);
                b.Draw(Game1.mouseCursors, arrowPos, UIHelpers.UpScrollArrowSourceRect,
                    Color.White, 0f, Vector2.Zero, ScheduleScrollArrowScale, SpriteEffects.None, 1f);
            }
            if (scrollOffset + SetPreviewMaxVisibleSets < totalCount)
            {
                int listBottomY = listY + SetPreviewMaxVisibleSets * SetPreviewRowHeight;
                Vector2 arrowPos = new Vector2(rightPanelX + rightPanelWidth - 24, listBottomY + 2);
                b.Draw(Game1.mouseCursors, arrowPos, UIHelpers.DownScrollArrowSourceRect,
                    Color.White, 0f, Vector2.Zero, ScheduleScrollArrowScale, SpriteEffects.None, 1f);
            }

            return hoveredTruncatedText;
        }

        private void DrawExcludeButton(SpriteBatch b, ClickableComponent button, bool isExcluded)
        {
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            bool isHovered = button.containsPoint(mouseX, mouseY);

            Rectangle sourceRect = new Rectangle(337, 494, 12, 12);
            float scale = isHovered ? 2.2f : 2f;
            float opacity = isExcluded ? MarkedForDeletionOpacity : 1f;
            Vector2 center = new Vector2(
                button.bounds.X + button.bounds.Width / 2 - 2,
                button.bounds.Y + button.bounds.Height / 2
            );
            Vector2 origin = new Vector2(6, 6);

            b.Draw(
                Game1.mouseCursors,
                center,
                sourceRect,
                Color.White * opacity,
                0f,
                origin,
                scale,
                SpriteEffects.None,
                1f
            );
        }

        public void DrawCloseXButton(SpriteBatch b)
        {
            UIHelpers.DrawTextureButton(b, CloseXButton);
        }

        public void DrawBottomButtons(SpriteBatch b)
        {
            UIHelpers.DrawTextButton(b, SaveButton, TranslationCache.CommonSave);
            UIHelpers.DrawTextButton(b, CancelButton, TranslationCache.CommonCancel);
        }
    }
}
