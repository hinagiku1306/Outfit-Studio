using Xunit;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio.Tests.Helpers
{
    public class TrimTextFromStartTests
    {
        private static float MeasureFixed(string text) => text.Length * 10f;

        [Fact]
        // Expected: empty string returns empty
        public void EmptyText_ReturnsEmpty()
        {
            string result = UIHelpers.TrimTextFromStart("", 100, MeasureFixed);
            Assert.Equal("", result);
        }

        [Fact]
        // Expected: null returns empty string
        public void NullText_ReturnsEmpty()
        {
            string result = UIHelpers.TrimTextFromStart(null!, 100, MeasureFixed);
            Assert.Equal("", result);
        }

        [Fact]
        // Expected: short text that fits returns unchanged
        public void FitsWithinWidth_ReturnsOriginal()
        {
            // "Hello" = 5 chars * 10 = 50px, maxWidth = 100
            string result = UIHelpers.TrimTextFromStart("Hello", 100, MeasureFixed);
            Assert.Equal("Hello", result);
        }

        [Fact]
        // Expected: long text trims from start so end is visible
        public void ExceedsWidth_TrimsFromStart()
        {
            // "Hello World!" = 12 chars * 10 = 120px, maxWidth = 60
            // Last 6 chars "orld!" = 50px fits, "World!" = 60px fits
            string result = UIHelpers.TrimTextFromStart("Hello World!", 60, MeasureFixed);
            Assert.Equal("World!", result);
        }

        [Fact]
        // Expected: single char always returned even if wider than maxWidth
        public void SingleChar_ReturnsIt()
        {
            string result = UIHelpers.TrimTextFromStart("X", 5, MeasureFixed);
            Assert.Equal("X", result);
        }

        [Fact]
        // Expected: text at exact boundary returns unchanged
        public void ExactFit_ReturnsOriginal()
        {
            // "Hello" = 5 chars * 10 = 50px, maxWidth = 50
            string result = UIHelpers.TrimTextFromStart("Hello", 50, MeasureFixed);
            Assert.Equal("Hello", result);
        }
    }

    public class CalculateInputBarMaxTextWidthTests
    {
        [Fact]
        // Expected: without clear button = boundsWidth - InputBarTextStartX - InputBarTextStartX (symmetric)
        public void NoClearButton_SymmetricPadding()
        {
            // 300 - 20 - 20 = 260
            int result = UIHelpers.CalculateInputBarMaxTextWidth(300, hasClearButton: false);
            Assert.Equal(260, result);
        }

        [Fact]
        // Expected: with clear button = boundsWidth - InputBarTextStartX - ClearButtonSize - ClearButtonRightMargin
        public void WithClearButton_ReservesButtonSpace()
        {
            // 300 - 20 - 24 - 12 = 244
            int result = UIHelpers.CalculateInputBarMaxTextWidth(300, hasClearButton: true);
            Assert.Equal(244, result);
        }

        [Fact]
        // Expected: narrow bounds with clear button can produce small/zero width
        public void NarrowBounds_SmallResult()
        {
            // 60 - 20 - 24 - 12 = 4
            int result = UIHelpers.CalculateInputBarMaxTextWidth(60, hasClearButton: true);
            Assert.Equal(4, result);
        }
    }

    public class CalculateButtonWidthWithMaxTests
    {
        private static float MeasureFixed(string text) => text.Length * 10f;

        [Fact]
        // Expected: short text returns TabAndButtonWidth (110) when no maxWidth
        public void ShortText_NoMaxWidth_ReturnsMinimum()
        {
            // "OK" = 2*10 = 20px + TextPadding*2 (15*2=30) = 50 → Max(110, 50) = 110
            int result = UIHelpers.CalculateButtonWidth("OK", int.MaxValue, MeasureFixed);
            Assert.Equal(110, result);
        }

        [Fact]
        // Expected: long text returns calculated width when no maxWidth
        public void LongText_NoMaxWidth_ReturnsCalculated()
        {
            // "Very Long Button Text" = 21*10 = 210 + 30 = 240 → Max(110, 240) = 240
            int result = UIHelpers.CalculateButtonWidth("Very Long Button Text", int.MaxValue, MeasureFixed);
            Assert.Equal(240, result);
        }

        [Fact]
        // Expected: long text capped at maxWidth
        public void LongText_WithMaxWidth_CapsResult()
        {
            // calculated = 240, maxWidth = 150 → Min(240, 150) = 150
            int result = UIHelpers.CalculateButtonWidth("Very Long Button Text", 150, MeasureFixed);
            Assert.Equal(150, result);
        }

        [Fact]
        // Expected: maxWidth smaller than TabAndButtonWidth caps to maxWidth (hard limit)
        public void MaxWidthBelowMinimum_ReturnsMaxWidth()
        {
            // short text: Max(110, 50) = 110, Min(110, 80) = 80
            int result = UIHelpers.CalculateButtonWidth("OK", 80, MeasureFixed);
            Assert.Equal(80, result);
        }
    }

    public class DropdownArrowPositionTests
    {
        private static readonly int ScaledArrowW = (int)(UIHelpers.UpScrollArrowSourceRect.Width * DropdownArrowScale);
        private static readonly int ScaledArrowH = (int)(UIHelpers.UpScrollArrowSourceRect.Height * DropdownArrowScale);

        [Fact]
        // Expected: arrowX = anchorRight - scaledWidth - pad
        public void ArrowX_UsesScaledWidthAndPad()
        {
            int anchorRight = 300;
            int result = UIHelpers.CalculateDropdownArrowX(anchorRight);
            Assert.Equal(anchorRight - ScaledArrowW - DropdownArrowPad, result);
        }

        [Fact]
        // Expected: different anchor produces correspondingly different X
        public void ArrowX_ShiftsWithAnchor()
        {
            int result1 = UIHelpers.CalculateDropdownArrowX(200);
            int result2 = UIHelpers.CalculateDropdownArrowX(400);
            Assert.Equal(200, result2 - result1);
        }

        [Fact]
        // Expected: upArrowY = anchorBottom + pad (no panel padding)
        public void UpArrowY_NoPanelPadding()
        {
            int anchorBottom = 250;
            int result = UIHelpers.CalculateDropdownUpArrowY(anchorBottom);
            Assert.Equal(anchorBottom + DropdownArrowPad, result);
        }

        [Fact]
        // Expected: upArrowY = anchorBottom + panelPaddingV + pad
        public void UpArrowY_WithPanelPadding()
        {
            int anchorBottom = 250;
            int panelPadding = 5;
            int result = UIHelpers.CalculateDropdownUpArrowY(anchorBottom, panelPadding);
            Assert.Equal(anchorBottom + panelPadding + DropdownArrowPad, result);
        }

        [Fact]
        // Expected: downArrowY = anchorBottom + dropdownHeight - scaledHeight - pad
        public void DownArrowY_NoPanelPadding()
        {
            int anchorBottom = 250;
            int dropdownHeight = 200;
            int result = UIHelpers.CalculateDropdownDownArrowY(anchorBottom, dropdownHeight);
            Assert.Equal(anchorBottom + dropdownHeight - ScaledArrowH - DropdownArrowPad, result);
        }

        [Fact]
        // Expected: downArrowY = anchorBottom + panelPaddingV + dropdownHeight - scaledHeight - pad
        public void DownArrowY_WithPanelPadding()
        {
            int anchorBottom = 250;
            int dropdownHeight = 200;
            int panelPadding = 5;
            int result = UIHelpers.CalculateDropdownDownArrowY(anchorBottom, dropdownHeight, panelPadding);
            Assert.Equal(anchorBottom + panelPadding + dropdownHeight - ScaledArrowH - DropdownArrowPad, result);
        }

        [Fact]
        // Expected: down arrow sits below up arrow (downY > upY for any positive dropdown height)
        public void DownArrow_BelowUpArrow()
        {
            int anchorBottom = 100;
            int dropdownHeight = 160;
            int upY = UIHelpers.CalculateDropdownUpArrowY(anchorBottom);
            int downY = UIHelpers.CalculateDropdownDownArrowY(anchorBottom, dropdownHeight);
            Assert.True(downY > upY);
        }
    }

    public class DropdownButtonMaxTextWidthTests
    {
        [Fact]
        // Expected: without clear button = boundsWidth - basePadLeft(20) - basePadRight(20)
        public void NoClearButton_UsesBasePadding()
        {
            int boundsWidth = 300;
            int result = UIHelpers.CalculateDropdownButtonMaxTextWidth(boundsWidth, hasClearButton: false);
            Assert.Equal(boundsWidth - 20 - 20, result);
        }

        [Fact]
        // Expected: with clear button = boundsWidth - ClearButtonSize - ClearButtonRightMargin - leftPad(20)
        public void WithClearButton_ReservesButtonSpace()
        {
            int boundsWidth = 300;
            int result = UIHelpers.CalculateDropdownButtonMaxTextWidth(boundsWidth, hasClearButton: true);
            Assert.Equal(boundsWidth - ClearButtonSize - ClearButtonRightMargin - 20, result);
        }

        [Fact]
        // Expected: clear button reserves more space than base padding
        public void ClearButton_ReservesMoreThanBase()
        {
            int boundsWidth = 300;
            int withClear = UIHelpers.CalculateDropdownButtonMaxTextWidth(boundsWidth, hasClearButton: true);
            int withoutClear = UIHelpers.CalculateDropdownButtonMaxTextWidth(boundsWidth, hasClearButton: false);
            Assert.True(withoutClear > withClear);
        }

        [Fact]
        // Expected: narrow bounds produces small/negative width
        public void NarrowBounds_SmallResult()
        {
            int result = UIHelpers.CalculateDropdownButtonMaxTextWidth(60, hasClearButton: true);
            Assert.Equal(60 - ClearButtonSize - ClearButtonRightMargin - 20, result);
        }
    }

    public class CalculateDeleteDialogLayoutTests
    {
        private static float MeasureFixed(string text) => text.Length * 10f;

        [Fact]
        // Expected: Dialog is horizontally centered in viewport
        public void DialogBounds_HorizontallyCentered()
        {
            var (bounds, _, _) = UIHelpers.CalculateDeleteDialogLayout(
                "Delete?", "Yes", "No", 800, 600, MeasureFixed, MeasureFixed);
            Assert.Equal((800 - bounds.Width) / 2, bounds.X);
        }

        [Fact]
        // Expected: Dialog is vertically centered in viewport
        public void DialogBounds_VerticallyCentered()
        {
            var (bounds, _, _) = UIHelpers.CalculateDeleteDialogLayout(
                "Delete?", "Yes", "No", 800, 600, MeasureFixed, MeasureFixed);
            Assert.Equal((600 - bounds.Height) / 2, bounds.Y);
        }

        [Fact]
        // Expected: Yes button is left of No button
        public void YesButton_LeftOfNoButton()
        {
            var (_, yes, no) = UIHelpers.CalculateDeleteDialogLayout(
                "Delete?", "Yes", "No", 800, 600, MeasureFixed, MeasureFixed);
            Assert.True(yes.X < no.X);
        }

        [Fact]
        // Expected: Both buttons are within dialog bounds
        public void Buttons_WithinDialogBounds()
        {
            var (bounds, yes, no) = UIHelpers.CalculateDeleteDialogLayout(
                "Delete?", "Yes", "No", 800, 600, MeasureFixed, MeasureFixed);
            Assert.True(yes.X >= bounds.X);
            Assert.True(no.Right <= bounds.Right);
        }

        [Fact]
        // Expected: Button height equals TabAndButtonHeight
        public void Buttons_HeightEqualsTabAndButtonHeight()
        {
            var (_, yes, no) = UIHelpers.CalculateDeleteDialogLayout(
                "Delete?", "Yes", "No", 800, 600, MeasureFixed, MeasureFixed);
            Assert.Equal(TabAndButtonHeight, yes.Height);
            Assert.Equal(TabAndButtonHeight, no.Height);
        }

        [Fact]
        // Expected: Longer question text widens the dialog
        public void LongQuestion_WidensDialog()
        {
            var (shortBounds, _, _) = UIHelpers.CalculateDeleteDialogLayout(
                "OK?", "Yes", "No", 800, 600, MeasureFixed, MeasureFixed);
            var (longBounds, _, _) = UIHelpers.CalculateDeleteDialogLayout(
                "Are you absolutely sure you want to delete this item forever?",
                "Yes", "No", 800, 600, MeasureFixed, MeasureFixed);
            Assert.True(longBounds.Width >= shortBounds.Width);
        }

        [Fact]
        // Expected: 20px spacing between Yes and No buttons
        public void Buttons_Have20pxSpacing()
        {
            var (_, yes, no) = UIHelpers.CalculateDeleteDialogLayout(
                "Delete?", "Yes", "No", 800, 600, MeasureFixed, MeasureFixed);
            Assert.Equal(20, no.X - yes.Right);
        }
    }
}
