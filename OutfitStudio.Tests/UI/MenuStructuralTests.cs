using OutfitStudio.Tests.Helpers;
using Xunit;

namespace OutfitStudio.Tests.UI
{
    public class MenuStructuralTests
    {
        // All menu/overlay source files that must follow structural contracts
        private static readonly string[] AllMenuFiles = new[]
        {
            "UI/OutfitMenu.cs",
            "UI/WardrobeOverlay.cs",
            "UI/SaveSetOverlay.cs",
            "UI/ConfigOverlay.cs",
            "UI/ScheduleMenu.cs",
            "UI/ScheduleEditOverlay.cs",
            "UI/ScheduleOutfitOverlay.cs",
            "UI/ScheduleDebugLogOverlay.cs",
        };

        // Type B overlays that swap Game1.activeClickableMenu and must forward resize to parent
        private static readonly string[] TypeBOverlayFiles = new[]
        {
            "UI/WardrobeOverlay.cs",
            "UI/SaveSetOverlay.cs",
            "UI/ConfigOverlay.cs",
            "UI/ScheduleMenu.cs",
            "UI/ScheduleEditOverlay.cs",
            "UI/ScheduleDebugLogOverlay.cs",
        };

        // ----------------------------------------------------------------
        //  S1: Every menu overrides gameWindowSizeChanged
        // ----------------------------------------------------------------

        [Theory]
        [InlineData("UI/OutfitMenu.cs")]
        [InlineData("UI/WardrobeOverlay.cs")]
        [InlineData("UI/SaveSetOverlay.cs")]
        [InlineData("UI/ConfigOverlay.cs")]
        [InlineData("UI/ScheduleMenu.cs")]
        [InlineData("UI/ScheduleEditOverlay.cs")]
        [InlineData("UI/ScheduleOutfitOverlay.cs")]
        [InlineData("UI/ScheduleDebugLogOverlay.cs")]
        // Expected: Every menu class overrides gameWindowSizeChanged to stay anchored on window resize
        public void Menu_Overrides_GameWindowSizeChanged(string sourceFile)
        {
            string source = SourceScanner.ReadSourceFile(sourceFile);
            Assert.Contains("override void gameWindowSizeChanged", source);
        }

        // ----------------------------------------------------------------
        //  S2: gameWindowSizeChanged calls Recalculate
        // ----------------------------------------------------------------

        [Theory]
        [InlineData("UI/OutfitMenu.cs")]
        [InlineData("UI/WardrobeOverlay.cs")]
        [InlineData("UI/SaveSetOverlay.cs")]
        [InlineData("UI/ConfigOverlay.cs")]
        [InlineData("UI/ScheduleMenu.cs")]
        [InlineData("UI/ScheduleEditOverlay.cs")]
        [InlineData("UI/ScheduleOutfitOverlay.cs")]
        // Expected: Every gameWindowSizeChanged body calls Recalculate (with or without args) to recompute layout
        public void GameWindowSizeChanged_CallsRecalculate(string sourceFile)
        {
            string source = SourceScanner.ReadSourceFile(sourceFile);
            bool found = SourceScanner.MethodContains(source,
                "override void gameWindowSizeChanged", "Recalculate(");
            Assert.True(found,
                $"{sourceFile}: gameWindowSizeChanged must call Recalculate()");
        }

        // ----------------------------------------------------------------
        //  S4: Type B overlays forward resize to parent
        // ----------------------------------------------------------------

        [Theory]
        [InlineData("UI/WardrobeOverlay.cs")]
        [InlineData("UI/SaveSetOverlay.cs")]
        [InlineData("UI/ConfigOverlay.cs")]
        [InlineData("UI/ScheduleMenu.cs")]
        [InlineData("UI/ScheduleEditOverlay.cs")]
        [InlineData("UI/ScheduleDebugLogOverlay.cs")]
        // Expected: Type B overlays forward gameWindowSizeChanged to parentMenu so the parent stays in sync
        public void TypeBOverlay_ForwardsResize_ToParent(string sourceFile)
        {
            string source = SourceScanner.ReadSourceFile(sourceFile);
            bool found = SourceScanner.MethodContains(source,
                "override void gameWindowSizeChanged", "parentMenu.gameWindowSizeChanged")
                || SourceScanner.MethodContains(source,
                "override void gameWindowSizeChanged", "parentMenu?.gameWindowSizeChanged");
            Assert.True(found,
                $"{sourceFile}: Type B overlay must forward gameWindowSizeChanged to parentMenu");
        }

        // ----------------------------------------------------------------
        //  S5: Every menu handles close-on-outside-bounds
        // ----------------------------------------------------------------

        [Theory]
        [InlineData("UI/OutfitMenu.cs")]
        [InlineData("UI/WardrobeOverlay.cs")]
        [InlineData("UI/SaveSetOverlay.cs")]
        [InlineData("UI/ConfigOverlay.cs")]
        [InlineData("UI/ScheduleMenu.cs")]
        [InlineData("UI/ScheduleEditOverlay.cs")]
        [InlineData("UI/ScheduleOutfitOverlay.cs")]
        [InlineData("UI/ScheduleDebugLogOverlay.cs")]
        // Expected: Every receiveLeftClick checks isWithinBounds for close-on-outside-bounds
        public void Menu_HandlesCloseOnClickOutside(string sourceFile)
        {
            string source = SourceScanner.ReadSourceFile(sourceFile);
            bool found = SourceScanner.MethodContains(source,
                "override void receiveLeftClick", "isWithinBounds");
            Assert.True(found,
                $"{sourceFile}: receiveLeftClick must check isWithinBounds for close-on-outside-bounds");
        }

        // ----------------------------------------------------------------
        //  S6b: ScheduleEditOverlay forwards input to ScheduleOutfitOverlay (Type A child)
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleEditOverlay forwards gameWindowSizeChanged to previewOverlay (Type A child)
        public void ScheduleEditOverlay_ForwardsResize_ToPreviewOverlay()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            bool found = SourceScanner.MethodContains(source,
                "override void gameWindowSizeChanged", "previewOverlay?.gameWindowSizeChanged");
            Assert.True(found,
                "ScheduleEditOverlay.gameWindowSizeChanged must forward to previewOverlay");
        }

