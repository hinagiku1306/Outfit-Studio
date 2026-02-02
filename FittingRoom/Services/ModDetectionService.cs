using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using StardewModdingAPI;
using StardewValley;

namespace FittingRoom
{
    /// <summary>
    /// Detects which mod an item belongs to, using intelligent prefix matching and fallback normalization.
    /// </summary>
    public class ModDetectionService
    {
        private readonly IMonitor monitor;
        private readonly IModHelper? modHelper;
        private readonly Dictionary<string, string> itemIdToModName = new();
        private readonly Dictionary<string, string> modPrefixToName = new();

        public ModDetectionService(IMonitor monitor, IModHelper? modHelper = null)
        {
            this.monitor = monitor;
            this.modHelper = modHelper;
        }

        /// <summary>
        /// Builds a cache of mod prefixes to friendly names from SMAPI's ModRegistry.
        /// </summary>
        private void BuildModPrefixCache()
        {
            if (modHelper == null)
                return;

            foreach (var mod in modHelper.ModRegistry.GetAll())
            {
                modPrefixToName[mod.Manifest.UniqueID] = mod.Manifest.Name;
            }

            DebugLogger.Log($"Built mod prefix cache with {modPrefixToName.Count} entries", LogLevel.Debug);
        }

        /// <summary>
        /// Builds a mapping of item IDs to their source mod names.
        /// </summary>
        public void BuildModMapping(List<string> shirtIds, List<string> pantsIds, List<string> hatIds)
        {
            itemIdToModName.Clear();
            modPrefixToName.Clear();

            BuildModPrefixCache();

            foreach (var id in shirtIds)
            {
                string modName = DetectModName("(S)" + id, id);
                itemIdToModName[id] = modName;
            }

            foreach (var id in pantsIds)
            {
                string modName = DetectModName("(P)" + id, id);
                itemIdToModName[id] = modName;
            }

            foreach (var hatId in hatIds)
            {
                if (hatId != OutfitLayoutConstants.NoHatId)
                {
                    string modName = DetectModName("(H)" + hatId, hatId);
                    itemIdToModName[hatId] = modName;
                }
            }

            DebugLogger.Log($"Loaded mod mapping for {itemIdToModName.Count} items", LogLevel.Debug);
        }

        /// <summary>
        /// Detects the mod name for a given item ID using intelligent prefix matching and fallback normalization.
        /// </summary>
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
                    string? bestMatch = null;
                    int bestMatchLength = 0;

