using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using StardewModdingAPI;
using StardewValley;

namespace OutfitStudio
{
    public class OutfitFilterManager
    {
        private readonly ModDetectionService detectionService;
        private readonly FilterCacheService cacheService;

        public OutfitFilterManager(IMonitor monitor, IModHelper? modHelper = null)
        {
            detectionService = new ModDetectionService(monitor, modHelper);
            cacheService = new FilterCacheService(detectionService);
        }

        public void BuildModMapping(List<string> shirtIds, List<string> pantsIds, List<string> hatIds)
        {
            detectionService.BuildModMapping(shirtIds, pantsIds, hatIds);
        }

        public string DetectModName(string qualifiedId, string unqualifiedId)
        {
            return detectionService.DetectModName(qualifiedId, unqualifiedId);
        }

        public string GetModNameForItem(string itemId)
        {
            return detectionService.GetModNameForItem(itemId);
        }

        public string GetModNameForHat(string hatId)
        {
            return detectionService.GetModNameForHat(hatId);
        }

        public List<string> GetUniqueModsForCategory(OutfitCategoryManager.Category category,
            List<string> shirtIds, List<string> pantsIds, List<string> hatIds)
        {
            return detectionService.GetUniqueModsForCategory(category, shirtIds, pantsIds, hatIds);
        }

        public List<string> GetFilteredShirtIds(List<string> shirtIds, string? modFilter)
        {
            return cacheService.GetFilteredShirtIds(shirtIds, modFilter);
        }

        public List<string> GetFilteredPantsIds(List<string> pantsIds, string? modFilter)
        {
            return cacheService.GetFilteredPantsIds(pantsIds, modFilter);
        }

        public List<string> GetFilteredHatIds(List<string> hatIds, string? modFilter)
        {
            return cacheService.GetFilteredHatIds(hatIds, modFilter);
        }

        public List<string> GetSearchFilteredShirtIds(List<string> shirtIds, string? searchText) =>
            cacheService.GetSearchFilteredShirtIds(shirtIds, searchText);

        public List<string> GetSearchFilteredPantsIds(List<string> pantsIds, string? searchText) =>
            cacheService.GetSearchFilteredPantsIds(pantsIds, searchText);

        public List<string> GetSearchFilteredHatIds(List<string> hatIds, string? searchText) =>
            cacheService.GetSearchFilteredHatIds(hatIds, searchText);

        public List<string> GetFilteredAndSearchedShirtIds(List<string> shirtIds, string? modFilter, string? searchText)
        {
            return cacheService.GetFilteredAndSearchedShirtIds(shirtIds, modFilter, searchText);
        }

        public List<string> GetFilteredAndSearchedPantsIds(List<string> pantsIds, string? modFilter, string? searchText)
        {
            return cacheService.GetFilteredAndSearchedPantsIds(pantsIds, modFilter, searchText);
        }

        public List<string> GetFilteredAndSearchedHatIds(List<string> hatIds, string? modFilter, string? searchText)
        {
            return cacheService.GetFilteredAndSearchedHatIds(hatIds, modFilter, searchText);
        }

        public int GetFilteredListCount(OutfitCategoryManager.Category category,
            List<string> shirtIds, List<string> pantsIds, List<string> hatIds, string? modFilter)
        {
            return cacheService.GetFilteredListCount(category, shirtIds, pantsIds, hatIds, modFilter);
        }

        public void ClearSearchCaches()
        {
            cacheService.ClearSearchCaches();
        }
    }
}
