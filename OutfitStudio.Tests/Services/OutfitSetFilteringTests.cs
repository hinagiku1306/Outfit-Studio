using System;
using System.Collections.Generic;
using System.Linq;
using OutfitStudio.Managers;
using OutfitStudio.Models;
using OutfitStudio.Services;
using OutfitStudio.Tests.Helpers;
using Xunit;

namespace OutfitStudio.Tests.Services
{
    public class OutfitSetFilteringTests
    {
        // --- Tag filtering ---

        [Fact]
        // Expected: ApplyTagFilter returns all sets when no tags are selected
        public void ApplyTagFilter_NoTags_ReturnsAll()
        {
            var sets = new[] { TestData.CreateSet(name: "A"), TestData.CreateSet(name: "B") };
            var indexes = TestData.BuildIndexes(sets);

            var result = OutfitSetFiltering.ApplyTagFilter(
                sets, new HashSet<string>(TranslationCache.TagComparer), true,
                indexes.ByTag, TranslationCache.TagComparison).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        // Expected: ApplyTagFilter with matchAll=true returns only sets that have ALL selected tags
        public void ApplyTagFilter_MatchAll_RequiresAll()
        {
            var setAB = TestData.CreateSet(name: "AB", tags: new List<string> { "Spring", "Summer" });
            var setA = TestData.CreateSet(name: "A", tags: new List<string> { "Spring" });
            var sets = new[] { setAB, setA };
            var indexes = TestData.BuildIndexes(sets);

            var tags = new HashSet<string>(TranslationCache.TagComparer) { "Spring", "Summer" };
            var result = OutfitSetFiltering.ApplyTagFilter(
                sets, tags, true, indexes.ByTag, TranslationCache.TagComparison).ToList();

            Assert.Single(result);
            Assert.Equal("AB", result[0].Name);
        }

        [Fact]
        // Expected: ApplyTagFilter with matchAll=false returns sets that have ANY selected tag
        public void ApplyTagFilter_MatchAny_ReturnsUnion()
        {
            var setA = TestData.CreateSet(name: "A", tags: new List<string> { "Spring" });
            var setB = TestData.CreateSet(name: "B", tags: new List<string> { "Summer" });
            var setC = TestData.CreateSet(name: "C", tags: new List<string> { "Winter" });
            var sets = new[] { setA, setB, setC };
            var indexes = TestData.BuildIndexes(sets);

            var tags = new HashSet<string>(TranslationCache.TagComparer) { "Spring", "Summer" };
            var result = OutfitSetFiltering.ApplyTagFilter(
                sets, tags, false, indexes.ByTag, TranslationCache.TagComparison).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, s => s.Name == "A");
            Assert.Contains(result, s => s.Name == "B");
        }

        [Fact]
        // Expected: ApplyTagFilter is case-insensitive (lowercase tag matches title-cased tag on set)
        public void ApplyTagFilter_CaseInsensitive()
        {
            var set = TestData.CreateSet(name: "A", tags: new List<string> { "Spring" });
            var sets = new[] { set };
            var indexes = TestData.BuildIndexes(sets);

            var tags = new HashSet<string>(TranslationCache.TagComparer) { "spring" };
            var result = OutfitSetFiltering.ApplyTagFilter(
                sets, tags, true, indexes.ByTag, TranslationCache.TagComparison).ToList();

            Assert.Single(result);
        }

        [Fact]
        // Expected: ApplyTagFilter with matchAll=true excludes a set missing one of the required tags
        public void ApplyTagFilter_MatchAll_MissingOne_Excluded()
        {
            var set = TestData.CreateSet(name: "A", tags: new List<string> { "Spring" });
            var sets = new[] { set };
            var indexes = TestData.BuildIndexes(sets);

            var tags = new HashSet<string>(TranslationCache.TagComparer) { "Spring", "Summer" };
            var result = OutfitSetFiltering.ApplyTagFilter(
                sets, tags, true, indexes.ByTag, TranslationCache.TagComparison).ToList();

            Assert.Empty(result);
        }

        // --- Scope filtering ---

