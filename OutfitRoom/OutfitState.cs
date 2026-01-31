using System;
using StardewValley;
using StardewValley.Objects;

namespace OutfitRoom
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

        // Original outfit (for reset)
        private readonly string originalShirt;
        private readonly string originalPants;
        private readonly int originalHat;

        // Saved outfit (for Set)
        private string savedShirt;
        private string savedPants;
        private int savedHat;

        // Scroll position
        private int scrollOffset = 0;

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

        /// <summary>Gets the original shirt ID.</summary>
        public string OriginalShirt => originalShirt;

        /// <summary>Gets the original pants ID.</summary>
        public string OriginalPants => originalPants;

        /// <summary>Gets the original hat ID.</summary>
        public int OriginalHat => originalHat;

        /// <summary>Gets or sets the saved shirt ID.</summary>
        public string SavedShirt
        {
            get => savedShirt;
            set => savedShirt = value;
        }

        /// <summary>Gets or sets the saved pants ID.</summary>
        public string SavedPants
        {
            get => savedPants;
            set => savedPants = value;
        }

        /// <summary>Gets or sets the saved hat ID.</summary>
        public int SavedHat
        {
            get => savedHat;
            set => savedHat = value;
        }

        /// <summary>
        /// Creates a new OutfitState, capturing the player's current outfit as the original.
        /// </summary>
        public OutfitState()
        {
            originalShirt = Game1.player.shirt.Value;
            originalPants = Game1.player.pants.Value;
            originalHat = GetHatIdFromItem(Game1.player.hat.Value);

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
            System.Collections.Generic.List<int> hatIds)
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
                        int hatId = hatIds[hatIndex];
                        if (hatId < 0)
                            Game1.player.hat.Value = null;
                        else
                            Game1.player.hat.Value = ItemRegistry.Create<Hat>("(H)" + hatId);
                    }
                    break;
            }
        }

        /// <summary>
        /// Resets the player's outfit to the original captured outfit.
        /// </summary>
        /// <param name="shirtIds">List of shirt IDs (for index lookup).</param>
        /// <param name="pantsIds">List of pants IDs (for index lookup).</param>
        /// <param name="hatIds">List of hat IDs (for index lookup).</param>
        public void ResetOutfit(System.Collections.Generic.List<string> shirtIds,
            System.Collections.Generic.List<string> pantsIds,
            System.Collections.Generic.List<int> hatIds)
        {
            // Reset shirt
            Game1.player.shirt.Value = originalShirt;

            // Reset pants
            Game1.player.pants.Value = originalPants;

            // Reset hat
            if (originalHat < 0)
                Game1.player.hat.Value = null;
            else
                Game1.player.hat.Value = ItemRegistry.Create<Hat>("(H)" + originalHat);

            Game1.player.FarmerRenderer.MarkSpriteDirty();

            // Reset indices
            shirtIndex = Math.Max(0, shirtIds.IndexOf(originalShirt));
            pantsIndex = Math.Max(0, pantsIds.IndexOf(originalPants));
            hatIndex = Math.Max(0, hatIds.IndexOf(originalHat));
            scrollOffset = 0;
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
        /// Extracts the numeric hat ID from a Hat item.
        /// </summary>
        /// <param name="hat">The hat item, can be null.</param>
        /// <returns>The hat ID, or -1 if no hat.</returns>
        public static int GetHatIdFromItem(Hat hat)
        {
            if (hat == null)
                return -1;

            // ItemId is like "(H)5", extract the number
            string itemId = hat.ItemId;
            if (string.IsNullOrEmpty(itemId))
                return -1;

            // Remove the "(H)" prefix if present
            if (itemId.StartsWith("(H)"))
                itemId = itemId.Substring(3);

            if (int.TryParse(itemId, out int hatId))
                return hatId;

            return -1;
        }
    }
}