                    foreach (var kvp in modPrefixToName)
                    {
                        string modId = kvp.Key;
                        if (unqualifiedId.StartsWith(modId + ".", StringComparison.OrdinalIgnoreCase))
                        {
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

                    // Try partial prefix matching (first 2 parts)
                    if (parts.Length >= 2)
                    {
                        string partialPrefix = parts[0] + "." + parts[1];
                        List<KeyValuePair<string, string>> candidates = new();
                        foreach (var kvp in modPrefixToName)
                        {
                            if (kvp.Key.StartsWith(partialPrefix + ".", StringComparison.OrdinalIgnoreCase))
                                candidates.Add(kvp);
                        }

                        if (candidates.Count == 1)
                        {
                            DebugLogger.Log($"[Partial Prefix Match] ID: '{unqualifiedId}' → Partial: '{partialPrefix}' → Filter: '{candidates[0].Value}'", LogLevel.Trace);
                            return candidates[0].Value;
                        }

                        if (candidates.Count > 1)
                        {
                            KeyValuePair<string, string>? thirdSegmentMatch = null;
                            foreach (var candidate in candidates)
                            {
                                string[] modIdParts = candidate.Key.Split('.');
                                if (modIdParts.Length >= 3 && parts.Length >= 3)
                                {
                                    string normalizedItemPart = NormalizeForComparison(parts[2]);
                                    string normalizedModPart = NormalizeForComparison(modIdParts[2]);

                                    if (normalizedItemPart.Contains(normalizedModPart, StringComparison.OrdinalIgnoreCase) ||
                                        normalizedModPart.Contains(normalizedItemPart, StringComparison.OrdinalIgnoreCase))
                                    {
                                        thirdSegmentMatch = candidate;

                                        string itemIdLower = unqualifiedId.ToLowerInvariant();
                                        string modIdLower = candidate.Key.ToLowerInvariant();
                                        bool itemHasVer2 = itemIdLower.Contains("ver2") || itemIdLower.Contains("_v2") || itemIdLower.Contains(".v2");
                                        bool modHasVer2 = modIdLower.Contains("ver2") || modIdLower.Contains(".ver2");

                                        if (itemHasVer2 == modHasVer2)
                                        {
                                            DebugLogger.Log($"[Partial Prefix Match] ID: '{unqualifiedId}' → Matched: '{candidate.Key}' ('{normalizedItemPart}' ~ '{normalizedModPart}', version match) → Filter: '{candidate.Value}'", LogLevel.Trace);
                                            return candidate.Value;
                                        }
                                    }
                                }
                            }

                            if (thirdSegmentMatch.HasValue)
                            {
                                DebugLogger.Log($"[Partial Prefix Match] ID: '{unqualifiedId}' → Matched: '{thirdSegmentMatch.Value.Key}' (third segment match, no version preference) → Filter: '{thirdSegmentMatch.Value.Value}'", LogLevel.Trace);
                                return thirdSegmentMatch.Value.Value;
                            }

                            DebugLogger.Log($"[Partial Prefix Match] ID: '{unqualifiedId}' → Multiple matches, using first: '{candidates[0].Key}' → Filter: '{candidates[0].Value}'", LogLevel.Trace);
                            return candidates[0].Value;
                        }
                    }

                    // Fallback: extract and normalize parts[2] or parts[1]
                    string extracted = (parts.Length >= 3 ? parts[2] : parts[1]) ?? "";
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
        private string HandleUnderscorePattern(string modId, string unqualifiedId, string context, string? fullPrefix = null)
        {
            string? result = TryModRegistryLookup(modId, $"{context} ModRegistry", unqualifiedId, $"ModID: '{modId}'");
            if (result != null) return result;

            if (!string.IsNullOrEmpty(fullPrefix))
            {
                result = TryModRegistryLookup(fullPrefix, $"{context} Full Prefix", unqualifiedId, $"FullPrefix: '{fullPrefix}'");
                if (result != null) return result;
            }

            return NormalizeAndLog(modId, $"{context} Normalized", unqualifiedId, $"ModID: '{modId}'");
        }

        /// <summary>
        /// Strips version suffixes from names (e.g., "_ver2", ".v3", "_v1_0").
        /// </summary>
        private static string StripVersionSuffix(string name)
        {
            return Regex.Replace(name, @"[._]v(?:er)?[._]?\d+(?:[._]\d+)*$", "", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Applies basic humanization to technical names (title case).
        /// </summary>
        private static string HumanizeName(string technicalName)
        {
            if (string.IsNullOrWhiteSpace(technicalName) || technicalName.Length <= 2)
                return technicalName;

            return char.ToUpper(technicalName[0]) + technicalName.Substring(1).ToLower();
        }

        /// <summary>
        /// Normalizes a raw name by stripping version suffixes and applying humanization.
        /// </summary>
        private static string NormalizeName(string rawName)
        {
            string cleaned = StripVersionSuffix(rawName);
            return HumanizeName(cleaned);
        }

        /// <summary>
        /// Normalizes a string for comparison by removing underscores, version suffixes, and making it uppercase.
        /// </summary>
        private static string NormalizeForComparison(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string cleaned = StripVersionSuffix(text);
            return cleaned.Replace("_", "").ToUpperInvariant();
        }

        /// <summary>
        /// Cleans up mod names by removing common prefixes like [CP], (JA), etc.
        /// </summary>
        private static string CleanModName(string modName)
        {
            if (string.IsNullOrWhiteSpace(modName))
                return modName;

            string cleaned = Regex.Replace(modName, @"^[\[\(](?:CP|JA|cp|ja)[\]\)]\s*", "", RegexOptions.IgnoreCase);
            return cleaned.Trim();
        }

        /// <summary>
        /// Gets the mod name for a specific item ID.
        /// </summary>
        public string GetModNameForItem(string itemId)
        {
            if (itemIdToModName.TryGetValue(itemId, out string? modName))
                return modName;

            return TranslationCache.FilterUnknown;
        }

        /// <summary>
        /// Gets the mod name for a specific hat ID.
        /// </summary>
        public string GetModNameForHat(string hatId)
        {
            return GetModNameForItem(hatId);
        }

        /// <summary>
        /// Gets a list of unique mod names that have items in the specified category.
        /// </summary>
        public List<string> GetUniqueModsForCategory(OutfitCategoryManager.Category category,
            List<string> shirtIds, List<string> pantsIds, List<string> hatIds)
        {
            var modNames = new HashSet<string>();

            switch (category)
            {
                case OutfitCategoryManager.Category.All:
                    foreach (var id in shirtIds)
                        modNames.Add(GetModNameForItem(id));
                    foreach (var id in pantsIds)
                        modNames.Add(GetModNameForItem(id));
                    foreach (var hatId in hatIds)
                    {
                        if (hatId != OutfitLayoutConstants.NoHatId)
                            modNames.Add(GetModNameForHat(hatId));
                    }
                    break;

                case OutfitCategoryManager.Category.Shirts:
                    foreach (var id in shirtIds)
                        modNames.Add(GetModNameForItem(id));
                    break;

                case OutfitCategoryManager.Category.Pants:
                    foreach (var id in pantsIds)
                        modNames.Add(GetModNameForItem(id));
                    break;

                case OutfitCategoryManager.Category.Hats:
                    foreach (var hatId in hatIds)
                    {
                        if (hatId != OutfitLayoutConstants.NoHatId)
                            modNames.Add(GetModNameForHat(hatId));
                    }
                    break;
            }

            var sortedMods = new List<string>(modNames);
            sortedMods.Sort();
            return sortedMods;
        }
    }
}