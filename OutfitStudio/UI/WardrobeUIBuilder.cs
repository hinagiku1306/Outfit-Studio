using System;
using System.Collections.Generic;
using OutfitStudio.Managers;
using OutfitStudio.Models;
using OutfitStudio.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    public class WardrobeUIBuilder
    {
        public Rectangle LeftPanel { get; private set; }
        public Rectangle RightPanel { get; private set; }

        public ClickableComponent SearchScopeDropdown { get; private set; } = null!;
        public ClickableComponent SearchBar { get; private set; } = null!;
        public ClickableComponent TagsDropdown { get; private set; } = null!;
        public ClickableComponent FilterDropdown { get; private set; } = null!;
        public ClickableComponent MatchAllCheckbox { get; private set; } = null!;
        public ClickableComponent ShowInvalidCheckbox { get; private set; } = null!;
        public ClickableComponent TagsClearButton { get; private set; } = null!;
        public ClickableComponent FilterClearButton { get; private set; } = null!;
        public ClickableComponent SearchClearButton { get; private set; } = null!;

        public List<ClickableComponent> SearchScopeOptions { get; private set; } = new();
        public List<ClickableComponent> TagsOptions { get; private set; } = new();
        public List<ClickableComponent> FilterOptions { get; private set; } = new();

        public List<ClickableComponent> OutfitListItems { get; private set; } = new();
        public Rectangle LeftPanelBox { get; private set; }
        public Rectangle RightPanelBox { get; private set; }
        public Rectangle PreviewBox { get; private set; }
        public Rectangle HatSlot { get; private set; }
        public Rectangle ShirtSlot { get; private set; }
        public Rectangle PantsSlot { get; private set; }

        public ClickableComponent ApplyCloseButton { get; private set; } = null!;
        public ClickableComponent EditButton { get; private set; } = null!;
        public ClickableComponent DeleteButton { get; private set; } = null!;
        public ClickableTextureComponent CloseButton { get; private set; } = null!;
        public ClickableTextureComponent LeftArrowButton { get; private set; } = null!;
        public ClickableTextureComponent RightArrowButton { get; private set; } = null!;

        public Rectangle TagsTextBounds { get; private set; }
        public bool TagsTextTruncated { get; private set; }
        public string? HoveredTruncatedSetName { get; private set; }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        private int tagsFirstVisible;

        // Cached calculated widths for components
        private int searchScopeWidth;
        private int tagsDropdownWidth;
        private int filterDropdownWidth;
        private int outfitListPanelWidth;
        private int previewPanelWidth;

        // Background padding (space from menu edge to content)
        private const int BackgroundPaddingTop = 40;
        private const int BackgroundPaddingLeft = 40;
        private const int BackgroundPaddingRight = 40;
        private const int BackgroundPaddingBottom = 20;

        // Gap between sections (left/right panels, button row)
        private const int SectionGap = 10;

        // Preview panel (left) internal padding
        private const int PreviewPanelPadding = 25;

        // Outfit list panel (right) internal padding
        private const int OutfitListPanelPadding = 20;

        public WardrobeUIBuilder()
        {
            Recalculate();
        }

        private int CalculateOutfitListPanelWidth()
        {
            // Dropdown padding: 20px left + 20px right base = 40px (no arrow)
            int dropdownPadding = 40;

            // Row 1: Search scope dropdown (widest label) + gap + search bar (minimum width)
            string[] scopeLabels = { TranslationCache.WardrobeFilterSearchAll, TranslationCache.WardrobeFilterSearchSet, TranslationCache.WardrobeFilterSearchItem };
            int maxScopeWidth = 0;
            foreach (var label in scopeLabels)
            {
                int labelWidth = (int)Game1.smallFont.MeasureString(label).X + dropdownPadding;
                maxScopeWidth = Math.Max(maxScopeWidth, labelWidth);
            }
            searchScopeWidth = Math.Max(maxScopeWidth, TabAndButtonWidth);

            int minSearchBarWidth = 200;
            int row1Width = searchScopeWidth + WardrobeFilterGap + minSearchBarWidth;

            // Row 3: Checkboxes with labels
            int checkboxLabelGap = 10;
            int checkboxGap = 24;
            float matchAllLabelWidth = Game1.smallFont.MeasureString(TranslationCache.WardrobeFilterMatchAll).X;
            float showInvalidLabelWidth = Game1.smallFont.MeasureString(TranslationCache.WardrobeFilterShowInvalid).X;
            int row3Width = WardrobeCheckboxSize + checkboxLabelGap + (int)matchAllLabelWidth + checkboxGap + WardrobeCheckboxSize + checkboxLabelGap + (int)showInvalidLabelWidth;

            int panelContentWidth = Math.Max(row1Width, row3Width) + ContentBoxPadding;

            // Row 2: Tags and Filter split the full width equally
            tagsDropdownWidth = (panelContentWidth - WardrobeFilterGap) / 2;
            filterDropdownWidth = panelContentWidth - WardrobeFilterGap - tagsDropdownWidth;

            return panelContentWidth;
        }

        private int CalculatePreviewPanelWidth()
        {
            int previewGroupWidth = SaveSetPreviewWidth + SaveSetPreviewToSlotsGap + SaveSetItemSlotSize;
            return previewGroupWidth + ContentBoxPadding * 2;
        }

        private int CalculatePanelHeight()
        {
            // Outfit list panel height: filter bar + list items
            int filterBarHeight = (TabAndButtonHeight * 2) + WardrobeCheckboxSize + (WardrobeFilterGap * 2) + CheckboxExtraGap + ContentBoxPadding;
            int minListHeight = WardrobeListItemHeight * 6;
            int listHeight = filterBarHeight + minListHeight;

            // Preview panel height: preview section (with arrows) + detail text
            int itemSlotsHeight = (SaveSetItemSlotSize * 3) + (SaveSetItemSlotGap * 2);
            int arrowHeight = (int)(ArrowNativeHeight * WardrobeArrowScale);
            int previewColumnHeight = SaveSetPreviewHeight + ElementGap + arrowHeight;
            int previewHeight = Math.Max(previewColumnHeight, itemSlotsHeight);
            int detailsHeight = 32 * 2;
            int previewTotalHeight = previewHeight + detailsHeight + ContentBoxPadding * 2;

            return Math.Max(listHeight, previewTotalHeight);
        }

        public int CalculateRequiredWidth()
        {
            int leftPanelTotal = previewPanelWidth + PreviewPanelPadding * 2;
            int rightPanelTotal = outfitListPanelWidth + OutfitListPanelPadding * 2;
            return BackgroundPaddingLeft + leftPanelTotal + SectionGap + rightPanelTotal + BackgroundPaddingRight;
        }

        public int CalculateRequiredHeight()
        {
            int panelContentHeight = CalculatePanelHeight();
            int leftPanelTotal = panelContentHeight + PreviewPanelPadding * 2;
            int rightPanelTotal = panelContentHeight + OutfitListPanelPadding * 2;
            int panelRowHeight = Math.Max(leftPanelTotal, rightPanelTotal);
            return BackgroundPaddingTop + panelRowHeight + SectionGap + WardrobeBottomButtonHeight + BackgroundPaddingBottom;
        }

        public void Recalculate()
        {
            outfitListPanelWidth = CalculateOutfitListPanelWidth();
            previewPanelWidth = CalculatePreviewPanelWidth();

            Width = CalculateRequiredWidth();
            Height = CalculateRequiredHeight();

            X = (Game1.uiViewport.Width - Width) / 2;
            Y = (Game1.uiViewport.Height - Height) / 2;

            CalculateLayout();
        }

        private void CalculateLayout()
        {
            int panelContentHeight = CalculatePanelHeight();

            // Left panel = Character Preview
            LeftPanelBox = new Rectangle(
                X + BackgroundPaddingLeft,
                Y + BackgroundPaddingTop,
                previewPanelWidth + PreviewPanelPadding * 2,
                panelContentHeight + PreviewPanelPadding * 2
            );

            LeftPanel = new Rectangle(
                LeftPanelBox.X + PreviewPanelPadding,
                LeftPanelBox.Y + PreviewPanelPadding,
                previewPanelWidth,
                panelContentHeight
            );

            // Right panel = Outfit Set List (match left panel height)
            RightPanelBox = new Rectangle(
                LeftPanelBox.Right + SectionGap,
                Y + BackgroundPaddingTop,
                outfitListPanelWidth + OutfitListPanelPadding * 2,
                LeftPanelBox.Height
            );

            RightPanel = new Rectangle(
                RightPanelBox.X + OutfitListPanelPadding,
                RightPanelBox.Y + OutfitListPanelPadding,
                outfitListPanelWidth,
                RightPanelBox.Height - OutfitListPanelPadding * 2
            );

            CalculateFilterBar();
            CalculateListItems();
            CalculatePreviewAndButtons();
        }

        private const int CheckboxExtraGap = 8;

        private void CalculateFilterBar()
        {
            // Filter bar is in the RIGHT panel (outfit list)
            int row1Y = RightPanel.Y;
            int row2Y = row1Y + TabAndButtonHeight + WardrobeFilterGap;
            int row3Y = row2Y + TabAndButtonHeight + WardrobeFilterGap + CheckboxExtraGap;

            SearchScopeDropdown = new ClickableComponent(
                new Rectangle(RightPanel.X, row1Y, searchScopeWidth, TabAndButtonHeight),
                "searchScope"
            );

            int searchBarX = SearchScopeDropdown.bounds.Right + WardrobeFilterGap;
            SearchBar = new ClickableComponent(
                new Rectangle(searchBarX, row1Y, RightPanel.Right - searchBarX, TabAndButtonHeight),
                "searchBar"
            );

            TagsDropdown = new ClickableComponent(
                new Rectangle(RightPanel.X, row2Y, tagsDropdownWidth, TabAndButtonHeight),
                "tagsDropdown"
            );

            FilterDropdown = new ClickableComponent(
                new Rectangle(TagsDropdown.bounds.Right + WardrobeFilterGap, row2Y, filterDropdownWidth, TabAndButtonHeight),
                "filterDropdown"
            );

            // Clear buttons positioned inside their respective components
            TagsClearButton = new ClickableComponent(
                new Rectangle(
                    TagsDropdown.bounds.Right - ClearButtonRightMargin - ClearButtonSize,
                    row2Y + (TabAndButtonHeight - ClearButtonSize) / 2,
                    ClearButtonSize, ClearButtonSize),
                "TagsClear"
            );

            FilterClearButton = new ClickableComponent(
                new Rectangle(
                    FilterDropdown.bounds.Right - ClearButtonRightMargin - ClearButtonSize,
                    row2Y + (TabAndButtonHeight - ClearButtonSize) / 2,
                    ClearButtonSize, ClearButtonSize),
                "FilterClear"
            );

            SearchClearButton = new ClickableComponent(
                new Rectangle(
                    SearchBar.bounds.Right - ClearButtonRightMargin - ClearButtonSize,
                    row1Y + (TabAndButtonHeight - ClearButtonSize) / 2,
                    ClearButtonSize, ClearButtonSize),
                "SearchClear"
            );

            int checkboxLabelGap = 10;
            MatchAllCheckbox = new ClickableComponent(
                new Rectangle(RightPanel.X, row3Y, WardrobeCheckboxSize, WardrobeCheckboxSize),
                "matchAll"
            );

            float matchAllLabelWidth = Game1.smallFont.MeasureString(TranslationCache.WardrobeFilterMatchAll).X;
            int showInvalidX = MatchAllCheckbox.bounds.Right + (int)matchAllLabelWidth + checkboxLabelGap + 24;

            ShowInvalidCheckbox = new ClickableComponent(
                new Rectangle(showInvalidX, row3Y, WardrobeCheckboxSize, WardrobeCheckboxSize),
                "showInvalid"
            );
        }

        private void CalculateListItems()
        {
            // List items are in the RIGHT panel (outfit list)
            int filterBarHeight = (TabAndButtonHeight * 2) + WardrobeCheckboxSize + (WardrobeFilterGap * 2) + CheckboxExtraGap + ContentBoxPadding;
            int listY = RightPanel.Y + filterBarHeight;
            int listHeight = RightPanel.Bottom - listY;
            int maxItems = listHeight / WardrobeListItemHeight;

            OutfitListItems.Clear();
            for (int i = 0; i < maxItems; i++)
            {
                OutfitListItems.Add(new ClickableComponent(
                    new Rectangle(RightPanel.X, listY + (i * WardrobeListItemHeight),
                        RightPanel.Width, WardrobeListItemHeight),
                    $"outfit_{i}"
                ));
            }
        }

        private void CalculatePreviewAndButtons()
        {
            // Preview is in the LEFT panel, vertically centered
            int itemSlotsHeight = (SaveSetItemSlotSize * 3) + (SaveSetItemSlotGap * 2);
            int arrowWidth = (int)(ArrowNativeWidth * WardrobeArrowScale);
            int arrowHeight = (int)(ArrowNativeHeight * WardrobeArrowScale);
            int previewColumnHeight = SaveSetPreviewHeight + ElementGap + arrowHeight;
            int previewSectionHeight = Math.Max(previewColumnHeight, itemSlotsHeight);
            int detailsHeight = 32 * 2;
            int totalPreviewContentHeight = previewSectionHeight + ContentBoxPadding + detailsHeight;

            int previewGroupWidth = SaveSetPreviewWidth + SaveSetPreviewToSlotsGap + SaveSetItemSlotSize;
            int previewGroupX = LeftPanel.X + (LeftPanel.Width - previewGroupWidth) / 2;

            // Vertically center the entire preview content in the left panel
            int previewStartY = LeftPanel.Y + (LeftPanel.Height - totalPreviewContentHeight) / 2;

            int previewColumnY = previewStartY + (previewSectionHeight - previewColumnHeight) / 2;
            PreviewBox = new Rectangle(previewGroupX, previewColumnY, SaveSetPreviewWidth, SaveSetPreviewHeight);

            // Direction arrows centered below preview
            int arrowY = PreviewBox.Bottom + ElementGap;
            int totalArrowsWidth = arrowWidth * 2 + ArrowGap;
            int arrowsCenterX = PreviewBox.X + SaveSetPreviewWidth / 2;
            int arrowsStartX = arrowsCenterX - totalArrowsWidth / 2;

            LeftArrowButton = new ClickableTextureComponent(
                new Rectangle(arrowsStartX, arrowY, arrowWidth, arrowHeight),
                Game1.mouseCursors,
                new Rectangle(352, 495, ArrowNativeWidth, ArrowNativeHeight),
                WardrobeArrowScale
            );
            RightArrowButton = new ClickableTextureComponent(
                new Rectangle(arrowsStartX + arrowWidth + ArrowGap, arrowY, arrowWidth, arrowHeight),
                Game1.mouseCursors,
                new Rectangle(365, 495, ArrowNativeWidth, ArrowNativeHeight),
                WardrobeArrowScale
            );

            int itemSlotsX = previewGroupX + SaveSetPreviewWidth + SaveSetPreviewToSlotsGap;
            int itemSlotsY = PreviewBox.Y + (SaveSetPreviewHeight - itemSlotsHeight) / 2;

            HatSlot = new Rectangle(itemSlotsX, itemSlotsY, SaveSetItemSlotSize, SaveSetItemSlotSize);
            ShirtSlot = new Rectangle(itemSlotsX, itemSlotsY + SaveSetItemSlotSize + SaveSetItemSlotGap, SaveSetItemSlotSize, SaveSetItemSlotSize);
            PantsSlot = new Rectangle(itemSlotsX, itemSlotsY + (SaveSetItemSlotSize + SaveSetItemSlotGap) * 2, SaveSetItemSlotSize, SaveSetItemSlotSize);

            // Button row is below panels with SectionGap
            int panelRowBottom = Math.Max(LeftPanelBox.Bottom, RightPanelBox.Bottom);
            int buttonY = panelRowBottom + SectionGap;

            int applyCloseWidth = UIHelpers.CalculateButtonWidth(TranslationCache.WardrobeButtonApplyClose);
            int editWidth = UIHelpers.CalculateButtonWidth(TranslationCache.WardrobeButtonEdit);
            int deleteWidth = UIHelpers.CalculateButtonWidth(TranslationCache.WardrobeButtonDelete);

            // Edit/Delete centered horizontally under left panel (character preview)
            int editDeleteTotalWidth = editWidth + BottomButtonGap + deleteWidth;
            int editDeleteX = LeftPanelBox.X + (LeftPanelBox.Width - editDeleteTotalWidth) / 2;

            EditButton = new ClickableComponent(
                new Rectangle(editDeleteX, buttonY, editWidth, TabAndButtonHeight),
                "edit"
            );
            DeleteButton = new ClickableComponent(
                new Rectangle(editDeleteX + editWidth + BottomButtonGap, buttonY, deleteWidth, TabAndButtonHeight),
                "delete"
            );

            // Apply & Close right-aligned under right panel (outfit set list)
            int applyCloseX = RightPanelBox.Right - applyCloseWidth;
            ApplyCloseButton = new ClickableComponent(
                new Rectangle(applyCloseX, buttonY, applyCloseWidth, TabAndButtonHeight),
                "applyClose"
            );

            CloseButton = new ClickableTextureComponent(
                new Rectangle(X + Width - CloseButtonSize - CloseButtonEdgeMargin,
                    Y + CloseButtonEdgeMargin, CloseButtonSize, CloseButtonSize),
                Game1.mouseCursors,
                new Rectangle(337, 494, 12, 12),
                4f
            );
        }

        public void BuildSearchScopeOptions(SearchScope currentScope)
        {
            SearchScopeOptions.Clear();
            float textHeight = Game1.smallFont.MeasureString("A").Y;
            int optionHeight = (int)Math.Ceiling(textHeight) + 16;

            string[] labels = { TranslationCache.WardrobeFilterSearchSet, TranslationCache.WardrobeFilterSearchItem, TranslationCache.WardrobeFilterSearchAll };

            for (int i = 0; i < labels.Length; i++)
            {
                SearchScopeOptions.Add(new ClickableComponent(
                    new Rectangle(
                        SearchScopeDropdown.bounds.X,
                        SearchScopeDropdown.bounds.Bottom + (i * optionHeight),
                        SearchScopeDropdown.bounds.Width,
                        optionHeight
                    ),
                    labels[i]
                ) { visible = true });
            }
        }

        public void BuildTagsOptions(List<string> allTags, HashSet<string> selectedTags)
        {
            TagsOptions.Clear();
            float textHeight = Game1.smallFont.MeasureString("A").Y;
            int optionHeight = (int)Math.Ceiling(textHeight) + 16;

            int maxVisible = Math.Min(WardrobeDropdownMaxVisible, allTags.Count);
            int maxFirst = Math.Max(0, allTags.Count - maxVisible);
            tagsFirstVisible = Math.Clamp(tagsFirstVisible, 0, maxFirst);

            for (int i = 0; i < allTags.Count; i++)
            {
                bool isVisible = i >= tagsFirstVisible && i < tagsFirstVisible + maxVisible;
                int visualIndex = i - tagsFirstVisible;

                var option = new ClickableComponent(
                    new Rectangle(
                        TagsDropdown.bounds.X,
                        TagsDropdown.bounds.Bottom + (visualIndex * optionHeight),
                        TagsDropdown.bounds.Width,
                        optionHeight
                    ),
                    allTags[i]
                ) { visible = isVisible };

                TagsOptions.Add(option);
            }
        }

        public void BuildFilterOptions(SetFilterState filter)
        {
            FilterOptions.Clear();
            float textHeight = Game1.smallFont.MeasureString("A").Y;
            int optionHeight = (int)Math.Ceiling(textHeight) + 16;

            string[] labels = { TranslationCache.CommonFavorite, TranslationCache.WardrobeFilterInvalid, TranslationCache.WardrobeFilterGlobal, TranslationCache.WardrobeFilterLocal };

            for (int i = 0; i < labels.Length; i++)
            {
                FilterOptions.Add(new ClickableComponent(
                    new Rectangle(
                        FilterDropdown.bounds.X,
                        FilterDropdown.bounds.Bottom + (i * optionHeight),
                        FilterDropdown.bounds.Width,
                        optionHeight
                    ),
                    labels[i]
                ) { visible = true });
            }
        }

        public bool ScrollTagsDropdown(int direction)
        {
            int maxVisible = Math.Min(WardrobeDropdownMaxVisible, TagsOptions.Count);
            int maxFirst = Math.Max(0, TagsOptions.Count - maxVisible);

            if (direction > 0 && tagsFirstVisible > 0)
            {
                tagsFirstVisible--;
                return true;
            }
            if (direction < 0 && tagsFirstVisible < maxFirst)
            {
                tagsFirstVisible++;
                return true;
            }
            return false;
        }

        public void DrawBackground(SpriteBatch b)
        {
            UIHelpers.DrawTextureBox(b, X, Y, Width, Height, Color.White);
        }

        public void DrawLeftPanelBox(SpriteBatch b)
        {
            UIHelpers.DrawTextureBox(b, LeftPanelBox.X, LeftPanelBox.Y,
                LeftPanelBox.Width, LeftPanelBox.Height, Color.White);
        }

        public void DrawRightPanelBox(SpriteBatch b)
        {
            UIHelpers.DrawTextureBox(b, RightPanelBox.X, RightPanelBox.Y,
                RightPanelBox.Width, RightPanelBox.Height, Color.White);
        }

        public void DrawOutfitSetList(SpriteBatch b, List<OutfitSet> sets, int selectedIndex, int scrollOffset, SetFilterState filter,
            bool searchScopeOpen, bool tagsOpen, bool filterOpen, string searchText, List<string> allTags, bool searchFocused)
        {
            DrawFilterBar(b, filter, searchScopeOpen, tagsOpen, filterOpen, searchText, allTags, searchFocused);

            int filterBarHeight = (TabAndButtonHeight * 2) + WardrobeCheckboxSize + (WardrobeFilterGap * 2) + CheckboxExtraGap + ContentBoxPadding;
            int separatorY = RightPanel.Y + filterBarHeight - ContentBoxPadding / 2;
            b.Draw(Game1.staminaRect, new Rectangle(RightPanel.X, separatorY, RightPanel.Width, 2), Color.Gray * 0.3f);

            float stardropScale = 2.8f;
            int stardropSize = (int)(8 * stardropScale);
            float warningScale = 3f;
            int warningWidth = (int)(3 * warningScale);
            int warningHeight = (int)(8 * warningScale);
            Rectangle warningSourceRect = new Rectangle(395, 497, 3, 8);
            int iconGap = 10;
            int leftMargin = 8;
            int maxIconAreaWidth = stardropSize + iconGap + warningWidth + iconGap;
            int maxTextWidth = RightPanel.Width - leftMargin - maxIconAreaWidth - 8;

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            bool anyDropdownOpen = searchScopeOpen || tagsOpen || filterOpen;
            HoveredTruncatedSetName = null;

            for (int i = 0; i < OutfitListItems.Count; i++)
            {
                int dataIndex = scrollOffset + i;
                if (dataIndex >= sets.Count)
                    break;

                var item = OutfitListItems[i];
                var set = sets[dataIndex];
                bool isHovered = !anyDropdownOpen && item.bounds.Contains(mouseX, mouseY);

                if (dataIndex == selectedIndex)
                    b.Draw(Game1.staminaRect, item.bounds, Color.Wheat);
                else if (isHovered)
                    b.Draw(Game1.staminaRect, item.bounds, Color.Wheat * 0.6f);

                int iconX = item.bounds.X + leftMargin;
                bool hasAnyIcon = false;

                if (set.IsFavorite)
                {
                    int iconY = item.bounds.Y + (WardrobeListItemHeight - stardropSize) / 2;
                    b.Draw(Game1.mouseCursors,
                        new Vector2(iconX, iconY),
                        new Rectangle(346, 392, 8, 8),
                        Color.White, 0f, Vector2.Zero, stardropScale, SpriteEffects.None, 1f);
                    iconX += stardropSize;
                    hasAnyIcon = true;
                }

                if (!set.IsValid)
                {
                    if (hasAnyIcon)
                        iconX += iconGap;
                    int iconY = item.bounds.Y + (WardrobeListItemHeight - warningHeight) / 2;
                    b.Draw(Game1.mouseCursors,
                        new Vector2(iconX, iconY),
                        warningSourceRect,
                        Color.White, 0f, Vector2.Zero, warningScale, SpriteEffects.None, 1f);
                    iconX += warningWidth;
                    hasAnyIcon = true;
                }

                int textX = hasAnyIcon ? iconX + iconGap : item.bounds.X + leftMargin;
                string truncatedText = UIHelpers.TruncateText(set.Name, maxTextWidth);
                Utility.drawTextWithShadow(b, truncatedText, Game1.smallFont,
                    new Vector2(textX, item.bounds.Y + 12), Game1.textColor);

                if (isHovered && truncatedText != set.Name)
                    HoveredTruncatedSetName = set.Name;
            }

            if (sets.Count == 0)
            {
                string emptyMessage = filter.HasActiveFilters
                    ? TranslationCache.WardrobeFilterNoResults
                    : TranslationCache.WardrobeNoSets;
                Vector2 textSize = Game1.smallFont.MeasureString(emptyMessage);
                int listStartY = RightPanel.Y + filterBarHeight;
                int listHeight = RightPanel.Bottom - listStartY;
                Utility.drawTextWithShadow(b, emptyMessage, Game1.smallFont,
                    new Vector2(RightPanel.X + (RightPanel.Width - textSize.X) / 2,
                        listStartY + (listHeight - textSize.Y) / 2), Color.Gray);
            }

            DrawListScrollIndicators(b, sets.Count, scrollOffset);
        }

        private void DrawListScrollIndicators(SpriteBatch b, int totalItems, int scrollOffset)
        {
            int maxVisible = OutfitListItems.Count;
            if (totalItems <= maxVisible)
                return;

            int filterBarHeight = (TabAndButtonHeight * 2) + WardrobeCheckboxSize + (WardrobeFilterGap * 2) + CheckboxExtraGap + ContentBoxPadding;
            int listStartY = RightPanel.Y + filterBarHeight;

            bool canScrollUp = scrollOffset > 0;
            bool canScrollDown = scrollOffset + maxVisible < totalItems;

            float arrowScale = 2.5f;
            int arrowWidth = (int)(11 * arrowScale);
            int arrowHeight = (int)(12 * arrowScale);

            if (canScrollUp)
            {
                Rectangle upArrow = new Rectangle(421, 459, 11, 12);
                Vector2 upPos = new Vector2(RightPanel.Right - arrowWidth - 4, listStartY + 4);
                b.Draw(Game1.mouseCursors, upPos, upArrow, Color.White, 0f, Vector2.Zero, arrowScale, SpriteEffects.None, 1f);
            }

            if (canScrollDown)
            {
                Rectangle downArrow = new Rectangle(421, 472, 11, 12);
                Vector2 downPos = new Vector2(RightPanel.Right - arrowWidth - 4, RightPanel.Bottom - arrowHeight - 4);
                b.Draw(Game1.mouseCursors, downPos, downArrow, Color.White, 0f, Vector2.Zero, arrowScale, SpriteEffects.None, 1f);
            }
        }

        private void DrawFilterBar(SpriteBatch b, SetFilterState filter, bool searchScopeOpen, bool tagsOpen, bool filterOpen,
            string searchText, List<string> allTags, bool searchFocused)
        {
            string scopeLabel = filter.SearchScope switch
            {
                SearchScope.Set => TranslationCache.WardrobeFilterSearchSet,
                SearchScope.Item => TranslationCache.WardrobeFilterSearchItem,
                SearchScope.All => TranslationCache.WardrobeFilterSearchAll,
                _ => TranslationCache.WardrobeFilterSearchSet
            };
            UIHelpers.DrawDropdownButton(b, SearchScopeDropdown.bounds, scopeLabel, searchScopeOpen);

            DrawSearchBar(b, SearchBar, searchText, searchFocused);

            bool hasTags = filter.SelectedTags.Count > 0;
            string tagsLabel = hasTags
                ? $"{TranslationCache.WardrobeFilterTags} ({filter.SelectedTags.Count})"
                : TranslationCache.WardrobeFilterTags;
            UIHelpers.DrawDropdownButton(b, TagsDropdown.bounds, tagsLabel, tagsOpen,
                clearButton: TagsClearButton, hasValue: hasTags, drawClearButton: UIHelpers.DrawClearButton);

            bool hasFilters = filter.FavoritesOnly || !filter.ShowGlobal || !filter.ShowLocal || filter.InvalidOnly;
            string filterLabel = TranslationCache.WardrobeFilterFilter;
            UIHelpers.DrawDropdownButton(b, FilterDropdown.bounds, filterLabel, filterOpen,
                clearButton: FilterClearButton, hasValue: hasFilters, drawClearButton: UIHelpers.DrawClearButton);

            DrawCheckbox(b, MatchAllCheckbox, TranslationCache.WardrobeFilterMatchAll, filter.MatchAllTags);
            DrawCheckbox(b, ShowInvalidCheckbox, TranslationCache.WardrobeFilterShowInvalid, filter.ShowInvalid);
        }

        public void DrawSearchBar(SpriteBatch b, ClickableComponent bar, string text, bool isFocused)
        {
            UIHelpers.DrawTextureBox(b, bar.bounds.X, bar.bounds.Y,
                bar.bounds.Width, bar.bounds.Height, Color.White);

            bool showPlaceholder = string.IsNullOrEmpty(text);
            string displayText = showPlaceholder ? TranslationCache.WardrobeSearch : text;
            Color textColor = showPlaceholder ? Color.Gray : Game1.textColor;

            float textHeight = Game1.smallFont.MeasureString("A").Y;
            Vector2 textPos = new Vector2(
                bar.bounds.X + 20,
                bar.bounds.Y + (bar.bounds.Height - textHeight) / 2
            );
            Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPos, textColor);

            if (!showPlaceholder)
            {
                UIHelpers.DrawClearButton(b, SearchClearButton);
            }

            if (isFocused)
            {
                float cursorX = showPlaceholder ? textPos.X : textPos.X + Game1.smallFont.MeasureString(text).X;
                if ((int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 500) % 2 == 0)
                {
                    b.Draw(Game1.staminaRect,
                        new Rectangle((int)cursorX, (int)textPos.Y, 4, (int)textHeight),
                        Game1.textColor);
                }
            }
        }

        private void DrawCheckbox(SpriteBatch b, ClickableComponent checkbox, string label, bool isChecked)
        {
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            float labelWidth = Game1.smallFont.MeasureString(label).X;
            Rectangle hitArea = new Rectangle(
                checkbox.bounds.X,
                checkbox.bounds.Y,
                checkbox.bounds.Width + 10 + (int)labelWidth,
                checkbox.bounds.Height
            );
            bool isHovered = hitArea.Contains(mouseX, mouseY);

            Rectangle sourceRect = isChecked
                ? new Rectangle(236, 425, 9, 9)
                : new Rectangle(227, 425, 9, 9);

            b.Draw(Game1.mouseCursors,
                new Vector2(checkbox.bounds.X, checkbox.bounds.Y),
                sourceRect, Color.White, 0f, Vector2.Zero, SaveSetLocalOnlyCheckboxScale, SpriteEffects.None, 1f);

            float labelHeight = Game1.smallFont.MeasureString(label).Y;
            Vector2 labelPos = new Vector2(
                checkbox.bounds.Right + 10,
                checkbox.bounds.Y + (checkbox.bounds.Height - labelHeight) / 2
            );

            if (isHovered)
            {
                Utility.drawTextWithShadow(b, label, Game1.smallFont, labelPos + new Vector2(-1, 0), Game1.textColor * 0.8f);
                Utility.drawTextWithShadow(b, label, Game1.smallFont, labelPos, Game1.textColor);
            }
            else
            {
                Utility.drawTextWithShadow(b, label, Game1.smallFont, labelPos, Game1.textColor);
            }
        }

        public void DrawSearchScopeDropdown(SpriteBatch b)
        {
            if (SearchScopeOptions.Count == 0)
                return;

            int optionHeight = SearchScopeOptions[0].bounds.Height;
            int dropdownHeight = SearchScopeOptions.Count * optionHeight;

            UIHelpers.DrawTextureBoxNoShadow(b,
                SearchScopeDropdown.bounds.X - 4,
                SearchScopeDropdown.bounds.Bottom - 4,
                SearchScopeDropdown.bounds.Width + 8,
                dropdownHeight + 8,
                Color.White);

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            for (int i = 0; i < SearchScopeOptions.Count; i++)
            {
                var option = SearchScopeOptions[i];
                bool isHovered = option.containsPoint(mouseX, mouseY);

                if (isHovered)
                    b.Draw(Game1.staminaRect, option.bounds, Color.Wheat * 0.6f);

                Vector2 textSize = Game1.smallFont.MeasureString(option.name);
                Vector2 textPos = new Vector2(
                    option.bounds.X + 16,
                    option.bounds.Y + (option.bounds.Height - textSize.Y) / 2
                );
                Utility.drawTextWithShadow(b, option.name, Game1.smallFont, textPos, Game1.textColor);
            }
        }

        public string? DrawTagsDropdown(SpriteBatch b, HashSet<string> selectedTags)
        {
            if (TagsOptions.Count == 0)
                return null;

            string? hoveredTruncatedText = null;
            int visibleCount = 0;
            foreach (var opt in TagsOptions)
            {
                if (opt.visible) visibleCount++;
            }
            if (visibleCount == 0)
                return null;

            int optionHeight = TagsOptions[0].bounds.Height;
            int dropdownHeight = visibleCount * optionHeight;

            UIHelpers.DrawTextureBoxNoShadow(b,
                TagsDropdown.bounds.X - 4,
                TagsDropdown.bounds.Bottom - 4,
                TagsDropdown.bounds.Width + 8,
                dropdownHeight + 8,
                Color.White);

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            foreach (var option in TagsOptions)
            {
                if (!option.visible)
                    continue;

                bool isSelected = selectedTags.Contains(option.name);
                bool isHovered = option.containsPoint(mouseX, mouseY);

                if (isHovered)
                    b.Draw(Game1.staminaRect, option.bounds, Color.Wheat * 0.6f);

                Rectangle checkboxSource = isSelected
                    ? new Rectangle(236, 425, 9, 9)
                    : new Rectangle(227, 425, 9, 9);

                b.Draw(Game1.mouseCursors,
                    new Vector2(option.bounds.X + 12, option.bounds.Y + (option.bounds.Height - SaveSetLocalOnlyCheckboxSize) / 2),
                    checkboxSource, Color.White, 0f, Vector2.Zero, SaveSetLocalOnlyCheckboxScale, SpriteEffects.None, 1f);

                string displayText = UIHelpers.TruncateText(option.name, option.bounds.Width - 57);
                bool isTruncated = displayText != option.name;
                Vector2 textSize = Game1.smallFont.MeasureString(displayText);
                Vector2 textPos = new Vector2(
                    option.bounds.X + 12 + SaveSetLocalOnlyCheckboxSize + 8,
                    option.bounds.Y + (option.bounds.Height - textSize.Y) / 2
                );
                Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPos, Game1.textColor);

                if (isHovered && isTruncated)
                    hoveredTruncatedText = option.name;
            }

            int maxVisible = Math.Min(WardrobeDropdownMaxVisible, TagsOptions.Count);
            bool canScrollUp = tagsFirstVisible > 0;
            bool canScrollDown = tagsFirstVisible + maxVisible < TagsOptions.Count;

            float tagsArrowScale = 1.5f;
            int tagsArrowWidth = (int)(11 * tagsArrowScale);
            int tagsArrowHeight = (int)(12 * tagsArrowScale);

            if (canScrollUp)
            {
                Rectangle upArrow = new Rectangle(421, 459, 11, 12);
                Vector2 upPos = new Vector2(TagsDropdown.bounds.Right - tagsArrowWidth - 8, TagsDropdown.bounds.Bottom + 8);
                b.Draw(Game1.mouseCursors, upPos, upArrow, Color.White, 0f, Vector2.Zero, tagsArrowScale, SpriteEffects.None, 1f);
            }

            if (canScrollDown)
            {
                Rectangle downArrow = new Rectangle(421, 472, 11, 12);
                Vector2 downPos = new Vector2(TagsDropdown.bounds.Right - tagsArrowWidth - 8, TagsDropdown.bounds.Bottom + dropdownHeight - tagsArrowHeight - 8);
                b.Draw(Game1.mouseCursors, downPos, downArrow, Color.White, 0f, Vector2.Zero, tagsArrowScale, SpriteEffects.None, 1f);
            }

            return hoveredTruncatedText;
        }

        public void DrawFilterDropdown(SpriteBatch b, SetFilterState filter)
        {
            if (FilterOptions.Count == 0)
                return;

            int optionHeight = FilterOptions[0].bounds.Height;
            int dropdownHeight = FilterOptions.Count * optionHeight;

            UIHelpers.DrawTextureBoxNoShadow(b,
                FilterDropdown.bounds.X - 4,
                FilterDropdown.bounds.Bottom - 4,
                FilterDropdown.bounds.Width + 8,
                dropdownHeight + 8,
                Color.White);

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            bool[] checked_ = { filter.FavoritesOnly, filter.InvalidOnly, filter.ShowGlobal, filter.ShowLocal };

            for (int i = 0; i < FilterOptions.Count; i++)
            {
                var option = FilterOptions[i];
                bool isChecked = checked_[i];
                bool isHovered = option.containsPoint(mouseX, mouseY);

                if (isHovered)
                    b.Draw(Game1.staminaRect, option.bounds, Color.Wheat * 0.6f);

                Rectangle checkboxSource = isChecked
                    ? new Rectangle(236, 425, 9, 9)
                    : new Rectangle(227, 425, 9, 9);

                b.Draw(Game1.mouseCursors,
                    new Vector2(option.bounds.X + 12, option.bounds.Y + (option.bounds.Height - SaveSetLocalOnlyCheckboxSize) / 2),
                    checkboxSource, Color.White, 0f, Vector2.Zero, SaveSetLocalOnlyCheckboxScale, SpriteEffects.None, 1f);

                Vector2 textSize = Game1.smallFont.MeasureString(option.name);
                Vector2 textPos = new Vector2(
                    option.bounds.X + 12 + SaveSetLocalOnlyCheckboxSize + 8,
                    option.bounds.Y + (option.bounds.Height - textSize.Y) / 2
                );
                Utility.drawTextWithShadow(b, option.name, Game1.smallFont, textPos, Game1.textColor);
            }
        }

        private string FormatTagsWithCount(List<string> tags, string prefix, int maxWidth)
        {
            if (tags.Count == 0)
                return prefix + TranslationCache.WardrobeItemNone;

            string fullText = prefix + string.Join(", ", tags);
            if (Game1.smallFont.MeasureString(fullText).X <= maxWidth)
                return fullText;

            for (int shown = tags.Count - 1; shown >= 1; shown--)
            {
                int remaining = tags.Count - shown;
                string partial = prefix;
                for (int i = 0; i < shown; i++)
                {
                    if (i > 0) partial += ", ";
                    partial += tags[i];
                }
                partial += $" (+{remaining})";

                if (Game1.smallFont.MeasureString(partial).X <= maxWidth)
                    return partial;
            }

            return prefix + $"(+{tags.Count})";
        }

        public void DrawPreviewPanel(SpriteBatch b, OutfitSet? set, OutfitSetStore store, Texture2D? previewTexture, int displayedCount)
        {
            b.Draw(Game1.daybg, PreviewBox, Color.White);

            if (previewTexture != null)
            {
                b.Draw(previewTexture, PreviewBox, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
            }

            if (set != null)
            {
                DrawItemSlot(b, HatSlot, set.HatId, "(H)", store);
                DrawItemSlot(b, ShirtSlot, set.ShirtId, "(S)", store);
                DrawItemSlot(b, PantsSlot, set.PantsId, "(P)", store);
            }
            else
            {
                UIHelpers.DrawTextureBoxNoShadow(b, HatSlot.X - 4, HatSlot.Y - 4, HatSlot.Width + 8, HatSlot.Height + 8, Color.White);
                UIHelpers.DrawTextureBoxNoShadow(b, ShirtSlot.X - 4, ShirtSlot.Y - 4, ShirtSlot.Width + 8, ShirtSlot.Height + 8, Color.White);
                UIHelpers.DrawTextureBoxNoShadow(b, PantsSlot.X - 4, PantsSlot.Y - 4, PantsSlot.Width + 8, PantsSlot.Height + 8, Color.White);
            }

            if (set == null)
            {
                if (displayedCount > 0)
                {
                    int itemSlotsHeight = (SaveSetItemSlotSize * 3) + (SaveSetItemSlotGap * 2);
                    int wardrobeArrowH = (int)(ArrowNativeHeight * WardrobeArrowScale);
                    int previewSectionHeight = Math.Max(SaveSetPreviewHeight + ElementGap + wardrobeArrowH, itemSlotsHeight);
                    int detailsHeight = 32 * 2;
                    int totalPreviewContentHeight = previewSectionHeight + ContentBoxPadding + detailsHeight;
                    int previewStartY = LeftPanel.Y + (LeftPanel.Height - totalPreviewContentHeight) / 2;
                    int previewSectionBottom = previewStartY + previewSectionHeight;

                    int maxTextWidth = LeftPanel.Width - 16;
                    string wrappedText = Game1.parseText(TranslationCache.WardrobeSelectToView, Game1.smallFont, maxTextWidth);
                    Vector2 textSize = Game1.smallFont.MeasureString(wrappedText);

                    int availableHeight = LeftPanel.Bottom - previewSectionBottom;
                    float textX = LeftPanel.X + (LeftPanel.Width - textSize.X) / 2;
                    float textY = previewSectionBottom + (availableHeight - textSize.Y) / 2;

                    Utility.drawTextWithShadow(b, wrappedText, Game1.smallFont,
                        new Vector2(textX, textY), Color.Gray);
                }
                TagsTextTruncated = false;
                return;
            }

            int itemSlotsHeight2 = (SaveSetItemSlotSize * 3) + (SaveSetItemSlotGap * 2);
            int previewSectionHeight2 = Math.Max(SaveSetPreviewHeight, itemSlotsHeight2);
            int detailsHeight2 = 32 * 2;
            int totalPreviewContentHeight2 = previewSectionHeight2 + ContentBoxPadding + detailsHeight2;
            int previewStartY2 = LeftPanel.Y + (LeftPanel.Height - totalPreviewContentHeight2) / 2;
            int previewSectionBottom2 = previewStartY2 + previewSectionHeight2;

            int textLeftX = LeftPanelBox.X + 40;
            int maxInfoWidth = LeftPanelBox.Right - textLeftX - 20;
            float lineHeight = Game1.smallFont.MeasureString("A").Y;
            int lineSpacing = 32;
            int totalInfoHeight = lineSpacing + (int)lineHeight;
            int availableInfoHeight = LeftPanel.Bottom - previewSectionBottom2;
            int infoStartY = previewSectionBottom2 + (availableInfoHeight - totalInfoHeight) / 2;

            string tagsPrefix = TranslationCache.WardrobeTags + " ";
            string displayedTags = FormatTagsWithCount(set.Tags, tagsPrefix, maxInfoWidth);
            string fullTagsText = set.Tags.Count > 0
                ? tagsPrefix + string.Join(", ", set.Tags)
                : tagsPrefix + TranslationCache.WardrobeItemNone;
            TagsTextTruncated = displayedTags != fullTagsText;
            TagsTextBounds = new Rectangle(textLeftX, infoStartY, maxInfoWidth, (int)lineHeight);
            Utility.drawTextWithShadow(b, displayedTags, Game1.smallFont,
                new Vector2(textLeftX, infoStartY), Game1.textColor);

            string scopeText = set.IsGlobal ? TranslationCache.WardrobeScopeGlobal : TranslationCache.WardrobeScopeLocal;
            string scopeDisplay = $"Scope: {scopeText}";
            Utility.drawTextWithShadow(b, scopeDisplay, Game1.smallFont,
                new Vector2(textLeftX, infoStartY + lineSpacing), Color.Gray);
        }

        private void DrawItemSlot(SpriteBatch b, Rectangle slot, string? itemId, string typePrefix, OutfitSetStore store)
        {
            UIHelpers.DrawTextureBoxNoShadow(b, slot.X - 4, slot.Y - 4, slot.Width + 8, slot.Height + 8, Color.White);

            bool hasItem = !string.IsNullOrEmpty(itemId);
            bool isValid = hasItem && store.IsItemValid(itemId!, typePrefix);

            if (!hasItem || !isValid)
            {
                b.Draw(Game1.staminaRect, slot, SaveSetExcludedItemSlotColor);
            }
        }

        public void DrawButtons(SpriteBatch b, bool hasSelection)
        {
            UIHelpers.DrawTextButton(b, ApplyCloseButton, TranslationCache.WardrobeButtonApplyClose);

            if (hasSelection)
            {
                UIHelpers.DrawTextButton(b, EditButton, TranslationCache.WardrobeButtonEdit);
                UIHelpers.DrawTextButton(b, DeleteButton, TranslationCache.WardrobeButtonDelete);
            }
            else
            {
                DrawInactiveButton(b, EditButton, TranslationCache.WardrobeButtonEdit);
                DrawInactiveButton(b, DeleteButton, TranslationCache.WardrobeButtonDelete);
            }
        }

        private void DrawInactiveButton(SpriteBatch b, ClickableComponent button, string label)
        {
            UIHelpers.DrawTextureBox(b, button.bounds.X, button.bounds.Y,
                button.bounds.Width, button.bounds.Height, Color.White * 0.8f);

            Vector2 textSize = Game1.smallFont.MeasureString(label);
            Vector2 textPos = UIHelpers.GetVisualCenter(button.bounds, textSize);
            Utility.drawTextWithShadow(b, label, Game1.smallFont, textPos, Game1.textColor * TabOpacity);
        }

        public void DrawCloseButton(SpriteBatch b)
        {
            UIHelpers.DrawTextureButton(b, CloseButton);
        }

        public void DrawArrows(SpriteBatch b)
        {
            UIHelpers.DrawTextureButton(b, LeftArrowButton);
            UIHelpers.DrawTextureButton(b, RightArrowButton);
        }
    }
}
