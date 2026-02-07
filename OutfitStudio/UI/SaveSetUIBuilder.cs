using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    public class SaveSetUIBuilder
    {
        private const int TextureBoxVerticalPadding = 32;
        private const float FavoriteIconScale = 3.5f;
        private const int FavoriteIconGap = 8;
        private const int CheckboxRowPadding = 120;
        private const int LocalOnlyRowHeight = 36;

        public ClickableComponent TitleBox { get; private set; } = null!;
        public Rectangle PreviewBox { get; private set; }
        public Rectangle ShirtSlot { get; private set; }
        public Rectangle PantsSlot { get; private set; }
        public Rectangle HatSlot { get; private set; }
        public ClickableComponent NameInputArea { get; private set; } = null!;
        public ClickableComponent FavoriteCheckbox { get; private set; } = null!;
        public ClickableComponent? LocalOnlyCheckbox { get; private set; }
        public ClickableComponent? AddTagsButton { get; private set; }
        public Rectangle TagRowBounds { get; private set; }
        public ClickableTextureComponent CloseButton { get; private set; } = null!;
        public ClickableComponent SaveButton { get; private set; } = null!;
        public ClickableComponent CancelButton { get; private set; } = null!;

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public int NameSectionHeight { get; private set; }
        public int PreviewSectionHeight { get; private set; }
        public int TagSectionHeight { get; private set; }
        public int FavoriteSectionHeight { get; private set; }
        public int LocalOnlySectionHeight { get; private set; }
        public int ButtonSectionHeight { get; private set; }

        private int contentX;
        private int contentWidth;
        private int tagsRowStartX;
        private int favoriteRowX;
        private int localOnlyRowX;

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
                   + SmallButtonHeight
                   + SaveSetSectionPadding * 2
                   + SaveSetCheckboxSize
                   + SaveSetSectionPadding
                   + LocalOnlyRowHeight
                   + SaveSetSectionPadding * 2
                   + textRowHeight
                   + SaveSetBorderPadding;
        }

        public void Recalculate()
        {
            CalculateSectionHeights();

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
            TagSectionHeight = SmallButtonHeight;
            FavoriteSectionHeight = SaveSetCheckboxSize;
            LocalOnlySectionHeight = LocalOnlyRowHeight;
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

            TagRowBounds = new Rectangle(contentX, currentY, contentWidth, TagSectionHeight);
            tagsRowStartX = contentX;

            Vector2 addButtonTextSize = Game1.smallFont.MeasureString("+");
            int addButtonWidth = (int)addButtonTextSize.X + TextPadding * 2;
            int addButtonHeight = SmallButtonHeight;
            int addButtonY = currentY + (SmallButtonHeight - addButtonHeight) / 2;
            AddTagsButton = new ClickableComponent(
                new Rectangle(tagsRowStartX, addButtonY, addButtonWidth, addButtonHeight),
                "addTags"
            );

            currentY += TagSectionHeight;
            currentY += SaveSetSectionPadding * 2;

            int iconSize = (int)(8 * FavoriteIconScale);
            int favoriteWidth = iconSize + FavoriteIconGap + (int)Game1.smallFont.MeasureString(TranslationCache.SaveSetFavorite).X;
            favoriteRowX = contentX + CheckboxRowPadding;

            FavoriteCheckbox = new ClickableComponent(
                new Rectangle(favoriteRowX, currentY, favoriteWidth, FavoriteSectionHeight),
                "favoriteCheckbox"
            );

            currentY += FavoriteSectionHeight;
            currentY += SaveSetSectionPadding;

            int localOnlyWidth = SaveSetLocalOnlyCheckboxSize + 8 + (int)Game1.smallFont.MeasureString(TranslationCache.SaveSetLocalOnly).X;
            localOnlyRowX = contentX + CheckboxRowPadding;

            LocalOnlyCheckbox = new ClickableComponent(
                new Rectangle(localOnlyRowX, currentY, localOnlyWidth, LocalOnlySectionHeight),
                "localOnlyCheckbox"
            );

            currentY += LocalOnlySectionHeight;
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

        public void UpdateTagsRowLayout()
        {
            int labelWidth = (int)Game1.smallFont.MeasureString(TranslationCache.SaveSetTagsLabel).X + 8;
            Vector2 addButtonTextSize = Game1.smallFont.MeasureString("+");
            int addButtonWidth = (int)addButtonTextSize.X + TextPadding * 2;
            int addButtonHeight = SmallButtonHeight;

            int totalWidth = labelWidth + addButtonWidth;
            int startX = contentX + (contentWidth - totalWidth) / 2;
            tagsRowStartX = startX;

            int buttonX = startX + labelWidth;
            int buttonY = TagRowBounds.Y + (SmallButtonHeight - addButtonHeight) / 2;
            AddTagsButton = new ClickableComponent(
                new Rectangle(buttonX, buttonY, addButtonWidth, addButtonHeight),
                "addTags"
            );
        }

        public void DrawNameInput(SpriteBatch b, string currentText, bool showPlaceholder, int jiggleOffset = 0)
        {
            Rectangle bounds = NameInputArea.bounds;

            float textHeight = Game1.smallFont.MeasureString("A").Y;
            int labelY = bounds.Y + (int)((bounds.Height - textHeight) / 2);
            Utility.drawTextWithShadow(b, TranslationCache.SaveSetNameLabel, Game1.smallFont,
                new Vector2(contentX, labelY), Game1.textColor);

            UIHelpers.DrawTextureBox(b, bounds.X + jiggleOffset, bounds.Y, bounds.Width, bounds.Height, Color.White);

            string displayText = showPlaceholder ? TranslationCache.SaveSetNamePlaceholder : currentText;
            Color textColor = showPlaceholder ? Color.Gray : Game1.textColor;
            Vector2 textPosition = new Vector2(bounds.X + 20 + jiggleOffset, bounds.Y + (bounds.Height - textHeight) / 2);
            Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPosition, textColor);
        }

        public void DrawNameCursor(SpriteBatch b, string currentText, bool isSelected, int jiggleOffset = 0)
        {
            if (!isSelected)
                return;

            Rectangle bounds = NameInputArea.bounds;
            Vector2 textSize = string.IsNullOrEmpty(currentText)
                ? Vector2.Zero
                : Game1.smallFont.MeasureString(currentText);

            Vector2 textPosition = new Vector2(bounds.X + 20 + jiggleOffset, bounds.Y + (bounds.Height - 32) / 2);
            Vector2 cursorPosition = new Vector2(textPosition.X + textSize.X, textPosition.Y);

            bool showCursor = Game1.currentGameTime != null
                && (int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 500) % 2 == 0;

            if (showCursor)
            {
                b.Draw(Game1.staminaRect, new Rectangle((int)cursorPosition.X, (int)cursorPosition.Y, 4, 32), Game1.textColor);
            }
        }

        public void DrawPreviewBackground(SpriteBatch b)
        {
            b.Draw(Game1.daybg, PreviewBox, Color.White);
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

        public void DrawTagsRow(SpriteBatch b, int mouseX, int mouseY, bool isTagMenuOpen)
        {
            float textHeight = Game1.smallFont.MeasureString("A").Y;
            int labelY = TagRowBounds.Y + (int)((SmallButtonHeight - textHeight) / 2);
            Utility.drawTextWithShadow(b, TranslationCache.SaveSetTagsLabel, Game1.smallFont,
                new Vector2(tagsRowStartX, labelY), Game1.textColor);

            if (AddTagsButton != null)
            {
                bool isHovered = AddTagsButton.containsPoint(mouseX, mouseY);
                UIHelpers.DrawTextureBox(b, AddTagsButton.bounds.X, AddTagsButton.bounds.Y,
                    AddTagsButton.bounds.Width, AddTagsButton.bounds.Height,
                    Color.White, shadowOffset: 2, shadowOpacity: 0.3f);

                if (isHovered)
                {
                    b.Draw(Game1.staminaRect, AddTagsButton.bounds, HoverEffectColor);
                }

                string buttonText = isTagMenuOpen ? "-" : "+";
                Vector2 addTextSize = Game1.smallFont.MeasureString(buttonText);
                Vector2 addTextPos = UIHelpers.GetVisualCenter(AddTagsButton.bounds, addTextSize);
                Utility.drawTextWithShadow(b, buttonText, Game1.smallFont, addTextPos, Game1.textColor);
            }
        }

        public void DrawFavoriteCheckbox(SpriteBatch b, bool isChecked, bool isHovered)
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
            Vector2 textPos = new Vector2(bounds.X + iconSize + FavoriteIconGap, labelY);

            if (isHovered)
            {
                Utility.drawTextWithShadow(b, TranslationCache.SaveSetFavorite, Game1.smallFont, textPos + new Vector2(-1, 0), Game1.textColor * 0.8f);
            }
            Utility.drawTextWithShadow(b, TranslationCache.SaveSetFavorite, Game1.smallFont, textPos, Game1.textColor);
        }

        public void DrawLocalOnlyCheckbox(SpriteBatch b, bool isChecked, bool isEnabled, bool isHovered)
        {
            if (LocalOnlyCheckbox == null)
                return;

            Rectangle bounds = LocalOnlyCheckbox.bounds;

            Rectangle sourceRect = isChecked && isEnabled
                ? new Rectangle(236, 425, 9, 9)
                : new Rectangle(227, 425, 9, 9);

            Color checkboxColor = isEnabled ? Color.White : Color.White * 0.5f;
            int checkboxY = bounds.Y + (LocalOnlySectionHeight - SaveSetLocalOnlyCheckboxSize) / 2;

            b.Draw(Game1.mouseCursors,
                new Vector2(bounds.X, checkboxY),
                sourceRect, checkboxColor, 0f, Vector2.Zero, SaveSetLocalOnlyCheckboxScale, SpriteEffects.None, 1f);

            float textHeight = Game1.smallFont.MeasureString("A").Y;
            int labelY = bounds.Y + (int)((LocalOnlySectionHeight - textHeight) / 2);
            Color textColor = isEnabled ? Game1.textColor : Game1.textColor * 0.5f;
            Vector2 textPos = new Vector2(bounds.X + SaveSetLocalOnlyCheckboxSize + 8, labelY);

            if (isHovered && isEnabled)
            {
                Utility.drawTextWithShadow(b, TranslationCache.SaveSetLocalOnly, Game1.smallFont, textPos + new Vector2(-1, 0), textColor * 0.8f);
            }
            Utility.drawTextWithShadow(b, TranslationCache.SaveSetLocalOnly, Game1.smallFont, textPos, textColor);
        }

        public void DrawButtons(SpriteBatch b)
        {
            UIHelpers.DrawTextButton(b, SaveButton, TranslationCache.SaveSetButtonSave);
            UIHelpers.DrawTextButton(b, CancelButton, TranslationCache.SaveSetButtonCancel);
        }

        public void DrawCloseButton(SpriteBatch b)
        {
            UIHelpers.DrawTextureButton(b, CloseButton);
        }
    }
}
