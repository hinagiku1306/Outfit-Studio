using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using StardewModdingAPI;
using StardewValley;

namespace FittingRoom
{
    /// <summary>
    /// Manages filtering and mod detection for outfit items.
    /// </summary>
    public class OutfitFilterManager
    {
        /// <summary>SMAPI monitor for logging.</summary>
        private readonly IMonitor monitor;

        /// <summary>SMAPI mod helper for accessing ModRegistry.</summary>
        private readonly IModHelper? modHelper;

        /// <summary>Maps item IDs to their source mod names.</summary>
        private readonly Dictionary<string, string> itemIdToModName = new();

        /// <summary>Maps mod prefixes/UniqueIDs to their friendly display names.</summary>
        private readonly Dictionary<string, string> modPrefixToName = new();

        /// <summary>Cached filtered lists to avoid rebuilding every frame.</summary>
        private readonly Dictionary<string, List<string>> cachedFilteredShirts = new();
        private readonly Dictionary<string, List<string>> cachedFilteredPants = new();
        private readonly Dictionary<string, List<string>> cachedFilteredHats = new();

        /// <summary>
        /// Initialize the filter manager.
        /// </summary>
        /// <param name="monitor">SMAPI monitor for logging.</param>
        /// <param name="modHelper">SMAPI mod helper for accessing ModRegistry (optional).</param>
        public OutfitFilterManager(IMonitor monitor, IModHelper? modHelper = null)
        {
            this.monitor = monitor;
            this.modHelper = modHelper;
        }

        /// <summary>
        /// Builds a cache of mod prefixes to friendly names from SMAPI's ModRegistry.
        /// This allows us to match item IDs to their source mods more effectively.
        /// </summary>
        private void BuildModPrefixCache()
        {
            if (modHelper == null)
                return;

            foreach (var mod in modHelper.ModRegistry.GetAll())
            {
                // Store the full UniqueID mapping only
                // Don't cache short prefixes as they can cause collisions
                // (multiple mods can have the same author.framework prefix)
                modPrefixToName[mod.Manifest.UniqueID] = mod.Manifest.Name;
            }

            DebugLogger.Log($"Built mod prefix cache with {modPrefixToName.Count} entries", LogLevel.Debug);
        }

        /// <summary>
        /// Builds a mapping of item IDs to their source mod names.
        /// </summary>
        /// <param name="shirtIds">List of shirt IDs to map.</param>
        /// <param name="pantsIds">List of pants IDs to map.</param>
        /// <param name="hatIds">List of hat IDs to map.</param>
        public void BuildModMapping(List<string> shirtIds, List<string> pantsIds, List<string> hatIds)
        {
            itemIdToModName.Clear();
            modPrefixToName.Clear();

            // Build the mod prefix cache from SMAPI registry
            BuildModPrefixCache();

            // Map shirts
            foreach (var id in shirtIds)
            {
                string modName = DetectModName("(S)" + id, id);
                itemIdToModName[id] = modName;
            }

            // Map pants
            foreach (var id in pantsIds)
            {
                string modName = DetectModName("(P)" + id, id);
                itemIdToModName[id] = modName;
            }

            // Map hats
            foreach (var hatId in hatIds)
            {
                if (hatId != "-1") // Skip "no hat" option
                {
                    string modName = DetectModName("(H)" + hatId, hatId);
                    itemIdToModName[hatId] = modName;
                }
            }

            DebugLogger.Log($"Loaded mod mapping for {itemIdToModName.Count} items", LogLevel.Debug);
        }

