using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley;

namespace OutfitStudio
{
    public class FilterCacheService
    {
        private static readonly HashSet<string> VanillaHairTextureNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "hairstyles", "hairstyles2"
        };

        private readonly ModDetectionService detectionService;
        private readonly Dictionary<string, List<string>> cachedFilteredShirts = new();
        private readonly Dictionary<string, List<string>> cachedFilteredPants = new();
        private readonly Dictionary<string, List<string>> cachedFilteredHats = new();
        private readonly Dictionary<string, List<int>> cachedFilteredHairs = new();
        private readonly Dictionary<string, List<string>> cachedCombinedShirts = new();
        private readonly Dictionary<string, List<string>> cachedCombinedPants = new();
        private readonly Dictionary<string, List<string>> cachedCombinedHats = new();
        private readonly Dictionary<string, List<int>> cachedCombinedHairs = new();
        private readonly Dictionary<string, string> displayNameCache = new();

        public FilterCacheService(ModDetectionService detectionService)
        {
            this.detectionService = detectionService ?? throw new ArgumentNullException(nameof(detectionService));
        }

        public List<string> GetFilteredShirtIds(List<string> shirtIds, string? modFilter)
        {
            if (string.IsNullOrEmpty(modFilter))
                return shirtIds;

            if (cachedFilteredShirts.TryGetValue(modFilter, out var cached))
                return cached;

            var filtered = new List<string>();
            foreach (var id in shirtIds)
            {
                if (detectionService.GetModNameForItem(id) == modFilter)
                    filtered.Add(id);
            }

            cachedFilteredShirts[modFilter] = filtered;
            return filtered;
        }

        public List<string> GetFilteredPantsIds(List<string> pantsIds, string? modFilter)
        {
            if (string.IsNullOrEmpty(modFilter))
                return pantsIds;

            if (cachedFilteredPants.TryGetValue(modFilter, out var cached))
                return cached;

            var filtered = new List<string>();
            foreach (var id in pantsIds)
            {
                if (detectionService.GetModNameForItem(id) == modFilter)
                    filtered.Add(id);
            }

            cachedFilteredPants[modFilter] = filtered;
            return filtered;
        }

        public List<string> GetFilteredHatIds(List<string> hatIds, string? modFilter)
        {
            if (string.IsNullOrEmpty(modFilter))
                return hatIds;

            if (cachedFilteredHats.TryGetValue(modFilter, out var cached))
                return cached;

            var filtered = new List<string>();
            foreach (var hatId in hatIds)
            {
                if (hatId == OutfitLayoutConstants.NoHatId || detectionService.GetModNameForHat(hatId) == modFilter)
                    filtered.Add(hatId);
            }

            cachedFilteredHats[modFilter] = filtered;
            return filtered;
        }

        public List<int> GetFilteredHairIds(List<int> hairIds, string? filter)
        {
            if (string.IsNullOrEmpty(filter))
                return hairIds;

            if (cachedFilteredHairs.TryGetValue(filter, out var cached))
                return cached;

            var dataFile = Farmer.GetHairStyleMetadataFile();
            var filtered = new List<int>();
            foreach (int id in hairIds)
            {
                dataFile.TryGetValue(id, out var rawData);
                bool isModded = IsModdedHairEntry(rawData);
                if ((filter == TranslationCache.FilterVanilla && !isModded) ||
                    (filter == TranslationCache.FilterModded && isModded))
                    filtered.Add(id);
            }

            cachedFilteredHairs[filter] = filtered;
            return filtered;
        }

        internal static bool IsModdedHairEntry(string? rawHairData)
        {
            if (rawHairData == null) return false;
            string textureName = rawHairData.Split('/')[0];
            return !VanillaHairTextureNames.Contains(textureName);
        }

