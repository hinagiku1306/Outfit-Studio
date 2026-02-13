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
            "UI/SetPreviewOverlay.cs",
        };

        // Type B overlays that swap Game1.activeClickableMenu and must forward resize to parent
        private static readonly string[] TypeBOverlayFiles = new[]
        {
            "UI/SaveSetOverlay.cs",
            "UI/ConfigOverlay.cs",
            "UI/ScheduleMenu.cs",
            "UI/ScheduleEditOverlay.cs",
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
        [InlineData("UI/SetPreviewOverlay.cs")]
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
        [InlineData("UI/SetPreviewOverlay.cs")]
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
        //  S3: OutfitMenu forwards resize to Type A child overlay
        // ----------------------------------------------------------------

        [Fact]
        // Expected: OutfitMenu forwards gameWindowSizeChanged to wardrobeOverlay (Type A child)
        public void OutfitMenu_ForwardsResize_ToWardrobeOverlay()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitMenu.cs");
            bool found = SourceScanner.MethodContains(source,
                "override void gameWindowSizeChanged", "wardrobeOverlay?.gameWindowSizeChanged");
            Assert.True(found,
                "OutfitMenu.gameWindowSizeChanged must forward to wardrobeOverlay");
        }

        // ----------------------------------------------------------------
        //  S4: Type B overlays forward resize to parent
        // ----------------------------------------------------------------

        [Theory]
        [InlineData("UI/SaveSetOverlay.cs")]
        [InlineData("UI/ConfigOverlay.cs")]
        [InlineData("UI/ScheduleMenu.cs")]
        [InlineData("UI/ScheduleEditOverlay.cs")]
        // Expected: Type B overlays forward gameWindowSizeChanged to parentMenu so the parent stays in sync
        public void TypeBOverlay_ForwardsResize_ToParent(string sourceFile)
        {
            string source = SourceScanner.ReadSourceFile(sourceFile);
            bool found = SourceScanner.MethodContains(source,
                "override void gameWindowSizeChanged", "parentMenu.gameWindowSizeChanged");
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
        [InlineData("UI/SetPreviewOverlay.cs")]
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
        //  S6: OutfitMenu forwards all input methods to wardrobeOverlay
        // ----------------------------------------------------------------

        [Fact]
        // Expected: OutfitMenu.update forwards to wardrobeOverlay.update
        public void OutfitMenu_Update_ForwardsToWardrobeOverlay()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitMenu.cs");
            bool found = SourceScanner.MethodContains(source,
                "override void update", "wardrobeOverlay");
            Assert.True(found,
                "OutfitMenu.update must reference wardrobeOverlay for forwarding");
        }

        [Fact]
        // Expected: OutfitMenu.draw forwards to wardrobeOverlay.draw
        public void OutfitMenu_Draw_ForwardsToWardrobeOverlay()
        {
            string source = SourceScanner.ReadSourceFile("UI/OutfitMenu.cs");
            bool found = SourceScanner.MethodContains(source,
                "override void draw", "wardrobeOverlay");
            Assert.True(found,
                "OutfitMenu.draw must reference wardrobeOverlay for forwarding");
        }

        [Fact]
        // Expected: OutfitInputHandler forwards receiveLeftClick to wardrobeOverlay
        public void InputHandler_HandleLeftClick_ForwardsToWardrobeOverlay()
        {
            string source = SourceScanner.ReadSourceFile("Input/OutfitInputHandler.cs");
            Assert.Contains("wardrobeOverlay.receiveLeftClick", source);
        }

        [Fact]
        // Expected: OutfitInputHandler forwards receiveScrollWheelAction to wardrobeOverlay
        public void InputHandler_HandleScrollWheel_ForwardsToWardrobeOverlay()
        {
            string source = SourceScanner.ReadSourceFile("Input/OutfitInputHandler.cs");
            Assert.Contains("wardrobeOverlay.receiveScrollWheelAction", source);
        }

        [Fact]
        // Expected: OutfitInputHandler forwards receiveKeyPress to wardrobeOverlay
        public void InputHandler_HandleKeyPress_ForwardsToWardrobeOverlay()
        {
            string source = SourceScanner.ReadSourceFile("Input/OutfitInputHandler.cs");
            Assert.Contains("wardrobeOverlay.receiveKeyPress", source);
        }

        [Fact]
        // Expected: OutfitInputHandler checks readyToClose after forwarding click to wardrobeOverlay
        public void InputHandler_HandleLeftClick_ChecksReadyToClose()
        {
            string source = SourceScanner.ReadSourceFile("Input/OutfitInputHandler.cs");
            // After forwarding the click, the handler must check readyToClose for cleanup
            Assert.Contains("wardrobeOverlay.readyToClose()", source);
        }

        // ----------------------------------------------------------------
        //  S6b: ScheduleEditOverlay forwards input to SetPreviewOverlay (Type A child)
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
        //  S6c: ScheduleEditOverlay forwards input to tagPicker and setPicker
        // ----------------------------------------------------------------

        [Fact]
        // Expected: ScheduleEditOverlay forwards receiveLeftClick to tagPicker when open
        public void ScheduleEditOverlay_ForwardsLeftClick_ToTagPicker()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            Assert.Contains("tagPicker.HandleClick", source);
        }

        [Fact]
        // Expected: ScheduleEditOverlay forwards receiveLeftClick to setPicker when open
        public void ScheduleEditOverlay_ForwardsLeftClick_ToSetPicker()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            Assert.Contains("setPicker.HandleClick", source);
        }

        [Fact]
        // Expected: ScheduleEditOverlay forwards receiveKeyPress to tagPicker when open
        public void ScheduleEditOverlay_ForwardsKeyPress_ToTagPicker()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            Assert.Contains("tagPicker.HandleKeyPress", source);
        }

        [Fact]
        // Expected: ScheduleEditOverlay forwards receiveScrollWheelAction to tagPicker when open
        public void ScheduleEditOverlay_ForwardsScrollWheel_ToTagPicker()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            Assert.Contains("tagPicker.HandleScrollWheel", source);
        }

        [Fact]
        // Expected: ScheduleEditOverlay forwards receiveScrollWheelAction to setPicker when open
        public void ScheduleEditOverlay_ForwardsScrollWheel_ToSetPicker()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            Assert.Contains("setPicker.HandleScrollWheel", source);
        }

        [Fact]
        // Expected: ScheduleEditOverlay forwards gameWindowSizeChanged to tagPicker when open
        public void ScheduleEditOverlay_ForwardsResize_ToTagPicker()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            bool found = SourceScanner.MethodContains(source,
                "override void gameWindowSizeChanged", "tagPicker");
            Assert.True(found,
                "ScheduleEditOverlay.gameWindowSizeChanged must update tagPicker bounds");
        }

        [Fact]
        // Expected: ScheduleEditOverlay forwards gameWindowSizeChanged to setPicker when open
        public void ScheduleEditOverlay_ForwardsResize_ToSetPicker()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            bool found = SourceScanner.MethodContains(source,
                "override void gameWindowSizeChanged", "setPicker");
            Assert.True(found,
                "ScheduleEditOverlay.gameWindowSizeChanged must update setPicker bounds");
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
        //  S6e: SetPreviewOverlay has Save button wiring
        // ----------------------------------------------------------------

        [Fact]
        // Expected: SetPreviewOverlay has a save button that calls the onSave callback
        public void SetPreviewOverlay_HasSaveButton()
        {
            string source = SourceScanner.ReadSourceFile("UI/SetPreviewOverlay.cs");
            Assert.Contains("SaveButton", source);
            Assert.Contains("onSave", source);
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
        // Expected: ScheduleEditOverlay has a name TextBox with focus tracking
        public void ScheduleEditOverlay_HasNameTextBox()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            Assert.Contains("nameTextBox", source);
            Assert.Contains("nameBoxFocused", source);
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

        // ----------------------------------------------------------------
        //  S6j: ScheduleEditOverlay name TextBox width matches list display width
        // ----------------------------------------------------------------

        [Fact]
        // Expected: UpdateNameTextBoxBounds computes width from ScheduleMenu list constants + TextBox 21px padding
        public void UpdateNameTextBoxBounds_UsesListDisplayWidth()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleEditOverlay.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void UpdateNameTextBoxBounds");
            // Must derive width from the ScheduleMenu list width constants
            Assert.Contains("ScheduleMenuWidth", body);
            Assert.Contains("ScheduleBorderPadding", body);
            // Must compensate for TextBox internal 21px padding
            Assert.Contains("+ 21", body);
        }

        [Fact]
        // Expected: Name TextBox effective input width equals the list name column width
        public void NameTextBox_EffectiveWidth_MatchesListNameColumn()
        {
            // Verify the math: TextBox.Width - 21 must equal the name column in BuildRuleComponents
            int listContentWidth = OutfitLayoutConstants.ScheduleMenuWidth
                - OutfitLayoutConstants.ScheduleBorderPadding * 2
                - OutfitLayoutConstants.ScheduleScrollArrowRightPadding;
            int iconsOffset = OutfitLayoutConstants.ClearButtonSize + 10
                + OutfitLayoutConstants.ScheduleNameToInfoGap
                + OutfitLayoutConstants.ScheduleInfoButtonSize
                + OutfitLayoutConstants.ScheduleNameToInfoGap;
            int nameStartOffset = OutfitLayoutConstants.ScheduleNameIndent
                + OutfitLayoutConstants.ScheduleCheckboxSize + 12;

            int expectedNameColumnWidth = listContentWidth - iconsOffset - nameStartOffset;
            int textBoxWidth = expectedNameColumnWidth + 21;

            // TextBox limits input at Width - 21 pixels, so effective = textBoxWidth - 21
            Assert.Equal(expectedNameColumnWidth, textBoxWidth - 21);
            // Sanity: the column width must be positive and reasonable
            Assert.True(expectedNameColumnWidth > 200, $"Name column width {expectedNameColumnWidth}px is too narrow");
            Assert.True(expectedNameColumnWidth < 600, $"Name column width {expectedNameColumnWidth}px is suspiciously wide");
        }

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
            Assert.Contains("Recalculate(displayedRules.Count)", body);
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
        // Expected: ScheduleMenu.draw renders search placeholder when search is empty
        public void ScheduleMenu_Draw_ShowsSearchPlaceholderWhenEmpty()
        {
            string source = SourceScanner.ReadSourceFile("UI/ScheduleMenu.cs");
            string body = SourceScanner.ExtractMethodBody(source, "override void draw");
            Assert.Contains("DrawSearchPlaceholder", body);
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
        // Expected: TranslationCache has ScheduleSearchPlaceholder property
        public void TranslationCache_HasScheduleSearchPlaceholder()
        {
            string source = SourceScanner.ReadSourceFile("Utilities/TranslationCache.cs");
            Assert.Contains("ScheduleSearchPlaceholder", source);
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
        [InlineData("UI/SetPreviewOverlay.cs")]
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
        // Expected: SearchScope dropdown (WardrobeUIBuilder) highlights the currently selected scope
        public void WardrobeUIBuilder_DrawSearchScopeDropdown_HighlightsSelectedScope()
        {
            string source = SourceScanner.ReadSourceFile("UI/WardrobeUIBuilder.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawSearchScopeDropdown");
            Assert.Contains("isSelected", body);
            Assert.Contains("Color.Wheat * 0.6f", body);
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
    }
}