        /// <summary>
        /// Detects the mod name for a given item ID using intelligent prefix matching
        /// and fallback normalization.
        /// </summary>
        /// <param name="qualifiedId">The qualified item ID (e.g., "(S)0" or "(S)ModId_ItemName").</param>
        /// <param name="unqualifiedId">The unqualified ID (without prefix).</param>
        /// <returns>The detected mod name, "Vanilla", or "Unknown".</returns>
        public string DetectModName(string qualifiedId, string unqualifiedId)
        {
            try
            {
                // 1. Vanilla numeric IDs (before 1.6)
                if (int.TryParse(unqualifiedId, out _))
                {
                    return TranslationCache.FilterVanilla;
                }

                // 2. Vanilla string IDs (1.6+)
                // Modded items typically have dots (CP) or underscore prefixes (JA)
                if (!unqualifiedId.Contains('.') && !unqualifiedId.Contains('_'))
                {
                    return TranslationCache.FilterVanilla;
                }

                // 3. Try exact ModRegistry lookup first
                if (modHelper != null && modPrefixToName.TryGetValue(unqualifiedId, out string? exactMatch))
                {
                    DebugLogger.Log($"[Exact Match] ID: '{unqualifiedId}' → Filter: '{exactMatch}'", LogLevel.Trace);
                    return exactMatch;
                }

                // 4. Try prefix matching for dot-separated IDs
                if (unqualifiedId.Contains('.'))
                {
                    string[] parts = unqualifiedId.Split('.');

                    // Try matching against all registered mod UniqueIDs
                    // Look for the longest matching prefix among registered mods
                    string? bestMatch = null;
                    int bestMatchLength = 0;

                    foreach (var kvp in modPrefixToName)
                    {
                        string modId = kvp.Key;

                        // Check if the item ID starts with this mod's UniqueID
                        if (unqualifiedId.StartsWith(modId + ".", StringComparison.OrdinalIgnoreCase))
                        {
                            // Prefer longer matches
                            if (modId.Length > bestMatchLength)
                            {
                                bestMatch = kvp.Value;
                                bestMatchLength = modId.Length;
                            }
                        }
                    }

                    if (bestMatch != null)
                    {
                        DebugLogger.Log($"[Prefix Match] ID: '{unqualifiedId}' → Filter: '{bestMatch}'", LogLevel.Trace);
                        return bestMatch;
                    }

                    // Try partial prefix matching (e.g., "delloti.CP" for items that don't have full mod ID)
                    // Build a partial prefix from first 2 parts
                    if (parts.Length >= 2)
                    {
                        string partialPrefix = parts[0] + "." + parts[1];

                        // Find all mods that start with this partial prefix
                        List<KeyValuePair<string, string>> candidates = new();
                        foreach (var kvp in modPrefixToName)
                        {
                            if (kvp.Key.StartsWith(partialPrefix + ".", StringComparison.OrdinalIgnoreCase))
                            {
                                candidates.Add(kvp);
                            }
                        }

                        // If we found exactly one mod with this partial prefix, use it
                        if (candidates.Count == 1)
                        {
                            DebugLogger.Log($"[Partial Prefix Match] ID: '{unqualifiedId}' → Partial: '{partialPrefix}' → Filter: '{candidates[0].Value}'", LogLevel.Trace);
                            return candidates[0].Value;
                        }

                        // If multiple candidates, try to find best match by checking if item ID contains part of mod ID
                        if (candidates.Count > 1)
                        {
                            KeyValuePair<string, string>? thirdSegmentMatch = null;

                            foreach (var candidate in candidates)
                            {
                                // Try to match based on third segment similarity
                                string[] modIdParts = candidate.Key.Split('.');
                                if (modIdParts.Length >= 3 && parts.Length >= 3)
                                {
                                    // Normalize both parts for comparison (remove underscores, version suffixes)
                                    string normalizedItemPart = NormalizeForComparison(parts[2]);
                                    string normalizedModPart = NormalizeForComparison(modIdParts[2]);

                                    // Check if either normalized part contains the other
                                    if (normalizedItemPart.Contains(normalizedModPart, StringComparison.OrdinalIgnoreCase) ||
                                        normalizedModPart.Contains(normalizedItemPart, StringComparison.OrdinalIgnoreCase))
                                    {
                                        // Found a matching third segment - save and continue checking
                                        thirdSegmentMatch = candidate;

                                        // Check for version match to distinguish between similar mods (e.g., ver.2 vs separate)
                                        // Look for version patterns like "ver2", "v2", "ver.2" in both item and mod IDs
                                        string itemIdLower = unqualifiedId.ToLowerInvariant();
                                        string modIdLower = candidate.Key.ToLowerInvariant();

                                        // Extract version indicators from both
                                        bool itemHasVer2 = itemIdLower.Contains("ver2") || itemIdLower.Contains("_v2") || itemIdLower.Contains(".v2");
                                        bool modHasVer2 = modIdLower.Contains("ver2") || modIdLower.Contains(".ver2");

                                        // If both have ver2 or neither have ver2, this is a strong match
                                        if (itemHasVer2 == modHasVer2)
                                        {
                                            DebugLogger.Log($"[Partial Prefix Match] ID: '{unqualifiedId}' → Matched: '{candidate.Key}' ('{normalizedItemPart}' ~ '{normalizedModPart}', version match) → Filter: '{candidate.Value}'", LogLevel.Trace);
                                            return candidate.Value;
                                        }
                                    }
                                }
                            }

                            // If we found a third-segment match but no version match, use that match
                            if (thirdSegmentMatch.HasValue)
                            {
                                DebugLogger.Log($"[Partial Prefix Match] ID: '{unqualifiedId}' → Matched: '{thirdSegmentMatch.Value.Key}' (third segment match, no version preference) → Filter: '{thirdSegmentMatch.Value.Value}'", LogLevel.Trace);
                                return thirdSegmentMatch.Value.Value;
                            }

                            // No good match found, use first candidate
                            DebugLogger.Log($"[Partial Prefix Match] ID: '{unqualifiedId}' → Multiple matches, using first: '{candidates[0].Key}' → Filter: '{candidates[0].Value}'", LogLevel.Trace);
                            return candidates[0].Value;
                        }
                    }

                    // Fallback: extract and normalize parts[2] or parts[1]
                    string extracted = (parts.Length >= 3 ? parts[2] : parts[1]) ?? "";

                    // Handle Json Assets pattern (e.g., "kkeuap.allshirtspackage_ItemName")
                    // Only apply this for 2-part IDs (author.modId_ItemName), not Content Patcher's longer IDs
                    if (parts.Length == 2 && extracted.Contains('_'))
                    {
                        string[] underscoreParts = extracted.Split('_');
                        if (underscoreParts.Length >= 2)
                        {
                            string modId = underscoreParts[0];
                            string fullPrefix = parts[0] + "." + modId;
                            return HandleUnderscorePattern(modId, unqualifiedId, "JA", fullPrefix);
                        }
                    }

                    // For Content Patcher IDs, strip version suffixes but keep the full name
                    return NormalizeAndLog(extracted, "Dot Normalized", unqualifiedId, $"Extracted: '{extracted}'");
                }

                // 5. Try underscore pattern with SMAPI lookup
                if (unqualifiedId.Contains('_'))
                {
                    string[] parts = unqualifiedId.Split('_');
                    if (parts.Length >= 2)
                    {
                        return HandleUnderscorePattern(parts[0], unqualifiedId, "Underscore");
                    }
                }

                // 6. Final fallback: use "Other" category
                DebugLogger.Log($"[Final Fallback] ID: '{unqualifiedId}' → Filter: 'Other'", LogLevel.Trace);
                return "Other";
            }
            catch (Exception ex)
            {
                monitor.Log($"Error detecting mod name for {unqualifiedId}: {ex.Message}", LogLevel.Warn);
                return TranslationCache.FilterUnknown;
            }
        }

