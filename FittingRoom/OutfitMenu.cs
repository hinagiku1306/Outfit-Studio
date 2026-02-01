using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Objects;

namespace FittingRoom
{
    public class OutfitMenu : IClickableMenu
    {
        // Helper classes
        private readonly OutfitCategoryManager categoryManager;
        private readonly OutfitFilterManager filterManager;
        private readonly OutfitItemRenderer itemRenderer;
        private readonly OutfitState state;
        private readonly OutfitUIBuilder uiBuilder;
        private readonly OutfitDropdownManager dropdownManager;
        private readonly OutfitSearchManager searchManager;
        private readonly OutfitTooltipRenderer tooltipRenderer;
        private readonly ContinuousScrollHandler continuousScrollHandler;

        // Reference to mod (for config if needed)
        private readonly ModEntry mod;

        // Item info toggle state
        private bool showItemInfo = false;

        public OutfitMenu(ModEntry mod, OutfitCategoryManager categoryManager, OutfitFilterManager filterManager, bool showItemInfo = false)
        {
            this.mod = mod;

            // Use the cached managers passed from ModEntry
            this.categoryManager = categoryManager;
            this.filterManager = filterManager;

            // Restore user preference for item info display
            this.showItemInfo = showItemInfo;

            // Initialize other helper classes
            itemRenderer = new OutfitItemRenderer(mod.Monitor, mod.Helper.ModRegistry);
            state = new OutfitState();
            uiBuilder = new OutfitUIBuilder();
            dropdownManager = new OutfitDropdownManager(filterManager, categoryManager, state, uiBuilder);
            searchManager = new OutfitSearchManager(uiBuilder, state);
            tooltipRenderer = new OutfitTooltipRenderer(filterManager, categoryManager);
            continuousScrollHandler = new ContinuousScrollHandler(initialDelay: 400, repeatDelay: 100);

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
            searchManager.UpdateBounds();

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

            if (dropdownManager.IsOpen)
            {
                dropdownManager.BuildOptions();
            }
        }

        // --- Helper methods for readability ---

        private int GetCurrentListCount()
        {
            var category = categoryManager.CurrentCategory;
            return category switch
            {
                OutfitCategoryManager.Category.Shirts => GetCurrentShirtIds().Count,
                OutfitCategoryManager.Category.Pants => GetCurrentPantsIds().Count,
                OutfitCategoryManager.Category.Hats => GetCurrentHatIds().Count,
                _ => 0
            };
        }
        private int GetCurrentIndex() => state.GetCurrentIndex(categoryManager.CurrentCategory);
        private void SetCurrentIndex(int index) => state.SetCurrentIndex(categoryManager.CurrentCategory, index);

        private List<string> GetCurrentShirtIds() => filterManager.GetFilteredAndSearchedShirtIds(
            categoryManager.ShirtIds,
            state.GetModFilter(OutfitCategoryManager.Category.Shirts),
            state.GetSearchText(OutfitCategoryManager.Category.Shirts));
        private List<string> GetCurrentPantsIds() => filterManager.GetFilteredAndSearchedPantsIds(
            categoryManager.PantsIds,
            state.GetModFilter(OutfitCategoryManager.Category.Pants),
            state.GetSearchText(OutfitCategoryManager.Category.Pants));
        private List<string> GetCurrentHatIds() => filterManager.GetFilteredAndSearchedHatIds(
            categoryManager.HatIds,
            state.GetModFilter(OutfitCategoryManager.Category.Hats),
            state.GetSearchText(OutfitCategoryManager.Category.Hats));

        private void ApplyCurrentSelection()
        {
            state.ApplySelection(
                categoryManager.CurrentCategory,
                GetCurrentShirtIds(),
                GetCurrentPantsIds(),
                GetCurrentHatIds()
            );
        }

