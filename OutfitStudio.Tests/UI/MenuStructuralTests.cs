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
        };

        // Type B overlays that swap Game1.activeClickableMenu and must forward resize to parent
        private static readonly string[] TypeBOverlayFiles = new[]
        {
            "UI/SaveSetOverlay.cs",
            "UI/ConfigOverlay.cs",
        };

        // ----------------------------------------------------------------
        //  S1: Every menu overrides gameWindowSizeChanged
        // ----------------------------------------------------------------

        [Theory]
        [InlineData("UI/OutfitMenu.cs")]
        [InlineData("UI/WardrobeOverlay.cs")]
        [InlineData("UI/SaveSetOverlay.cs")]
        [InlineData("UI/ConfigOverlay.cs")]
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
        // Expected: Every gameWindowSizeChanged body calls uiBuilder.Recalculate() to recompute layout
        public void GameWindowSizeChanged_CallsRecalculate(string sourceFile)
        {
            string source = SourceScanner.ReadSourceFile(sourceFile);
            bool found = SourceScanner.MethodContains(source,
                "override void gameWindowSizeChanged", "Recalculate()");
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
        //  S7: Truncated-text tooltips gated by ShowTooltip config
        // ----------------------------------------------------------------

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
    }
}