        /// <summary>
        /// Tries to look up a mod name from the ModRegistry and returns cleaned name if found.
        /// </summary>
        /// <param name="modId">The mod ID to look up.</param>
        /// <param name="context">The context for logging (e.g., "JA ModRegistry").</param>
        /// <param name="unqualifiedId">The original item ID for logging.</param>
        /// <param name="extraLogInfo">Additional info to include in log message.</param>
        /// <returns>The cleaned mod name if found, null otherwise.</returns>
        private string? TryModRegistryLookup(string modId, string context, string unqualifiedId, string? extraLogInfo = null)
        {
            if (modHelper == null) return null;

            var modInfo = modHelper.ModRegistry.Get(modId);
            if (modInfo != null)
            {
                string modName = modInfo.Manifest.Name;
                string logMsg = string.IsNullOrEmpty(extraLogInfo)
                    ? $"[{context}] ID: '{unqualifiedId}' → ModID: '{modId}' → Filter: '{modName}'"
                    : $"[{context}] ID: '{unqualifiedId}' → {extraLogInfo} → Filter: '{modName}'";
                DebugLogger.Log(logMsg, LogLevel.Trace);
                return modName;
            }
            return null;
        }

        /// <summary>
        /// Normalizes a name and logs the result.
        /// </summary>
        /// <param name="rawName">The raw name to normalize.</param>
        /// <param name="context">The context for logging.</param>
        /// <param name="unqualifiedId">The original item ID for logging.</param>
        /// <param name="extraLogInfo">Additional info to include in log message.</param>
        /// <returns>The normalized name.</returns>
        private string NormalizeAndLog(string rawName, string context, string unqualifiedId, string? extraLogInfo = null)
        {
            string detectedName = NormalizeName(rawName);
            string logMsg = string.IsNullOrEmpty(extraLogInfo)
                ? $"[{context}] ID: '{unqualifiedId}' → Filter: '{detectedName}'"
                : $"[{context}] ID: '{unqualifiedId}' → {extraLogInfo} → Filter: '{detectedName}'";
            DebugLogger.Log(logMsg, LogLevel.Trace);
            return detectedName;
        }

