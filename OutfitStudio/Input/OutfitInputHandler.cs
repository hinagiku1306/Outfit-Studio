using System;
using System.Collections.Generic;
using OutfitStudio.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Objects;

namespace OutfitStudio
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
        private readonly OutfitSetStore outfitSetStore;

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
        private readonly Func<WardrobeOverlay?> getWardrobeOverlay;
        private readonly Action<WardrobeOverlay?> setWardrobeOverlay;
        private readonly Func<OutfitMenu?> getParentMenu;
        private readonly Func<DyeColorManager> getDyeColorManager;
        private readonly Func<OutfitCategoryManager.Category> getLastColorCategory;
        private readonly Action<OutfitCategoryManager.Category> setLastColorCategory;

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
            Func<WardrobeOverlay?> getWardrobeOverlay,
            Action<WardrobeOverlay?> setWardrobeOverlay,
            Func<OutfitMenu?> getParentMenu,
            OutfitSetStore outfitSetStore,
            Action showSavedMessage,
            Func<DyeColorManager> getDyeColorManager,
            Func<OutfitCategoryManager.Category> getLastColorCategory,
            Action<OutfitCategoryManager.Category> setLastColorCategory)
        {
            this.categoryManager = categoryManager;
            this.state = state;
            this.uiBuilder = uiBuilder;
            this.dropdownManager = dropdownManager;
            this.searchManager = searchManager;
            this.continuousScrollHandler = continuousScrollHandler;
            this.mod = mod;
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
            this.getWardrobeOverlay = getWardrobeOverlay;
            this.setWardrobeOverlay = setWardrobeOverlay;
            this.getParentMenu = getParentMenu;
            this.outfitSetStore = outfitSetStore;
            this.getDyeColorManager = getDyeColorManager;
            this.getLastColorCategory = getLastColorCategory;
            this.setLastColorCategory = setLastColorCategory;
        }

        public bool HandleLeftClick(int x, int y, bool playSound)
        {
            var wardrobeOverlay = getWardrobeOverlay();
            if (wardrobeOverlay != null)
            {
                wardrobeOverlay.receiveLeftClick(x, y, playSound);

                if (wardrobeOverlay.readyToClose())
                {
                    setWardrobeOverlay(null);
                }

                return true;
            }

            var dcm = getDyeColorManager();
            if (dcm.IsOpen)
            {
                dcm.HandleClick(x, y, out bool consumed);
                if (consumed) return true;
            }

            if (searchManager.IsPointInBounds(x, y))
                searchManager.Focus();
            else
                searchManager.Unfocus();

            if (dropdownManager.IsOpen)
            {
                if (uiBuilder.CloseButton.containsPoint(x, y))
                {
                    dropdownManager.Close();
                    onRevertAndClose();
                    if (playSound) Game1.playSound("bigDeSelect");
                    return true;
                }

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

                if (uiBuilder.AllTab.containsPoint(x, y) ||
                    uiBuilder.ShirtsTab.containsPoint(x, y) ||
                    uiBuilder.PantsTab.containsPoint(x, y) ||
                    uiBuilder.HatsTab.containsPoint(x, y) ||
                    uiBuilder.ApplyButton.containsPoint(x, y) ||
                    uiBuilder.ResetButton.containsPoint(x, y) ||
                    uiBuilder.SaveButton.containsPoint(x, y) ||
                    uiBuilder.WardrobeButton.containsPoint(x, y) ||
                    uiBuilder.GearButton.containsPoint(x, y) ||
                    uiBuilder.ScheduleButton.containsPoint(x, y) ||
                    uiBuilder.DyeColorButton.containsPoint(x, y))
                {
                    dropdownManager.Close();
                    continuousScrollHandler.Reset();
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
                    dropdownManager.Close();
                    continuousScrollHandler.Reset();
                    return true;
                }
            }

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

            if (uiBuilder.ModFilterDropdown != null && uiBuilder.ModFilterDropdown.containsPoint(x, y))
            {
                if (uiBuilder.FilterClearButton == null || !uiBuilder.FilterClearButton.containsPoint(x, y))
                {
                    dropdownManager.Toggle();
                    continuousScrollHandler.Reset();
                    if (playSound) Game1.playSound("smallSelect");
                    return true;
                }
            }

            if (HandleTabClick(uiBuilder.AllTab, OutfitCategoryManager.Category.All, x, y, playSound))
                return true;
            if (HandleTabClick(uiBuilder.ShirtsTab, OutfitCategoryManager.Category.Shirts, x, y, playSound))
                return true;
            if (HandleTabClick(uiBuilder.PantsTab, OutfitCategoryManager.Category.Pants, x, y, playSound))
                return true;
            if (HandleTabClick(uiBuilder.HatsTab, OutfitCategoryManager.Category.Hats, x, y, playSound))
                return true;

            if (HandleItemSlotClick(x, y, playSound))
                return true;

            if (HandleGridScrollArrowClick(x, y, playSound))
                return true;

            if (uiBuilder.DyeColorButton.containsPoint(x, y))
            {
                Color currentColor = GetActiveItemColor();
                Rectangle menuBounds = new Rectangle(uiBuilder.X, uiBuilder.Y, uiBuilder.Width, uiBuilder.Height);
                dcm.Toggle(menuBounds, currentColor);
                UpdateDyeColorIsDyeable();
                if (playSound) Game1.playSound("smallSelect");
                return true;
            }

            if (uiBuilder.ApplyButton.containsPoint(x, y))
            {
                onApplyOutfit();
                uiBuilder.ShowAppliedMessage();
                if (playSound) Game1.playSound("coin");
                return true;
            }

            if (uiBuilder.ResetButton.containsPoint(x, y))
            {
                onResetOutfit();
                if (playSound) Game1.playSound("drumkit6");
                return true;
            }

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

            // Type B swap: opens SaveSet overlay
            if (uiBuilder.SaveButton.containsPoint(x, y))
            {
                Game1.activeClickableMenu = new SaveSetOverlay(Game1.activeClickableMenu, outfitSetStore, () => showSavedMessage());
                if (playSound) Game1.playSound("bigSelect");
                return true;
            }

            if (uiBuilder.WardrobeButton.containsPoint(x, y))
            {
                setWardrobeOverlay(new WardrobeOverlay(outfitSetStore, getParentMenu()));
                if (playSound) Game1.playSound("bigSelect");
                return true;
            }

            // Type B swap: opens config overlay
            if (uiBuilder.GearButton.containsPoint(x, y))
            {
                Game1.activeClickableMenu = new ConfigOverlay(Game1.activeClickableMenu, mod);
                if (playSound) Game1.playSound("bigSelect");
                return true;
            }

            // Type B swap: opens schedule menu
            if (uiBuilder.ScheduleButton.containsPoint(x, y))
            {
                var scheduleStore = mod.GetScheduleStore();
                if (scheduleStore != null)
                {
                    Game1.activeClickableMenu = new ScheduleMenu(Game1.activeClickableMenu, mod, outfitSetStore, scheduleStore);
                    if (playSound) Game1.playSound("bigSelect");
                }
                return true;
            }

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

                var tabDcm = getDyeColorManager();
                if (tabDcm.IsOpen)
                    tabDcm.SetSlidersFromColor(GetActiveItemColor());

                UpdateDyeColorIsDyeable();

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

                            // Track color category and auto-apply dye color
                            var currentCat = categoryManager.CurrentCategory;
                            if (currentCat == OutfitCategoryManager.Category.Shirts || currentCat == OutfitCategoryManager.Category.Pants)
                            {
                                setLastColorCategory(currentCat);
                                var slotDcm = getDyeColorManager();
                                if (slotDcm.IsOpen)
                                    slotDcm.ApplyToActiveItem(currentCat);
                            }

                            UpdateDyeColorIsDyeable();
                        }
                        if (playSound) Game1.playSound("stoneStep");
                    }
                    return true;
                }
            }
            return false;
        }

        private bool HandleGridScrollArrowClick(int x, int y, bool playSound)
        {
            int totalRows = Math.Max(1, (int)Math.Ceiling(getCurrentListCount() / (float)uiBuilder.COLUMNS));
            int maxScroll = Math.Max(0, totalRows - uiBuilder.VISIBLE_ROWS);

            if (uiBuilder.GridScrollUpButton.containsPoint(x, y) && state.ScrollOffset > 0)
            {
                state.ScrollOffset--;
                if (playSound) Game1.playSound("shiny4");
                return true;
            }

            if (uiBuilder.GridScrollDownButton.containsPoint(x, y) && state.ScrollOffset < maxScroll)
            {
                state.ScrollOffset++;
                if (playSound) Game1.playSound("shiny4");
                return true;
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
                    int shirtIndex = getCurrentShirtIds().IndexOf(itemId);
                    if (shirtIndex >= 0)
                        state.SetCurrentIndex(OutfitCategoryManager.Category.Shirts, shirtIndex);
                    break;

                case OutfitCategoryManager.Category.Pants:
                    Game1.player.pantsItem.Value = ItemRegistry.Create<Clothing>(qualifiedId);
                    Game1.player.FarmerRenderer.MarkSpriteDirty();
                    int pantsIndex = getCurrentPantsIds().IndexOf(itemId);
                    if (pantsIndex >= 0)
                        state.SetCurrentIndex(OutfitCategoryManager.Category.Pants, pantsIndex);
                    break;

                case OutfitCategoryManager.Category.Hats:
                    Game1.player.hat.Value = ItemRegistry.Create<Hat>(qualifiedId);
                    int hatIdx = getCurrentHatIds().IndexOf(itemId);
                    if (hatIdx >= 0)
                        state.SetCurrentIndex(OutfitCategoryManager.Category.Hats, hatIdx);
                    break;
            }

            // Track color category and auto-apply dye color for dyeable items
            if (itemCategory == OutfitCategoryManager.Category.Shirts || itemCategory == OutfitCategoryManager.Category.Pants)
            {
                setLastColorCategory(itemCategory);
                var allDcm = getDyeColorManager();
                if (allDcm.IsOpen)
                    allDcm.ApplyToActiveItem(itemCategory);
            }

            UpdateDyeColorIsDyeable();
            onOutfitChanged();
        }

        public bool HandleScrollWheel(int direction)
        {
            var wardrobeOverlay = getWardrobeOverlay();
            if (wardrobeOverlay != null)
            {
                wardrobeOverlay.receiveScrollWheelAction(direction);
                return true;
            }

            if (dropdownManager.IsOpen)
            {
                if (dropdownManager.HandleScrollWheel(direction))
                {
                    Game1.playSound("shiny4");
                }
                return true;
            }

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
            var wardrobeOverlay = getWardrobeOverlay();
            if (wardrobeOverlay != null)
            {
                wardrobeOverlay.receiveKeyPress(key);

                if (wardrobeOverlay.readyToClose())
                {
                    setWardrobeOverlay(null);
                }

                return true;
            }

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

            if (mod.GetConfig().ArrowKeyScrolling)
            {
                int totalRows = Math.Max(1, (int)Math.Ceiling(getCurrentListCount() / (float)uiBuilder.COLUMNS));
                int maxScroll = Math.Max(0, totalRows - uiBuilder.VISIBLE_ROWS);

                if (key == Keys.Up && state.ScrollOffset > 0)
                {
                    state.ScrollOffset--;
                    Game1.playSound("shiny4");
                    return true;
                }

                if (key == Keys.Down && state.ScrollOffset < maxScroll)
                {
                    state.ScrollOffset++;
                    Game1.playSound("shiny4");
                    return true;
                }

                if (key == Keys.Left && state.ScrollOffset > 0)
                {
                    state.ScrollOffset = Math.Max(0, state.ScrollOffset - uiBuilder.VISIBLE_ROWS);
                    Game1.playSound("shiny4");
                    return true;
                }

                if (key == Keys.Right && state.ScrollOffset < maxScroll)
                {
                    state.ScrollOffset = Math.Min(maxScroll, state.ScrollOffset + uiBuilder.VISIBLE_ROWS);
                    Game1.playSound("shiny4");
                    return true;
                }
            }

            // Only Escape closes — other menu buttons (like E) are used for typing
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

        private Color GetActiveItemColor()
        {
            var category = GetActiveColorCategory();
            return category switch
            {
                OutfitCategoryManager.Category.Shirts => Game1.player.GetShirtColor(),
                OutfitCategoryManager.Category.Pants => Game1.player.GetPantsColor(),
                _ => Color.White
            };
        }

        private OutfitCategoryManager.Category GetActiveColorCategory()
        {
            var category = categoryManager.CurrentCategory;
            if (category == OutfitCategoryManager.Category.All)
                return getLastColorCategory();
            return category;
        }

        private void UpdateDyeColorIsDyeable()
        {
            var dcm = getDyeColorManager();
            if (!dcm.IsOpen) return;

            var category = GetActiveColorCategory();
            dcm.IsDyeable = category switch
            {
                OutfitCategoryManager.Category.Shirts => Game1.player.CanDyeShirt(),
                OutfitCategoryManager.Category.Pants => Game1.player.CanDyePants(),
                _ => false
            };
        }
    }
}
