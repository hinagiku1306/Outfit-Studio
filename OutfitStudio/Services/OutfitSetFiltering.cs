using System;
using System.Collections.Generic;
using System.Linq;
using OutfitStudio.Managers;
using OutfitStudio.Models;

namespace OutfitStudio.Services
{
    internal static class OutfitSetFiltering
    {
        internal static IEnumerable<OutfitSet> ApplyTagFilter(
            IEnumerable<OutfitSet> sets, HashSet<string> selectedTags, bool matchAll,
            Dictionary<string, HashSet<string>> byTag, StringComparison tagComparison)
        {
            if (selectedTags.Count == 0)
                return sets;

            if (matchAll)
            {
                return sets.Where(s => selectedTags.All(tag =>
                    s.Tags.Any(t => t.Equals(tag, tagComparison))));
            }

            HashSet<string> matchingIds = new();
            foreach (var tag in selectedTags)
            {
                if (byTag.TryGetValue(tag, out var ids))
                {
                    matchingIds.UnionWith(ids);
                }
            }

            return sets.Where(s => matchingIds.Contains(s.Id));
        }

        internal static IEnumerable<OutfitSet> ApplyScopeFilter(
            IEnumerable<OutfitSet> sets, bool showGlobal, bool showLocal,
            HashSet<string> globalIds, HashSet<string> localIds)
        {
            if (showGlobal && showLocal)
                return sets;

            if (!showGlobal && !showLocal)
                return Enumerable.Empty<OutfitSet>();

            if (showGlobal)
                return sets.Where(s => globalIds.Contains(s.Id));

            return sets.Where(s => localIds.Contains(s.Id));
        }

        internal static IEnumerable<OutfitSet> ApplyFavoriteFilter(
            IEnumerable<OutfitSet> sets, bool favoritesOnly, HashSet<string> favoriteIds)
        {
            if (!favoritesOnly)
                return sets;

            return sets.Where(s => favoriteIds.Contains(s.Id));
        }

        internal static IEnumerable<OutfitSet> ApplyValidityFilter(
            IEnumerable<OutfitSet> sets, bool showInvalid, HashSet<string> validIds)
        {
            if (showInvalid)
                return sets;

            return sets.Where(s => validIds.Contains(s.Id));
        }

        internal static IEnumerable<OutfitSet> ApplyInvalidOnlyFilter(
            IEnumerable<OutfitSet> sets, bool invalidOnly, HashSet<string> validIds)
        {
            if (!invalidOnly)
                return sets;

            return sets.Where(s => !validIds.Contains(s.Id));
        }

        internal static IEnumerable<OutfitSet> ApplySearchFilter(
            IEnumerable<OutfitSet> sets, string searchText, SearchScope scope,
            Dictionary<string, string> setSearchText, Dictionary<string, string> setItemSearchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return sets;

            string search = searchText.Trim();

            return sets.Where(s => MatchesSearch(s, search, scope, setSearchText, setItemSearchText));
        }

        internal static bool MatchesSearch(
            OutfitSet set, string search, SearchScope scope,
            Dictionary<string, string> setSearchText, Dictionary<string, string> setItemSearchText)
        {
            if (scope == SearchScope.Set)
                return setSearchText.TryGetValue(set.Id, out var name) &&
                       name.Contains(search, StringComparison.OrdinalIgnoreCase);

            if (scope == SearchScope.Item)
                return setItemSearchText.TryGetValue(set.Id, out var items) &&
                       items.Contains(search, StringComparison.OrdinalIgnoreCase);

            return (setSearchText.TryGetValue(set.Id, out var n) &&
                    n.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                   (setItemSearchText.TryGetValue(set.Id, out var it) &&
                    it.Contains(search, StringComparison.OrdinalIgnoreCase));
        }
    }
}