        /// <summary>
        /// Handles underscore-separated item IDs by trying ModRegistry lookup and normalization.
        /// </summary>
        /// <param name="modId">The extracted mod ID.</param>
        /// <param name="unqualifiedId">The full item ID.</param>
        /// <param name="context">Context prefix for logging (e.g., "JA", "Underscore").</param>
        /// <param name="fullPrefix">Optional full prefix to try as well (for Json Assets pattern).</param>
        /// <returns>The detected mod name.</returns>
        private string HandleUnderscorePattern(string modId, string unqualifiedId, string context, string? fullPrefix = null)
        {
            // Try ModRegistry lookup with the modId
            string? result = TryModRegistryLookup(modId, $"{context} ModRegistry", unqualifiedId, $"ModID: '{modId}'");
            if (result != null) return result;

            // If fullPrefix provided, try that too (for Json Assets)
            if (!string.IsNullOrEmpty(fullPrefix))
            {
                result = TryModRegistryLookup(fullPrefix, $"{context} Full Prefix", unqualifiedId, $"FullPrefix: '{fullPrefix}'");
                if (result != null) return result;
            }

            // Fallback: normalize the modId
            return NormalizeAndLog(modId, $"{context} Normalized", unqualifiedId, $"ModID: '{modId}'");
        }

