using System.Collections.Generic;
using Xunit;

namespace OutfitStudio.Tests.UI
{
    public class ClampScrollOffsetTests
    {
        [Fact]
        // Expected: Offset within valid range is returned unchanged
        public void WithinRange_ReturnsUnchanged()
        {
            int result = UIHelpers.ClampScrollOffset(3, 10, 5);
            Assert.Equal(3, result);
        }

        [Fact]
        // Expected: Offset beyond max is clamped to max (totalItems - visibleItems)
        public void BeyondMax_ClampsToMax()
        {
            int result = UIHelpers.ClampScrollOffset(8, 10, 5);
            Assert.Equal(5, result);
        }

        [Fact]
        // Expected: Negative offset is clamped to 0
        public void Negative_ClampsToZero()
        {
            int result = UIHelpers.ClampScrollOffset(-3, 10, 5);
            Assert.Equal(0, result);
        }

        [Fact]
        // Expected: When totalItems equals visibleItems, max offset is 0
        public void ExactFit_MaxIsZero()
        {
            int result = UIHelpers.ClampScrollOffset(1, 5, 5);
            Assert.Equal(0, result);
        }

        [Fact]
        // Expected: When visibleItems exceeds totalItems, max offset is 0
        public void VisibleExceedsTotal_MaxIsZero()
        {
            int result = UIHelpers.ClampScrollOffset(3, 3, 10);
            Assert.Equal(0, result);
        }

        [Fact]
        // Expected: Empty list (0 items) always returns 0
        public void EmptyList_ReturnsZero()
        {
            int result = UIHelpers.ClampScrollOffset(5, 0, 10);
            Assert.Equal(0, result);
        }

        [Fact]
        // Expected: Zero offset on a non-empty list returns 0
        public void ZeroOffset_ReturnsZero()
        {
            int result = UIHelpers.ClampScrollOffset(0, 20, 5);
            Assert.Equal(0, result);
        }

        [Fact]
        // Expected: Offset at exactly the max value is valid
        public void AtExactMax_ReturnsMax()
        {
            int result = UIHelpers.ClampScrollOffset(5, 10, 5);
            Assert.Equal(5, result);
        }

        [Fact]
        // Expected: Single visible item with many total items clamps correctly
        public void SingleVisibleItem_ClampsCorrectly()
        {
            int result = UIHelpers.ClampScrollOffset(100, 50, 1);
            Assert.Equal(49, result);
        }
    }

    public class TruncateTextTests
    {
        // Simple measurement: each character = 10px wide
        private static float MeasureFixed(string text) => text.Length * 10f;

        [Fact]
        // Expected: Text that fits within maxWidth is returned unchanged
        public void FitsWithinWidth_ReturnsUnchanged()
        {
            string result = UIHelpers.TruncateText("Hello", 100, MeasureFixed);
            Assert.Equal("Hello", result);
        }

        [Fact]
        // Expected: Text that exactly fits is returned unchanged
        public void ExactFit_ReturnsUnchanged()
        {
            // "Hello" = 5 chars * 10 = 50px, maxWidth = 50
            string result = UIHelpers.TruncateText("Hello", 50, MeasureFixed);
            Assert.Equal("Hello", result);
        }

        [Fact]
        // Expected: Text that exceeds maxWidth is truncated with "..." suffix
        public void Exceeds_TruncatesWithEllipsis()
        {
            // "Hello World" = 11 chars * 10 = 110px, maxWidth = 80
            // Need "text..." to fit in 80px → text + "..." = 8 chars max → text = 5 chars
            string result = UIHelpers.TruncateText("Hello World", 80, MeasureFixed);
            Assert.EndsWith("...", result);
            Assert.True(MeasureFixed(result) <= 80, "Truncated text should fit within maxWidth");
        }

        [Fact]
        // Expected: Empty text returns empty (no crash)
        public void EmptyText_ReturnsEmpty()
        {
            string result = UIHelpers.TruncateText("", 100, MeasureFixed);
            Assert.Equal("", result);
        }

