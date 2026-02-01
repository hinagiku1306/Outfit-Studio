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

        // Reference to mod (for config if needed)
        private readonly ModEntry mod;

        // Dropdown filter state
        private bool dropdownOpen = false;
        private List<ClickableComponent> dropdownOptions = new();

        // Item info toggle state
        private bool showItemInfo = false;

        // Scroll hold state for continuous scrolling
        private int scrollHoldTimer = 0;
        private int lastScrollTime = 0;
        private const int ScrollHoldDelay = 100;
        private const int InitialScrollDelay = 400;

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

        private int GetCurrentListCount() => filterManager.GetFilteredListCount(
            categoryManager.CurrentCategory,
            categoryManager.ShirtIds,
            categoryManager.PantsIds,
            categoryManager.HatIds,
            state.CurrentModFilter);
        private int GetCurrentIndex() => state.GetCurrentIndex(categoryManager.CurrentCategory);
        private void SetCurrentIndex(int index) => state.SetCurrentIndex(categoryManager.CurrentCategory, index);

        private List<string> GetCurrentShirtIds() => filterManager.GetFilteredShirtIds(categoryManager.ShirtIds, state.CurrentModFilter);
        private List<string> GetCurrentPantsIds() => filterManager.GetFilteredPantsIds(categoryManager.PantsIds, state.CurrentModFilter);
        private List<string> GetCurrentHatIds() => filterManager.GetFilteredHatIds(categoryManager.HatIds, state.CurrentModFilter);

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
            if (dropdownOpen)
            {
                for (int i = 0; i < dropdownOptions.Count; i++)
                {
                    if (dropdownOptions[i].containsPoint(x, y))
                    {
                        string selectedMod = dropdownOptions[i].name;
                        if (selectedMod == TranslationCache.FilterAll)
                        {
                            state.CurrentModFilter = null;
                        }
                        else
                        {
                            state.CurrentModFilter = selectedMod;
                        }
                        state.ScrollOffset = 0;
                        dropdownOpen = false;
                        if (playSound) Game1.playSound("smallSelect");
                        return;
                    }
                }
                // Clicked outside dropdown, close it
                dropdownOpen = false;
                return;
            }

            // Handle dropdown button click
            if (uiBuilder.ModFilterDropdown != null && uiBuilder.ModFilterDropdown.containsPoint(x, y))
            {
                ToggleDropdown();
                if (playSound) Game1.playSound("smallSelect");
                return;
            }

            // Category tabs (reset filter when switching to a different category)
            if (uiBuilder.ShirtsTab.containsPoint(x, y))
            {
                if (categoryManager.CurrentCategory != OutfitCategoryManager.Category.Shirts)
                {
                    categoryManager.CurrentCategory = OutfitCategoryManager.Category.Shirts;
                    state.ScrollOffset = 0;
                    state.CurrentModFilter = null;
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
                    state.CurrentModFilter = null;
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
                    state.CurrentModFilter = null;
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
            // Handle scrolling with arrow keys and WASD
            int totalRows = Math.Max(1, (int)Math.Ceiling(GetCurrentListCount() / (float)uiBuilder.COLUMNS));
            int maxScroll = Math.Max(0, totalRows - uiBuilder.VISIBLE_ROWS);

            // Scroll up (Up arrow or W)
            if (key == Keys.Up || key == Keys.W)
            {
                if (state.ScrollOffset > 0)
                {
                    state.ScrollOffset--;
                    Game1.playSound("shiny4");
                    return;
                }
            }

            // Scroll down (Down arrow or S)
            if (key == Keys.Down || key == Keys.S)
            {
                if (state.ScrollOffset < maxScroll)
                {
                    state.ScrollOffset++;
                    Game1.playSound("shiny4");
                    return;
                }
            }

            // Page up (Left arrow or A) - scroll up by visible rows
            if (key == Keys.Left || key == Keys.A)
            {
                if (state.ScrollOffset > 0)
                {
                    state.ScrollOffset = Math.Max(0, state.ScrollOffset - uiBuilder.VISIBLE_ROWS);
                    Game1.playSound("shiny4");
                    return;
                }
            }

            // Page down (Right arrow or D) - scroll down by visible rows
            if (key == Keys.Right || key == Keys.D)
            {
                if (state.ScrollOffset < maxScroll)
                {
                    state.ScrollOffset = Math.Min(maxScroll, state.ScrollOffset + uiBuilder.VISIBLE_ROWS);
                    Game1.playSound("shiny4");
                    return;
                }
            }

            // Handle Escape or menu button to close with revert
            if (key == Keys.Escape || Game1.options.doesInputListContain(Game1.options.menuButton, key))
            {
                RevertAndClose();
                Game1.playSound("bigDeSelect");
                return;
            }

            base.receiveKeyPress(key);
        }

        private void ToggleDropdown()
        {
            dropdownOpen = !dropdownOpen;

            if (dropdownOpen)
            {
                BuildDropdownOptions();
            }
        }

        private void BuildDropdownOptions()
        {
            dropdownOptions.Clear();

            if (uiBuilder.ModFilterDropdown == null)
                return;

            // Get unique mods for current category
            var mods = filterManager.GetUniqueModsForCategory(
                categoryManager.CurrentCategory,
                categoryManager.ShirtIds,
                categoryManager.PantsIds,
                categoryManager.HatIds);

            mods.Insert(0, TranslationCache.FilterAll);

            // Move "Vanilla" filter to position 1 (right after "All")
            string vanillaFilter = TranslationCache.FilterVanilla;
            int vanillaIndex = mods.IndexOf(vanillaFilter);
            if (vanillaIndex > 1) // Only move if it's not already at position 1
            {
                mods.RemoveAt(vanillaIndex);
                mods.Insert(1, vanillaFilter);
            }

            // Build clickable options
            int optionHeight = 32;
            int dropdownY = uiBuilder.ModFilterDropdown.bounds.Bottom;

            for (int i = 0; i < mods.Count; i++)
            {
                dropdownOptions.Add(new ClickableComponent(
                    new Rectangle(
                        uiBuilder.ModFilterDropdown.bounds.X,
                        dropdownY + (i * optionHeight),
                        uiBuilder.ModFilterDropdown.bounds.Width,
                        optionHeight
                    ),
                    mods[i]
                ));
            }
        }

        public override void update(GameTime time)
        {
            base.update(time);
            uiBuilder.Update((float)time.ElapsedGameTime.TotalMilliseconds);

            // Handle item info toggle keybind
            var config = mod.GetConfig();
            if (config.ToggleItemInfoKey.JustPressed())
            {
                showItemInfo = !showItemInfo;
                mod.SetShowItemInfoPreference(showItemInfo);
                Game1.playSound(showItemInfo ? "bigSelect" : "bigDeSelect");
            }

            // Handle continuous scrolling when keys are held down
            var keyboard = Keyboard.GetState();
            int totalRows = Math.Max(1, (int)Math.Ceiling(GetCurrentListCount() / (float)uiBuilder.COLUMNS));
            int maxScroll = Math.Max(0, totalRows - uiBuilder.VISIBLE_ROWS);

            bool scrollKeyHeld = false;
            int scrollDirection = 0; // -1 for up, 1 for down

            // Check if any scroll keys are held
            if (keyboard.IsKeyDown(Keys.Up) || keyboard.IsKeyDown(Keys.W))
            {
                scrollKeyHeld = true;
                scrollDirection = -1;
            }
            else if (keyboard.IsKeyDown(Keys.Down) || keyboard.IsKeyDown(Keys.S))
            {
                scrollKeyHeld = true;
                scrollDirection = 1;
            }
            else if (keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.A))
            {
                scrollKeyHeld = true;
                scrollDirection = -uiBuilder.VISIBLE_ROWS; // Page up
            }
            else if (keyboard.IsKeyDown(Keys.Right) || keyboard.IsKeyDown(Keys.D))
            {
                scrollKeyHeld = true;
                scrollDirection = uiBuilder.VISIBLE_ROWS; // Page down
            }

            if (scrollKeyHeld)
            {
                scrollHoldTimer += (int)time.ElapsedGameTime.TotalMilliseconds;

                // Only start continuous scrolling after initial delay (to avoid double-trigger with receiveKeyPress)
                if (scrollHoldTimer >= InitialScrollDelay)
                {
                    // Check if enough time has passed since last scroll
                    int timeSinceLastScroll = scrollHoldTimer - lastScrollTime;
                    if (timeSinceLastScroll >= ScrollHoldDelay)
                    {
                        // Apply scroll based on direction
                        if (scrollDirection < 0) // Scroll up
                        {
                            int newOffset = state.ScrollOffset + scrollDirection;
                            if (newOffset >= 0 && newOffset != state.ScrollOffset)
                            {
                                state.ScrollOffset = Math.Max(0, newOffset);
                                lastScrollTime = scrollHoldTimer;
                                Game1.playSound("shiny4");
                            }
                        }
                        else if (scrollDirection > 0) // Scroll down
                        {
                            int newOffset = state.ScrollOffset + scrollDirection;
                            if (newOffset <= maxScroll && newOffset != state.ScrollOffset)
                            {
                                state.ScrollOffset = Math.Min(maxScroll, newOffset);
                                lastScrollTime = scrollHoldTimer;
                                Game1.playSound("shiny4");
                            }
                        }
                    }
                }
            }
            else
            {
                // Reset timers when no scroll keys are held
                scrollHoldTimer = 0;
                lastScrollTime = 0;
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

            // Draw mod filter dropdown
            uiBuilder.DrawModFilterDropdown(b, state.CurrentModFilter, dropdownOpen);

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
            if (uiBuilder.IsHoveringClickable(Game1.getMouseX(), Game1.getMouseY()) || dropdownOpen)
            {
                Game1.mouseCursor = 1;
            }

            // Draw dropdown options if open (AFTER everything else, on top)
            if (dropdownOpen)
            {
                DrawDropdownOptions(b);
            }

            // Draw item info tooltip if toggle is active and hovering (AFTER everything else, on top)
            if (showItemInfo && hoveredIndex >= 0)
            {
                DrawItemTooltip(b, hoveredIndex);
            }

            // Draw cursor (must be last)
            drawMouse(b);
        }

        private void DrawDropdownOptions(SpriteBatch b)
        {
            if (dropdownOptions.Count == 0)
                return;

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            // Calculate total dropdown height
            int totalHeight = dropdownOptions.Count * dropdownOptions[0].bounds.Height;
            int dropdownX = dropdownOptions[0].bounds.X;
            int dropdownY = dropdownOptions[0].bounds.Y;
            int dropdownWidth = dropdownOptions[0].bounds.Width;

            // Draw dropdown background
            IClickableMenu.drawTextureBox(b, dropdownX, dropdownY, dropdownWidth, totalHeight, Color.White);

            // Draw each option
            foreach (var option in dropdownOptions)
            {
                bool isHovered = option.containsPoint(mouseX, mouseY);

                // Draw hover highlight
                if (isHovered)
                {
                    b.Draw(Game1.staminaRect, option.bounds, Color.Wheat * 0.5f);
                }

                // Draw option text
                Vector2 textSize = Game1.smallFont.MeasureString(option.name);
                Vector2 textPos = new Vector2(
                    option.bounds.X + 12,
                    option.bounds.Y + 12 + (option.bounds.Height - textSize.Y) / 2
                );

                Utility.drawTextWithShadow(b, option.name, Game1.smallFont, textPos,
                    isHovered ? Color.Black : Game1.textColor);
            }
        }

        private void DrawItemTooltip(SpriteBatch b, int listIndex)
        {
            string itemName = "";
            string description = "";
            string modName = "";
            Item? actualItem = null;

            // Get item data
            switch (categoryManager.CurrentCategory)
            {
                case OutfitCategoryManager.Category.Shirts:
                    var shirtIds = GetCurrentShirtIds();
                    if (listIndex >= 0 && listIndex < shirtIds.Count)
                    {
                        string id = shirtIds[listIndex];
                        string qualifiedId = "(S)" + id;
                        actualItem = ItemRegistry.Create(qualifiedId);
                        if (actualItem != null)
                        {
                            itemName = actualItem.DisplayName;
                            description = actualItem.getDescription();
                        }
                        modName = filterManager.GetModNameForItem(id);
                    }
                    break;

                case OutfitCategoryManager.Category.Pants:
                    var pantsIds = GetCurrentPantsIds();
                    if (listIndex >= 0 && listIndex < pantsIds.Count)
                    {
                        string id = pantsIds[listIndex];
                        string qualifiedId = "(P)" + id;
                        actualItem = ItemRegistry.Create(qualifiedId);
                        if (actualItem != null)
                        {
                            itemName = actualItem.DisplayName;
                            description = actualItem.getDescription();
                        }
                        modName = filterManager.GetModNameForItem(id);
                    }
                    break;

                case OutfitCategoryManager.Category.Hats:
                    var hatIds = GetCurrentHatIds();
                    if (listIndex >= 0 && listIndex < hatIds.Count)
                    {
                        string hatId = hatIds[listIndex];
                        if (!string.IsNullOrEmpty(hatId) && hatId != "-1")
                        {
                            string qualifiedId = "(H)" + hatId;
                            actualItem = ItemRegistry.Create(qualifiedId);
                            if (actualItem != null)
                            {
                                itemName = actualItem.DisplayName;
                                description = actualItem.getDescription();
                            }
                            modName = filterManager.GetModNameForHat(hatId);
                        }
                        else
                        {
                            itemName = TranslationCache.ItemNoHat;
                            description = "";
                        }
                    }
                    break;
            }

            // Draw using vanilla hover text method (for proper formatting with divider)
            if (actualItem != null)
            {
                // Append mod name to description if present (skip vanilla items)
                string fullDescription = description;
                if (!string.IsNullOrEmpty(modName) && modName != TranslationCache.FilterVanilla)
                {
                    fullDescription += "\n\n" + TranslationCache.ItemModInfoTemplate.Replace("{{modName}}", modName);
                }

                // Use the vanilla drawHoverText that includes name, divider, and description
                IClickableMenu.drawHoverText(b, fullDescription, Game1.smallFont, 0, 0, -1, itemName, -1, null, actualItem);
            }
            else if (!string.IsNullOrEmpty(itemName))
            {
                // Fallback for items without actual item instance (like No Hat)
                string hoverText = itemName;
                if (!string.IsNullOrEmpty(description))
                {
                    hoverText += "\n" + description;
                }
                if (!string.IsNullOrEmpty(modName) && modName != TranslationCache.FilterVanilla)
                {
                    hoverText += "\n\n" + TranslationCache.ItemModInfoTemplate.Replace("{{modName}}", modName);
                }
                IClickableMenu.drawToolTip(b, hoverText, "", null);
            }
        }
    }
}