        /// <summary>
        /// Strips version suffixes from names (e.g., "_ver2", ".v3", "_v1_0").
        /// </summary>
        /// <param name="name">The name to clean.</param>
        /// <returns>The name without version suffix.</returns>
        private static string StripVersionSuffix(string name)
        {
            return Regex.Replace(name, @"[._]v(?:er)?[._]?\d+(?:[._]\d+)*$", "", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Applies basic humanization to technical names (title case).
        /// </summary>
        /// <param name="technicalName">The technical name to humanize.</param>
        /// <returns>The humanized name.</returns>
        private static string HumanizeName(string technicalName)
        {
            if (string.IsNullOrWhiteSpace(technicalName) || technicalName.Length <= 2)
                return technicalName;

            // Simple title case: capitalize first letter, lowercase the rest
            return char.ToUpper(technicalName[0]) + technicalName.Substring(1).ToLower();
        }

        /// <summary>
        /// Normalizes a raw name by stripping version suffixes and applying humanization.
        /// </summary>
        /// <param name="rawName">The raw name to normalize.</param>
        /// <returns>The normalized name.</returns>
        private static string NormalizeName(string rawName)
        {
            string cleaned = StripVersionSuffix(rawName);
            return HumanizeName(cleaned);
        }

        /// <summary>
        /// Normalizes a string for comparison by removing underscores, version suffixes, and making it uppercase.
        /// Used to match similar mod/item names that differ only in formatting.
        /// </summary>
        /// <param name="text">The text to normalize.</param>
        /// <returns>The normalized text for comparison.</returns>
        private static string NormalizeForComparison(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Strip version suffixes first
            string cleaned = StripVersionSuffix(text);

            // Remove underscores and convert to uppercase for case-insensitive comparison
            return cleaned.Replace("_", "").ToUpperInvariant();
        }

        /// <summary>
        /// Cleans up mod names by removing common prefixes like [CP], (JA), etc.
        /// </summary>
        /// <param name="modName">The mod name to clean.</param>
        /// <returns>The cleaned mod name.</returns>
        private static string CleanModName(string modName)
        {
            if (string.IsNullOrWhiteSpace(modName))
                return modName;

            // Remove common mod prefixes: [CP], (CP), [JA], (JA), etc.
            string cleaned = Regex.Replace(modName, @"^[\[\(](?:CP|JA|cp|ja)[\]\)]\s*", "", RegexOptions.IgnoreCase);
            return cleaned.Trim();
        }

        /// <summary>
        /// Gets the mod name for a specific item ID.
        /// </summary>
        /// <param name="itemId">The unqualified item ID.</param>
        /// <returns>The mod name, or "Unknown" if not found.</returns>
        public string GetModNameForItem(string itemId)
        {
            if (itemIdToModName.TryGetValue(itemId, out string? modName))
            {
                return modName;
            }
            return TranslationCache.FilterUnknown;
        }

        /// <summary>
        /// Gets the mod name for a specific hat ID.
        /// </summary>
        /// <param name="hatId">The hat ID.</param>
        /// <returns>The mod name, or "Unknown" if not found.</returns>
        public string GetModNameForHat(string hatId)
        {
            return GetModNameForItem(hatId);
        }

        /// <summary>
        /// Gets a list of unique mod names that have items in the specified category.
        /// </summary>
        /// <param name="category">The category to check.</param>
        /// <param name="shirtIds">List of shirt IDs.</param>
        /// <param name="pantsIds">List of pants IDs.</param>
        /// <param name="hatIds">List of hat IDs.</param>
        /// <returns>A sorted list of unique mod names.</returns>
        public List<string> GetUniqueModsForCategory(OutfitCategoryManager.Category category,
            List<string> shirtIds, List<string> pantsIds, List<string> hatIds)
        {
            var modNames = new HashSet<string>();

            switch (category)
            {
                case OutfitCategoryManager.Category.Shirts:
                    foreach (var id in shirtIds)
                    {
                        string modName = GetModNameForItem(id);
                        modNames.Add(modName);
                    }
                    break;

                case OutfitCategoryManager.Category.Pants:
                    foreach (var id in pantsIds)
                    {
                        string modName = GetModNameForItem(id);
                        modNames.Add(modName);
                    }
                    break;

                case OutfitCategoryManager.Category.Hats:
                    foreach (var hatId in hatIds)
                    {
                        if (hatId != "-1") // Skip "no hat"
                        {
                            string modName = GetModNameForHat(hatId);
                            modNames.Add(modName);
                        }
                    }
                    break;
            }

            var sortedMods = new List<string>(modNames);
            sortedMods.Sort();
            return sortedMods;
        }

        /// <summary>
        /// Gets a filtered list of shirt IDs based on the mod filter.
        /// Uses caching to avoid rebuilding the same filtered list every frame.
        /// </summary>
        /// <param name="shirtIds">The complete list of shirt IDs.</param>
        /// <param name="modFilter">The mod name to filter by, or null for no filtering.</param>
        /// <returns>A filtered list maintaining original order.</returns>
        public List<string> GetFilteredShirtIds(List<string> shirtIds, string? modFilter)
        {
            if (string.IsNullOrEmpty(modFilter))
                return shirtIds;

            // Check cache first
            if (cachedFilteredShirts.TryGetValue(modFilter, out var cached))
                return cached;

            // Build filtered list
            var filtered = new List<string>();
            foreach (var id in shirtIds)
            {
                if (GetModNameForItem(id) == modFilter)
                {
                    filtered.Add(id);
                }
            }

            // Cache it
            cachedFilteredShirts[modFilter] = filtered;
            return filtered;
        }

        /// <summary>
        /// Gets a filtered list of pants IDs based on the mod filter.
        /// Uses caching to avoid rebuilding the same filtered list every frame.
        /// </summary>
        /// <param name="pantsIds">The complete list of pants IDs.</param>
        /// <param name="modFilter">The mod name to filter by, or null for no filtering.</param>
        /// <returns>A filtered list maintaining original order.</returns>
        public List<string> GetFilteredPantsIds(List<string> pantsIds, string? modFilter)
        {
            if (string.IsNullOrEmpty(modFilter))
                return pantsIds;

            // Check cache first
            if (cachedFilteredPants.TryGetValue(modFilter, out var cached))
                return cached;

            // Build filtered list
            var filtered = new List<string>();
            foreach (var id in pantsIds)
            {
                if (GetModNameForItem(id) == modFilter)
                {
                    filtered.Add(id);
                }
            }

            // Cache it
            cachedFilteredPants[modFilter] = filtered;
            return filtered;
        }

        /// <summary>
        /// Gets a filtered list of hat IDs based on the mod filter.
        /// Uses caching to avoid rebuilding the same filtered list every frame.
        /// </summary>
        /// <param name="hatIds">The complete list of hat IDs.</param>
        /// <param name="modFilter">The mod name to filter by, or null for no filtering.</param>
        /// <returns>A filtered list maintaining original order.</returns>
        public List<string> GetFilteredHatIds(List<string> hatIds, string? modFilter)
        {
            if (string.IsNullOrEmpty(modFilter))
                return hatIds;

            // Check cache first
            if (cachedFilteredHats.TryGetValue(modFilter, out var cached))
                return cached;

            // Build filtered list
            var filtered = new List<string>();
            foreach (var hatId in hatIds)
            {
                if (hatId == "-1" || GetModNameForHat(hatId) == modFilter)
                {
                    filtered.Add(hatId);
                }
            }

            // Cache it
            cachedFilteredHats[modFilter] = filtered;
            return filtered;
        }

        /// <summary>
        /// Gets the count of items in the current category, respecting the active filter.
        /// </summary>
        /// <param name="category">The category to check.</param>
        /// <param name="shirtIds">List of shirt IDs.</param>
        /// <param name="pantsIds">List of pants IDs.</param>
        /// <param name="hatIds">List of hat IDs.</param>
        /// <param name="modFilter">The mod filter to apply.</param>
        /// <returns>The count of filtered items.</returns>
        public int GetFilteredListCount(OutfitCategoryManager.Category category,
            List<string> shirtIds, List<string> pantsIds, List<string> hatIds, string? modFilter)
        {
            if (string.IsNullOrEmpty(modFilter))
            {
                return category switch
                {
                    OutfitCategoryManager.Category.Shirts => shirtIds.Count,
                    OutfitCategoryManager.Category.Pants => pantsIds.Count,
                    OutfitCategoryManager.Category.Hats => hatIds.Count,
                    _ => 0
                };
            }

            return category switch
            {
                OutfitCategoryManager.Category.Shirts => GetFilteredShirtIds(shirtIds, modFilter).Count,
                OutfitCategoryManager.Category.Pants => GetFilteredPantsIds(pantsIds, modFilter).Count,
                OutfitCategoryManager.Category.Hats => GetFilteredHatIds(hatIds, modFilter).Count,
                _ => 0
            };
        }
    }
}
