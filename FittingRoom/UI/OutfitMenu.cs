using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using static FittingRoom.OutfitLayoutConstants;

namespace FittingRoom
{
    public class OutfitMenu : IClickableMenu
    {
        private readonly OutfitCategoryManager categoryManager;
        private readonly OutfitFilterManager filterManager;
        private readonly OutfitItemRenderer itemRenderer;
        private readonly OutfitState state;
        private readonly OutfitUIBuilder uiBuilder;
        private readonly OutfitDropdownManager dropdownManager;
        private readonly OutfitSearchManager searchManager;
        private readonly OutfitTooltipRenderer tooltipRenderer;
        private readonly ContinuousScrollHandler continuousScrollHandler;
        private readonly OutfitItemListProvider itemListProvider;
        private readonly OutfitDrawingHelper drawingHelper;
        private readonly OutfitInputHandler inputHandler;
        private readonly ModEntry mod;

        private bool showItemInfo = false;
        private TemplatesOverlay? templatesOverlay = null;

        public OutfitMenu(ModEntry mod, OutfitCategoryManager categoryManager, OutfitFilterManager filterManager, bool showItemInfo = false)
        {
            this.mod = mod;
            this.categoryManager = categoryManager;
            this.filterManager = filterManager;
            this.showItemInfo = showItemInfo;

            // Reset to All tab when opening menu
            categoryManager.CurrentCategory = OutfitCategoryManager.Category.All;

            itemRenderer = new OutfitItemRenderer(mod.Monitor, mod.Helper.ModRegistry);
            state = new OutfitState();
            uiBuilder = new OutfitUIBuilder();
            dropdownManager = new OutfitDropdownManager(filterManager, categoryManager, state, uiBuilder);
            searchManager = new OutfitSearchManager(uiBuilder, state);
            tooltipRenderer = new OutfitTooltipRenderer(filterManager, categoryManager);
            continuousScrollHandler = new ContinuousScrollHandler(initialDelay: 400, repeatDelay: 100);

            // Initialize helper classes
            itemListProvider = new OutfitItemListProvider(filterManager, categoryManager, state);
            drawingHelper = new OutfitDrawingHelper(uiBuilder, dropdownManager, state, mod);
            inputHandler = new OutfitInputHandler(
                categoryManager, state, uiBuilder, dropdownManager, searchManager, continuousScrollHandler, mod,
                onRevertAndClose: RevertAndClose,
                onApplyOutfit: ApplyOutfit,
                onResetOutfit: ResetOutfit,
                onOutfitChanged: () => uiBuilder.MarkPreviewDirty(),
                getCurrentListCount: () => itemListProvider.GetCurrentListCount(),
                getCurrentShirtIds: () => itemListProvider.GetCurrentShirtIds(),
                getCurrentPantsIds: () => itemListProvider.GetCurrentPantsIds(),
                getCurrentHatIds: () => itemListProvider.GetCurrentHatIds(),
                getCurrentAllItems: () => itemListProvider.GetCurrentAllItems(),
                getTemplatesOverlay: () => templatesOverlay,
                setTemplatesOverlay: overlay => templatesOverlay = overlay
            );

            width = uiBuilder.Width;
            height = uiBuilder.Height;
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;

            state.ShirtIndex = Math.Max(0, categoryManager.ShirtIds.IndexOf(state.OriginalShirt));
            state.PantsIndex = Math.Max(0, categoryManager.PantsIds.IndexOf(state.OriginalPants));
            state.HatIndex = Math.Max(0, categoryManager.HatIds.IndexOf(state.OriginalHat));
        }

        // --- Resize handling ---

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);

            uiBuilder.Recalculate();
            searchManager.UpdateBounds();

            width = uiBuilder.Width;
            height = uiBuilder.Height;
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;

            // Clamp scroll offset if visible items changed
            int totalRows = Math.Max(1, (int)Math.Ceiling(itemListProvider.GetCurrentListCount() / (float)uiBuilder.COLUMNS));
            int maxScroll = Math.Max(0, totalRows - uiBuilder.VISIBLE_ROWS);
            if (state.ScrollOffset > maxScroll)
                state.ScrollOffset = maxScroll;

