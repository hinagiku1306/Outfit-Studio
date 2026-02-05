using System;
using System.Collections.Generic;
using FittingRoom.Managers;
using FittingRoom.Models;
using FittingRoom.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using static FittingRoom.OutfitLayoutConstants;

namespace FittingRoom
{
    public class TemplatesOverlay : IClickableMenu
    {
        private readonly TemplatesUIBuilder uiBuilder;
        private readonly OutfitSetStore store;
        private readonly Action? onNewSetRequested;
        private bool shouldClose;

        private List<OutfitSet> displayedSets = new();
        private int selectedIndex = -1;
        private int listScrollOffset;
        private OutfitSet? SelectedSet => selectedIndex >= 0 && selectedIndex < displayedSets.Count
            ? displayedSets[selectedIndex]
            : null;

        private bool isRenaming;
        private TextBox? renameTextBox;
        private string originalName = "";

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

        private static readonly FarmerSprite.AnimationFrame FrontFacingFrame = new(0, 0, secondaryArm: false, flip: false);
        private const int PreviewFarmerScale = 4;

        public TemplatesOverlay(OutfitSetStore store, Action? onNewSetRequested = null)
        {
            this.store = store;
            this.onNewSetRequested = onNewSetRequested;

            width = TemplatesOverlayWidth;
            height = TemplatesOverlayHeight;

            uiBuilder = new TemplatesUIBuilder(width, height);
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;

            CreateSearchTextBox();
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
                Selected = false
            };
            UpdateSearchTextBoxBounds();
        }

        private void UpdateSearchTextBoxBounds()
        {
            if (searchTextBox == null)
                return;

            Rectangle bounds = uiBuilder.SearchBar.bounds;
            searchTextBox.X = bounds.X + 8;
            searchTextBox.Y = bounds.Y + ((bounds.Height - 48) / 2);
            searchTextBox.Width = bounds.Width - 16;
        }

        private void RefreshDisplayedSets()
        {
            allTags = store.GetAllTags();
            displayedSets = store.GetFilteredSets(filterState);

            if (selectedIndex >= displayedSets.Count)
                selectedIndex = displayedSets.Count - 1;

            if (selectedIndex < 0 && displayedSets.Count > 0)
                selectedIndex = 0;

            ClampListScroll();
            MarkPreviewDirty();
        }

        private void ClampListScroll()
        {
            int maxVisible = uiBuilder.TemplateListItems.Count;
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
            }
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
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;
            UpdateSearchTextBoxBounds();

            if (isRenaming)
            {
                CancelRename();
            }

