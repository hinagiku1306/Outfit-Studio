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
        private const int SearchDebounceMs = 300;

        public SearchScope SearchScope { get; set; } = SearchScope.Set;
        public string SearchText { get; private set; } = "";
        public HashSet<string> SelectedTags { get; } = new(StringComparer.OrdinalIgnoreCase);
        public bool MatchAllTags { get; set; }
        public bool FavoritesOnly { get; set; }
        public bool ShowGlobal { get; set; } = true;
        public bool ShowLocal { get; set; } = true;
        public bool ShowInvalid { get; set; } = true;

        private string pendingSearchText = "";
        private double lastSearchChangeTime;
        private bool searchPending;

        public bool HasActiveFilters =>
            !string.IsNullOrEmpty(SearchText) ||
            SelectedTags.Count > 0 ||
            FavoritesOnly ||
            !ShowGlobal ||
            !ShowLocal ||
            !ShowInvalid;

        public void SetSearchText(string text, double currentTime)
        {
            pendingSearchText = text ?? "";
            lastSearchChangeTime = currentTime;
            searchPending = true;
        }

        public bool UpdateSearchDebounce(double currentTime)
        {
            if (!searchPending)
                return false;

            if (currentTime - lastSearchChangeTime >= SearchDebounceMs)
            {
                SearchText = pendingSearchText;
                searchPending = false;
                return true;
            }

            return false;
        }

        public void ForceApplySearch()
        {
            if (searchPending)
            {
                SearchText = pendingSearchText;
                searchPending = false;
            }
        }

        public string GetPendingSearchText() => pendingSearchText;

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

        public void Reset()
        {
            SearchScope = SearchScope.Set;
            SearchText = "";
            pendingSearchText = "";
            searchPending = false;
            SelectedTags.Clear();
            MatchAllTags = false;
            FavoritesOnly = false;
            ShowGlobal = true;
            ShowLocal = true;
            ShowInvalid = true;
        }
    }
}
