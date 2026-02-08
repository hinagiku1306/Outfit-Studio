using StardewValley;
using StardewValley.Objects;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    public static class ItemIdHelper
    {
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

        public static string GetUnqualifiedId(string qualifiedId)
        {
            if (string.IsNullOrEmpty(qualifiedId))
                return qualifiedId;

            if (qualifiedId.StartsWith("(S)") || qualifiedId.StartsWith("(P)") || qualifiedId.StartsWith("(H)"))
                return qualifiedId.Substring(3);

            return qualifiedId;
        }

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

        public static bool IsNoHatId(string? itemId)
        {
            return string.IsNullOrEmpty(itemId) || itemId == NoHatId;
        }

        public static bool IsNoShirtId(string? itemId)
        {
            return string.IsNullOrEmpty(itemId) || itemId == NoShirtId;
        }

        public static bool IsNoPantsId(string? itemId)
        {
            return string.IsNullOrEmpty(itemId) || itemId == NoPantsId;
        }

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