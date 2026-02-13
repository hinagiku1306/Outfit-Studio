using System;
using System.Collections.Generic;
using System.Linq;
using OutfitStudio.Models;
using OutfitStudio.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    public class ScheduleSetPickerPanel
    {
        private const int PanelWidth = 300;
        private const int PanelPadding = 35;
        private const int OptionHeight = 36;
        private const int MaxVisibleOptions = 6;
        private const int BottomPadding = 36;
        private const float ScrollArrowScale = 2.5f;
        private const int ScrollArrowRightMargin = 48;
        private const float MatchedOpacity = 1.0f;
        private const float UnmatchedOpacity = 0.4f;

        private readonly OutfitSetStore store;
        private readonly Action onChanged;

        private bool isOpen;
        private Rectangle panelBounds;
        private Rectangle parentBounds;

        private List<OutfitSet> allSets = new();
        private HashSet<string> matchedSetIds = new();
        private List<ClickableComponent> setOptions = new();
        private List<ClickableComponent> clearButtons = new();
        private List<string> truncatedNames = new();
        private List<string> fullNames = new();
        private int firstVisibleIndex;
        private string? hoverTooltip;

        private ClickableComponent? upArrowButton;
        private ClickableComponent? downArrowButton;
        private int listStartY;

        private HashSet<string> selectedTags = new(TranslationCache.TagComparer);
        private bool tagsSelectAll;
        private List<string> includedSetIds = new();
        private List<string> excludedSetIds = new();

        public bool IsOpen => isOpen;
        public Rectangle Bounds => panelBounds;

        public ScheduleSetPickerPanel(OutfitSetStore store, Action onChanged)
        {
            this.store = store;
            this.onChanged = onChanged;
        }

        public void Open(Rectangle parentBounds, HashSet<string> selectedTags, bool tagsSelectAll,
            List<string> includedSetIds, List<string> excludedSetIds)
        {
            this.parentBounds = parentBounds;
            this.selectedTags = selectedTags;
            this.tagsSelectAll = tagsSelectAll;
            this.includedSetIds = includedSetIds;
            this.excludedSetIds = excludedSetIds;
            firstVisibleIndex = 0;
            isOpen = true;

            RefreshData();
            CalculateLayout();
            BuildOptions();
        }

        public void Close()
        {
            isOpen = false;
        }

        public void UpdateState(HashSet<string> selectedTags, bool tagsSelectAll,
            List<string> includedSetIds, List<string> excludedSetIds)
        {
            this.selectedTags = selectedTags;
            this.tagsSelectAll = tagsSelectAll;
            this.includedSetIds = includedSetIds;
            this.excludedSetIds = excludedSetIds;
            RefreshData();
            BuildOptions();
        }

        public void UpdateParentBounds(Rectangle newParentBounds)
        {
            parentBounds = newParentBounds;
            RefreshData();
            CalculateLayout();
            BuildOptions();
        }

        private void RefreshData()
        {
            allSets = store.GetAllSets()
                .Where(s => s.IsValid)
                .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            matchedSetIds.Clear();
            if (tagsSelectAll)
            {
                foreach (var s in allSets)
                    matchedSetIds.Add(s.Id);
            }
            else
            {
                var tagSet = new HashSet<string>(selectedTags, TranslationCache.TagComparer);
                foreach (var s in allSets)
                {
                    if (s.Tags.Any(t => tagSet.Contains(t)))
                        matchedSetIds.Add(s.Id);
                }
            }
        }

        private void CalculateLayout()
        {
            int titleHeight = (int)Game1.smallFont.MeasureString("A").Y;
            int listHeight = MaxVisibleOptions * OptionHeight;

            int totalHeight = PanelPadding
                              + titleHeight + 12 + 12
                              + listHeight
                              + BottomPadding;

            int panelX = parentBounds.Right;
            int panelY = parentBounds.Y + (parentBounds.Height - totalHeight) / 2;

            panelBounds = new Rectangle(panelX, panelY, PanelWidth, totalHeight);
        }

        private void BuildOptions()
        {
            setOptions.Clear();
            clearButtons.Clear();
            truncatedNames.Clear();
            fullNames.Clear();

            int titleHeight = (int)Game1.smallFont.MeasureString("A").Y;
            int optionsY = panelBounds.Y + PanelPadding + titleHeight + 24;
            listStartY = optionsY;

            int optionWidth = panelBounds.Width - (PanelPadding * 2);
            int arrowWidth = (int)(ArrowNativeWidth * ScrollArrowScale);
            int scrollArrowIntrusion = arrowWidth / 2 + ScrollArrowRightMargin - PanelPadding;
            int leftUsed = 4 + SaveSetLocalOnlyCheckboxSize + 8;
            int rightReserved = scrollArrowIntrusion + 8;
            int textMaxWidth = optionWidth - leftUsed - rightReserved;

            for (int i = 0; i < allSets.Count; i++)
            {
                fullNames.Add(allSets[i].Name);
                truncatedNames.Add(UIHelpers.TruncateText(allSets[i].Name, textMaxWidth));
            }

            int maxFirstVisible = Math.Max(0, allSets.Count - MaxVisibleOptions);
            firstVisibleIndex = Math.Clamp(firstVisibleIndex, 0, maxFirstVisible);

            for (int i = 0; i < allSets.Count; i++)
            {
                bool isVisible = i >= firstVisibleIndex && i < firstVisibleIndex + MaxVisibleOptions;
                int rowY = optionsY + (i - firstVisibleIndex) * OptionHeight;

                var option = new ClickableComponent(
                    new Rectangle(panelBounds.X + PanelPadding, rowY, optionWidth, OptionHeight),
                    allSets[i].Id)
                {
                    visible = isVisible
                };
                setOptions.Add(option);

                // Clear button for included sets
                int clearBtnX = option.bounds.Right - ClearButtonSize - 4;
                int clearBtnY = rowY + (OptionHeight - ClearButtonSize) / 2;
                var clearBtn = new ClickableComponent(
                    new Rectangle(clearBtnX, clearBtnY, ClearButtonSize, ClearButtonSize),
                    $"clear:{allSets[i].Id}")
                {
                    visible = isVisible && includedSetIds.Contains(allSets[i].Id)
                };
                clearButtons.Add(clearBtn);
            }

            int listHeight = MaxVisibleOptions * OptionHeight;
            int arrowHeight = (int)(ArrowNativeHeight * ScrollArrowScale);
            int arrowX = panelBounds.Right - ScrollArrowRightMargin - arrowWidth / 2;

            upArrowButton = new ClickableComponent(
                new Rectangle(arrowX, optionsY, arrowWidth, arrowHeight), "upArrow");
            downArrowButton = new ClickableComponent(
                new Rectangle(arrowX, optionsY + listHeight - arrowHeight, arrowWidth, arrowHeight), "downArrow");
        }

        public bool HandleClick(int x, int y, out bool consumed)
        {
            consumed = false;
            if (!isOpen)
                return false;

            if (!panelBounds.Contains(x, y))
                return false;

            consumed = true;

            // Clear buttons first
            for (int i = 0; i < clearButtons.Count; i++)
            {
                if (clearButtons[i].visible && clearButtons[i].containsPoint(x, y))
                {
                    string setId = clearButtons[i].name.Replace("clear:", "");
                    includedSetIds.Remove(setId);
                    BuildOptions();
                    onChanged();
                    Game1.playSound("coin");
                    return true;
                }
            }

            // Set option clicks
            for (int i = 0; i < setOptions.Count; i++)
            {
                if (setOptions[i].visible && setOptions[i].containsPoint(x, y))
                {
                    string setId = setOptions[i].name;
                    if (includedSetIds.Contains(setId))
                    {
                        includedSetIds.Remove(setId);
                    }
                    else
                    {
                        includedSetIds.Add(setId);
                        excludedSetIds.Remove(setId);
                    }
                    BuildOptions();
                    onChanged();
                    Game1.playSound("smallSelect");
                    return true;
                }
            }

            return true;
        }

        public bool HandleScrollWheel(int direction)
        {
            if (!isOpen)
                return false;

            if (direction > 0 && firstVisibleIndex > 0)
            {
                firstVisibleIndex--;
                BuildOptions();
                return true;
            }
            else if (direction < 0 && firstVisibleIndex < Math.Max(0, allSets.Count - MaxVisibleOptions))
            {
                firstVisibleIndex++;
                BuildOptions();
                return true;
            }

            return false;
        }

        public void Draw(SpriteBatch b)
        {
            if (!isOpen)
                return;

            hoverTooltip = null;

            UIHelpers.DrawTextureBox(b, panelBounds.X, panelBounds.Y,
                panelBounds.Width, panelBounds.Height, Color.White);

            // Title
            Vector2 titlePos = new Vector2(panelBounds.X + PanelPadding, panelBounds.Y + PanelPadding);
            Utility.drawTextWithShadow(b, TranslationCache.ScheduleEditSets, Game1.smallFont,
                titlePos, Game1.textColor);

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            // Set options
            for (int i = 0; i < setOptions.Count; i++)
            {
                var option = setOptions[i];
                if (!option.visible)
                    continue;

                string setId = option.name;
                bool isIncluded = includedSetIds.Contains(setId);
                bool isMatched = matchedSetIds.Contains(setId);
                bool isHovered = option.containsPoint(mouseX, mouseY);
                float opacity = (isMatched || isIncluded) ? MatchedOpacity : UnmatchedOpacity;

                if (isHovered)
                    b.Draw(Game1.staminaRect, option.bounds, Color.Wheat * 0.3f);

                // Checkbox
                Rectangle checkboxSource = isIncluded
                    ? new Rectangle(236, 425, 9, 9)
                    : new Rectangle(227, 425, 9, 9);
                int checkboxY = option.bounds.Y + (option.bounds.Height - SaveSetLocalOnlyCheckboxSize) / 2;
                b.Draw(Game1.mouseCursors,
                    new Vector2(option.bounds.X + 4, checkboxY),
                    checkboxSource, Color.White * opacity, 0f, Vector2.Zero,
                    SaveSetLocalOnlyCheckboxScale, SpriteEffects.None, 1f);

                // Name
                string displayText = i < truncatedNames.Count ? truncatedNames[i] : "";
                string fullText = i < fullNames.Count ? fullNames[i] : "";
                float textHeight = Game1.smallFont.MeasureString(displayText).Y;
                Vector2 textPos = new Vector2(
                    option.bounds.X + 4 + SaveSetLocalOnlyCheckboxSize + 8,
                    option.bounds.Y + (option.bounds.Height - textHeight) / 2);
                Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPos, Game1.textColor * opacity);

                // Clear button for included sets
                if (i < clearButtons.Count && clearButtons[i].visible)
                    UIHelpers.DrawClearButton(b, clearButtons[i]);

                if (isHovered && displayText != fullText)
                    hoverTooltip = fullText;
            }

            // Scroll arrows
            int arrowHeight = (int)(ArrowNativeHeight * ScrollArrowScale);

            if (firstVisibleIndex > 0 && upArrowButton != null)
            {
                Rectangle upArrowSource = new Rectangle(421, 459, 11, 12);
                Vector2 upPos = new Vector2(upArrowButton.bounds.X, listStartY + 4);
                b.Draw(Game1.mouseCursors, upPos, upArrowSource, Color.White, 0f,
                    Vector2.Zero, ScrollArrowScale, SpriteEffects.None, 1f);
            }

            if (firstVisibleIndex < Math.Max(0, allSets.Count - MaxVisibleOptions) && downArrowButton != null)
            {
                Rectangle downArrowSource = new Rectangle(421, 472, 11, 12);
                Vector2 downPos = new Vector2(
                    downArrowButton.bounds.X,
                    listStartY + MaxVisibleOptions * OptionHeight - arrowHeight - 8);
                b.Draw(Game1.mouseCursors, downPos, downArrowSource, Color.White, 0f,
                    Vector2.Zero, ScrollArrowScale, SpriteEffects.None, 1f);
            }

            if (hoverTooltip != null && ModEntry.Config.ShowTooltip)
            {
                string text = hoverTooltip.Contains(' ')
                    ? Game1.parseText(hoverTooltip, Game1.smallFont, 300)
                    : hoverTooltip;
                IClickableMenu.drawHoverText(b, text, Game1.smallFont);
            }
        }
    }
}
