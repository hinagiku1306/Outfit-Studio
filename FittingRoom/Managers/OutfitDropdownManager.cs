using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace FittingRoom
{
    /// <summary>
    /// Manages the mod filter dropdown state and interactions.
    /// </summary>
    public class OutfitDropdownManager
    {
        // Dependencies
        private readonly OutfitFilterManager filterManager;
        private readonly OutfitCategoryManager categoryManager;
        private readonly OutfitState state;
        private readonly OutfitUIBuilder uiBuilder;

        // Dropdown state
        private bool dropdownOpen = false;
        private List<ClickableComponent> dropdownOptions = new();
        private int dropdownFirstVisibleIndex = 0;
        private int dropdownMaxVisibleItems = 0;

        // Constants
        private const int MaxVisibleOptions = 5;
        private const int OptionVerticalPadding = 8; // Padding above and below text

        /// <summary>
        /// Gets whether the dropdown is currently open.
        /// </summary>
        public bool IsOpen => dropdownOpen;

        /// <summary>
        /// Gets the list of dropdown options.
        /// </summary>
        public List<ClickableComponent> Options => dropdownOptions;

        /// <summary>
        /// Gets the index of the first visible option.
        /// </summary>
        public int FirstVisibleIndex => dropdownFirstVisibleIndex;

        /// <summary>
        /// Gets the maximum number of visible items.
        /// </summary>
        public int MaxVisibleItems => dropdownMaxVisibleItems;

        public OutfitDropdownManager(
            OutfitFilterManager filterManager,
            OutfitCategoryManager categoryManager,
            OutfitState state,
            OutfitUIBuilder uiBuilder)
        {
            this.filterManager = filterManager ?? throw new ArgumentNullException(nameof(filterManager));
            this.categoryManager = categoryManager ?? throw new ArgumentNullException(nameof(categoryManager));
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.uiBuilder = uiBuilder ?? throw new ArgumentNullException(nameof(uiBuilder));
        }

        /// <summary>
        /// Toggles the dropdown between open and closed states.
        /// </summary>
        public void Toggle()
        {
            dropdownOpen = !dropdownOpen;

            if (dropdownOpen)
            {
                dropdownFirstVisibleIndex = 0; // Reset scroll when opening
                BuildOptions();
            }
            else
            {
                // Reset scroll state when closing
                dropdownFirstVisibleIndex = 0;
            }
        }

        /// <summary>
        /// Closes the dropdown.
        /// </summary>
        public void Close()
        {
            dropdownOpen = false;
            dropdownFirstVisibleIndex = 0;
        }

        /// <summary>
        /// Builds the dropdown option list based on current category and filters.
        /// </summary>
        public void BuildOptions()
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
            int dropdownY = uiBuilder.ModFilterDropdown.bounds.Bottom;

            // Calculate option height based on text height + padding
            float textHeight = Game1.smallFont.MeasureString("Ay").Y; // Measure with tall characters
            int optionHeight = (int)Math.Ceiling(textHeight) + (OptionVerticalPadding * 2);

            // Set max visible items to 7
            dropdownMaxVisibleItems = Math.Min(MaxVisibleOptions, mods.Count);

            // Clamp FirstVisibleIndex to valid range
            int maxFirstVisibleIndex = Math.Max(0, mods.Count - dropdownMaxVisibleItems);
            dropdownFirstVisibleIndex = Math.Clamp(dropdownFirstVisibleIndex, 0, maxFirstVisibleIndex);

            for (int i = 0; i < mods.Count; i++)
            {
                bool isVisible = (i >= dropdownFirstVisibleIndex && i < dropdownFirstVisibleIndex + dropdownMaxVisibleItems);
                int visualIndex = i - dropdownFirstVisibleIndex;

                var option = new ClickableComponent(
                    new Rectangle(
                        uiBuilder.ModFilterDropdown.bounds.X,
                        dropdownY + (visualIndex * optionHeight),
                        uiBuilder.ModFilterDropdown.bounds.Width,
                        optionHeight
                    ),
                    mods[i]
                );
                option.visible = isVisible;
                dropdownOptions.Add(option);
            }
        }

        /// <summary>
        /// Handles mouse click on dropdown. Returns selected mod name if an option was clicked, null otherwise.
        /// </summary>
        /// <returns>The selected mod name, or null for "All" filter</returns>
        public string? HandleClick(int x, int y, out bool clickedOption)
        {
            // Check if any visible option was clicked
            for (int i = 0; i < dropdownOptions.Count; i++)
            {
                if (dropdownOptions[i].visible && dropdownOptions[i].containsPoint(x, y))
                {
                    clickedOption = true;
                    string selectedMod = dropdownOptions[i].name;

                    // Close dropdown
                    Close();

                    // Return null for "All" filter, mod name otherwise
                    return selectedMod == TranslationCache.FilterAll ? null : selectedMod;
                }
            }

            // Clicked outside dropdown
            clickedOption = false;
            return null;
        }

        /// <summary>
        /// Handles scroll wheel input for the dropdown.
        /// </summary>
        /// <returns>True if scroll was handled, false otherwise</returns>
        public bool HandleScrollWheel(int direction)
        {
            if (!dropdownOpen || dropdownOptions.Count == 0)
                return false;

            int maxFirstVisibleIndex = Math.Max(0, dropdownOptions.Count - dropdownMaxVisibleItems);

            if (direction > 0 && dropdownFirstVisibleIndex > 0)
            {
                dropdownFirstVisibleIndex--;
                BuildOptions();
                return true;
            }
            else if (direction < 0 && dropdownFirstVisibleIndex < maxFirstVisibleIndex)
            {
                dropdownFirstVisibleIndex++;
                BuildOptions();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Handles continuous scrolling with a scroll amount (for held keys).
        /// </summary>
        /// <returns>True if scroll was handled, false otherwise</returns>
        public bool HandleScrollAmount(int scrollAmount)
        {
            if (!dropdownOpen || dropdownOptions.Count == 0 || scrollAmount == 0)
                return false;

            int maxFirstVisibleIndex = Math.Max(0, dropdownOptions.Count - dropdownMaxVisibleItems);
            int newIndex = dropdownFirstVisibleIndex + scrollAmount;
            newIndex = Math.Clamp(newIndex, 0, maxFirstVisibleIndex);

            if (newIndex != dropdownFirstVisibleIndex)
            {
                dropdownFirstVisibleIndex = newIndex;
                BuildOptions();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Handles keyboard input for the dropdown.
        /// </summary>
        /// <returns>True if key was handled, false otherwise</returns>
        public bool HandleKeyPress(Keys key)
        {
            if (!dropdownOpen)
                return false;

            int maxFirstVisibleIndex = Math.Max(0, dropdownOptions.Count - dropdownMaxVisibleItems);

            // Scroll up (Up arrow or W)
            if (key == Keys.Up || key == Keys.W)
            {
                if (dropdownFirstVisibleIndex > 0)
                {
                    dropdownFirstVisibleIndex--;
                    BuildOptions();
                    return true;
                }
                return true; // Consume the key even if at top
            }

            // Scroll down (Down arrow or S)
            if (key == Keys.Down || key == Keys.S)
            {
                if (dropdownFirstVisibleIndex < maxFirstVisibleIndex)
                {
                    dropdownFirstVisibleIndex++;
                    BuildOptions();
                    return true;
                }
                return true; // Consume the key even if at bottom
            }

            // Escape closes the dropdown
            if (key == Keys.Escape || Game1.options.doesInputListContain(Game1.options.menuButton, key))
            {
                Close();
                return true;
            }

            // Consume all other keys when dropdown is open
            return true;
        }
    }
}
