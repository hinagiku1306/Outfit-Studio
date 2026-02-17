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
        public Rectangle HairSlot { get; private set; }
        public ClickableComponent NameInputArea { get; private set; } = null!;
        public ClickableComponent NameClearButton { get; private set; } = null!;
        public ClickableComponent NameRandomButton { get; private set; } = null!;
        public ClickableComponent FavoriteCheckbox { get; private set; } = null!;
        public ClickableComponent? LocalOnlyCheckbox { get; private set; }
        public ClickableTextureComponent LeftArrowButton { get; private set; } = null!;
        public ClickableTextureComponent RightArrowButton { get; private set; } = null!;
        public ClickableTextureComponent CloseButton { get; private set; } = null!;
        public ClickableComponent SaveButton { get; private set; } = null!;
        public ClickableComponent CancelButton { get; private set; } = null!;
        public ClickableComponent? ScissorsButton { get; private set; }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public int NameSectionHeight { get; private set; }
        public int PreviewSectionHeight { get; private set; }
        public int FavoriteSectionHeight { get; private set; }
        public int LocalOnlySectionHeight { get; private set; }
        public int ButtonSectionHeight { get; private set; }

        private int contentX;
        private int contentWidth;
        private int favoriteRowX;
        private int localOnlyRowX;
        private readonly bool isEditMode;

        public SaveSetUIBuilder(int width, int height, bool isEditMode = false)
        {
            Width = width;
            Height = height;
            this.isEditMode = isEditMode;

            Recalculate();
        }

        public static int CalculateRequiredHeight()
        {
            int textRowHeight = (int)Game1.smallFont.MeasureString("A").Y + TextureBoxVerticalPadding;
            int itemSlotsHeight = (SaveSetItemSlotSize * 3) + (SaveSetItemSlotGap * 2);
            int arrowHeight = (int)(ArrowNativeHeight * SaveSetArrowScale);
            int previewHeight = Math.Max(SaveSetPreviewHeight + ElementGap + arrowHeight, itemSlotsHeight);

            return SaveSetBorderPadding
                   + textRowHeight
                   + SaveSetSectionPadding * 2
                   + previewHeight
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

            int arrowHeight = (int)(ArrowNativeHeight * SaveSetArrowScale);

            NameSectionHeight = textRowHeight;
            PreviewSectionHeight = Math.Max(SaveSetPreviewHeight + ElementGap + arrowHeight, itemSlotsHeight);
            FavoriteSectionHeight = SaveSetCheckboxSize;
            LocalOnlySectionHeight = LocalOnlyRowHeight;
            ButtonSectionHeight = textRowHeight;
        }

        private void CalculateLayout()
        {
            contentX = X + SaveSetBorderPadding;
            contentWidth = Width - (SaveSetBorderPadding * 2);

            int currentY = Y + SaveSetBorderPadding;

            int diceOffset = DiceButtonSize + 15;
            NameRandomButton = new ClickableComponent(
                new Rectangle(contentX, currentY + (NameSectionHeight - DiceButtonSize) / 2, DiceButtonSize, DiceButtonSize),
                "nameRandom"
            );

            int nameInputWidth = contentWidth - diceOffset - 30;
            NameInputArea = new ClickableComponent(
                new Rectangle(contentX + diceOffset, currentY, nameInputWidth, NameSectionHeight),
                "nameInput"
            );
            NameClearButton = new ClickableComponent(
                new Rectangle(
                    NameInputArea.bounds.Right - ClearButtonRightMargin - ClearButtonSize,
                    NameInputArea.bounds.Y + (NameSectionHeight - ClearButtonSize) / 2,
                    ClearButtonSize,
                    ClearButtonSize
                ),
                "nameClear"
            );

            currentY += NameSectionHeight;
            currentY += SaveSetSectionPadding * 2;

            int itemSlotsHeight = (SaveSetItemSlotSize * 3) + (SaveSetItemSlotGap * 2);
            int previewGroupWidth = SaveSetItemSlotSize + SaveSetPreviewToSlotsGap + SaveSetPreviewWidth + SaveSetPreviewToSlotsGap + SaveSetItemSlotSize;
            int previewGroupX = contentX + (contentWidth - previewGroupWidth) / 2;

            int hairSlotX = previewGroupX;
            int previewStartX = previewGroupX + SaveSetItemSlotSize + SaveSetPreviewToSlotsGap;
            int itemSlotsX = previewStartX + SaveSetPreviewWidth + SaveSetPreviewToSlotsGap;

            int arrowHeight = (int)(ArrowNativeHeight * SaveSetArrowScale);
            int previewWithArrowsHeight = SaveSetPreviewHeight + ElementGap + arrowHeight;
            int previewY = currentY + (PreviewSectionHeight - previewWithArrowsHeight) / 2;
            PreviewBox = new Rectangle(previewStartX, previewY, SaveSetPreviewWidth, SaveSetPreviewHeight);

            int arrowWidth = (int)(ArrowNativeWidth * SaveSetArrowScale);
            int arrowButtonY = PreviewBox.Bottom + ElementGap;
            int totalArrowsWidth = arrowWidth * 2 + ArrowGap;
            int arrowsCenterX = previewStartX + SaveSetPreviewWidth / 2;
            int arrowsStartX = arrowsCenterX - totalArrowsWidth / 2;

            LeftArrowButton = new ClickableTextureComponent(
                new Rectangle(arrowsStartX, arrowButtonY, arrowWidth, arrowHeight),
                Game1.mouseCursors,
                new Rectangle(352, 495, ArrowNativeWidth, ArrowNativeHeight),
                SaveSetArrowScale
            );
            RightArrowButton = new ClickableTextureComponent(
                new Rectangle(arrowsStartX + arrowWidth + ArrowGap, arrowButtonY, arrowWidth, arrowHeight),
                Game1.mouseCursors,
                new Rectangle(365, 495, ArrowNativeWidth, ArrowNativeHeight),
                SaveSetArrowScale
            );

            int itemSlotsY = currentY + (PreviewSectionHeight - itemSlotsHeight) / 2;

            HatSlot = new Rectangle(itemSlotsX, itemSlotsY, SaveSetItemSlotSize, SaveSetItemSlotSize);
            ShirtSlot = new Rectangle(itemSlotsX, itemSlotsY + SaveSetItemSlotSize + SaveSetItemSlotGap, SaveSetItemSlotSize, SaveSetItemSlotSize);
            PantsSlot = new Rectangle(itemSlotsX, itemSlotsY + (SaveSetItemSlotSize + SaveSetItemSlotGap) * 2, SaveSetItemSlotSize, SaveSetItemSlotSize);

            int hairSlotY = PreviewBox.Y + (SaveSetPreviewHeight - SaveSetItemSlotSize) / 2;
            HairSlot = new Rectangle(hairSlotX, hairSlotY, SaveSetItemSlotSize, SaveSetItemSlotSize);

            if (isEditMode)
            {
                int editBtnSize = (int)(CloseButtonSize * 1.1f);
                int editBtnX = itemSlotsX + SaveSetItemSlotSize + 23;
                int editBtnY = itemSlotsY + 15;
                ScissorsButton = new ClickableComponent(
                    new Rectangle(editBtnX, editBtnY, editBtnSize, editBtnSize),
                    "editOutfit"
                );
            }

            currentY += PreviewSectionHeight;
            currentY += SaveSetSectionPadding * 2;

            int iconSize = (int)(8 * FavoriteIconScale);
            int favoriteWidth = iconSize + FavoriteIconGap + (int)Game1.smallFont.MeasureString(TranslationCache.CommonFavorite).X;
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

            int maxButtonWidth = (contentWidth - SaveSetButtonGap) / 2;
            int saveButtonWidth = UIHelpers.CalculateButtonWidth(TranslationCache.CommonSave, maxButtonWidth);
            int cancelButtonWidth = UIHelpers.CalculateButtonWidth(TranslationCache.CommonCancel, maxButtonWidth);
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

        public void DrawDiceButton(SpriteBatch b)
        {
            Rectangle diceBounds = NameRandomButton.bounds;
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            bool isHovered = NameRandomButton.containsPoint(mouseX, mouseY);

            Rectangle sourceRect = new Rectangle(381, 361, 10, 10);
            float scale = isHovered ? 4.0f : 3.8f;
            Vector2 center = new Vector2(
                diceBounds.X + diceBounds.Width / 2,
                diceBounds.Y + diceBounds.Height / 2
            );
            Vector2 origin = new Vector2(5, 5);

            b.Draw(Game1.mouseCursors, center, sourceRect, Color.White, 0f, origin, scale, SpriteEffects.None, 1f);
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
                Utility.drawTextWithShadow(b, TranslationCache.CommonFavorite, Game1.smallFont, textPos + new Vector2(-1, 0), Game1.textColor * 0.8f);
            }
            Utility.drawTextWithShadow(b, TranslationCache.CommonFavorite, Game1.smallFont, textPos, Game1.textColor);
        }

        public void DrawLocalOnlyCheckbox(SpriteBatch b, bool isChecked, bool isEnabled, bool isHovered)
        {
            if (LocalOnlyCheckbox == null)
                return;

            Rectangle bounds = LocalOnlyCheckbox.bounds;

            Rectangle sourceRect = isChecked && isEnabled
                ? UIHelpers.CheckedSourceRect
                : UIHelpers.UncheckedSourceRect;

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
            UIHelpers.DrawTextButton(b, SaveButton, TranslationCache.CommonSave);
            UIHelpers.DrawTextButton(b, CancelButton, TranslationCache.CommonCancel);
        }

        public void DrawEditOutfitButton(SpriteBatch b)
        {
            if (ScissorsButton == null) return;

            Rectangle bounds = ScissorsButton.bounds;
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            bool isHovered = ScissorsButton.containsPoint(mouseX, mouseY);

            float buttonScale = isHovered ? 1.1f : 1f;
            int bgSize = (int)(bounds.Width * buttonScale);
            int bgX = bounds.X + (bounds.Width - bgSize) / 2;
            int bgY = bounds.Y + (bounds.Height - bgSize) / 2;

            UIHelpers.DrawTextureBox(b, bgX, bgY, bgSize, bgSize, Color.White);

            Rectangle craftingSource = new Rectangle(268, 1436, 11, 13);
            float iconScale = (bgSize / 11f) * 0.6f;
            Vector2 iconCenter = new Vector2(
                bounds.X + bounds.Width / 2,
                bounds.Y + bounds.Height / 2
            );
            Vector2 origin = new Vector2(5.5f, 6.5f);

            b.Draw(Game1.mouseCursors, iconCenter, craftingSource, Color.White, 0f,
                origin, iconScale, SpriteEffects.None, 1f);
        }

        public void DrawArrows(SpriteBatch b)
        {
            UIHelpers.DrawTextureButton(b, LeftArrowButton);
            UIHelpers.DrawTextureButton(b, RightArrowButton);
        }

        public void DrawCloseButton(SpriteBatch b)
        {
            UIHelpers.DrawTextureButton(b, CloseButton);
        }
    }
}
