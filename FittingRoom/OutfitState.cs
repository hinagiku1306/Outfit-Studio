using System;
using StardewValley;
using StardewValley.Objects;

namespace FittingRoom
{
    /// <summary>
    /// Tracks the current outfit selection, original outfit, and saved outfit.
    /// </summary>
    public class OutfitState
    {
        // Current selection indices
        private int shirtIndex;
        private int pantsIndex;
        private int hatIndex;

        // Original outfit (captured when menu opens, for reverting on close)
        private readonly string originalShirt;
        private readonly string originalPants;
        private readonly string originalHat;

        // Applied outfit (set when Apply is clicked, for Reset button)
        private string appliedShirt;
        private string appliedPants;
        private string appliedHat;

        // Scroll position (row offset in the grid)
        private int scrollOffset = 0;

        // Current mod filter (null = no filter active)
        private string? currentModFilter = null;

        /// <summary>Gets or sets the current shirt index.</summary>
        public int ShirtIndex
        {
            get => shirtIndex;
            set => shirtIndex = Math.Max(0, value);
        }

        /// <summary>Gets or sets the current pants index.</summary>
        public int PantsIndex
        {
            get => pantsIndex;
            set => pantsIndex = Math.Max(0, value);
        }

        /// <summary>Gets or sets the current hat index.</summary>
        public int HatIndex
        {
            get => hatIndex;
            set => hatIndex = Math.Max(0, value);
        }

        /// <summary>Gets or sets the scroll offset for the item list.</summary>
        public int ScrollOffset
        {
            get => scrollOffset;
            set => scrollOffset = Math.Max(0, value);
        }

        /// <summary>Gets or sets the current mod filter. Null means no filter is active.</summary>
        public string? CurrentModFilter
        {
            get => currentModFilter;
            set => currentModFilter = value;
        }

        /// <summary>Gets the original shirt ID.</summary>
        public string OriginalShirt => originalShirt;

        /// <summary>Gets the original pants ID.</summary>
        public string OriginalPants => originalPants;

        /// <summary>Gets the original hat ID.</summary>
        public string OriginalHat => originalHat;

        /// <summary>
        /// Creates a new OutfitState, capturing the player's current outfit as both original and applied.
        /// </summary>
        public OutfitState()
        {
            originalShirt = Game1.player.shirt.Value;
            originalPants = Game1.player.pants.Value;
            originalHat = GetHatIdFromItem(Game1.player.hat.Value);

            // Applied starts as the original outfit
            appliedShirt = originalShirt;
            appliedPants = originalPants;
            appliedHat = originalHat;

            // Initialize indices to match current outfit
            // (will be set later by the menu)
            shirtIndex = 0;
            pantsIndex = 0;
            hatIndex = 0;
        }

        /// <summary>
        /// Applies the current selection to the player for a given category.
        /// </summary>
        /// <param name="category">The category to apply.</param>
        /// <param name="shirtIds">List of shirt IDs.</param>
        /// <param name="pantsIds">List of pants IDs.</param>
        /// <param name="hatIds">List of hat IDs.</param>
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
                        if (string.IsNullOrEmpty(hatId) || hatId == "-1")
                            Game1.player.hat.Value = null;
                        else
                            Game1.player.hat.Value = ItemRegistry.Create<Hat>("(H)" + hatId);
                    }
                    break;
            }
        }

        /// <summary>
        /// Saves the current preview as the applied outfit.
        /// </summary>
        public void SaveAppliedOutfit()
        {
            appliedShirt = Game1.player.shirt.Value;
            appliedPants = Game1.player.pants.Value;
            appliedHat = GetHatIdFromItem(Game1.player.hat.Value);
        }

        /// <summary>
        /// Resets the player's outfit to the last applied outfit (or original if nothing was applied).
        /// </summary>
        /// <param name="shirtIds">List of shirt IDs (for index lookup).</param>
        /// <param name="pantsIds">List of pants IDs (for index lookup).</param>
        /// <param name="hatIds">List of hat IDs (for index lookup).</param>
        public void ResetToApplied(System.Collections.Generic.List<string> shirtIds,
            System.Collections.Generic.List<string> pantsIds,
            System.Collections.Generic.List<string> hatIds)
        {
            // Reset to applied outfit
            Game1.player.shirt.Value = appliedShirt;
            Game1.player.pants.Value = appliedPants;

            if (string.IsNullOrEmpty(appliedHat) || appliedHat == "-1")
                Game1.player.hat.Value = null;
            else
                Game1.player.hat.Value = ItemRegistry.Create<Hat>("(H)" + appliedHat);

            Game1.player.FarmerRenderer.MarkSpriteDirty();

            // Reset indices to match applied outfit
            shirtIndex = Math.Max(0, shirtIds.IndexOf(appliedShirt));
            pantsIndex = Math.Max(0, pantsIds.IndexOf(appliedPants));
            hatIndex = Math.Max(0, hatIds.IndexOf(appliedHat));
        }

        /// <summary>
        /// Reverts the player's outfit to the applied state without changing indices.
        /// Used when closing the menu to discard unsaved changes.
        /// </summary>
        public void RevertToApplied()
        {
            Game1.player.shirt.Value = appliedShirt;
            Game1.player.pants.Value = appliedPants;

            if (string.IsNullOrEmpty(appliedHat) || appliedHat == "-1")
                Game1.player.hat.Value = null;
            else
                Game1.player.hat.Value = ItemRegistry.Create<Hat>("(H)" + appliedHat);

            Game1.player.FarmerRenderer.MarkSpriteDirty();
        }

        /// <summary>
        /// Gets the current index for the given category.
        /// </summary>
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

        /// <summary>
        /// Sets the current index for the given category.
        /// </summary>
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

        /// <summary>
        /// Extracts the hat ID from a Hat item (unqualified ID).
        /// </summary>
        /// <param name="hat">The hat item, can be null.</param>
        /// <returns>The hat ID, or "-1" if no hat.</returns>
        public static string GetHatIdFromItem(Hat hat)
        {
            if (hat == null)
                return "-1";

            // ItemId is like "(H)5" or "(H)delloti.CP.DL_Hats_0", extract the unqualified ID
            string itemId = hat.ItemId;
            if (string.IsNullOrEmpty(itemId))
                return "-1";

            // Remove the "(H)" prefix if present
            if (itemId.StartsWith("(H)"))
                return itemId.Substring(3);

            return itemId;
        }
    }
}
