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

        public bool IsOverlayBlocking { get; set; } = false;
        public bool ShowItemInfo => showItemInfo;

        public void NotifyOutfitApplied(OutfitSet? set = null)
        {
            state.SaveAppliedOutfit();
            uiBuilder.MarkPreviewDirty();
            if (set != null)
                mod.GetScheduleEngine()?.SetManualOutfit(set.Id);
        }

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

            categoryManager.CurrentCategory = OutfitCategoryManager.Category.All;

            itemRenderer = new OutfitItemRenderer(mod.Monitor, mod.Helper.ModRegistry);
            state = new OutfitState();
            uiBuilder = new OutfitUIBuilder(mod.GetConfig());
            dropdownManager = new OutfitDropdownManager(filterManager, categoryManager, state, uiBuilder);
            searchManager = new OutfitSearchManager(uiBuilder, state);
            tooltipRenderer = new OutfitTooltipRenderer(filterManager, categoryManager);
            continuousScrollHandler = new ContinuousScrollHandler(initialDelay: 400, repeatDelay: 100);

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

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);

            uiBuilder.Recalculate();
            searchManager.UpdateBounds();

            width = uiBuilder.Width;
            height = uiBuilder.Height;
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;

            int totalRows = Math.Max(1, (int)Math.Ceiling(itemListProvider.GetCurrentListCount() / (float)uiBuilder.COLUMNS));
            state.ScrollOffset = UIHelpers.ClampScrollOffset(state.ScrollOffset, totalRows, uiBuilder.VISIBLE_ROWS);

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


        private void ResetOutfit()
        {
            state.ResetToApplied(
                itemListProvider.GetCurrentShirtIds(),
                itemListProvider.GetCurrentPantsIds(),
                itemListProvider.GetCurrentHatIds()
            );

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


        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            bool clickInPanel = dyeColorManager.IsOpen && dyeColorManager.Bounds.Contains(x, y);
            bool clickOnFloatingButton = uiBuilder.GearButton.containsPoint(x, y) || uiBuilder.DyeColorButton.containsPoint(x, y);
            if (!isWithinBounds(x, y) && !clickInPanel && !clickOnFloatingButton && mod.GetConfig().CloseOnClickOutside)
            {
                RevertAndClose();
                return;
            }

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

            bool hasOverlay = IsOverlayBlocking;
            searchManager.Update(allowFocus: !hasOverlay);

            if (searchManager.HasSearchTextChanged)
            {
                state.SetSearchText(categoryManager.CurrentCategory, searchManager.CurrentSearchText);
                state.ScrollOffset = 0;
            }

            HandleItemInfoToggle();

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
            bool hasOverlay = IsOverlayBlocking;
            bool oldSuppressHover = UIHelpers.SuppressHover;
            if (hasOverlay) UIHelpers.SuppressHover = true;

            OutfitUIBuilder.DrawOverlay(b);

            UIHelpers.DrawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

            uiBuilder.DrawPlayerPreview(b);
            uiBuilder.DrawLookupIcon(b);
            uiBuilder.DrawSavedMessage(b);
            uiBuilder.DrawLeftPanelButtons(b);

            UIHelpers.DrawTabWithText(b, uiBuilder.AllTab, TranslationCache.TabAll,
                categoryManager.CurrentCategory == OutfitCategoryManager.Category.All);

            UIHelpers.DrawTabWithText(b, uiBuilder.ShirtsTab, TranslationCache.TabShirts,
                categoryManager.CurrentCategory == OutfitCategoryManager.Category.Shirts);

            UIHelpers.DrawTabWithText(b, uiBuilder.PantsTab, TranslationCache.TabPants,
                categoryManager.CurrentCategory == OutfitCategoryManager.Category.Pants);

            UIHelpers.DrawTabWithText(b, uiBuilder.HatsTab, TranslationCache.TabHats,
                categoryManager.CurrentCategory == OutfitCategoryManager.Category.Hats);

            searchManager.Draw(b);

            uiBuilder.DrawModFilterDropdown(b, state.GetModFilter(categoryManager.CurrentCategory), dropdownManager.IsOpen);

            var currentCategory = categoryManager.CurrentCategory;
            var allItems = currentCategory == OutfitCategoryManager.Category.All ? itemListProvider.GetCurrentAllItems() : null;
            var shirtIds = currentCategory != OutfitCategoryManager.Category.All ? itemListProvider.GetCurrentShirtIds() : null;
            var pantsIds = currentCategory != OutfitCategoryManager.Category.All ? itemListProvider.GetCurrentPantsIds() : null;
            var hatIds = currentCategory != OutfitCategoryManager.Category.All ? itemListProvider.GetCurrentHatIds() : null;
            int listCount = allItems?.Count ?? itemListProvider.GetCurrentListCount();

            string equippedShirtId = OutfitState.GetClothingId(Game1.player.shirtItem.Value);
            string equippedPantsId = OutfitState.GetClothingId(Game1.player.pantsItem.Value);
            string equippedHatId = OutfitState.GetHatIdFromItem(Game1.player.hat.Value);

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
                {
                    b.Draw(Game1.staminaRect, slot, Color.Wheat);
                }

                if (!hasOverlay)
                {
                    bool isHovered = uiBuilder.ItemSlots[i].containsPoint(Game1.getMouseX(), Game1.getMouseY());
                    if (isHovered)
                    {
                        b.Draw(Game1.staminaRect, slot, HoverEffectColor);
                        hoveredIndex = listIndex; // Track for tooltip rendering later
                    }
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
                    itemRenderer.DrawItemSprite(b, currentCategory, listIndex, slot, shirtIds!, pantsIds!, hatIds!);
                }
            }

            uiBuilder.DrawBottomButtons(b);
            uiBuilder.DrawCloseButton(b);
            uiBuilder.DrawFloatingButtons(b);

            if (dyeColorManager.IsOpen)
                dyeColorManager.Draw(b);

            if (!hasOverlay)
            {
                if (dropdownManager.IsOpen)
                {
                    drawingHelper.DrawDropdownOptions(b, state.GetModFilter(categoryManager.CurrentCategory));

                    if (!string.IsNullOrEmpty(drawingHelper.HoveredTruncatedFilterText))
                    {
                        drawingHelper.DrawFilterTooltip(b, drawingHelper.HoveredTruncatedFilterText);
                    }
                }

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

                if (uiBuilder.LookupButton != null && uiBuilder.LookupButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
                {
                    drawingHelper.DrawLookupTooltip(b);
                }

            }

            UIHelpers.SuppressHover = oldSuppressHover;

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
