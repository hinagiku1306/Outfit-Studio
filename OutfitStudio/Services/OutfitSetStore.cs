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

        public Action? OnSetsChanged { get; set; }

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
        }

        public void ClearLocalData()
        {
            localData = new OutfitSetLocalData();
            RebuildIndexes();
        }

        public List<OutfitSet> GetAllSets()
        {
            return byId.Values.ToList();
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
            result = ApplyInvalidOnlyFilter(result, filter.InvalidOnly);
            result = ApplySearchFilter(result, filter.SearchText, filter.SearchScope);

            cachedFilteredSets = result
                .OrderByDescending(s => s.IsFavorite)
                .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            cachedFilterKey = key;
            return cachedFilteredSets;
        }

        private IEnumerable<OutfitSet> ApplyTagFilter(IEnumerable<OutfitSet> sets, HashSet<string> selectedTags, bool matchAll)
            => OutfitSetFiltering.ApplyTagFilter(sets, selectedTags, matchAll, byTag, TranslationCache.TagComparison);

        private IEnumerable<OutfitSet> ApplyScopeFilter(IEnumerable<OutfitSet> sets, bool showGlobal, bool showLocal)
            => OutfitSetFiltering.ApplyScopeFilter(sets, showGlobal, showLocal, globalIds, localIds);

        private IEnumerable<OutfitSet> ApplyFavoriteFilter(IEnumerable<OutfitSet> sets, bool favoritesOnly)
            => OutfitSetFiltering.ApplyFavoriteFilter(sets, favoritesOnly, favoriteIds);

        private IEnumerable<OutfitSet> ApplyValidityFilter(IEnumerable<OutfitSet> sets, bool showInvalid)
            => OutfitSetFiltering.ApplyValidityFilter(sets, showInvalid, validIds);

        private IEnumerable<OutfitSet> ApplyInvalidOnlyFilter(IEnumerable<OutfitSet> sets, bool invalidOnly)
            => OutfitSetFiltering.ApplyInvalidOnlyFilter(sets, invalidOnly, validIds);

        private IEnumerable<OutfitSet> ApplySearchFilter(IEnumerable<OutfitSet> sets, string searchText, SearchScope scope)
            => OutfitSetFiltering.ApplySearchFilter(sets, searchText, scope, setSearchText, setItemSearchText);

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
            OnSetsChanged?.Invoke();
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

            OnSetsChanged?.Invoke();
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

            OnSetsChanged?.Invoke();
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
            OnSetsChanged?.Invoke();
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
            OnSetsChanged?.Invoke();
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

        public static bool IsHairIdValid(int? hairId)
        {
            if (!hairId.HasValue)
                return true;

            if (Farmer.GetHairStyleMetadata(hairId.Value) != null)
                return true;

            // Vanilla fallback: check if hairId is within the vanilla hair texture bounds
            var texture = FarmerRenderer.hairStylesTexture;
            if (texture == null)
                return false;

            int maxVanillaHairs = (texture.Width / 16) * (texture.Height / 96);
            return hairId.Value >= 0 && hairId.Value < maxVanillaHairs;
        }

        public OutfitSet CreateFromCurrentOutfit(string name, List<string> tags, bool isFavorite, bool isGlobal,
            string? shirtId = null, string? pantsId = null, string? hatId = null,
            string? shirtColor = null, string? pantsColor = null,
            int? hairId = null, string? hairColor = null,
            bool useCurrentOutfit = true)
        {
            var set = new OutfitSet
            {
                Name = name,
                Tags = tags,
                IsFavorite = isFavorite,
                IsGlobal = isGlobal,
                ShirtId = useCurrentOutfit ? OutfitState.GetClothingId(Game1.player.shirtItem.Value) : shirtId,
                PantsId = useCurrentOutfit ? OutfitState.GetClothingId(Game1.player.pantsItem.Value) : pantsId,
                HatId = useCurrentOutfit ? OutfitState.GetHatIdFromItem(Game1.player.hat.Value) : hatId,
                ShirtColor = shirtColor,
                PantsColor = pantsColor,
                HairId = useCurrentOutfit ? Game1.player.hair.Value : hairId,
                HairColor = hairColor
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

            if (set.ShirtColor != null && Game1.player.shirtItem.Value != null)
            {
                var color = ColorHelper.ParseColor(set.ShirtColor);
                if (color.HasValue)
                {
                    Game1.player.shirtItem.Value.clothesColor.Set(color.Value);
                    Game1.player.FarmerRenderer.MarkSpriteDirty();
                }
            }
            if (set.PantsColor != null && Game1.player.pantsItem.Value != null)
            {
                var color = ColorHelper.ParseColor(set.PantsColor);
                if (color.HasValue)
                {
                    Game1.player.changePantsColor(color.Value);
                }
            }

            if (ModEntry.Config.IncludeHairInOutfitSets && set.HairId.HasValue && IsHairIdValid(set.HairId))
            {
                OutfitState.ApplyHair(set.HairId.Value);
                if (set.HairColor != null)
                {
                    var color = ColorHelper.ParseColor(set.HairColor);
                    if (color.HasValue)
                        Game1.player.changeHairColor(color.Value);
                }
            }
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
            bool hairValid = IsHairIdValid(set.HairId);

            set.IsValid = shirtValid && pantsValid && hatValid && hairValid;

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
