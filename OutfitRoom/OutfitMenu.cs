using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Objects;

namespace OutfitRoom
{
    public class OutfitMenu : IClickableMenu
    {
        // Helper classes
        private readonly OutfitCategoryManager categoryManager;
        private readonly OutfitItemRenderer itemRenderer;
        private readonly OutfitState state;
        private readonly OutfitUIBuilder uiBuilder;

        // Reference to mod (for config if needed)
        private readonly ModEntry mod;

        public OutfitMenu(ModEntry mod)
        {
            this.mod = mod;

            // Initialize helper classes (UI builder calculates dimensions dynamically)
            categoryManager = new OutfitCategoryManager();
            itemRenderer = new OutfitItemRenderer();
            state = new OutfitState();
            uiBuilder = new OutfitUIBuilder();

            // Set menu dimensions from UI builder
            width = uiBuilder.Width;
            height = uiBuilder.Height;
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;

            // Set current indices to match player's outfit
            state.ShirtIndex = Math.Max(0, categoryManager.ShirtIds.IndexOf(Game1.player.shirt.Value));
            state.PantsIndex = Math.Max(0, categoryManager.PantsIds.IndexOf(Game1.player.pants.Value));
            state.HatIndex = Math.Max(0, categoryManager.HatIds.IndexOf(state.OriginalHat));
        }

        // --- Resize handling ---

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);

            // Recalculate UI positions for new window size
            uiBuilder.Recalculate();

            // Update menu dimensions from UI builder
            width = uiBuilder.Width;
            height = uiBuilder.Height;
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;