            if (dropdownManager.IsOpen)
            {
                dropdownManager.BuildOptions();
            }
        }

        // --- Helper methods ---

        private void ResetOutfit()
        {
            state.ResetToApplied(
                itemListProvider.GetCurrentShirtIds(),
                itemListProvider.GetCurrentPantsIds(),
                itemListProvider.GetCurrentHatIds()
            );
            uiBuilder.MarkPreviewDirty();
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
            inputHandler.HandleLeftClick(x, y, playSound);
        }

        public override void receiveScrollWheelAction(int direction)
        {
            base.receiveScrollWheelAction(direction);
            inputHandler.HandleScrollWheel(direction);
        }

        public override void receiveKeyPress(Keys key)
        {
            if (!inputHandler.HandleKeyPress(key))
            {
                base.receiveKeyPress(key);
            }
        }

        public override void update(GameTime time)
        {
            base.update(time);
            uiBuilder.Update((float)time.ElapsedGameTime.TotalMilliseconds);

            searchManager.Update();

            if (searchManager.HasSearchTextChanged)
            {
                state.SetSearchText(categoryManager.CurrentCategory, searchManager.CurrentSearchText);
                state.ScrollOffset = 0;
            }

            // Handle item info toggle keybind
            var config = mod.GetConfig();
            if (config.ToggleItemInfoKey.JustPressed())
            {
                showItemInfo = !showItemInfo;
                mod.SetShowItemInfoPreference(showItemInfo);
                Game1.playSound(showItemInfo ? "bigSelect" : "bigDeSelect");
            }

            // Handle continuous scrolling when keys are held down
            int scrollAmount = continuousScrollHandler.Update(time, uiBuilder.VISIBLE_ROWS, out bool shouldPlaySound);
            if (scrollAmount != 0)
            {
                if (dropdownManager.IsOpen)
                {
                    // Scroll dropdown when it's open
                    if (dropdownManager.HandleScrollAmount(scrollAmount))
                    {
                        if (shouldPlaySound) Game1.playSound("shiny4");
                    }
                }
                else
                {
                    // Scroll item grid when dropdown is closed
                    int totalRows = Math.Max(1, (int)Math.Ceiling(itemListProvider.GetCurrentListCount() / (float)uiBuilder.COLUMNS));
                    int maxScroll = Math.Max(0, totalRows - uiBuilder.VISIBLE_ROWS);

                    int newOffset = state.ScrollOffset + scrollAmount;
                    newOffset = Math.Clamp(newOffset, 0, maxScroll);

                    if (newOffset != state.ScrollOffset)
                    {
                        state.ScrollOffset = newOffset;
                        if (shouldPlaySound) Game1.playSound("shiny4");
                    }
                }
            }
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
            uiBuilder.DrawLookupIcon(b);
            uiBuilder.DrawSavedMessage(b);
            uiBuilder.DrawLeftPanelButtons(b);

            // === RIGHT PANEL: Category Tabs & Item List ===
            // Draw category tabs with text labels
            uiBuilder.DrawTabWithText(b, uiBuilder.AllTab, TranslationCache.TabAll,
                categoryManager.CurrentCategory == OutfitCategoryManager.Category.All);

            uiBuilder.DrawTabWithText(b, uiBuilder.ShirtsTab, TranslationCache.TabShirts,
                categoryManager.CurrentCategory == OutfitCategoryManager.Category.Shirts);

            uiBuilder.DrawTabWithText(b, uiBuilder.PantsTab, TranslationCache.TabPants,
                categoryManager.CurrentCategory == OutfitCategoryManager.Category.Pants);

            uiBuilder.DrawTabWithText(b, uiBuilder.HatsTab, TranslationCache.TabHats,
                categoryManager.CurrentCategory == OutfitCategoryManager.Category.Hats);

            // Draw search bar
            uiBuilder.DrawSearchBar(b, searchManager.IsFocused, !string.IsNullOrEmpty(searchManager.CurrentSearchText));
            searchManager.Draw(b);

            // Draw mod filter dropdown
            uiBuilder.DrawModFilterDropdown(b, state.GetModFilter(categoryManager.CurrentCategory), dropdownManager.IsOpen);

            // Cache list provider results once per frame to avoid repeated allocations
            var currentCategory = categoryManager.CurrentCategory;
            var allItems = currentCategory == OutfitCategoryManager.Category.All ? itemListProvider.GetCurrentAllItems() : null;
            var shirtIds = currentCategory != OutfitCategoryManager.Category.All ? itemListProvider.GetCurrentShirtIds() : null;
            var pantsIds = currentCategory != OutfitCategoryManager.Category.All ? itemListProvider.GetCurrentPantsIds() : null;
            var hatIds = currentCategory != OutfitCategoryManager.Category.All ? itemListProvider.GetCurrentHatIds() : null;
            int listCount = allItems?.Count ?? itemListProvider.GetCurrentListCount();

            // Cache equipped item IDs once for All tab selection highlighting
            string? equippedShirtId = null, equippedPantsId = null, equippedHatId = null;
            if (allItems != null)
            {
                equippedShirtId = OutfitState.GetClothingId(Game1.player.shirtItem.Value);
                equippedPantsId = OutfitState.GetClothingId(Game1.player.pantsItem.Value);
                equippedHatId = OutfitState.GetHatIdFromItem(Game1.player.hat.Value);
            }

            // Draw item list background and scroll buttons
            uiBuilder.DrawItemList(b, state.ScrollOffset, listCount);

            // Draw items
            int hoveredIndex = -1;

            for (int i = 0; i < uiBuilder.VISIBLE_ITEMS; i++)
            {
                int row = i / uiBuilder.COLUMNS;
                int col = i % uiBuilder.COLUMNS;
                int listIndex = state.ScrollOffset * uiBuilder.COLUMNS + row * uiBuilder.COLUMNS + col;
                if (listIndex >= listCount)
                    break;

                Rectangle slot = uiBuilder.ItemSlots[i].bounds;
                bool isSelected;

                // Check if item matches the current selection
                if (allItems != null && listIndex < allItems.Count)
                {
                    // All tab: check by category and item ID against equipped items
                    var (itemCategory, itemId) = allItems[listIndex];
                    isSelected = itemCategory switch
                    {
                        OutfitCategoryManager.Category.Shirts => itemId == equippedShirtId,
                        OutfitCategoryManager.Category.Pants => itemId == equippedPantsId,
                        OutfitCategoryManager.Category.Hats => itemId == equippedHatId,
                        _ => false
                    };
                }
                else
                {
                    // Category tabs: use category-specific index
                    int currentIndex = state.GetCurrentIndex(currentCategory);
                    isSelected = listIndex == currentIndex;
                }

                // Highlight selected item
                if (isSelected)
                {
                    b.Draw(Game1.staminaRect, slot, Color.Wheat);
                }

                // Hover highlight
                bool isHovered = uiBuilder.ItemSlots[i].containsPoint(Game1.getMouseX(), Game1.getMouseY());
                if (isHovered)
                {
                    b.Draw(Game1.staminaRect, slot, HoverEffectColor);
                    hoveredIndex = listIndex; // Track for tooltip rendering later
                }

                // Draw item sprite with cached filtered lists
                if (allItems != null)
                {
                    if (listIndex < allItems.Count)
                    {
                        var (itemCategory, itemId) = allItems[listIndex];
                        itemRenderer.DrawItemFromAllCategory(b, itemCategory, itemId, slot);
                    }
                }
                else
                {
                    itemRenderer.DrawItemSprite(b, currentCategory, listIndex, slot, shirtIds!, pantsIds!, hatIds!);
                }
            }

            // Draw bottom and close buttons
            uiBuilder.DrawBottomButtons(b);
            uiBuilder.DrawCloseButton(b);

            // Draw dropdown options if open
            if (dropdownManager.IsOpen)
            {
                drawingHelper.DrawDropdownOptions(b);

                if (!string.IsNullOrEmpty(drawingHelper.HoveredTruncatedFilterText))
                {
                    drawingHelper.DrawFilterTooltip(b, drawingHelper.HoveredTruncatedFilterText);
                }
            }

            // Draw item info tooltip if toggle is active, hovering, and dropdown is closed
            if (showItemInfo && hoveredIndex >= 0 && !dropdownManager.IsOpen)
            {
                if (allItems != null)
                {
                    if (hoveredIndex < allItems.Count)
                    {
                        var (itemCategory, itemId) = allItems[hoveredIndex];
                        tooltipRenderer.DrawTooltipForAllCategory(b, itemCategory, itemId);
                    }
                }
                else
                {
                    tooltipRenderer.DrawTooltip(b, hoveredIndex, shirtIds!, pantsIds!, hatIds!);
                }
            }

            // Draw lookup tooltip if hovering over lookup icon
            if (uiBuilder.LookupButton != null && uiBuilder.LookupButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawingHelper.DrawLookupTooltip(b);
            }

            // Draw templates overlay if open
            if (templatesOverlay != null)
            {
                templatesOverlay.draw(b);
            }

            // Draw cursor
            drawMouse(b);
        }

        public override void emergencyShutDown()
        {
            filterManager.ClearSearchCaches();
            itemRenderer.ClearCache();
            tooltipRenderer.ClearCache();
            uiBuilder.Cleanup();
            base.emergencyShutDown();
        }
    }
}