        public List<int> GetSearchFilteredHairIds(List<int> hairIds, string? searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return hairIds;

            var filtered = new List<int>();
            foreach (int id in hairIds)
            {
                if (id.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    filtered.Add(id);
            }
            return filtered;
        }

        public List<int> GetFilteredAndSearchedHairIds(List<int> hairIds, string? filter, string? searchText)
        {
            string cacheKey = $"{filter ?? "All"}::{searchText ?? ""}";
            if (cachedCombinedHairs.TryGetValue(cacheKey, out var cached))
                return cached;

            var modFiltered = GetFilteredHairIds(hairIds, filter);
            var result = GetSearchFilteredHairIds(modFiltered, searchText);
            cachedCombinedHairs[cacheKey] = result;
            return result;
        }

        private string GetCachedDisplayName(string qualifiedId)
        {
            if (displayNameCache.TryGetValue(qualifiedId, out var name))
                return name;

            var data = ItemRegistry.GetDataOrErrorItem(qualifiedId);
            name = data?.DisplayName ?? "";
            displayNameCache[qualifiedId] = name;
            return name;
        }

        private List<string> SearchItemsByDisplayName(List<string> itemIds, string? searchText, string itemTypePrefix)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return itemIds;

            var filtered = new List<string>();
            foreach (var id in itemIds)
            {
                if (id == OutfitLayoutConstants.NoHatId)
                {
                    if (TranslationCache.ItemNoHat.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        filtered.Add(id);
                    continue;
                }

                var displayName = GetCachedDisplayName($"{itemTypePrefix}{id}");
                if (displayName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    filtered.Add(id);
            }

            return filtered;
        }

        public List<string> GetSearchFilteredShirtIds(List<string> shirtIds, string? searchText) =>
            SearchItemsByDisplayName(shirtIds, searchText, "(S)");

        public List<string> GetSearchFilteredPantsIds(List<string> pantsIds, string? searchText) =>
            SearchItemsByDisplayName(pantsIds, searchText, "(P)");

        public List<string> GetSearchFilteredHatIds(List<string> hatIds, string? searchText) =>
            SearchItemsByDisplayName(hatIds, searchText, "(H)");

        public List<string> GetFilteredAndSearchedShirtIds(List<string> shirtIds, string? modFilter, string? searchText)
        {
            string cacheKey = $"{modFilter ?? "All"}::{searchText ?? ""}";
            if (cachedCombinedShirts.TryGetValue(cacheKey, out var cached))
                return cached;

            var modFiltered = GetFilteredShirtIds(shirtIds, modFilter);
            var result = GetSearchFilteredShirtIds(modFiltered, searchText);
            cachedCombinedShirts[cacheKey] = result;
            return result;
        }

        public List<string> GetFilteredAndSearchedPantsIds(List<string> pantsIds, string? modFilter, string? searchText)
        {
            string cacheKey = $"{modFilter ?? "All"}::{searchText ?? ""}";
            if (cachedCombinedPants.TryGetValue(cacheKey, out var cached))
                return cached;

            var modFiltered = GetFilteredPantsIds(pantsIds, modFilter);
            var result = GetSearchFilteredPantsIds(modFiltered, searchText);
            cachedCombinedPants[cacheKey] = result;
            return result;
        }

        public List<string> GetFilteredAndSearchedHatIds(List<string> hatIds, string? modFilter, string? searchText)
        {
            string cacheKey = $"{modFilter ?? "All"}::{searchText ?? ""}";
            if (cachedCombinedHats.TryGetValue(cacheKey, out var cached))
                return cached;

            var modFiltered = GetFilteredHatIds(hatIds, modFilter);
            var result = GetSearchFilteredHatIds(modFiltered, searchText);
            cachedCombinedHats[cacheKey] = result;
            return result;
        }

        public int GetFilteredListCount(OutfitCategoryManager.Category category,
            List<string> shirtIds, List<string> pantsIds, List<string> hatIds, string? modFilter)
        {
            if (string.IsNullOrEmpty(modFilter))
            {
                return category switch
                {
                    OutfitCategoryManager.Category.All => shirtIds.Count + pantsIds.Count + hatIds.Count,
                    OutfitCategoryManager.Category.Shirts => shirtIds.Count,
                    OutfitCategoryManager.Category.Pants => pantsIds.Count,
                    OutfitCategoryManager.Category.Hats => hatIds.Count,
                    _ => 0
                };
            }

            return category switch
            {
                OutfitCategoryManager.Category.All => GetFilteredShirtIds(shirtIds, modFilter).Count +
                                                       GetFilteredPantsIds(pantsIds, modFilter).Count +
                                                       GetFilteredHatIds(hatIds, modFilter).Count,
                OutfitCategoryManager.Category.Shirts => GetFilteredShirtIds(shirtIds, modFilter).Count,
                OutfitCategoryManager.Category.Pants => GetFilteredPantsIds(pantsIds, modFilter).Count,
                OutfitCategoryManager.Category.Hats => GetFilteredHatIds(hatIds, modFilter).Count,
                _ => 0
            };
        }

        public void ClearSearchCaches()
        {
            cachedCombinedShirts.Clear();
            cachedCombinedPants.Clear();
            cachedCombinedHats.Clear();
            cachedCombinedHairs.Clear();
            displayNameCache.Clear();
        }
    }
}