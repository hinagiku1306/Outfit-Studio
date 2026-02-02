using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace FittingRoom
{
    public class SaveSetDropdownManager
    {
        private readonly SaveSetUIBuilder uiBuilder;

        private bool dropdownOpen = false;
        private List<ClickableComponent> dropdownOptions = new();
        private int dropdownFirstVisibleIndex = 0;
        private int dropdownMaxVisibleItems = 0;
        private string? selectedTag = null;

        private const int MaxVisibleOptions = 4;
        private const int OptionVerticalPadding = 8;

        public bool IsOpen => dropdownOpen;
        public List<ClickableComponent> Options => dropdownOptions;
        public int FirstVisibleIndex => dropdownFirstVisibleIndex;
        public int MaxVisibleItems => dropdownMaxVisibleItems;
        public string? SelectedTag => selectedTag;

        public string SelectedTagDisplay => selectedTag ?? TranslationCache.SaveSetTagNone;

        private static readonly List<(string key, Func<string> getLabel)> PredefinedTags = new()
        {
            ("none", () => TranslationCache.SaveSetTagNone),
            ("spring", () => TranslationCache.SaveSetTagSpring),
            ("summer", () => TranslationCache.SaveSetTagSummer),
            ("fall", () => TranslationCache.SaveSetTagFall),
            ("winter", () => TranslationCache.SaveSetTagWinter),
            ("festival", () => TranslationCache.SaveSetTagFestival),
            ("work", () => TranslationCache.SaveSetTagWork),
            ("formal", () => TranslationCache.SaveSetTagFormal),
            ("casual", () => TranslationCache.SaveSetTagCasual)
        };

        public SaveSetDropdownManager(SaveSetUIBuilder uiBuilder)
        {
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

            if (uiBuilder.TagDropdownButton == null)
                return;

            float textHeight = Game1.smallFont.MeasureString("Ay").Y;
            int optionHeight = (int)Math.Ceiling(textHeight) + (OptionVerticalPadding * 2);

            dropdownMaxVisibleItems = Math.Min(MaxVisibleOptions, PredefinedTags.Count);

            int maxFirstVisibleIndex = Math.Max(0, PredefinedTags.Count - dropdownMaxVisibleItems);
            dropdownFirstVisibleIndex = Math.Clamp(dropdownFirstVisibleIndex, 0, maxFirstVisibleIndex);

            int dropdownY = uiBuilder.TagDropdownButton.bounds.Bottom;

            for (int i = 0; i < PredefinedTags.Count; i++)
            {
                bool isVisible = (i >= dropdownFirstVisibleIndex && i < dropdownFirstVisibleIndex + dropdownMaxVisibleItems);
                int visualIndex = i - dropdownFirstVisibleIndex;

                var option = new ClickableComponent(
                    new Rectangle(
                        uiBuilder.TagDropdownButton.bounds.X,
                        dropdownY + (visualIndex * optionHeight),
                        uiBuilder.TagDropdownButton.bounds.Width,
                        optionHeight
                    ),
                    PredefinedTags[i].getLabel()
                );
                option.visible = isVisible;
                dropdownOptions.Add(option);
            }
        }

        public void HandleClick(int x, int y, out bool clickedOption)
        {
            for (int i = 0; i < dropdownOptions.Count; i++)
            {
                if (dropdownOptions[i].visible && dropdownOptions[i].containsPoint(x, y))
                {
                    clickedOption = true;
                    string selectedLabel = dropdownOptions[i].name;

                    // Map label back to tag key (null for "None")
                    if (selectedLabel == TranslationCache.SaveSetTagNone)
                    {
                        selectedTag = null;
                    }
                    else
                    {
                        // Find the tag key by matching the label
                        foreach (var (key, getLabel) in PredefinedTags)
                        {
                            if (getLabel() == selectedLabel && key != "none")
                            {
                                selectedTag = key;
                                break;
                            }
                        }
                    }

                    Close();
                    return;
                }
            }

            clickedOption = false;
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

        public bool HandleKeyPress(Keys key)
        {
            if (!dropdownOpen)
                return false;

            int maxFirstVisibleIndex = Math.Max(0, dropdownOptions.Count - dropdownMaxVisibleItems);

            if (key == Keys.Up)
            {
                if (dropdownFirstVisibleIndex > 0)
                {
                    dropdownFirstVisibleIndex--;
                    BuildOptions();
                }
                return true;
            }

            if (key == Keys.Down)
            {
                if (dropdownFirstVisibleIndex < maxFirstVisibleIndex)
                {
                    dropdownFirstVisibleIndex++;
                    BuildOptions();
                }
                return true;
            }

            if (key == Keys.Escape)
            {
                Close();
                return true;
            }

            return true;
        }
    }
}
