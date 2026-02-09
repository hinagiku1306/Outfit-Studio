using Xunit;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio.Tests.Utilities
{
    public class ItemIdHelperTests
    {
        [Theory]
        [InlineData("(S)123", "123")]
        [InlineData("(P)abc", "abc")]
        [InlineData("(H)hat1", "hat1")]
        [InlineData("raw", "raw")]
        [InlineData("", "")]
        // Expected: GetUnqualifiedId strips the (S)/(P)/(H) prefix, or returns as-is if none
        public void GetUnqualifiedId_StripsPrefix(string input, string expected)
        {
            Assert.Equal(expected, ItemIdHelper.GetUnqualifiedId(input));
        }

        [Theory]
        [InlineData("(S)x", OutfitCategoryManager.Category.Shirts)]
        [InlineData("(P)x", OutfitCategoryManager.Category.Pants)]
        [InlineData("(H)x", OutfitCategoryManager.Category.Hats)]
        [InlineData("plain", OutfitCategoryManager.Category.All)]
        // Expected: GetCategoryFromQualifiedId returns the correct category based on prefix
        public void GetCategoryFromQualifiedId_ReturnsCorrectCategory(string input, OutfitCategoryManager.Category expected)
        {
            Assert.Equal(expected, ItemIdHelper.GetCategoryFromQualifiedId(input));
        }

        [Theory]
        [InlineData(OutfitCategoryManager.Category.Shirts, "123", "(S)123")]
        [InlineData(OutfitCategoryManager.Category.Pants, "456", "(P)456")]
        [InlineData(OutfitCategoryManager.Category.Hats, "hat1", "(H)hat1")]
        [InlineData(OutfitCategoryManager.Category.All, "raw", "raw")]
        // Expected: GetQualifiedId prepends the correct prefix for the category
        public void GetQualifiedId_PrependsCorrectPrefix(OutfitCategoryManager.Category category, string id, string expected)
        {
            Assert.Equal(expected, ItemIdHelper.GetQualifiedId(id, category));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        // Expected: GetQualifiedId returns null for null or empty input
        public void GetQualifiedId_NullOrEmpty_ReturnsNull(string? id)
        {
            Assert.Null(ItemIdHelper.GetQualifiedId(id, OutfitCategoryManager.Category.Shirts));
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("-1", true)]
        [InlineData("hat1", false)]
        // Expected: IsNoHatId returns true for null, empty, or "-1"
        public void IsNoHatId_CorrectResult(string? input, bool expected)
        {
            Assert.Equal(expected, ItemIdHelper.IsNoHatId(input));
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("-1", true)]
        [InlineData("shirt1", false)]
        // Expected: IsNoShirtId returns true for null, empty, or "-1"
        public void IsNoShirtId_CorrectResult(string? input, bool expected)
        {
            Assert.Equal(expected, ItemIdHelper.IsNoShirtId(input));
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("-1", true)]
        [InlineData("pants1", false)]
        // Expected: IsNoPantsId returns true for null, empty, or "-1"
        public void IsNoPantsId_CorrectResult(string? input, bool expected)
        {
            Assert.Equal(expected, ItemIdHelper.IsNoPantsId(input));
        }

        [Fact]
        // Expected: GetQualifiedItemId returns the qualified ID for a valid index in the shirts list
        public void GetQualifiedItemId_ValidIndex_ReturnsQualifiedId()
        {
            var shirts = new List<string> { "shirt0", "shirt1", "shirt2" };
            var pants = new List<string>();
            var hats = new List<string>();

            var result = ItemIdHelper.GetQualifiedItemId(
                OutfitCategoryManager.Category.Shirts, 2, shirts, pants, hats);
            Assert.Equal("(S)shirt2", result);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(5)]
        // Expected: GetQualifiedItemId returns null when index is out of bounds
        public void GetQualifiedItemId_OutOfBounds_ReturnsNull(int index)
        {
            var shirts = new List<string> { "shirt0", "shirt1" };
            var pants = new List<string>();
            var hats = new List<string>();

            Assert.Null(ItemIdHelper.GetQualifiedItemId(
                OutfitCategoryManager.Category.Shirts, index, shirts, pants, hats));
        }

        [Fact]
        // Expected: GetQualifiedItemId returns null for a hat entry that is the NoHatId sentinel
        public void GetQualifiedItemId_NoHatId_ReturnsNull()
        {
            var shirts = new List<string>();
            var pants = new List<string>();
            var hats = new List<string> { "-1" };

            Assert.Null(ItemIdHelper.GetQualifiedItemId(
                OutfitCategoryManager.Category.Hats, 0, shirts, pants, hats));
        }

        [Fact]
        // Expected: GetQualifiedItemId returns the qualified hat ID for a valid hat entry
        public void GetQualifiedItemId_ValidHat_ReturnsQualifiedId()
        {
            var shirts = new List<string>();
            var pants = new List<string>();
            var hats = new List<string> { "hat42" };

            Assert.Equal("(H)hat42", ItemIdHelper.GetQualifiedItemId(
                OutfitCategoryManager.Category.Hats, 0, shirts, pants, hats));
        }

        [Fact]
        // Expected: GetQualifiedItemId returns the qualified pants ID for a valid index
        public void GetQualifiedItemId_ValidPants_ReturnsQualifiedId()
        {
            var shirts = new List<string>();
            var pants = new List<string> { "pants0", "pants1" };
            var hats = new List<string>();

            Assert.Equal("(P)pants1", ItemIdHelper.GetQualifiedItemId(
                OutfitCategoryManager.Category.Pants, 1, shirts, pants, hats));
        }

        [Fact]
        // Expected: GetQualifiedItemId returns null for Category.All (no case in switch)
        public void GetQualifiedItemId_CategoryAll_ReturnsNull()
        {
            var shirts = new List<string> { "shirt0" };
            var pants = new List<string> { "pants0" };
            var hats = new List<string> { "hat0" };

            Assert.Null(ItemIdHelper.GetQualifiedItemId(
                OutfitCategoryManager.Category.All, 0, shirts, pants, hats));
        }

        [Fact]
        // Expected: GetQualifiedItemId returns null for a hat entry that is an empty string
        public void GetQualifiedItemId_EmptyHatId_ReturnsNull()
        {
            var shirts = new List<string>();
            var pants = new List<string>();
            var hats = new List<string> { "" };

            Assert.Null(ItemIdHelper.GetQualifiedItemId(
                OutfitCategoryManager.Category.Hats, 0, shirts, pants, hats));
        }
    }
}
