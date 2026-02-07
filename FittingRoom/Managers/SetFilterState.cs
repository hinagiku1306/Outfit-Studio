using System;
using System.Collections.Generic;

namespace FittingRoom.Managers
{
    public enum SearchScope
    {
        Set,
        Item,
        All
    }

    public class SetFilterState
    {
        public SearchScope SearchScope { get; set; } = SearchScope.Set;
        public string SearchText { get; set; } = "";
        public HashSet<string> SelectedTags { get; } = new(TranslationCache.TagComparer);
        public bool MatchAllTags { get; set; } = true;
        public bool FavoritesOnly { get; set; }
        public bool ShowGlobal { get; set; } = true;
        public bool ShowLocal { get; set; } = true;
        public bool ShowInvalid { get; set; } = true;

        public bool HasActiveFilters =>
            !string.IsNullOrEmpty(SearchText) ||
            SelectedTags.Count > 0 ||
            FavoritesOnly ||
            !ShowGlobal ||
            !ShowLocal ||
            !ShowInvalid;

        public void ToggleTag(string tag)
        {
            if (SelectedTags.Contains(tag))
                SelectedTags.Remove(tag);
            else
                SelectedTags.Add(tag);
        }

        public void ClearTags()
        {
            SelectedTags.Clear();
        }

        public string ToCacheKey()
        {
            string tags = SelectedTags.Count > 0
                ? string.Join(",", SelectedTags.OrderBy(t => t, TranslationCache.TagComparer))
                : "";
            return $"{(int)SearchScope}|{SearchText}|{tags}|{MatchAllTags}|{FavoritesOnly}|{ShowGlobal}|{ShowLocal}|{ShowInvalid}";
        }

        public void Reset()
        {
            SearchScope = SearchScope.Set;
            SearchText = "";
            SelectedTags.Clear();
            MatchAllTags = true;
            FavoritesOnly = false;
            ShowGlobal = true;
            ShowLocal = true;
            ShowInvalid = true;
        }
    }
}
