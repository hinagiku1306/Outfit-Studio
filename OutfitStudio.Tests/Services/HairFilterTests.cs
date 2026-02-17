using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace OutfitStudio.Tests.Services
{
    public class HairFilterTests
    {
        private static List<int> SearchFilter(List<int> hairIds, string? searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return hairIds;

            return hairIds.Where(id =>
                id.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        [Fact]
        // Expected: Null search text returns all hair IDs unchanged
        public void SearchFilter_NullText_ReturnsAll()
        {
            var ids = new List<int> { 0, 5, 10, 100 };
            var result = SearchFilter(ids, null);
            Assert.Equal(ids, result);
        }

        [Fact]
        // Expected: Empty search text returns all hair IDs unchanged
        public void SearchFilter_EmptyText_ReturnsAll()
        {
            var ids = new List<int> { 0, 5, 10 };
            var result = SearchFilter(ids, "");
            Assert.Equal(ids, result);
        }

        [Fact]
        // Expected: Search "10" matches ID 10, 100, 101 (contains "10")
        public void SearchFilter_PartialMatch_ReturnsContaining()
        {
            var ids = new List<int> { 1, 10, 100, 101, 200 };
            var result = SearchFilter(ids, "10");
            Assert.Equal(new List<int> { 10, 100, 101 }, result);
        }

        [Fact]
        // Expected: Search "999" with no matching IDs returns empty list
        public void SearchFilter_NoMatch_ReturnsEmpty()
        {
            var ids = new List<int> { 0, 1, 2, 3 };
            var result = SearchFilter(ids, "999");
            Assert.Empty(result);
        }

        [Fact]
        // Expected: Search "0" matches ID 0, 10, 20, 100 (all containing digit "0")
        public void SearchFilter_SingleDigit_MatchesAllContaining()
        {
            var ids = new List<int> { 0, 1, 10, 20, 100 };
            var result = SearchFilter(ids, "0");
            Assert.Equal(new List<int> { 0, 10, 20, 100 }, result);
        }

        [Fact]
        // Expected: Category enum includes Hair value
        public void Category_IncludesHair()
        {
            Assert.True(System.Enum.IsDefined(typeof(OutfitCategoryManager.Category), OutfitCategoryManager.Category.Hair));
        }

        [Fact]
        // Expected: Null rawData (texture-grid hair with no HairData entry) is not modded
        public void IsModdedHairEntry_NullData_NotModded()
        {
            Assert.False(FilterCacheService.IsModdedHairEntry(null));
        }

        [Fact]
        // Expected: "hairstyles" texture name is vanilla (not modded)
        public void IsModdedHairEntry_Hairstyles_NotModded()
        {
            Assert.False(FilterCacheService.IsModdedHairEntry("hairstyles/0/0"));
        }

        [Fact]
        // Expected: "hairstyles2" texture name is vanilla (not modded)
        public void IsModdedHairEntry_Hairstyles2_NotModded()
        {
            Assert.False(FilterCacheService.IsModdedHairEntry("hairstyles2/3/1"));
        }

        [Fact]
        // Expected: Vanilla texture name check is case-insensitive
        public void IsModdedHairEntry_CaseInsensitive_NotModded()
        {
            Assert.False(FilterCacheService.IsModdedHairEntry("Hairstyles/5/2"));
        }

        [Fact]
        // Expected: Custom texture name from a mod is classified as modded
        public void IsModdedHairEntry_CustomTexture_IsModded()
        {
            Assert.True(FilterCacheService.IsModdedHairEntry("MyMod_CustomHair/0/0"));
        }
    }
}
