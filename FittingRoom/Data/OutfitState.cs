using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Objects;

namespace FittingRoom
{
    /// <summary>
    /// Tracks current selection, original outfit (for closing menu), and applied outfit (for Reset button).
    /// </summary>
    public class OutfitState
    {
        private int shirtIndex;
        private int pantsIndex;
        private int hatIndex;

        // Original outfit - captured when menu opens, for reverting on close
        private readonly string originalShirt;
        private readonly string originalPants;
        private readonly string originalHat;

        // Applied outfit - set when Apply is clicked, for Reset button
        private string appliedShirt;
        private string appliedPants;
        private string appliedHat;

        private int scrollOffset = 0;

        // Per-category state (null = no filter/search active)
        private readonly Dictionary<OutfitCategoryManager.Category, string?> modFilters = new();
        private readonly Dictionary<OutfitCategoryManager.Category, string?> searchTexts = new();

        public int ShirtIndex
        {
            get => shirtIndex;
            set => shirtIndex = Math.Max(0, value);
        }

        public int PantsIndex
        {
            get => pantsIndex;
            set => pantsIndex = Math.Max(0, value);
        }

        public int HatIndex
        {
            get => hatIndex;
            set => hatIndex = Math.Max(0, value);
        }

        public int ScrollOffset
        {
            get => scrollOffset;
            set => scrollOffset = Math.Max(0, value);
        }

        public string? GetModFilter(OutfitCategoryManager.Category category)
        {
            return modFilters.TryGetValue(category, out var filter) ? filter : null;
        }

        public void SetModFilter(OutfitCategoryManager.Category category, string? filter)
        {
            modFilters[category] = filter;
        }

        public string? GetSearchText(OutfitCategoryManager.Category category)
        {
            return searchTexts.TryGetValue(category, out var text) ? text : null;
        }

        public void SetSearchText(OutfitCategoryManager.Category category, string? text)
        {
            searchTexts[category] = text;
        }

        public string OriginalShirt => originalShirt;
        public string OriginalPants => originalPants;
        public string OriginalHat => originalHat;

        public OutfitState()
        {
            originalShirt = Game1.player.shirt.Value;
            originalPants = Game1.player.pants.Value;
            originalHat = GetHatIdFromItem(Game1.player.hat.Value);

            // Applied starts as the original outfit
            appliedShirt = originalShirt;
            appliedPants = originalPants;
            appliedHat = originalHat;

            // Indices will be set by menu to match current outfit
            shirtIndex = 0;
            pantsIndex = 0;
            hatIndex = 0;
        }

        public void ApplySelection(OutfitCategoryManager.Category category,
            System.Collections.Generic.List<string> shirtIds,
            System.Collections.Generic.List<string> pantsIds,
            System.Collections.Generic.List<string> hatIds)
        {
            switch (category)
            {
                case OutfitCategoryManager.Category.Shirts:
                    if (shirtIndex >= 0 && shirtIndex < shirtIds.Count)
                    {
                        Game1.player.shirt.Value = shirtIds[shirtIndex];
                        Game1.player.FarmerRenderer.MarkSpriteDirty();
                    }
                    break;

                case OutfitCategoryManager.Category.Pants:
                    if (pantsIndex >= 0 && pantsIndex < pantsIds.Count)
                    {
                        Game1.player.pants.Value = pantsIds[pantsIndex];
                        Game1.player.FarmerRenderer.MarkSpriteDirty();
                    }
                    break;

                case OutfitCategoryManager.Category.Hats:
                    if (hatIndex >= 0 && hatIndex < hatIds.Count)
                    {
                        string hatId = hatIds[hatIndex];
                        if (string.IsNullOrEmpty(hatId) || hatId == OutfitLayoutConstants.NoHatId)
                            Game1.player.hat.Value = null;
                        else
                            Game1.player.hat.Value = ItemRegistry.Create<Hat>("(H)" + hatId);
                    }
                    break;
            }
        }

        public void SaveAppliedOutfit()
        {
            appliedShirt = Game1.player.shirt.Value;
            appliedPants = Game1.player.pants.Value;
            appliedHat = GetHatIdFromItem(Game1.player.hat.Value);
        }

        // Resets player's outfit and indices to last applied outfit
        public void ResetToApplied(System.Collections.Generic.List<string> shirtIds,
            System.Collections.Generic.List<string> pantsIds,
            System.Collections.Generic.List<string> hatIds)
        {
            Game1.player.shirt.Value = appliedShirt;
            Game1.player.pants.Value = appliedPants;

            if (string.IsNullOrEmpty(appliedHat) || appliedHat == OutfitLayoutConstants.NoHatId)
                Game1.player.hat.Value = null;
            else
                Game1.player.hat.Value = ItemRegistry.Create<Hat>("(H)" + appliedHat);

            Game1.player.FarmerRenderer.MarkSpriteDirty();

            shirtIndex = Math.Max(0, shirtIds.IndexOf(appliedShirt));
            pantsIndex = Math.Max(0, pantsIds.IndexOf(appliedPants));
            hatIndex = Math.Max(0, hatIds.IndexOf(appliedHat));
        }

        // Reverts player's outfit to applied state without changing indices (for closing menu)
        public void RevertToApplied()
        {
            Game1.player.shirt.Value = appliedShirt;
            Game1.player.pants.Value = appliedPants;

            if (string.IsNullOrEmpty(appliedHat) || appliedHat == OutfitLayoutConstants.NoHatId)
                Game1.player.hat.Value = null;
            else
                Game1.player.hat.Value = ItemRegistry.Create<Hat>("(H)" + appliedHat);

            Game1.player.FarmerRenderer.MarkSpriteDirty();
        }

        public int GetCurrentIndex(OutfitCategoryManager.Category category)
        {
            return category switch
            {
                OutfitCategoryManager.Category.Shirts => shirtIndex,
                OutfitCategoryManager.Category.Pants => pantsIndex,
                OutfitCategoryManager.Category.Hats => hatIndex,
                _ => 0
            };
        }

        public void SetCurrentIndex(OutfitCategoryManager.Category category, int index)
        {
            switch (category)
            {
                case OutfitCategoryManager.Category.Shirts:
                    shirtIndex = index;
                    break;
                case OutfitCategoryManager.Category.Pants:
                    pantsIndex = index;
                    break;
                case OutfitCategoryManager.Category.Hats:
                    hatIndex = index;
                    break;
            }
        }

        // Extracts unqualified ID from Hat item (returns NoHatId for no hat)
        public static string GetHatIdFromItem(Hat hat)
        {
            if (hat == null)
                return OutfitLayoutConstants.NoHatId;

            string itemId = hat.ItemId;
            if (string.IsNullOrEmpty(itemId))
                return OutfitLayoutConstants.NoHatId;

            // Remove "(H)" prefix if present
            if (itemId.StartsWith("(H)"))
                return itemId.Substring(3);

            return itemId;
        }
    }
}
