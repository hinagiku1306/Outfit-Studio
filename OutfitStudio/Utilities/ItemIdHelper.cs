using StardewValley;
using StardewValley.Objects;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    /// <summary>
    /// Static helper methods for converting between item IDs and qualified IDs.
    /// </summary>
    public static class ItemIdHelper
    {
        /// <summary>Get the qualified item ID for a given unqualified ID and category.</summary>
        /// <returns>The qualified item ID (e.g., "(S)0"), or null if itemId is null/empty.</returns>
        public static string? GetQualifiedId(string? itemId, OutfitCategoryManager.Category category)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;

            string prefix = category switch
            {
                OutfitCategoryManager.Category.Shirts => "(S)",
                OutfitCategoryManager.Category.Pants => "(P)",
                OutfitCategoryManager.Category.Hats => "(H)",
                _ => ""
            };

            return prefix + itemId;
        }

        /// <summary>Get the unqualified item ID from a qualified item ID.</summary>
        /// <returns>The unqualified ID (e.g., "0"), or the original string if it doesn't start with a known prefix.</returns>
        public static string GetUnqualifiedId(string qualifiedId)
        {
            if (string.IsNullOrEmpty(qualifiedId))
                return qualifiedId;

            if (qualifiedId.StartsWith("(S)") || qualifiedId.StartsWith("(P)") || qualifiedId.StartsWith("(H)"))
                return qualifiedId.Substring(3);

            return qualifiedId;
        }

        /// <summary>Get the item category from a qualified item ID.</summary>
        /// <returns>The category, or All if unknown.</returns>
        public static OutfitCategoryManager.Category GetCategoryFromQualifiedId(string qualifiedId)
        {
            if (qualifiedId.StartsWith("(S)"))
                return OutfitCategoryManager.Category.Shirts;
            if (qualifiedId.StartsWith("(P)"))
                return OutfitCategoryManager.Category.Pants;
            if (qualifiedId.StartsWith("(H)"))
                return OutfitCategoryManager.Category.Hats;

            return OutfitCategoryManager.Category.All;
        }

        /// <summary>Check if an item ID represents "no hat".</summary>
        public static bool IsNoHatId(string? itemId)
        {
            return string.IsNullOrEmpty(itemId) || itemId == NoHatId;
        }

        /// <summary>Check if an item ID represents "no shirt".</summary>
        public static bool IsNoShirtId(string? itemId)
        {
            return string.IsNullOrEmpty(itemId) || itemId == NoShirtId;
        }

        /// <summary>Check if an item ID represents "no pants".</summary>
        public static bool IsNoPantsId(string? itemId)
        {
            return string.IsNullOrEmpty(itemId) || itemId == NoPantsId;
        }

        /// <summary>Get the unqualified hat ID from a Hat item.</summary>
        public static string GetHatIdFromItem(Hat? hat)
        {
            if (hat == null)
                return NoHatId;

            string itemId = hat.ItemId;
            if (string.IsNullOrEmpty(itemId))
                return NoHatId;

            if (itemId.StartsWith("(H)"))
                return itemId.Substring(3);

            return itemId;
        }

        /// <summary>Get the unqualified clothing ID from a Clothing item (shirt or pants).</summary>
        public static string GetClothingIdFromItem(Clothing? clothing)
        {
            if (clothing == null)
                return NoShirtId;

            string itemId = clothing.ItemId;
            if (string.IsNullOrEmpty(itemId))
                return NoShirtId;

            if (itemId.StartsWith("(S)") || itemId.StartsWith("(P)"))
                return itemId.Substring(3);

            return itemId;
        }

        /// <summary>Get the qualified item ID for the current selection in a category.</summary>
        /// <returns>The qualified item ID, or null if index out of range or no hat.</returns>
        public static string? GetQualifiedItemId(OutfitCategoryManager.Category category, int listIndex,
            System.Collections.Generic.List<string> shirtIds,
            System.Collections.Generic.List<string> pantsIds,
            System.Collections.Generic.List<string> hatIds)
        {
            switch (category)
            {
                case OutfitCategoryManager.Category.Shirts:
                    if (listIndex >= 0 && listIndex < shirtIds.Count)
                        return "(S)" + shirtIds[listIndex];
                    break;

                case OutfitCategoryManager.Category.Pants:
                    if (listIndex >= 0 && listIndex < pantsIds.Count)
                        return "(P)" + pantsIds[listIndex];
                    break;

                case OutfitCategoryManager.Category.Hats:
                    if (listIndex >= 0 && listIndex < hatIds.Count)
                    {
                        string hatId = hatIds[listIndex];
                        if (!string.IsNullOrEmpty(hatId) && hatId != NoHatId)
                            return "(H)" + hatId;
                    }
                    break;
            }
            return null;
        }
    }
}