            CloseAllDropdowns();
        }

        public override bool readyToClose() => shouldClose;

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (isRenaming)
            {
                HandleRenameClick(x, y, playSound);
                return;
            }

            if (HandleDropdownClick(x, y, playSound))
                return;

            if (HandleFilterBarClick(x, y, playSound))
                return;

            if (uiBuilder.CloseButton.containsPoint(x, y))
            {
                shouldClose = true;
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            for (int i = 0; i < uiBuilder.TemplateListItems.Count; i++)
            {
                int dataIndex = listScrollOffset + i;
                if (dataIndex >= displayedSets.Count)
                    break;

                if (uiBuilder.TemplateListItems[i].containsPoint(x, y))
                {
                    selectedIndex = dataIndex;
                    MarkPreviewDirty();
                    if (playSound) Game1.playSound("smallSelect");
                    return;
                }
            }

            if (uiBuilder.LoadButton.containsPoint(x, y))
            {
                if (SelectedSet != null)
                {
                    store.ApplySet(SelectedSet);
                    if (playSound) Game1.playSound("coin");
                }
                return;
            }

            if (uiBuilder.ApplyCloseButton.containsPoint(x, y))
            {
                if (SelectedSet != null)
                {
                    store.ApplySet(SelectedSet);
                    if (playSound) Game1.playSound("coin");
                }
                shouldClose = true;
                return;
            }

            if (uiBuilder.NewButton.containsPoint(x, y))
            {
                if (onNewSetRequested != null)
                {
                    shouldClose = true;
                    onNewSetRequested();
                }
                if (playSound) Game1.playSound("bigSelect");
                return;
            }

            if (uiBuilder.RenameButton.containsPoint(x, y))
            {
                if (SelectedSet != null)
                {
                    StartRename();
                    if (playSound) Game1.playSound("smallSelect");
                }
                return;
            }

            if (uiBuilder.FavoriteButton.containsPoint(x, y))
            {
                if (SelectedSet != null)
                {
                    SelectedSet.IsFavorite = !SelectedSet.IsFavorite;
                    store.Update(SelectedSet);
                    RefreshDisplayedSets();
                    if (playSound) Game1.playSound("smallSelect");
                }
                return;
            }

            if (uiBuilder.DeleteButton.containsPoint(x, y))
            {
                if (SelectedSet != null)
                {
                    store.Delete(SelectedSet.Id);
                    RefreshDisplayedSets();
                    if (playSound) Game1.playSound("trashcan");
                }
                return;
            }
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
                CloseAllDropdowns();
                tagsDropdownOpen = true;
                uiBuilder.BuildTagsOptions(allTags, filterState.SelectedTags);
                if (playSound) Game1.playSound("smallSelect");
                return true;
            }

            if (uiBuilder.FilterDropdown.containsPoint(x, y))
            {
                CloseAllDropdowns();
                filterDropdownOpen = true;
                uiBuilder.BuildFilterOptions(filterState);
                if (playSound) Game1.playSound("smallSelect");
                return true;
            }

            Rectangle matchAllArea = new Rectangle(
                uiBuilder.MatchAllCheckbox.bounds.X,
                uiBuilder.MatchAllCheckbox.bounds.Y,
                uiBuilder.MatchAllCheckbox.bounds.Width + (int)Game1.smallFont.MeasureString(TranslationCache.TemplatesFilterMatchAll).X + 8,
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
                uiBuilder.ShowInvalidCheckbox.bounds.Width + (int)Game1.smallFont.MeasureString(TranslationCache.TemplatesFilterShowInvalid).X + 8,
                uiBuilder.ShowInvalidCheckbox.bounds.Height
            );
            if (showInvalidArea.Contains(x, y))
            {
                filterState.ShowInvalid = !filterState.ShowInvalid;
                RefreshDisplayedSets();
                if (playSound) Game1.playSound("smallSelect");
                return true;
            }

            return false;
        }

        private void HandleRenameClick(int x, int y, bool playSound)
        {
            if (uiBuilder.RenameConfirmButton != null && uiBuilder.RenameConfirmButton.containsPoint(x, y))
            {
                ConfirmRename();
                if (playSound) Game1.playSound("coin");
                return;
            }

            if (uiBuilder.RenameCancelButton != null && uiBuilder.RenameCancelButton.containsPoint(x, y))
            {
                CancelRename();
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            if (uiBuilder.RenameInputArea != null && uiBuilder.RenameInputArea.containsPoint(x, y))
            {
                renameTextBox!.Selected = true;
                return;
            }
        }

        private void StartRename()
        {
            if (SelectedSet == null)
                return;

            isRenaming = true;
            originalName = SelectedSet.Name;

            renameTextBox = new TextBox(
                Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                null,
                Game1.smallFont,
                Game1.textColor)
            {
                Text = SelectedSet.Name,
                Selected = true
            };

            uiBuilder.SetupRenameMode(true);
            UpdateRenameTextBoxBounds();
        }

        private void UpdateRenameTextBoxBounds()
        {
            if (renameTextBox == null || uiBuilder.RenameInputArea == null)
                return;

            Rectangle bounds = uiBuilder.RenameInputArea.bounds;
            renameTextBox.X = bounds.X + 12;
            renameTextBox.Y = bounds.Y + ((bounds.Height - 48) / 2);
            renameTextBox.Width = bounds.Width - 24;
        }

        private void ConfirmRename()
        {
            if (SelectedSet == null || renameTextBox == null)
            {
                CancelRename();
                return;
            }

            string newName = renameTextBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(newName))
            {
                newName = originalName;
            }

            SelectedSet.Name = newName;
            store.Update(SelectedSet);
            RefreshDisplayedSets();

            CancelRename();
        }

        private void CancelRename()
        {
            isRenaming = false;
            renameTextBox = null;
            originalName = "";
            uiBuilder.SetupRenameMode(false);
        }

        public override void receiveKeyPress(Keys key)
        {
            if (isRenaming)
            {
                if (key == Keys.Escape)
                {
                    CancelRename();
                    Game1.playSound("bigDeSelect");
                    return;
                }

                if (key == Keys.Enter)
                {
                    ConfirmRename();
                    Game1.playSound("coin");
                    return;
                }

                return;
            }

            if (searchTextBox?.Selected == true)
            {
                if (key == Keys.Escape)
                {
                    searchTextBox.Selected = false;
                    return;
                }

                if (key == Keys.Enter)
                {
                    filterState.ForceApplySearch();
                    RefreshDisplayedSets();
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

            if (key == Keys.Escape || Game1.options.menuButton.Contains(new InputButton(key)))
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

            if (uiBuilder.LeftPanel.Contains(mouseX, mouseY))
            {
                int maxVisible = uiBuilder.TemplateListItems.Count;
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

            if (isRenaming && renameTextBox != null)
            {
                renameTextBox.Update();
                renameTextBox.Selected = true;
            }

            if (searchTextBox != null)
            {
                searchTextBox.Update();

                if (searchTextBox.Text != filterState.GetPendingSearchText())
                {
                    filterState.SetSearchText(searchTextBox.Text, time.TotalGameTime.TotalMilliseconds);
                }

                if (filterState.UpdateSearchDebounce(time.TotalGameTime.TotalMilliseconds))
                {
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
                    TemplatesPreviewSize,
                    TemplatesPreviewSize,
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

        private void RenderPreviewToTarget(OutfitSet set)
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

                Game1.player.FarmerRenderer.MarkSpriteDirty();

                int baseY = Game1.player.bathingClothes.Value ? 576 : 0;
                Rectangle sourceRect = new Rectangle(0, baseY, FarmerSpriteWidth, FarmerSpriteHeight);

                int scaledWidth = FarmerSpriteWidth * PreviewFarmerScale;
                int scaledHeight = FarmerSpriteHeight * PreviewFarmerScale;
                Vector2 centeredPosition = new Vector2(
                    (TemplatesPreviewSize - scaledWidth) / 2,
                    (TemplatesPreviewSize - scaledHeight) / 2 + 16
                );

                previewSpriteBatch!.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

                FarmerRenderer.isDrawingForUI = true;
                Game1.player.FarmerRenderer.draw(
                    previewSpriteBatch,
                    FrontFacingFrame,
                    0,
                    sourceRect,
                    centeredPosition,
                    Vector2.Zero,
                    0.8f,
                    2,
                    Color.White,
                    0f,
                    1f,
                    Game1.player
                );

                if (Game1.timeOfDay >= NightTimeStartHour)
                {
                    Game1.player.FarmerRenderer.draw(
                        previewSpriteBatch,
                        FrontFacingFrame,
                        0,
                        sourceRect,
                        centeredPosition,
                        Vector2.Zero,
                        0.8f,
                        2,
                        Color.DarkBlue * 0.3f,
                        0f,
                        1f,
                        Game1.player
                    );
                }
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

            Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height,
                speaker: false, drawOnlyBox: true);

            Vector2 titleSize = Game1.dialogueFont.MeasureString(TranslationCache.TemplatesTitle);
            Utility.drawTextWithShadow(b, TranslationCache.TemplatesTitle, Game1.dialogueFont,
                new Vector2(xPositionOnScreen + (width - titleSize.X) / 2, yPositionOnScreen + ContentBoxPadding),
                Game1.textColor);

            string searchText = searchTextBox?.Text ?? "";
            uiBuilder.DrawLeftPanel(b, displayedSets, selectedIndex, listScrollOffset, filterState,
                searchScopeOpen, tagsDropdownOpen, filterDropdownOpen, searchText, allTags);

            if (isRenaming)
            {
                uiBuilder.DrawRenamePanel(b, renameTextBox?.Text ?? "", renameTextBox?.Selected ?? false);
            }
            else
            {
                if (SelectedSet != null && previewDirty)
                {
                    RenderPreviewToTarget(SelectedSet);
                    previewDirty = false;
                }
                uiBuilder.DrawRightPanel(b, SelectedSet, store, previewRenderTarget);
            }

            uiBuilder.DrawButtons(b, SelectedSet);
            uiBuilder.DrawCloseButton(b);

            if (searchScopeOpen)
            {
                uiBuilder.DrawSearchScopeDropdown(b, filterState.SearchScope);
            }
            else if (tagsDropdownOpen)
            {
                uiBuilder.DrawTagsDropdown(b, filterState.SelectedTags);
            }
            else if (filterDropdownOpen)
            {
                uiBuilder.DrawFilterDropdown(b, filterState);
            }

            drawMouse(b);
        }
    }
}
