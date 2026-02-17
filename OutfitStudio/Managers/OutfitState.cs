using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.Pants;
using StardewValley.GameData.Shirts;
using StardewValley.Objects;

namespace OutfitStudio
{
    public class OutfitState
    {
        private int shirtIndex;
        private int pantsIndex;
        private int hatIndex;
        private int hairIndex;

        // Captured when menu opens — used to revert on close
        private readonly string originalShirt;
        private readonly string originalPants;
        private readonly string originalHat;
        private readonly int originalHair;
        private readonly Color originalHairColor;

        // Set when Apply is clicked — used for Reset button
        private string appliedShirt;
        private string appliedPants;
        private string appliedHat;
        private int appliedHair;
        private Color appliedHairColor;

        private readonly Color originalPantsColor;
        private readonly Color originalShirtColor;
        private Color appliedPantsColor;
        private Color appliedShirtColor;

        private int scrollOffset = 0;

        private readonly Dictionary<OutfitCategoryManager.Category, string?> modFilters = new();
        private readonly Dictionary<OutfitCategoryManager.Category, string?> searchTexts = new();

        public bool HideHatInPreview { get; set; }

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

        public int HairIndex
        {
            get => hairIndex;
            set => hairIndex = Math.Max(0, value);
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

        public string AppliedShirt => appliedShirt;
        public string AppliedPants => appliedPants;
        public string AppliedHat => appliedHat;
        public int AppliedHair => appliedHair;
        public Color AppliedHairColor => appliedHairColor;

        public bool IsShirtUnsaved => GetClothingId(Game1.player.shirtItem.Value) != appliedShirt;
        public bool IsPantsUnsaved => GetClothingId(Game1.player.pantsItem.Value) != appliedPants;
        public bool IsHatUnsaved => GetHatIdFromItem(Game1.player.hat.Value) != appliedHat;
        public bool IsHairUnsaved => Game1.player.hair.Value != appliedHair;

        public OutfitState()
        {
            originalShirt = GetClothingId(Game1.player.shirtItem.Value);
            originalPants = GetClothingId(Game1.player.pantsItem.Value);
            originalHat = GetHatIdFromItem(Game1.player.hat.Value);
            originalHair = Game1.player.hair.Value;
            originalHairColor = Game1.player.hairstyleColor.Value;

            appliedShirt = originalShirt;
            appliedPants = originalPants;
            appliedHat = originalHat;
            appliedHair = originalHair;
            appliedHairColor = originalHairColor;

            originalPantsColor = Game1.player.GetPantsColor();
            originalShirtColor = Game1.player.GetShirtColor();
            appliedPantsColor = originalPantsColor;
            appliedShirtColor = originalShirtColor;

            shirtIndex = 0;
            pantsIndex = 0;
            hatIndex = 0;
            hairIndex = 0;
        }

        public void ApplySelection(OutfitCategoryManager.Category category,
            System.Collections.Generic.List<string> shirtIds,
            System.Collections.Generic.List<string> pantsIds,
            System.Collections.Generic.List<string> hatIds,
            System.Collections.Generic.List<int>? hairIds = null)
        {
            switch (category)
            {
                case OutfitCategoryManager.Category.Shirts:
                    if (shirtIndex >= 0 && shirtIndex < shirtIds.Count)
                    {
                        string shirtId = shirtIds[shirtIndex];
                        Game1.player.shirtItem.Value = ItemRegistry.Create<Clothing>("(S)" + shirtId);
                        Game1.player.FarmerRenderer.MarkSpriteDirty();
                    }
                    break;

                case OutfitCategoryManager.Category.Pants:
                    if (pantsIndex >= 0 && pantsIndex < pantsIds.Count)
                    {
                        string pantsId = pantsIds[pantsIndex];
                        Game1.player.pantsItem.Value = ItemRegistry.Create<Clothing>("(P)" + pantsId);
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

                case OutfitCategoryManager.Category.Hair:
                    if (hairIds != null && hairIndex >= 0 && hairIndex < hairIds.Count)
                    {
                        Game1.player.changeHairStyle(hairIds[hairIndex]);
                        Game1.player.FarmerRenderer.MarkSpriteDirty();
                    }
                    break;
            }
        }

        public Color AppliedPantsColor => appliedPantsColor;
        public Color AppliedShirtColor => appliedShirtColor;

        public void SaveAppliedOutfit()
        {
            appliedShirt = GetClothingId(Game1.player.shirtItem.Value);
            appliedPants = GetClothingId(Game1.player.pantsItem.Value);
            appliedHat = GetHatIdFromItem(Game1.player.hat.Value);
            appliedHair = Game1.player.hair.Value;
            appliedPantsColor = Game1.player.GetPantsColor();
            appliedShirtColor = Game1.player.GetShirtColor();
            appliedHairColor = Game1.player.hairstyleColor.Value;
        }

        public void ResetToApplied(System.Collections.Generic.List<string> shirtIds,
            System.Collections.Generic.List<string> pantsIds,
            System.Collections.Generic.List<string> hatIds,
            System.Collections.Generic.List<int>? hairIds = null)
        {
            if (string.IsNullOrEmpty(appliedShirt) || appliedShirt == OutfitLayoutConstants.NoShirtId)
                Game1.player.shirtItem.Value = null;
            else
                Game1.player.shirtItem.Value = ItemRegistry.Create<Clothing>("(S)" + appliedShirt);

            if (string.IsNullOrEmpty(appliedPants) || appliedPants == OutfitLayoutConstants.NoPantsId)
                Game1.player.pantsItem.Value = null;
            else
                Game1.player.pantsItem.Value = ItemRegistry.Create<Clothing>("(P)" + appliedPants);

            if (string.IsNullOrEmpty(appliedHat) || appliedHat == OutfitLayoutConstants.NoHatId)
                Game1.player.hat.Value = null;
            else
                Game1.player.hat.Value = ItemRegistry.Create<Hat>("(H)" + appliedHat);

            Game1.player.changeHairStyle(appliedHair);
            Game1.player.changeHairColor(appliedHairColor);
            Game1.player.changePantsColor(appliedPantsColor);
            if (Game1.player.shirtItem.Value != null)
                Game1.player.shirtItem.Value.clothesColor.Set(appliedShirtColor);

            Game1.player.FarmerRenderer.MarkSpriteDirty();

            shirtIndex = Math.Max(0, shirtIds.IndexOf(appliedShirt));
            pantsIndex = Math.Max(0, pantsIds.IndexOf(appliedPants));
            hatIndex = Math.Max(0, hatIds.IndexOf(appliedHat));
            hairIndex = hairIds != null ? Math.Max(0, hairIds.IndexOf(appliedHair)) : 0;
        }

        public void RevertToApplied()
        {
            if (string.IsNullOrEmpty(appliedShirt) || appliedShirt == OutfitLayoutConstants.NoShirtId)
                Game1.player.shirtItem.Value = null;
            else
                Game1.player.shirtItem.Value = ItemRegistry.Create<Clothing>("(S)" + appliedShirt);

            if (string.IsNullOrEmpty(appliedPants) || appliedPants == OutfitLayoutConstants.NoPantsId)
                Game1.player.pantsItem.Value = null;
            else
                Game1.player.pantsItem.Value = ItemRegistry.Create<Clothing>("(P)" + appliedPants);

            if (string.IsNullOrEmpty(appliedHat) || appliedHat == OutfitLayoutConstants.NoHatId)
                Game1.player.hat.Value = null;
            else
                Game1.player.hat.Value = ItemRegistry.Create<Hat>("(H)" + appliedHat);

            Game1.player.changeHairStyle(appliedHair);
            Game1.player.changeHairColor(appliedHairColor);
            Game1.player.changePantsColor(appliedPantsColor);
            if (Game1.player.shirtItem.Value != null)
                Game1.player.shirtItem.Value.clothesColor.Set(appliedShirtColor);

            Game1.player.FarmerRenderer.MarkSpriteDirty();
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
                case OutfitCategoryManager.Category.Hair:
                    hairIndex = index;
                    break;
            }
        }

        public static string GetHatIdFromItem(Hat? hat)
        {
            return ItemIdHelper.GetHatIdFromItem(hat);
        }

        public static string GetClothingId(Clothing? clothing)
        {
            return ItemIdHelper.GetClothingIdFromItem(clothing);
        }

        public static void ApplyShirt(string shirtId)
        {
            if (string.IsNullOrEmpty(shirtId) || shirtId == OutfitLayoutConstants.NoShirtId)
                Game1.player.shirtItem.Value = null;
            else
                Game1.player.shirtItem.Value = ItemRegistry.Create<Clothing>("(S)" + shirtId);
            Game1.player.FarmerRenderer.MarkSpriteDirty();
        }

        public static void ApplyPants(string pantsId)
        {
            if (string.IsNullOrEmpty(pantsId) || pantsId == OutfitLayoutConstants.NoPantsId)
                Game1.player.pantsItem.Value = null;
            else
                Game1.player.pantsItem.Value = ItemRegistry.Create<Clothing>("(P)" + pantsId);
            Game1.player.FarmerRenderer.MarkSpriteDirty();
        }

        public static void ApplyHat(string hatId)
        {
            if (string.IsNullOrEmpty(hatId) || hatId == OutfitLayoutConstants.NoHatId)
                Game1.player.hat.Value = null;
            else
                Game1.player.hat.Value = ItemRegistry.Create<Hat>("(H)" + hatId);
            Game1.player.FarmerRenderer.MarkSpriteDirty();
        }

        public static void ApplyHair(int hairId)
        {
            Game1.player.changeHairStyle(hairId);
            Game1.player.FarmerRenderer.MarkSpriteDirty();
        }
    }
}
