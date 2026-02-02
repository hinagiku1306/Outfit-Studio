using System;
using System.Collections.Generic;
using FittingRoom.Services;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace FittingRoom
{
    public class OutfitInputHandler
    {
        private readonly OutfitCategoryManager categoryManager;
        private readonly OutfitState state;
        private readonly OutfitUIBuilder uiBuilder;
        private readonly OutfitDropdownManager dropdownManager;
        private readonly OutfitSearchManager searchManager;
        private readonly ContinuousScrollHandler continuousScrollHandler;
        private readonly ModEntry mod;
        private readonly TemplateManager templateManager;

        private readonly Action onRevertAndClose;
        private readonly Action onApplyOutfit;
        private readonly Action onResetOutfit;
        private readonly Action onOutfitChanged;
        private readonly Action showSavedMessage;
        private readonly Func<int> getCurrentListCount;
        private readonly Func<List<string>> getCurrentShirtIds;
        private readonly Func<List<string>> getCurrentPantsIds;
        private readonly Func<List<string>> getCurrentHatIds;
        private readonly Func<List<(OutfitCategoryManager.Category, string)>> getCurrentAllItems;
        private readonly Func<TemplatesOverlay?> getTemplatesOverlay;
        private readonly Action<TemplatesOverlay?> setTemplatesOverlay;

        public OutfitInputHandler(
            OutfitCategoryManager categoryManager,
            OutfitState state,
            OutfitUIBuilder uiBuilder,
            OutfitDropdownManager dropdownManager,
            OutfitSearchManager searchManager,
            ContinuousScrollHandler continuousScrollHandler,
            ModEntry mod,
            Action onRevertAndClose,
            Action onApplyOutfit,
            Action onResetOutfit,
            Action onOutfitChanged,
            Func<int> getCurrentListCount,
            Func<List<string>> getCurrentShirtIds,
            Func<List<string>> getCurrentPantsIds,
            Func<List<string>> getCurrentHatIds,
            Func<List<(OutfitCategoryManager.Category, string)>> getCurrentAllItems,
            Func<TemplatesOverlay?> getTemplatesOverlay,
            Action<TemplatesOverlay?> setTemplatesOverlay,
            TemplateManager templateManager,
            Action showSavedMessage)
        {
            this.categoryManager = categoryManager;
            this.state = state;
            this.uiBuilder = uiBuilder;
            this.dropdownManager = dropdownManager;
            this.searchManager = searchManager;
            this.continuousScrollHandler = continuousScrollHandler;
            this.mod = mod;
            this.templateManager = templateManager;
            this.onRevertAndClose = onRevertAndClose;
            this.onApplyOutfit = onApplyOutfit;
            this.onResetOutfit = onResetOutfit;
            this.onOutfitChanged = onOutfitChanged;
            this.showSavedMessage = showSavedMessage;
            this.getCurrentListCount = getCurrentListCount;
            this.getCurrentShirtIds = getCurrentShirtIds;
            this.getCurrentPantsIds = getCurrentPantsIds;
            this.getCurrentHatIds = getCurrentHatIds;
            this.getCurrentAllItems = getCurrentAllItems;
            this.getTemplatesOverlay = getTemplatesOverlay;
            this.setTemplatesOverlay = setTemplatesOverlay;
        }

        public bool HandleLeftClick(int x, int y, bool playSound)
        {
            // Templates overlay handling
            var templatesOverlay = getTemplatesOverlay();
            if (templatesOverlay != null)
            {
                templatesOverlay.receiveLeftClick(x, y, playSound);

                if (templatesOverlay.readyToClose())
                {
                    setTemplatesOverlay(null);
                }

                return true;
            }

            // Handle dropdown option clicks (but allow Close, Tab, and Clear buttons to work)
            if (dropdownManager.IsOpen)
            {
                // Close button always works
                if (uiBuilder.CloseButton.containsPoint(x, y))
                {
                    dropdownManager.Close();
                    onRevertAndClose();
                    if (playSound) Game1.playSound("bigDeSelect");
                    return true;
                }

                // Filter clear button works when dropdown is open
                if (uiBuilder.FilterClearButton != null && uiBuilder.FilterClearButton.containsPoint(x, y))
                {
                    string? currentFilter = state.GetModFilter(categoryManager.CurrentCategory);
                    if (!string.IsNullOrEmpty(currentFilter))
                    {
                        state.SetModFilter(categoryManager.CurrentCategory, null);
                        state.ScrollOffset = 0;
                        dropdownManager.Close();
                        if (playSound) Game1.playSound("smallSelect");
                        return true;
                    }
                }

                // Search clear button works when dropdown is open
                if (uiBuilder.SearchClearButton != null && uiBuilder.SearchClearButton.containsPoint(x, y))
                {
                    if (!string.IsNullOrEmpty(searchManager.CurrentSearchText))
                    {
                        searchManager.Clear();
                        state.SetSearchText(categoryManager.CurrentCategory, null);
                        state.ScrollOffset = 0;
                        dropdownManager.Close();
                        if (playSound) Game1.playSound("smallSelect");
                        return true;
                    }
                }

                // Tab buttons work even when dropdown is open
                if (uiBuilder.AllTab.containsPoint(x, y) ||
                    uiBuilder.ShirtsTab.containsPoint(x, y) ||
                    uiBuilder.PantsTab.containsPoint(x, y) ||
                    uiBuilder.HatsTab.containsPoint(x, y))
                {
                    dropdownManager.Close();
                    continuousScrollHandler.Reset();
                    // Fall through to handle tab click below
                }
                else
                {
                    string? selectedMod = dropdownManager.HandleClick(x, y, out bool clickedOption);
                    if (clickedOption)
                    {
                        state.SetModFilter(categoryManager.CurrentCategory, selectedMod);
                        state.SetSearchText(categoryManager.CurrentCategory, searchManager.CurrentSearchText);
                        state.ScrollOffset = 0;
                        if (playSound) Game1.playSound("smallSelect");
                        return true;
                    }
                    // Clicked outside dropdown, close it
                    dropdownManager.Close();
                    continuousScrollHandler.Reset();
                    return true;
                }
            }

            // Handle filter clear button click
            if (uiBuilder.FilterClearButton != null && uiBuilder.FilterClearButton.containsPoint(x, y))
            {
                string? currentFilter = state.GetModFilter(categoryManager.CurrentCategory);
                if (!string.IsNullOrEmpty(currentFilter))
                {
                    state.SetModFilter(categoryManager.CurrentCategory, null);
                    state.ScrollOffset = 0;
                    if (playSound) Game1.playSound("smallSelect");
                    return true;
                }
            }

            // Handle search clear button click
            if (uiBuilder.SearchClearButton != null && uiBuilder.SearchClearButton.containsPoint(x, y))
            {
                if (!string.IsNullOrEmpty(searchManager.CurrentSearchText))
                {
                    searchManager.Clear();
                    state.SetSearchText(categoryManager.CurrentCategory, null);
                    state.ScrollOffset = 0;
                    if (playSound) Game1.playSound("smallSelect");
                    return true;
                }
            }

            // Handle dropdown button click (excluding clear button area)
            if (uiBuilder.ModFilterDropdown != null && uiBuilder.ModFilterDropdown.containsPoint(x, y))
            {
                // Don't toggle if clicking the clear button
                if (uiBuilder.FilterClearButton == null || !uiBuilder.FilterClearButton.containsPoint(x, y))
                {
                    dropdownManager.Toggle();
                    continuousScrollHandler.Reset();
                    if (playSound) Game1.playSound("smallSelect");
                    return true;
                }
            }

            // Category tabs
            if (HandleTabClick(uiBuilder.AllTab, OutfitCategoryManager.Category.All, x, y, playSound))
                return true;
            if (HandleTabClick(uiBuilder.ShirtsTab, OutfitCategoryManager.Category.Shirts, x, y, playSound))
                return true;
            if (HandleTabClick(uiBuilder.PantsTab, OutfitCategoryManager.Category.Pants, x, y, playSound))
                return true;
            if (HandleTabClick(uiBuilder.HatsTab, OutfitCategoryManager.Category.Hats, x, y, playSound))
                return true;

            // Item slots
            if (HandleItemSlotClick(x, y, playSound))
                return true;

            // Apply button
            if (uiBuilder.ApplyButton.containsPoint(x, y))
            {
                onApplyOutfit();
                uiBuilder.ShowSavedMessage();
                if (playSound) Game1.playSound("coin");
                return true;
            }

            // Reset button
            if (uiBuilder.ResetButton.containsPoint(x, y))
            {
                onResetOutfit();
                if (playSound) Game1.playSound("drumkit6");
                return true;
            }

            // Direction preview arrows
            if (uiBuilder.LeftArrowButton.containsPoint(x, y))
            {
                uiBuilder.RotatePreviewLeft();
                if (playSound) Game1.playSound("shwip");
                return true;
            }
            if (uiBuilder.RightArrowButton.containsPoint(x, y))
            {
                uiBuilder.RotatePreviewRight();
                if (playSound) Game1.playSound("shwip");
                return true;
            }

            // Save button - opens SaveSet overlay (swaps active menu)
            if (uiBuilder.SaveButton.containsPoint(x, y))
            {
                Game1.activeClickableMenu = new SaveSetOverlay(Game1.activeClickableMenu, templateManager, () => showSavedMessage());
                if (playSound) Game1.playSound("bigSelect");
                return true;
            }

            // Templates button - opens overlay
            if (uiBuilder.TemplatesButton.containsPoint(x, y))
            {
                setTemplatesOverlay(new TemplatesOverlay());
                if (playSound) Game1.playSound("bigSelect");
                return true;
            }

            // Close button - reverts to applied outfit and closes
            if (uiBuilder.CloseButton.containsPoint(x, y))
            {
                onRevertAndClose();
                if (playSound) Game1.playSound("bigDeSelect");
                return true;
            }

            return false;
        }

        private bool HandleTabClick(StardewValley.Menus.ClickableComponent tab, OutfitCategoryManager.Category targetCategory, int x, int y, bool playSound)
        {
            if (!tab.containsPoint(x, y))
                return false;

            if (categoryManager.CurrentCategory != targetCategory)
            {
                categoryManager.CurrentCategory = targetCategory;
                state.ScrollOffset = 0;

                if (mod.GetConfig().ResetFilterOnTabSwitch)
                {
                    state.SetModFilter(categoryManager.CurrentCategory, null);
                }

                if (mod.GetConfig().ResetSearchOnTabSwitch)
                {
                    state.SetSearchText(categoryManager.CurrentCategory, null);
                    searchManager.Clear();
                }
                else
                {
                    state.SetSearchText(categoryManager.CurrentCategory, searchManager.CurrentSearchText);
                }

                if (playSound) Game1.playSound("smallSelect");
            }
            return true;
        }

        private bool HandleItemSlotClick(int x, int y, bool playSound)
        {
            for (int i = 0; i < uiBuilder.ItemSlots.Count; i++)
            {
                if (uiBuilder.ItemSlots[i].containsPoint(x, y))
                {
                    int row = i / uiBuilder.COLUMNS;
                    int col = i % uiBuilder.COLUMNS;
                    int listIndex = state.ScrollOffset * uiBuilder.COLUMNS + row * uiBuilder.COLUMNS + col;
                    if (listIndex < getCurrentListCount())
                    {
                        if (categoryManager.CurrentCategory == OutfitCategoryManager.Category.All)
                        {
                            var allItems = getCurrentAllItems();
                            if (listIndex < allItems.Count)
                            {
                                var (itemCategory, itemId) = allItems[listIndex];
                                ApplyItemFromAllCategory(itemCategory, itemId);
                            }
                        }
                        else
                        {
                            state.SetCurrentIndex(categoryManager.CurrentCategory, listIndex);
                            ApplyCurrentSelection();
                        }
                        if (playSound) Game1.playSound("stoneStep");
                    }
                    return true;
                }
            }
            return false;
        }

        private void ApplyCurrentSelection()
        {
            state.ApplySelection(
                categoryManager.CurrentCategory,
                getCurrentShirtIds(),
                getCurrentPantsIds(),
                getCurrentHatIds()
            );
            onOutfitChanged();
        }

        private void ApplyItemFromAllCategory(OutfitCategoryManager.Category itemCategory, string itemId)
        {
            if (itemCategory == OutfitCategoryManager.Category.Hats && ItemIdHelper.IsNoHatId(itemId))
            {
                Game1.player.hat.Value = null;
                // Update hat index to match the selected item in the hats list
                int hatIndex = getCurrentHatIds().IndexOf(itemId);
                if (hatIndex >= 0)
                    state.SetCurrentIndex(OutfitCategoryManager.Category.Hats, hatIndex);
                onOutfitChanged();
                return;
            }

            string? qualifiedId = ItemIdHelper.GetQualifiedId(itemId, itemCategory);
            if (qualifiedId == null)
                return;

            switch (itemCategory)
            {
                case OutfitCategoryManager.Category.Shirts:
                    Game1.player.shirtItem.Value = ItemRegistry.Create<Clothing>(qualifiedId);
                    Game1.player.FarmerRenderer.MarkSpriteDirty();
                    // Update shirt index to match the selected item in the shirts list
                    int shirtIndex = getCurrentShirtIds().IndexOf(itemId);
                    if (shirtIndex >= 0)
                        state.SetCurrentIndex(OutfitCategoryManager.Category.Shirts, shirtIndex);
                    break;

                case OutfitCategoryManager.Category.Pants:
                    Game1.player.pantsItem.Value = ItemRegistry.Create<Clothing>(qualifiedId);
                    Game1.player.FarmerRenderer.MarkSpriteDirty();
                    // Update pants index to match the selected item in the pants list
                    int pantsIndex = getCurrentPantsIds().IndexOf(itemId);
                    if (pantsIndex >= 0)
                        state.SetCurrentIndex(OutfitCategoryManager.Category.Pants, pantsIndex);
                    break;

                case OutfitCategoryManager.Category.Hats:
                    Game1.player.hat.Value = ItemRegistry.Create<Hat>(qualifiedId);
                    // Update hat index to match the selected item in the hats list
                    int hatIdx = getCurrentHatIds().IndexOf(itemId);
                    if (hatIdx >= 0)
                        state.SetCurrentIndex(OutfitCategoryManager.Category.Hats, hatIdx);
                    break;
            }
            onOutfitChanged();
        }

        public bool HandleScrollWheel(int direction)
        {
            // Handle dropdown scrolling if open (block item grid scrolling when dropdown is open)
            if (dropdownManager.IsOpen)
            {
                if (dropdownManager.HandleScrollWheel(direction))
                {
                    Game1.playSound("shiny4");
                }
                return true; // Always consume scroll events when dropdown is open
            }

            // Original item grid scrolling logic
            int totalRows = Math.Max(1, (int)Math.Ceiling(getCurrentListCount() / (float)uiBuilder.COLUMNS));
            int maxScroll = Math.Max(0, totalRows - uiBuilder.VISIBLE_ROWS);
            if (direction > 0 && state.ScrollOffset > 0)
            {
                state.ScrollOffset--;
                Game1.playSound("shiny4");
                return true;
            }
            else if (direction < 0 && state.ScrollOffset < maxScroll)
            {
                state.ScrollOffset++;
                Game1.playSound("shiny4");
                return true;
            }
            return false;
        }

        public bool HandleKeyPress(Keys key)
        {
            // Handle templates overlay input
            var templatesOverlay = getTemplatesOverlay();
            if (templatesOverlay != null)
            {
                templatesOverlay.receiveKeyPress(key);
                return true;
            }

            // Handle dropdown scrolling when dropdown is open
            bool wasDropdownOpen = dropdownManager.IsOpen;
            if (dropdownManager.HandleKeyPress(key))
            {
                // Reset scroll state if dropdown was just closed (e.g., via Escape key)
                if (wasDropdownOpen && !dropdownManager.IsOpen)
                {
                    continuousScrollHandler.Reset();
                }
                Game1.playSound("shiny4");
                return true;
            }

            // Handle scrolling with arrow keys only (removed WASD)
            int totalRows = Math.Max(1, (int)Math.Ceiling(getCurrentListCount() / (float)uiBuilder.COLUMNS));
            int maxScroll = Math.Max(0, totalRows - uiBuilder.VISIBLE_ROWS);

            // Scroll up (Up arrow only)
            if (key == Keys.Up)
            {
                if (state.ScrollOffset > 0)
                {
                    state.ScrollOffset--;
                    Game1.playSound("shiny4");
                    return true;
                }
            }

            // Scroll down (Down arrow only)
            if (key == Keys.Down)
            {
                if (state.ScrollOffset < maxScroll)
                {
                    state.ScrollOffset++;
                    Game1.playSound("shiny4");
                    return true;
                }
            }

            // Page up (Left arrow only)
            if (key == Keys.Left)
            {
                if (state.ScrollOffset > 0)
                {
                    state.ScrollOffset = Math.Max(0, state.ScrollOffset - uiBuilder.VISIBLE_ROWS);
                    Game1.playSound("shiny4");
                    return true;
                }
            }

            // Page down (Right arrow only)
            if (key == Keys.Right)
            {
                if (state.ScrollOffset < maxScroll)
                {
                    state.ScrollOffset = Math.Min(maxScroll, state.ScrollOffset + uiBuilder.VISIBLE_ROWS);
                    Game1.playSound("shiny4");
                    return true;
                }
            }

            // Handle Escape to close with revert
            // Only Escape closes the menu - other menu buttons (like E) are used for typing
            if (key == Keys.Escape)
            {
                onRevertAndClose();
                Game1.playSound("bigDeSelect");
                return true;
            }

            // Don't pass menu button keys (like E) to base class - they should be used for typing
            if (Game1.options.doesInputListContain(Game1.options.menuButton, key))
            {
                return true; // Block menu button from closing the menu
            }

            return false;
        }
    }
}
