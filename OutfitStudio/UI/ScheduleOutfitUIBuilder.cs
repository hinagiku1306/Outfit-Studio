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
    public class ScheduleOutfitUIBuilder
    {
        public Rectangle ContentAreaBox { get; private set; }
        public Rectangle LeftPanel { get; private set; }
        public Rectangle RightPanel { get; private set; }

        public ClickableComponent SearchScopeDropdown { get; private set; } = null!;
        public ClickableComponent SearchBar { get; private set; } = null!;
        public ClickableComponent TagsDropdown { get; private set; } = null!;
        public ClickableComponent FilterDropdown { get; private set; } = null!;
        public ClickableComponent ShowSelectedCheckbox { get; private set; } = null!;
        public ClickableComponent MatchAllCheckbox { get; private set; } = null!;
        public ClickableComponent ShowInvalidCheckbox { get; private set; } = null!;
        public ClickableComponent TagsClearButton { get; private set; } = null!;
        public ClickableComponent FilterClearButton { get; private set; } = null!;
        public ClickableComponent SearchClearButton { get; private set; } = null!;

        public List<ClickableComponent> SearchScopeOptions { get; private set; } = new();
        public List<ClickableComponent> TagsOptions { get; private set; } = new();
        public List<ClickableComponent> FilterOptions { get; private set; } = new();

        public List<ClickableComponent> OutfitListItems { get; private set; } = new();
        public List<ClickableComponent> ExcludeButtons { get; private set; } = new();
        public Rectangle PreviewBox { get; private set; }
        public ClickableComponent CloseMenuButton { get; private set; } = null!;
        public ClickableComponent SelectAllButton { get; private set; } = null!;
        public ClickableTextureComponent CloseButton { get; private set; } = null!;
        public ClickableTextureComponent LeftArrowButton { get; private set; } = null!;
        public ClickableTextureComponent RightArrowButton { get; private set; } = null!;

        public string? HoveredTruncatedSetName { get; private set; }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        private int tagsFirstVisible;
        public int TagsFirstVisible => tagsFirstVisible;

        private int searchScopeWidth;
        private int outfitListPanelWidth;
        private int previewPanelWidth;
        private int headerY;
        private float headerHeight;
        private int checkboxDividerY;

        private const int BackgroundPaddingTop = 40;
        private const int BackgroundPaddingLeft = 30;
        private const int BackgroundPaddingRight = 30;
        private const int BackgroundPaddingBottom = 20;
        private const int SectionGap = 10;
        private const int ContentAreaPadding = 20;
        private const int FilterBarIndent = 10;
        private const int CheckboxExtraGap = 14;
        private const int CheckboxDividerGap = 8;
        private const int DividerHeight = 2;
        private const int DividerToContentGap = 0;

        public ScheduleOutfitUIBuilder()
        {
            Recalculate();
        }

        private int CalculateOutfitListPanelWidth()
        {
            int dropdownPadding = 40;

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

            int row2Width = TabAndButtonWidth * 2 + WardrobeFilterGap;

            return Math.Max(row1Width, row2Width) + ContentBoxPadding + 150;
        }

        private int CalculatePreviewPanelWidth()
        {
            return SaveSetPreviewWidth + ContentBoxPadding * 2 + 30;
        }

        public void Recalculate()
        {
            headerHeight = Game1.smallFont.MeasureString("A").Y;
            outfitListPanelWidth = CalculateOutfitListPanelWidth();
            previewPanelWidth = CalculatePreviewPanelWidth();

            int contentWidth = previewPanelWidth + SectionGap + outfitListPanelWidth;
            Width = BackgroundPaddingLeft + contentWidth + BackgroundPaddingRight;

            int filterBarHeight = TabAndButtonHeight * 2 + WardrobeFilterGap;
            int arrowHeight = (int)(ArrowNativeHeight * WardrobeArrowScale);
            int previewNeedHeight = SaveSetPreviewHeight + ElementGap + arrowHeight;
            int minListHeight = WardrobeListItemHeight * 6;
            int contentAreaInnerHeight = Math.Max(previewNeedHeight, minListHeight);
            int contentAreaBoxHeight = ContentAreaPadding * 2 + contentAreaInnerHeight;

            Height = BackgroundPaddingTop + (int)headerHeight + ElementGap
                + filterBarHeight + CheckboxExtraGap
                + WardrobeCheckboxSize + CheckboxDividerGap + DividerHeight + DividerToContentGap
                + contentAreaBoxHeight
                + SectionGap + WardrobeBottomButtonHeight
                + BackgroundPaddingBottom;

            X = (Game1.uiViewport.Width - Width) / 2;
            Y = (Game1.uiViewport.Height - Height) / 2;

            CalculateLayout();
        }

        private void CalculateLayout()
        {
            int contentX = X + BackgroundPaddingLeft;
            int contentWidth = Width - BackgroundPaddingLeft - BackgroundPaddingRight;
            int currentY = Y + BackgroundPaddingTop;

            headerY = currentY;
            currentY += (int)headerHeight + ElementGap;

            int filterX = contentX + FilterBarIndent;
            int filterWidth = contentWidth - FilterBarIndent * 2;
            CalculateFilterBar(filterX, currentY, filterWidth);
            currentY += TabAndButtonHeight * 2 + WardrobeFilterGap + CheckboxExtraGap;

            CalculateCheckboxes(filterX, currentY);
            currentY += WardrobeCheckboxSize + CheckboxDividerGap;
            checkboxDividerY = currentY;
            currentY += DividerHeight + DividerToContentGap;

            int arrowHeight = (int)(ArrowNativeHeight * WardrobeArrowScale);
            int previewNeedHeight = SaveSetPreviewHeight + ElementGap + arrowHeight;
            int minListHeight = WardrobeListItemHeight * 6;
            int contentAreaInnerHeight = Math.Max(previewNeedHeight, minListHeight);

            ContentAreaBox = new Rectangle(contentX, currentY, contentWidth, contentAreaInnerHeight + ContentAreaPadding * 2);

            LeftPanel = new Rectangle(
                ContentAreaBox.X + ContentAreaPadding,
                ContentAreaBox.Y + ContentAreaPadding,
                previewPanelWidth,
                contentAreaInnerHeight
            );

            int listX = LeftPanel.Right + SectionGap + 6;
            int listWidth = ContentAreaBox.Right - ContentAreaPadding - listX;
            RightPanel = new Rectangle(listX, ContentAreaBox.Y + ContentAreaPadding, listWidth, contentAreaInnerHeight);

            CalculateListItems();
            CalculatePreviewAndButtons();
        }

        private void CalculateFilterBar(int x, int y, int width)
        {
            int row1Y = y;
            int row2Y = y + TabAndButtonHeight + WardrobeFilterGap;

            SearchScopeDropdown = new ClickableComponent(
                new Rectangle(x, row1Y, searchScopeWidth, TabAndButtonHeight),
                "searchScope"
            );

            int searchBarX = SearchScopeDropdown.bounds.Right + WardrobeFilterGap;
            SearchBar = new ClickableComponent(
                new Rectangle(searchBarX, row1Y, x + width - searchBarX, TabAndButtonHeight),
                "searchBar"
            );

            int tagsWidth = (width - WardrobeFilterGap) / 2;
            int filterWidth = width - WardrobeFilterGap - tagsWidth;

            TagsDropdown = new ClickableComponent(
                new Rectangle(x, row2Y, tagsWidth, TabAndButtonHeight),
                "tagsDropdown"
            );

            FilterDropdown = new ClickableComponent(
                new Rectangle(TagsDropdown.bounds.Right + WardrobeFilterGap, row2Y, filterWidth, TabAndButtonHeight),
                "filterDropdown"
            );

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
        }

        private void CalculateCheckboxes(int contentX, int y)
        {
            int checkboxLabelGap = 10;
            int checkboxGap = 24;

            ShowSelectedCheckbox = new ClickableComponent(
                new Rectangle(contentX, y, WardrobeCheckboxSize, WardrobeCheckboxSize),
                "showSelected"
            );

            float showSelectedLabelWidth = Game1.smallFont.MeasureString(TranslationCache.ScheduleOutfitShowSelected).X;
            int matchAllX = ShowSelectedCheckbox.bounds.Right + (int)showSelectedLabelWidth + checkboxLabelGap + checkboxGap;

            MatchAllCheckbox = new ClickableComponent(
                new Rectangle(matchAllX, y, WardrobeCheckboxSize, WardrobeCheckboxSize),
                "matchAll"
            );

            float matchAllLabelWidth = Game1.smallFont.MeasureString(TranslationCache.WardrobeFilterMatchAll).X;
            int showInvalidX = MatchAllCheckbox.bounds.Right + (int)matchAllLabelWidth + checkboxLabelGap + checkboxGap;

            ShowInvalidCheckbox = new ClickableComponent(
                new Rectangle(showInvalidX, y, WardrobeCheckboxSize, WardrobeCheckboxSize),
                "showInvalid"
            );
        }

        private void CalculateListItems()
        {
            int maxItems = RightPanel.Height / WardrobeListItemHeight;

            OutfitListItems.Clear();
            ExcludeButtons.Clear();
            for (int i = 0; i < maxItems; i++)
            {
                int rowY = RightPanel.Y + (i * WardrobeListItemHeight);
                OutfitListItems.Add(new ClickableComponent(
                    new Rectangle(RightPanel.X, rowY, RightPanel.Width, WardrobeListItemHeight),
                    $"outfit_{i}"
                ));

                int btnY = rowY + (WardrobeListItemHeight - ClearButtonSize) / 2;
                ExcludeButtons.Add(new ClickableComponent(
                    new Rectangle(RightPanel.X + RightPanel.Width - ClearButtonSize - 8, btnY,
                        ClearButtonSize, ClearButtonSize),
                    "exclude_" + i
                ));
            }
        }

        private void CalculatePreviewAndButtons()
        {
            int arrowWidth = (int)(ArrowNativeWidth * WardrobeArrowScale);
            int arrowHeight = (int)(ArrowNativeHeight * WardrobeArrowScale);
            int totalPreviewHeight = SaveSetPreviewHeight + ElementGap + arrowHeight;

            int previewX = LeftPanel.X + (LeftPanel.Width - SaveSetPreviewWidth) / 2;
            int previewStartY = LeftPanel.Y + (LeftPanel.Height - totalPreviewHeight) / 2;

            PreviewBox = new Rectangle(previewX, previewStartY, SaveSetPreviewWidth, SaveSetPreviewHeight);

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

            int buttonY = ContentAreaBox.Bottom + SectionGap;
            int selectBtnWidth = UIHelpers.CalculateButtonWidth(TranslationCache.CommonSelectAll, Width / 2);
            int closeWidth = UIHelpers.CalculateButtonWidth(TranslationCache.CommonClose, Width / 2);
            int buttonGap = 16;
            int totalButtonsWidth = selectBtnWidth + buttonGap + closeWidth;
            int buttonsStartX = X + (Width - totalButtonsWidth) / 2;

            SelectAllButton = new ClickableComponent(
                new Rectangle(buttonsStartX, buttonY, selectBtnWidth, TabAndButtonHeight), "selectAll");
            CloseMenuButton = new ClickableComponent(
                new Rectangle(buttonsStartX + selectBtnWidth + buttonGap, buttonY, closeWidth, TabAndButtonHeight), "close");

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

        public void ResetTagsScroll() => tagsFirstVisible = 0;

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

        public void DrawCheckboxDivider(SpriteBatch b)
        {
            int contentX = X + BackgroundPaddingLeft + FilterBarIndent;
            int contentWidth = Width - BackgroundPaddingLeft - BackgroundPaddingRight - FilterBarIndent * 2;
            b.Draw(Game1.staminaRect, new Rectangle(contentX, checkboxDividerY, contentWidth, DividerHeight), Color.Gray * 0.3f);
        }

        public void DrawContentDivider(SpriteBatch b)
        {
            int dividerX = LeftPanel.Right + SectionGap / 2;
            b.Draw(Game1.staminaRect, new Rectangle(dividerX, ContentAreaBox.Y + ContentAreaPadding, 2, ContentAreaBox.Height - ContentAreaPadding * 2), Color.Gray * 0.3f);
        }

        public void DrawHeader(SpriteBatch b, int activeCount, int totalCount, int? remainingCount = null)
        {
            string headerText = $"{TranslationCache.ScheduleOutfitActiveSets} {activeCount} / {totalCount}";
            if (remainingCount.HasValue)
                headerText += $"   \u00b7   {remainingCount.Value} {TranslationCache.ScheduleEditRemainingBeforeReset}";
            Utility.drawTextWithShadow(b, headerText, Game1.smallFont,
                new Vector2(X + BackgroundPaddingLeft + FilterBarIndent, headerY), Game1.textColor);
        }

        public void DrawOutfitSetList(SpriteBatch b, List<OutfitSet> sets, int selectedIndex, int scrollOffset,
            SetFilterState filter, bool searchScopeOpen, bool tagsOpen, bool filterOpen,
            string searchText, List<string> allTags, bool searchFocused,
            HashSet<string> selectedSetIds, bool showSelected,
            HashSet<string>? remainingSetIds = null)
        {
            DrawFilterBar(b, filter, searchScopeOpen, tagsOpen, filterOpen, searchText, allTags, searchFocused);
            DrawCheckboxRow(b, filter, showSelected);

            float stardropScale = 2.8f;
            int stardropSize = (int)(8 * stardropScale);
            float warningScale = 3f;
            int warningWidth = (int)(3 * warningScale);
            int warningHeight = (int)(8 * warningScale);
            Rectangle warningSourceRect = new Rectangle(395, 497, 3, 8);
            int iconGap = 10;
            int leftMargin = 8;
            int maxIconAreaWidth = stardropSize + iconGap + warningWidth + iconGap;
            int maxTextWidth = RightPanel.Width - leftMargin - maxIconAreaWidth - ClearButtonSize - 8 - 8;

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
                bool isSelected = selectedSetIds.Contains(set.Id);
                bool isHovered = !UIHelpers.SuppressHover && !anyDropdownOpen && item.bounds.Contains(mouseX, mouseY);
                float opacity = isSelected ? 1f : MarkedForDeletionOpacity;

                if (dataIndex == selectedIndex)
                    b.Draw(Game1.staminaRect, item.bounds, Color.Wheat);
                else if (isHovered)
                    b.Draw(Game1.staminaRect, item.bounds, Color.Wheat * 0.6f);

                int iconX = item.bounds.X + leftMargin;
                bool hasAnyIcon = false;

                if (set.IsFavorite)
                {
                    int iconY = item.bounds.Y + (WardrobeListItemHeight - stardropSize) / 2;
                    b.Draw(Game1.mouseCursors, new Vector2(iconX, iconY),
                        new Rectangle(346, 392, 8, 8),
                        Color.White * opacity, 0f, Vector2.Zero, stardropScale, SpriteEffects.None, 1f);
                    iconX += stardropSize;
                    hasAnyIcon = true;
                }

                if (!set.IsValid)
                {
                    if (hasAnyIcon) iconX += iconGap;
                    int iconY = item.bounds.Y + (WardrobeListItemHeight - warningHeight) / 2;
                    b.Draw(Game1.mouseCursors, new Vector2(iconX, iconY),
                        warningSourceRect, Color.White * opacity, 0f, Vector2.Zero, warningScale, SpriteEffects.None, 1f);
                    iconX += warningWidth;
                    hasAnyIcon = true;
                }

                int textX = hasAnyIcon ? iconX + iconGap : item.bounds.X + leftMargin;
                string truncatedText = UIHelpers.TruncateText(set.Name, maxTextWidth);
                bool isRemaining = remainingSetIds != null && remainingSetIds.Contains(set.Id);
                Color textColor = isRemaining ? Color.DarkOrange : Game1.textColor;
                Utility.drawTextWithShadow(b, truncatedText, Game1.smallFont,
                    new Vector2(textX, item.bounds.Y + 12), textColor * opacity);

                if (i < ExcludeButtons.Count)
                    DrawExcludeButton(b, ExcludeButtons[i], !isSelected);

                if (isHovered && truncatedText != set.Name)
                    HoveredTruncatedSetName = set.Name;
            }

            if (sets.Count == 0)
            {
                string emptyMessage = filter.HasActiveFilters
                    ? TranslationCache.WardrobeFilterNoResults
                    : TranslationCache.WardrobeNoSets;
                Vector2 textSize = Game1.smallFont.MeasureString(emptyMessage);
                Utility.drawTextWithShadow(b, emptyMessage, Game1.smallFont,
                    new Vector2(RightPanel.X + (RightPanel.Width - textSize.X) / 2,
                        RightPanel.Y + (RightPanel.Height - textSize.Y) / 2), Color.Gray);
            }

            DrawListScrollIndicators(b, sets.Count, scrollOffset);
        }

        private void DrawListScrollIndicators(SpriteBatch b, int totalItems, int scrollOffset)
        {
            int maxVisible = OutfitListItems.Count;
            if (totalItems <= maxVisible)
                return;

            bool canScrollUp = scrollOffset > 0;
            bool canScrollDown = scrollOffset + maxVisible < totalItems;

            float arrowScale = 2.0f;
            int arrowWidth = (int)(11 * arrowScale);
            int arrowHeight = (int)(12 * arrowScale);

            int excludeRight = RightPanel.Right - 8;
            int menuRight = X + Width;
            int arrowX = excludeRight + (menuRight - excludeRight - arrowWidth) / 2 - 6;

            if (canScrollUp)
            {
                Vector2 upPos = new Vector2(arrowX, RightPanel.Y + 4);
                b.Draw(Game1.mouseCursors, upPos, UIHelpers.UpScrollArrowSourceRect, Color.White, 0f, Vector2.Zero, arrowScale, SpriteEffects.None, 1f);
            }

            if (canScrollDown)
            {
                Vector2 downPos = new Vector2(arrowX, RightPanel.Bottom - arrowHeight - 4);
                b.Draw(Game1.mouseCursors, downPos, UIHelpers.DownScrollArrowSourceRect, Color.White, 0f, Vector2.Zero, arrowScale, SpriteEffects.None, 1f);
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

            UIHelpers.DrawInputBar(b, SearchBar.bounds, searchText, searchFocused,
                placeholder: TranslationCache.WardrobeSearch,
                clearButton: !string.IsNullOrEmpty(searchText) ? SearchClearButton : null);

            bool hasTags = filter.SelectedTags.Count > 0;
            string tagsLabel = hasTags
                ? $"{TranslationCache.WardrobeFilterTags} ({filter.SelectedTags.Count})"
                : TranslationCache.WardrobeFilterTags;
            UIHelpers.DrawDropdownButton(b, TagsDropdown.bounds, tagsLabel, tagsOpen,
                clearButton: TagsClearButton, hasValue: hasTags);

            bool hasFilters = filter.FavoritesOnly || !filter.ShowGlobal || !filter.ShowLocal || filter.InvalidOnly;
            string filterLabel = TranslationCache.WardrobeFilterFilter;
            UIHelpers.DrawDropdownButton(b, FilterDropdown.bounds, filterLabel, filterOpen,
                clearButton: FilterClearButton, hasValue: hasFilters);
        }

        private void DrawCheckboxRow(SpriteBatch b, SetFilterState filter, bool showSelected)
        {
            DrawCheckbox(b, ShowSelectedCheckbox, TranslationCache.ScheduleOutfitShowSelected, showSelected);
            DrawCheckbox(b, MatchAllCheckbox, TranslationCache.WardrobeFilterMatchAll, filter.MatchAllTags);
            DrawCheckbox(b, ShowInvalidCheckbox, TranslationCache.WardrobeFilterShowInvalid, filter.ShowInvalid);
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
            bool isHovered = !UIHelpers.SuppressHover && hitArea.Contains(mouseX, mouseY);

            Rectangle sourceRect = isChecked
                ? UIHelpers.CheckedSourceRect
                : UIHelpers.UncheckedSourceRect;

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

        public void DrawPreviewPanel(SpriteBatch b, OutfitSet? set, Texture2D? previewTexture)
        {
            b.Draw(Game1.daybg, PreviewBox, Color.White);

            if (previewTexture != null)
            {
                b.Draw(previewTexture, PreviewBox, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
            }
        }

        private void DrawExcludeButton(SpriteBatch b, ClickableComponent button, bool isExcluded)
        {
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            bool isHovered = button.containsPoint(mouseX, mouseY);

            Rectangle sourceRect = new Rectangle(337, 494, 12, 12);
            float scale = isHovered ? 2.2f : 2f;
            float opacity = isExcluded ? MarkedForDeletionOpacity : 1f;
            Vector2 center = new Vector2(
                button.bounds.X + button.bounds.Width / 2 - 2,
                button.bounds.Y + button.bounds.Height / 2
            );
            Vector2 origin = new Vector2(6, 6);

            b.Draw(
                Game1.mouseCursors,
                center,
                sourceRect,
                Color.White * opacity,
                0f,
                origin,
                scale,
                SpriteEffects.None,
                1f
            );
        }

        public void DrawButtons(SpriteBatch b)
        {
            UIHelpers.DrawTextButton(b, SelectAllButton, TranslationCache.CommonSelectAll);
            UIHelpers.DrawTextButton(b, CloseMenuButton, TranslationCache.CommonClose);
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
