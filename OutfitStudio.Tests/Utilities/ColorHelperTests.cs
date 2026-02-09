using Microsoft.Xna.Framework;
using Xunit;

namespace OutfitStudio.Tests.Utilities
{
    public class ColorHelperTests
    {
        [Fact]
        // Expected: ToColorString returns comma-separated R,G,B,A string
        public void ToColorString_ReturnsCommaSeparatedRGBA()
        {
            var color = new Color(255, 128, 0, 200);
            Assert.Equal("255,128,0,200", ColorHelper.ToColorString(color));
        }

        [Fact]
        // Expected: ParseColor returns Color with correct R,G,B,A for valid 4-part input
        public void ParseColor_ValidRGBA_ReturnsColor()
        {
            var result = ColorHelper.ParseColor("255,128,0,200");
            Assert.NotNull(result);
            Assert.Equal(255, result!.Value.R);
            Assert.Equal(128, result.Value.G);
            Assert.Equal(0, result.Value.B);
            Assert.Equal(200, result.Value.A);
        }

        [Fact]
        // Expected: ParseColor defaults alpha to 255 when only 3 parts are provided
        public void ParseColor_RGBOnly_DefaultsAlphaTo255()
        {
            var result = ColorHelper.ParseColor("255,128,0");
            Assert.NotNull(result);
            Assert.Equal(255, result!.Value.A);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        // Expected: ParseColor returns null for null or empty input
        public void ParseColor_NullOrEmpty_ReturnsNull(string? input)
        {
            Assert.Null(ColorHelper.ParseColor(input));
        }

        [Fact]
        // Expected: ParseColor returns null when input has fewer than 3 comma-separated parts
        public void ParseColor_TooFewParts_ReturnsNull()
        {
            Assert.Null(ColorHelper.ParseColor("255,128"));
        }

        [Fact]
        // Expected: ParseColor returns null when any part is not a valid number
        public void ParseColor_InvalidNumber_ReturnsNull()
        {
            Assert.Null(ColorHelper.ParseColor("abc,128,0"));
        }

        [Fact]
        // Expected: ToColorString then ParseColor round-trips to the original color
        public void RoundTrip_PreservesColor()
        {
            var original = new Color(42, 200, 100, 180);
            var str = ColorHelper.ToColorString(original);
            var parsed = ColorHelper.ParseColor(str);
            Assert.NotNull(parsed);
            Assert.Equal(original, parsed!.Value);
        }

        [Fact]
        // Expected: ParseColor with invalid alpha part defaults alpha to 0 (byte.TryParse out param default)
        public void ParseColor_InvalidAlpha_DefaultsToZero()
        {
            var result = ColorHelper.ParseColor("255,128,0,abc");
            Assert.NotNull(result);
            Assert.Equal(0, result!.Value.A);
        }

        [Fact]
        // Expected: ParseColor ignores extra parts beyond the 4th
        public void ParseColor_ExtraParts_StillParses()
        {
            var result = ColorHelper.ParseColor("255,128,0,200,extra");
            Assert.NotNull(result);
            Assert.Equal(255, result!.Value.R);
            Assert.Equal(200, result!.Value.A);
        }

        [Fact]
        // Expected: ParseColor returns null when a value is negative (byte cannot be negative)
        public void ParseColor_NegativeValue_ReturnsNull()
        {
            Assert.Null(ColorHelper.ParseColor("-1,128,0"));
        }
    }
}
