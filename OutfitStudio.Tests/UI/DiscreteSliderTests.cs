using Xunit;

namespace OutfitStudio.Tests.UI
{
    public class DiscreteSliderTests
    {
        private const int HandleWidth = 40; // 10 * 4f scale
        private const int BoundsX = 100;
        private const int BoundsWidth = 200;

        [Fact]
        // Expected: Clicking at the left edge of the track returns the minimum value
        public void CalculateValue_LeftEdge_ReturnsMin()
        {
            int clickX = BoundsX + HandleWidth / 2;
            int result = DiscreteSlider.CalculateValueFromClick(clickX, BoundsX, BoundsWidth, HandleWidth, 1, 10);
            Assert.Equal(1, result);
        }

        [Fact]
        // Expected: Clicking at the right edge of the track returns the maximum value
        public void CalculateValue_RightEdge_ReturnsMax()
        {
            int clickX = BoundsX + BoundsWidth - HandleWidth / 2;
            int result = DiscreteSlider.CalculateValueFromClick(clickX, BoundsX, BoundsWidth, HandleWidth, 1, 10);
            Assert.Equal(10, result);
        }

        [Fact]
        // Expected: Clicking at the center of the track returns the midpoint value
        public void CalculateValue_Center_ReturnsMidpoint()
        {
            int trackWidth = BoundsWidth - HandleWidth;
            int clickX = BoundsX + HandleWidth / 2 + trackWidth / 2;
            int result = DiscreteSlider.CalculateValueFromClick(clickX, BoundsX, BoundsWidth, HandleWidth, 0, 10);
            Assert.Equal(5, result);
        }

        [Fact]
        // Expected: Clicking before the track start clamps to minimum value
        public void CalculateValue_ClampsToMin()
        {
            int clickX = BoundsX;
            int result = DiscreteSlider.CalculateValueFromClick(clickX, BoundsX, BoundsWidth, HandleWidth, 1, 10);
            Assert.Equal(1, result);
        }

        [Fact]
        // Expected: Clicking after the track end clamps to maximum value
        public void CalculateValue_ClampsToMax()
        {
            int clickX = BoundsX + BoundsWidth;
            int result = DiscreteSlider.CalculateValueFromClick(clickX, BoundsX, BoundsWidth, HandleWidth, 1, 10);
            Assert.Equal(10, result);
        }

        [Fact]
        // Expected: Fractional positions round away from zero correctly
        public void CalculateValue_RoundsAwayFromZero()
        {
            // Place click at exactly 50% of a 1-to-3 range → 1 + 0.5*2 = 2.0 → rounds to 2
            int trackWidth = BoundsWidth - HandleWidth;
            int clickX = BoundsX + HandleWidth / 2 + trackWidth / 2;
            int result = DiscreteSlider.CalculateValueFromClick(clickX, BoundsX, BoundsWidth, HandleWidth, 1, 3);
            Assert.Equal(2, result);
        }

        [Theory]
        [InlineData(0, 100)]
        [InlineData(-10, 10)]
        [InlineData(5, 8)]
        // Expected: CalculateValueFromClick works correctly for various min/max ranges
        public void CalculateValue_CustomRange(int min, int max)
        {
            // Click at left edge → min
            int leftClick = BoundsX + HandleWidth / 2;
            Assert.Equal(min, DiscreteSlider.CalculateValueFromClick(leftClick, BoundsX, BoundsWidth, HandleWidth, min, max));

            // Click at right edge → max
            int rightClick = BoundsX + BoundsWidth - HandleWidth / 2;
            Assert.Equal(max, DiscreteSlider.CalculateValueFromClick(rightClick, BoundsX, BoundsWidth, HandleWidth, min, max));
        }

        [Fact]
        // Expected: CalculateValueFromClick returns min (== max) when range is degenerate (min equals max)
        public void CalculateValue_MinEqualsMax_ReturnsSameValue()
        {
            int clickX = BoundsX + BoundsWidth / 2;
            int result = DiscreteSlider.CalculateValueFromClick(clickX, BoundsX, BoundsWidth, HandleWidth, 5, 5);
            Assert.Equal(5, result);
        }
    }
}