            // Clamp scroll offset in case visible items changed
            int totalRows = Math.Max(1, (int)Math.Ceiling(GetCurrentListCount() / (float)uiBuilder.COLUMNS));
            int maxScroll = Math.Max(0, totalRows - uiBuilder.VISIBLE_ROWS);
            if (state.ScrollOffset > maxScroll)
                state.ScrollOffset = maxScroll;
        }

        // --- Helper methods for readability ---

        private int GetCurrentListCount() => categoryManager.GetCurrentListCount();
        private int GetCurrentIndex() => state.GetCurrentIndex(categoryManager.CurrentCategory);
        private void SetCurrentIndex(int index) => state.SetCurrentIndex(categoryManager.CurrentCategory, index);

        private void ApplyCurrentSelection()
        {
            state.ApplySelection(
                categoryManager.CurrentCategory,
                categoryManager.ShirtIds,
                categoryManager.PantsIds,
                categoryManager.HatIds
            );
        }

        private void ResetOutfit()
        {
            state.ResetToApplied(
                categoryManager.ShirtIds,
                categoryManager.PantsIds,
                categoryManager.HatIds
            );
        }

        private void ApplyOutfit()
        {
            state.SaveAppliedOutfit();
        }

        private void RevertAndClose()
        {
            state.RevertToApplied();
            exitThisMenu();
        }

        // --- Input handling ---

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            // Category tabs
            if (uiBuilder.ShirtsTab.containsPoint(x, y))
            {
                categoryManager.CurrentCategory = OutfitCategoryManager.Category.Shirts;
                state.ScrollOffset = 0;
                if (playSound) Game1.playSound("smallSelect");
                return;
            }
            if (uiBuilder.PantsTab.containsPoint(x, y))
            {
                categoryManager.CurrentCategory = OutfitCategoryManager.Category.Pants;
                state.ScrollOffset = 0;
                if (playSound) Game1.playSound("smallSelect");
                return;
            }
            if (uiBuilder.HatsTab.containsPoint(x, y))
            {
                categoryManager.CurrentCategory = OutfitCategoryManager.Category.Hats;
                state.ScrollOffset = 0;
                if (playSound) Game1.playSound("smallSelect");
                return;
            }

            // Scroll buttons
            int totalRows = Math.Max(1, (int)Math.Ceiling(GetCurrentListCount() / (float)uiBuilder.COLUMNS));
            int maxScroll = Math.Max(0, totalRows - uiBuilder.VISIBLE_ROWS);
            if (uiBuilder.ScrollUpButton.containsPoint(x, y) && state.ScrollOffset > 0)
            {
                state.ScrollOffset--;
                if (playSound) Game1.playSound("shwip");
                return;
            }
            if (uiBuilder.ScrollDownButton.containsPoint(x, y) && state.ScrollOffset < maxScroll)
            {
                state.ScrollOffset++;
                if (playSound) Game1.playSound("shwip");
                return;
            }

            // Item slots
            for (int i = 0; i < uiBuilder.ItemSlots.Count; i++)
            {
                if (uiBuilder.ItemSlots[i].containsPoint(x, y))
                {
                    int row = i / uiBuilder.COLUMNS;
                    int col = i % uiBuilder.COLUMNS;
                    int listIndex = state.ScrollOffset * uiBuilder.COLUMNS + row * uiBuilder.COLUMNS + col;
                    if (listIndex < GetCurrentListCount())
                    {
                        SetCurrentIndex(listIndex);
                        ApplyCurrentSelection();
                        if (playSound) Game1.playSound("stoneStep");
                    }
                    return;
                }
            }

            // Apply button
            if (uiBuilder.ApplyButton.containsPoint(x, y))
            {
                ApplyOutfit();
                uiBuilder.ShowSavedMessage();
                if (playSound) Game1.playSound("coin");
                return;
            }

            // Reset button
            if (uiBuilder.ResetButton.containsPoint(x, y))
            {
                ResetOutfit();
                if (playSound) Game1.playSound("drumkit6");
                return;
            }

            // Close button - reverts to applied outfit and closes
            if (uiBuilder.CloseButton.containsPoint(x, y))
            {
                RevertAndClose();
                if (playSound) Game1.playSound("bigDeSelect");
            }
        }

        public override void receiveScrollWheelAction(int direction)
        {
            base.receiveScrollWheelAction(direction);

            int totalRows = Math.Max(1, (int)Math.Ceiling(GetCurrentListCount() / (float)uiBuilder.COLUMNS));
            int maxScroll = Math.Max(0, totalRows - uiBuilder.VISIBLE_ROWS);
            if (direction > 0 && state.ScrollOffset > 0)
            {
                state.ScrollOffset--;
                Game1.playSound("shiny4");
            }
            else if (direction < 0 && state.ScrollOffset < maxScroll)
            {
                state.ScrollOffset++;
                Game1.playSound("shiny4");
            }
        }

        public override void receiveKeyPress(Keys key)
        {
            // Handle Escape or menu button to close with revert
            if (key == Keys.Escape || Game1.options.doesInputListContain(Game1.options.menuButton, key))
            {
                RevertAndClose();
                Game1.playSound("bigDeSelect");
                return;
            }

            base.receiveKeyPress(key);
        }

        public override void update(GameTime time)
        {
            base.update(time);
            uiBuilder.Update((float)time.ElapsedGameTime.TotalMilliseconds);
        }

        // --- Drawing ---

        public override void draw(SpriteBatch b)
        {
            // Draw semi-transparent background overlay
            OutfitUIBuilder.DrawOverlay(b);

            // Draw menu box
            drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

            // === LEFT PANEL: Player Preview ===
            uiBuilder.DrawPlayerPreview(b);
            uiBuilder.DrawSavedMessage(b);

            // Draw apply and reset buttons
            uiBuilder.DrawApplyButton(b);
            uiBuilder.DrawResetButton(b);

            // === RIGHT PANEL: Category Tabs & Item List ===
            // Draw category tabs with text labels
            uiBuilder.DrawTabWithText(b, uiBuilder.ShirtsTab, "Shirts",
                categoryManager.CurrentCategory == OutfitCategoryManager.Category.Shirts);

            uiBuilder.DrawTabWithText(b, uiBuilder.PantsTab, "Pants",
                categoryManager.CurrentCategory == OutfitCategoryManager.Category.Pants);

            uiBuilder.DrawTabWithText(b, uiBuilder.HatsTab, "Hats",
                categoryManager.CurrentCategory == OutfitCategoryManager.Category.Hats);

            // Draw item list background and scroll buttons
            uiBuilder.DrawItemList(b, state.ScrollOffset, GetCurrentListCount());

            // Draw items
            int currentIndex = GetCurrentIndex();
            int listCount = GetCurrentListCount();

            for (int i = 0; i < uiBuilder.VISIBLE_ITEMS; i++)
            {
                int row = i / uiBuilder.COLUMNS;
                int col = i % uiBuilder.COLUMNS;
                int listIndex = state.ScrollOffset * uiBuilder.COLUMNS + row * uiBuilder.COLUMNS + col;
                if (listIndex >= listCount)
                    break;

                Rectangle slot = uiBuilder.ItemSlots[i].bounds;
                bool isSelected = listIndex == currentIndex;

                // Highlight selected item
                if (isSelected)
                {
                    b.Draw(Game1.staminaRect, slot, Color.Green * 0.3f);
                }

                // Hover highlight
                if (uiBuilder.ItemSlots[i].containsPoint(Game1.getMouseX(), Game1.getMouseY()))
                {
                    b.Draw(Game1.staminaRect, slot, Color.White * 0.2f);
                }

                // Draw item sprite
                itemRenderer.DrawItemSprite(b, categoryManager.CurrentCategory, listIndex,
                    slot, categoryManager.ShirtIds, categoryManager.PantsIds, categoryManager.HatIds);
            }

            // Draw close button
            uiBuilder.DrawCloseButton(b);

            // Set hand cursor when hovering over clickable elements
            if (uiBuilder.IsHoveringClickable(Game1.getMouseX(), Game1.getMouseY()))
            {
                Game1.mouseCursor = 1;
            }

            // Draw cursor
            drawMouse(b);
        }
    }
}