        private void ResetOutfit()
        {
            state.ResetToApplied(
                GetCurrentShirtIds(),
                GetCurrentPantsIds(),
                GetCurrentHatIds()
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

            // Handle dropdown option clicks
            if (dropdownManager.IsOpen)
            {
                string? selectedMod = dropdownManager.HandleClick(x, y, out bool clickedOption);
                if (clickedOption)
                {
                    state.SetModFilter(categoryManager.CurrentCategory, selectedMod);
                    state.SetSearchText(categoryManager.CurrentCategory, searchManager.CurrentSearchText);
                    state.ScrollOffset = 0;
                    if (playSound) Game1.playSound("smallSelect");
                    return;
                }
                // Clicked outside dropdown, close it
                dropdownManager.Close();
                continuousScrollHandler.Reset(); // Reset scroll state when closing dropdown
                return;
            }

            // Handle dropdown button click
            if (uiBuilder.ModFilterDropdown != null && uiBuilder.ModFilterDropdown.containsPoint(x, y))
            {
                dropdownManager.Toggle();
                continuousScrollHandler.Reset(); // Reset scroll state when toggling dropdown
                if (playSound) Game1.playSound("smallSelect");
                return;
            }

            // Category tabs
            if (uiBuilder.ShirtsTab.containsPoint(x, y))
            {
                if (categoryManager.CurrentCategory != OutfitCategoryManager.Category.Shirts)
                {
                    categoryManager.CurrentCategory = OutfitCategoryManager.Category.Shirts;
                    state.ScrollOffset = 0;

                    // Handle mod filter reset
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
                return;
            }
            if (uiBuilder.PantsTab.containsPoint(x, y))
            {
                if (categoryManager.CurrentCategory != OutfitCategoryManager.Category.Pants)
                {
                    categoryManager.CurrentCategory = OutfitCategoryManager.Category.Pants;
                    state.ScrollOffset = 0;

                    // Handle mod filter reset
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
                return;
            }
            if (uiBuilder.HatsTab.containsPoint(x, y))
            {
                if (categoryManager.CurrentCategory != OutfitCategoryManager.Category.Hats)
                {
                    categoryManager.CurrentCategory = OutfitCategoryManager.Category.Hats;
                    state.ScrollOffset = 0;

                    // Handle mod filter reset
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

            // Handle dropdown scrolling if open (block item grid scrolling when dropdown is open)
            if (dropdownManager.IsOpen)
            {
                if (dropdownManager.HandleScrollWheel(direction))
                {
                    Game1.playSound("shiny4");
                }
                return; // Always consume scroll events when dropdown is open
            }

            // Original item grid scrolling logic
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
                return;
            }

            // Handle scrolling with arrow keys only (removed WASD)
            int totalRows = Math.Max(1, (int)Math.Ceiling(GetCurrentListCount() / (float)uiBuilder.COLUMNS));
            int maxScroll = Math.Max(0, totalRows - uiBuilder.VISIBLE_ROWS);

            // Scroll up (Up arrow only)
            if (key == Keys.Up)
            {
                if (state.ScrollOffset > 0)
                {
                    state.ScrollOffset--;
                    Game1.playSound("shiny4");
                    return;
                }
            }

            // Scroll down (Down arrow only)
            if (key == Keys.Down)
            {
                if (state.ScrollOffset < maxScroll)
                {
                    state.ScrollOffset++;
                    Game1.playSound("shiny4");
                    return;
                }
            }

            // Page up (Left arrow only)
            if (key == Keys.Left)
            {
                if (state.ScrollOffset > 0)
                {
                    state.ScrollOffset = Math.Max(0, state.ScrollOffset - uiBuilder.VISIBLE_ROWS);
                    Game1.playSound("shiny4");
                    return;
                }
            }

            // Page down (Right arrow only)
            if (key == Keys.Right)
            {
                if (state.ScrollOffset < maxScroll)
                {
                    state.ScrollOffset = Math.Min(maxScroll, state.ScrollOffset + uiBuilder.VISIBLE_ROWS);
                    Game1.playSound("shiny4");
                    return;
                }
            }

            // Handle Escape to close with revert
            // Only Escape closes the menu - other menu buttons (like E) are used for typing
            if (key == Keys.Escape)
            {
                RevertAndClose();
                Game1.playSound("bigDeSelect");
                return;
            }

            // Don't pass menu button keys (like E) to base class - they should be used for typing
            if (Game1.options.doesInputListContain(Game1.options.menuButton, key))
            {
                return; // Block menu button from closing the menu
            }

            base.receiveKeyPress(key);
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
                    int totalRows = Math.Max(1, (int)Math.Ceiling(GetCurrentListCount() / (float)uiBuilder.COLUMNS));
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
            uiBuilder.DrawSavedMessage(b);

            // Draw apply and reset buttons
            uiBuilder.DrawApplyButton(b);
            uiBuilder.DrawResetButton(b);

            // === RIGHT PANEL: Category Tabs & Item List ===
            // Draw category tabs with text labels
            uiBuilder.DrawTabWithText(b, uiBuilder.ShirtsTab, TranslationCache.TabShirts,
                categoryManager.CurrentCategory == OutfitCategoryManager.Category.Shirts);

            uiBuilder.DrawTabWithText(b, uiBuilder.PantsTab, TranslationCache.TabPants,
                categoryManager.CurrentCategory == OutfitCategoryManager.Category.Pants);

            uiBuilder.DrawTabWithText(b, uiBuilder.HatsTab, TranslationCache.TabHats,
                categoryManager.CurrentCategory == OutfitCategoryManager.Category.Hats);

            // Draw search bar
            uiBuilder.DrawSearchBar(b, searchManager.IsFocused);
            searchManager.Draw(b);

            // Draw mod filter dropdown
            uiBuilder.DrawModFilterDropdown(b, state.GetModFilter(categoryManager.CurrentCategory), dropdownManager.IsOpen);

            // Draw item list background and scroll buttons
            uiBuilder.DrawItemList(b, state.ScrollOffset, GetCurrentListCount());

            // Draw items
            int currentIndex = GetCurrentIndex();
            int listCount = GetCurrentListCount();
            int hoveredIndex = -1;

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
                    b.Draw(Game1.staminaRect, slot, Color.LimeGreen * 0.3f);
                }

                // Hover highlight
                bool isHovering = uiBuilder.ItemSlots[i].containsPoint(Game1.getMouseX(), Game1.getMouseY());
                if (isHovering)
                {
                    b.Draw(Game1.staminaRect, slot, Color.White * 0.2f);
                    hoveredIndex = listIndex; // Track for tooltip rendering later
                }

                // Draw item sprite (use filtered lists)
                itemRenderer.DrawItemSprite(b, categoryManager.CurrentCategory, listIndex,
                    slot, GetCurrentShirtIds(), GetCurrentPantsIds(), GetCurrentHatIds());
            }

            // Draw close button
            uiBuilder.DrawCloseButton(b);

            // Set hand cursor when hovering over clickable elements
            if (uiBuilder.IsHoveringClickable(Game1.getMouseX(), Game1.getMouseY()) || dropdownManager.IsOpen)
            {
                Game1.mouseCursor = 1;
            }

            // Draw dropdown options if open (AFTER everything else, on top)
            if (dropdownManager.IsOpen)
            {
                DrawDropdownOptions(b);
            }

            // Draw item info tooltip if toggle is active and hovering (AFTER everything else, on top)
            if (showItemInfo && hoveredIndex >= 0)
            {
                tooltipRenderer.DrawTooltip(b, hoveredIndex,
                    GetCurrentShirtIds(), GetCurrentPantsIds(), GetCurrentHatIds());
            }

            // Draw cursor (must be last)
            drawMouse(b);
        }

        private void DrawDropdownOptions(SpriteBatch b)
        {
            if (dropdownManager.Options.Count == 0 || uiBuilder.ModFilterDropdown == null)
                return;

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            // Calculate total dropdown height based on VISIBLE items
            int visibleCount = dropdownManager.Options.Count(opt => opt.visible);
            if (visibleCount == 0)
                return;

            // Get dropdown position from the dropdown button (not from options, which can have negative Y)
            int totalHeight = visibleCount * dropdownManager.Options[0].bounds.Height;
            int dropdownX = uiBuilder.ModFilterDropdown.bounds.X;
            int dropdownY = uiBuilder.ModFilterDropdown.bounds.Bottom;
            int dropdownWidth = uiBuilder.ModFilterDropdown.bounds.Width;

            // Draw dropdown background (simple solid color box like CJB Item Spawner)
            var bgColor = new Color(224, 203, 169); // Tan menu background color
            b.Draw(Game1.staminaRect, new Rectangle(dropdownX, dropdownY, dropdownWidth, totalHeight), bgColor);

            // Draw border
            var borderColor = Color.Black * 0.5f;
            int borderWidth = 2;
            b.Draw(Game1.staminaRect, new Rectangle(dropdownX, dropdownY, dropdownWidth, borderWidth), borderColor); // Top
            b.Draw(Game1.staminaRect, new Rectangle(dropdownX, dropdownY + totalHeight - borderWidth, dropdownWidth, borderWidth), borderColor); // Bottom
            b.Draw(Game1.staminaRect, new Rectangle(dropdownX, dropdownY, borderWidth, totalHeight), borderColor); // Left
            b.Draw(Game1.staminaRect, new Rectangle(dropdownX + dropdownWidth - borderWidth, dropdownY, borderWidth, totalHeight), borderColor); // Right

            // Draw each VISIBLE option
            foreach (var option in dropdownManager.Options.Where(opt => opt.visible))
            {
                bool isHovered = option.containsPoint(mouseX, mouseY);

                // Draw hover highlight
                if (isHovered)
                {
                    b.Draw(Game1.staminaRect, option.bounds, Color.Wheat * 0.3f);
                }

                // Truncate text with ellipsis if too long
                string displayText = option.name;
                Vector2 textSize = Game1.smallFont.MeasureString(displayText);
                int maxTextWidth = option.bounds.Width - 24; // 12px padding on each side

                if (textSize.X > maxTextWidth)
                {
                    // Truncate and add ellipsis
                    while (textSize.X > maxTextWidth && displayText.Length > 3)
                    {
                        displayText = displayText.Substring(0, displayText.Length - 1);
                        textSize = Game1.smallFont.MeasureString(displayText + "...");
                    }
                    displayText += "...";
                }

                // Draw option text
                Vector2 textPos = new Vector2(
                    option.bounds.X + 12,
                    option.bounds.Y + (option.bounds.Height - textSize.Y) / 2
                );

                Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPos,
                    isHovered ? Color.Black : Game1.textColor);
            }
        }

        // --- Cleanup ---

        public override void emergencyShutDown()
        {
            filterManager.ClearSearchCaches();
            uiBuilder.Cleanup();
            base.emergencyShutDown();
        }
    }
}
