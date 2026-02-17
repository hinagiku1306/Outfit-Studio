using System;
using System.Collections.Generic;
using OutfitStudio.Models;
using OutfitStudio.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    public class EditOutfitMenu : IClickableMenu
    {
        private readonly IClickableMenu parentMenu;
        private readonly OutfitSet editingSet;
        private readonly OutfitSetStore store;
        private readonly ModEntry mod;

        private readonly OutfitCategoryManager categoryManager;
        private readonly OutfitFilterManager filterManager;
        private readonly OutfitItemRenderer itemRenderer;
        private readonly OutfitState state;
        private readonly EditOutfitUIBuilder uiBuilder;
        private readonly OutfitDropdownManager dropdownManager;
        private readonly OutfitSearchManager searchManager;
        private readonly OutfitTooltipRenderer tooltipRenderer;
        private readonly ContinuousScrollHandler continuousScrollHandler;
        private readonly OutfitItemListProvider itemListProvider;
        private readonly OutfitDrawingHelper drawingHelper;
        private readonly OutfitInputHandler inputHandler;

        private readonly DyeColorManager dyeColorManager;
        private OutfitCategoryManager.Category lastColorCategory = OutfitCategoryManager.Category.Pants;

        private readonly Action? onClose;

        // Pre-menu player snapshot for revert on close
        private readonly string preMenuShirtId;
        private readonly string preMenuPantsId;
        private readonly string preMenuHatId;
        private readonly int preMenuHairId;
        private readonly Color preMenuHairColor;
        private readonly Color preMenuShirtColor;
        private readonly Color preMenuPantsColor;

        // Original set values — preserved for invalid items that can't be equipped
        private readonly string? origSetShirtId;
        private readonly string? origSetPantsId;
        private readonly string? origSetHatId;
        private readonly int? origSetHairId;
        private readonly string? origSetShirtColor;
        private readonly string? origSetPantsColor;
        private readonly string? origSetHairColor;

        public bool IsOverlayBlocking { get; set; } = false;
        public bool ShowItemInfo => ModEntry.Config.ShowItemInfo;

        public EditOutfitMenu(IClickableMenu parentMenu, OutfitSet editingSet, OutfitSetStore store,
            ModEntry mod, OutfitCategoryManager categoryManager, OutfitFilterManager filterManager,
            Action? onClose = null)
        {
            this.parentMenu = parentMenu ?? throw new ArgumentNullException(nameof(parentMenu));
            this.editingSet = editingSet ?? throw new ArgumentNullException(nameof(editingSet));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.mod = mod;
            this.categoryManager = categoryManager;
            this.filterManager = filterManager;
            this.onClose = onClose;

            // Snapshot current player equipment before equipping set items
            preMenuShirtId = OutfitState.GetClothingId(Game1.player.shirtItem.Value);
            preMenuPantsId = OutfitState.GetClothingId(Game1.player.pantsItem.Value);
            preMenuHatId = OutfitState.GetHatIdFromItem(Game1.player.hat.Value);
            preMenuHairId = Game1.player.hair.Value;
            preMenuHairColor = Game1.player.hairstyleColor.Value;
            preMenuShirtColor = Game1.player.shirtItem.Value != null ? Game1.player.GetShirtColor() : Color.White;
            preMenuPantsColor = Game1.player.pantsItem.Value != null ? Game1.player.GetPantsColor() : Color.White;

            // Snapshot original set values before applying (preserves invalid item IDs)
            origSetShirtId = editingSet.ShirtId;
            origSetPantsId = editingSet.PantsId;
            origSetHatId = editingSet.HatId;
            origSetHairId = editingSet.HairId;
            origSetShirtColor = editingSet.ShirtColor;
            origSetPantsColor = editingSet.PantsColor;
            origSetHairColor = editingSet.HairColor;

            // Equip the set's items so OutfitState captures them as "original".
            // For invalid items (removed mods etc.), explicitly unequip so the slot
            // shows empty rather than the player's currently worn item.
            store.ApplySet(editingSet);

            if (!string.IsNullOrEmpty(editingSet.ShirtId) && !ItemIdHelper.IsNoShirtId(editingSet.ShirtId)
                && !store.IsItemValid(editingSet.ShirtId, "(S)"))
                Game1.player.shirtItem.Value = null;

            if (!string.IsNullOrEmpty(editingSet.PantsId) && !ItemIdHelper.IsNoPantsId(editingSet.PantsId)
                && !store.IsItemValid(editingSet.PantsId, "(P)"))
                Game1.player.pantsItem.Value = null;

            if (!string.IsNullOrEmpty(editingSet.HatId) && !ItemIdHelper.IsNoHatId(editingSet.HatId)
                && !store.IsItemValid(editingSet.HatId, "(H)"))
                Game1.player.hat.Value = null;

            Game1.player.FarmerRenderer.MarkSpriteDirty();

            categoryManager.CurrentCategory = OutfitCategoryManager.Category.All;

            itemRenderer = new OutfitItemRenderer(mod.Monitor, mod.Helper.ModRegistry);
            state = new OutfitState();
            uiBuilder = new EditOutfitUIBuilder(mod.GetConfig());
            dropdownManager = new OutfitDropdownManager(filterManager, categoryManager, state, uiBuilder);
            searchManager = new OutfitSearchManager(uiBuilder, state);
            tooltipRenderer = new OutfitTooltipRenderer(filterManager, categoryManager);
            continuousScrollHandler = new ContinuousScrollHandler(initialDelay: 200, repeatDelay: 100);

            itemListProvider = new OutfitItemListProvider(filterManager, categoryManager, state);
            drawingHelper = new OutfitDrawingHelper(uiBuilder, dropdownManager, state, mod);
            dyeColorManager = new DyeColorManager(
                onColorChanged: () => uiBuilder.MarkPreviewDirty(),
                getActiveCategory: () => GetActiveColorCategory()
            );
            inputHandler = new OutfitInputHandler(
                categoryManager, state, uiBuilder, dropdownManager, searchManager, continuousScrollHandler, mod,
                onRevertAndClose: RevertAndClose,
                onApplyOutfit: SaveOutfit,
                onResetOutfit: ResetOutfit,
                onOutfitChanged: () => uiBuilder.MarkPreviewDirty(),
                getCurrentListCount: () => itemListProvider.GetCurrentListCount(),
                getCurrentShirtIds: () => itemListProvider.GetCurrentShirtIds(),
                getCurrentPantsIds: () => itemListProvider.GetCurrentPantsIds(),
                getCurrentHatIds: () => itemListProvider.GetCurrentHatIds(),
                getCurrentHairIds: () => itemListProvider.GetCurrentHairIds(),
                getCurrentAllItems: () => itemListProvider.GetCurrentAllItems(),
                outfitSetStore: store,
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
                    OutfitCategoryManager.Category.Hair => Game1.player.hairstyleColor.Value,
                    _ => Color.White
                };
                Rectangle menuBounds = new Rectangle(uiBuilder.X, uiBuilder.Y, uiBuilder.Width, uiBuilder.Height);
                dyeColorManager.Open(menuBounds, currentColor);

                var autoOpenCategory = categoryManager.CurrentCategory;
                dyeColorManager.IsDyeable = autoOpenCategory switch
                {
                    OutfitCategoryManager.Category.Shirts => Game1.player.CanDyeShirt(),
                    OutfitCategoryManager.Category.Pants => Game1.player.CanDyePants(),
                    _ => true
                };
            }
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            parentMenu.gameWindowSizeChanged(oldBounds, newBounds);

            uiBuilder.Recalculate();
            searchManager.UpdateBounds();
            width = uiBuilder.Width;
            height = uiBuilder.Height;
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;

            int totalRows = Math.Max(1, (int)Math.Ceiling(itemListProvider.GetCurrentListCount() / (float)uiBuilder.COLUMNS));
            state.ScrollOffset = UIHelpers.ClampScrollOffset(state.ScrollOffset, totalRows, uiBuilder.VISIBLE_ROWS);

            if (dyeColorManager.IsOpen)
            {
                Rectangle menuBoundsRect = new Rectangle(uiBuilder.X, uiBuilder.Y, uiBuilder.Width, uiBuilder.Height);
                dyeColorManager.UpdateParentBounds(menuBoundsRect);
            }
        }

        private void ResetOutfit()
        {
            // Re-equip the set's original items
            store.ApplySet(editingSet);

            // ApplySet skips hair when the set has no HairId — restore pre-menu hair
            if (!editingSet.HairId.HasValue || !ModEntry.Config.IncludeHairInOutfitSets)
            {
                OutfitState.ApplyHair(preMenuHairId);
                Game1.player.changeHairColor(preMenuHairColor);
            }

            state.ShirtIndex = Math.Max(0, categoryManager.ShirtIds.IndexOf(
                OutfitState.GetClothingId(Game1.player.shirtItem.Value)));
            state.PantsIndex = Math.Max(0, categoryManager.PantsIds.IndexOf(
                OutfitState.GetClothingId(Game1.player.pantsItem.Value)));
            state.HatIndex = Math.Max(0, categoryManager.HatIds.IndexOf(
                OutfitState.GetHatIdFromItem(Game1.player.hat.Value)));
            state.HairIndex = Math.Max(0, categoryManager.HairIds.IndexOf(Game1.player.hair.Value));

            if (dyeColorManager.IsOpen)
            {
                var category = GetActiveColorCategory();
                Color restoredColor = category switch
                {
                    OutfitCategoryManager.Category.Shirts => Game1.player.GetShirtColor(),
                    OutfitCategoryManager.Category.Pants => Game1.player.GetPantsColor(),
                    OutfitCategoryManager.Category.Hair => Game1.player.hairstyleColor.Value,
                    _ => Color.White
                };
                dyeColorManager.SetSlidersFromColor(restoredColor);
            }

            uiBuilder.MarkPreviewDirty();
        }

        private void SaveOutfit()
        {
            bool includeHair = ModEntry.Config.IncludeHairInOutfitSets;

            // Only overwrite fields the user actually changed.
            // For unchanged slots, preserve original set values (may be invalid item IDs).
            string currentShirt = OutfitState.GetClothingId(Game1.player.shirtItem.Value);
            string currentPants = OutfitState.GetClothingId(Game1.player.pantsItem.Value);
            string currentHat = OutfitState.GetHatIdFromItem(Game1.player.hat.Value);

            bool shirtChanged = currentShirt != state.AppliedShirt;
            bool pantsChanged = currentPants != state.AppliedPants;
            bool hatChanged = currentHat != state.AppliedHat;

            if (shirtChanged)
            {
                editingSet.ShirtId = currentShirt;
                editingSet.ShirtColor = Game1.player.shirtItem.Value != null && Game1.player.CanDyeShirt()
                    ? ColorHelper.ToColorString(Game1.player.GetShirtColor())
                    : null;
            }
            else
            {
                editingSet.ShirtId = origSetShirtId;
                // Preserve color from player if the item is valid (user may have dyed it)
                editingSet.ShirtColor = Game1.player.shirtItem.Value != null
                    ? ColorHelper.ToColorString(Game1.player.GetShirtColor())
                    : origSetShirtColor;
            }

            if (pantsChanged)
            {
                editingSet.PantsId = currentPants;
                editingSet.PantsColor = Game1.player.pantsItem.Value != null && Game1.player.CanDyePants()
                    ? ColorHelper.ToColorString(Game1.player.GetPantsColor())
                    : null;
            }
            else
            {
                editingSet.PantsId = origSetPantsId;
                editingSet.PantsColor = Game1.player.pantsItem.Value != null
                    ? ColorHelper.ToColorString(Game1.player.GetPantsColor())
                    : origSetPantsColor;
            }

            editingSet.HatId = hatChanged ? currentHat : origSetHatId;

            if (includeHair)
            {
                bool hairChanged = Game1.player.hair.Value != state.AppliedHair;
                bool hairColorChanged = Game1.player.hairstyleColor.Value != state.AppliedHairColor;
                if (hairChanged)
                {
                    editingSet.HairId = Game1.player.hair.Value;
                    editingSet.HairColor = ColorHelper.ToColorString(Game1.player.hairstyleColor.Value);
                }
                else
                {
                    editingSet.HairId = origSetHairId;
                    editingSet.HairColor = hairColorChanged
                        ? ColorHelper.ToColorString(Game1.player.hairstyleColor.Value)
                        : origSetHairColor;
                }
            }

            RevertToPreMenuSnapshot();
            Game1.playSound("coin");
            CloseMenu();
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
            RevertToPreMenuSnapshot();
            CloseMenu();
        }

        private void RevertToPreMenuSnapshot()
        {
            OutfitState.ApplyShirt(preMenuShirtId);
            OutfitState.ApplyPants(preMenuPantsId);
            OutfitState.ApplyHat(preMenuHatId);
            OutfitState.ApplyHair(preMenuHairId);
            Game1.player.changeHairColor(preMenuHairColor);

            if (Game1.player.shirtItem.Value != null)
                Game1.player.shirtItem.Value.clothesColor.Set(preMenuShirtColor);
            Game1.player.changePantsColor(preMenuPantsColor);

            Game1.player.FarmerRenderer.MarkSpriteDirty();
        }

        private void CloseMenu()
        {
            onClose?.Invoke();
            Game1.activeClickableMenu = parentMenu;
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            bool clickInPanel = dyeColorManager.IsOpen && dyeColorManager.Bounds.Contains(x, y);
            bool clickOnFloatingButton = uiBuilder.DyeColorButton.containsPoint(x, y);
            if (!isWithinBounds(x, y) && !clickInPanel && !clickOnFloatingButton && mod.GetConfig().CloseOnClickOutside)
            {
                RevertAndClose();
                return;
            }

            // Block hair tab when config is disabled
            if (!ModEntry.Config.IncludeHairInOutfitSets && uiBuilder.HairTab.containsPoint(x, y))
                return;

            inputHandler.HandleLeftClick(x, y, playSound);
        }

        public override void leftClickHeld(int x, int y)
        {
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

            uiBuilder.Update((float)time.ElapsedGameTime.TotalMilliseconds);

            bool isHairTab = categoryManager.CurrentCategory == OutfitCategoryManager.Category.Hair;
            uiBuilder.SetHideHat(isHairTab && state.HideHatInPreview);

            searchManager.Update(allowFocus: !dropdownManager.IsOpen);

            if (searchManager.HasSearchTextChanged)
            {
                state.SetSearchText(categoryManager.CurrentCategory, searchManager.CurrentSearchText);
                state.ScrollOffset = 0;
            }

            dropdownManager.UpdateSearch();

            // Allow item info toggle keybind
            if (ModEntry.Config.ToggleItemInfoKey.JustPressed())
            {
                ModEntry.Config.ShowItemInfo = !ModEntry.Config.ShowItemInfo;
                ModEntry.PersistConfig();
                Game1.playSound(ModEntry.Config.ShowItemInfo ? "bigSelect" : "bigDeSelect");
            }

            int scrollAmount = continuousScrollHandler.Update(time, uiBuilder.VISIBLE_ROWS, out bool shouldPlaySound);
            if (scrollAmount != 0)
            {
                if (dropdownManager.IsOpen)
                {
                    if (dropdownManager.HandleScrollAmount(scrollAmount))
                    {
                        if (shouldPlaySound) Game1.playSound("shiny4");
                    }
                }
                else
                {
                    int totalRows = Math.Max(1, (int)Math.Ceiling(itemListProvider.GetCurrentListCount() / (float)uiBuilder.COLUMNS));
                    int newOffset = UIHelpers.ClampScrollOffset(state.ScrollOffset + scrollAmount, totalRows, uiBuilder.VISIBLE_ROWS);

                    if (newOffset != state.ScrollOffset)
                    {
                        state.ScrollOffset = newOffset;
                        if (shouldPlaySound) Game1.playSound("shiny4");
                    }
                }
            }
        }

        public override void draw(SpriteBatch b)
        {
            // Draw background overlay
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * BackgroundOverlayOpacity);

            UIHelpers.DrawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

            if (categoryManager.CurrentCategory == OutfitCategoryManager.Category.Hair)
                uiBuilder.DrawHideHatCheckbox(b, state.HideHatInPreview);

            uiBuilder.DrawPlayerPreview(b);
            uiBuilder.DrawSavedMessage(b);
            uiBuilder.DrawLeftPanelButtons(b);

            // Hair tab: inactive when IncludeHairInOutfitSets is disabled
            bool hairTabActive = ModEntry.Config.IncludeHairInOutfitSets;
            float hairTabOpacity = hairTabActive ? 1f : DisabledSectionOpacity;
            UIHelpers.DrawTabWithText(b, uiBuilder.HairTab, TranslationCache.TabHair,
                categoryManager.CurrentCategory == OutfitCategoryManager.Category.Hair, hairTabOpacity);

            uiBuilder.DrawTabDivider(b);

            UIHelpers.DrawTabWithText(b, uiBuilder.AllTab, TranslationCache.TabAll,
                categoryManager.CurrentCategory == OutfitCategoryManager.Category.All);

            UIHelpers.DrawTabWithText(b, uiBuilder.ShirtsTab, TranslationCache.TabShirts,
                categoryManager.CurrentCategory == OutfitCategoryManager.Category.Shirts);

            UIHelpers.DrawTabWithText(b, uiBuilder.PantsTab, TranslationCache.TabPants,
                categoryManager.CurrentCategory == OutfitCategoryManager.Category.Pants);

            UIHelpers.DrawTabWithText(b, uiBuilder.HatsTab, TranslationCache.TabHats,
                categoryManager.CurrentCategory == OutfitCategoryManager.Category.Hats);

            searchManager.Draw(b);

            uiBuilder.DrawModFilterDropdown(b, state.GetModFilter(categoryManager.CurrentCategory), dropdownManager.IsOpen, dropdownManager.SearchText);

            var currentCategory = categoryManager.CurrentCategory;
            var allItems = currentCategory == OutfitCategoryManager.Category.All ? itemListProvider.GetCurrentAllItems() : null;
            var shirtIds = currentCategory != OutfitCategoryManager.Category.All ? itemListProvider.GetCurrentShirtIds() : null;
            var pantsIds = currentCategory != OutfitCategoryManager.Category.All ? itemListProvider.GetCurrentPantsIds() : null;
            var hatIds = currentCategory != OutfitCategoryManager.Category.All ? itemListProvider.GetCurrentHatIds() : null;
            var hairIds = currentCategory == OutfitCategoryManager.Category.Hair ? itemListProvider.GetCurrentHairIds() : null;
            int listCount = allItems?.Count ?? itemListProvider.GetCurrentListCount();

            string equippedShirtId = OutfitState.GetClothingId(Game1.player.shirtItem.Value);
            string equippedPantsId = OutfitState.GetClothingId(Game1.player.pantsItem.Value);
            string equippedHatId = OutfitState.GetHatIdFromItem(Game1.player.hat.Value);
            int equippedHairId = Game1.player.hair.Value;

            uiBuilder.DrawItemList(b, state.ScrollOffset, listCount);

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

                if (allItems != null && listIndex < allItems.Count)
                {
                    var (itemCategory, itemId) = allItems[listIndex];
                    isSelected = itemCategory switch
                    {
                        OutfitCategoryManager.Category.Shirts => itemId == equippedShirtId,
                        OutfitCategoryManager.Category.Pants => itemId == equippedPantsId,
                        OutfitCategoryManager.Category.Hats => itemId == equippedHatId,
                        _ => false
                    };
                }
                else if (currentCategory == OutfitCategoryManager.Category.Hair)
                {
                    isSelected = hairIds != null && listIndex < hairIds.Count && hairIds[listIndex] == equippedHairId;
                }
                else
                {
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

                if (isSelected)
                    b.Draw(Game1.staminaRect, slot, Color.Wheat);

                bool isHovered = uiBuilder.ItemSlots[i].containsPoint(Game1.getMouseX(), Game1.getMouseY());
                if (isHovered)
                {
                    b.Draw(Game1.staminaRect, slot, HoverEffectColor);
                    hoveredIndex = listIndex;
                }

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
                    itemRenderer.DrawItemSprite(b, currentCategory, listIndex, slot, shirtIds!, pantsIds!, hatIds!, hairIds);
                }
            }

            uiBuilder.DrawBottomButtons(b);
            uiBuilder.DrawCloseButton(b);
            uiBuilder.DrawFloatingButtons(b);

            if (dyeColorManager.IsOpen)
                dyeColorManager.Draw(b);

            if (dropdownManager.IsOpen)
            {
                drawingHelper.DrawDropdownOptions(b, state.GetModFilter(categoryManager.CurrentCategory));

                if (!string.IsNullOrEmpty(drawingHelper.HoveredTruncatedFilterText))
                    drawingHelper.DrawFilterTooltip(b, drawingHelper.HoveredTruncatedFilterText);
            }

            if (ShowItemInfo && hoveredIndex >= 0 && !dropdownManager.IsOpen &&
                currentCategory != OutfitCategoryManager.Category.Hair)
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

            if (ShowItemInfo && hoveredIndex >= 0 && !dropdownManager.IsOpen &&
                currentCategory == OutfitCategoryManager.Category.Hair &&
                hairIds != null && hoveredIndex < hairIds.Count)
            {
                string tooltipText = TranslationCache.ItemHairIdTemplate.Replace("{{id}}", hairIds[hoveredIndex].ToString());
                IClickableMenu.drawToolTip(b, tooltipText, "", null);
            }

            drawMouse(b);
        }

        protected override void cleanupBeforeExit()
        {
            base.cleanupBeforeExit();
            filterManager.ClearSearchCaches();
            itemRenderer.ClearCache();
            tooltipRenderer.ClearCache();
            uiBuilder.SafeDispose();
        }

        public override void emergencyShutDown()
        {
            dyeColorManager.Close();
            filterManager.ClearSearchCaches();
            itemRenderer.ClearCache();
            tooltipRenderer.ClearCache();
            uiBuilder.SafeDispose();
            base.emergencyShutDown();
        }
    }
}
