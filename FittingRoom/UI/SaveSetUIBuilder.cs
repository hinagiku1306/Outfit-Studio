using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using static FittingRoom.OutfitLayoutConstants;

namespace FittingRoom
{
    public class SaveSetUIBuilder
    {
        private const int TextureBoxVerticalPadding = 32;
        private const float FavoriteIconScale = 3.5f;
        private const int FavoriteIconGap = 8;

        // Title box
        public ClickableComponent TitleBox { get; private set; } = null!;

        // Character preview
        public Rectangle PreviewBox { get; private set; }

        // Item slots
        public Rectangle ShirtSlot { get; private set; }
        public Rectangle PantsSlot { get; private set; }
        public Rectangle HatSlot { get; private set; }

        // Input components
        public ClickableComponent NameInputArea { get; private set; } = null!;
        public ClickableComponent TagDropdownButton { get; private set; } = null!;
        public ClickableComponent FavoriteCheckbox { get; private set; } = null!;

        // Buttons
        public ClickableTextureComponent CloseButton { get; private set; } = null!;
        public ClickableComponent SaveButton { get; private set; } = null!;
        public ClickableComponent CancelButton { get; private set; } = null!;

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        // Calculated section heights
        public int NameSectionHeight { get; private set; }
        public int PreviewSectionHeight { get; private set; }
        public int TagSectionHeight { get; private set; }
        public int FavoriteSectionHeight { get; private set; }
        public int ButtonSectionHeight { get; private set; }

        // Content area bounds
        private int contentX;
        private int contentWidth;

        // Tag row position for label drawing
        private int tagRowX;
        private int favoriteRowX;

        public SaveSetUIBuilder(int width, int height)
        {
            Width = width;
            Height = height;

            Recalculate();
        }

        public static int CalculateRequiredHeight()
        {
            int textRowHeight = (int)Game1.smallFont.MeasureString("A").Y + TextureBoxVerticalPadding;
            int itemSlotsHeight = (SaveSetItemSlotSize * 3) + (SaveSetItemSlotGap * 2);
            int previewHeight = Math.Max(SaveSetPreviewHeight, itemSlotsHeight);

            return SaveSetBorderPadding
                   + textRowHeight
                   + SaveSetSectionPadding * 2
                   + previewHeight
                   + SaveSetSectionPadding * 2
                   + textRowHeight
                   + SaveSetSectionPadding * 2
                   + SaveSetCheckboxSize
                   + SaveSetSectionPadding * 2
                   + textRowHeight
                   + SaveSetBorderPadding;
        }

        public void Recalculate()
        {
            CalculateSectionHeights();

            // Center just the menu on screen (no title)
            X = (Game1.uiViewport.Width - Width) / 2;
            Y = (Game1.uiViewport.Height - Height) / 2;

            CalculateLayout();
        }

        private void CalculateSectionHeights()
        {
            int textRowHeight = (int)Game1.smallFont.MeasureString("A").Y + TextureBoxVerticalPadding;
            int itemSlotsHeight = (SaveSetItemSlotSize * 3) + (SaveSetItemSlotGap * 2);

            NameSectionHeight = textRowHeight;
            PreviewSectionHeight = Math.Max(SaveSetPreviewHeight, itemSlotsHeight);
            TagSectionHeight = textRowHeight;
            FavoriteSectionHeight = SaveSetCheckboxSize;
            ButtonSectionHeight = textRowHeight;
        }

