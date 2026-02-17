using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OutfitStudio.Models;
using OutfitStudio.Services;
using StardewValley;
using StardewValley.Menus;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    public class ScheduleMenu : IClickableMenu
    {
        private static readonly RasterizerState ScissorEnabled = new RasterizerState { ScissorTestEnable = true };

        private readonly IClickableMenu? parentMenu;
        private readonly ScheduleMenuUIBuilder uiBuilder;
        private readonly ModEntry mod;
        private readonly OutfitSetStore outfitSetStore;
        private readonly ScheduleStore scheduleStore;

        private bool masterEnabled;
        private List<RuleState> ruleStates = new();

        // Priority filter: null = all, label string = filtered
        private string? selectedPriorityLabel;
        private bool priorityDropdownOpen;
        private readonly List<RuleState> displayedRules = new();

        private TextBox? searchTextBox;
        private bool searchBarFocused;
        private string lastSearchText = "";
        private readonly ContinuousScrollHandler scrollHandler = new();

        public bool IsOverlayBlocking { get; set; }

        // Delete confirmation
        private bool showDeleteConfirmation;
        private int pendingDeleteIndex = -1;
        private Rectangle deleteDialogBounds;
        private ClickableComponent? deleteYesButton;
        private ClickableComponent? deleteNoButton;

        public ScheduleMenu(IClickableMenu? parentMenu, ModEntry mod, OutfitSetStore outfitSetStore, ScheduleStore scheduleStore)
        {
            this.parentMenu = parentMenu;
            this.mod = mod;
            this.outfitSetStore = outfitSetStore;
            this.scheduleStore = scheduleStore;

            uiBuilder = new ScheduleMenuUIBuilder();

            LoadStateFromStore();
            RebuildDisplayedRules();

            CreateSearchTextBox();

            width = uiBuilder.Width;
            height = uiBuilder.Height;
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;
        }

        private void CreateSearchTextBox()
        {
            searchBarFocused = ModEntry.Config.AutoFocusSearchBar;

            searchTextBox = new TextBox(
                Game1.content.Load<Microsoft.Xna.Framework.Graphics.Texture2D>("LooseSprites\\textBox"), null,
                Game1.smallFont, Game1.textColor)
            { Text = "", Selected = searchBarFocused };

            if (searchBarFocused)
                Game1.keyboardDispatcher.Subscriber = searchTextBox;

            UpdateSearchTextBoxBounds();
        }

        private void UpdateSearchTextBoxBounds()
        {
            if (searchTextBox == null) return;
            var bar = uiBuilder.SearchBar.bounds;
            searchTextBox.X = bar.X;
            searchTextBox.Y = bar.Y;
            searchTextBox.Width = bar.Width - ClearButtonSize - 16;
            searchTextBox.Height = bar.Height;

            uiBuilder.SearchClearButton.bounds = new Rectangle(
                bar.Right - ClearButtonRightMargin - ClearButtonSize,
                bar.Y + (bar.Height - ClearButtonSize) / 2,
                ClearButtonSize, ClearButtonSize);
        }

        private void LoadStateFromStore()
        {
            masterEnabled = scheduleStore.IsEnabled;
            ruleStates.Clear();

            var allSets = outfitSetStore.GetAllSets();

            foreach (var rule in scheduleStore.GetRules())
            {
                ruleStates.Add(new RuleState
                {
                    Rule = rule,
                    TotalOutfits = ScheduleEngine.ResolvePool(rule, allSets).Count
                });
            }

            ruleStates.Sort((a, b) =>
                string.Compare(a.Rule.Name, b.Rule.Name, StringComparison.OrdinalIgnoreCase));

            for (int i = 0; i < ruleStates.Count; i++)
                ruleStates[i].OriginalIndex = i + 1;
        }

        private void RebuildDisplayedRules()
        {
            string searchText = searchTextBox?.Text ?? "";

            displayedRules.Clear();
            foreach (var rs in ruleStates)
            {
                if (!MatchesPriorityFilter(rs.Rule))
                    continue;

                if (string.IsNullOrEmpty(searchText)
                    || rs.Rule.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    displayedRules.Add(rs);
            }

            uiBuilder.ScrollOffset = 0;
            uiBuilder.Recalculate(displayedRules.Count, ruleStates.Count);
            UpdateSearchTextBoxBounds();
        }

        private bool MatchesPriorityFilter(ScheduleRule rule)
        {
            if (selectedPriorityLabel == null)
                return true;

            int p = rule.EffectivePriority;

            if (selectedPriorityLabel == TranslationCache.ScheduleEditPrioritySpecial)
                return p >= ScheduleRule.PrioritySpecial;
            if (selectedPriorityLabel == TranslationCache.ScheduleEditPriorityHigh)
                return p == 3;
            if (selectedPriorityLabel == TranslationCache.ScheduleEditPriorityMedium)
                return p == 2;
            if (selectedPriorityLabel == TranslationCache.ScheduleEditPriorityLow)
                return p <= 1;

            return true;
        }

        private void SelectPriority(string? label)
        {
            selectedPriorityLabel = label;
            priorityDropdownOpen = false;
            RebuildDisplayedRules();
        }

        public void RefreshFromStore()
        {
            LoadStateFromStore();
            RebuildDisplayedRules();
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            if (showDeleteConfirmation)
            {
                HandleDeleteConfirmationClick(x, y, playSound);
                return;
            }

            if (uiBuilder.CloseButton.containsPoint(x, y))
            {
                CloseOverlay();
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            if (ModEntry.Config.ShowScheduleDebugLog && uiBuilder.DebugLogButton.containsPoint(x, y))
            {
                var evalLog = mod.GetScheduleEvalLog();
                if (evalLog != null)
                {
                    Game1.activeClickableMenu = new ScheduleDebugLogOverlay(Game1.activeClickableMenu, evalLog);
                    if (playSound) Game1.playSound("bigSelect");
                }
                return;
            }

            if (!isWithinBounds(x, y) && ModEntry.Config.CloseOnClickOutside)
            {
                bool clickedDropdownPanel = priorityDropdownOpen &&
                    uiBuilder.PriorityDropdownOptions.Any(o => o.containsPoint(x, y));
                if (!clickedDropdownPanel)
                {
                    CloseOverlay();
                    if (playSound) Game1.playSound("bigDeSelect");
                    return;
                }
            }

            // Master toggle
            if (uiBuilder.MasterStatusButton.containsPoint(x, y))
            {
                masterEnabled = !masterEnabled;
                scheduleStore.IsEnabled = masterEnabled;
                scheduleStore.SaveLocalData();
                if (!masterEnabled)
                {
                    priorityDropdownOpen = false;
                    searchBarFocused = false;
                    if (searchTextBox != null)
                        searchTextBox.Selected = false;
                }
                if (playSound) Game1.playSound("drumkit6");
                return;
            }

            // When master is disabled, only Close + floating debug log work (handled above)
            if (!masterEnabled)
            {
                if (uiBuilder.CloseMenuButton.containsPoint(x, y))
                {
                    CloseOverlay();
                    if (playSound) Game1.playSound("bigDeSelect");
                }
                return;
            }

            // --- Dropdown open: handle options and blocking ---
            if (priorityDropdownOpen)
            {
                // Bottom buttons always work
                if (masterEnabled && uiBuilder.NewButton.containsPoint(x, y))
                {
                    priorityDropdownOpen = false;
                    OpenCreateOverlay();
                    if (playSound) Game1.playSound("bigSelect");
                    return;
                }
                if (uiBuilder.CloseMenuButton.containsPoint(x, y))
                {
                    priorityDropdownOpen = false;
                    CloseOverlay();
                    if (playSound) Game1.playSound("bigDeSelect");
                    return;
                }

                // Search clear button still works
                if (!string.IsNullOrEmpty(searchTextBox?.Text) && uiBuilder.SearchClearButton.containsPoint(x, y))
                {
                    searchTextBox!.Text = "";
                    lastSearchText = "";
                    priorityDropdownOpen = false;
                    RebuildDisplayedRules();
                    if (playSound) Game1.playSound("smallSelect");
                    return;
                }

                // Priority clear button still works
                if (selectedPriorityLabel != null && uiBuilder.PriorityClearButton.containsPoint(x, y))
                {
                    priorityDropdownOpen = false;
                    SelectPriority(null);
                    if (playSound) Game1.playSound("smallSelect");
                    return;
                }

                // Clicking the priority dropdown bar toggles off
                if (uiBuilder.PriorityDropdown.containsPoint(x, y))
                {
                    priorityDropdownOpen = false;
                    if (playSound) Game1.playSound("smallSelect");
                    return;
                }

                // Dropdown option clicks
                foreach (var option in uiBuilder.PriorityDropdownOptions)
                {
                    if (option.containsPoint(x, y))
                    {
                        SelectPriority(option.name);
                        if (playSound) Game1.playSound("smallSelect");
                        return;
                    }
                }

                // Click anywhere else closes dropdown (block)
                priorityDropdownOpen = false;
                return;
            }

            // --- Normal (dropdown closed) input ---

            // Priority clear button
            if (selectedPriorityLabel != null && uiBuilder.PriorityClearButton.containsPoint(x, y))
            {
                SelectPriority(null);
                if (playSound) Game1.playSound("smallSelect");
                return;
            }

            // Priority dropdown bar
            if (uiBuilder.PriorityDropdown.containsPoint(x, y))
            {
                priorityDropdownOpen = true;
                if (playSound) Game1.playSound("smallSelect");
                return;
            }

            // Search clear button
            if (!string.IsNullOrEmpty(searchTextBox?.Text) && uiBuilder.SearchClearButton.containsPoint(x, y))
            {
                searchTextBox!.Text = "";
                lastSearchText = "";
                RebuildDisplayedRules();
                if (playSound) Game1.playSound("smallSelect");
                return;
            }

            // Rule list interactions
            if (uiBuilder.RuleListClipRect.Contains(x, y))
            {
                for (int i = 0; i < displayedRules.Count; i++)
                {
                    if (i >= uiBuilder.RuleEditButtons.Count)
                        break;

                    var editBtn = uiBuilder.RuleEditButtons[i];
                    var deleteBtn = uiBuilder.RuleDeleteButtons[i];
                    var nameArea = uiBuilder.RuleNameAreas[i];
                    int rowY = nameArea.bounds.Y;

                    if (rowY < uiBuilder.RuleListClipRect.Y
                        || rowY + ScheduleRuleRowHeight > uiBuilder.RuleListClipRect.Bottom)
                        continue;

                    if (editBtn.containsPoint(x, y))
                    {
                        OpenEditOverlay(displayedRules[i].Rule.Id);
                        if (playSound) Game1.playSound("bigSelect");
                        return;
                    }

                    if (deleteBtn.containsPoint(x, y))
                    {
                        ShowDeleteConfirmation(i);
                        if (playSound) Game1.playSound("smallSelect");
                        return;
                    }

                    var checkbox = uiBuilder.RuleCheckboxes[i];
                    if (y >= rowY && y < rowY + ScheduleRuleRowHeight
                        && x >= checkbox.bounds.X && x < editBtn.bounds.X)
                    {
                        displayedRules[i].Rule.IsEnabled = !displayedRules[i].Rule.IsEnabled;
                        scheduleStore.UpdateRule(displayedRules[i].Rule);
                        scheduleStore.SaveLocalData();
                        if (playSound) Game1.playSound("drumkit6");
                        return;
                    }
                }
            }

            // New button
            if (uiBuilder.NewButton.containsPoint(x, y))
            {
                OpenCreateOverlay();
                if (playSound) Game1.playSound("bigSelect");
                return;
            }

            // Close button
            if (uiBuilder.CloseMenuButton.containsPoint(x, y))
            {
                CloseOverlay();
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
                }
                return;
            }

            if (priorityDropdownOpen)
            {
                if (key == Keys.Escape)
                {
                    priorityDropdownOpen = false;
                    Game1.playSound("smallSelect");
                }
                return;
            }

            if (ModEntry.Config.ArrowKeyScrolling && masterEnabled && uiBuilder.IsScrollable)
            {
                int stride = ScheduleRuleRowHeight + ScheduleItemGap;
                int visibleRows = Math.Max(1, uiBuilder.RuleListClipRect.Height / stride);

                int scrollAmount = key switch
                {
                    Keys.Up => -stride,
                    Keys.Down => stride,
                    Keys.Left => -stride * visibleRows,
                    Keys.Right => stride * visibleRows,
                    _ => 0
                };

                if (scrollAmount != 0)
                {
                    int oldOffset = uiBuilder.ScrollOffset;
                    uiBuilder.ScrollOffset += scrollAmount;
                    uiBuilder.ClampScrollOffset();
                    if (uiBuilder.ScrollOffset != oldOffset)
                    {
                        uiBuilder.Recalculate(displayedRules.Count, ruleStates.Count);
                        UpdateSearchTextBoxBounds();
                        Game1.playSound("shiny4");
                    }
                    return;
                }
            }

            if (key == Keys.Escape)
            {
                CloseOverlay();
                Game1.playSound("bigDeSelect");
                return;
            }

            if (Game1.options.doesInputListContain(Game1.options.menuButton, key))
                return;
        }

        public override void receiveScrollWheelAction(int direction)
        {
            if (!uiBuilder.IsScrollable || priorityDropdownOpen || showDeleteConfirmation)
                return;

            int oldOffset = uiBuilder.ScrollOffset;
            int stride = ScheduleRuleRowHeight + ScheduleItemGap;

            if (direction > 0)
                uiBuilder.ScrollOffset -= stride;
            else if (direction < 0)
                uiBuilder.ScrollOffset += stride;

            uiBuilder.ClampScrollOffset();

            if (uiBuilder.ScrollOffset != oldOffset)
            {
                uiBuilder.Recalculate(displayedRules.Count, ruleStates.Count);
                UpdateSearchTextBoxBounds();
                Game1.playSound("shiny4");
            }
        }

        public override void update(GameTime time)
        {
            base.update(time);

            if (searchTextBox == null) return;

            if (!masterEnabled)
            {
                searchTextBox.Selected = false;
                scrollHandler.Reset();
                return;
            }

            searchTextBox.Update();
            searchTextBox.Selected = ModEntry.Config.AutoFocusSearchBar || searchBarFocused;

            if (searchTextBox.Text != lastSearchText)
            {
                lastSearchText = searchTextBox.Text;
                RebuildDisplayedRules();
            }

            if (ModEntry.Config.ArrowKeyScrolling && !showDeleteConfirmation
                && !priorityDropdownOpen && uiBuilder.IsScrollable)
            {
                int stride = ScheduleRuleRowHeight + ScheduleItemGap;
                int visibleRows = Math.Max(1, uiBuilder.RuleListClipRect.Height / stride);
                int scrollAmount = scrollHandler.Update(time, visibleRows, out bool shouldPlaySound);
                if (scrollAmount != 0)
                {
                    int oldOffset = uiBuilder.ScrollOffset;
                    uiBuilder.ScrollOffset += scrollAmount * stride;
                    uiBuilder.ClampScrollOffset();
                    if (uiBuilder.ScrollOffset != oldOffset)
                    {
                        uiBuilder.Recalculate(displayedRules.Count, ruleStates.Count);
                        UpdateSearchTextBoxBounds();
                        if (shouldPlaySound) Game1.playSound("shiny4");
                    }
                }
            }
            else
            {
                scrollHandler.Reset();
            }
        }

        public override void draw(SpriteBatch b)
        {
            bool oldSuppressHover = UIHelpers.SuppressHover;
            UIHelpers.SuppressHover = true;

            if (parentMenu is OutfitMenu outfitMenu)
                outfitMenu.IsOverlayBlocking = true;

            parentMenu?.draw(b);

            if (parentMenu is OutfitMenu outfitMenuAfter)
                outfitMenuAfter.IsOverlayBlocking = false;

            UIHelpers.SuppressHover = oldSuppressHover;

            b.Draw(Game1.fadeToBlackRect,
                new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
                Color.Black * BackgroundOverlayOpacity);

            // Title box
            UIHelpers.DrawTextureBox(b, uiBuilder.TitleBoxBounds.X, uiBuilder.TitleBoxBounds.Y,
                uiBuilder.TitleBoxBounds.Width, uiBuilder.TitleBoxBounds.Height, Color.White);

            // Content box
            UIHelpers.DrawTextureBox(b, uiBuilder.ContentBoxBounds.X, uiBuilder.ContentBoxBounds.Y,
                uiBuilder.ContentBoxBounds.Width, uiBuilder.ContentBoxBounds.Height, Color.White);

            uiBuilder.DrawTitle(b);
            uiBuilder.DrawMasterToggle(b, masterEnabled);

            // Priority dropdown + search bar
            float filterOpacity = masterEnabled ? 1f : DisabledSectionOpacity;
            uiBuilder.DrawPriorityDropdown(b, selectedPriorityLabel, priorityDropdownOpen, filterOpacity);

            bool hasSearchText = !string.IsNullOrEmpty(searchTextBox?.Text);
            bool searchFocused = masterEnabled && (searchTextBox?.Selected ?? false);
            UIHelpers.DrawInputBar(b, uiBuilder.SearchBar.bounds,
                searchTextBox?.Text ?? "", searchFocused,
                placeholder: TranslationCache.WardrobeSearch,
                clearButton: masterEnabled && hasSearchText ? uiBuilder.SearchClearButton : null,
                opacity: filterOpacity);

            // Divider
            uiBuilder.DrawDivider(b);

            // Rule list with scissor
            string? hoveredTooltip = null;
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            if (priorityDropdownOpen)
                UIHelpers.SuppressHover = true;

            if (displayedRules.Count == 0)
            {
                uiBuilder.DrawNoRulesText(b, masterEnabled);
            }
            else if (uiBuilder.IsScrollable)
            {
                var oldScissorRect = b.GraphicsDevice.ScissorRectangle;
                b.End();
                b.GraphicsDevice.ScissorRectangle = uiBuilder.RuleListClipRect;
                b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, ScissorEnabled);

                hoveredTooltip = DrawRuleList(b, mouseX, mouseY);

                b.End();
                b.GraphicsDevice.ScissorRectangle = oldScissorRect;
                b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            }
            else
            {
                hoveredTooltip = DrawRuleList(b, mouseX, mouseY);
            }

            if (priorityDropdownOpen)
                UIHelpers.SuppressHover = false;

            uiBuilder.DrawScrollIndicators(b);
            uiBuilder.DrawButtons(b, masterEnabled);
            uiBuilder.DrawCloseButton(b);

            if (ModEntry.Config.ShowScheduleDebugLog)
                uiBuilder.DrawDebugLogButton(b);

            // Priority dropdown options (drawn on top)
            if (priorityDropdownOpen && masterEnabled)
            {
                uiBuilder.DrawPriorityDropdownOptions(b, selectedPriorityLabel);
            }

            if (!showDeleteConfirmation && !priorityDropdownOpen && hoveredTooltip != null && ModEntry.Config.ShowTooltip)
            {
                UIHelpers.DrawWrappedTooltip(b, hoveredTooltip);
            }

            if (showDeleteConfirmation)
                UIHelpers.DrawDeleteConfirmationDialog(b, deleteDialogBounds,
                    TranslationCache.ScheduleEditDeleteScheduleConfirm,
                    deleteYesButton!, TranslationCache.DeleteConfirmYes,
                    deleteNoButton!, TranslationCache.DeleteConfirmNo);

            drawMouse(b);
        }

        private string? DrawRuleList(SpriteBatch b, int mouseX, int mouseY)
        {
            string? tooltip = null;

            for (int i = 0; i < displayedRules.Count; i++)
            {
                bool isHovered = !IsOverlayBlocking && masterEnabled && !priorityDropdownOpen
                    && uiBuilder.RuleListClipRect.Contains(mouseX, mouseY)
                    && uiBuilder.GetRuleIndexAtPoint(mouseX, mouseY) == i;

                string? rowTooltip = uiBuilder.DrawRuleRow(b, i,
                    displayedRules[i].Rule.Name,
                    displayedRules[i].Rule.IsEnabled,
                    isHovered, masterEnabled,
                    displayedRules[i].Rule.AdvanceOnWarp);
                if (rowTooltip != null)
                    tooltip = rowTooltip;
            }

            return tooltip;
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            parentMenu?.gameWindowSizeChanged(oldBounds, newBounds);

            uiBuilder.Recalculate(displayedRules.Count, ruleStates.Count);
            UpdateSearchTextBoxBounds();
            width = uiBuilder.Width;
            height = uiBuilder.Height;
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;

            if (showDeleteConfirmation)
                (deleteDialogBounds, deleteYesButton, deleteNoButton) = UIHelpers.CalculateDeleteDialogLayout(
                    TranslationCache.ScheduleEditDeleteScheduleConfirm, TranslationCache.DeleteConfirmYes, TranslationCache.DeleteConfirmNo);
        }

        private void OpenEditOverlay(string ruleId)
        {
            var rule = scheduleStore.GetRuleById(ruleId);
            if (rule == null) return;

            Game1.activeClickableMenu = new ScheduleEditOverlay(
                this, scheduleStore, outfitSetStore, rule, RefreshFromStore);
        }

        private void OpenCreateOverlay()
        {
            Game1.activeClickableMenu = new ScheduleEditOverlay(
                this, scheduleStore, outfitSetStore, null, RefreshFromStore);
        }

        // --- Delete confirmation ---

        private void ShowDeleteConfirmation(int ruleIndex)
        {
            showDeleteConfirmation = true;
            pendingDeleteIndex = ruleIndex;
            (deleteDialogBounds, deleteYesButton, deleteNoButton) = UIHelpers.CalculateDeleteDialogLayout(
                TranslationCache.ScheduleEditDeleteScheduleConfirm, TranslationCache.DeleteConfirmYes, TranslationCache.DeleteConfirmNo);
        }

        private void HideDeleteConfirmation()
        {
            showDeleteConfirmation = false;
            pendingDeleteIndex = -1;
            deleteYesButton = null;
            deleteNoButton = null;
        }

        private void HandleDeleteConfirmationClick(int x, int y, bool playSound)
        {
            if (deleteYesButton != null && deleteYesButton.containsPoint(x, y))
            {
                if (pendingDeleteIndex >= 0 && pendingDeleteIndex < displayedRules.Count)
                {
                    scheduleStore.DeleteRule(displayedRules[pendingDeleteIndex].Rule.Id);
                    RefreshFromStore();
                    if (playSound) Game1.playSound("trashcan");
                }
                HideDeleteConfirmation();
                return;
            }

            if (deleteNoButton != null && deleteNoButton.containsPoint(x, y))
            {
                HideDeleteConfirmation();
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }
        }


        private void CloseOverlay()
        {
            if (parentMenu != null)
                Game1.activeClickableMenu = parentMenu;
            else
                Game1.exitActiveMenu();
        }

        private class RuleState
        {
            public ScheduleRule Rule { get; set; } = null!;
            public int TotalOutfits { get; set; }
            public int OriginalIndex { get; set; }
        }
    }
}
