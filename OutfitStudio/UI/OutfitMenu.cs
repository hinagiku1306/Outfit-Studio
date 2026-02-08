using System;
using System.Collections.Generic;
using OutfitStudio.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
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
        private readonly OutfitSetStore outfitSetStore;

        private readonly DyeColorManager dyeColorManager;
        private OutfitCategoryManager.Category lastColorCategory = OutfitCategoryManager.Category.Pants;

        private bool showItemInfo = false;
        private WardrobeOverlay? wardrobeOverlay = null;

        public bool IsOverlayBlocking { get; set; } = false;
        public bool ShowItemInfo => showItemInfo;

        public void NotifyOutfitApplied()
        {
            state.SaveAppliedOutfit();
            uiBuilder.MarkPreviewDirty();
        }

        /// <summary>
        /// Checks if the item info toggle keybind was pressed and toggles if so.
        /// Called from overlays to allow keybind to work while overlay is active.
        /// </summary>
        public void HandleItemInfoToggle()
        {
            var config = mod.GetConfig();
            if (config.ToggleItemInfoKey.JustPressed())
            {
                showItemInfo = !showItemInfo;
                mod.SetShowItemInfoPreference(showItemInfo);
                Game1.playSound(showItemInfo ? "bigSelect" : "bigDeSelect");
            }
        }

        public OutfitMenu(ModEntry mod, OutfitCategoryManager categoryManager, OutfitFilterManager filterManager, OutfitSetStore outfitSetStore, bool showItemInfo = false)
        {
            this.mod = mod;
            this.categoryManager = categoryManager;
            this.filterManager = filterManager;
            this.outfitSetStore = outfitSetStore;
            this.showItemInfo = showItemInfo;

            // Reset to All tab when opening menu
            categoryManager.CurrentCategory = OutfitCategoryManager.Category.All;

            itemRenderer = new OutfitItemRenderer(mod.Monitor, mod.Helper.ModRegistry);
            state = new OutfitState();
            uiBuilder = new OutfitUIBuilder(mod.GetConfig());
            dropdownManager = new OutfitDropdownManager(filterManager, categoryManager, state, uiBuilder);
            searchManager = new OutfitSearchManager(uiBuilder, state);
            tooltipRenderer = new OutfitTooltipRenderer(filterManager, categoryManager);
            continuousScrollHandler = new ContinuousScrollHandler(initialDelay: 400, repeatDelay: 100);

            // Initialize helper classes
            itemListProvider = new OutfitItemListProvider(filterManager, categoryManager, state);
            drawingHelper = new OutfitDrawingHelper(uiBuilder, dropdownManager, state, mod);
            dyeColorManager = new DyeColorManager(
                onColorChanged: () => uiBuilder.MarkPreviewDirty(),
                getActiveCategory: () => GetActiveColorCategory()
            );
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
                getWardrobeOverlay: () => wardrobeOverlay,
                setWardrobeOverlay: overlay => wardrobeOverlay = overlay,
                getParentMenu: () => this,
                outfitSetStore: outfitSetStore,
                showSavedMessage: () => uiBuilder.ShowSavedMessage(),
                getDyeColorManager: () => dyeColorManager,
                getLastColorCategory: () => lastColorCategory,
                setLastColorCategory: cat => lastColorCategory = cat
            );

            width = uiBuilder.Width;
            height = uiBuilder.Height;
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;

            state.ShirtIndex = Math.Max(0, categoryManager.ShirtIds.IndexOf(state.OriginalShirt));
            state.PantsIndex = Math.Max(0, categoryManager.PantsIds.IndexOf(state.OriginalPants));
            state.HatIndex = Math.Max(0, categoryManager.HatIds.IndexOf(state.OriginalHat));

            if (mod.GetConfig().AutoOpenDyeColorMenu)
            {
                Color currentColor = GetActiveColorCategory() switch
                {
                    OutfitCategoryManager.Category.Shirts => Game1.player.GetShirtColor(),
                    OutfitCategoryManager.Category.Pants => Game1.player.GetPantsColor(),
                    _ => Color.White
                };
                Rectangle menuBounds = new Rectangle(uiBuilder.X, uiBuilder.Y, uiBuilder.Width, uiBuilder.Height);
                dyeColorManager.Open(menuBounds, currentColor);

                var autoOpenCategory = GetActiveColorCategory();
                dyeColorManager.IsDyeable = autoOpenCategory switch
                {
                    OutfitCategoryManager.Category.Shirts => Game1.player.CanDyeShirt(),
                    OutfitCategoryManager.Category.Pants => Game1.player.CanDyePants(),
                    _ => false
                };
            }
        }

        // --- Resize handling ---

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);

            wardrobeOverlay?.gameWindowSizeChanged(oldBounds, newBounds);

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

            if (dyeColorManager.IsOpen)
            {
                Rectangle menuBoundsRect = new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height);
                dyeColorManager.UpdateParentBounds(menuBoundsRect);
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

            // Sync dye panel sliders to the restored color
            if (dyeColorManager.IsOpen)
            {
                var category = GetActiveColorCategory();
                Color restoredColor = category switch
                {
                    OutfitCategoryManager.Category.Shirts => Game1.player.GetShirtColor(),
                    OutfitCategoryManager.Category.Pants => Game1.player.GetPantsColor(),
                    _ => Color.White
                };
                dyeColorManager.SetSlidersFromColor(restoredColor);
            }

            uiBuilder.MarkPreviewDirty();
        }

        private void ApplyOutfit()
        {
            state.SaveAppliedOutfit();
        }

        private OutfitCategoryManager.Category GetActiveColorCategory()
        {
            var category = categoryManager.CurrentCategory;
            if (category == OutfitCategoryManager.Category.All)
                return lastColorCategory;
            return category;
        }

        private void RevertAndClose()
        {
            dyeColorManager.Close();
            state.RevertToApplied();
            exitThisMenu();
        }

        // --- Input handling ---

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            bool clickInPanel = dyeColorManager.IsOpen && dyeColorManager.Bounds.Contains(x, y);
            bool clickOnFloatingButton = uiBuilder.GearButton.containsPoint(x, y) || uiBuilder.DyeColorButton.containsPoint(x, y);
            if (wardrobeOverlay == null && !isWithinBounds(x, y) && !clickInPanel && !clickOnFloatingButton && mod.GetConfig().CloseOnClickOutside)
            {
                RevertAndClose();
                return;
            }

            inputHandler.HandleLeftClick(x, y, playSound);
        }

        public override void leftClickHeld(int x, int y)
        {
            if (wardrobeOverlay != null) return;
            if (dyeColorManager.IsOpen)
                dyeColorManager.HandleClickHeld(x, y);
        }

        public override void releaseLeftClick(int x, int y)
        {
            dyeColorManager.HandleClickRelease();
            base.releaseLeftClick(x, y);
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

            if (wardrobeOverlay != null)
            {
                wardrobeOverlay.update(time);
                return;
            }

            uiBuilder.Update((float)time.ElapsedGameTime.TotalMilliseconds);

            bool hasOverlay = IsOverlayBlocking;
            searchManager.Update(allowFocus: !hasOverlay);

            if (searchManager.HasSearchTextChanged)
            {
                state.SetSearchText(categoryManager.CurrentCategory, searchManager.CurrentSearchText);
                state.ScrollOffset = 0;
            }

            // Handle item info toggle keybind
            HandleItemInfoToggle();

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
            bool hasOverlay = wardrobeOverlay != null || IsOverlayBlocking;

            // Draw semi-transparent background overlay
            OutfitUIBuilder.DrawOverlay(b);

            // Draw menu box
            UIHelpers.DrawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

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

            // Cache equipped item IDs once for selection highlighting
            string equippedShirtId = OutfitState.GetClothingId(Game1.player.shirtItem.Value);
            string equippedPantsId = OutfitState.GetClothingId(Game1.player.pantsItem.Value);
            string equippedHatId = OutfitState.GetHatIdFromItem(Game1.player.hat.Value);

            // Draw item list background and scroll buttons
            uiBuilder.DrawItemList(b, state.ScrollOffset, listCount);

            // Draw items (skip hover effects when overlay is open)
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
                    // Category tabs: check by item ID against equipped item
                    string? equippedId = currentCategory switch
                    {
                        OutfitCategoryManager.Category.Shirts => equippedShirtId,
                        OutfitCategoryManager.Category.Pants => equippedPantsId,
                        OutfitCategoryManager.Category.Hats => equippedHatId,
                        _ => null
                    };
                    List<string>? ids = currentCategory switch
                    {
                        OutfitCategoryManager.Category.Shirts => shirtIds,
                        OutfitCategoryManager.Category.Pants => pantsIds,
                        OutfitCategoryManager.Category.Hats => hatIds,
                        _ => null
                    };
                    isSelected = ids != null && listIndex < ids.Count && ids[listIndex] == equippedId;
                }

                // Highlight selected item
                if (isSelected)
                {
                    b.Draw(Game1.staminaRect, slot, Color.Wheat);
                }

                // Hover highlight (only when no overlay is open)
                if (!hasOverlay)
                {
                    bool isHovered = uiBuilder.ItemSlots[i].containsPoint(Game1.getMouseX(), Game1.getMouseY());
                    if (isHovered)
                    {
                        b.Draw(Game1.staminaRect, slot, HoverEffectColor);
                        hoveredIndex = listIndex; // Track for tooltip rendering later
                    }
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
            uiBuilder.DrawFloatingButtons(b);

            // Draw dye color panel (after menu, before dropdowns)
            if (dyeColorManager.IsOpen)
                dyeColorManager.Draw(b);

            // Skip dropdown and tooltips when overlay is open
            if (!hasOverlay)
            {
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

            }

            // Draw wardrobe overlay if open
            if (wardrobeOverlay != null)
            {
                wardrobeOverlay.draw(b);
            }

            // Only draw cursor if no overlay (overlays draw their own cursor)
            if (!hasOverlay)
            {
                drawMouse(b);
            }
        }

        public override void emergencyShutDown()
        {
            dyeColorManager.Close();
            filterManager.ClearSearchCaches();
            itemRenderer.ClearCache();
            tooltipRenderer.ClearCache();
            uiBuilder.Cleanup();
            base.emergencyShutDown();
        }
    }
}