        private void CalculateLayout()
        {
            contentX = X + SaveSetBorderPadding;
            contentWidth = Width - (SaveSetBorderPadding * 2);

            int currentY = Y + SaveSetBorderPadding;

            int nameLabelWidth = (int)Game1.smallFont.MeasureString(TranslationCache.SaveSetNameLabel).X + 12;
            int nameInputWidth = contentWidth - nameLabelWidth;
            NameInputArea = new ClickableComponent(
                new Rectangle(contentX + nameLabelWidth, currentY, nameInputWidth, NameSectionHeight),
                "nameInput"
            );

            currentY += NameSectionHeight;
            currentY += SaveSetSectionPadding * 2;

            int itemSlotsHeight = (SaveSetItemSlotSize * 3) + (SaveSetItemSlotGap * 2);
            int previewGroupWidth = SaveSetPreviewWidth + SaveSetPreviewToSlotsGap + SaveSetItemSlotSize;
            int previewGroupX = contentX + (contentWidth - previewGroupWidth) / 2;

            int previewY = currentY + (PreviewSectionHeight - SaveSetPreviewHeight) / 2;
            PreviewBox = new Rectangle(previewGroupX, previewY, SaveSetPreviewWidth, SaveSetPreviewHeight);

            int itemSlotsX = previewGroupX + SaveSetPreviewWidth + SaveSetPreviewToSlotsGap;
            int itemSlotsY = currentY + (PreviewSectionHeight - itemSlotsHeight) / 2;

            HatSlot = new Rectangle(itemSlotsX, itemSlotsY, SaveSetItemSlotSize, SaveSetItemSlotSize);
            ShirtSlot = new Rectangle(itemSlotsX, itemSlotsY + SaveSetItemSlotSize + SaveSetItemSlotGap, SaveSetItemSlotSize, SaveSetItemSlotSize);
            PantsSlot = new Rectangle(itemSlotsX, itemSlotsY + (SaveSetItemSlotSize + SaveSetItemSlotGap) * 2, SaveSetItemSlotSize, SaveSetItemSlotSize);

            currentY += PreviewSectionHeight;
            currentY += SaveSetSectionPadding * 2;

            int tagLabelWidth = (int)Game1.smallFont.MeasureString(TranslationCache.SaveSetTagLabel).X + 12;
            int tagRowWidth = tagLabelWidth + FilterDropdownWidth;
            tagRowX = contentX + (contentWidth - tagRowWidth) / 2;

            TagDropdownButton = new ClickableComponent(
                new Rectangle(tagRowX + tagLabelWidth, currentY, FilterDropdownWidth, TagSectionHeight),
                "tagDropdown"
            );

            currentY += TagSectionHeight;
            currentY += SaveSetSectionPadding * 2;

            int iconSize = (int)(8 * FavoriteIconScale);
            int favoriteWidth = iconSize + FavoriteIconGap + (int)Game1.smallFont.MeasureString(TranslationCache.SaveSetFavorite).X;
            favoriteRowX = contentX + (contentWidth - favoriteWidth) / 2;

            FavoriteCheckbox = new ClickableComponent(
                new Rectangle(favoriteRowX, currentY, favoriteWidth, FavoriteSectionHeight),
                "favoriteCheckbox"
            );

            currentY += FavoriteSectionHeight;
            currentY += SaveSetSectionPadding * 2;

            int saveButtonWidth = UIHelpers.CalculateButtonWidth(TranslationCache.SaveSetButtonSave);
            int cancelButtonWidth = UIHelpers.CalculateButtonWidth(TranslationCache.SaveSetButtonCancel);
            int totalButtonWidth = saveButtonWidth + cancelButtonWidth + SaveSetButtonGap;
            int buttonStartX = X + (Width / 2) - (totalButtonWidth / 2);

            SaveButton = new ClickableComponent(
                new Rectangle(buttonStartX, currentY, saveButtonWidth, ButtonSectionHeight),
                "save"
            );
            CancelButton = new ClickableComponent(
                new Rectangle(buttonStartX + saveButtonWidth + SaveSetButtonGap, currentY, cancelButtonWidth, ButtonSectionHeight),
                "cancel"
            );

            CloseButton = new ClickableTextureComponent(
                new Rectangle(X + Width - CloseButtonSize - CloseButtonEdgeMargin,
                    Y + CloseButtonEdgeMargin, CloseButtonSize, CloseButtonSize),
                Game1.mouseCursors,
                new Rectangle(337, 494, 12, 12),
                4f
            );
        }

        public void DrawTitle(SpriteBatch b)
        {
            UIHelpers.DrawTextureBox(b, TitleBox.bounds.X, TitleBox.bounds.Y,
                TitleBox.bounds.Width, TitleBox.bounds.Height, Color.White);

            Vector2 titleSize = Game1.dialogueFont.MeasureString(TranslationCache.SaveSetTitle);
            Utility.drawTextWithShadow(b, TranslationCache.SaveSetTitle, Game1.dialogueFont,
                new Vector2(TitleBox.bounds.X + (TitleBox.bounds.Width - titleSize.X) / 2,
                    TitleBox.bounds.Y + (TitleBox.bounds.Height - titleSize.Y) / 2),
                Game1.textColor);
        }

        public void DrawNameInput(SpriteBatch b, string currentText, bool showPlaceholder)
        {
            Rectangle bounds = NameInputArea.bounds;

            // Draw label (vertically centered with input box)
            float textHeight = Game1.smallFont.MeasureString("A").Y;
            int labelY = bounds.Y + (int)((bounds.Height - textHeight) / 2);
            Utility.drawTextWithShadow(b, TranslationCache.SaveSetNameLabel, Game1.smallFont,
                new Vector2(contentX, labelY), Game1.textColor);

            // Draw text box background
            UIHelpers.DrawTextureBox(b, bounds.X, bounds.Y, bounds.Width, bounds.Height, Color.White);

            // Draw text or placeholder (matching search bar padding)
            string displayText = showPlaceholder ? TranslationCache.SaveSetNamePlaceholder : currentText;
            Color textColor = showPlaceholder ? Color.Gray : Game1.textColor;
            Vector2 textPosition = new Vector2(bounds.X + 20, bounds.Y + (bounds.Height - textHeight) / 2);
            Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPosition, textColor);
        }

