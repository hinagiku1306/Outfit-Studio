using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace OutfitStudio
{
    public class OutfitDropdownManager
    {
        private readonly OutfitFilterManager filterManager;
        private readonly OutfitCategoryManager categoryManager;
        private readonly OutfitState state;
        private readonly OutfitUIBuilder uiBuilder;

        private bool dropdownOpen = false;
        private List<ClickableComponent> dropdownOptions = new();
        private int dropdownFirstVisibleIndex = 0;
        private int dropdownMaxVisibleItems = 0;

        private const int MaxVisibleOptions = 5;
        private const int OptionVerticalPadding = 8;

        public bool IsOpen => dropdownOpen;
        public List<ClickableComponent> Options => dropdownOptions;
        public int FirstVisibleIndex => dropdownFirstVisibleIndex;
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

        public void Toggle()
        {
            dropdownOpen = !dropdownOpen;

            if (dropdownOpen)
            {
                dropdownFirstVisibleIndex = 0;
                BuildOptions();
            }
            else
            {
                dropdownFirstVisibleIndex = 0;
            }
        }

        public void Close()
        {
            dropdownOpen = false;
            dropdownFirstVisibleIndex = 0;
        }

        public void BuildOptions()
        {
            dropdownOptions.Clear();

            if (uiBuilder.ModFilterDropdown == null)
                return;

            var mods = filterManager.GetUniqueModsForCategory(
                categoryManager.CurrentCategory,
                categoryManager.ShirtIds,
                categoryManager.PantsIds,
                categoryManager.HatIds);

            mods.Insert(0, TranslationCache.FilterAll);

            string vanillaFilter = TranslationCache.FilterVanilla;
            int vanillaIndex = mods.IndexOf(vanillaFilter);
            if (vanillaIndex > 1)
            {
                mods.RemoveAt(vanillaIndex);
                mods.Insert(1, vanillaFilter);
            }

            int dropdownY = uiBuilder.ModFilterDropdown.bounds.Bottom;

            // "Ay" measures tall characters like g/y for proper height
            float textHeight = Game1.smallFont.MeasureString("Ay").Y;
            int optionHeight = (int)Math.Ceiling(textHeight) + (OptionVerticalPadding * 2);

            dropdownMaxVisibleItems = Math.Min(MaxVisibleOptions, mods.Count);
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

        public string? HandleClick(int x, int y, out bool clickedOption)
        {
            for (int i = 0; i < dropdownOptions.Count; i++)
            {
                if (dropdownOptions[i].visible && dropdownOptions[i].containsPoint(x, y))
                {
                    clickedOption = true;
                    string selectedMod = dropdownOptions[i].name;

                    Close();

                    // null = "All" (no filter)
                    return selectedMod == TranslationCache.FilterAll ? null : selectedMod;
                }
            }

            clickedOption = false;
            return null;
        }

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

        public bool HandleKeyPress(Keys key)
        {
            if (!dropdownOpen)
                return false;

            int maxFirstVisibleIndex = Math.Max(0, dropdownOptions.Count - dropdownMaxVisibleItems);

            if (key == Keys.Up || key == Keys.W)
            {
                if (dropdownFirstVisibleIndex > 0)
                {
                    dropdownFirstVisibleIndex--;
                    BuildOptions();
                    return true;
                }
                return true;
            }

            if (key == Keys.Down || key == Keys.S)
            {
                if (dropdownFirstVisibleIndex < maxFirstVisibleIndex)
                {
                    dropdownFirstVisibleIndex++;
                    BuildOptions();
                    return true;
                }
                return true;
            }

            if (key == Keys.Escape || Game1.options.doesInputListContain(Game1.options.menuButton, key))
            {
                Close();
                return true;
            }

            return true;
        }
    }
}
