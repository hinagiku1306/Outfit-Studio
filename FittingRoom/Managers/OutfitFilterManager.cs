using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using StardewModdingAPI;
using StardewValley;

namespace FittingRoom
{
    /// <summary>
    /// Manages filtering and mod detection for outfit items.
    /// Delegates to ModDetectionService and FilterCacheService for separation of concerns.
    /// </summary>
    public class OutfitFilterManager
    {
        private readonly ModDetectionService detectionService;
        private readonly FilterCacheService cacheService;

        /// <summary>Pt
        /// Initialize the filter manager.
        public OutfitFilterManager(IMonitor monitor, IModHelper? modHelper = null)
        {
            detectionService = new ModDetectionService(monitor, modHelper);
            cacheService = new FilterCacheService(detectionService);
        }

        /// <summary>
        /// Builds a mapping of item IDs to their source mod names.
        /// </summary>
        public void BuildModMapping(List<string> shirtIds, List<string> pantsIds, List<string> hatIds)
        {
            detectionService.BuildModMapping(shirtIds, pantsIds, hatIds);
        }

        /// <summary>
        /// Detects the mod name for a given item ID using intelligent prefix matching
        /// and fallback normalization.
        /// </summary>
        public string DetectModName(string qualifiedId, string unqualifiedId)
        {
            return detectionService.DetectModName(qualifiedId, unqualifiedId);
        }

        /// <summary>
        /// Gets the mod name for a specific item ID.
        /// </summary>
        /// <returns>The mod name, or "Unknown" if not found.</returns>
        public string GetModNameForItem(string itemId)
        {
            return detectionService.GetModNameForItem(itemId);
        }

        /// <summary>
        /// Gets the mod name for a specific hat ID.
        /// </summary>
        /// <returns>The mod name, or "Unknown" if not found.</returns>
        public string GetModNameForHat(string hatId)
        {
            return detectionService.GetModNameForHat(hatId);
        }

        /// <summary>
        /// Gets a list of unique mod names that have items in the specified category.
        /// </summary>
        /// <returns>A sorted list of unique mod names.</returns>
        public List<string> GetUniqueModsForCategory(OutfitCategoryManager.Category category,
            List<string> shirtIds, List<string> pantsIds, List<string> hatIds)
        {
            return detectionService.GetUniqueModsForCategory(category, shirtIds, pantsIds, hatIds);
        }

        /// <summary>
        /// Gets a filtered list of shirt IDs based on the mod filter.
        /// Uses caching to avoid rebuilding the same filtered list every frame.
        /// </summary>
        /// <returns>A filtered list maintaining original order.</returns>
        public List<string> GetFilteredShirtIds(List<string> shirtIds, string? modFilter)
        {
            return cacheService.GetFilteredShirtIds(shirtIds, modFilter);
        }

        /// <summary>
        /// Gets a filtered list of pants IDs based on the mod filter.
        /// Uses caching to avoid rebuilding the same filtered list every frame.
        /// </summary>
        /// <returns>A filtered list maintaining original order.</returns>
        public List<string> GetFilteredPantsIds(List<string> pantsIds, string? modFilter)
        {
            return cacheService.GetFilteredPantsIds(pantsIds, modFilter);
        }

        /// <summary>
        /// Gets a filtered list of hat IDs based on the mod filter.
        /// Uses caching to avoid rebuilding the same filtered list every frame.
        /// </summary>
        /// <returns>A filtered list maintaining original order.</returns>
        public List<string> GetFilteredHatIds(List<string> hatIds, string? modFilter)
        {
            return cacheService.GetFilteredHatIds(hatIds, modFilter);
        }

        /// <summary>Filters shirts by search text.</summary>
        public List<string> GetSearchFilteredShirtIds(List<string> shirtIds, string? searchText) =>
            cacheService.GetSearchFilteredShirtIds(shirtIds, searchText);

        /// <summary>Filters pants by search text.</summary>
        public List<string> GetSearchFilteredPantsIds(List<string> pantsIds, string? searchText) =>
            cacheService.GetSearchFilteredPantsIds(pantsIds, searchText);

        /// <summary>Filters hats by search text.</summary>
        public List<string> GetSearchFilteredHatIds(List<string> hatIds, string? searchText) =>
            cacheService.GetSearchFilteredHatIds(hatIds, searchText);

        /// <summary>
        /// Combines mod filter and search filter for shirts (applies mod filter first, then search).
        /// </summary>
        /// <returns>A filtered list matching both criteria.</returns>
        public List<string> GetFilteredAndSearchedShirtIds(List<string> shirtIds, string? modFilter, string? searchText)
        {
            return cacheService.GetFilteredAndSearchedShirtIds(shirtIds, modFilter, searchText);
        }

        /// <summary>
        /// Combines mod filter and search filter for pants (applies mod filter first, then search).
        /// </summary>
        /// <returns>A filtered list matching both criteria.</returns>
        public List<string> GetFilteredAndSearchedPantsIds(List<string> pantsIds, string? modFilter, string? searchText)
        {
            return cacheService.GetFilteredAndSearchedPantsIds(pantsIds, modFilter, searchText);
        }

        /// <summary>
        /// Combines mod filter and search filter for hats (applies mod filter first, then search).
        /// </summary>
        /// <returns>A filtered list matching both criteria.</returns>
        public List<string> GetFilteredAndSearchedHatIds(List<string> hatIds, string? modFilter, string? searchText)
        {
            return cacheService.GetFilteredAndSearchedHatIds(hatIds, modFilter, searchText);
        }

        /// <summary>
        /// Gets the count of items in the current category, respecting the active filter.
        /// </summary>
        /// <returns>The count of filtered items.</returns>
        public int GetFilteredListCount(OutfitCategoryManager.Category category,
            List<string> shirtIds, List<string> pantsIds, List<string> hatIds, string? modFilter)
        {
            return cacheService.GetFilteredListCount(category, shirtIds, pantsIds, hatIds, modFilter);
        }

        /// <summary>
        /// Clears search-related caches to free memory.
        /// </summary>
        public void ClearSearchCaches()
        {
            cacheService.ClearSearchCaches();
        }
    }
}
