using System;
using System.Collections.Generic;
using System.Linq;
using OutfitStudio.Managers;
using OutfitStudio.Models;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace OutfitStudio.Services
{
    public class OutfitSetStore
    {
        private const string GlobalFileName = "outfit-sets-global.json";
        private const string LocalSaveDataKey = "OutfitStudio.LocalSets";

        private static readonly List<string> DefaultTags = new()
        {
            "Spring", "Summer", "Fall", "Winter", "Wedding", "Combat", "Daily"
        };

        private readonly IModHelper helper;
        private readonly IMonitor monitor;

        private OutfitSetGlobalData globalData = new();
        private OutfitSetLocalData localData = new();

        private readonly Dictionary<string, OutfitSet> byId = new();
        private readonly Dictionary<string, HashSet<string>> byTag;
        private readonly HashSet<string> favoriteIds = new();
        private readonly HashSet<string> globalIds = new();
        private readonly HashSet<string> localIds = new();
        private readonly HashSet<string> validIds = new();
        private readonly Dictionary<string, string> itemDisplayNames = new();
        private readonly Dictionary<string, string> setSearchText = new();
        private readonly Dictionary<string, string> setItemSearchText = new();
        private List<OutfitSet>? cachedFilteredSets;
        private string? cachedFilterKey;
        private List<string>? cachedAllTags;

        public OutfitSetStore(IModHelper helper, IMonitor monitor)
        {
            this.helper = helper;
            this.monitor = monitor;
            byTag = new(TranslationCache.TagComparer);
        }

        public void LoadGlobalData()
        {
            globalData = helper.Data.ReadJsonFile<OutfitSetGlobalData>(GlobalFileName)
                         ?? CreateDefaultGlobalData();

            RebuildIndexes();
            monitor.Log($"Loaded {globalData.Sets.Count} global outfit sets.", LogLevel.Debug);
        }

        public void LoadLocalData()
        {
            localData = helper.Data.ReadSaveData<OutfitSetLocalData>(LocalSaveDataKey)
                        ?? new OutfitSetLocalData();

            foreach (var set in localData.Sets)
            {
                set.IsGlobal = false;
            }

            RebuildIndexes();
            ValidateAllSets();
            monitor.Log($"Loaded {localData.Sets.Count} local outfit sets.", LogLevel.Debug);
        }

        public void ClearLocalData()
        {
            localData = new OutfitSetLocalData();
            RebuildIndexes();
        }

        public List<OutfitSet> GetAllSetsSorted()
        {
            return byId.Values
                .OrderByDescending(s => s.IsFavorite)
                .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public List<OutfitSet> GetFilteredSets(SetFilterState filter)
        {
            string key = filter.ToCacheKey();
            if (cachedFilteredSets != null && cachedFilterKey == key)
                return cachedFilteredSets;

            IEnumerable<OutfitSet> result = byId.Values;

            result = ApplyTagFilter(result, filter.SelectedTags, filter.MatchAllTags);
            result = ApplyScopeFilter(result, filter.ShowGlobal, filter.ShowLocal);
            result = ApplyFavoriteFilter(result, filter.FavoritesOnly);
            result = ApplyValidityFilter(result, filter.ShowInvalid);
            result = ApplySearchFilter(result, filter.SearchText, filter.SearchScope);

            cachedFilteredSets = result
                .OrderByDescending(s => s.IsFavorite)
                .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            cachedFilterKey = key;
            return cachedFilteredSets;
        }

        private IEnumerable<OutfitSet> ApplyTagFilter(IEnumerable<OutfitSet> sets, HashSet<string> selectedTags, bool matchAll)
        {
            if (selectedTags.Count == 0)
                return sets;

            if (matchAll)
            {
                return sets.Where(s => selectedTags.All(tag =>
                    s.Tags.Any(t => t.Equals(tag, TranslationCache.TagComparison))));
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

        private IEnumerable<OutfitSet> ApplyScopeFilter(IEnumerable<OutfitSet> sets, bool showGlobal, bool showLocal)
        {
            if (showGlobal && showLocal)
                return sets;

            if (!showGlobal && !showLocal)
                return sets;

            if (showGlobal)
                return sets.Where(s => globalIds.Contains(s.Id));

            return sets.Where(s => localIds.Contains(s.Id));
        }

        private IEnumerable<OutfitSet> ApplyFavoriteFilter(IEnumerable<OutfitSet> sets, bool favoritesOnly)
        {
            if (!favoritesOnly)
                return sets;

            return sets.Where(s => favoriteIds.Contains(s.Id));
        }

        private IEnumerable<OutfitSet> ApplyValidityFilter(IEnumerable<OutfitSet> sets, bool showInvalid)
        {
            if (showInvalid)
                return sets;

            return sets.Where(s => validIds.Contains(s.Id));
        }

        private IEnumerable<OutfitSet> ApplySearchFilter(IEnumerable<OutfitSet> sets, string searchText, SearchScope scope)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return sets;

            string search = searchText.Trim();

            return sets.Where(s => MatchesSearch(s, search, scope));
        }

        private bool MatchesSearch(OutfitSet set, string search, SearchScope scope)
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

        public OutfitSet? GetById(string id)
        {
            return byId.TryGetValue(id, out var set) ? set : null;
        }

        public void Add(OutfitSet set)
        {
            if (set.IsGlobal)
            {
                globalData.Sets.Add(set);
                PersistGlobalData();
            }
            else
            {
                localData.Sets.Add(set);
                PersistLocalData();
            }

            byId[set.Id] = set;
            ValidateSet(set);
            UpdateIndexesForSet(set);
            InvalidateFilterCache();
        }

        public void Update(OutfitSet set)
        {
            if (!byId.ContainsKey(set.Id))
                return;

            RemoveFromIndexes(set.Id);
            byId[set.Id] = set;
            ValidateSet(set);
            UpdateIndexesForSet(set);
            InvalidateFilterCache();

            if (set.IsGlobal)
            {
                int index = globalData.Sets.FindIndex(s => s.Id == set.Id);
                if (index >= 0)
                {
                    globalData.Sets[index] = set;
                    PersistGlobalData();
                }
            }
            else
            {
                int index = localData.Sets.FindIndex(s => s.Id == set.Id);
                if (index >= 0)
                {
                    localData.Sets[index] = set;
                    PersistLocalData();
                }
            }
        }

        public void Delete(string id)
        {
            if (!byId.TryGetValue(id, out var set))
                return;

            RemoveFromIndexes(id);
            byId.Remove(id);
            InvalidateFilterCache();

            if (set.IsGlobal)
            {
                globalData.Sets.RemoveAll(s => s.Id == id);
                PersistGlobalData();
            }
            else
            {
                localData.Sets.RemoveAll(s => s.Id == id);
                PersistLocalData();
            }
        }

        public List<string> GetAllTags()
        {
            if (cachedAllTags != null)
                return cachedAllTags;

            cachedAllTags = globalData.Tags.OrderBy(t => t, TranslationCache.TagComparer).ToList();
            return cachedAllTags;
        }

        public void AddTag(string tag)
        {
            string trimmed = tag.Trim();
            if (string.IsNullOrEmpty(trimmed))
                return;

            if (globalData.Tags.Any(t => t.Equals(trimmed, TranslationCache.TagComparison)))
                return;

            globalData.Tags.Add(trimmed);
            cachedAllTags = null;
            PersistGlobalData();
        }

        public bool RemoveTag(string tag)
        {
            int index = globalData.Tags.FindIndex(t => t.Equals(tag, TranslationCache.TagComparison));
            if (index < 0)
                return false;

            string actualTag = globalData.Tags[index];
            globalData.Tags.RemoveAt(index);

            if (byTag.TryGetValue(actualTag, out var outfitIds))
            {
                foreach (var id in outfitIds.ToList())
                {
                    if (byId.TryGetValue(id, out var outfit))
                    {
                        outfit.Tags.RemoveAll(t => t.Equals(actualTag, TranslationCache.TagComparison));
                    }
                }
                byTag.Remove(actualTag);
            }

            cachedAllTags = null;
            InvalidateFilterCache();
            PersistGlobalData();
            return true;
        }

        public int RemoveTags(IEnumerable<string> tags)
        {
            int count = 0;
            foreach (var tag in tags)
            {
                if (RemoveTag(tag))
                    count++;
            }
            return count;
        }

        public string? GetItemDisplayName(string? itemId, string typePrefix)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;

            string qualifiedId = typePrefix + itemId;

            if (itemDisplayNames.TryGetValue(qualifiedId, out string? cached))
                return cached;

            var data = ItemRegistry.GetData(qualifiedId);
            string? displayName = data?.DisplayName;

            if (displayName != null)
                itemDisplayNames[qualifiedId] = displayName;

            return displayName;
        }

        public bool IsItemValid(string? itemId, string typePrefix)
        {
            if (string.IsNullOrEmpty(itemId))
                return true;

            string qualifiedId = typePrefix + itemId;
            return ItemRegistry.Exists(qualifiedId);
        }

        public OutfitSet CreateFromCurrentOutfit(string name, List<string> tags, bool isFavorite, bool isGlobal,
            string? shirtId = null, string? pantsId = null, string? hatId = null, bool useCurrentOutfit = true)
        {
            var set = new OutfitSet
            {
                Name = name,
                Tags = tags,
                IsFavorite = isFavorite,
                IsGlobal = isGlobal,
                ShirtId = useCurrentOutfit ? OutfitState.GetClothingId(Game1.player.shirtItem.Value) : shirtId,
                PantsId = useCurrentOutfit ? OutfitState.GetClothingId(Game1.player.pantsItem.Value) : pantsId,
                HatId = useCurrentOutfit ? OutfitState.GetHatIdFromItem(Game1.player.hat.Value) : hatId
            };

            Add(set);
            return set;
        }

        public void ApplySet(OutfitSet set)
        {
            if (ItemIdHelper.IsNoShirtId(set.ShirtId) || IsItemValid(set.ShirtId, "(S)"))
                OutfitState.ApplyShirt(set.ShirtId ?? "");

            if (ItemIdHelper.IsNoPantsId(set.PantsId) || IsItemValid(set.PantsId, "(P)"))
                OutfitState.ApplyPants(set.PantsId ?? "");

            if (ItemIdHelper.IsNoHatId(set.HatId) || IsItemValid(set.HatId, "(H)"))
                OutfitState.ApplyHat(set.HatId ?? "");
        }

        private OutfitSetGlobalData CreateDefaultGlobalData()
        {
            var data = new OutfitSetGlobalData
            {
                Version = 1,
                Tags = new List<string>(DefaultTags),
                Sets = new List<OutfitSet>()
            };

            helper.Data.WriteJsonFile(GlobalFileName, data);
            monitor.Log("Created default outfit sets global file.", LogLevel.Debug);
            return data;
        }

        private void InvalidateFilterCache()
        {
            cachedFilteredSets = null;
            cachedFilterKey = null;
        }

        private void RebuildIndexes()
        {
            byId.Clear();
            byTag.Clear();
            favoriteIds.Clear();
            globalIds.Clear();
            localIds.Clear();
            validIds.Clear();
            setSearchText.Clear();
            setItemSearchText.Clear();
            InvalidateFilterCache();
            cachedAllTags = null;

            foreach (var set in globalData.Sets)
            {
                set.IsGlobal = true;
                byId[set.Id] = set;
                UpdateIndexesForSet(set);
            }

            foreach (var set in localData.Sets)
            {
                set.IsGlobal = false;
                byId[set.Id] = set;
                UpdateIndexesForSet(set);
            }
        }

        private void UpdateIndexesForSet(OutfitSet set)
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

            setSearchText[set.Id] = set.Name;
            var parts = new List<string>(3);
            if (!string.IsNullOrEmpty(set.ShirtId))
                parts.Add(GetItemDisplayName(set.ShirtId, "(S)") ?? "");
            if (!string.IsNullOrEmpty(set.PantsId))
                parts.Add(GetItemDisplayName(set.PantsId, "(P)") ?? "");
            if (!string.IsNullOrEmpty(set.HatId))
                parts.Add(GetItemDisplayName(set.HatId, "(H)") ?? "");
            setItemSearchText[set.Id] = string.Join(" ", parts);
        }

        private void RemoveFromIndexes(string id)
        {
            favoriteIds.Remove(id);
            globalIds.Remove(id);
            localIds.Remove(id);
            validIds.Remove(id);
            setSearchText.Remove(id);
            setItemSearchText.Remove(id);

            foreach (var tagSet in byTag.Values)
            {
                tagSet.Remove(id);
            }
        }

        private void ValidateAllSets()
        {
            foreach (var set in byId.Values)
            {
                ValidateSet(set);
            }
        }

        private void ValidateSet(OutfitSet set)
        {
            bool shirtValid = ItemIdHelper.IsNoShirtId(set.ShirtId) || IsItemValid(set.ShirtId, "(S)");
            bool pantsValid = ItemIdHelper.IsNoPantsId(set.PantsId) || IsItemValid(set.PantsId, "(P)");
            bool hatValid = ItemIdHelper.IsNoHatId(set.HatId) || IsItemValid(set.HatId, "(H)");

            set.IsValid = shirtValid && pantsValid && hatValid;

            if (set.IsValid)
                validIds.Add(set.Id);
            else
                validIds.Remove(set.Id);
        }

        private void PersistGlobalData()
        {
            helper.Data.WriteJsonFile(GlobalFileName, globalData);
        }

        private void PersistLocalData()
        {
            helper.Data.WriteSaveData(LocalSaveDataKey, localData);
        }
    }
}