        [Fact]
        // Expected: Very narrow width still produces "..." at minimum
        public void VeryNarrowWidth_ProducesEllipsis()
        {
            string result = UIHelpers.TruncateText("Hello World", 30, MeasureFixed);
            Assert.EndsWith("...", result);
        }

        [Fact]
        // Expected: Single character text that fits returns unchanged
        public void SingleChar_Fits_ReturnsUnchanged()
        {
            string result = UIHelpers.TruncateText("A", 50, MeasureFixed);
            Assert.Equal("A", result);
        }
    }

    public class FormatTagsWithCountTests
    {
        // Simple measurement: each character = 10px wide
        private static float MeasureFixed(string text) => text.Length * 10f;
        private const string Prefix = "Tags: ";
        private const string NoneText = "None";

        [Fact]
        // Expected: Empty tags list returns prefix + noneText
        public void EmptyTags_ReturnsPrefixPlusNone()
        {
            var tags = new List<string>();
            string result = UIHelpers.FormatTagsWithCount(tags, Prefix, 500, MeasureFixed, NoneText);
            Assert.Equal("Tags: None", result);
        }

        [Fact]
        // Expected: Single tag that fits returns full text
        public void SingleTag_Fits_ReturnsFullText()
        {
            var tags = new List<string> { "Daily" };
            string result = UIHelpers.FormatTagsWithCount(tags, Prefix, 500, MeasureFixed, NoneText);
            Assert.Equal("Tags: Daily", result);
        }

        [Fact]
        // Expected: Multiple tags that all fit returns comma-separated full text
        public void MultipleTags_AllFit_ReturnsFullText()
        {
            var tags = new List<string> { "Daily", "Combat" };
            string result = UIHelpers.FormatTagsWithCount(tags, Prefix, 500, MeasureFixed, NoneText);
            Assert.Equal("Tags: Daily, Combat", result);
        }

        [Fact]
        // Expected: When all tags don't fit, trailing tags are replaced with (+N) count
        public void MultipleTags_Overflow_ShowsCount()
        {
            var tags = new List<string> { "Daily", "Combat", "Wedding", "Spring", "Summer" };
            // "Tags: Daily, Combat, Wedding, Spring, Summer" = 45 chars * 10 = 450px
            // Set maxWidth so only some tags fit
            // "Tags: Daily, Combat (+3)" = 24 chars * 10 = 240px
            string result = UIHelpers.FormatTagsWithCount(tags, Prefix, 250, MeasureFixed, NoneText);
            Assert.Contains("(+", result);
            Assert.StartsWith("Tags: ", result);
        }

        [Fact]
        // Expected: When no individual tags fit, returns prefix + (+N) for total count
        public void AllTagsOverflow_ShowsTotalCount()
        {
            var tags = new List<string> { "Daily", "Combat", "Wedding" };
            // Even "Tags: Daily (+2)" = 17 chars * 10 = 170px
            // Set maxWidth very small so nothing fits
            string result = UIHelpers.FormatTagsWithCount(tags, Prefix, 50, MeasureFixed, NoneText);
            Assert.Equal("Tags: (+3)", result);
        }

        [Fact]
        // Expected: Exactly at the boundary still returns full text
        public void ExactFit_ReturnsFullText()
        {
            var tags = new List<string> { "A", "B" };
            // "Tags: A, B" = 10 chars * 10 = 100px
            string result = UIHelpers.FormatTagsWithCount(tags, Prefix, 100, MeasureFixed, NoneText);
            Assert.Equal("Tags: A, B", result);
        }

