using System;
using System.Collections.Generic;
using FittingRoom.Managers;
using FittingRoom.Models;
using FittingRoom.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using static FittingRoom.OutfitLayoutConstants;

namespace FittingRoom
{
    public class TemplatesUIBuilder
    {
        public Rectangle LeftPanel { get; private set; }
        public Rectangle RightPanel { get; private set; }

        public ClickableComponent SearchScopeDropdown { get; private set; } = null!;
        public ClickableComponent SearchBar { get; private set; } = null!;
        public ClickableComponent TagsDropdown { get; private set; } = null!;
        public ClickableComponent FilterDropdown { get; private set; } = null!;
        public ClickableComponent MatchAllCheckbox { get; private set; } = null!;
        public ClickableComponent ShowInvalidCheckbox { get; private set; } = null!;

        public List<ClickableComponent> SearchScopeOptions { get; private set; } = new();
        public List<ClickableComponent> TagsOptions { get; private set; } = new();
        public List<ClickableComponent> FilterOptions { get; private set; } = new();

        public List<ClickableComponent> TemplateListItems { get; private set; } = new();
        public Rectangle PreviewBox { get; private set; }

        public ClickableComponent LoadButton { get; private set; } = null!;
        public ClickableComponent ApplyCloseButton { get; private set; } = null!;
        public ClickableComponent NewButton { get; private set; } = null!;
        public ClickableComponent RenameButton { get; private set; } = null!;
        public ClickableComponent DeleteButton { get; private set; } = null!;
        public ClickableComponent FavoriteButton { get; private set; } = null!;
        public ClickableTextureComponent CloseButton { get; private set; } = null!;

        public ClickableComponent? RenameInputArea { get; private set; }
        public ClickableComponent? RenameConfirmButton { get; private set; }
        public ClickableComponent? RenameCancelButton { get; private set; }

        private bool isRenameMode;

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        private int tagsFirstVisible;

        public TemplatesUIBuilder(int width, int height)
        {
            Width = width;
            Height = height;

            Recalculate();
        }

        public void Recalculate()
        {
            X = (Game1.uiViewport.Width - Width) / 2;
            Y = (Game1.uiViewport.Height - Height) / 2;

            CalculateLayout();
        }

        private void CalculateLayout()
        {
            int contentY = Y + ContentBoxPadding + TitleAreaHeight;
            int panelHeight = Height - TitleAreaHeight - TemplatesBottomButtonHeight - (ContentBoxPadding * 3);

            LeftPanel = new Rectangle(
                X + ContentBoxPadding,
                contentY,
                TemplatesLeftPanelWidth,
                panelHeight
            );

            RightPanel = new Rectangle(
                LeftPanel.Right + TemplatesPanelGap,
                contentY,
                TemplatesRightPanelWidth,
                panelHeight
            );

            CalculateFilterBar();
            CalculateListItems();
            CalculatePreviewAndButtons();
        }

        private void CalculateFilterBar()
        {
            int row1Y = LeftPanel.Y;
            int row2Y = row1Y + TemplatesFilterRowHeight + TemplatesFilterGap;
            int row3Y = row2Y + TemplatesFilterRowHeight + TemplatesFilterGap;

            SearchScopeDropdown = new ClickableComponent(
                new Rectangle(LeftPanel.X, row1Y, TemplatesSearchScopeWidth, TemplatesFilterRowHeight),
                "searchScope"
            );

            int searchBarX = SearchScopeDropdown.bounds.Right + TemplatesFilterGap;
            SearchBar = new ClickableComponent(
                new Rectangle(searchBarX, row1Y, LeftPanel.Right - searchBarX, TemplatesFilterRowHeight),
                "searchBar"
            );

            TagsDropdown = new ClickableComponent(
                new Rectangle(LeftPanel.X, row2Y, TemplatesTagsDropdownWidth, TemplatesFilterRowHeight),
                "tagsDropdown"
            );

            FilterDropdown = new ClickableComponent(
                new Rectangle(TagsDropdown.bounds.Right + TemplatesFilterGap, row2Y, TemplatesFilterDropdownWidth, TemplatesFilterRowHeight),
                "filterDropdown"
            );

            int checkboxLabelGap = 4;
            MatchAllCheckbox = new ClickableComponent(
                new Rectangle(LeftPanel.X, row3Y, TemplatesCheckboxSize, TemplatesCheckboxSize),
                "matchAll"
            );

            float matchAllLabelWidth = Game1.smallFont.MeasureString(TranslationCache.TemplatesFilterMatchAll).X;
            int showInvalidX = MatchAllCheckbox.bounds.Right + (int)matchAllLabelWidth + checkboxLabelGap + 24;

            ShowInvalidCheckbox = new ClickableComponent(
                new Rectangle(showInvalidX, row3Y, TemplatesCheckboxSize, TemplatesCheckboxSize),
                "showInvalid"
            );
        }