        [Fact]
        // Expected: ScheduleEditOverlay forwards receiveLeftClick to previewOverlay
        public void ScheduleEditOverlay_ForwardsLeftClick_ToPreviewOverlay()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            Assert.Contains("previewOverlay.receiveLeftClick", source);
        }

        [Fact]
        // Expected: ScheduleEditOverlay forwards receiveKeyPress to previewOverlay
        public void ScheduleEditOverlay_ForwardsKeyPress_ToPreviewOverlay()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            Assert.Contains("previewOverlay.receiveKeyPress", source);
        }

        [Fact]
        // Expected: ScheduleEditOverlay forwards receiveScrollWheelAction to previewOverlay
        public void ScheduleEditOverlay_ForwardsScrollWheel_ToPreviewOverlay()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            Assert.Contains("previewOverlay.receiveScrollWheelAction", source);
        }

        [Fact]
        // Expected: ScheduleEditOverlay checks readyToClose after forwarding click to previewOverlay
        public void ScheduleEditOverlay_ChecksReadyToClose_AfterPreviewForward()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            Assert.Contains("previewOverlay.readyToClose()", source);
        }

        // ----------------------------------------------------------------
        //  S6d: ScheduleMenu delete confirmation pattern
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleMenu has delete confirmation dialog (pattern: delete dialog blocks input)
        public void ScheduleMenu_HasDeleteConfirmation()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            Assert.Contains("showDeleteConfirmation", source);
        }

        [Fact]
        // Expected: ScheduleMenu delete confirmation plays trashcan sound on confirm
        public void ScheduleMenu_DeleteConfirm_PlaysTrashcanSound()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            Assert.Contains("trashcan", source);
        }

        [Fact]
        // Expected: ScheduleMenu delete confirmation calls scheduleStore.DeleteRule
        public void ScheduleMenu_DeleteConfirm_CallsDeleteRule()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            Assert.Contains("scheduleStore.DeleteRule", source);
        }

        // ----------------------------------------------------------------
        //  S6e: ScheduleEditOverlay has Save button that calls HandleSave
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleEditOverlay has a save button that calls HandleSave directly
        public void ScheduleEditOverlay_HasSaveButton()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            Assert.Contains("SaveButton", source);
            Assert.Contains("HandleSave", source);
        }

        // ----------------------------------------------------------------
        //  S6f: ScheduleEditOverlay auto-generates rule name on save
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleEditOverlay generates rule name via GenerateRuleName on save
        public void ScheduleEditOverlay_HandleSave_UsesGenerateRuleName()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            Assert.Contains("GenerateRuleName()", source);
        }

        [Fact]
        // Expected: ScheduleEditOverlay has no title box (removed in Phase 2)
        public void ScheduleEditOverlay_HasNoTitleBox()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            Assert.DoesNotContain("TitleBoxBounds", source);
        }

        [Fact]
        // Expected: ScheduleEditOverlay auto-generates rule name (no name TextBox)
        public void ScheduleEditOverlay_AutoGeneratesRuleName()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            Assert.Contains("GenerateRuleName", source);
            Assert.DoesNotContain("nameTextBox", source);
            Assert.DoesNotContain("nameBoxFocused", source);
        }

        // ----------------------------------------------------------------
        //  S6g: ScheduleEditOverlay parses tokenized strings from game data
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleEditOverlay uses TokenParser.ParseText for passive festival display names
        public void ScheduleEditOverlay_ParsesPassiveFestivalDisplayNames()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            Assert.Contains("TokenParser.ParseText", source);
        }

        [Fact]
        // Expected: ScheduleEditOverlay disambiguates duplicate location display names
        public void ScheduleEditOverlay_DisambiguatesDuplicateLocationNames()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            Assert.Contains("displayCounts", source);
        }

        // ----------------------------------------------------------------
        //  S6h: ScheduleMenu draws priority dropdown
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleMenu draw method uses DrawPriorityDropdown and DrawPriorityDropdownOptions
        public void ScheduleMenu_Draw_UsesPriorityDropdown()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void draw");
            Assert.Contains("DrawPriorityDropdown", body);
            Assert.Contains("DrawPriorityDropdownOptions", body);
        }

        // ----------------------------------------------------------------
        //  S6i: Schedule rule icons include 10px left offset
        // ----------------------------------------------------------------

        [Fact]
        // Expected: BuildRuleComponents offsets delete icon 10px left of the content edge
        public void BuildRuleComponents_HasIconLeftOffset()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenuUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void BuildRuleComponents");
            Assert.Contains("ClearButtonSize - 10", body);
        }

        // S6j: removed — name TextBox was removed from ScheduleEditOverlay

        // ----------------------------------------------------------------
        //  S6k: ScheduleMenu search TextBox focus pattern
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleMenu.update gates searchTextBox.Selected with AutoFocusSearchBar config
        public void ScheduleMenu_Update_GatesSearchBoxSelectedWithConfig()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void update");
            Assert.Contains("searchTextBox.Update()", body);
            Assert.Contains("AutoFocusSearchBar", body);
            Assert.Contains("searchBarFocused", body);
        }

        [Fact]
        // Expected: ScheduleMenu.CreateSearchTextBox conditionally sets keyboardDispatcher.Subscriber based on AutoFocusSearchBar
        public void ScheduleMenu_CreateSearchTextBox_SetsSubscriber()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void CreateSearchTextBox");
            Assert.Contains("Game1.keyboardDispatcher.Subscriber = searchTextBox", body);
        }

        // ----------------------------------------------------------------
        //  S6l: ScheduleMenu search integration
        // ----------------------------------------------------------------

        [Fact]
        // Expected: RebuildDisplayedRules resets scroll and recalculates layout for new display count
        public void ScheduleMenu_RebuildDisplayedRules_ResetsScrollAndRecalculates()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void RebuildDisplayedRules");
            Assert.Contains("ScrollOffset = 0", body);
            Assert.Contains("Recalculate(displayedRules.Count, ruleStates.Count)", body);
            Assert.Contains("UpdateSearchTextBoxBounds()", body);
        }

        [Fact]
        // Expected: ScheduleMenu.update triggers RebuildDisplayedRules when search text changes
        public void ScheduleMenu_Update_RebuildOnSearchChange()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void update");
            Assert.Contains("lastSearchText", body);
            Assert.Contains("RebuildDisplayedRules()", body);
        }

        // ----------------------------------------------------------------
        //  S6m: ScheduleMenu receiveLeftClick handles priority dropdown and search clear
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleMenu.receiveLeftClick handles priority dropdown open/close
        public void ScheduleMenu_ReceiveLeftClick_HandlesPriorityDropdown()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void receiveLeftClick");
            Assert.Contains("PriorityDropdown.containsPoint", body);
            Assert.Contains("PriorityClearButton.containsPoint", body);
            Assert.Contains("priorityDropdownOpen", body);
        }

        [Fact]
        // Expected: ScheduleMenu.receiveLeftClick clears search text on clear button click
        public void ScheduleMenu_ReceiveLeftClick_ClearsSearchOnClearButton()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void receiveLeftClick");
            Assert.Contains("SearchClearButton.containsPoint", body);
        }

        [Fact]
        // Expected: ScheduleMenu.receiveLeftClick uses displayedRules (not ruleStates) for rule list interactions
        public void ScheduleMenu_ReceiveLeftClick_UsesDisplayedRules()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void receiveLeftClick");
            Assert.Contains("displayedRules[", body);
            Assert.DoesNotContain("ruleStates[", body);
        }

        // ----------------------------------------------------------------
        //  S6n: ScheduleMenu draw pipeline
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleMenu.draw uses UIHelpers.DrawInputBar for search with placeholder
        public void ScheduleMenu_Draw_UsesDrawInputBarForSearch()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void draw");
            Assert.Contains("UIHelpers.DrawInputBar", body);
            Assert.Contains("WardrobeSearch", body);
        }

        [Fact]
        // Expected: ScheduleMenu.draw renders the divider
        public void ScheduleMenu_Draw_DrawsDivider()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void draw");
            Assert.Contains("DrawDivider", body);
        }

        [Fact]
        // Expected: ScheduleMenu.DrawRuleList uses displayedRules (not ruleStates) for rule rendering
        public void ScheduleMenu_DrawRuleList_UsesDisplayedRules()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "string? DrawRuleList");
            Assert.Contains("displayedRules[", body);
            Assert.DoesNotContain("ruleStates[", body);
        }

        // ----------------------------------------------------------------
        //  S6o: DrawTabWithText extracted to UIHelpers (shared by both menus)
        // ----------------------------------------------------------------

        [Fact]
        // Expected: UIHelpers has DrawTabWithText as a static method with opacity parameter
        public void UIHelpers_HasDrawTabWithText_WithOpacity()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/UIHelpers.cs");
            Assert.Contains("public static void DrawTabWithText", source);
            Assert.Contains("float opacity = 1f", source);
        }

        [Fact]
        // Expected: OutfitUIBuilder no longer has its own DrawTabWithText (refactored to UIHelpers)
        public void OutfitUIBuilder_NoDrawTabWithText()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitUIBuilder.cs");
            Assert.DoesNotContain("DrawTabWithText", source);
        }

        [Fact]
        // Expected: OutfitMenu calls UIHelpers.DrawTabWithText (not uiBuilder.DrawTabWithText)
        public void OutfitMenu_UsesUIHelpers_DrawTabWithText()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitMenu.cs");
            Assert.Contains("UIHelpers.DrawTabWithText", source);
        }

        // ----------------------------------------------------------------
        //  S6p: ScheduleMenu gameWindowSizeChanged updates TextBox bounds
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleMenu.gameWindowSizeChanged repositions search TextBox after resize
        public void ScheduleMenu_GameWindowSizeChanged_UpdatesSearchTextBoxBounds()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            bool found = SourceScanner.MethodContains(source,
                "override void gameWindowSizeChanged", "UpdateSearchTextBoxBounds()");
            Assert.True(found,
                "ScheduleMenu.gameWindowSizeChanged must call UpdateSearchTextBoxBounds()");
        }

        [Fact]
        // Expected: ScheduleMenu.gameWindowSizeChanged uses displayedRules.Count (not ruleStates.Count)
        public void ScheduleMenu_GameWindowSizeChanged_UsesDisplayedRulesCount()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            bool found = SourceScanner.MethodContains(source,
                "override void gameWindowSizeChanged", "displayedRules.Count");
            Assert.True(found,
                "ScheduleMenu.gameWindowSizeChanged must pass displayedRules.Count to Recalculate");
        }

        // ----------------------------------------------------------------
        //  S6q: ScheduleMenu AutoFocusSearchBar config gating
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleMenu.CreateSearchTextBox gates keyboard subscriber on AutoFocusSearchBar
        public void ScheduleMenu_CreateSearchTextBox_GatesAutoFocus()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void CreateSearchTextBox");
            Assert.Contains("AutoFocusSearchBar", body);
            Assert.Contains("searchBarFocused", body);
        }

        [Fact]
        // Expected: ScheduleMenu.receiveKeyPress closes priority dropdown on Escape
        public void ScheduleMenu_ReceiveKeyPress_ClosesDropdownOnEscape()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void receiveKeyPress");
            Assert.Contains("priorityDropdownOpen", body);
            Assert.Contains("Keys.Escape", body);
        }

        // ----------------------------------------------------------------
        //  S6r: ScheduleMenu search placeholder i18n
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleMenu search uses WardrobeSearch (shared key)
        public void ScheduleMenu_Search_UsesSharedWardrobeSearchKey()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            Assert.Contains("WardrobeSearch", source);
        }

        // ----------------------------------------------------------------
        //  S6s: ScheduleMenuUIBuilder layout structure
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleMenuUIBuilder has priority dropdown components
        public void ScheduleMenuUIBuilder_HasPriorityDropdownComponents()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenuUIBuilder.cs");
            Assert.Contains("PriorityDropdown", source);
            Assert.Contains("PriorityClearButton", source);
            Assert.Contains("PriorityDropdownOptions", source);
        }

        [Fact]
        // Expected: ScheduleMenuUIBuilder has SearchBar and SearchClearButton components
        public void ScheduleMenuUIBuilder_HasSearchComponents()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenuUIBuilder.cs");
            Assert.Contains("SearchBar", source);
            Assert.Contains("SearchClearButton", source);
        }

        [Fact]
        // Expected: ScheduleMenuUIBuilder.Recalculate includes search bar and divider height in content calculation
        public void ScheduleMenuUIBuilder_Recalculate_IncludesSearchAndDividerHeight()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenuUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void Recalculate");
            Assert.Contains("ScheduleSearchBarHeight", body);
            Assert.Contains("ScheduleDividerHeight", body);
        }

        // ----------------------------------------------------------------
        //  S7: Truncated-text tooltips gated by ShowTooltip config
        // ----------------------------------------------------------------

        [Theory]
        [InlineData("UI/ScheduleMenu.cs")]
        [InlineData("UI/ScheduleEditOverlay.cs")]
        [InlineData("UI/ScheduleOutfitOverlay.cs")]
        [InlineData("UI/ScheduleDebugLogOverlay.cs")]
        // Expected: Schedule menus gate truncated-text tooltips with Config.ShowTooltip
        public void ScheduleMenu_GatesTooltips_WithShowTooltipConfig(string sourceFile)
        {
            string source = SourceScanner.ReadSourceFile(sourceFile);
            Assert.Contains("ShowTooltip", source);
        }

        [Fact]
        // Expected: WardrobeOverlay gates truncated-text tooltips with Config.ShowTooltip
        public void WardrobeOverlay_GatesTooltips_WithShowTooltipConfig()
        {
            string source = SourceScanner.ReadSourceFile("UI/WardrobeOverlay.cs");
            // WardrobeOverlay draws tooltips for truncated outfit names and tags —
            // these must be gated by the ShowTooltip config
            Assert.Contains("ShowTooltip", source);
        }

        [Fact]
        // Expected: ConfigOverlay's draw method does NOT gate help tooltips with ShowTooltip (they always show)
        public void ConfigOverlay_DoesNotGate_HelpTooltips_WithShowTooltip()
        {
            string source = SourceScanner.ReadSourceFile("UI/ConfigOverlay.cs");
            // ConfigOverlay manages the ShowTooltip config toggle, so the field exists in the file.
            // But its draw method must NOT gate tooltip drawing with ShowTooltip — help tooltips always show.
            string drawBody = SourceScanner.ExtractMethodBody(source, "override void draw");
            Assert.DoesNotContain("ShowTooltip", drawBody);
        }

        // ----------------------------------------------------------------
        //  S8: ModEntry subscribes to LocaleChanged for translation refresh
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ModEntry subscribes to Content.LocaleChanged so TranslationCache updates when player changes language
        public void ModEntry_SubscribesTo_LocaleChanged()
        {
            string source = SourceScanner.ReadSourceFile("Core/ModEntry.cs");
            Assert.Contains("LocaleChanged", source);
        }

        [Fact]
        // Expected: ModEntry's LocaleChanged handler re-initializes TranslationCache so cached strings update to the new language
        public void ModEntry_LocaleChangedHandler_ReInitializesTranslationCache()
        {
            string source = SourceScanner.ReadSourceFile("Core/ModEntry.cs");
            bool found = SourceScanner.MethodContains(source,
                "void OnLocaleChanged", "TranslationCache.Initialize");
            Assert.True(found,
                "OnLocaleChanged must call TranslationCache.Initialize to refresh cached translations");
        }

        // ----------------------------------------------------------------
        //  S9: ScheduleMenu master toggle disables filter bar and cleans up state
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleMenu.receiveLeftClick blocks all input (except Close) when master is disabled
        public void ScheduleMenu_MasterDisabled_BlocksInputExceptClose()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void receiveLeftClick");
            // The !masterEnabled guard must come before dropdown/search/rule handling
            Assert.Contains("!masterEnabled", body);
            Assert.Contains("CloseMenuButton.containsPoint", body);
        }

        [Fact]
        // Expected: ScheduleMenu master toggle cleans up dropdown and search state when disabling
        public void ScheduleMenu_MasterToggleOff_CleansUpState()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void receiveLeftClick");
            // When toggling master off, must close dropdown and defocus search
            Assert.Contains("priorityDropdownOpen = false", body);
            Assert.Contains("searchBarFocused = false", body);
        }

        [Fact]
        // Expected: ScheduleMenu.update suppresses searchTextBox when master is disabled
        public void ScheduleMenu_Update_SuppressesSearchWhenMasterDisabled()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void update");
            Assert.Contains("!masterEnabled", body);
        }

        [Fact]
        // Expected: ScheduleMenu.draw uses DisabledSectionOpacity for priority dropdown and search bar when master is disabled
        public void ScheduleMenu_Draw_DimsPriorityAndSearchWhenMasterDisabled()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void draw");
            Assert.Contains("DisabledSectionOpacity", body);
            Assert.Contains("filterOpacity", body);
        }

        // ----------------------------------------------------------------
        //  S10: Speed icon for AdvanceOnWarp rules
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleMenuUIBuilder uses Game1.mouseCursors (weapon speed icon), not buffsIconsTexture
        public void ScheduleMenuUIBuilder_SpeedIcon_UsesMouseCursors()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenuUIBuilder.cs");
            Assert.DoesNotContain("buffsIconsTexture", source);
            Assert.Contains("SpeedIconSourceRect", source);
            // Weapon speed icon rect from MeleeWeapon.cs
            Assert.Contains("130, 428, 10, 10", source);
        }

        [Fact]
        // Expected: Speed icon is drawn flipped horizontally
        public void ScheduleMenuUIBuilder_SpeedIcon_FlippedHorizontally()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenuUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "string? DrawRuleRow");
            Assert.Contains("SpriteEffects.FlipHorizontally", body);
        }

        [Fact]
        // Expected: Speed icon is drawn after the name text (nameStartX + textWidth + gap)
        public void ScheduleMenuUIBuilder_SpeedIcon_DrawnAfterName()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenuUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "string? DrawRuleRow");
            Assert.Contains("nameStartX + textWidth + NameToWarpIconGap", body);
        }

        // ----------------------------------------------------------------
        //  S11: Dropdown selected option uses Wheat color
        // ----------------------------------------------------------------

        [Fact]
        // Expected: UIHelpers.DrawDropdownOptions uses Color.Wheat for selected options (not LightBlue)
        public void UIHelpers_DrawDropdownOptions_UsesWheatForSelected()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/UIHelpers.cs");
            string body = SourceScanner.ExtractMethodBody(source, "string? DrawDropdownOptions");
            Assert.Contains("Color.Wheat", body);
            Assert.DoesNotContain("Color.LightBlue", body);
        }

        [Fact]
        // Expected: UIHelpers.DrawDropdownButton has an opacity parameter for inactive state
        public void UIHelpers_DrawDropdownButton_HasOpacityParameter()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/UIHelpers.cs");
            Assert.Contains("float opacity = 1f", source);
        }

        // ----------------------------------------------------------------
        //  S12: All single-select dropdowns pass isSelected
        // ----------------------------------------------------------------

        [Fact]
        // Expected: Mod dropdown (OutfitDrawingHelper) passes isSelected based on current mod filter
        public void OutfitDrawingHelper_DrawDropdownOptions_PassesIsSelected()
        {
            string source = SourceScanner.ReadSourceFile("Rendering/OutfitDrawingHelper.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawDropdownOptions");
            Assert.Contains("isSelected:", body);
            Assert.DoesNotContain("isSelected: null", body);
        }

        [Fact]
        // Expected: SearchScope dropdown draw in WardrobeOverlay passes isSelected to DrawDropdownOptions
        public void WardrobeOverlay_DrawSearchScopeDropdown_PassesIsSelected()
        {
            string source = SourceScanner.ReadSourceFile("UI/WardrobeOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void draw");
            Assert.Contains("isSelected:", body);
        }

        [Fact]
        // Expected: Config SearchScope dropdown (ConfigUIBuilder) passes isSelected based on current value
        public void ConfigUIBuilder_DrawSearchScopeDropdownOptions_PassesIsSelected()
        {
            string source = SourceScanner.ReadSourceFile("UI/ConfigUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawSearchScopeDropdownOptions");
            Assert.Contains("isSelected:", body);
            Assert.DoesNotContain("isSelected: null", body);
        }

        [Fact]
        // Expected: Priority dropdown (ScheduleMenuUIBuilder) passes isSelected based on selected priority label
        public void ScheduleMenuUIBuilder_DrawPriorityDropdownOptions_PassesIsSelected()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenuUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "string? DrawPriorityDropdownOptions");
            Assert.Contains("isSelected:", body);
        }

        // ----------------------------------------------------------------
        //  S13: Main menu search bar placeholder
        // ----------------------------------------------------------------

        [Fact]
        // Expected: OutfitSearchManager.Draw passes a placeholder to UIHelpers.DrawInputBar
        public void OutfitSearchManager_Draw_PassesPlaceholder()
        {
            string source = SourceScanner.ReadSourceFile("Managers/OutfitSearchManager.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void Draw");
            Assert.Contains("UIHelpers.DrawInputBar", body);
            Assert.Contains("placeholder:", body);
        }

        [Fact]
        // Expected: OutfitSearchManager.Draw reuses WardrobeSearch translation for placeholder
        public void OutfitSearchManager_Draw_ReusesWardrobeSearchTranslation()
        {
            string source = SourceScanner.ReadSourceFile("Managers/OutfitSearchManager.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void Draw");
            Assert.Contains("TranslationCache.WardrobeSearch", body);
        }

        // ----------------------------------------------------------------
        //  S14: Schedule edit UI hover effects
        // ----------------------------------------------------------------

        [Fact]
        // Expected: DrawWeddingRow computes a hitArea for hover detection over checkbox + label
        public void ScheduleEditUIBuilder_DrawWeddingRow_HasHoverHitArea()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawWeddingRow");
            Assert.Contains("hitArea", body);
            Assert.Contains("isHovered", body);
        }

        [Fact]
        // Expected: DrawWeddingRow bold effect uses offset shadow pattern (Vector2(-1, 0)) when hovered
        public void ScheduleEditUIBuilder_DrawWeddingRow_BoldOnHover()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawWeddingRow");
            Assert.Contains("new Vector2(-1, 0)", body);
            Assert.Contains("textColor * 0.8f", body);
        }

        // ----------------------------------------------------------------
        //  S15: SuppressHover pattern
        // ----------------------------------------------------------------

        [Fact]
        // Expected: UIHelpers.SuppressHover is a public static property
        public void UIHelpers_HasSuppressHover_PublicStatic()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/UIHelpers.cs");
            Assert.Contains("public static bool SuppressHover", source);
        }

        [Theory]
        [InlineData("void DrawTextButton")]
        [InlineData("void DrawTextureButton")]
        [InlineData("void DrawDropdownButton")]
        [InlineData("void DrawClearButton")]
        [InlineData("void DrawTabWithText")]
        [InlineData("void DrawToggleButton")]
        // Expected: Each UIHelpers draw method gates hover with !SuppressHover
        public void UIHelpers_DrawMethod_GatesHoverWithSuppressHover(string methodSignature)
        {
            string source = SourceScanner.ReadSourceFile("Utilities/UIHelpers.cs");
            string body = SourceScanner.ExtractMethodBody(source, methodSignature);
            Assert.Contains("!SuppressHover", body);
        }

        [Theory]
        [InlineData("UI/WardrobeOverlay.cs")]
        [InlineData("UI/ConfigOverlay.cs")]
        [InlineData("UI/ScheduleMenu.cs")]
        [InlineData("UI/SaveSetOverlay.cs")]
        [InlineData("UI/ScheduleEditOverlay.cs")]
        [InlineData("UI/ScheduleDebugLogOverlay.cs")]
        // Expected: Type B overlay draw() sets SuppressHover=true before parentMenu.draw and restores after
        public void TypeBOverlay_Draw_SetsSuppressHoverAroundParentDraw(string sourceFile)
        {
            string source = SourceScanner.ReadSourceFile(sourceFile);
            string body = SourceScanner.ExtractMethodBody(source, "override void draw");
            Assert.Contains("oldSuppressHover = UIHelpers.SuppressHover", body);
            Assert.Contains("UIHelpers.SuppressHover = true", body);
            Assert.True(body.Contains("parentMenu.draw(b)") || body.Contains("parentMenu?.draw(b)"),
                $"{sourceFile}: draw must call parentMenu.draw(b)");
            Assert.Contains("UIHelpers.SuppressHover = oldSuppressHover", body);
        }

        [Fact]
        // Expected: OutfitMenu.draw sets SuppressHover based on hasOverlay flag
        public void OutfitMenu_Draw_SetsSuppressHoverBasedOnHasOverlay()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void draw");
            Assert.Contains("hasOverlay", body);
            Assert.Contains("oldSuppressHover = UIHelpers.SuppressHover", body);
            Assert.Contains("UIHelpers.SuppressHover = true", body);
        }

        [Fact]
        // Expected: OutfitUIBuilder.DrawGearButton gates hover with UIHelpers.SuppressHover
        public void OutfitUIBuilder_DrawGearButton_GatesSuppressHover()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawGearButton");
            Assert.Contains("!UIHelpers.SuppressHover", body);
        }

        [Fact]
        // Expected: OutfitUIBuilder.DrawDyeColorButton gates hover with UIHelpers.SuppressHover
        public void OutfitUIBuilder_DrawDyeColorButton_GatesSuppressHover()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawDyeColorButton");
            Assert.Contains("!UIHelpers.SuppressHover", body);
        }

        [Fact]
        // Expected: ScheduleMenuUIBuilder.DrawMasterToggle gates hover with UIHelpers.SuppressHover
        public void ScheduleMenuUIBuilder_DrawMasterToggle_GatesSuppressHover()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenuUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawMasterToggle");
            Assert.Contains("!UIHelpers.SuppressHover", body);
        }

        [Fact]
        // Expected: WardrobeUIBuilder outfit list hover gates with UIHelpers.SuppressHover
        public void WardrobeUIBuilder_OutfitListHover_GatesSuppressHover()
        {
            string source = SourceScanner.ReadSourceFile("UI/WardrobeUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawOutfitSetList");
            Assert.Contains("!UIHelpers.SuppressHover", body);
        }

        [Fact]
        // Expected: WardrobeUIBuilder.DrawCheckbox gates hover with UIHelpers.SuppressHover
        public void WardrobeUIBuilder_DrawCheckbox_GatesSuppressHover()
        {
            string source = SourceScanner.ReadSourceFile("UI/WardrobeUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawCheckbox");
            Assert.Contains("!UIHelpers.SuppressHover", body);
        }

        [Fact]
        // Expected: ScheduleEditOverlay.draw suppresses own hover when previewOverlay is open
        public void ScheduleEditOverlay_Draw_SuppressesHoverWhenPreviewOpen()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void draw");
            Assert.Contains("previewOverlay != null", body);
            Assert.Contains("UIHelpers.SuppressHover = true", body);
            Assert.Contains("UIHelpers.SuppressHover = false", body);
        }

        // ----------------------------------------------------------------
        //  S15b: DrawToggleButton usage
        // ----------------------------------------------------------------

        [Fact]
        // Expected: UIHelpers has DrawToggleButton with SpriteBatch, ClickableComponent, bool params
        public void UIHelpers_HasDrawToggleButton()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/UIHelpers.cs");
            Assert.Contains("public static void DrawToggleButton(SpriteBatch b, ClickableComponent button, bool isOpen)", source);
        }

        [Fact]
        // Expected: UIHelpers has GetToggleButtonWidth method
        public void UIHelpers_HasGetToggleButtonWidth()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/UIHelpers.cs");
            Assert.Contains("public static int GetToggleButtonWidth()", source);
        }

        [Fact]
        // Expected: SaveSetUIBuilder.DrawTagsRow delegates to UIHelpers.DrawToggleButton
        public void SaveSetUIBuilder_DrawTagsRow_UsesDrawToggleButton()
        {
            string source = SourceScanner.ReadSourceFile("UI/SaveSetUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawTagsRow");
            Assert.Contains("UIHelpers.DrawToggleButton", body);
        }


        [Fact]
        // Expected: ScheduleEditUIBuilder no longer has private DrawAddButton method
        public void ScheduleEditUIBuilder_NoDrawAddButtonMethod()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            Assert.DoesNotContain("private void DrawAddButton", source);
        }

        // ----------------------------------------------------------------
        //  S16: Config menu clickable text hover effects
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ConfigUIBuilder.DrawKeybindRow has hover detection via containsPoint
        public void ConfigUIBuilder_DrawKeybindRow_HasHoverDetection()
        {
            string source = SourceScanner.ReadSourceFile("UI/ConfigUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawKeybindRow");
            Assert.Contains("containsPoint", body);
            Assert.Contains("isHovered", body);
        }

        [Fact]
        // Expected: ConfigUIBuilder.DrawKeybindRow suppresses hover when listening for keybind
        public void ConfigUIBuilder_DrawKeybindRow_SuppressesHoverWhenListening()
        {
            string source = SourceScanner.ReadSourceFile("UI/ConfigUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawKeybindRow");
            Assert.Contains("!isListening", body);
        }

        [Fact]
        // Expected: ConfigOverlay sets keybind to None when Escape is pressed while listening
        public void ConfigOverlay_EscapeSetsKeybindToNone()
        {
            string source = SourceScanner.ReadSourceFile("UI/ConfigOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void receiveKeyPress");
            Assert.Contains("Keys.Escape", body);
            Assert.Contains("\"None\"", body);
        }

        [Fact]
        // Expected: ConfigOverlay Escape keybind path assigns to the keybind field, not just cancels
        public void ConfigOverlay_EscapeKeybind_AssignsNewKeybind()
        {
            string source = SourceScanner.ReadSourceFile("UI/ConfigOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void receiveKeyPress");
            int escapeIndex = body.IndexOf("Keys.Escape");
            int parseNoneIndex = body.IndexOf("\"None\"");
            Assert.True(escapeIndex < parseNoneIndex, "None keybind should be set after Escape check");
        }

        [Fact]
        // Expected: ConfigUIBuilder.DrawDropdownRow has hover detection via containsPoint
        public void ConfigUIBuilder_DrawDropdownRow_HasHoverDetection()
        {
            string source = SourceScanner.ReadSourceFile("UI/ConfigUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawDropdownRow");
            Assert.Contains("containsPoint", body);
            Assert.Contains("isHovered", body);
        }

        [Fact]
        // Expected: ConfigUIBuilder.DrawDropdownRow suppresses hover when dropdown is open
        public void ConfigUIBuilder_DrawDropdownRow_SuppressesHoverWhenOpen()
        {
            string source = SourceScanner.ReadSourceFile("UI/ConfigUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawDropdownRow");
            Assert.Contains("!isOpen", body);
        }
        // ----------------------------------------------------------------
        //  S17: ApplySavedColor / ApplyCapturedColor consolidated to ColorHelper.ApplyColor
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ColorHelper has public static ApplyColor method
        public void ColorHelper_HasApplyColor()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/ColorHelper.cs");
            Assert.Contains("public static void ApplyColor", source);
        }

        [Theory]
        [InlineData("UI/WardrobeOverlay.cs")]
        [InlineData("UI/SaveSetOverlay.cs")]
        [InlineData("UI/ScheduleOutfitOverlay.cs")]
        // Expected: No private ApplySavedColor/ApplyCapturedColor — all use ColorHelper.ApplyColor
        public void Menu_UsesColorHelperApplyColor(string sourceFile)
        {
            string source = SourceScanner.ReadSourceFile(sourceFile);
            Assert.DoesNotContain("private static void ApplySavedColor", source);
            Assert.DoesNotContain("private static void ApplyCapturedColor", source);
            Assert.Contains("ColorHelper.ApplyColor", source);
        }

        // ----------------------------------------------------------------
        //  S18: Delete confirmation uses UIHelpers shared helpers
        // ----------------------------------------------------------------

        [Theory]
        [InlineData("UI/WardrobeOverlay.cs")]
        [InlineData("UI/ScheduleMenu.cs")]
        // Expected: No private DrawDeleteDialogButton/DrawDeleteConfirmation — uses UIHelpers
        public void Menu_UsesSharedDeleteDialog(string sourceFile)
        {
            string source = SourceScanner.ReadSourceFile(sourceFile);
            Assert.DoesNotContain("private void DrawDeleteDialogButton", source);
            Assert.DoesNotContain("private void DrawDeleteConfirmation", source);
            Assert.Contains("UIHelpers.DrawDeleteConfirmationDialog", source);
            Assert.Contains("UIHelpers.CalculateDeleteDialogLayout", source);
        }
        // ----------------------------------------------------------------
        //  S19: Tooltip drawing uses UIHelpers.DrawWrappedTooltip
        // ----------------------------------------------------------------

        [Fact]
        // Expected: UIHelpers has public static DrawWrappedTooltip method
        public void UIHelpers_HasDrawWrappedTooltip()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/UIHelpers.cs");
            Assert.Contains("public static void DrawWrappedTooltip", source);
        }

        [Theory]
        [InlineData("UI/WardrobeOverlay.cs")]
        [InlineData("UI/ScheduleMenu.cs")]
        [InlineData("UI/ScheduleEditOverlay.cs")]
        [InlineData("UI/ScheduleOutfitOverlay.cs")]
        // Expected: These files use UIHelpers.DrawWrappedTooltip instead of inline wrapping
        public void Menu_UsesDrawWrappedTooltip(string sourceFile)
        {
            string source = SourceScanner.ReadSourceFile(sourceFile);
            Assert.Contains("UIHelpers.DrawWrappedTooltip", source);
        }
        // ----------------------------------------------------------------
        //  S20: Item sprite drawing uses UIHelpers.DrawItemInSlot
        // ----------------------------------------------------------------

        [Fact]
        // Expected: UIHelpers has public static DrawItemInSlot method
        public void UIHelpers_HasDrawItemInSlot()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/UIHelpers.cs");
            Assert.Contains("public static void DrawItemInSlot", source);
        }

        [Theory]
        [InlineData("UI/WardrobeOverlay.cs")]
        [InlineData("UI/SaveSetOverlay.cs")]
        // Expected: DrawItemSprites body uses UIHelpers.DrawItemInSlot, no inline centering math
        public void Menu_DrawItemSprites_UsesDrawItemInSlot(string sourceFile)
        {
            string source = SourceScanner.ReadSourceFile(sourceFile);
            string body = SourceScanner.ExtractMethodBody(source, "void DrawItemSprites");
            Assert.Contains("UIHelpers.DrawItemInSlot", body);
            Assert.DoesNotContain("DrawnItemSize) / 2", body);
        }

        // ----------------------------------------------------------------
        //  S21: All checkbox draws use UIHelpers source rect constants
        // ----------------------------------------------------------------

        // ----------------------------------------------------------------
        //  S22: All menus with render targets override cleanupBeforeExit
        // ----------------------------------------------------------------

        [Theory]
        [InlineData("UI/WardrobeOverlay.cs")]
        [InlineData("UI/SaveSetOverlay.cs")]
        [InlineData("UI/ScheduleOutfitOverlay.cs")]
        // Expected: Overlays with render targets override cleanupBeforeExit to prevent GPU leaks
        public void Overlay_WithRenderTarget_OverridesCleanupBeforeExit(string sourceFile)
        {
            string source = SourceScanner.ReadSourceFile(sourceFile);
            Assert.Contains("override void cleanupBeforeExit", source);
        }

        [Fact]
        // Expected: UIHelpers has public static SafeDispose method
        public void UIHelpers_HasSafeDispose()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/UIHelpers.cs");
            Assert.Contains("public static void SafeDispose", source);
        }

        // ----------------------------------------------------------------
        //  S23: Delete dialog recalculates on window resize
        // ----------------------------------------------------------------

        [Theory]
        [InlineData("UI/WardrobeOverlay.cs")]
        [InlineData("UI/ScheduleMenu.cs")]
        // Expected: gameWindowSizeChanged recalculates delete dialog layout when dialog is showing
        public void Menu_GameWindowSizeChanged_RecalculatesDeleteDialog(string sourceFile)
        {
            string source = SourceScanner.ReadSourceFile(sourceFile);
            string body = SourceScanner.ExtractMethodBody(source, "override void gameWindowSizeChanged");
            Assert.Contains("showDeleteConfirmation", body);
            Assert.Contains("CalculateDeleteDialogLayout", body);
        }

        // ----------------------------------------------------------------
        //  S24: DrawWrappedTooltip uses custom drawing (not drawHoverText)
        // ----------------------------------------------------------------

        [Fact]
        // Expected: DrawWrappedTooltip draws text with drawTextWithShadow, not drawHoverText
        public void UIHelpers_DrawWrappedTooltip_UsesCustomDrawing()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/UIHelpers.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawWrappedTooltip");
            Assert.Contains("drawTextWithShadow", body);
            Assert.Contains("drawTextureBox", body);
            Assert.DoesNotContain("drawHoverText", body);
        }

        // ----------------------------------------------------------------
        //  S26: DyeColorManager slider hit areas expanded horizontally
        // ----------------------------------------------------------------

        [Fact]
        // Expected: TryStartSliderDrag uses horizontal expansion (expandX) for cursor half-width
        public void DyeColorManager_TryStartSliderDrag_ExpandsHitAreaHorizontally()
        {
            string source = SourceScanner.ReadSourceFile("Managers/DyeColorManager.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void TryStartSliderDrag");
            Assert.Contains("expandX", body);
            Assert.Contains("hueBarBounds.X - expandX", body);
            Assert.Contains("hueBarBounds.Width + expandX * 2", body);
        }

        [Fact]
        // Expected: CalculateLayout uses 16px gap between HSV label and slider bar
        public void DyeColorManager_CalculateLayout_HasCorrectBarOffset()
        {
            string source = SourceScanner.ReadSourceFile("Managers/DyeColorManager.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void CalculateLayout");
            Assert.Contains("maxLabelWidth + 16", body);
        }

        // ----------------------------------------------------------------
        //  S27: Wardrobe preview panel shows total outfits count
        // ----------------------------------------------------------------

        [Fact]
        // Expected: DrawPreviewPanel renders total outfits text using ScheduleEditTotalOutfits translation
        public void WardrobeUIBuilder_DrawPreviewPanel_ShowsTotalOutfits()
        {
            string source = SourceScanner.ReadSourceFile("UI/WardrobeUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawPreviewPanel");
            Assert.Contains("ScheduleEditTotalOutfits", body);
            Assert.Contains("displayedCount", body);
        }

        [Fact]
        // Expected: CalculatePreviewAndButtons shifts preview down 10px to make room for total outfits
        public void WardrobeUIBuilder_CalculatePreviewAndButtons_ShiftsPreviewDown()
        {
            string source = SourceScanner.ReadSourceFile("UI/WardrobeUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void CalculatePreviewAndButtons");
            Assert.Contains("+ 10", body);
        }

        // ----------------------------------------------------------------
        //  S28: WardrobeOverlay respects ResetMatchAllOnOpen config
        // ----------------------------------------------------------------

        [Fact]
        // Expected: WardrobeOverlay constructor checks ResetMatchAllOnOpen to restore lastMatchAllTags
        public void WardrobeOverlay_Constructor_RespectsResetMatchAllOnOpenConfig()
        {
            string source = SourceScanner.ReadSourceFile("UI/WardrobeOverlay.cs");
            Assert.Contains("ResetMatchAllOnOpen", source);
            Assert.Contains("lastMatchAllTags", source);
        }

        [Fact]
        // Expected: WardrobeOverlay persists MatchAllTags to static field on toggle
        public void WardrobeOverlay_MatchAllToggle_PersistsToStaticField()
        {
            string source = SourceScanner.ReadSourceFile("UI/WardrobeOverlay.cs");
            Assert.Contains("lastMatchAllTags = filterState.MatchAllTags", source);
        }

        [Fact]
        // Expected: DrawWrappedTooltip text position uses x+16 and y+20
        public void UIHelpers_DrawWrappedTooltip_TextPosition()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/UIHelpers.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawWrappedTooltip");
            Assert.Contains("x + 20", body);
            Assert.Contains("y + 16", body);
        }

        // ----------------------------------------------------------------
        //  S29: ContinuousScrollHandler only uses arrow keys (no WASD)
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ContinuousScrollHandler.Update does not reference Keys.W, Keys.A, Keys.S, or Keys.D
        public void ContinuousScrollHandler_NoWASDKeys()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/ContinuousScrollHandler.cs");
            string body = SourceScanner.ExtractMethodBody(source, "int Update");
            Assert.DoesNotContain("Keys.W)", body);
            Assert.DoesNotContain("Keys.A)", body);
            Assert.DoesNotContain("Keys.S)", body);
            Assert.DoesNotContain("Keys.D)", body);
        }

        [Fact]
        // Expected: ContinuousScrollHandler.Update checks all four arrow keys
        public void ContinuousScrollHandler_ChecksAllArrowKeys()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/ContinuousScrollHandler.cs");
            string body = SourceScanner.ExtractMethodBody(source, "int Update");
            Assert.Contains("Keys.Up", body);
            Assert.Contains("Keys.Down", body);
            Assert.Contains("Keys.Left", body);
            Assert.Contains("Keys.Right", body);
        }

        [Fact]
        // Expected: ContinuousScrollHandler.Update signature has no arrowKeysOnly parameter
        public void ContinuousScrollHandler_NoArrowKeysOnlyParameter()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/ContinuousScrollHandler.cs");
            Assert.DoesNotContain("arrowKeysOnly", source);
        }

        // ----------------------------------------------------------------
        //  S30: Arrow key scrolling is config-gated in all menus
        // ----------------------------------------------------------------

        [Fact]
        // Expected: OutfitInputHandler.HandleKeyPress gates arrow key scrolling on ArrowKeyScrolling config
        public void OutfitInputHandler_HandleKeyPress_GatesArrowScrollOnConfig()
        {
            string source = SourceScanner.ReadSourceFile("Input/OutfitInputHandler.cs");
            string body = SourceScanner.ExtractMethodBody(source, "bool HandleKeyPress");
            Assert.Contains("ArrowKeyScrolling", body);
            Assert.Contains("Keys.Up", body);
            Assert.Contains("Keys.Down", body);
        }

        [Fact]
        // Expected: WardrobeOverlay.receiveKeyPress gates arrow key scrolling on ArrowKeyScrolling config
        public void WardrobeOverlay_ReceiveKeyPress_GatesArrowScrollOnConfig()
        {
            string source = SourceScanner.ReadSourceFile("UI/WardrobeOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void receiveKeyPress");
            Assert.Contains("ArrowKeyScrolling", body);
            Assert.Contains("Keys.Up", body);
            Assert.Contains("Keys.Down", body);
        }

        [Fact]
        // Expected: WardrobeOverlay.update gates continuous scroll on ArrowKeyScrolling config
        public void WardrobeOverlay_Update_GatesContinuousScrollOnConfig()
        {
            string source = SourceScanner.ReadSourceFile("UI/WardrobeOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void update");
            Assert.Contains("ArrowKeyScrolling", body);
            Assert.Contains("scrollHandler.Update", body);
        }

        [Fact]
        // Expected: ScheduleMenu.receiveKeyPress gates arrow key scrolling on ArrowKeyScrolling config
        public void ScheduleMenu_ReceiveKeyPress_GatesArrowScrollOnConfig()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void receiveKeyPress");
            Assert.Contains("ArrowKeyScrolling", body);
            Assert.Contains("Keys.Up", body);
            Assert.Contains("Keys.Down", body);
        }

        [Fact]
        // Expected: ScheduleMenu.update gates continuous scroll on ArrowKeyScrolling config
        public void ScheduleMenu_Update_GatesContinuousScrollOnConfig()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void update");
            Assert.Contains("ArrowKeyScrolling", body);
            Assert.Contains("scrollHandler.Update", body);
        }

        [Fact]
        // Expected: ScheduleEditOverlay.receiveKeyPress gates arrow key scrolling on ArrowKeyScrolling config
        public void ScheduleEditOverlay_ReceiveKeyPress_GatesArrowScrollOnConfig()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void receiveKeyPress");
            Assert.Contains("ArrowKeyScrolling", body);
            Assert.Contains("Keys.Up", body);
            Assert.Contains("Keys.Down", body);
        }

        [Fact]
        // Expected: ScheduleEditOverlay.update gates continuous scroll on ArrowKeyScrolling config
        public void ScheduleEditOverlay_Update_GatesContinuousScrollOnConfig()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void update");
            Assert.Contains("ArrowKeyScrolling", body);
            Assert.Contains("scrollHandler.Update", body);
        }

        // ----------------------------------------------------------------
        //  S31: ArrowKeyScrolling config option has all 6 touchpoints
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ModConfig declares ArrowKeyScrolling property with default true
        public void ModConfig_HasArrowKeyScrollingProperty()
        {
            string source = SourceScanner.ReadSourceFile("Core/ModConfig.cs");
            Assert.Contains("ArrowKeyScrolling", source);
            Assert.Contains("= true", source);
        }

        [Fact]
        // Expected: ConfigUIBuilder creates ArrowKeyScrolling checkbox component
        public void ConfigUIBuilder_HasArrowKeyScrollingCheckbox()
        {
            string source = SourceScanner.ReadSourceFile("UI/ConfigUIBuilder.cs");
            Assert.Contains("ArrowKeyScrollingCheckbox", source);
            Assert.Contains("ConfigArrowKeyScrollingName", source);
        }

        [Fact]
        // Expected: ConfigOverlay loads, toggles, saves, and draws ArrowKeyScrolling
        public void ConfigOverlay_HandlesArrowKeyScrolling()
        {
            string source = SourceScanner.ReadSourceFile("UI/ConfigOverlay.cs");
            Assert.Contains("arrowKeyScrolling", source);
            Assert.Contains("config.ArrowKeyScrolling = arrowKeyScrolling", source);
        }

        [Fact]
        // Expected: TranslationCache has ConfigArrowKeyScrolling properties initialized from i18n
        public void TranslationCache_HasArrowKeyScrollingTranslations()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/TranslationCache.cs");
            Assert.Contains("ConfigArrowKeyScrollingName", source);
            Assert.Contains("ConfigArrowKeyScrollingTooltip", source);
            Assert.Contains("config.arrow-key-scrolling.name", source);
        }

        [Fact]
        // Expected: default.json has i18n keys for ArrowKeyScrolling config option
        public void I18n_HasArrowKeyScrollingKeys()
        {
            string source = SourceScanner.ReadSourceFile("i18n/default.json");
            Assert.Contains("config.arrow-key-scrolling.name", source);
            Assert.Contains("config.arrow-key-scrolling.tooltip", source);
        }

        [Fact]
        // Expected: ModEntry registers ArrowKeyScrolling with GMCM
        public void ModEntry_RegistersArrowKeyScrollingWithGMCM()
        {
            string source = SourceScanner.ReadSourceFile("Core/ModEntry.cs");
            Assert.Contains("config.ArrowKeyScrolling", source);
            Assert.Contains("ConfigArrowKeyScrollingName", source);
        }

        // ----------------------------------------------------------------
        //  S32: Arrow key scroll blocking conditions
        // ----------------------------------------------------------------

        [Fact]
        // Expected: WardrobeOverlay blocks arrow key scrolling during delete confirmation and open dropdowns
        public void WardrobeOverlay_ArrowScroll_BlockedByDeleteConfirmAndDropdowns()
        {
            string source = SourceScanner.ReadSourceFile("UI/WardrobeOverlay.cs");
            string updateBody = SourceScanner.ExtractMethodBody(source, "override void update");
            Assert.Contains("!showDeleteConfirmation", updateBody);
            Assert.Contains("!searchScopeOpen", updateBody);
            Assert.Contains("!tagsDropdownOpen", updateBody);
            Assert.Contains("!filterDropdownOpen", updateBody);
        }

        [Fact]
        // Expected: ScheduleMenu blocks arrow key scrolling during delete confirmation and priority dropdown
        public void ScheduleMenu_ArrowScroll_BlockedByDeleteConfirmAndPriorityDropdown()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string updateBody = SourceScanner.ExtractMethodBody(source, "override void update");
            Assert.Contains("!showDeleteConfirmation", updateBody);
            Assert.Contains("!priorityDropdownOpen", updateBody);
        }

        [Fact]
        // Expected: ScheduleEditOverlay blocks continuous scroll when previewOverlay is active
        public void ScheduleEditOverlay_ArrowScroll_BlockedByPreview()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            string updateBody = SourceScanner.ExtractMethodBody(source, "override void update");
            Assert.Contains("previewOverlay == null", updateBody);
        }

        // ── S33–S48: ScheduleEdit refactor tests ──────────────────────────────

        [Fact]
        // Expected: Section header renamed from Triggers to Conditions
        public void ScheduleEditUIBuilder_HasConditionsHeader_NotTriggersHeader()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            Assert.Contains("DrawConditionsHeader", source);
            Assert.DoesNotContain("DrawTriggersHeader", source);
        }

        [Fact]
        // Expected: ScheduleEditUIBuilder has DrawSpecialEventsHeader method
        public void ScheduleEditUIBuilder_HasSpecialEventsHeader()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            Assert.Contains("DrawSpecialEventsHeader", source);
            string body = SourceScanner.ExtractMethodBody(source, "void DrawSpecialEventsHeader");
            // Bold double-draw pattern
            Assert.Contains("new Vector2(1, 0)", body);
        }

        [Fact]
        // Expected: Condition bars use placeholder parameter (no label: for condition dropdowns)
        public void ScheduleEditOverlay_ConditionBars_UsePlaceholder()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            string drawBody = SourceScanner.ExtractMethodBody(source, "override void draw");
            // All five condition bars pass placeholder:
            Assert.Contains("placeholder: seasonPlaceholder", drawBody);
            Assert.Contains("placeholder: weatherPlaceholder", drawBody);
            Assert.Contains("placeholder: areaPlaceholder", drawBody);
            Assert.Contains("placeholder: locationPlaceholder", drawBody);
            Assert.Contains("placeholder: festivalPlaceholder", drawBody);
            // Condition bars should NOT use label: parameter
            Assert.DoesNotContain("label: seasonLabel", drawBody);
            Assert.DoesNotContain("label: weatherLabel", drawBody);
            Assert.DoesNotContain("label: areaLabel", drawBody);
        }

        [Fact]
        // Expected: Festival and Location dropdowns use DrawInputBar when open
        public void ScheduleEditOverlay_SearchDropdowns_UseDrawInputBar()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            string drawBody = SourceScanner.ExtractMethodBody(source, "override void draw");
            Assert.Contains("locationsDropdownOpen", drawBody);
            Assert.Contains("festivalsDropdownOpen", drawBody);
            Assert.Contains("DrawInputBar", drawBody);
        }

        [Fact]
        // Expected: CloseAllDropdowns cleans up search state (subscriber, textbox, activeSearchDropdown)
        public void ScheduleEditOverlay_CloseAllDropdowns_CleansUpSearchState()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void CloseAllDropdowns");
            Assert.Contains("activeSearchDropdown", body);
            Assert.Contains("searchTextBox", body);
            Assert.Contains("keyboardDispatcher.Subscriber", body);
        }

        [Fact]
        // Expected: Draw method suppresses hover behind open dropdown panels
        public void ScheduleEditOverlay_Draw_SuppressesHoverBehindDropdownPanels()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            string drawBody = SourceScanner.ExtractMethodBody(source, "override void draw");
            Assert.Contains("IsAnyDropdownOpen", drawBody);
            Assert.Contains("IsInAnyDropdownPanel", drawBody);
            Assert.Contains("SuppressHover", drawBody);
        }

        [Fact]
        // Expected: Bottom buttons use Math.Max for same width
        public void ScheduleEditUIBuilder_BottomButtons_SameWidth()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void CalculateLayout");
            Assert.Contains("Math.Max(saveWidth, cancelWidth)", body);
        }

        [Fact]
        // Expected: Priority and Rotation use inline text draw methods from UIBuilder
        public void ScheduleEditOverlay_BehaviorRows_UseInlineTextDraw()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            string drawBody = SourceScanner.ExtractMethodBody(source, "override void draw");
            Assert.Contains("DrawPriorityRow", drawBody);
            Assert.Contains("DrawRotationRow", drawBody);
        }

        [Fact]
        // Expected: Recalculate uses new gap constants
        public void ScheduleEditUIBuilder_Recalculate_UsesNewGapConstants()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void Recalculate");
            Assert.Contains("ScheduleEditSectionHeaderHeight", body);
            Assert.Contains("ScheduleEditBarRowGap", body);
        }

        [Fact]
        // Expected: CreateClearButton uses ScheduleEditClearButtonLeftShift
        public void ScheduleEditUIBuilder_ClearButton_UsesLeftShift()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "ClickableComponent CreateClearButton");
            Assert.Contains("ScheduleEditClearButtonLeftShift", body);
        }

        [Fact]
        // Expected: TagPickerManager dims non-All tags when All is selected (0.4f opacity)
        public void TagPickerManager_DimsTagsWhenAllSelected()
        {
            string source = SourceScanner.ReadSourceFile("Managers/TagPickerManager.cs");
            string drawBody = SourceScanner.ExtractMethodBody(source, "void DrawTagOptions");
            Assert.Contains("isDimmed", drawBody);
            Assert.Contains("0.4f", drawBody);
        }

        [Fact]
        // Expected: TagPickerManager blocks clicks on dimmed tags when All is selected
        public void TagPickerManager_BlocksClicksOnDimmedTags()
        {
            string source = SourceScanner.ReadSourceFile("Managers/TagPickerManager.cs");
            string body = SourceScanner.ExtractMethodBody(source, "bool HandleClick");
            // After "AllOptionKey" check, there's an "else if (allSelected)" guard
            Assert.Contains("else if (allSelected)", body);
        }

        [Fact]
        // Expected: UIHelpers.DrawDropdownButton has placeholder parameter
        public void UIHelpers_DrawDropdownButton_HasPlaceholderParameter()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/UIHelpers.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawDropdownButton");
            Assert.Contains("placeholder", body);
            Assert.Contains("Color.Gray", body);
        }

        [Fact]
        // Expected: Old names removed — no AdvanceQueue or Triggers references remain
        public void ScheduleEdit_OldNamesRemoved()
        {
            string overlay = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            string uiBuilder = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string translations = SourceScanner.ReadSourceFile("Utilities/TranslationCache.cs");

            Assert.DoesNotContain("DrawTriggersHeader", overlay);
            Assert.DoesNotContain("DrawTriggersHeader", uiBuilder);
            Assert.DoesNotContain("ScheduleEditTriggers", translations);
            Assert.DoesNotContain("ScheduleEditAdvanceQueue", translations);
        }

        [Fact]
        // Expected: TranslationCache has renamed properties (Conditions, Rotate, SpecialEvents)
        public void TranslationCache_HasRenamedScheduleEditProperties()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/TranslationCache.cs");
            Assert.Contains("ScheduleEditConditions", source);
            Assert.Contains("ScheduleEditRotate", source);
            Assert.Contains("ScheduleEditSpecialEvents", source);
        }

        [Fact]
        // Expected: ScheduleEditOverlay blocks all keys (except Escape) when search dropdown is active
        public void ScheduleEditOverlay_SearchDropdown_BlocksNonEscapeKeys()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void receiveKeyPress");
            Assert.Contains("activeSearchDropdown", body);
            Assert.Contains("Keys.Escape", body);
        }

        [Fact]
        // Expected: ScheduleMenu blocks scroll wheel when priority dropdown or delete confirmation is active
        public void ScheduleMenu_ScrollWheel_BlockedByDropdownAndDeleteConfirm()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void receiveScrollWheelAction");
            Assert.Contains("priorityDropdownOpen", body);
            Assert.Contains("showDeleteConfirmation", body);
        }

        [Fact]
        // Expected: ScheduleMenu suppresses hover on rule list when priority dropdown is open
        public void ScheduleMenu_Draw_SuppressesRuleListHoverWhenDropdownOpen()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string drawBody = SourceScanner.ExtractMethodBody(source, "override void draw");
            Assert.Contains("priorityDropdownOpen", drawBody);
            Assert.Contains("SuppressHover = true", drawBody);
            Assert.Contains("SuppressHover = false", drawBody);
        }
        // ----------------------------------------------------------------
        //  S49: WardrobeOverlay Type B conversion
        // ----------------------------------------------------------------

        [Fact]
        // Expected: OutfitInputHandler opens wardrobe via Game1.activeClickableMenu swap (Type B)
        public void InputHandler_OpensWardrobe_ViaActiveClickableMenuSwap()
        {
            string source = SourceScanner.ReadSourceFile("Input/OutfitInputHandler.cs");
            Assert.Contains("Game1.activeClickableMenu = new WardrobeOverlay(", source);
        }

        [Fact]
        // Expected: WardrobeOverlay.CloseOverlay restores parentMenu as active menu
        public void WardrobeOverlay_CloseOverlay_RestoresParentMenu()
        {
            string source = SourceScanner.ReadSourceFile("UI/WardrobeOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void CloseOverlay");
            Assert.Contains("Game1.activeClickableMenu = parentMenu", body);
        }

        [Fact]
        // Expected: WardrobeOverlay no longer overrides readyToClose (Type B uses CloseOverlay instead)
        public void WardrobeOverlay_DoesNotOverride_ReadyToClose()
        {
            string source = SourceScanner.ReadSourceFile("UI/WardrobeOverlay.cs");
            Assert.DoesNotContain("override bool readyToClose", source);
        }

        [Fact]
        // Expected: OutfitMenu no longer references wardrobeOverlay (forwarding removed)
        public void OutfitMenu_NoLongerReferences_WardrobeOverlay()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitMenu.cs");
            Assert.DoesNotContain("wardrobeOverlay", source);
        }

        [Fact]
        // Expected: OutfitInputHandler no longer references wardrobe forwarding fields
        public void InputHandler_NoLongerReferences_WardrobeOverlay()
        {
            string source = SourceScanner.ReadSourceFile("Input/OutfitInputHandler.cs");
            Assert.DoesNotContain("getWardrobeOverlay", source);
            Assert.DoesNotContain("setWardrobeOverlay", source);
            Assert.DoesNotContain("getParentMenu", source);
        }

        [Fact]
        // Expected: WardrobeOverlay parentMenu field is IClickableMenu (not OutfitMenu)
        public void WardrobeOverlay_ParentMenu_IsIClickableMenu()
        {
            string source = SourceScanner.ReadSourceFile("UI/WardrobeOverlay.cs");
            Assert.Contains("IClickableMenu? parentMenu", source);
        }

        [Fact]
        // Expected: WardrobeOverlay.OpenEditOverlay passes self as parent to SaveSetOverlay
        public void WardrobeOverlay_OpenEditOverlay_PassesSelfAsParent()
        {
            string source = SourceScanner.ReadSourceFile("UI/WardrobeOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void OpenEditOverlay");
            Assert.Contains("new SaveSetOverlay(this,", body);
        }

        // ----------------------------------------------------------------
        //  S50: ScheduleDebugLogOverlay structural contracts
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleDebugLogOverlay.gameWindowSizeChanged calls RecalculateAndSync to recompute layout
        public void ScheduleDebugLogOverlay_GameWindowSizeChanged_CallsRecalculateAndSync()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogOverlay.cs");
            bool found = SourceScanner.MethodContains(source,
                "override void gameWindowSizeChanged", "RecalculateAndSync()");
            Assert.True(found,
                "ScheduleDebugLogOverlay.gameWindowSizeChanged must call RecalculateAndSync()");
        }

        [Fact]
        // Expected: ScheduleDebugLogOverlay clears keyboard dispatcher (Type B, no TextBox)
        public void ScheduleDebugLogOverlay_ClearsKeyboardDispatcher()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogOverlay.cs");
            Assert.Contains("keyboardDispatcher.Subscriber = null", source);
        }

        [Fact]
        // Expected: ScheduleDebugLogOverlay.receiveKeyPress gates arrow key scrolling on ArrowKeyScrolling config
        public void ScheduleDebugLogOverlay_ReceiveKeyPress_GatesArrowScrollOnConfig()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void receiveKeyPress");
            Assert.Contains("ArrowKeyScrolling", body);
            Assert.Contains("Keys.Up", body);
            Assert.Contains("Keys.Down", body);
        }

        [Fact]
        // Expected: ScheduleDebugLogOverlay.update gates continuous scroll on ArrowKeyScrolling config
        public void ScheduleDebugLogOverlay_Update_GatesContinuousScrollOnConfig()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void update");
            Assert.Contains("ArrowKeyScrolling", body);
            Assert.Contains("scrollHandler.Update", body);
        }

        [Fact]
        // Expected: ScheduleDebugLogOverlay.CloseOverlay restores parentMenu as active menu
        public void ScheduleDebugLogOverlay_CloseOverlay_RestoresParentMenu()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void CloseOverlay");
            Assert.Contains("Game1.activeClickableMenu = parentMenu", body);
        }

        // ----------------------------------------------------------------
        //  S51: OutfitMenu floating Debug Log button
        // ----------------------------------------------------------------

        [Fact]
        // Expected: OutfitUIBuilder has DebugLogButton property
        public void OutfitUIBuilder_HasDebugLogButton()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitUIBuilder.cs");
            Assert.Contains("DebugLogButton", source);
        }

        [Fact]
        // Expected: OutfitUIBuilder draws DebugLogButton gated on ShowScheduleDebugLog config
        public void OutfitUIBuilder_DrawFloatingButtons_GatesDebugLogOnConfig()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawFloatingButtons");
            Assert.Contains("ShowScheduleDebugLog", body);
            Assert.Contains("DrawDebugLogButton", body);
        }

        [Fact]
        // Expected: OutfitInputHandler handles DebugLogButton click to open ScheduleDebugLogOverlay
        public void OutfitInputHandler_HandlesDebugLogButtonClick()
        {
            string source = SourceScanner.ReadSourceFile("Input/OutfitInputHandler.cs");
            Assert.Contains("DebugLogButton.containsPoint", source);
            Assert.Contains("new ScheduleDebugLogOverlay", source);
        }

        [Fact]
        // Expected: OutfitMenu close-on-click-outside includes DebugLogButton
        public void OutfitMenu_CloseOnClickOutside_IncludesDebugLogButton()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void receiveLeftClick");
            Assert.Contains("DebugLogButton.containsPoint", body);
        }

        // ----------------------------------------------------------------
        //  S52: ScheduleDebugLogOverlay multi-expand and clear
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleDebugLogOverlay uses HashSet<int> for multi-expand instead of single int
        public void ScheduleDebugLogOverlay_UsesHashSetForMultiExpand()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogOverlay.cs");
            Assert.Contains("HashSet<int> expandedEntries", source);
        }

        [Fact]
        // Expected: Toggle logic adds/removes from HashSet (not single index assignment)
        public void ScheduleDebugLogOverlay_ToggleExpand_UsesHashSet()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void receiveLeftClick");
            Assert.Contains("expandedEntries.Contains(clickedIndex)", body);
            Assert.Contains("expandedEntries.Remove(clickedIndex)", body);
            Assert.Contains("expandedEntries.Add(clickedIndex)", body);
        }

        [Fact]
        // Expected: RecalculateAndSync builds Dictionary<int, int> from expandedEntries HashSet
        public void ScheduleDebugLogOverlay_RecalculateAndSync_BuildsExpandedRuleCounts()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void RecalculateAndSync");
            Assert.Contains("new Dictionary<int, int>()", body);
            Assert.Contains("foreach", body);
            Assert.Contains("expandedEntries", body);
        }

        [Fact]
        // Expected: ScheduleDebugLogOverlay has CollapseButton handling that collapses all expanded entries
        public void ScheduleDebugLogOverlay_CollapseButton_CollapsesAll()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void receiveLeftClick");
            Assert.Contains("CollapseButton.containsPoint", body);
            Assert.Contains("expandedEntries.Clear()", body);
        }

        [Fact]
        // Expected: ScheduleDebugLogOverlay draw calls DrawButtons which draws both Clear and Close
        public void ScheduleDebugLogOverlay_Draw_CallsDrawButtons()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void draw");
            Assert.Contains("uiBuilder.DrawButtons(b)", body);
        }

        // ----------------------------------------------------------------
        //  S53: ScheduleDebugLogUIBuilder changes
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleDebugLogUIBuilder has CollapseButton property
        public void ScheduleDebugLogUIBuilder_HasCollapseButton()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            Assert.Contains("CollapseButton", source);
        }

        [Fact]
        // Expected: Recalculate accepts expandedRuleHeights dictionary
        public void ScheduleDebugLogUIBuilder_Recalculate_AcceptsDictionary()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            Assert.Contains("void Recalculate(int entryCount, Dictionary<int, int> expandedRuleHeights)", source);
        }

        [Fact]
        // Expected: DrawCollapsedEntry uses LocationDisplayName for title
        public void ScheduleDebugLogUIBuilder_DrawCollapsedEntry_UsesLocationDisplayName()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawCollapsedEntry");
            Assert.Contains("LocationDisplayName", body);
        }

        [Fact]
        // Expected: DrawCollapsedEntry uses " > " arrow format for info line
        public void ScheduleDebugLogUIBuilder_DrawCollapsedEntry_UsesArrowFormat()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawCollapsedEntry");
            Assert.Contains("\" ~ \"", body);
        }

        [Fact]
        // Expected: DrawCollapsedEntry truncates rule and outfit parts independently (halfWidth)
        public void ScheduleDebugLogUIBuilder_DrawCollapsedEntry_TruncatesIndependently()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawCollapsedEntry");
            Assert.Contains("halfWidth", body);
            Assert.Contains("TruncateText(rulePart, halfWidth)", body);
            Assert.Contains("TruncateText(outfitPart, halfWidth)", body);
        }

        [Fact]
        // Expected: Cached path draws all matched rules, not just the first
        public void ScheduleDebugLogUIBuilder_CachedPath_DrawsAllMatchedRules()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "string? DrawExpandedSections");
            // Find the cached rules branch (in the Rules section)
            int rulesSection = body.IndexOf("// --- Rules ---");
            Assert.True(rulesSection >= 0, "Rules section must exist");
            string rulesBody = body.Substring(rulesSection);
            int cachedIdx = rulesBody.IndexOf("CacheOutcome == EvalCacheOutcome.Cached");
            Assert.True(cachedIdx >= 0, "Cached branch in rules section must exist");
            // After the cached check, must have a foreach loop (not just [0])
            string afterCached = rulesBody.Substring(cachedIdx);
            int foreachIdx = afterCached.IndexOf("foreach");
            int elseIdx = afterCached.IndexOf("else if");
            Assert.True(foreachIdx >= 0 && foreachIdx < elseIdx,
                "Cached path must iterate rules with foreach before next branch");
        }

        [Fact]
        // Expected: DrawExpandedSections draws sections in order: Context > Result > Rules
        public void ScheduleDebugLogUIBuilder_DrawExpandedSections_SectionOrder()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawExpandedSections");
            int ctxIdx = body.IndexOf("ScheduleDebugContext");
            int resIdx = body.IndexOf("ScheduleDebugResult");
            int rulIdx = body.IndexOf("DrawRulesHeader");
            Assert.True(ctxIdx < resIdx && resIdx < rulIdx,
                "Expanded sections must be ordered Context > Result > Rules");
        }

        [Fact]
        // Expected: DrawButtons draws both Collapse and Close buttons
        public void ScheduleDebugLogUIBuilder_DrawButtons_DrawsCollapseAndClose()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawButtons");
            Assert.Contains("CollapseButton", body);
            Assert.Contains("CloseMenuButton", body);
        }

        [Fact]
        // Expected: DrawScrollIndicators positions arrows in right border padding area
        public void ScheduleDebugLogUIBuilder_DrawScrollIndicators_ArrowsInRightPadding()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawScrollIndicators");
            Assert.Contains("rightPaddingStart", body);
            Assert.Contains("arrowX", body);
        }

        [Fact]
        // Expected: DayStarted title format includes " | " separator with location
        public void ScheduleDebugLogUIBuilder_DrawCollapsedEntry_DayStartedTitleFormat()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawCollapsedEntry");
            Assert.Contains("ScheduleDebugTriggerDayStarted", body);
        }

        // ----------------------------------------------------------------
        //  S54: ScheduleEvalEntry.LocationDisplayName and engine population
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleEvalEntry model has LocationDisplayName property
        public void ScheduleEvalEntry_HasLocationDisplayName()
        {
            string source = SourceScanner.ReadSourceFile("Models/ScheduleEvalLog.cs");
            Assert.Contains("LocationDisplayName", source);
        }

        [Fact]
        // Expected: ScheduleEngine populates LocationDisplayName in the eval log entry
        public void ScheduleEngine_PopulatesLocationDisplayName()
        {
            string source = SourceScanner.ReadSourceFile("Services/ScheduleEngine.cs");
            Assert.Contains("logEntry.LocationDisplayName", source);
        }

        // ----------------------------------------------------------------
        //  S56: ScheduleDebugLogOverlay scroll preservation on expand/collapse
        // ----------------------------------------------------------------

        [Fact]
        // Expected: Toggle expand does not reset ScrollOffset to 0
        public void ScheduleDebugLogOverlay_ToggleExpand_PreservesScroll()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void receiveLeftClick");
            // Find the expand/collapse block and ensure it does NOT reset scroll
            int toggleIdx = body.IndexOf("expandedEntries.Add(clickedIndex)");
            int nextReturnIdx = body.IndexOf("}", toggleIdx);
            string toggleBlock = body.Substring(toggleIdx, nextReturnIdx - toggleIdx);
            Assert.DoesNotContain("ScrollOffset = 0", toggleBlock);
        }

        [Fact]
        // Expected: CollapseButton handler calls RecalculateAndSync after clearing expanded entries
        public void ScheduleDebugLogOverlay_CollapseButton_RecalculatesAfterCollapse()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void receiveLeftClick");
            int collapseIdx = body.IndexOf("CollapseButton.containsPoint");
            Assert.True(collapseIdx >= 0, "CollapseButton handler must exist");
            string afterCollapse = body.Substring(collapseIdx, Math.Min(300, body.Length - collapseIdx));
            Assert.Contains("expandedEntries.Clear()", afterCollapse);
            Assert.Contains("RecalculateAndSync()", afterCollapse);
        }

        // ----------------------------------------------------------------
        //  S57: Layout constants for debug log changes
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleDebugLogWidth is 700
        public void OutfitLayoutConstants_ScheduleDebugLogWidth_Is700()
        {
            string source = SourceScanner.ReadSourceFile("Core/OutfitLayoutConstants.cs");
            Assert.Contains("ScheduleDebugLogWidth = 700", source);
        }

        [Fact]
        // Expected: ScheduleDebugMaxVisibleEntries is 5 (was 6)
        public void OutfitLayoutConstants_ScheduleDebugMaxVisibleEntries_Is5()
        {
            string source = SourceScanner.ReadSourceFile("Core/OutfitLayoutConstants.cs");
            Assert.Contains("ScheduleDebugMaxVisibleEntries = 5", source);
        }

        [Fact]
        // Expected: ScheduleDebugTitleToInfoGap constant exists for 8px gap
        public void OutfitLayoutConstants_HasScheduleDebugTitleToInfoGap()
        {
            string source = SourceScanner.ReadSourceFile("Core/OutfitLayoutConstants.cs");
            Assert.Contains("ScheduleDebugTitleToInfoGap", source);
        }

        [Fact]
        // Expected: ScheduleDebugButtonGap constant exists for Clear/Close spacing
        public void OutfitLayoutConstants_HasScheduleDebugButtonGap()
        {
            string source = SourceScanner.ReadSourceFile("Core/OutfitLayoutConstants.cs");
            Assert.Contains("ScheduleDebugButtonGap", source);
        }

        // ----------------------------------------------------------------
        //  S58: ScheduleEvalLog.Clear method
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleEvalLog service has Clear method
        public void ScheduleEvalLog_HasClearMethod()
        {
            string source = SourceScanner.ReadSourceFile("Services/ScheduleEvalLog.cs");
            Assert.Contains("void Clear()", source);
        }

        // ----------------------------------------------------------------
        //  S59: Debug Log UI polish (v2.0.0 batch)
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleDebugItemGap is 10 for proper rule spacing
        public void OutfitLayoutConstants_ScheduleDebugItemGap_Is10()
        {
            string source = SourceScanner.ReadSourceFile("Core/OutfitLayoutConstants.cs");
            Assert.Contains("ScheduleDebugItemGap = 10", source);
        }

        [Fact]
        // Expected: ScheduleDebugContextLineHeight is 31 (+1px gap between lines)
        public void OutfitLayoutConstants_ScheduleDebugContextLineHeight_Is31()
        {
            string source = SourceScanner.ReadSourceFile("Core/OutfitLayoutConstants.cs");
            Assert.Contains("ScheduleDebugContextLineHeight = 31", source);
        }

        [Fact]
        // Expected: ScheduleDebugExpandedSectionPad is 18 for info-to-Context gap
        public void OutfitLayoutConstants_ScheduleDebugExpandedSectionPad_Is18()
        {
            string source = SourceScanner.ReadSourceFile("Core/OutfitLayoutConstants.cs");
            Assert.Contains("ScheduleDebugExpandedSectionPad = 18", source);
        }

        [Fact]
        // Expected: ScheduleDebugTitleToInfoGap is 6 (reduced by 2px)
        public void OutfitLayoutConstants_ScheduleDebugTitleToInfoGap_Is6()
        {
            string source = SourceScanner.ReadSourceFile("Core/OutfitLayoutConstants.cs");
            Assert.Contains("ScheduleDebugTitleToInfoGap = 6", source);
        }

        [Fact]
        // Expected: contentWidth subtracts extra 20px for right-side padding
        public void ScheduleDebugLogUIBuilder_ContentWidth_Has20pxRightPadding()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void Recalculate");
            Assert.Contains("ScheduleDebugBorderPadding * 2 - 20", body);
        }

        [Fact]
        // Expected: Hover rect is clipped to EntryListClipRect to prevent overflow
        public void ScheduleDebugLogUIBuilder_DrawCollapsedEntry_ClipsHoverToClipRect()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawCollapsedEntry");
            Assert.Contains("Rectangle.Intersect", body);
            Assert.Contains("EntryListClipRect", body);
        }

        [Fact]
        // Expected: EnsureEntryVisible method exists for scroll adjustment on expand
        public void ScheduleDebugLogUIBuilder_HasEnsureEntryVisible()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            Assert.Contains("void EnsureEntryVisible(int entryIndex)", source);
        }

        [Fact]
        // Expected: EnsureEntryVisible scrolls to show expanded entry bottom, capped at entry top
        public void ScheduleDebugLogUIBuilder_EnsureEntryVisible_ScrollsToShowEntry()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void EnsureEntryVisible");
            Assert.Contains("entryBottom > visibleBottom", body);
            Assert.Contains("Math.Min(entryBottom - entryListHeight, entryTop)", body);
            Assert.Contains("ClampScrollOffset()", body);
        }

        [Fact]
        // Expected: Overlay preserves scroll position when expanding/collapsing entries
        public void ScheduleDebugLogOverlay_Expand_PreservesScrollPosition()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void receiveLeftClick");
            Assert.Contains("screenYBefore", body);
            Assert.Contains("screenYAfter", body);
            Assert.Contains("ScrollOffset +=", body);
        }

        [Fact]
        // Expected: Reason line in expanded view shows Tiebreak via TranslationCache
        public void ScheduleDebugLogUIBuilder_ReasonLine_ShowsTiebreak()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawExpandedSections");
            Assert.Contains("ScheduleDebugLabelReason", body);
            Assert.Contains("TranslationCache.ScheduleDebugTiebreak", body);
        }

        [Fact]
        // Expected: Failed rule X icon uses faint red (IndianRed) tint
        public void ScheduleDebugLogUIBuilder_DrawFailedRuleLine_XIconFaintRed()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "string? DrawFailedRuleLine");
            Assert.Contains("FailIconSourceRect, failColor", body);
            Assert.Contains("Color.IndianRed", body);
        }

        [Fact]
        // Expected: X icon moved down by 2px in failed rule lines
        public void ScheduleDebugLogUIBuilder_DrawFailedRuleLine_XIconMovedDown2px()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "string? DrawFailedRuleLine");
            Assert.Contains("/ 2 + 2", body);
        }

        [Fact]
        // Expected: X icon gap to fail label is 6px
        public void ScheduleDebugLogUIBuilder_DrawFailedRuleLine_XIconGap6()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "string? DrawFailedRuleLine");
            Assert.Contains("xIconGap = 6", body);
        }

        [Fact]
        // Expected: Matched rules drawn in ForestGreen with priority suffix
        public void ScheduleDebugLogUIBuilder_DrawMatchedRuleSummary_GreenWithPriority()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "string? DrawMatchedRuleSummary");
            Assert.Contains("Color.ForestGreen", body);
            Assert.Contains("GetPriorityLabel", body);
            Assert.Contains("nameDisplay + prioritySuffix", body);
        }

        [Fact]
        // Expected: Priority short labels S/H/M/L still available via GetPriorityShortLabel
        public void ScheduleDebugLogUIBuilder_GetPriorityShortLabel_ReturnsSHML()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "string GetPriorityShortLabel");
            Assert.Contains("\"S\"", body);
            Assert.Contains("\"H\"", body);
            Assert.Contains("\"M\"", body);
            Assert.Contains("\"L\"", body);
        }

        [Fact]
        // Expected: Rules section groups failed rules by priority with headers
        public void ScheduleDebugLogUIBuilder_DrawExpandedSections_GroupsFailedByPriority()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawExpandedSections");
            Assert.Contains("ScheduleDebugPriorityGroupSpecial", body);
            Assert.Contains("ScheduleDebugPriorityGroupHigh", body);
            Assert.Contains("ScheduleDebugPriorityGroupMedium", body);
            Assert.Contains("ScheduleDebugPriorityGroupLow", body);
            Assert.Contains("DrawMatchedRuleSummary", body);
            Assert.Contains("DrawFailedRuleLine", body);
        }

        [Fact]
        // Expected: Line 1 (location/trigger) has truncation tooltip when text is truncated
        public void ScheduleDebugLogUIBuilder_DrawCollapsedEntry_Line1HasTruncationTooltip()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawCollapsedEntry");
            Assert.Contains("line1Display != line1Text", body);
        }

        //  S59: ScheduleDebugLogOverlay scissor clipping

        [Fact]
        // Expected: ScheduleDebugLogOverlay has ScissorEnabled RasterizerState field for clipping
        public void ScheduleDebugLogOverlay_HasScissorEnabledField()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogOverlay.cs");
            Assert.Contains("ScissorEnabled", source);
            Assert.Contains("ScissorTestEnable = true", source);
        }

        [Fact]
        // Expected: ScheduleDebugLogOverlay.draw uses scissor rect around entry list drawing
        public void ScheduleDebugLogOverlay_Draw_UsesScissorClipping()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "draw");
            Assert.Contains("ScissorRectangle", body);
            Assert.Contains("EntryListClipRect", body);
            Assert.Contains("ScissorEnabled", body);
        }

        [Fact]
        // Expected: ScheduleDebugLogOverlay.draw restores old scissor rect after drawing entries
        public void ScheduleDebugLogOverlay_Draw_RestoresOldScissorRect()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "draw");
            Assert.Contains("oldScissorRect", body);
        }

        //  S60: Rotation reshuffled display

        [Fact]
        // Expected: DrawExpandedSections checks WasReshuffled before QueueSizeAfter for rotation line
        public void ScheduleDebugLogUIBuilder_DrawExpandedSections_ReshuffledBeforeQueueCheck()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawExpandedSections");
            int reshuffledIdx = body.IndexOf("WasReshuffled");
            int queueIdx = body.IndexOf("QueueSizeAfter");
            Assert.True(reshuffledIdx >= 0, "Must check WasReshuffled");
            Assert.True(reshuffledIdx < queueIdx, "WasReshuffled must be checked before QueueSizeAfter");
        }

        [Fact]
        // Expected: Reshuffled rotation line uses ScheduleDebugRotationReshuffled translation
        public void ScheduleDebugLogUIBuilder_DrawExpandedSections_UsesReshuffledTranslation()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawExpandedSections");
            Assert.Contains("ScheduleDebugRotationReshuffled", body);
        }

        [Fact]
        // Expected: ScheduleDebugRotationReshuffled translation exists in TranslationCache
        public void TranslationCache_HasScheduleDebugRotationReshuffled()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/TranslationCache.cs");
            Assert.Contains("ScheduleDebugRotationReshuffled", source);
            Assert.Contains("schedule.debug.rotation-reshuffled", source);
        }

        //  S61: ManualOverrideOutfitName fallback

        [Fact]
        // Expected: ScheduleEvalEntry has ManualOverrideOutfitName property
        public void ScheduleEvalEntry_HasManualOverrideOutfitName()
        {
            string source = SourceScanner.ReadSourceFile("Models/ScheduleEvalLog.cs");
            Assert.Contains("ManualOverrideOutfitName", source);
        }

        [Fact]
        // Expected: ScheduleEngine populates ManualOverrideOutfitName at log entry creation
        public void ScheduleEngine_PopulatesManualOverrideOutfitName()
        {
            string source = SourceScanner.ReadSourceFile("Services/ScheduleEngine.cs");
            Assert.Contains("ManualOverrideOutfitName", source);
        }

        [Fact]
        // Expected: Collapsed entry outfit part uses ?? chain: ChosenOutfitName ?? ManualOverrideOutfitName ?? em-dash
        public void ScheduleDebugLogUIBuilder_DrawCollapsedEntry_FallsBackToManualOverrideName()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawCollapsedEntry");
            Assert.Contains("ChosenOutfitName ?? entry.ManualOverrideOutfitName ??", body);
        }

        //  S62: TotalRules/EnabledRules populated at log entry creation

        [Fact]
        // Expected: ScheduleEngine sets TotalRules and EnabledRules at log entry creation (before cache check)
        public void ScheduleEngine_SetsRuleCountsAtLogEntryCreation()
        {
            string source = SourceScanner.ReadSourceFile("Services/ScheduleEngine.cs");
            int createEntryIdx = source.IndexOf("evalLog.CreateEntry()");
            int cacheCheckIdx = source.IndexOf("contextCache.TryGetValue");
            Assert.True(createEntryIdx >= 0 && cacheCheckIdx >= 0);
            string betweenCreateAndCache = source.Substring(createEntryIdx, cacheCheckIdx - createEntryIdx);
            Assert.Contains("TotalRules", betweenCreateAndCache);
            Assert.Contains("EnabledRules", betweenCreateAndCache);
        }

        [Fact]
        // Expected: Cached path adds RuleEvalEntry for ALL tied rules, not just the winner
        public void ScheduleEngine_CachedPath_AddsAllTiedRulesToResults()
        {
            string source = SourceScanner.ReadSourceFile("Services/ScheduleEngine.cs");
            // Find the cached path (between contextCache.TryGetValue and the full-eval "var rules = ")
            int cacheHitIdx = source.IndexOf("contextCache.TryGetValue");
            int fullEvalIdx = source.IndexOf("var rules = scheduleStore.GetRules()");
            Assert.True(cacheHitIdx >= 0 && fullEvalIdx >= 0);
            string cachedSection = source.Substring(cacheHitIdx, fullEvalIdx - cacheHitIdx);

            // Must iterate cached.WinningRuleIds when adding RuleResults
            Assert.Contains("foreach", cachedSection);
            Assert.Contains("cached.WinningRuleIds", cachedSection);
            Assert.Contains("RuleResults.Add", cachedSection);
        }

        //  S63: Collapse button (renamed from Clear)

        [Fact]
        // Expected: ScheduleDebugCollapse translation exists in TranslationCache
        public void TranslationCache_HasScheduleDebugCollapse()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/TranslationCache.cs");
            Assert.Contains("ScheduleDebugCollapse", source);
            Assert.Contains("schedule.debug.collapse", source);
        }

        [Fact]
        // Expected: CollapseButton only collapses when entries are expanded (guards with Count > 0)
        public void ScheduleDebugLogOverlay_CollapseButton_GuardsOnExpandedCount()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void receiveLeftClick");
            Assert.Contains("expandedEntries.Count > 0", body);
        }

        //  S65: Debug log UI polish batch

        [Fact]
        // Expected: DrawCollapsedEntry expanded background uses full entryHeight, not just collapsed row height
        public void ScheduleDebugLogUIBuilder_ExpandedBackground_UsesFullEntryHeight()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            Assert.Contains("int entryHeight", source);
            string body = SourceScanner.ExtractMethodBody(source, "DrawCollapsedEntry");
            Assert.Contains("new Rectangle(contentX, rowY, contentWidth, entryHeight)", body);
        }

        [Fact]
        // Expected: Collapsed entry uses em-dash for missing rule/outfit instead of NoMatch translation
        public void ScheduleDebugLogUIBuilder_CollapsedEntry_UsesEmDashNotNoMatch()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawCollapsedEntry");
            Assert.DoesNotContain("ScheduleDebugNoMatch", body);
            Assert.Contains("\\u2014", body);
        }

        [Fact]
        // Expected: Speed icon in collapsed entry has +1 vertical offset for alignment
        public void ScheduleDebugLogUIBuilder_CollapsedEntry_SpeedIcon1pxDown()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawCollapsedEntry");
            Assert.Contains("/ 2 + 1", body);
        }

        [Fact]
        // Expected: DrawExpandedSections includes a Reason line with Tiebreak and Priority logic
        public void ScheduleDebugLogUIBuilder_ExpandedSections_HasReasonLine()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawExpandedSections");
            Assert.Contains("ScheduleDebugLabelReason", body);
            Assert.Contains("WasTiebreak", body);
            Assert.Contains("ScheduleEditPriority", body);
        }

        [Fact]
        // Expected: CalculateExpandedHeight Result section has 5 lines (SELECTED, Reason, Outfit, Status, Rotation)
        public void ScheduleDebugLogUIBuilder_ExpandedHeight_ResultSection5Lines()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "int CalculateExpandedHeight");
            Assert.Contains("5 * ScheduleDebugContextLineHeight", body);
        }

        [Fact]
        // Expected: CollapseButton resets ScrollOffset to 0 after collapsing
        public void ScheduleDebugLogOverlay_CollapseButton_ScrollsToTop()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void receiveLeftClick");
            int collapseIdx = body.IndexOf("expandedEntries.Clear()");
            int scrollIdx = body.IndexOf("ScrollOffset = 0");
            Assert.True(collapseIdx >= 0 && scrollIdx > collapseIdx,
                "ScrollOffset must be reset to 0 after collapse");
        }

        [Fact]
        // Expected: Failed rules under priority groups use PriorityGroupGap spacing
        public void ScheduleDebugLogUIBuilder_PriorityGroups_UseGapConstant()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawExpandedSections");
            Assert.Contains("PriorityGroupGap", body);
            Assert.Contains("PriorityHeaderToRulesGap", body);
        }

        [Fact]
        // Expected: Failed rules under priority groups use PriorityRuleExtraIndent (20px)
        public void ScheduleDebugLogUIBuilder_PriorityRules_Use20pxIndent()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            Assert.Contains("PriorityRuleExtraIndent = 20", source);
            string body = SourceScanner.ExtractMethodBody(source, "DrawExpandedSections");
            Assert.Contains("lineX + PriorityRuleExtraIndent", body);
        }

        [Fact]
        // Expected: CalculateRuleSectionHeight includes PriorityGroupGap and PriorityHeaderToRulesGap in pixel calc
        public void ScheduleDebugLogUIBuilder_RuleSectionHeight_IncludesGaps()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "int CalculateRuleSectionHeight");
            Assert.Contains("PriorityGroupGap", body);
            Assert.Contains("PriorityHeaderToRulesGap", body);
        }

        [Fact]
        // Expected: DrawFailedRuleLine draws rule name in default text color and fail label in IndianRed
        public void ScheduleDebugLogUIBuilder_FailedRuleLine_NameDefaultColor_FailRed()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "string? DrawFailedRuleLine");
            Assert.Contains("nameDisplay, Game1.smallFont", body);
            Assert.Contains("Game1.textColor * 0.8f", body);
            Assert.Contains("failLabel, Game1.smallFont", body);
            Assert.Contains("Color.IndianRed", body);
        }

        [Fact]
        // Expected: All expanded section labels use TranslationCache (no hardcoded "Status:", "Rotation:", etc.)
        public void ScheduleDebugLogUIBuilder_ExpandedSections_AllLabelsTranslated()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawExpandedSections");
            Assert.Contains("ScheduleDebugLabelSelected", body);
            Assert.Contains("ScheduleDebugLabelReason", body);
            Assert.Contains("ScheduleDebugLabelOutfit", body);
            Assert.Contains("ScheduleDebugLabelStatus", body);
            Assert.Contains("ScheduleDebugLabelRotation", body);
            Assert.Contains("ScheduleEditSeason", body);
            Assert.Contains("ScheduleEditWeather", body);
            Assert.Contains("ScheduleEditLocation", body);
            Assert.Contains("ScheduleEditArea", body);
            Assert.Contains("ScheduleEditFestival", body);
            Assert.Contains("ScheduleEditWedding", body);
        }

        [Fact]
        // Expected: GetFailLabel uses TranslationCache for all fail reasons
        public void ScheduleDebugLogUIBuilder_GetFailLabel_UsesTranslationCache()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "string GetFailLabel");
            Assert.Contains("ScheduleEditSeason", body);
            Assert.Contains("ScheduleEditFestival", body);
            Assert.Contains("ScheduleEditWeather", body);
            Assert.Contains("ScheduleEditLocation", body);
            Assert.Contains("ScheduleEditArea", body);
            Assert.Contains("ScheduleEditWedding", body);
            Assert.Contains("ScheduleDebugFailEmptyPool", body);
        }

        [Fact]
        // Expected: GetPriorityLabel uses TranslationCache for High/Medium/Low
        public void ScheduleDebugLogUIBuilder_GetPriorityLabel_UsesTranslationCache()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "string GetPriorityLabel");
            Assert.Contains("ScheduleEditPriorityHigh", body);
            Assert.Contains("ScheduleEditPriorityMedium", body);
            Assert.Contains("ScheduleEditPriorityLow", body);
        }

        [Fact]
        // Expected: Priority group headers use TranslationCache
        public void ScheduleDebugLogUIBuilder_PriorityGroupHeaders_UseTranslationCache()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawExpandedSections");
            Assert.Contains("ScheduleDebugPriorityGroupSpecial", body);
            Assert.Contains("ScheduleDebugPriorityGroupHigh", body);
            Assert.Contains("ScheduleDebugPriorityGroupMedium", body);
            Assert.Contains("ScheduleDebugPriorityGroupLow", body);
        }

        [Theory]
        [InlineData("schedule.debug.label-selected")]
        [InlineData("schedule.debug.label-reason")]
        [InlineData("schedule.debug.label-outfit")]
        [InlineData("schedule.debug.label-status")]
        [InlineData("schedule.debug.label-rotation")]
        [InlineData("schedule.debug.trigger-daystarted")]
        [InlineData("schedule.debug.fail-empty-pool")]
        [InlineData("schedule.debug.priority-group-high")]
        [InlineData("schedule.debug.priority-group-medium")]
        [InlineData("schedule.debug.priority-group-low")]
        [InlineData("schedule.debug.tiebreak-consistent")]
        [InlineData("schedule.debug.tiebreak-random")]
        [InlineData("schedule.debug.reason-manual")]
        // Expected: All new debug log i18n keys exist in default.json
        public void DefaultJson_HasScheduleDebugLabelKey(string key)
        {
            string json = SourceScanner.ReadSourceFile("i18n/default.json");
            Assert.Contains($"\"{key}\"", json);
        }

        [Fact]
        // Expected: Reason line handles manual override with ScheduleDebugReasonManual
        public void ScheduleDebugLogUIBuilder_ReasonLine_HandlesManualOverride()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawExpandedSections");
            Assert.Contains("ManualOverrideOutfitName", body);
            Assert.Contains("ScheduleDebugReasonManual", body);
        }

        [Fact]
        // Expected: Outfit line falls back to ManualOverrideOutfitName
        public void ScheduleDebugLogUIBuilder_OutfitLine_FallsBackToManualOverride()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawExpandedSections");
            Assert.Contains("ChosenOutfitName ?? entry.ManualOverrideOutfitName", body);
        }

        [Fact]
        // Expected: Rules section is hidden for manual override entries
        public void ScheduleDebugLogUIBuilder_RulesSection_HiddenForManualOverride()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawExpandedSections");
            Assert.Contains("isManualOverride", body);
            Assert.Contains("!isManualOverride", body);
        }

        [Fact]
        // Expected: CalculateRuleSectionHeight returns 0 for manual override
        public void ScheduleDebugLogUIBuilder_RuleSectionHeight_ZeroForManualOverride()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "int CalculateRuleSectionHeight");
            Assert.Contains("isManualOverride", body);
        }

        [Fact]
        // Expected: CalculateExpandedHeight skips rules section when ruleSectionHeight is 0
        public void ScheduleDebugLogUIBuilder_ExpandedHeight_SkipsRulesWhenZero()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "int CalculateExpandedHeight");
            Assert.Contains("ruleSectionHeight > 0", body);
        }

        [Fact]
        // Expected: Tiebreak reason line shows tooltip with TiedRuleNames on hover
        public void ScheduleDebugLogUIBuilder_TiebreakReason_ShowsTiedRuleNamesTooltip()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawExpandedSections");
            Assert.Contains("TiedRuleNames", body);
        }

        [Fact]
        // Expected: OutfitUIBuilder debug log button uses ButtonHoveringScale for hover effect
        public void OutfitUIBuilder_DebugLogButton_BoxScalesOnHover()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawDebugLogButton");
            Assert.Contains("ButtonHoveringScale", body);
            Assert.Contains("bgSize", body);
        }

        // ----------------------------------------------------------------
        //  S62: Schedule rule list numbering
        // ----------------------------------------------------------------

        [Fact]
        // Expected: DrawRuleRow draws rule name with truncation and hover highlight
        public void ScheduleMenuUIBuilder_DrawRuleRow_DrawsNameAndHighlight()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenuUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "string? DrawRuleRow");
            Assert.Contains("TruncateText", body);
            Assert.Contains("Color.Wheat", body);
        }

        // ----------------------------------------------------------------
        //  S63: Debug log Collapse/Close same width
        // ----------------------------------------------------------------

        [Fact]
        // Expected: Collapse and Close buttons use Math.Max for equal width
        public void ScheduleDebugLogUIBuilder_CollapseCloseButtons_SameWidth()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            Assert.Contains("Math.Max(collapseWidth, closeWidth)", source);
        }

        // ----------------------------------------------------------------
        //  S64: Debug log context line tooltip shows value only
        // ----------------------------------------------------------------

        [Fact]
        // Expected: DrawContextLine accepts separate label and value parameters
        public void ScheduleDebugLogUIBuilder_DrawContextLine_HasLabelAndValueParams()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "string? DrawContextLine");
            Assert.Contains("leftLabel", body);
            Assert.Contains("leftValue", body);
            Assert.Contains("rightLabel", body);
            Assert.Contains("rightValue", body);
        }

        [Fact]
        // Expected: DrawContextLine tooltip returns value only, not the full "Label: Value" string
        public void ScheduleDebugLogUIBuilder_DrawContextLine_TooltipReturnsValueOnly()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "string? DrawContextLine");
            Assert.Contains("tooltip = leftValue", body);
            Assert.Contains("tooltip = rightValue", body);
            Assert.DoesNotContain("tooltip = leftFull", body);
            Assert.DoesNotContain("tooltip = rightFull", body);
        }

        [Fact]
        // Expected: DrawContextLine callers pass label and value as separate args (not pre-formatted)
        public void ScheduleDebugLogUIBuilder_ContextLines_PassSeparateLabelAndValue()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleDebugLogUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "DrawExpandedSections");
            Assert.Contains("ScheduleEditFestival, festivalText", body);
            Assert.Contains("ScheduleEditLocation, entry.LocationName", body);
            Assert.Contains("ScheduleEditWeather, entry.Weather", body);
        }

        [Fact]
        // Expected: TranslationCache uses "ScheduleEditFestival" (singular) not "ScheduleEditFestivals"
        public void TranslationCache_UsesSingularFestivalLabel()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/TranslationCache.cs");
            Assert.Contains("ScheduleEditFestival", source);
            Assert.DoesNotContain("ScheduleEditFestivals", source);
        }

        // ----------------------------------------------------------------
        //  S65: Manual outfit snapshot for individual items
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleEngine uses ManualOutfitSnapshot instead of string for manual cache
        public void ScheduleEngine_ManualCacheUsesSnapshot()
        {
            string source = SourceScanner.ReadSourceFile("Services/ScheduleEngine.cs");
            Assert.Contains("Dictionary<string, ManualOutfitSnapshot>", source);
            Assert.Contains("ManualOutfitSnapshot? lastManualSnapshot", source);
        }

        [Fact]
        // Expected: SetManualOutfit accepts ManualOutfitSnapshot parameter
        public void ScheduleEngine_SetManualOutfit_AcceptsSnapshot()
        {
            string source = SourceScanner.ReadSourceFile("Services/ScheduleEngine.cs");
            Assert.Contains("SetManualOutfit(ManualOutfitSnapshot snapshot)", source);
        }

        [Fact]
        // Expected: TryApplyManualOverride uses value comparison via MatchesCurrentPlayer
        public void ScheduleEngine_TryApplyManualOverride_UsesValueComparison()
        {
            string source = SourceScanner.ReadSourceFile("Services/ScheduleEngine.cs");
            string body = SourceScanner.ExtractMethodBody(source, "bool TryApplyManualOverride");
            Assert.Contains("MatchesCurrentPlayer", body);
        }

        [Fact]
        // Expected: ManualOutfitSnapshot has FromCurrentPlayer and FromOutfitSet factory methods
        public void ManualOutfitSnapshot_HasFactoryMethods()
        {
            string source = SourceScanner.ReadSourceFile("Services/ScheduleEngine.cs");
            Assert.Contains("FromCurrentPlayer()", source);
            Assert.Contains("FromOutfitSet(OutfitSet set)", source);
        }

        [Fact]
        // Expected: ManualOutfitSnapshot has EquipmentEquals for value comparison ignoring metadata
        public void ManualOutfitSnapshot_HasEquipmentEquals()
        {
            string source = SourceScanner.ReadSourceFile("Services/ScheduleEngine.cs");
            Assert.Contains("EquipmentEquals(ManualOutfitSnapshot other)", source);
        }

        [Fact]
        // Expected: OutfitMenu.ApplyOutfit notifies schedule engine with snapshot from current player
        public void OutfitMenu_ApplyOutfit_NotifiesScheduleEngine()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void ApplyOutfit");
            Assert.Contains("ManualOutfitSnapshot.FromCurrentPlayer()", body);
            Assert.Contains("SetManualOutfit", body);
        }

        [Fact]
        // Expected: OutfitMenu.NotifyOutfitApplied creates snapshot from set or current player
        public void OutfitMenu_NotifyOutfitApplied_CreatesSnapshotFromSetOrPlayer()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void NotifyOutfitApplied");
            Assert.Contains("ManualOutfitSnapshot.FromOutfitSet(set)", body);
            Assert.Contains("ManualOutfitSnapshot.FromCurrentPlayer()", body);
        }

        [Fact]
        // Expected: ScheduleEngine log populates ManualOverrideOutfitName with ScheduleManualItems fallback
        public void ScheduleEngine_LogEntry_FallsBackToScheduleManualItems()
        {
            string source = SourceScanner.ReadSourceFile("Services/ScheduleEngine.cs");
            Assert.Contains("ScheduleManualItems", source);
        }

        [Fact]
        // Expected: TranslationCache has ScheduleManualItems property
        public void TranslationCache_HasScheduleManualItems()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/TranslationCache.cs");
            Assert.Contains("ScheduleManualItems", source);
            Assert.Contains("schedule.manual-items", source);
        }

        [Fact]
        // Expected: default.json has schedule.manual-items key
        public void DefaultJson_HasScheduleManualItemsKey()
        {
            string json = SourceScanner.ReadSourceFile("i18n/default.json");
            Assert.Contains("\"schedule.manual-items\"", json);
        }

        // ----------------------------------------------------------------
        //  S9a: i18n rule→schedule rename (no stale "rule" keys)
        // ----------------------------------------------------------------

        [Theory]
        [InlineData("schedule.debug.schedules")]
        [InlineData("schedule.no-schedules")]
        [InlineData("schedule.schedules-header")]
        [InlineData("schedule.edit.delete-schedule-confirm")]
        // Expected: Renamed schedule i18n keys exist in default.json
        public void DefaultJson_HasRenamedScheduleKeys(string key)
        {
            string json = SourceScanner.ReadSourceFile("i18n/default.json");
            Assert.Contains($"\"{key}\"", json);
        }

        [Theory]
        [InlineData("schedule.debug.rules")]
        [InlineData("schedule.no-rules")]
        [InlineData("schedule.rules-header")]
        [InlineData("schedule.edit.delete-rule-confirm")]
        // Expected: Old "rule" i18n keys no longer exist in default.json
        public void DefaultJson_NoStaleRuleKeys(string key)
        {
            string json = SourceScanner.ReadSourceFile("i18n/default.json");
            Assert.DoesNotContain($"\"{key}\"", json);
        }

        [Theory]
        [InlineData("ScheduleDebugSchedules")]
        [InlineData("ScheduleNoSchedules")]
        [InlineData("ScheduleEditDeleteScheduleConfirm")]
        // Expected: TranslationCache has renamed schedule properties
        public void TranslationCache_HasRenamedScheduleProperties(string propertyName)
        {
            string source = SourceScanner.ReadSourceFile("Utilities/TranslationCache.cs");
            Assert.Contains(propertyName, source);
        }

        [Theory]
        [InlineData("ScheduleDebugRules")]
        [InlineData("ScheduleNoRules")]
        [InlineData("ScheduleEditDeleteRuleConfirm")]
        // Expected: Old "Rule" TranslationCache properties no longer exist
        public void TranslationCache_NoStaleRuleProperties(string propertyName)
        {
            string source = SourceScanner.ReadSourceFile("Utilities/TranslationCache.cs");
            Assert.DoesNotContain(propertyName, source);
        }

        // ----------------------------------------------------------------
        //  S9b: Save button moved to right panel bottom (next to Apply)
        // ----------------------------------------------------------------

        [Fact]
        // Expected: DrawBottomButtons draws SaveButton (moved from left panel)
        public void OutfitUIBuilder_DrawBottomButtons_IncludesSaveButton()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawBottomButtons");
            Assert.Contains("SaveButton", body);
            Assert.Contains("ButtonNewOutfit", body);
        }

        [Fact]
        // Expected: DrawLeftPanelButtons no longer draws SaveButton
        public void OutfitUIBuilder_DrawLeftPanelButtons_NoSaveButton()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawLeftPanelButtons");
            Assert.DoesNotContain("SaveButton", body);
            Assert.DoesNotContain("ButtonNewOutfit", body);
        }

        // ----------------------------------------------------------------
        //  S9c: Apply button closes menu
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ApplyButton click handler calls exitActiveMenu to close the menu
        public void OutfitInputHandler_ApplyButton_ClosesMenu()
        {
            string source = SourceScanner.ReadSourceFile("Input/OutfitInputHandler.cs");
            Assert.Contains("exitActiveMenu", source);
        }

        // ----------------------------------------------------------------
        //  S9d: Main menu panel height calculations
        // ----------------------------------------------------------------

        [Fact]
        // Expected: CalculateLeftPanelHeight uses actual left panel components (preview, arrows, buttons), not right panel height
        public void OutfitUIBuilder_LeftPanelHeight_UsesActualComponents()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "int CalculateLeftPanelHeight");
            Assert.Contains("CharacterPreviewHeight", body);
            Assert.Contains("ArrowNativeHeight", body);
            Assert.Contains("GapBetweenPortraitAndButtons", body);
            Assert.Contains("TabAndButtonHeight", body);
            Assert.DoesNotContain("CalculateRightPanelHeight", body);
        }

        [Fact]
        // Expected: CalculateRightPanelHeight excludes bottom buttons (grid section only)
        public void OutfitUIBuilder_RightPanelHeight_ExcludesBottomButtons()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "int CalculateRightPanelHeight");
            Assert.DoesNotContain("GridToButtonGap", body);
        }

        [Fact]
        // Expected: Bottom buttons section height is separated from right panel height
        public void OutfitUIBuilder_BottomButtonsSectionHeight_Exists()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "int CalculateBottomButtonsSectionHeight");
            Assert.Contains("GridToButtonGap", body);
            Assert.Contains("TabAndButtonHeight", body);
        }

        [Fact]
        // Expected: Content section height = max(left, right + bottom)
        public void OutfitUIBuilder_ContentSectionHeight_UsesMaxOfLeftAndRightPlusBottom()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "int CalculateContentSectionHeight");
            Assert.Contains("CalculateLeftPanelHeight()", body);
            Assert.Contains("CalculateRightPanelHeight()", body);
            Assert.Contains("CalculateBottomButtonsSectionHeight()", body);
            Assert.Contains("Math.Max", body);
        }

        [Fact]
        // Expected: Right panel positioning uses combined right + bottom height for centering
        public void OutfitUIBuilder_PositionContentSection_CentersRightWithBottomButtons()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void PositionContentSection");
            Assert.Contains("CalculateBottomButtonsSectionHeight()", body);
        }
        // ── ScheduleEdit layout v2 tests ──────────────────────────────

        [Fact]
        // Expected: ScheduleEditUIBuilder places Season, Weather, Area on one row (3 columns)
        public void ScheduleEditUIBuilder_ConditionsRow1_ThreeColumns()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void CalculateLayout");
            Assert.Contains("col3Width", body);
            Assert.Contains("col2X", body);
            Assert.Contains("col3X", body);
            Assert.Contains("SeasonsDropdownBar", body);
            Assert.Contains("WeatherDropdownBar", body);
            Assert.Contains("AreasDropdownBar", body);
        }

        [Fact]
        // Expected: Location and Festival bars use full trigger width
        public void ScheduleEditUIBuilder_LocationAndFestival_FullWidth()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void CalculateLayout");
            Assert.Contains("triggerAvail", body);
            Assert.Contains("LocationsDropdownBar", body);
            Assert.Contains("FestivalsDropdownBar", body);
        }

        [Fact]
        // Expected: Priority and Rotation are on separate rows under Behavior section
        public void ScheduleEditUIBuilder_PriorityAndRotation_SeparateRows()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void CalculateLayout");
            Assert.Contains("priorityRowY = currentY", body);
            Assert.Contains("rotationRowY = currentY", body);
            Assert.Contains("behaviorHeaderY = currentY", body);
        }

        [Fact]
        // Expected: CalculateBehaviorLayout uses PriorityClickArea and RotationClickArea
        public void ScheduleEditUIBuilder_BehaviorLayout_UsesClickAreas()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void CalculateBehaviorLayout");
            Assert.Contains("PriorityClickArea", body);
            Assert.Contains("RotationClickArea", body);
        }

        [Fact]
        // Expected: Recalculate uses separate top/bottom padding constants
        public void ScheduleEditUIBuilder_Recalculate_UsesSeparatePadding()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void Recalculate");
            Assert.Contains("ScheduleEditTopPadding", body);
            Assert.Contains("ScheduleEditBottomPadding", body);
            Assert.Contains("ScheduleEditSectionGap", body);
        }

        [Fact]
        // Expected: Condition dropdown bars are drawn without opacity parameter
        public void ScheduleEditOverlay_ConditionBars_NoOpacity()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            string drawBody = SourceScanner.ExtractMethodBody(source, "override void draw");
            Assert.DoesNotContain("conditionOpacity", drawBody);
        }

        [Fact]
        // Expected: ScheduleOutfitOverlay includes invalid sets in the list (no IsValid filter)
        public void ScheduleOutfitOverlay_RefreshDisplayedSets_IncludesInvalidSets()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleOutfitOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void RefreshDisplayedSets");
            Assert.DoesNotContain("s.IsValid", body);
        }

        [Fact]
        // Expected: ScheduleOutfitUIBuilder.DrawOutfitSetList checks IsValid and draws warning icon
        public void ScheduleOutfitUIBuilder_DrawOutfitSetList_ShowsInvalidIcon()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleOutfitUIBuilder.cs");
            Assert.Contains("IsValid", source);
            Assert.Contains("warningSourceRect", source);
        }

        [Fact]
        // Expected: Wedding checkbox invalidates pool cache
        public void ScheduleEditOverlay_WeddingCheckbox_InvalidatesPoolCache()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void receiveLeftClick");
            int weddingIdx = body.IndexOf("IsInWeddingRow");
            string afterWedding = body.Substring(weddingIdx, Math.Min(200, body.Length - weddingIdx));
            Assert.Contains("InvalidatePoolCache", afterWedding);
        }

        [Fact]
        // Expected: Wedding checkbox is on same row as Festival, vertically centered with ScheduleCheckboxSize
        public void ScheduleEditUIBuilder_WeddingRow_UsesCheckboxSize()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string layout = SourceScanner.ExtractMethodBody(source, "void CalculateLayout");
            int weddingCheckboxIdx = layout.IndexOf("WeddingCheckbox = new");
            Assert.True(weddingCheckboxIdx > 0, "WeddingCheckbox should be created in CalculateLayout");
            string weddingSection = layout.Substring(weddingCheckboxIdx, Math.Min(300, layout.Length - weddingCheckboxIdx));
            Assert.Contains("ScheduleCheckboxSize", weddingSection);
            Assert.Contains("FestivalsDropdownBar", layout.Substring(0, weddingCheckboxIdx));
        }

        [Fact]
        // Expected: DrawWeddingRow hit area uses ScheduleCheckboxSize, not TabAndButtonHeight
        public void ScheduleEditUIBuilder_DrawWeddingRow_HitAreaUsesCheckboxSize()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawWeddingRow");
            Assert.Contains("ScheduleCheckboxSize", body);
            Assert.DoesNotContain("TabAndButtonHeight", body);
        }

        [Fact]
        // Expected: IsInWeddingRow uses checkbox bounds directly, not TabAndButtonHeight offset
        public void ScheduleEditOverlay_IsInWeddingRow_UsesCheckboxBounds()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "bool IsInWeddingRow");
            Assert.Contains("ScheduleCheckboxSize", body);
            Assert.DoesNotContain("TabAndButtonHeight", body);
        }

        [Fact]
        // Expected: Total outfits row is always drawn (shows remaining only in edit mode)
        public void ScheduleEditOverlay_Draw_AlwaysDrawsTotalOutfitsRow()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void draw");
            Assert.Contains("DrawTotalOutfitsRow", body);
            Assert.Contains("selectedSetIds.Count", body);
        }

        [Fact]
        // Expected: Recalculate always includes total outfits row height
        public void ScheduleEditUIBuilder_Recalculate_AlwaysIncludesTotalOutfitsRow()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void Recalculate");
            Assert.Contains("ScheduleEditInfoRowGap", body);
        }

        [Fact]
        // Expected: CalculateLayout always positions total outfits row
        public void ScheduleEditUIBuilder_CalculateLayout_AlwaysPositionsTotalOutfitsRow()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void CalculateLayout");
            Assert.Contains("totalOutfitsRowY", body);
        }

        [Fact]
        // Expected: Recalculate uses section-specific header gaps for Special Events and Behavior
        public void ScheduleEditUIBuilder_Recalculate_UsesSectionSpecificHeaderGaps()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void Recalculate");
            Assert.Contains("ScheduleEditSpecialEventsHeaderGap", body);
            Assert.Contains("ScheduleEditBehaviorHeaderGap", body);
        }

        [Fact]
        // Expected: CalculateLayout uses section-specific header gaps for Special Events and Behavior
        public void ScheduleEditUIBuilder_CalculateLayout_UsesSectionSpecificHeaderGaps()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void CalculateLayout");
            Assert.Contains("ScheduleEditSpecialEventsHeaderGap", body);
            Assert.Contains("ScheduleEditBehaviorHeaderGap", body);
        }

        [Fact]
        // Expected: ScheduleEditUIBuilder sets IsEditing from overlay
        public void ScheduleEditOverlay_Constructor_SetsIsEditingOnUIBuilder()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            Assert.Contains("IsEditing = IsEditing", source);
        }

        [Fact]
        // Expected: Condition bar placeholders use plain label without "(Any)" suffix
        public void ScheduleEditOverlay_Placeholders_NoAnySuffix()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void draw");
            Assert.DoesNotContain("ScheduleEditAny", body);
        }

        [Fact]
        // Expected: ScheduleEditAny is removed from TranslationCache (dead code)
        public void TranslationCache_NoScheduleEditAny()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/TranslationCache.cs");
            Assert.DoesNotContain("ScheduleEditAny", source);
        }

        [Fact]
        // Expected: Condition bars pass opacity when in special event mode
        public void ScheduleEditOverlay_Draw_ConditionBarsPassOpacity()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void draw");
            Assert.Contains("conditionsOpacity", body);
            Assert.Contains("ScheduleEditInactiveOpacity", body);
            Assert.Contains("IsSpecialEventMode", body);
        }

        [Fact]
        // Expected: DrawConditionsHeader accepts opacity parameter
        public void ScheduleEditUIBuilder_DrawConditionsHeader_AcceptsOpacity()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            Assert.Contains("DrawConditionsHeader(SpriteBatch b, float opacity", source);
        }

        [Fact]
        // Expected: DrawPriorityRow accepts isSpecial parameter and suppresses hover when special
        public void ScheduleEditUIBuilder_DrawPriorityRow_AcceptsIsSpecial()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawPriorityRow");
            Assert.Contains("isSpecial", body);
            Assert.Contains("!isSpecial", body);
        }

        [Fact]
        // Expected: Priority click is blocked when IsSpecialEventMode
        public void ScheduleEditOverlay_PriorityClick_BlockedInSpecialEventMode()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "bool HandleDropdownBarClick");
            int priorityIdx = body.IndexOf("PriorityClickArea");
            string before = body.Substring(Math.Max(0, priorityIdx - 80), 80);
            Assert.Contains("!IsSpecialEventMode", before);
        }

        [Fact]
        // Expected: Priority row shows "Special" text when IsSpecialEventMode
        public void ScheduleEditOverlay_Draw_PriorityRowPassesIsSpecial()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void draw");
            Assert.Contains("ScheduleEditPrioritySpecial", body);
            Assert.Contains("DrawPriorityRow(b, priorityText, priorityDropdownOpen, isSpecial: IsSpecialEventMode)", body);
        }

        [Fact]
        // Expected: i18n uses "Daily" not "Once a day" for rotate option
        public void I18n_RotateOnceADay_SaysDaily()
        {
            string source = SourceScanner.ReadSourceFile("i18n/default.json");
            Assert.Contains("\"schedule.edit.rotate.once-a-day\": \"Daily\"", source);
        }

        // ── Schedule Edit: Name row & Festival+Wedding layout ──────────

        [Fact]
        // Expected: ScheduleEditUIBuilder has DrawNameRow method using bold text
        public void ScheduleEditUIBuilder_DrawNameRow_UsesBoldText()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "string? DrawNameRow");
            Assert.Contains("ScheduleEditNameLabel", body);
            Assert.Contains("new Vector2(1, 0)", body);
        }

        [Fact]
        // Expected: ScheduleEditUIBuilder.DrawTotalOutfitsRow uses bold text
        public void ScheduleEditUIBuilder_DrawTotalOutfitsRow_UsesBoldText()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawTotalOutfitsRow");
            Assert.Contains("new Vector2(1, 0)", body);
        }

        [Fact]
        // Expected: ScheduleEditOverlay.draw calls DrawNameRow with GenerateRuleName
        public void ScheduleEditOverlay_Draw_CallsDrawNameRow()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void draw");
            Assert.Contains("DrawNameRow", body);
            Assert.Contains("GenerateRuleName()", body);
        }

        [Fact]
        // Expected: Recalculate includes Name row height (textRowHeight after TotalOutfitsRow)
        public void ScheduleEditUIBuilder_Recalculate_IncludesNameRow()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void Recalculate");
            Assert.Contains("nameRowY", SourceScanner.ExtractMethodBody(source, "void CalculateLayout"));
        }

        [Fact]
        // Expected: Festival and Wedding share one row (ScheduleEditFestivalToWeddingGap between them)
        public void ScheduleEditUIBuilder_FestivalAndWedding_ShareRow()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void CalculateLayout");
            Assert.Contains("ScheduleEditFestivalToWeddingGap", body);
            int festivalIdx = body.IndexOf("FestivalsDropdownBar = new");
            int weddingIdx = body.IndexOf("WeddingCheckbox = new");
            Assert.True(festivalIdx > 0 && weddingIdx > festivalIdx,
                "Festival bar should be created before Wedding checkbox in the same block");
            int nextCurrentY = body.IndexOf("currentY +=", weddingIdx);
            Assert.Contains("TabAndButtonHeight", body.Substring(nextCurrentY, 60));
        }

        [Fact]
        // Expected: i18n has schedule.edit.name-label key
        public void I18n_HasScheduleEditNameLabel()
        {
            string source = SourceScanner.ReadSourceFile("i18n/default.json");
            Assert.Contains("schedule.edit.name-label", source);
        }

        [Fact]
        // Expected: TranslationCache has ScheduleEditNameLabel property
        public void TranslationCache_HasScheduleEditNameLabel()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/TranslationCache.cs");
            Assert.Contains("ScheduleEditNameLabel", source);
            Assert.Contains("schedule.edit.name-label", source);
        }

        // ── Schedule Outfit: layout tweaks ──────────

        [Fact]
        // Expected: ScheduleOutfitUIBuilder uses FilterBarIndent for header, filter bar, checkboxes, divider
        public void ScheduleOutfitUIBuilder_FilterBarIndent_AppliedToAll()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleOutfitUIBuilder.cs");
            string layout = SourceScanner.ExtractMethodBody(source, "void CalculateLayout");
            Assert.Contains("FilterBarIndent", layout);
            string header = SourceScanner.ExtractMethodBody(source, "void DrawHeader");
            Assert.Contains("FilterBarIndent", header);
            string divider = SourceScanner.ExtractMethodBody(source, "void DrawCheckboxDivider");
            Assert.Contains("FilterBarIndent", divider);
        }

        [Fact]
        // Expected: ScheduleOutfitUIBuilder uses DividerToContentGap after divider (not SectionGap)
        public void ScheduleOutfitUIBuilder_DividerToContentGap_UsedAfterDivider()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleOutfitUIBuilder.cs");
            string layout = SourceScanner.ExtractMethodBody(source, "void CalculateLayout");
            Assert.Contains("DividerToContentGap", layout);
            int dividerIdx = layout.IndexOf("checkboxDividerY = currentY");
            string afterDivider = layout.Substring(dividerIdx, Math.Min(120, layout.Length - dividerIdx));
            Assert.DoesNotContain("+ SectionGap", afterDivider);
        }
        // ----------------------------------------------------------------
        //  Toggle Wardrobe/Schedule Key configs
        // ----------------------------------------------------------------

        [Theory]
        [InlineData("ToggleWardrobeKey")]
        [InlineData("ToggleScheduleKey")]
        // Expected: ConfigOverlay has state fields for each new keybind
        public void ConfigOverlay_HasKeybindStateField(string fieldName)
        {
            string source = SourceScanner.ReadSourceFile("UI/ConfigOverlay.cs");
            Assert.Contains($"KeybindList {char.ToLower(fieldName[0])}{fieldName.Substring(1)}", source);
        }

        [Theory]
        [InlineData("ToggleWardrobeKey")]
        [InlineData("ToggleScheduleKey")]
        // Expected: ConfigOverlay receiveKeyPress assigns to new keybind fields
        public void ConfigOverlay_ReceiveKeyPress_AssignsNewKeybindField(string fieldName)
        {
            string source = SourceScanner.ReadSourceFile("UI/ConfigOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void receiveKeyPress");
            Assert.Contains($"\"{fieldName}\"", body);
        }

        [Theory]
        [InlineData("ToggleWardrobeKey")]
        [InlineData("ToggleScheduleKey")]
        // Expected: ConfigOverlay save handler writes new keybind fields to config
        public void ConfigOverlay_Save_WritesNewKeybindToConfig(string fieldName)
        {
            string source = SourceScanner.ReadSourceFile("UI/ConfigOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void HandleSave");
            Assert.Contains(fieldName, body);
        }

        [Theory]
        [InlineData("ToggleWardrobeKeyArea")]
        [InlineData("ToggleScheduleKeyArea")]
        // Expected: ConfigUIBuilder has ClickableComponent properties for the new keybind areas
        public void ConfigUIBuilder_HasKeybindArea(string propertyName)
        {
            string source = SourceScanner.ReadSourceFile("UI/ConfigUIBuilder.cs");
            Assert.Contains($"ClickableComponent {propertyName}", source);
        }

        [Theory]
        [InlineData("ToggleWardrobeKey")]
        [InlineData("ToggleScheduleKey")]
        // Expected: ConfigOverlay click handler starts listening for new keybinds
        public void ConfigOverlay_Click_StartsListeningForNewKeybind(string fieldName)
        {
            string source = SourceScanner.ReadSourceFile("UI/ConfigOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void receiveLeftClick");
            Assert.Contains($"listeningForKeybind = \"{fieldName}\"", body);
        }

        [Theory]
        [InlineData("ToggleWardrobeKey")]
        [InlineData("ToggleScheduleKey")]
        // Expected: ConfigOverlay cancel-listening check includes new keybind areas
        public void ConfigOverlay_CancelListening_IncludesNewKeybindArea(string fieldName)
        {
            string source = SourceScanner.ReadSourceFile("UI/ConfigOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void receiveLeftClick");
            Assert.Contains($"\"{fieldName}\"", body);
            Assert.Contains($"{fieldName}Area", body);
        }

        [Theory]
        [InlineData("UI/WardrobeOverlay.cs")]
        [InlineData("UI/ScheduleMenu.cs")]
        // Expected: Nullable parentMenu allows direct-open without parent OutfitMenu
        public void TypeBOverlay_ParentMenu_IsNullable(string sourceFile)
        {
            string source = SourceScanner.ReadSourceFile(sourceFile);
            Assert.Contains("IClickableMenu? parentMenu", source);
        }

        [Theory]
        [InlineData("UI/WardrobeOverlay.cs")]
        [InlineData("UI/ScheduleMenu.cs")]
        // Expected: CloseOverlay uses exitActiveMenu when parentMenu is null
        public void TypeBOverlay_CloseOverlay_HandlesNullParent(string sourceFile)
        {
            string source = SourceScanner.ReadSourceFile(sourceFile);
            string body = SourceScanner.ExtractMethodBody(source, "void CloseOverlay");
            Assert.Contains("parentMenu != null", body);
            Assert.Contains("Game1.exitActiveMenu()", body);
        }

        [Theory]
        [InlineData("ToggleWardrobeKey")]
        [InlineData("ToggleScheduleKey")]
        // Expected: ModEntry.OnButtonsChanged has toggle logic for each new keybind
        public void ModEntry_OnButtonsChanged_HasToggleLogic(string configField)
        {
            string source = SourceScanner.ReadSourceFile("Core/ModEntry.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void OnButtonsChanged");
            Assert.Contains($"{configField}.JustPressed()", body);
        }

        [Theory]
        [InlineData("ToggleWardrobeKey", "WardrobeOverlay")]
        [InlineData("ToggleScheduleKey", "ScheduleMenu")]
        // Expected: Toggle key closes when same menu type is already active
        public void ModEntry_ToggleKey_ClosesWhenSameMenuActive(string configField, string menuType)
        {
            string source = SourceScanner.ReadSourceFile("Core/ModEntry.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void OnButtonsChanged");
            int keyIndex = body.IndexOf($"{configField}.JustPressed()");
            string afterKey = body.Substring(keyIndex, Math.Min(500, body.Length - keyIndex));
            Assert.Contains($"is {menuType}", afterKey);
            Assert.Contains("exitActiveMenu()", afterKey);
        }
    }
}