        [Fact]
        // Expected: One tag over the boundary shows overflow count
        public void OnePastFit_ShowsOverflow()
        {
            var tags = new List<string> { "A", "B" };
            // "Tags: A, B" = 10 chars * 10 = 100px, maxWidth = 99 → doesn't fit
            // "Tags: A (+1)" = 12 chars * 10 = 120px → also doesn't fit at 99
            // Falls through to "(+2)" = "Tags: (+2)" = 10 chars * 10 = 100px → also doesn't fit
            string result = UIHelpers.FormatTagsWithCount(tags, Prefix, 99, MeasureFixed, NoneText);
            Assert.Contains("(+", result);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(7)]
        // Expected: Overflow count matches the number of hidden tags
        public void OverflowCount_MatchesHiddenTags(int totalTags)
        {
            var tags = new List<string>();
            for (int i = 0; i < totalTags; i++)
                tags.Add($"Tag{i}");

            // Very narrow width forces total overflow
            string result = UIHelpers.FormatTagsWithCount(tags, Prefix, 1, MeasureFixed, NoneText);
            Assert.Contains($"(+{totalTags})", result);
        }
    }

    public class ConfigDropdownPanelWidthTests
    {
        [Fact]
        // Expected: Width is maxOptionTextWidth + 32 (16px padding each side)
        public void Width_Is_TextWidthPlus32()
        {
            int result = ConfigUIBuilder.CalculateDropdownPanelWidth(100, 300);
            Assert.Equal(132, result);
        }

        [Fact]
        // Expected: Width is capped at maxWidth when text + padding exceeds it
        public void Width_CappedAtMaxWidth()
        {
            int result = ConfigUIBuilder.CalculateDropdownPanelWidth(180, 200);
            Assert.Equal(200, result);
        }

        [Fact]
        // Expected: When text + padding exactly equals maxWidth, returns maxWidth
        public void Width_ExactlyAtMax_ReturnsMax()
        {
            int result = ConfigUIBuilder.CalculateDropdownPanelWidth(168, 200);
            Assert.Equal(200, result);
        }

        [Fact]
        // Expected: Small text width still gets 32px padding
        public void Width_SmallText_StillPadded()
        {
            int result = ConfigUIBuilder.CalculateDropdownPanelWidth(20, 200);
            Assert.Equal(52, result);
        }
    }

    public class ConfigDropdownPanelCenteringTests
    {
        [Fact]
        // Expected: Panel center aligns with center of right-aligned value text
        public void PanelCenter_AlignsWith_ValueTextCenter()
        {
            int controlX = 500;
            int valueWidth = 60;
            int panelWidth = 100;
            int panelX = ConfigUIBuilder.CalculateCenteredPanelX(controlX, valueWidth, panelWidth);

            int panelCenter = panelX + panelWidth / 2;
            int textCenter = controlX - valueWidth / 2;
            Assert.Equal(textCenter, panelCenter);
        }

        [Fact]
        // Expected: Narrow value text shifts panel right (closer to controlX)
        public void NarrowValue_PanelShiftsRight()
        {
            int controlX = 500;
            int panelWidth = 100;
            int panelXNarrow = ConfigUIBuilder.CalculateCenteredPanelX(controlX, 30, panelWidth);
            int panelXWide = ConfigUIBuilder.CalculateCenteredPanelX(controlX, 120, panelWidth);
            Assert.True(panelXNarrow > panelXWide);
        }

        [Fact]
        // Expected: When value width equals panel width, panel right edge equals controlX
        public void ValueWidth_EqualsPanelWidth_RightEdgeAtControlX()
        {
            int controlX = 500;
            int width = 100;
            int panelX = ConfigUIBuilder.CalculateCenteredPanelX(controlX, width, width);
            Assert.Equal(controlX, panelX + width);
        }

        [Fact]
        // Expected: Panel center shifts with different value widths
        public void DifferentValues_DifferentPositions()
        {
            int controlX = 500;
            int panelWidth = 80;
            int panelX1 = ConfigUIBuilder.CalculateCenteredPanelX(controlX, 40, panelWidth);
            int panelX2 = ConfigUIBuilder.CalculateCenteredPanelX(controlX, 80, panelWidth);
            Assert.NotEqual(panelX1, panelX2);
        }
    }
}