        private void CalculateListItems()
        {
            int filterBarHeight = (TemplatesFilterRowHeight * 3) + (TemplatesFilterGap * 2) + ContentBoxPadding;
            int listY = LeftPanel.Y + filterBarHeight;
            int listHeight = LeftPanel.Bottom - listY;
            int maxItems = listHeight / TemplatesListItemHeight;

            TemplateListItems.Clear();
            for (int i = 0; i < maxItems; i++)
            {
                TemplateListItems.Add(new ClickableComponent(
                    new Rectangle(LeftPanel.X, listY + (i * TemplatesListItemHeight),
                        LeftPanel.Width, TemplatesListItemHeight),
                    $"template_{i}"
                ));
            }
        }

        private void CalculatePreviewAndButtons()
        {
            int previewX = RightPanel.X + (RightPanel.Width - TemplatesPreviewSize) / 2;
            PreviewBox = new Rectangle(previewX, RightPanel.Y, TemplatesPreviewSize, TemplatesPreviewSize);

            int buttonY = Y + Height - TemplatesBottomButtonHeight + ContentBoxPadding;
            int buttonWidth = 120;

            LoadButton = new ClickableComponent(
                new Rectangle(X + ContentBoxPadding, buttonY, buttonWidth, TabAndButtonHeight),
                "load"
            );
            ApplyCloseButton = new ClickableComponent(
                new Rectangle(LoadButton.bounds.Right + BottomButtonGap, buttonY, 150, TabAndButtonHeight),
                "applyClose"
            );

            int rightButtonsX = X + Width - ContentBoxPadding - buttonWidth;
            DeleteButton = new ClickableComponent(
                new Rectangle(rightButtonsX, buttonY, buttonWidth, TabAndButtonHeight),
                "delete"
            );
            RenameButton = new ClickableComponent(
                new Rectangle(rightButtonsX - buttonWidth - BottomButtonGap, buttonY, buttonWidth, TabAndButtonHeight),
                "rename"
            );
            NewButton = new ClickableComponent(
                new Rectangle(RenameButton.bounds.X - buttonWidth - BottomButtonGap, buttonY, buttonWidth, TabAndButtonHeight),
                "new"
            );

            FavoriteButton = new ClickableComponent(
                new Rectangle(X + ContentBoxPadding, buttonY + TabAndButtonHeight + 8, 140, TabAndButtonHeight),
                "favorite"
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

            string[] labels = { TranslationCache.TemplatesFilterSearchSet, TranslationCache.TemplatesFilterSearchItem, TranslationCache.TemplatesFilterSearchAll };

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

            int maxVisible = Math.Min(TemplatesDropdownMaxVisible, allTags.Count);
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

            string[] labels = { TranslationCache.TemplatesFilterFavorites, TranslationCache.TemplatesFilterGlobal, TranslationCache.TemplatesFilterLocal };

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
            int maxVisible = Math.Min(TemplatesDropdownMaxVisible, TagsOptions.Count);
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

        public void DrawLeftPanel(SpriteBatch b, List<OutfitSet> sets, int selectedIndex, int scrollOffset, SetFilterState filter,
            bool searchScopeOpen, bool tagsOpen, bool filterOpen, string searchText, List<string> allTags)
        {
            UIHelpers.DrawTextureBox(b, LeftPanel.X - 8, LeftPanel.Y - 8,
                LeftPanel.Width + 16, LeftPanel.Height + 16, Color.White);

            DrawFilterBar(b, filter, searchScopeOpen, tagsOpen, filterOpen, searchText, allTags);

            int filterBarHeight = (TemplatesFilterRowHeight * 3) + (TemplatesFilterGap * 2) + ContentBoxPadding;
            int separatorY = LeftPanel.Y + filterBarHeight - ContentBoxPadding / 2;
            b.Draw(Game1.staminaRect, new Rectangle(LeftPanel.X, separatorY, LeftPanel.Width, 2), Color.Gray * 0.3f);

            int maxTextWidth = LeftPanel.Width - 24;

            for (int i = 0; i < TemplateListItems.Count; i++)
            {
                int dataIndex = scrollOffset + i;
                if (dataIndex >= sets.Count)
                    break;

                var item = TemplateListItems[i];
                var set = sets[dataIndex];
                Color bgColor = dataIndex == selectedIndex ? Color.LightBlue * 0.3f : Color.Transparent;

                if (bgColor != Color.Transparent)
                    b.Draw(Game1.staminaRect, item.bounds, bgColor);

                string displayText = BuildSetDisplayText(set);
                string truncatedText = UIHelpers.TruncateText(displayText, maxTextWidth);
                Utility.drawTextWithShadow(b, truncatedText, Game1.smallFont,
                    new Vector2(item.bounds.X + 8, item.bounds.Y + 12), Game1.textColor);
            }

            if (sets.Count == 0)
            {
                string emptyMessage = filter.HasActiveFilters
                    ? TranslationCache.TemplatesFilterNoResults
                    : TranslationCache.TemplatesNoSets;
                Vector2 textSize = Game1.smallFont.MeasureString(emptyMessage);
                int listStartY = LeftPanel.Y + filterBarHeight;
                int listHeight = LeftPanel.Bottom - listStartY;
                Utility.drawTextWithShadow(b, emptyMessage, Game1.smallFont,
                    new Vector2(LeftPanel.X + (LeftPanel.Width - textSize.X) / 2,
                        listStartY + listHeight / 2), Color.Gray);
            }

            DrawListScrollIndicators(b, sets.Count, scrollOffset);
        }

        private void DrawListScrollIndicators(SpriteBatch b, int totalItems, int scrollOffset)
        {
            int maxVisible = TemplateListItems.Count;
            if (totalItems <= maxVisible)
                return;

            int filterBarHeight = (TemplatesFilterRowHeight * 3) + (TemplatesFilterGap * 2) + ContentBoxPadding;
            int listStartY = LeftPanel.Y + filterBarHeight;

            bool canScrollUp = scrollOffset > 0;
            bool canScrollDown = scrollOffset + maxVisible < totalItems;

            if (canScrollUp)
            {
                Rectangle upArrow = new Rectangle(421, 459, 11, 12);
                Vector2 upPos = new Vector2(LeftPanel.Right - 20, listStartY + 4);
                b.Draw(Game1.mouseCursors, upPos, upArrow, Color.White, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 1f);
            }

            if (canScrollDown)
            {
                Rectangle downArrow = new Rectangle(421, 472, 11, 12);
                Vector2 downPos = new Vector2(LeftPanel.Right - 20, LeftPanel.Bottom - 22);
                b.Draw(Game1.mouseCursors, downPos, downArrow, Color.White, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 1f);
            }
        }

        private void DrawFilterBar(SpriteBatch b, SetFilterState filter, bool searchScopeOpen, bool tagsOpen, bool filterOpen,
            string searchText, List<string> allTags)
        {
            string scopeLabel = filter.SearchScope switch
            {
                SearchScope.Set => TranslationCache.TemplatesFilterSearchSet,
                SearchScope.Item => TranslationCache.TemplatesFilterSearchItem,
                SearchScope.All => TranslationCache.TemplatesFilterSearchAll,
                _ => TranslationCache.TemplatesFilterSearchSet
            };
            DrawDropdownButton(b, SearchScopeDropdown, scopeLabel + " ▾", searchScopeOpen);

            DrawSearchBar(b, SearchBar, searchText);

            string tagsLabel = filter.SelectedTags.Count > 0
                ? $"{TranslationCache.TemplatesFilterTags} ({filter.SelectedTags.Count})"
                : TranslationCache.TemplatesFilterTags;
            DrawDropdownButton(b, TagsDropdown, tagsLabel + " ▾", tagsOpen);

            int activeFilters = 0;
            if (filter.FavoritesOnly) activeFilters++;
            if (!filter.ShowGlobal || !filter.ShowLocal) activeFilters++;

            string filterLabel = activeFilters > 0
                ? $"{TranslationCache.TemplatesFilterFilter} ({activeFilters})"
                : TranslationCache.TemplatesFilterFilter;
            DrawDropdownButton(b, FilterDropdown, filterLabel + " ▾", filterOpen);

            DrawCheckbox(b, MatchAllCheckbox, TranslationCache.TemplatesFilterMatchAll, filter.MatchAllTags);
            DrawCheckbox(b, ShowInvalidCheckbox, TranslationCache.TemplatesFilterShowInvalid, filter.ShowInvalid);
        }

        private void DrawDropdownButton(SpriteBatch b, ClickableComponent button, string label, bool isOpen)
        {
            UIHelpers.DrawTextureBox(b, button.bounds.X, button.bounds.Y,
                button.bounds.Width, button.bounds.Height, isOpen ? Color.Wheat : Color.White);

            Vector2 textSize = Game1.smallFont.MeasureString(label);
            Vector2 textPos = new Vector2(
                button.bounds.X + 8,
                button.bounds.Y + (button.bounds.Height - textSize.Y) / 2
            );
            Utility.drawTextWithShadow(b, label, Game1.smallFont, textPos, Game1.textColor);
        }

        private void DrawSearchBar(SpriteBatch b, ClickableComponent bar, string text)
        {
            UIHelpers.DrawTextureBox(b, bar.bounds.X, bar.bounds.Y,
                bar.bounds.Width, bar.bounds.Height, Color.White);

            string displayText = string.IsNullOrEmpty(text) ? TranslationCache.TemplatesSearch : text;
            Color textColor = string.IsNullOrEmpty(text) ? Color.Gray : Game1.textColor;

            Vector2 textSize = Game1.smallFont.MeasureString(displayText);
            Vector2 textPos = new Vector2(
                bar.bounds.X + 12,
                bar.bounds.Y + (bar.bounds.Height - textSize.Y) / 2
            );
            Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPos, textColor);
        }

        private void DrawCheckbox(SpriteBatch b, ClickableComponent checkbox, string label, bool isChecked)
        {
            Rectangle sourceRect = isChecked
                ? new Rectangle(236, 425, 9, 9)
                : new Rectangle(227, 425, 9, 9);

            b.Draw(Game1.mouseCursors,
                new Vector2(checkbox.bounds.X, checkbox.bounds.Y),
                sourceRect, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);

            float labelHeight = Game1.smallFont.MeasureString(label).Y;
            Vector2 labelPos = new Vector2(
                checkbox.bounds.Right + 4,
                checkbox.bounds.Y + (checkbox.bounds.Height - labelHeight) / 2
            );
            Utility.drawTextWithShadow(b, label, Game1.smallFont, labelPos, Game1.textColor);
        }

        public void DrawSearchScopeDropdown(SpriteBatch b, SearchScope current)
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

            SearchScope[] scopes = { SearchScope.Set, SearchScope.Item, SearchScope.All };

            for (int i = 0; i < SearchScopeOptions.Count; i++)
            {
                var option = SearchScopeOptions[i];
                bool isSelected = scopes[i] == current;
                bool isHovered = option.containsPoint(mouseX, mouseY);

                if (isSelected)
                    b.Draw(Game1.staminaRect, option.bounds, Color.LightBlue * 0.3f);
                else if (isHovered)
                    b.Draw(Game1.staminaRect, option.bounds, Color.Wheat * 0.6f);

                Vector2 textSize = Game1.smallFont.MeasureString(option.name);
                Vector2 textPos = new Vector2(
                    option.bounds.X + 12,
                    option.bounds.Y + (option.bounds.Height - textSize.Y) / 2
                );
                Utility.drawTextWithShadow(b, option.name, Game1.smallFont, textPos, Game1.textColor);
            }
        }

