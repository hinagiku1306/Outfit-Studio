using System;
using System.Collections.Generic;
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
    public class WardrobeOverlay : IClickableMenu
    {
        private readonly WardrobeUIBuilder uiBuilder;
        private readonly OutfitSetStore store;
        private readonly OutfitMenu? parentMenu;
        private bool shouldClose;

        private List<OutfitSet> displayedSets = new();
        private int selectedIndex = -1;
        private string? selectedSetId;
        private int listScrollOffset;
        private OutfitSet? SelectedSet => selectedIndex >= 0 && selectedIndex < displayedSets.Count
            ? displayedSets[selectedIndex]
            : null;

        private bool showDeleteConfirmation;
        private ClickableComponent? deleteYesButton;
        private ClickableComponent? deleteNoButton;
        private Rectangle deleteDialogBounds;

        private readonly SetFilterState filterState = new();
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
            new FarmerSprite.AnimationFrame(12, 0, secondaryArm: false, flip: false), // 0 = Up
            new FarmerSprite.AnimationFrame(6, 0, secondaryArm: false, flip: false),  // 1 = Right
            new FarmerSprite.AnimationFrame(0, 0, secondaryArm: false, flip: false),  // 2 = Down
            new FarmerSprite.AnimationFrame(6, 0, secondaryArm: false, flip: true),   // 3 = Left
        };
        private static bool lastShowInvalid = true;
        private int previewDirection = 2;
        private const int PreviewFarmerScale = 4;

        private Clothing? cachedShirt;
        private Clothing? cachedPants;
        private Hat? cachedHat;
        private string? cachedSetId;

        public WardrobeOverlay(OutfitSetStore store, OutfitMenu? parentMenu = null)
        {
            this.store = store;
            this.parentMenu = parentMenu;

            uiBuilder = new WardrobeUIBuilder();
            width = uiBuilder.Width;
            height = uiBuilder.Height;
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;

            if (Enum.TryParse<SearchScope>(ModEntry.Config.DefaultSearchScope, out var scope))
                filterState.SearchScope = scope;

            if (!ModEntry.Config.ResetShowInvalidOnOpen)
                filterState.ShowInvalid = lastShowInvalid;

            CreateSearchTextBox();
            RefreshDisplayedSets();
            CacheItemsForSet(null);
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
            allTags = store.GetAllTags();
            filterState.SelectedTags.IntersectWith(allTags);
            displayedSets = store.GetFilteredSets(filterState);

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

        private void ClampListScroll()
        {
            int maxVisible = uiBuilder.OutfitListItems.Count;
            int maxScroll = Math.Max(0, displayedSets.Count - maxVisible);
            listScrollOffset = Math.Clamp(listScrollOffset, 0, maxScroll);
        }

        private void MarkPreviewDirty()
        {
            string? currentId = SelectedSet?.Id;
            if (currentId != lastPreviewSetId)
            {
                previewDirty = true;
                lastPreviewSetId = currentId;
                CacheItemsForSet(SelectedSet);
            }
        }

        private void CacheItemsForSet(OutfitSet? set)
        {
            string? targetId = set?.Id;
            if (targetId != null && targetId == cachedSetId)
                return;

            cachedShirt = null;
            cachedPants = null;
            cachedHat = null;
            cachedSetId = targetId;

            if (set == null)
            {
                cachedShirt = Game1.player.shirtItem.Value;
                cachedPants = Game1.player.pantsItem.Value;
                cachedHat = Game1.player.hat.Value;
                return;
            }

            if (!string.IsNullOrEmpty(set.ShirtId) && store.IsItemValid(set.ShirtId, "(S)"))
                cachedShirt = ItemRegistry.Create<Clothing>("(S)" + set.ShirtId);

            if (!string.IsNullOrEmpty(set.PantsId) && store.IsItemValid(set.PantsId, "(P)"))
                cachedPants = ItemRegistry.Create<Clothing>("(P)" + set.PantsId);

            if (!string.IsNullOrEmpty(set.HatId) && store.IsItemValid(set.HatId, "(H)"))
                cachedHat = ItemRegistry.Create<Hat>("(H)" + set.HatId);
        }

        private void CloseAllDropdowns()
        {
            searchScopeOpen = false;
            tagsDropdownOpen = false;
            filterDropdownOpen = false;
        }

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

        public override bool readyToClose() => shouldClose;

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (!isWithinBounds(x, y) && ModEntry.Config.CloseOnClickOutside)
            {
                shouldClose = true;
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            if (showDeleteConfirmation)
            {
                HandleDeleteConfirmationClick(x, y, playSound);
                return;
            }

            if (searchScopeOpen || tagsDropdownOpen || filterDropdownOpen)
            {
                // Close button always works
                if (uiBuilder.CloseButton.containsPoint(x, y))
                {
                    CloseAllDropdowns();
                    shouldClose = true;
                    if (playSound) Game1.playSound("bigDeSelect");
                    return;
                }

                // Clear buttons always work
                if (uiBuilder.SearchClearButton.containsPoint(x, y) && searchTextBox != null && !string.IsNullOrEmpty(searchTextBox.Text))
                {
                    CloseAllDropdowns();
                    searchTextBox.Text = "";
                    filterState.SearchText = "";
                    RefreshDisplayedSets();
                    if (playSound) Game1.playSound("smallSelect");
                    return;
                }
                if (uiBuilder.TagsClearButton.containsPoint(x, y) && filterState.SelectedTags.Count > 0)
                {
                    CloseAllDropdowns();
                    filterState.ClearTags();
                    RefreshDisplayedSets();
                    if (playSound) Game1.playSound("smallSelect");
                    return;
                }
                if (uiBuilder.FilterClearButton.containsPoint(x, y) && (filterState.FavoritesOnly || !filterState.ShowGlobal || !filterState.ShowLocal))
                {
                    CloseAllDropdowns();
                    filterState.FavoritesOnly = false;
                    filterState.ShowGlobal = true;
                    filterState.ShowLocal = true;
                    RefreshDisplayedSets();
                    if (playSound) Game1.playSound("smallSelect");
                    return;
                }

                // Bottom buttons always work: close dropdown and fall through
                if (uiBuilder.ApplyCloseButton.containsPoint(x, y) ||
                    uiBuilder.EditButton.containsPoint(x, y) ||
                    uiBuilder.DeleteButton.containsPoint(x, y))
                {
                    CloseAllDropdowns();
                    // Fall through to normal button handling below
                }
                // Dropdown options take priority over dropdown bars (options may overlap bars)
                else if (TryHandleDropdownOptionClick(x, y, playSound))
                {
                    return;
                }
                // Other dropdown bars: close current and open new (fall through to HandleFilterBarClick)
                else if (uiBuilder.SearchScopeDropdown.containsPoint(x, y) ||
                         uiBuilder.TagsDropdown.containsPoint(x, y) ||
                         uiBuilder.FilterDropdown.containsPoint(x, y) ||
                         uiBuilder.SearchBar.containsPoint(x, y))
                {
                    bool clickedSameDropdown =
                        (searchScopeOpen && uiBuilder.SearchScopeDropdown.containsPoint(x, y)) ||
                        (tagsDropdownOpen && uiBuilder.TagsDropdown.containsPoint(x, y)) ||
                        (filterDropdownOpen && uiBuilder.FilterDropdown.containsPoint(x, y));

                    CloseAllDropdowns();

                    if (clickedSameDropdown)
                    {
                        if (playSound) Game1.playSound("smallSelect");
                        return;
                    }
                    // Fall through to HandleFilterBarClick to open the new dropdown
                }
                else
                {
                    // Not an exception element — handle dropdown options or block
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

            if (uiBuilder.ApplyCloseButton.containsPoint(x, y))
            {
                if (SelectedSet != null)
                {
                    store.ApplySet(SelectedSet);
                    parentMenu?.NotifyOutfitApplied();
                    if (playSound) Game1.playSound("coin");
                }
                shouldClose = true;
                return;
            }

            if (uiBuilder.EditButton.containsPoint(x, y))
            {
                if (SelectedSet != null)
                {
                    OpenEditOverlay();
                    if (playSound) Game1.playSound("bigSelect");
                }
                return;
            }

            if (uiBuilder.DeleteButton.containsPoint(x, y))
            {
                if (SelectedSet != null)
                {
                    ShowDeleteConfirmation();
                    if (playSound) Game1.playSound("smallSelect");
                }
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
            // Clear buttons checked first so clicking X doesn't toggle the parent
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

            if (uiBuilder.FilterClearButton.containsPoint(x, y) && (filterState.FavoritesOnly || !filterState.ShowGlobal || !filterState.ShowLocal))
            {
                filterState.FavoritesOnly = false;
                filterState.ShowGlobal = true;
                filterState.ShowLocal = true;
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
                    uiBuilder.BuildTagsOptions(allTags, filterState.SelectedTags);
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

            Rectangle matchAllArea = new Rectangle(
                uiBuilder.MatchAllCheckbox.bounds.X,
                uiBuilder.MatchAllCheckbox.bounds.Y,
                uiBuilder.MatchAllCheckbox.bounds.Width + (int)Game1.smallFont.MeasureString(TranslationCache.WardrobeFilterMatchAll).X + 14,
                uiBuilder.MatchAllCheckbox.bounds.Height
            );
            if (matchAllArea.Contains(x, y))
            {
                filterState.MatchAllTags = !filterState.MatchAllTags;
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

        private void OpenEditOverlay()
        {
            if (SelectedSet == null || parentMenu == null)
                return;

            Game1.activeClickableMenu = new SaveSetOverlay(parentMenu, store, () =>
            {
                cachedSetId = null;
                lastPreviewSetId = null;
                RefreshDisplayedSets();
            }, editingSet: SelectedSet, onClose: RefreshDisplayedSets);
        }

        private void ShowDeleteConfirmation()
        {
            showDeleteConfirmation = true;

            string questionText = TranslationCache.DeleteConfirmQuestion;
            Vector2 questionSize = Game1.dialogueFont.MeasureString(questionText);

            int yesWidth = UIHelpers.CalculateButtonWidth(TranslationCache.DeleteConfirmYes);
            int noWidth = UIHelpers.CalculateButtonWidth(TranslationCache.DeleteConfirmNo);
            int buttonSpacing = 20;
            int totalButtonsWidth = yesWidth + buttonSpacing + noWidth;

            int topPadding = 28;
            int questionToButtons = 30;
            int bottomPadding = 24;

            int dialogWidth = Math.Max((int)questionSize.X + 60, totalButtonsWidth + 60);
            int dialogHeight = topPadding + (int)questionSize.Y + questionToButtons + TabAndButtonHeight + bottomPadding;
            int dialogX = (Game1.uiViewport.Width - dialogWidth) / 2;
            int dialogY = (Game1.uiViewport.Height - dialogHeight) / 2;
            deleteDialogBounds = new Rectangle(dialogX, dialogY, dialogWidth, dialogHeight);

            int buttonsY = dialogY + topPadding + (int)questionSize.Y + questionToButtons;
            int buttonsStartX = dialogX + (dialogWidth - totalButtonsWidth) / 2;

            deleteYesButton = new ClickableComponent(
                new Rectangle(buttonsStartX, buttonsY, yesWidth, TabAndButtonHeight),
                "yes"
            );

            deleteNoButton = new ClickableComponent(
                new Rectangle(buttonsStartX + yesWidth + buttonSpacing, buttonsY, noWidth, TabAndButtonHeight),
                "no"
            );
        }

        private void ConfirmDelete()
        {
            if (SelectedSet != null)
            {
                store.Delete(SelectedSet.Id);
                selectedSetId = null;
                RefreshDisplayedSets();
                Game1.playSound("trashcan");
            }
            HideDeleteConfirmation();
        }

        private void HideDeleteConfirmation()
        {
            showDeleteConfirmation = false;
            deleteYesButton = null;
            deleteNoButton = null;
        }

        private void HandleDeleteConfirmationClick(int x, int y, bool playSound)
        {
            if (deleteYesButton != null && deleteYesButton.containsPoint(x, y))
            {
                ConfirmDelete();
                return;
            }

            if (deleteNoButton != null && deleteNoButton.containsPoint(x, y))
            {
                HideDeleteConfirmation();
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }
        }

        public override void receiveKeyPress(Keys key)
        {
            if (showDeleteConfirmation)
            {
                if (key == Keys.Escape)
                {
                    HideDeleteConfirmation();
                    Game1.playSound("bigDeSelect");
                    return;
                }

                if (key == Keys.Enter)
                {
                    ConfirmDelete();
                    return;
                }

                return;
            }

            if (searchScopeOpen || tagsDropdownOpen || filterDropdownOpen)
            {
                if (key == Keys.Escape)
                {
                    CloseAllDropdowns();
                    Game1.playSound("bigDeSelect");
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
            }
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

            // Outfit list is in the right panel
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

            parentMenu?.HandleItemInfoToggle();

            if (searchTextBox != null)
            {
                searchTextBox.Update();

                if (!showDeleteConfirmation)
                    searchTextBox.Selected = true;

                if (searchTextBox.Text != filterState.SearchText)
                {
                    filterState.SearchText = searchTextBox.Text;
                    RefreshDisplayedSets();
                }
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

            try
            {
                Game1.player.currentEyes = 0;

                if (set != null)
                {
                    if (!string.IsNullOrEmpty(set.ShirtId) && store.IsItemValid(set.ShirtId, "(S)"))
                        Game1.player.shirtItem.Value = ItemRegistry.Create<Clothing>("(S)" + set.ShirtId);
                    else
                        Game1.player.shirtItem.Value = null;

                    if (!string.IsNullOrEmpty(set.PantsId) && store.IsItemValid(set.PantsId, "(P)"))
                        Game1.player.pantsItem.Value = ItemRegistry.Create<Clothing>("(P)" + set.PantsId);
                    else
                        Game1.player.pantsItem.Value = null;

                    if (!string.IsNullOrEmpty(set.HatId) && store.IsItemValid(set.HatId, "(H)"))
                        Game1.player.hat.Value = ItemRegistry.Create<Hat>("(H)" + set.HatId);
                    else
                        Game1.player.hat.Value = null;
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
                Game1.player.currentEyes = originalEyes;
                Game1.player.FarmerRenderer.MarkSpriteDirty();
            }

            Game1.graphics.GraphicsDevice.SetRenderTargets(renderTargets);
        }

        private void DrawItemSprites(SpriteBatch b)
        {
            if (cachedHat != null)
            {
                Rectangle slot = uiBuilder.HatSlot;
                Vector2 drawPos = new Vector2(
                    slot.X + (slot.Width - DrawnItemSize) / 2,
                    slot.Y + (slot.Height - DrawnItemSize) / 2
                );
                cachedHat.drawInMenu(b, drawPos, 1f, 1f, 0.9f, StackDrawType.Hide);
            }

            if (cachedShirt != null)
            {
                Rectangle slot = uiBuilder.ShirtSlot;
                Vector2 drawPos = new Vector2(
                    slot.X + (slot.Width - DrawnItemSize) / 2,
                    slot.Y + (slot.Height - DrawnItemSize) / 2
                );
                cachedShirt.drawInMenu(b, drawPos, 1f, 1f, 0.9f, StackDrawType.Hide);
            }

            if (cachedPants != null)
            {
                Rectangle slot = uiBuilder.PantsSlot;
                Vector2 drawPos = new Vector2(
                    slot.X + (slot.Width - DrawnItemSize) / 2,
                    slot.Y + (slot.Height - DrawnItemSize) / 2
                );
                cachedPants.drawInMenu(b, drawPos, 1f, 1f, 0.9f, StackDrawType.Hide);
            }
        }

        private void DrawDeleteConfirmation(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
                Color.Black * 0.2f);

            UIHelpers.DrawTextureBox(b, deleteDialogBounds.X, deleteDialogBounds.Y,
                deleteDialogBounds.Width, deleteDialogBounds.Height, Color.White);

            string questionText = TranslationCache.DeleteConfirmQuestion;
            Vector2 textSize = Game1.dialogueFont.MeasureString(questionText);
            Vector2 textPos = new Vector2(
                deleteDialogBounds.X + (deleteDialogBounds.Width - textSize.X) / 2,
                deleteDialogBounds.Y + 28
            );
            Utility.drawTextWithShadow(b, questionText, Game1.dialogueFont, textPos, Game1.textColor);

            if (deleteYesButton != null)
                DrawDeleteDialogButton(b, deleteYesButton, TranslationCache.DeleteConfirmYes, Color.LightGreen);

            if (deleteNoButton != null)
                DrawDeleteDialogButton(b, deleteNoButton, TranslationCache.DeleteConfirmNo, Color.Red);
        }

        private void DrawDeleteDialogButton(SpriteBatch b, ClickableComponent button, string label, Color hoverColor)
        {
            bool isHovered = button.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            UIHelpers.DrawTextureBox(b, button.bounds.X, button.bounds.Y,
                button.bounds.Width, button.bounds.Height, Color.White);

            if (isHovered)
                b.Draw(Game1.staminaRect, button.bounds, hoverColor * 0.2f);

            Vector2 textSize = Game1.smallFont.MeasureString(label);
            Vector2 textPos = UIHelpers.GetVisualCenter(button.bounds, textSize);

            if (isHovered)
            {
                Utility.drawTextWithShadow(b, label, Game1.smallFont, textPos + new Vector2(-1, 0), Game1.textColor * 0.8f);
                Utility.drawTextWithShadow(b, label, Game1.smallFont, textPos, Game1.textColor);
            }
            else
            {
                Utility.drawTextWithShadow(b, label, Game1.smallFont, textPos, Game1.textColor);
            }
        }

        private void DrawItemTooltips(SpriteBatch b, int mouseX, int mouseY)
        {
            if (parentMenu != null && !parentMenu.ShowItemInfo)
                return;

            if (uiBuilder.ShirtSlot.Contains(mouseX, mouseY) && cachedShirt != null)
            {
                IClickableMenu.drawToolTip(b, cachedShirt.getDescription(), cachedShirt.DisplayName, cachedShirt);
            }
            else if (uiBuilder.PantsSlot.Contains(mouseX, mouseY) && cachedPants != null)
            {
                IClickableMenu.drawToolTip(b, cachedPants.getDescription(), cachedPants.DisplayName, cachedPants);
            }
            else if (uiBuilder.HatSlot.Contains(mouseX, mouseY) && cachedHat != null)
            {
                IClickableMenu.drawToolTip(b, cachedHat.getDescription(), cachedHat.DisplayName, cachedHat);
            }
        }

        protected override void cleanupBeforeExit()
        {
            base.cleanupBeforeExit();

            if (previewRenderTarget != null && !previewRenderTarget.IsDisposed)
            {
                previewRenderTarget.Dispose();
                previewRenderTarget = null;
            }

            if (previewSpriteBatch != null && !previewSpriteBatch.IsDisposed)
            {
                previewSpriteBatch.Dispose();
                previewSpriteBatch = null;
            }
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
                Color.Black * BackgroundOverlayOpacity);

            uiBuilder.DrawBackground(b);
            uiBuilder.DrawLeftPanelBox(b);
            uiBuilder.DrawRightPanelBox(b);

            string searchText = searchTextBox?.Text ?? "";
            bool searchFocused = searchTextBox?.Selected ?? false;

            // Right panel: outfit set list
            uiBuilder.DrawOutfitSetList(b, displayedSets, selectedIndex, listScrollOffset, filterState,
                searchScopeOpen, tagsDropdownOpen, filterDropdownOpen, searchText, allTags, searchFocused);

            // Left panel: character preview
            if (previewDirty)
            {
                RenderPreviewToTarget(SelectedSet);
                previewDirty = false;
            }
            uiBuilder.DrawPreviewPanel(b, SelectedSet, store, previewRenderTarget, displayedSets.Count);
            uiBuilder.DrawArrows(b);
            DrawItemSprites(b);

            uiBuilder.DrawButtons(b, SelectedSet != null);
            uiBuilder.DrawCloseButton(b);

            string? tagsDropdownTooltip = null;
            if (searchScopeOpen)
            {
                uiBuilder.DrawSearchScopeDropdown(b);
            }
            else if (tagsDropdownOpen)
            {
                tagsDropdownTooltip = uiBuilder.DrawTagsDropdown(b, filterState.SelectedTags);
            }
            else if (filterDropdownOpen)
            {
                uiBuilder.DrawFilterDropdown(b, filterState);
            }

            if (!searchScopeOpen && !tagsDropdownOpen && !filterDropdownOpen && !showDeleteConfirmation)
            {
                int mouseX = Game1.getMouseX();
                int mouseY = Game1.getMouseY();
                DrawItemTooltips(b, mouseX, mouseY);

                if (uiBuilder.HoveredTruncatedSetName != null && ModEntry.Config.ShowTooltip)
                {
                    string wrapped = Game1.parseText(uiBuilder.HoveredTruncatedSetName, Game1.smallFont, 300);
                    IClickableMenu.drawHoverText(b, wrapped, Game1.smallFont);
                }
                else if (uiBuilder.TagsTextTruncated && SelectedSet != null
                    && uiBuilder.TagsTextBounds.Contains(mouseX, mouseY))
                {
                    string fullTags = string.Join(", ", SelectedSet.Tags);
                    string wrapped = Game1.parseText(fullTags, Game1.smallFont, 300);
                    IClickableMenu.drawHoverText(b, wrapped, Game1.smallFont);
                }
            }

            if (tagsDropdownTooltip != null)
            {
                string wrapped = Game1.parseText(tagsDropdownTooltip, Game1.smallFont, 300);
                IClickableMenu.drawHoverText(b, wrapped, Game1.smallFont);
            }
            else if (!searchScopeOpen && !showDeleteConfirmation && filterState.SelectedTags.Count > 0)
            {
                int mouseX = Game1.getMouseX();
                int mouseY = Game1.getMouseY();
                if (uiBuilder.TagsDropdown.containsPoint(mouseX, mouseY))
                {
                    string tagsList = string.Join(", ", filterState.SelectedTags);
                    string wrapped = Game1.parseText(tagsList, Game1.smallFont, 300);
                    IClickableMenu.drawHoverText(b, wrapped, Game1.smallFont);
                }
            }

            if (showDeleteConfirmation)
            {
                DrawDeleteConfirmation(b);
            }

            drawMouse(b);
        }
    }
}
