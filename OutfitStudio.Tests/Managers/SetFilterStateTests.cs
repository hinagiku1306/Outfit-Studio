using OutfitStudio.Managers;
using Xunit;

namespace OutfitStudio.Tests.Managers
{
    public class SetFilterStateTests
    {
        [Fact]
        // Expected: Two default SetFilterState instances produce the same cache key
        public void ToCacheKey_DefaultState_IsConsistent()
        {
            var a = new SetFilterState();
            var b = new SetFilterState();
            Assert.Equal(a.ToCacheKey(), b.ToCacheKey());
        }

        [Fact]
        // Expected: Changing a single property produces a different cache key
        public void ToCacheKey_DifferentStates_DifferentKeys()
        {
            var a = new SetFilterState();
            var b = new SetFilterState { FavoritesOnly = true };
            Assert.NotEqual(a.ToCacheKey(), b.ToCacheKey());
        }

        [Fact]
        // Expected: Tags added in different order produce the same cache key (sorted alphabetically)
        public void ToCacheKey_SortsTagsAlphabetically()
        {
            var a = new SetFilterState();
            a.SelectedTags.Add("Winter");
            a.SelectedTags.Add("Daily");

            var b = new SetFilterState();
            b.SelectedTags.Add("Daily");
            b.SelectedTags.Add("Winter");

            Assert.Equal(a.ToCacheKey(), b.ToCacheKey());
        }

        [Fact]
        // Expected: A fresh SetFilterState has no active filters
        public void HasActiveFilters_Default_False()
        {
            Assert.False(new SetFilterState().HasActiveFilters);
        }

        [Fact]
        // Expected: Setting SearchText makes HasActiveFilters true
        public void HasActiveFilters_WithSearchText_True()
        {
            var state = new SetFilterState { SearchText = "hello" };
            Assert.True(state.HasActiveFilters);
        }

        [Fact]
        // Expected: Setting FavoritesOnly makes HasActiveFilters true
        public void HasActiveFilters_FavoritesOnly_True()
        {
            var state = new SetFilterState { FavoritesOnly = true };
            Assert.True(state.HasActiveFilters);
        }

        [Fact]
        // Expected: Disabling ShowGlobal makes HasActiveFilters true (default is true)
        public void HasActiveFilters_ShowGlobalFalse_True()
        {
            var state = new SetFilterState { ShowGlobal = false };
            Assert.True(state.HasActiveFilters);
        }

        [Fact]
        // Expected: ToggleTag adds a tag when it's not present
        public void ToggleTag_AddsWhenAbsent()
        {
            var state = new SetFilterState();
            state.ToggleTag("Spring");
            Assert.Contains("Spring", state.SelectedTags);
        }

        [Fact]
        // Expected: ToggleTag removes a tag when it's already present
        public void ToggleTag_RemovesWhenPresent()
        {
            var state = new SetFilterState();
            state.ToggleTag("Spring");
            state.ToggleTag("Spring");
            Assert.DoesNotContain("Spring", state.SelectedTags);
        }

        [Fact]
        // Expected: SelectedTags uses case-insensitive comparison (adding "spring" then checking "SPRING")
        public void ToggleTag_CaseInsensitive()
        {
            var state = new SetFilterState();
            state.ToggleTag("spring");
            Assert.Contains("SPRING", state.SelectedTags);
        }

        [Fact]
        // Expected: ClearTags removes all selected tags
        public void ClearTags_RemovesAll()
        {
            var state = new SetFilterState();
            state.SelectedTags.Add("Spring");
            state.SelectedTags.Add("Winter");
            state.ClearTags();
            Assert.Empty(state.SelectedTags);
        }

        [Fact]
        // Expected: Reset restores all properties to their default values
        public void Reset_RestoresDefaults()
        {
            var state = new SetFilterState
            {
                SearchScope = SearchScope.All,
                SearchText = "test",
                MatchAllTags = false,
                FavoritesOnly = true,
                ShowGlobal = false,
                ShowLocal = false,
                ShowInvalid = false,
                InvalidOnly = true
            };
            state.SelectedTags.Add("Spring");

            state.Reset();

            var fresh = new SetFilterState();
            Assert.Equal(fresh.ToCacheKey(), state.ToCacheKey());
            Assert.False(state.HasActiveFilters);
        }

        // --- Additional HasActiveFilters coverage ---

        [Fact]
        // Expected: Non-empty SelectedTags makes HasActiveFilters true
        public void HasActiveFilters_WithSelectedTags_True()
        {
            var state = new SetFilterState();
            state.SelectedTags.Add("Spring");
            Assert.True(state.HasActiveFilters);
        }

        [Fact]
        // Expected: Disabling ShowLocal makes HasActiveFilters true (default is true)
        public void HasActiveFilters_ShowLocalFalse_True()
        {
            var state = new SetFilterState { ShowLocal = false };
            Assert.True(state.HasActiveFilters);
        }

        [Fact]
        // Expected: Setting InvalidOnly makes HasActiveFilters true
        public void HasActiveFilters_InvalidOnly_True()
        {
            var state = new SetFilterState { InvalidOnly = true };
            Assert.True(state.HasActiveFilters);
        }

        [Fact]
        // Expected: Disabling ShowInvalid makes HasActiveFilters true (default is true)
        public void HasActiveFilters_ShowInvalidFalse_True()
        {
            var state = new SetFilterState { ShowInvalid = false };
            Assert.True(state.HasActiveFilters);
        }

        // --- Additional ToCacheKey coverage ---

        [Fact]
        // Expected: Changing any single property from default produces a different cache key
        public void ToCacheKey_EveryPropertyChange_DifferentKey()
        {
            var baseline = new SetFilterState().ToCacheKey();

            Assert.NotEqual(baseline, new SetFilterState { SearchScope = SearchScope.All }.ToCacheKey());
            Assert.NotEqual(baseline, new SetFilterState { SearchText = "x" }.ToCacheKey());
            Assert.NotEqual(baseline, new SetFilterState { MatchAllTags = false }.ToCacheKey());
            Assert.NotEqual(baseline, new SetFilterState { FavoritesOnly = true }.ToCacheKey());
            Assert.NotEqual(baseline, new SetFilterState { ShowGlobal = false }.ToCacheKey());
            Assert.NotEqual(baseline, new SetFilterState { ShowLocal = false }.ToCacheKey());
            Assert.NotEqual(baseline, new SetFilterState { ShowInvalid = false }.ToCacheKey());
            Assert.NotEqual(baseline, new SetFilterState { InvalidOnly = true }.ToCacheKey());
        }
    }
}