        public void DrawNameCursor(SpriteBatch b, string currentText, bool isSelected)
        {
            if (!isSelected)
                return;

            Rectangle bounds = NameInputArea.bounds;
            Vector2 textSize = string.IsNullOrEmpty(currentText)
                ? Vector2.Zero
                : Game1.smallFont.MeasureString(currentText);

            Vector2 textPosition = new Vector2(bounds.X + 20, bounds.Y + (bounds.Height - 32) / 2);
            Vector2 cursorPosition = new Vector2(textPosition.X + textSize.X, textPosition.Y);

            // Blinking cursor (matching search bar pattern)
            if ((int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 500) % 2 == 0)
            {
                b.Draw(Game1.staminaRect, new Rectangle((int)cursorPosition.X, (int)cursorPosition.Y, 4, 32), Game1.textColor);
            }
        }

        public void DrawPreviewBackground(SpriteBatch b)
        {
            // Draw background (day/night based on time, matching main menu)
            b.Draw((Game1.timeOfDay >= NightTimeStartHour) ? Game1.nightbg : Game1.daybg, PreviewBox, Color.White);
        }

        public void DrawItemSlot(SpriteBatch b, Rectangle slot, bool isIncluded, bool hasItem, int mouseX, int mouseY)
        {
            UIHelpers.DrawTextureBoxNoShadow(b, slot.X - 4, slot.Y - 4, slot.Width + 8, slot.Height + 8, Color.White);

            bool isExcluded = !hasItem || !isIncluded;
            if (isExcluded)
            {
                b.Draw(Game1.staminaRect, slot, SaveSetExcludedItemSlotColor);
            }

            bool isHovered = slot.Contains(mouseX, mouseY) && hasItem;
            if (isHovered)
            {
                b.Draw(Game1.staminaRect, slot, HoverEffectColor);
            }
        }

        public void DrawTagDropdown(SpriteBatch b, string currentTagDisplay, bool isOpen)
        {
            UIHelpers.DrawDropdownButton(
                b,
                TagDropdownButton.bounds,
                currentTagDisplay,
                isOpen,
                showArrow: false,
                label: TranslationCache.SaveSetTagLabel,
                labelX: tagRowX
            );
        }

        public void DrawFavoriteCheckbox(SpriteBatch b, bool isChecked)
        {
            Rectangle bounds = FavoriteCheckbox.bounds;
            Rectangle sourceRect = new Rectangle(346, 392, 8, 8);

            int iconSize = (int)(8 * FavoriteIconScale);
            int iconY = bounds.Y + (FavoriteSectionHeight - iconSize) / 2;

            Color iconColor = isChecked ? Color.White : Color.White * 0.5f;
            b.Draw(Game1.mouseCursors, new Vector2(bounds.X, iconY), sourceRect,
                iconColor, 0f, Vector2.Zero, FavoriteIconScale, SpriteEffects.None, 1f);

            float textHeight = Game1.smallFont.MeasureString("A").Y;
            int labelY = bounds.Y + (int)((FavoriteSectionHeight - textHeight) / 2);
            Color textColor = isChecked ? Game1.textColor : Game1.textColor * 0.5f;
            Utility.drawTextWithShadow(b, TranslationCache.SaveSetFavorite, Game1.smallFont,
                new Vector2(bounds.X + iconSize + FavoriteIconGap, labelY), textColor);
        }

        public void DrawButtons(SpriteBatch b)
        {
            UIHelpers.DrawTextButton(b, SaveButton, TranslationCache.SaveSetButtonSave);
            UIHelpers.DrawTextButton(b, CancelButton, TranslationCache.SaveSetButtonCancel);
        }

        public void DrawCloseButton(SpriteBatch b)
        {
            CloseButton.draw(b);
        }

        public void DrawDropdownOptions(SpriteBatch b, List<ClickableComponent> options, int firstVisibleIndex, int maxVisibleItems, string? selectedTag)
        {
            UIHelpers.DrawDropdownOptions(
                b,
                TagDropdownButton.bounds,
                options,
                firstVisibleIndex,
                maxVisibleItems,
                isSelected: selectedTag != null ? option => option.name == selectedTag : null,
                enableTruncation: true
            );
        }
    }
}
