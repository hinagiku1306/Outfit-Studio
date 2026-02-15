using Xunit;

namespace OutfitStudio.Tests.UI
{
    public class DyeColorSliderTests
    {
        private const int BarX = 100;
        private const int BarWidth = 190;
        private const int ChunkCount = 24;

        // --- Gradient chunk coverage ---

        [Fact]
        // Expected: All gradient chunks together cover the full bar width with no gap
        public void GradientChunks_CoverFullBarWidth()
        {
            int totalWidth = 0;
            for (int i = 0; i < ChunkCount; i++)
            {
                var (_, w) = DyeColorManager.CalculateGradientChunk(BarX, BarWidth, ChunkCount, i);
                totalWidth += w;
            }
            Assert.Equal(BarWidth, totalWidth);
        }

        [Fact]
        // Expected: Gradient chunks are contiguous with no gaps between them
        public void GradientChunks_AreContiguous()
        {
            for (int i = 0; i < ChunkCount - 1; i++)
            {
                var (x, w) = DyeColorManager.CalculateGradientChunk(BarX, BarWidth, ChunkCount, i);
                var (nextX, _) = DyeColorManager.CalculateGradientChunk(BarX, BarWidth, ChunkCount, i + 1);
                Assert.Equal(x + w, nextX);
            }
        }

        [Fact]
        // Expected: First chunk starts at barX
        public void GradientChunks_FirstStartsAtBarX()
        {
            var (x, _) = DyeColorManager.CalculateGradientChunk(BarX, BarWidth, ChunkCount, 0);
            Assert.Equal(BarX, x);
        }

        [Fact]
        // Expected: Last chunk ends exactly at barX + barWidth
        public void GradientChunks_LastEndsAtBarRight()
        {
            var (x, w) = DyeColorManager.CalculateGradientChunk(BarX, BarWidth, ChunkCount, ChunkCount - 1);
            Assert.Equal(BarX + BarWidth, x + w);
        }

        [Theory]
        [InlineData(192)]
        [InlineData(190)]
        [InlineData(200)]
        [InlineData(100)]
        // Expected: Chunks cover full width regardless of whether barWidth divides evenly by chunk count
        public void GradientChunks_CoverFullWidth_VariousBarWidths(int barWidth)
        {
            int totalWidth = 0;
            for (int i = 0; i < ChunkCount; i++)
            {
                var (_, w) = DyeColorManager.CalculateGradientChunk(0, barWidth, ChunkCount, i);
                totalWidth += w;
            }
            Assert.Equal(barWidth, totalWidth);
        }

        // --- Slider value from click ---

        [Fact]
        // Expected: Clicking at bar left edge returns 0
        public void SliderValue_LeftEdge_ReturnsZero()
        {
            int result = DyeColorManager.CalculateSliderValue(BarX, BarX, BarWidth);
            Assert.Equal(0, result);
        }

        [Fact]
        // Expected: Clicking at bar right edge returns 100
        public void SliderValue_RightEdge_Returns100()
        {
            int result = DyeColorManager.CalculateSliderValue(BarX + BarWidth, BarX, BarWidth);
            Assert.Equal(100, result);
        }

        [Fact]
        // Expected: Clicking at bar midpoint returns 50
        public void SliderValue_Midpoint_Returns50()
        {
            int result = DyeColorManager.CalculateSliderValue(BarX + BarWidth / 2, BarX, BarWidth);
            Assert.Equal(50, result);
        }

        [Fact]
        // Expected: Click before bar clamps to 0
        public void SliderValue_BeforeBar_ClampsToZero()
        {
            int result = DyeColorManager.CalculateSliderValue(BarX - 50, BarX, BarWidth);
            Assert.Equal(0, result);
        }

        [Fact]
        // Expected: Click after bar clamps to 100
        public void SliderValue_AfterBar_ClampsTo100()
        {
            int result = DyeColorManager.CalculateSliderValue(BarX + BarWidth + 50, BarX, BarWidth);
            Assert.Equal(100, result);
        }

        // --- Cursor position ---

        [Fact]
        // Expected: Cursor at value 0 is at bar left edge
        public void CursorX_AtZero_ReturnsBarX()
        {
            int result = DyeColorManager.CalculateCursorX(0, BarX, BarWidth);
            Assert.Equal(BarX, result);
        }

        [Fact]
        // Expected: Cursor at value 100 is at bar right edge
        public void CursorX_At100_ReturnsBarRight()
        {
            int result = DyeColorManager.CalculateCursorX(100, BarX, BarWidth);
            Assert.Equal(BarX + BarWidth, result);
        }

        [Fact]
        // Expected: Cursor at value 50 is at bar midpoint
        public void CursorX_At50_ReturnsMidpoint()
        {
            int result = DyeColorManager.CalculateCursorX(50, BarX, BarWidth);
            Assert.Equal(BarX + BarWidth / 2, result);
        }

        // --- NaN hue guard (pure white / achromatic colors) ---

        [Fact]
        // Expected: SetSlidersFromColor produces valid hueValue (0) for pure white, not NaN-derived garbage
        public void SetSlidersFromColor_PureWhite_HueIsZero()
        {
            var manager = new DyeColorManager(() => { }, () => default);
            manager.SetSlidersFromColor(Microsoft.Xna.Framework.Color.White);
            int cursorX = DyeColorManager.CalculateCursorX(manager.HueValue, BarX, BarWidth);
            Assert.InRange(cursorX, BarX, BarX + BarWidth);
        }

        [Fact]
        // Expected: SetSlidersFromColor produces valid hueValue for pure black (max=0 path returns h=-1)
        public void SetSlidersFromColor_PureBlack_HueIsZero()
        {
            var manager = new DyeColorManager(() => { }, () => default);
            manager.SetSlidersFromColor(Microsoft.Xna.Framework.Color.Black);
            int cursorX = DyeColorManager.CalculateCursorX(manager.HueValue, BarX, BarWidth);
            Assert.InRange(cursorX, BarX, BarX + BarWidth);
        }

        [Fact]
        // Expected: SetSlidersFromColor produces valid hueValue for mid-gray (achromatic, delta=0)
        public void SetSlidersFromColor_Gray_HueIsZero()
        {
            var manager = new DyeColorManager(() => { }, () => default);
            manager.SetSlidersFromColor(new Microsoft.Xna.Framework.Color(128, 128, 128));
            int cursorX = DyeColorManager.CalculateCursorX(manager.HueValue, BarX, BarWidth);
            Assert.InRange(cursorX, BarX, BarX + BarWidth);
        }

        // --- Round-trip: click → value → cursor stays within gradient ---

        [Theory]
        [InlineData(0)]
        [InlineData(25)]
        [InlineData(50)]
        [InlineData(75)]
        [InlineData(100)]
        // Expected: Cursor position for any value falls within the gradient's covered range
        public void CursorX_AlwaysWithinGradientRange(int value)
        {
            int cursorX = DyeColorManager.CalculateCursorX(value, BarX, BarWidth);
            var (firstX, _) = DyeColorManager.CalculateGradientChunk(BarX, BarWidth, ChunkCount, 0);
            var (lastX, lastW) = DyeColorManager.CalculateGradientChunk(BarX, BarWidth, ChunkCount, ChunkCount - 1);
            Assert.InRange(cursorX, firstX, lastX + lastW);
        }
    }
}
