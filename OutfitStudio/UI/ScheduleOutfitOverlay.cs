using System;
using System.Collections.Generic;
using System.Linq;
using OutfitStudio.Managers;
using OutfitStudio.Models;
using OutfitStudio.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    public class ScheduleOutfitOverlay : IClickableMenu
    {
        private readonly ScheduleOutfitUIBuilder uiBuilder;
        private readonly OutfitSetStore outfitSetStore;
        private readonly ScheduleStore scheduleStore;
        private readonly ScheduleRule? editingRule;

        private readonly SetFilterState filterState = new();
        private List<OutfitSet> displayedSets = new();
        private int selectedIndex = -1;
        private string? selectedSetId;
        private int listScrollOffset;
        private OutfitSet? SelectedSet => selectedIndex >= 0 && selectedIndex < displayedSets.Count
            ? displayedSets[selectedIndex]
            : null;

        private bool searchScopeOpen;
        private bool tagsDropdownOpen;
        private bool filterDropdownOpen;
        private TextBox? searchTextBox;
        private List<string> allTags = new();

        private RenderTarget2D? previewRenderTarget;
        private SpriteBatch? previewSpriteBatch;
        private bool previewDirty = true;
        private string? lastPreviewSetId;

        private static readonly FarmerSprite.AnimationFrame[] DirectionFrames = new[]
        {
            new FarmerSprite.AnimationFrame(12, 0, secondaryArm: false, flip: false),
            new FarmerSprite.AnimationFrame(6, 0, secondaryArm: false, flip: false),
            new FarmerSprite.AnimationFrame(0, 0, secondaryArm: false, flip: false),
            new FarmerSprite.AnimationFrame(6, 0, secondaryArm: false, flip: true),
        };
        private readonly ContinuousScrollHandler scrollHandler = new();

        private static bool lastShowInvalid = true;
        private static bool lastMatchAllTags = true;
        private int previewDirection = 2;
        private const int PreviewFarmerScale = 4;

        private bool shouldClose;
        private bool showSelected;
        private HashSet<string>? remainingIds;
        private int? remainingBeforeReset;
        private List<string> selectedSetIds;
        private readonly List<string>? originalSetIds;
        private readonly Action<List<string>> onSelectedChanged;

        public ScheduleOutfitOverlay(
            OutfitSetStore outfitSetStore,
            ScheduleStore scheduleStore,
            ScheduleRule? editingRule,
            List<string> selectedSetIds,
            List<string>? originalSetIds,
            Action<List<string>> onSelectedChanged)
        {
            this.outfitSetStore = outfitSetStore;
            this.scheduleStore = scheduleStore;
            this.editingRule = editingRule;
            this.selectedSetIds = new List<string>(selectedSetIds);
            this.originalSetIds = originalSetIds;
            this.onSelectedChanged = onSelectedChanged;

            uiBuilder = new ScheduleOutfitUIBuilder();
            width = uiBuilder.Width;
            height = uiBuilder.Height;
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;

            if (Enum.TryParse<SearchScope>(ModEntry.Config.DefaultSearchScope, out var scope))
                filterState.SearchScope = scope;

            if (!ModEntry.Config.ResetShowInvalidOnOpen)
                filterState.ShowInvalid = lastShowInvalid;

            if (!ModEntry.Config.ResetMatchAllOnOpen)
                filterState.MatchAllTags = lastMatchAllTags;

            CreateSearchTextBox();
            Game1.keyboardDispatcher.Subscriber = searchTextBox;
            RefreshDisplayedSets();
        }

        private void CreateSearchTextBox()
        {
            searchTextBox = new TextBox(
                Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                null,
                Game1.smallFont,
                Game1.textColor
            )
            {
                Text = "",
                Selected = true
            };
            UpdateSearchTextBoxBounds();
        }

        private void UpdateSearchTextBoxBounds()
        {
            if (searchTextBox == null)
                return;

            Rectangle bounds = uiBuilder.SearchBar.bounds;
            searchTextBox.X = bounds.X + 16;
            searchTextBox.Y = bounds.Y + ((bounds.Height - 48) / 2);
            searchTextBox.Width = bounds.Width - 32;
        }

        private void RefreshDisplayedSets()
        {
            allTags = outfitSetStore.GetAllTags();
            filterState.SelectedTags.IntersectWith(allTags);

            displayedSets = outfitSetStore.GetFilteredSets(filterState);

            if (showSelected)
                displayedSets = displayedSets.Where(s => selectedSetIds.Contains(s.Id)).ToList();

            ComputeRemainingIds();

            if (remainingIds != null && remainingIds.Count > 0)
            {
                displayedSets = displayedSets
                    .OrderByDescending(s => remainingIds.Contains(s.Id))
                    .ToList();
            }

            if (selectedSetId != null)
            {
                selectedIndex = displayedSets.FindIndex(s => s.Id == selectedSetId);
                if (selectedIndex < 0)
                    selectedSetId = null;
            }
            else
            {
                selectedIndex = -1;
            }

            ClampListScroll();
            MarkPreviewDirty();
        }

        private void ComputeRemainingIds()
        {
            remainingIds = null;
            remainingBeforeReset = null;

            if (editingRule == null || originalSetIds == null)
                return;

            var state = scheduleStore.GetRotationState(editingRule.Id);
            if (state == null)
                return;

            var simulated = new RotationState
            {
                RuleId = state.RuleId,
                Queue = new List<string>(state.Queue),
                LastUsedId = state.LastUsedId
            };
            ScheduleStore.SyncQueueWithSetIds(simulated, originalSetIds, selectedSetIds, new Random());
            remainingBeforeReset = simulated.Queue.Count;
            remainingIds = new HashSet<string>(simulated.Queue);
        }

        private void ClampListScroll()
        {
            int maxVisible = uiBuilder.OutfitListItems.Count;
            listScrollOffset = UIHelpers.ClampScrollOffset(listScrollOffset, displayedSets.Count, maxVisible);
        }

        private void MarkPreviewDirty()
        {
            string? currentId = SelectedSet?.Id;
            if (currentId != lastPreviewSetId)
            {
                previewDirty = true;
                lastPreviewSetId = currentId;
            }
        }

        private void CloseAllDropdowns()
        {
            searchScopeOpen = false;
            if (tagsDropdownOpen)
                uiBuilder.CloseTagSearch();
            tagsDropdownOpen = false;
            filterDropdownOpen = false;
        }

        private int ActiveCount => selectedSetIds.Count;

        public override bool readyToClose() => shouldClose;

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);

            uiBuilder.Recalculate();
            width = uiBuilder.Width;
            height = uiBuilder.Height;
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;
            UpdateSearchTextBoxBounds();
            CloseAllDropdowns();
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (!isWithinBounds(x, y) && ModEntry.Config.CloseOnClickOutside)
            {
                shouldClose = true;
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            if (searchScopeOpen || tagsDropdownOpen || filterDropdownOpen)
            {
                if (uiBuilder.CloseButton.containsPoint(x, y))
                {
                    CloseAllDropdowns();
                    shouldClose = true;
                    if (playSound) Game1.playSound("bigDeSelect");
                    return;
                }

                if (uiBuilder.SearchClearButton.containsPoint(x, y) && searchTextBox != null && !string.IsNullOrEmpty(searchTextBox.Text))
                {
                    CloseAllDropdowns();
                    searchTextBox.Text = "";
                    filterState.SearchText = "";
                    RefreshDisplayedSets();
                    if (playSound) Game1.playSound("smallSelect");
                    return;
                }
                if (uiBuilder.TagsClearButton.containsPoint(x, y))
                {
                    if (tagsDropdownOpen)
                    {
                        uiBuilder.ClearTagSearchText(allTags, filterState.SelectedTags);
                        if (playSound) Game1.playSound("smallSelect");
                        return;
                    }
                    if (filterState.SelectedTags.Count > 0)
                    {
                        CloseAllDropdowns();
                        filterState.ClearTags();
                        RefreshDisplayedSets();
                        if (playSound) Game1.playSound("smallSelect");
                        return;
                    }
                }
                if (uiBuilder.FilterClearButton.containsPoint(x, y) && (filterState.FavoritesOnly || !filterState.ShowGlobal || !filterState.ShowLocal || filterState.InvalidOnly))
                {
                    CloseAllDropdowns();
                    filterState.FavoritesOnly = false;
                    filterState.InvalidOnly = false;
                    filterState.ShowGlobal = true;
                    filterState.ShowLocal = true;
                    RefreshDisplayedSets();
                    if (playSound) Game1.playSound("smallSelect");
                    return;
                }

                if (uiBuilder.CloseMenuButton.containsPoint(x, y) || uiBuilder.SelectAllButton.containsPoint(x, y))
                {
                    CloseAllDropdowns();
                }
                else if (TryHandleDropdownOptionClick(x, y, playSound))
                {
                    return;
                }
                // Tags bar when tags dropdown open = input bar — absorb click
                else if (tagsDropdownOpen && uiBuilder.TagsDropdown.containsPoint(x, y))
                {
                    return;
                }
                else if (uiBuilder.SearchScopeDropdown.containsPoint(x, y) ||
                         uiBuilder.TagsDropdown.containsPoint(x, y) ||
                         uiBuilder.FilterDropdown.containsPoint(x, y) ||
                         uiBuilder.SearchBar.containsPoint(x, y))
                {
                    bool clickedSameDropdown =
                        (searchScopeOpen && uiBuilder.SearchScopeDropdown.containsPoint(x, y)) ||
                        (filterDropdownOpen && uiBuilder.FilterDropdown.containsPoint(x, y));

                    CloseAllDropdowns();

                    if (clickedSameDropdown)
                    {
                        if (playSound) Game1.playSound("smallSelect");
                        return;
                    }
                }
                else
                {
                    if (HandleDropdownClick(x, y, playSound))
                        return;
                    return;
                }
            }

            if (HandleFilterBarClick(x, y, playSound))
                return;

            if (uiBuilder.CloseButton.containsPoint(x, y))
            {
                shouldClose = true;
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            for (int i = 0; i < uiBuilder.ExcludeButtons.Count; i++)
            {
                int dataIndex = listScrollOffset + i;
                if (dataIndex >= displayedSets.Count)
                    break;

                if (uiBuilder.ExcludeButtons[i].containsPoint(x, y))
                {
                    ToggleSelection(dataIndex);
                    if (playSound) Game1.playSound("drumkit6");
                    return;
                }
            }

            for (int i = 0; i < uiBuilder.OutfitListItems.Count; i++)
            {
                int dataIndex = listScrollOffset + i;
                if (dataIndex >= displayedSets.Count)
                    break;

                if (uiBuilder.OutfitListItems[i].containsPoint(x, y))
                {
                    selectedIndex = dataIndex;
                    selectedSetId = displayedSets[dataIndex].Id;
                    MarkPreviewDirty();
                    if (playSound) Game1.playSound("smallSelect");
                    return;
                }
            }

            if (uiBuilder.LeftArrowButton.containsPoint(x, y))
            {
                previewDirection = (previewDirection + 1) % 4;
                previewDirty = true;
                if (playSound) Game1.playSound("shwip");
                return;
            }

            if (uiBuilder.RightArrowButton.containsPoint(x, y))
            {
                previewDirection = (previewDirection + 3) % 4;
                previewDirty = true;
                if (playSound) Game1.playSound("shwip");
                return;
            }

            if (uiBuilder.SelectAllButton.containsPoint(x, y))
            {
                SelectAllDisplayed();
                if (playSound) Game1.playSound("smallSelect");
                return;
            }

            if (uiBuilder.CloseMenuButton.containsPoint(x, y))
            {
                shouldClose = true;
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }
        }

        private bool TryHandleDropdownOptionClick(int x, int y, bool playSound)
        {
            if (searchScopeOpen)
            {
                for (int i = 0; i < uiBuilder.SearchScopeOptions.Count; i++)
                {
                    if (uiBuilder.SearchScopeOptions[i].containsPoint(x, y))
                    {
                        SearchScope[] scopes = { SearchScope.Set, SearchScope.Item, SearchScope.All };
                        filterState.SearchScope = scopes[i];
                        CloseAllDropdowns();
                        RefreshDisplayedSets();
                        if (playSound) Game1.playSound("smallSelect");
                        return true;
                    }
                }
            }

            if (tagsDropdownOpen)
            {
                foreach (var option in uiBuilder.TagsOptions)
                {
                    if (option.visible && option.containsPoint(x, y))
                    {
                        filterState.ToggleTag(option.name);
                        uiBuilder.BuildTagsOptions(allTags, filterState.SelectedTags);
                        RefreshDisplayedSets();
                        if (playSound) Game1.playSound("smallSelect");
                        return true;
                    }
                }
            }

            if (filterDropdownOpen)
            {
                for (int i = 0; i < uiBuilder.FilterOptions.Count; i++)
                {
                    if (uiBuilder.FilterOptions[i].containsPoint(x, y))
                    {
                        switch (i)
                        {
                            case 0:
                                filterState.FavoritesOnly = !filterState.FavoritesOnly;
                                break;
                            case 1:
                                filterState.InvalidOnly = !filterState.InvalidOnly;
                                break;
                            case 2:
                                filterState.ShowGlobal = !filterState.ShowGlobal;
                                break;
                            case 3:
                                filterState.ShowLocal = !filterState.ShowLocal;
                                break;
                        }
                        uiBuilder.BuildFilterOptions(filterState);
                        RefreshDisplayedSets();
                        if (playSound) Game1.playSound("smallSelect");
                        return true;
                    }
                }
            }

            return false;
        }

        private bool HandleDropdownClick(int x, int y, bool playSound)
        {
            if (searchScopeOpen)
            {
                for (int i = 0; i < uiBuilder.SearchScopeOptions.Count; i++)
                {
                    if (uiBuilder.SearchScopeOptions[i].containsPoint(x, y))
                    {
                        SearchScope[] scopes = { SearchScope.Set, SearchScope.Item, SearchScope.All };
                        filterState.SearchScope = scopes[i];
                        CloseAllDropdowns();
                        RefreshDisplayedSets();
                        if (playSound) Game1.playSound("smallSelect");
                        return true;
                    }
                }

                CloseAllDropdowns();
                return true;
            }

            if (tagsDropdownOpen)
            {
                foreach (var option in uiBuilder.TagsOptions)
                {
                    if (option.visible && option.containsPoint(x, y))
                    {
                        filterState.ToggleTag(option.name);
                        uiBuilder.BuildTagsOptions(allTags, filterState.SelectedTags);
                        RefreshDisplayedSets();
                        if (playSound) Game1.playSound("smallSelect");
                        return true;
                    }
                }

                CloseAllDropdowns();
                return true;
            }

            if (filterDropdownOpen)
            {
                for (int i = 0; i < uiBuilder.FilterOptions.Count; i++)
                {
                    if (uiBuilder.FilterOptions[i].containsPoint(x, y))
                    {
                        switch (i)
                        {
                            case 0:
                                filterState.FavoritesOnly = !filterState.FavoritesOnly;
                                break;
                            case 1:
                                filterState.ShowGlobal = !filterState.ShowGlobal;
                                break;
                            case 2:
                                filterState.ShowLocal = !filterState.ShowLocal;
                                break;
                        }
                        uiBuilder.BuildFilterOptions(filterState);
                        RefreshDisplayedSets();
                        if (playSound) Game1.playSound("smallSelect");
                        return true;
                    }
                }

                CloseAllDropdowns();
                return true;
            }

            return false;
        }

        private bool HandleFilterBarClick(int x, int y, bool playSound)
        {
            if (uiBuilder.SearchClearButton.containsPoint(x, y) && searchTextBox != null && !string.IsNullOrEmpty(searchTextBox.Text))
            {
                searchTextBox.Text = "";
                filterState.SearchText = "";
                RefreshDisplayedSets();
                if (playSound) Game1.playSound("smallSelect");
                return true;
            }

            if (uiBuilder.TagsClearButton.containsPoint(x, y) && filterState.SelectedTags.Count > 0)
            {
                filterState.ClearTags();
                RefreshDisplayedSets();
                if (playSound) Game1.playSound("smallSelect");
                return true;
            }

            if (uiBuilder.FilterClearButton.containsPoint(x, y) && (filterState.FavoritesOnly || !filterState.ShowGlobal || !filterState.ShowLocal || filterState.InvalidOnly))
            {
                filterState.FavoritesOnly = false;
                filterState.ShowGlobal = true;
                filterState.ShowLocal = true;
                filterState.InvalidOnly = false;
                RefreshDisplayedSets();
                if (playSound) Game1.playSound("smallSelect");
                return true;
            }

            if (uiBuilder.SearchScopeDropdown.containsPoint(x, y))
            {
                CloseAllDropdowns();
                searchScopeOpen = true;
                uiBuilder.BuildSearchScopeOptions(filterState.SearchScope);
                if (playSound) Game1.playSound("smallSelect");
                return true;
            }

            if (uiBuilder.SearchBar.containsPoint(x, y))
            {
                CloseAllDropdowns();
                if (searchTextBox != null)
                {
                    searchTextBox.Selected = true;
                }
                return true;
            }

            if (uiBuilder.TagsDropdown.containsPoint(x, y))
            {
                if (!uiBuilder.TagsClearButton.containsPoint(x, y))
                {
                    CloseAllDropdowns();
                    tagsDropdownOpen = true;
                    uiBuilder.OpenTagSearch(allTags, filterState.SelectedTags);
                    if (playSound) Game1.playSound("smallSelect");
                }
                return true;
            }

            if (uiBuilder.FilterDropdown.containsPoint(x, y))
            {
                if (!uiBuilder.FilterClearButton.containsPoint(x, y))
                {
                    CloseAllDropdowns();
                    filterDropdownOpen = true;
                    uiBuilder.BuildFilterOptions(filterState);
                    if (playSound) Game1.playSound("smallSelect");
                }
                return true;
            }

            Rectangle showSelectedArea = new Rectangle(
                uiBuilder.ShowSelectedCheckbox.bounds.X,
                uiBuilder.ShowSelectedCheckbox.bounds.Y,
                uiBuilder.ShowSelectedCheckbox.bounds.Width + (int)Game1.smallFont.MeasureString(TranslationCache.ScheduleOutfitShowSelected).X + 14,
                uiBuilder.ShowSelectedCheckbox.bounds.Height
            );
            if (showSelectedArea.Contains(x, y))
            {
                showSelected = !showSelected;
                RefreshDisplayedSets();
                if (playSound) Game1.playSound("smallSelect");
                return true;
            }

            Rectangle matchAllArea = new Rectangle(
                uiBuilder.MatchAllCheckbox.bounds.X,
                uiBuilder.MatchAllCheckbox.bounds.Y,
                uiBuilder.MatchAllCheckbox.bounds.Width + (int)Game1.smallFont.MeasureString(TranslationCache.WardrobeFilterMatchAll).X + 14,
                uiBuilder.MatchAllCheckbox.bounds.Height
            );
            if (matchAllArea.Contains(x, y))
            {
                filterState.MatchAllTags = !filterState.MatchAllTags;
                lastMatchAllTags = filterState.MatchAllTags;
                RefreshDisplayedSets();
                if (playSound) Game1.playSound("smallSelect");
                return true;
            }

            Rectangle showInvalidArea = new Rectangle(
                uiBuilder.ShowInvalidCheckbox.bounds.X,
                uiBuilder.ShowInvalidCheckbox.bounds.Y,
                uiBuilder.ShowInvalidCheckbox.bounds.Width + (int)Game1.smallFont.MeasureString(TranslationCache.WardrobeFilterShowInvalid).X + 14,
                uiBuilder.ShowInvalidCheckbox.bounds.Height
            );
            if (showInvalidArea.Contains(x, y))
            {
                filterState.ShowInvalid = !filterState.ShowInvalid;
                lastShowInvalid = filterState.ShowInvalid;
                RefreshDisplayedSets();
                if (playSound) Game1.playSound("smallSelect");
                return true;
            }

            return false;
        }

        private void ToggleSelection(int dataIndex)
        {
            var set = displayedSets[dataIndex];
            if (selectedSetIds.Contains(set.Id))
                selectedSetIds.Remove(set.Id);
            else
                selectedSetIds.Add(set.Id);

            ComputeRemainingIds();
            onSelectedChanged(new List<string>(selectedSetIds));
        }

        private void SelectAllDisplayed()
        {
            foreach (var set in displayedSets)
            {
                if (!selectedSetIds.Contains(set.Id))
                    selectedSetIds.Add(set.Id);
            }

            ComputeRemainingIds();
            onSelectedChanged(new List<string>(selectedSetIds));
        }

        public override void receiveKeyPress(Keys key)
        {
            if (searchScopeOpen || tagsDropdownOpen || filterDropdownOpen)
            {
                if (key == Keys.Escape)
                {
                    CloseAllDropdowns();
                    Game1.playSound("bigDeSelect");
                }
                else if (tagsDropdownOpen && ModEntry.Config.ArrowKeyScrolling
                    && (key == Keys.Up || key == Keys.Down))
                {
                    int direction = key == Keys.Up ? 1 : -1;
                    if (uiBuilder.ScrollTagsDropdown(direction))
                    {
                        uiBuilder.BuildTagsOptions(allTags, filterState.SelectedTags);
                        Game1.playSound("shiny4");
                    }
                }
                return;
            }

            if (ModEntry.Config.ArrowKeyScrolling)
            {
                int maxVisible = uiBuilder.OutfitListItems.Count;
                int maxScroll = Math.Max(0, displayedSets.Count - maxVisible);

                if (key == Keys.Up && listScrollOffset > 0)
                {
                    listScrollOffset--;
                    Game1.playSound("shiny4");
                    return;
                }

                if (key == Keys.Down && listScrollOffset < maxScroll)
                {
                    listScrollOffset++;
                    Game1.playSound("shiny4");
                    return;
                }

                if (key == Keys.Left && listScrollOffset > 0)
                {
                    listScrollOffset = Math.Max(0, listScrollOffset - maxVisible);
                    Game1.playSound("shiny4");
                    return;
                }

                if (key == Keys.Right && listScrollOffset < maxScroll)
                {
                    listScrollOffset = Math.Min(maxScroll, listScrollOffset + maxVisible);
                    Game1.playSound("shiny4");
                    return;
                }
            }

            if (key == Keys.Escape)
            {
                shouldClose = true;
                Game1.playSound("bigDeSelect");
                return;
            }

            if (Game1.options.doesInputListContain(Game1.options.menuButton, key))
                return;
        }

        public override void receiveScrollWheelAction(int direction)
        {
            if (tagsDropdownOpen)
            {
                if (uiBuilder.ScrollTagsDropdown(direction))
                {
                    uiBuilder.BuildTagsOptions(allTags, filterState.SelectedTags);
                }
                return;
            }

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            if (uiBuilder.RightPanel.Contains(mouseX, mouseY))
            {
                int maxVisible = uiBuilder.OutfitListItems.Count;
                int maxScroll = Math.Max(0, displayedSets.Count - maxVisible);

                if (direction > 0 && listScrollOffset > 0)
                {
                    listScrollOffset--;
                    Game1.playSound("shiny4");
                }
                else if (direction < 0 && listScrollOffset < maxScroll)
                {
                    listScrollOffset++;
                    Game1.playSound("shiny4");
                }
                return;
            }

            base.receiveScrollWheelAction(direction);
        }

        public override void update(GameTime time)
        {
            base.update(time);

            if (searchTextBox != null)
            {
                if (!tagsDropdownOpen)
                {
                    searchTextBox.Update();
                    searchTextBox.Selected = true;
                }
                else
                {
                    searchTextBox.Selected = false;
                }

                if (searchTextBox.Text != filterState.SearchText)
                {
                    filterState.SearchText = searchTextBox.Text;
                    RefreshDisplayedSets();
                }
            }

            if (tagsDropdownOpen)
            {
                uiBuilder.UpdateTagSearch(allTags, filterState.SelectedTags);
            }

            if (ModEntry.Config.ArrowKeyScrolling
                && !searchScopeOpen && !filterDropdownOpen)
            {
                int maxVisible = uiBuilder.OutfitListItems.Count;
                int scrollAmount = scrollHandler.Update(time, maxVisible, out bool shouldPlaySound);
                if (scrollAmount != 0)
                {
                    if (tagsDropdownOpen)
                    {
                        if (uiBuilder.ScrollTagsDropdown(-scrollAmount))
                        {
                            uiBuilder.BuildTagsOptions(allTags, filterState.SelectedTags);
                            if (shouldPlaySound) Game1.playSound("shiny4");
                        }
                    }
                    else
                    {
                        int maxScroll = Math.Max(0, displayedSets.Count - maxVisible);
                        int newOffset = Math.Clamp(listScrollOffset + scrollAmount, 0, maxScroll);
                        if (newOffset != listScrollOffset)
                        {
                            listScrollOffset = newOffset;
                            if (shouldPlaySound) Game1.playSound("shiny4");
                        }
                    }
                }
            }
            else
            {
                scrollHandler.Reset();
            }
        }

        private void InitializePreviewResources()
        {
            if (previewRenderTarget == null || previewRenderTarget.IsDisposed)
            {
                previewRenderTarget = new RenderTarget2D(
                    Game1.graphics.GraphicsDevice,
                    SaveSetPreviewWidth,
                    SaveSetPreviewHeight,
                    false,
                    SurfaceFormat.Color,
                    DepthFormat.None,
                    0,
                    RenderTargetUsage.DiscardContents
                );
            }

            if (previewSpriteBatch == null || previewSpriteBatch.IsDisposed)
            {
                previewSpriteBatch = new SpriteBatch(Game1.graphics.GraphicsDevice);
            }
        }

        private void RenderPreviewToTarget(OutfitSet? set)
        {
            InitializePreviewResources();

            var renderTargets = Game1.graphics.GraphicsDevice.GetRenderTargets();
            Game1.graphics.GraphicsDevice.SetRenderTarget(previewRenderTarget);
            Game1.graphics.GraphicsDevice.Clear(Color.Transparent);

            var savedShirt = Game1.player.shirtItem.Value;
            var savedPants = Game1.player.pantsItem.Value;
            var savedHat = Game1.player.hat.Value;
            int originalEyes = Game1.player.currentEyes;
            int savedHair = Game1.player.hair.Value;
            Color savedHairColor = Game1.player.hairstyleColor.Value;

            try
            {
                Game1.player.currentEyes = 0;

                if (set != null)
                {
                    if (!string.IsNullOrEmpty(set.ShirtId) && outfitSetStore.IsItemValid(set.ShirtId, "(S)"))
                    {
                        var shirt = ItemRegistry.Create<Clothing>("(S)" + set.ShirtId);
                        ColorHelper.ApplyColor(shirt, set.ShirtColor);
                        Game1.player.shirtItem.Value = shirt;
                    }
                    else
                        Game1.player.shirtItem.Value = null;

                    if (!string.IsNullOrEmpty(set.PantsId) && outfitSetStore.IsItemValid(set.PantsId, "(P)"))
                    {
                        var pants = ItemRegistry.Create<Clothing>("(P)" + set.PantsId);
                        ColorHelper.ApplyColor(pants, set.PantsColor);
                        Game1.player.pantsItem.Value = pants;
                    }
                    else
                        Game1.player.pantsItem.Value = null;

                    if (!string.IsNullOrEmpty(set.HatId) && outfitSetStore.IsItemValid(set.HatId, "(H)"))
                        Game1.player.hat.Value = ItemRegistry.Create<Hat>("(H)" + set.HatId);
                    else
                        Game1.player.hat.Value = null;

                    if (set.HairId.HasValue)
                    {
                        Game1.player.changeHairStyle(set.HairId.Value);
                        var hairColor = ColorHelper.ParseColor(set.HairColor);
                        if (hairColor.HasValue)
                            Game1.player.changeHairColor(hairColor.Value);
                    }
                }

                Game1.player.FarmerRenderer.MarkSpriteDirty();

                int frameIndex = DirectionFrames[previewDirection].frame;
                int baseY = Game1.player.bathingClothes.Value ? 576 : 0;
                int sourceX = (frameIndex * 16) % 96;
                int sourceY = baseY + (frameIndex * 16) / 96 * 32;
                Rectangle sourceRect = new Rectangle(sourceX, sourceY, FarmerSpriteWidth, FarmerSpriteHeight);

                int scaledWidth = FarmerSpriteWidth * PreviewFarmerScale;
                int scaledHeight = FarmerSpriteHeight * PreviewFarmerScale;
                Vector2 centeredPosition = new Vector2(
                    (SaveSetPreviewWidth - scaledWidth) / 2,
                    (SaveSetPreviewHeight - scaledHeight) / 2
                );

                previewSpriteBatch!.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

                FarmerRenderer.isDrawingForUI = true;
                Game1.player.FarmerRenderer.draw(
                    previewSpriteBatch,
                    DirectionFrames[previewDirection],
                    DirectionFrames[previewDirection].frame,
                    sourceRect,
                    centeredPosition,
                    Vector2.Zero,
                    0.8f,
                    previewDirection,
                    Color.White,
                    0f,
                    1f,
                    Game1.player
                );

                FarmerRenderer.isDrawingForUI = false;

                previewSpriteBatch.End();
            }
            finally
            {
                Game1.player.shirtItem.Value = savedShirt;
                Game1.player.pantsItem.Value = savedPants;
                Game1.player.hat.Value = savedHat;
                Game1.player.changeHairStyle(savedHair);
                Game1.player.changeHairColor(savedHairColor);
                Game1.player.currentEyes = originalEyes;
                Game1.player.FarmerRenderer.MarkSpriteDirty();
            }

            Game1.graphics.GraphicsDevice.SetRenderTargets(renderTargets);
        }


        protected override void cleanupBeforeExit()
        {
            base.cleanupBeforeExit();
            UIHelpers.SafeDispose(ref previewRenderTarget, ref previewSpriteBatch);
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
                Color.Black * BackgroundOverlayOpacity);

            uiBuilder.DrawBackground(b);
            uiBuilder.DrawCheckboxDivider(b);
            uiBuilder.DrawContentDivider(b);
            uiBuilder.DrawHeader(b, ActiveCount, outfitSetStore.GetAllSets().Count, remainingBeforeReset);

            string searchText = searchTextBox?.Text ?? "";
            bool searchFocused = searchTextBox?.Selected ?? false;

            var selectedSet = new HashSet<string>(selectedSetIds);
            uiBuilder.DrawOutfitSetList(b, displayedSets, selectedIndex, listScrollOffset, filterState,
                searchScopeOpen, tagsDropdownOpen, filterDropdownOpen, searchText, allTags, searchFocused,
                selectedSet, showSelected, remainingIds);

            if (previewDirty)
            {
                RenderPreviewToTarget(SelectedSet);
                previewDirty = false;
            }
            uiBuilder.DrawPreviewPanel(b, SelectedSet, previewRenderTarget);
            uiBuilder.DrawArrows(b);

            uiBuilder.DrawButtons(b);
            uiBuilder.DrawCloseButton(b);

            string? tagsDropdownTooltip = null;
            if (searchScopeOpen)
            {
                string scopeLabel = filterState.SearchScope switch
                {
                    SearchScope.Set => TranslationCache.WardrobeFilterSearchSet,
                    SearchScope.Item => TranslationCache.WardrobeFilterSearchItem,
                    SearchScope.All => TranslationCache.WardrobeFilterSearchAll,
                    _ => TranslationCache.WardrobeFilterSearchSet
                };
                UIHelpers.DrawDropdownOptions(b, uiBuilder.SearchScopeDropdown.bounds,
                    uiBuilder.SearchScopeOptions, 0, uiBuilder.SearchScopeOptions.Count,
                    isSelected: opt => opt.name == scopeLabel, arrowXNudge: 2);
            }
            else if (tagsDropdownOpen)
            {
                tagsDropdownTooltip = UIHelpers.DrawMultiSelectDropdownOptions(b,
                    uiBuilder.TagsDropdown.bounds, uiBuilder.TagsOptions,
                    uiBuilder.TagsFirstVisible, WardrobeDropdownMaxVisible,
                    isChecked: opt => filterState.SelectedTags.Contains(opt.name),
                    totalItemCount: allTags.Count, arrowXNudge: 2);
            }
            else if (filterDropdownOpen)
            {
                UIHelpers.DrawMultiSelectDropdownOptions(b,
                    uiBuilder.FilterDropdown.bounds, uiBuilder.FilterOptions,
                    0, uiBuilder.FilterOptions.Count,
                    isChecked: opt =>
                        (opt.name == TranslationCache.CommonFavorite && filterState.FavoritesOnly) ||
                        (opt.name == TranslationCache.WardrobeFilterInvalid && filterState.InvalidOnly) ||
                        (opt.name == TranslationCache.WardrobeFilterGlobal && filterState.ShowGlobal) ||
                        (opt.name == TranslationCache.WardrobeFilterLocal && filterState.ShowLocal),
                    arrowXNudge: 2);
            }

            if (!searchScopeOpen && !tagsDropdownOpen && !filterDropdownOpen)
            {
                if (uiBuilder.HoveredTruncatedSetName != null && ModEntry.Config.ShowTooltip)
                {
                    UIHelpers.DrawWrappedTooltip(b, uiBuilder.HoveredTruncatedSetName);
                }
            }

            if (tagsDropdownTooltip != null)
            {
                UIHelpers.DrawWrappedTooltip(b, tagsDropdownTooltip);
            }
            else if (!searchScopeOpen && filterState.SelectedTags.Count > 0)
            {
                int mouseX = Game1.getMouseX();
                int mouseY = Game1.getMouseY();
                if (uiBuilder.TagsDropdown.containsPoint(mouseX, mouseY))
                {
                    UIHelpers.DrawWrappedTooltip(b, string.Join(", ", filterState.SelectedTags));
                }
            }

            drawMouse(b);
        }
    }
}
