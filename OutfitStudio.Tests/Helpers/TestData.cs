using OutfitStudio.Models;

namespace OutfitStudio.Tests.Helpers
{
    internal static class TestData
    {
        internal static ScheduleRule CreateRule(
            string? id = null,
            string? name = null,
            bool isEnabled = true,
            bool seasonsSelectAll = false,
            List<string>? selectedSeasons = null,
            bool festivalsSelectAll = false,
            List<string>? selectedFestivals = null,
            bool weatherSelectAll = false,
            List<string>? selectedWeather = null,
            bool locationsSelectAll = false,
            List<string>? selectedLocations = null,
            bool areasSelectAll = false,
            List<string>? selectedAreas = null,
            List<string>? selectedSetIds = null,
            int priority = 2,
            bool isWeddingDay = false,
            bool advanceOnWarp = false)
        {
            return new ScheduleRule
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Name = name ?? "Test Rule",
                IsEnabled = isEnabled,
                SeasonsSelectAll = seasonsSelectAll,
                SelectedSeasons = selectedSeasons ?? new List<string>(),
                FestivalsSelectAll = festivalsSelectAll,
                SelectedFestivals = selectedFestivals ?? new List<string>(),
                WeatherSelectAll = weatherSelectAll,
                SelectedWeather = selectedWeather ?? new List<string>(),
                LocationsSelectAll = locationsSelectAll,
                SelectedLocations = selectedLocations ?? new List<string>(),
                AreasSelectAll = areasSelectAll,
                SelectedAreas = selectedAreas ?? new List<string>(),
                SelectedSetIds = selectedSetIds ?? new List<string>(),
                Priority = priority,
                IsWeddingDay = isWeddingDay,
                AdvanceOnWarp = advanceOnWarp
            };
        }

        /// <summary>
        /// Creates an OutfitSet with sensible defaults. All parameters are optional overrides.
        /// </summary>
        internal static OutfitSet CreateSet(
            string? id = null,
            string? name = null,
            List<string>? tags = null,
            bool isFavorite = false,
            bool isGlobal = true,
            bool isValid = true,
            string? shirtId = "shirt1",
            string? pantsId = "pants1",
            string? hatId = null)
        {
            return new OutfitSet
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Name = name ?? "Test Outfit",
                Tags = tags ?? new List<string>(),
                IsFavorite = isFavorite,
                IsGlobal = isGlobal,
                IsValid = isValid,
                ShirtId = shirtId,
                PantsId = pantsId,
                HatId = hatId
            };
        }

        /// <summary>
        /// Builds all index dictionaries from a list of sets, mirroring OutfitSetStore.UpdateIndexesForSet logic.
        /// Provides setSearchText (name) and setItemSearchText (joined item IDs as placeholders).
        /// </summary>
        internal static SetIndexes BuildIndexes(IEnumerable<OutfitSet> sets)
        {
            var byTag = new Dictionary<string, HashSet<string>>(TranslationCache.TagComparer);
            var favoriteIds = new HashSet<string>();
            var globalIds = new HashSet<string>();
            var localIds = new HashSet<string>();
            var validIds = new HashSet<string>();
            var setSearchText = new Dictionary<string, string>();
            var setItemSearchText = new Dictionary<string, string>();

            foreach (var set in sets)
            {
                if (set.IsFavorite)
                    favoriteIds.Add(set.Id);

                if (set.IsGlobal)
                    globalIds.Add(set.Id);
                else
                    localIds.Add(set.Id);

                if (set.IsValid)
                    validIds.Add(set.Id);

                foreach (var tag in set.Tags)
                {
                    if (!byTag.TryGetValue(tag, out var ids))
                    {
                        ids = new HashSet<string>();
                        byTag[tag] = ids;
                    }
                    ids.Add(set.Id);
                }

                // Use the set name as search text
                setSearchText[set.Id] = set.Name;

                // Build item search text from item IDs (in real code this uses display names,
                // but for testing we use the raw IDs as stand-ins)
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(set.ShirtId))
                    parts.Add(set.ShirtId);
                if (!string.IsNullOrEmpty(set.PantsId))
                    parts.Add(set.PantsId);
                if (!string.IsNullOrEmpty(set.HatId))
                    parts.Add(set.HatId);
                setItemSearchText[set.Id] = string.Join(" ", parts);
            }

            return new SetIndexes(byTag, favoriteIds, globalIds, localIds, validIds, setSearchText, setItemSearchText);
        }
    }

    /// <summary>
    /// Holds all index dictionaries needed by OutfitSetFiltering methods.
    /// </summary>
    internal record SetIndexes(
        Dictionary<string, HashSet<string>> ByTag,
        HashSet<string> FavoriteIds,
        HashSet<string> GlobalIds,
        HashSet<string> LocalIds,
        HashSet<string> ValidIds,
        Dictionary<string, string> SetSearchText,
        Dictionary<string, string> SetItemSearchText);
}