        [Fact]
        // Expected: ApplyScopeFilter returns all sets when both global and local are shown
        public void ApplyScopeFilter_Both_ReturnsAll()
        {
            var global = TestData.CreateSet(name: "G", isGlobal: true);
            var local = TestData.CreateSet(name: "L", isGlobal: false);
            var sets = new[] { global, local };
            var indexes = TestData.BuildIndexes(sets);

            var result = OutfitSetFiltering.ApplyScopeFilter(
                sets, true, true, indexes.GlobalIds, indexes.LocalIds).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        // Expected: ApplyScopeFilter returns empty when both scopes are disabled
        public void ApplyScopeFilter_NeitherScope_ReturnsEmpty()
        {
            var set = TestData.CreateSet();
            var sets = new[] { set };
            var indexes = TestData.BuildIndexes(sets);

            var result = OutfitSetFiltering.ApplyScopeFilter(
                sets, false, false, indexes.GlobalIds, indexes.LocalIds).ToList();

            Assert.Empty(result);
        }

        [Fact]
        // Expected: ApplyScopeFilter returns only global sets when showGlobal=true, showLocal=false
        public void ApplyScopeFilter_GlobalOnly()
        {
            var global = TestData.CreateSet(name: "G", isGlobal: true);
            var local = TestData.CreateSet(name: "L", isGlobal: false);
            var sets = new[] { global, local };
            var indexes = TestData.BuildIndexes(sets);

            var result = OutfitSetFiltering.ApplyScopeFilter(
                sets, true, false, indexes.GlobalIds, indexes.LocalIds).ToList();

            Assert.Single(result);
            Assert.Equal("G", result[0].Name);
        }

        [Fact]
        // Expected: ApplyScopeFilter returns only local sets when showGlobal=false, showLocal=true
        public void ApplyScopeFilter_LocalOnly()
        {
            var global = TestData.CreateSet(name: "G", isGlobal: true);
            var local = TestData.CreateSet(name: "L", isGlobal: false);
            var sets = new[] { global, local };
            var indexes = TestData.BuildIndexes(sets);

            var result = OutfitSetFiltering.ApplyScopeFilter(
                sets, false, true, indexes.GlobalIds, indexes.LocalIds).ToList();

            Assert.Single(result);
            Assert.Equal("L", result[0].Name);
        }

        // --- Favorite filtering ---

        [Fact]
        // Expected: ApplyFavoriteFilter returns all sets when favoritesOnly is false
        public void ApplyFavoriteFilter_NotActive_ReturnsAll()
        {
            var fav = TestData.CreateSet(name: "Fav", isFavorite: true);
            var nonfav = TestData.CreateSet(name: "NonFav", isFavorite: false);
            var sets = new[] { fav, nonfav };
            var indexes = TestData.BuildIndexes(sets);

            var result = OutfitSetFiltering.ApplyFavoriteFilter(
                sets, false, indexes.FavoriteIds).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        // Expected: ApplyFavoriteFilter returns only favorites when favoritesOnly is true
        public void ApplyFavoriteFilter_Active_OnlyFavorites()
        {
            var fav = TestData.CreateSet(name: "Fav", isFavorite: true);
            var nonfav = TestData.CreateSet(name: "NonFav", isFavorite: false);
            var sets = new[] { fav, nonfav };
            var indexes = TestData.BuildIndexes(sets);

            var result = OutfitSetFiltering.ApplyFavoriteFilter(
                sets, true, indexes.FavoriteIds).ToList();

            Assert.Single(result);
            Assert.Equal("Fav", result[0].Name);
        }

        // --- Validity filtering ---

        [Fact]
        // Expected: ApplyValidityFilter returns all sets when showInvalid is true
        public void ApplyValidityFilter_ShowInvalid_ReturnsAll()
        {
            var valid = TestData.CreateSet(name: "Valid", isValid: true);
            var invalid = TestData.CreateSet(name: "Invalid", isValid: false);
            var sets = new[] { valid, invalid };
            var indexes = TestData.BuildIndexes(sets);

            var result = OutfitSetFiltering.ApplyValidityFilter(
                sets, true, indexes.ValidIds).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        // Expected: ApplyValidityFilter filters out invalid sets when showInvalid is false
        public void ApplyValidityFilter_HideInvalid_FiltersOut()
        {
            var valid = TestData.CreateSet(name: "Valid", isValid: true);
            var invalid = TestData.CreateSet(name: "Invalid", isValid: false);
            var sets = new[] { valid, invalid };
            var indexes = TestData.BuildIndexes(sets);

            var result = OutfitSetFiltering.ApplyValidityFilter(
                sets, false, indexes.ValidIds).ToList();

            Assert.Single(result);
            Assert.Equal("Valid", result[0].Name);
        }

        [Fact]
        // Expected: ApplyInvalidOnlyFilter returns only invalid sets when invalidOnly is true
        public void ApplyInvalidOnlyFilter_Active_OnlyInvalid()
        {
            var valid = TestData.CreateSet(name: "Valid", isValid: true);
            var invalid = TestData.CreateSet(name: "Invalid", isValid: false);
            var sets = new[] { valid, invalid };
            var indexes = TestData.BuildIndexes(sets);

            var result = OutfitSetFiltering.ApplyInvalidOnlyFilter(
                sets, true, indexes.ValidIds).ToList();

            Assert.Single(result);
            Assert.Equal("Invalid", result[0].Name);
        }

        // --- Search filtering ---

        [Fact]
        // Expected: ApplySearchFilter returns all sets when search text is empty/whitespace
        public void ApplySearchFilter_EmptySearch_ReturnsAll()
        {
            var sets = new[] { TestData.CreateSet(name: "A"), TestData.CreateSet(name: "B") };
            var indexes = TestData.BuildIndexes(sets);

            var result = OutfitSetFiltering.ApplySearchFilter(
                sets, "  ", SearchScope.Set, indexes.SetSearchText, indexes.SetItemSearchText).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        // Expected: ApplySearchFilter with Set scope matches set name
        public void ApplySearchFilter_SetScope_MatchesName()
        {
            var setA = TestData.CreateSet(name: "Summer Outfit");
            var setB = TestData.CreateSet(name: "Winter Outfit");
            var sets = new[] { setA, setB };
            var indexes = TestData.BuildIndexes(sets);

            var result = OutfitSetFiltering.ApplySearchFilter(
                sets, "Summer", SearchScope.Set, indexes.SetSearchText, indexes.SetItemSearchText).ToList();

            Assert.Single(result);
            Assert.Equal("Summer Outfit", result[0].Name);
        }

        [Fact]
        // Expected: ApplySearchFilter with Item scope matches item search text
        public void ApplySearchFilter_ItemScope_MatchesItems()
        {
            var set = TestData.CreateSet(name: "Outfit", shirtId: "fancy_shirt");
            var sets = new[] { set };
            var indexes = TestData.BuildIndexes(sets);

            var result = OutfitSetFiltering.ApplySearchFilter(
                sets, "fancy", SearchScope.Item, indexes.SetSearchText, indexes.SetItemSearchText).ToList();

            Assert.Single(result);
        }

        [Fact]
        // Expected: ApplySearchFilter with All scope matches either name or item text
        public void ApplySearchFilter_AllScope_MatchesEither()
        {
            var setByName = TestData.CreateSet(name: "Fancy Outfit", shirtId: "plain");
            var setByItem = TestData.CreateSet(name: "Plain Outfit", shirtId: "fancy_shirt");
            var setNeither = TestData.CreateSet(name: "Other Outfit", shirtId: "other");
            var sets = new[] { setByName, setByItem, setNeither };
            var indexes = TestData.BuildIndexes(sets);

            var result = OutfitSetFiltering.ApplySearchFilter(
                sets, "fancy", SearchScope.All, indexes.SetSearchText, indexes.SetItemSearchText).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        // Expected: ApplySearchFilter is case-insensitive
        public void ApplySearchFilter_CaseInsensitive()
        {
            var set = TestData.CreateSet(name: "Summer Outfit");
            var sets = new[] { set };
            var indexes = TestData.BuildIndexes(sets);

            var result = OutfitSetFiltering.ApplySearchFilter(
                sets, "summer", SearchScope.Set, indexes.SetSearchText, indexes.SetItemSearchText).ToList();

            Assert.Single(result);
        }

        [Fact]
        // Expected: ApplySearchFilter returns empty when nothing matches
        public void ApplySearchFilter_NoMatch_ReturnsEmpty()
        {
            var set = TestData.CreateSet(name: "Summer Outfit");
            var sets = new[] { set };
            var indexes = TestData.BuildIndexes(sets);

            var result = OutfitSetFiltering.ApplySearchFilter(
                sets, "xyz", SearchScope.Set, indexes.SetSearchText, indexes.SetItemSearchText).ToList();

            Assert.Empty(result);
        }

        [Fact]
        // Expected: ApplySearchFilter trims whitespace from search text before matching
        public void ApplySearchFilter_TrimsWhitespace()
        {
            var set = TestData.CreateSet(name: "Summer Outfit");
            var sets = new[] { set };
            var indexes = TestData.BuildIndexes(sets);

            var result = OutfitSetFiltering.ApplySearchFilter(
                sets, "  Summer  ", SearchScope.Set, indexes.SetSearchText, indexes.SetItemSearchText).ToList();

            Assert.Single(result);
        }

        // --- Additional edge cases ---

        [Fact]
        // Expected: ApplyInvalidOnlyFilter returns all sets when invalidOnly is false
        public void ApplyInvalidOnlyFilter_NotActive_ReturnsAll()
        {
            var valid = TestData.CreateSet(name: "Valid", isValid: true);
            var invalid = TestData.CreateSet(name: "Invalid", isValid: false);
            var sets = new[] { valid, invalid };
            var indexes = TestData.BuildIndexes(sets);

            var result = OutfitSetFiltering.ApplyInvalidOnlyFilter(
                sets, false, indexes.ValidIds).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        // Expected: ApplySearchFilter returns all sets when search text is null
        public void ApplySearchFilter_NullSearch_ReturnsAll()
        {
            var sets = new[] { TestData.CreateSet(name: "A"), TestData.CreateSet(name: "B") };
            var indexes = TestData.BuildIndexes(sets);

            var result = OutfitSetFiltering.ApplySearchFilter(
                sets, null!, SearchScope.Set, indexes.SetSearchText, indexes.SetItemSearchText).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        // Expected: ApplyTagFilter matchAny ignores selected tags that have no sets in the index
        public void ApplyTagFilter_MatchAny_UnknownTag_Ignored()
        {
            var set = TestData.CreateSet(name: "A", tags: new List<string> { "Spring" });
            var sets = new[] { set };
            var indexes = TestData.BuildIndexes(sets);

            var tags = new HashSet<string>(TranslationCache.TagComparer) { "Spring", "NonExistentTag" };
            var result = OutfitSetFiltering.ApplyTagFilter(
                sets, tags, false, indexes.ByTag, TranslationCache.TagComparison).ToList();

            Assert.Single(result);
            Assert.Equal("A", result[0].Name);
        }

        [Fact]
        // Expected: MatchesSearch returns false when set ID is not in the set search text index
        public void MatchesSearch_SetScope_NotInIndex_ReturnsFalse()
        {
            var set = TestData.CreateSet(name: "Test");
            var emptySetSearchText = new Dictionary<string, string>();
            var emptyItemSearchText = new Dictionary<string, string>();

            var result = OutfitSetFiltering.MatchesSearch(
                set, "Test", SearchScope.Set, emptySetSearchText, emptyItemSearchText);

            Assert.False(result);
        }

        [Fact]
        // Expected: MatchesSearch returns false when set ID is not in the item search text index
        public void MatchesSearch_ItemScope_NotInIndex_ReturnsFalse()
        {
            var set = TestData.CreateSet(name: "Test", shirtId: "shirt1");
            var emptySetSearchText = new Dictionary<string, string>();
            var emptyItemSearchText = new Dictionary<string, string>();

            var result = OutfitSetFiltering.MatchesSearch(
                set, "shirt1", SearchScope.Item, emptySetSearchText, emptyItemSearchText);

            Assert.False(result);
        }

        [Fact]
        // Expected: MatchesSearch with All scope returns true when set is only in set index (not item index)
        public void MatchesSearch_AllScope_PartialIndex_MatchesAvailable()
        {
            var set = TestData.CreateSet(name: "Fancy Outfit", shirtId: "plain");
            var setSearchText = new Dictionary<string, string> { { set.Id, "Fancy Outfit" } };
            var setItemSearchText = new Dictionary<string, string>();

            var result = OutfitSetFiltering.MatchesSearch(
                set, "Fancy", SearchScope.All, setSearchText, setItemSearchText);

            Assert.True(result);
        }

        [Fact]
        // Expected: ApplySearchFilter with Item scope matches hat ID in item search text
        public void ApplySearchFilter_ItemScope_MatchesHatId()
        {
            var set = TestData.CreateSet(name: "Hat Outfit", shirtId: "plain", hatId: "cool_hat");
            var sets = new[] { set };
            var indexes = TestData.BuildIndexes(sets);

            var result = OutfitSetFiltering.ApplySearchFilter(
                sets, "cool_hat", SearchScope.Item, indexes.SetSearchText, indexes.SetItemSearchText).ToList();

            Assert.Single(result);
        }
    }
}