        public void DrawTagsDropdown(SpriteBatch b, HashSet<string> selectedTags)
        {
            if (TagsOptions.Count == 0)
                return;

            int visibleCount = 0;
            foreach (var opt in TagsOptions)
            {
                if (opt.visible) visibleCount++;
            }
            if (visibleCount == 0)
                return;

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
                    new Vector2(option.bounds.X + 8, option.bounds.Y + (option.bounds.Height - 36) / 2),
                    checkboxSource, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);

                string displayText = UIHelpers.TruncateText(option.name, option.bounds.Width - 56);
                Vector2 textSize = Game1.smallFont.MeasureString(displayText);
                Vector2 textPos = new Vector2(
                    option.bounds.X + 48,
                    option.bounds.Y + (option.bounds.Height - textSize.Y) / 2
                );
                Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPos, Game1.textColor);
            }

            int maxVisible = Math.Min(TemplatesDropdownMaxVisible, TagsOptions.Count);
            bool canScrollUp = tagsFirstVisible > 0;
            bool canScrollDown = tagsFirstVisible + maxVisible < TagsOptions.Count;

            if (canScrollUp)
            {
                Rectangle upArrow = new Rectangle(421, 459, 11, 12);
                Vector2 upPos = new Vector2(TagsDropdown.bounds.Right - 18, TagsDropdown.bounds.Bottom + 4);
                b.Draw(Game1.mouseCursors, upPos, upArrow, Color.White, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 1f);
            }

            if (canScrollDown)
            {
                Rectangle downArrow = new Rectangle(421, 472, 11, 12);
                Vector2 downPos = new Vector2(TagsDropdown.bounds.Right - 18, TagsDropdown.bounds.Bottom + dropdownHeight - 18);
                b.Draw(Game1.mouseCursors, downPos, downArrow, Color.White, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 1f);
            }
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

            bool[] checked_ = { filter.FavoritesOnly, filter.ShowGlobal, filter.ShowLocal };

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
                    new Vector2(option.bounds.X + 8, option.bounds.Y + (option.bounds.Height - 36) / 2),
                    checkboxSource, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);

                Vector2 textSize = Game1.smallFont.MeasureString(option.name);
                Vector2 textPos = new Vector2(
                    option.bounds.X + 48,
                    option.bounds.Y + (option.bounds.Height - textSize.Y) / 2
                );
                Utility.drawTextWithShadow(b, option.name, Game1.smallFont, textPos, Game1.textColor);
            }
        }

        private string BuildSetDisplayText(OutfitSet set)
        {
            string prefix = "";

            if (!set.IsValid)
                prefix += "⚠ ";

            if (set.IsFavorite)
                prefix += "★ ";
            else if (set.IsValid)
                prefix += "   ";

            return prefix + set.Name;
        }

        public void DrawRightPanel(SpriteBatch b, OutfitSet? set, OutfitSetStore store, Texture2D? previewTexture)
        {
            UIHelpers.DrawTextureBox(b, RightPanel.X - 8, RightPanel.Y - 8,
                RightPanel.Width + 16, RightPanel.Height + 16, Color.White);

            Utility.drawTextWithShadow(b, TranslationCache.TemplatesDetailsLabel, Game1.smallFont,
                new Vector2(RightPanel.X + 8, RightPanel.Y + 8), Game1.textColor);

            Rectangle previewBounds = new Rectangle(PreviewBox.X - 4, PreviewBox.Y - 4 + 32, PreviewBox.Width + 8, PreviewBox.Height + 8);
            UIHelpers.DrawTextureBox(b, previewBounds.X, previewBounds.Y, previewBounds.Width, previewBounds.Height, Color.White);

            if (set != null && previewTexture != null)
            {
                Rectangle drawRect = new Rectangle(PreviewBox.X, PreviewBox.Y + 32, PreviewBox.Width, PreviewBox.Height);
                b.Draw(previewTexture, drawRect, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
            }
            else
            {
                Utility.drawTextWithShadow(b, TranslationCache.TemplatesPreviewLabel, Game1.smallFont,
                    new Vector2(PreviewBox.X + 8, PreviewBox.Y + PreviewBox.Height / 2 + 32), Color.Gray);
            }

            if (set == null)
            {
                int noSelectionY = PreviewBox.Bottom + ContentBoxPadding + 32;
                Utility.drawTextWithShadow(b, TranslationCache.TemplatesSelectToView, Game1.smallFont,
                    new Vector2(RightPanel.X + 8, noSelectionY), Color.Gray);
                return;
            }

            int detailsY = PreviewBox.Bottom + ContentBoxPadding + 32;
            int maxTextWidth = RightPanel.Width - 24;

            string nameText = $"{TranslationCache.TemplatesName} {set.Name}";
            string truncatedName = UIHelpers.TruncateText(nameText, maxTextWidth);
            Utility.drawTextWithShadow(b, truncatedName, Game1.smallFont,
                new Vector2(RightPanel.X + 8, detailsY), Game1.textColor);

            detailsY += 32;
            string tagsDisplay = set.Tags.Count > 0 ? string.Join(", ", set.Tags) : TranslationCache.TemplatesItemNone;
            string tagsText = $"{TranslationCache.TemplatesTags} {tagsDisplay}";
            string truncatedTags = UIHelpers.TruncateText(tagsText, maxTextWidth);
            Utility.drawTextWithShadow(b, truncatedTags, Game1.smallFont,
                new Vector2(RightPanel.X + 8, detailsY), Game1.textColor);

            detailsY += 48;
            DrawItemDetail(b, detailsY, TranslationCache.TemplatesShirt, set.ShirtId, "(S)", store, maxTextWidth);

            detailsY += 28;
            DrawItemDetail(b, detailsY, TranslationCache.TemplatesPants, set.PantsId, "(P)", store, maxTextWidth);

            detailsY += 28;
            DrawItemDetail(b, detailsY, TranslationCache.TemplatesHat, set.HatId, "(H)", store, maxTextWidth);

            detailsY += 40;
            string scopeText = set.IsGlobal ? TranslationCache.TemplatesScopeGlobal : TranslationCache.TemplatesScopeLocal;
            Utility.drawTextWithShadow(b, $"Scope: {scopeText}", Game1.smallFont,
                new Vector2(RightPanel.X + 8, detailsY), Color.Gray);
        }

        private void DrawItemDetail(SpriteBatch b, int y, string label, string? itemId, string typePrefix, OutfitSetStore store, int maxTextWidth)
        {
            string displayText;

            if (string.IsNullOrEmpty(itemId))
            {
                displayText = TranslationCache.TemplatesItemNone;
            }
            else if (!store.IsItemValid(itemId, typePrefix))
            {
                displayText = "⚠ " + TranslationCache.TemplatesItemMissing;
            }
            else
            {
                displayText = store.GetItemDisplayName(itemId, typePrefix) ?? itemId;
            }

            string fullText = $"{label} {displayText}";
            string truncatedText = UIHelpers.TruncateText(fullText, maxTextWidth);
            Utility.drawTextWithShadow(b, truncatedText, Game1.smallFont,
                new Vector2(RightPanel.X + 8, y), Game1.textColor);
        }

        public void DrawButtons(SpriteBatch b, OutfitSet? selectedSet)
        {
            DrawTextButton(b, LoadButton, TranslationCache.TemplatesButtonLoad);
            DrawTextButton(b, ApplyCloseButton, TranslationCache.TemplatesButtonApplyClose);
            DrawTextButton(b, NewButton, TranslationCache.TemplatesButtonNew);
            DrawTextButton(b, RenameButton, TranslationCache.TemplatesButtonRename);
            DrawTextButton(b, DeleteButton, TranslationCache.TemplatesButtonDelete);

            string favoriteLabel = selectedSet?.IsFavorite == true
                ? "★ " + TranslationCache.TemplatesButtonFavorite
                : TranslationCache.TemplatesButtonFavorite;
            DrawTextButton(b, FavoriteButton, favoriteLabel);
        }

        public void DrawCloseButton(SpriteBatch b)
        {
            CloseButton.draw(b);
        }

        private void DrawTextButton(SpriteBatch b, ClickableComponent button, string label)
        {
            UIHelpers.DrawTextureBox(b, button.bounds.X, button.bounds.Y,
                button.bounds.Width, button.bounds.Height, Color.White);

            Vector2 textSize = Game1.smallFont.MeasureString(label);
            Utility.drawTextWithShadow(b, label, Game1.smallFont,
                new Vector2(button.bounds.X + (button.bounds.Width - textSize.X) / 2,
                    button.bounds.Y + (button.bounds.Height - textSize.Y) / 2),
                Game1.textColor);
        }

        public void SetupRenameMode(bool enabled)
        {
            isRenameMode = enabled;

            if (enabled)
            {
                int inputHeight = 50;
                int buttonWidth = 80;
                int buttonHeight = 40;
                int padding = 16;

                int centerY = RightPanel.Y + RightPanel.Height / 2;

                RenameInputArea = new ClickableComponent(
                    new Rectangle(
                        RightPanel.X + padding,
                        centerY - inputHeight - padding,
                        RightPanel.Width - (padding * 2),
                        inputHeight
                    ),
                    "renameInput"
                );

                int totalButtonWidth = (buttonWidth * 2) + padding;
                int buttonStartX = RightPanel.X + (RightPanel.Width - totalButtonWidth) / 2;

                RenameConfirmButton = new ClickableComponent(
                    new Rectangle(buttonStartX, centerY + padding, buttonWidth, buttonHeight),
                    "confirmRename"
                );

                RenameCancelButton = new ClickableComponent(
                    new Rectangle(buttonStartX + buttonWidth + padding, centerY + padding, buttonWidth, buttonHeight),
                    "cancelRename"
                );
            }
            else
            {
                RenameInputArea = null;
                RenameConfirmButton = null;
                RenameCancelButton = null;
            }
        }

        public void DrawRenamePanel(SpriteBatch b, string currentText, bool isSelected)
        {
            UIHelpers.DrawTextureBox(b, RightPanel.X - 8, RightPanel.Y - 8,
                RightPanel.Width + 16, RightPanel.Height + 16, Color.White);

            string title = TranslationCache.TemplatesButtonRename;
            Vector2 titleSize = Game1.smallFont.MeasureString(title);
            Utility.drawTextWithShadow(b, title, Game1.smallFont,
                new Vector2(RightPanel.X + (RightPanel.Width - titleSize.X) / 2, RightPanel.Y + 16),
                Game1.textColor);

            if (RenameInputArea != null)
            {
                Rectangle bounds = RenameInputArea.bounds;

                float labelHeight = Game1.smallFont.MeasureString("A").Y;
                Utility.drawTextWithShadow(b, TranslationCache.TemplatesName, Game1.smallFont,
                    new Vector2(bounds.X, bounds.Y - labelHeight - 8), Game1.textColor);

                UIHelpers.DrawTextureBox(b, bounds.X, bounds.Y, bounds.Width, bounds.Height, Color.White);

                bool showPlaceholder = string.IsNullOrEmpty(currentText);
                string displayText = showPlaceholder ? TranslationCache.TemplatesEnterName : currentText;
                Color textColor = showPlaceholder ? Color.Gray : Game1.textColor;
                Vector2 textPosition = new Vector2(bounds.X + 20, bounds.Y + (bounds.Height - labelHeight) / 2);
                Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPosition, textColor);

                if (isSelected && !showPlaceholder)
                {
                    Vector2 textSize = Game1.smallFont.MeasureString(currentText);
                    if ((int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 500) % 2 == 0)
                    {
                        b.Draw(Game1.staminaRect,
                            new Rectangle((int)(textPosition.X + textSize.X), (int)textPosition.Y, 4, (int)labelHeight),
                            Game1.textColor);
                    }
                }
                else if (isSelected && showPlaceholder)
                {
                    if ((int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 500) % 2 == 0)
                    {
                        b.Draw(Game1.staminaRect,
                            new Rectangle((int)textPosition.X, (int)textPosition.Y, 4, (int)labelHeight),
                            Game1.textColor);
                    }
                }
            }

            if (RenameConfirmButton != null)
            {
                UIHelpers.DrawTextButton(b, RenameConfirmButton, TranslationCache.SaveSetButtonSave);
            }

            if (RenameCancelButton != null)
            {
                UIHelpers.DrawTextButton(b, RenameCancelButton, TranslationCache.SaveSetButtonCancel);
            }
        }
    }
}
