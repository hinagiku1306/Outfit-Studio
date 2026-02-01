using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;

namespace FittingRoom
{
    /// <summary>
    /// Manages clothing categories (shirts, pants, hats) and their item lists.
    /// </summary>
    public class OutfitCategoryManager
    {
        /// <summary>SMAPI monitor for logging.</summary>
        private readonly IMonitor monitor;

        /// <summary>Reference to the filter manager for mod detection during logging.</summary>
        private readonly OutfitFilterManager? filterManager;

        /// <summary>The current selected category.</summary>
        public Category CurrentCategory { get; set; } = Category.Shirts;

        /// <summary>Available shirt IDs.</summary>
        public readonly List<string> ShirtIds = new();

        /// <summary>Available pants IDs.</summary>
        public readonly List<string> PantsIds = new();

        /// <summary>Available hat IDs (including "-1" for no hat).</summary>
        public readonly List<string> HatIds = new();

        /// <summary>Enum of clothing categories.</summary>
        public enum Category { Shirts, Pants, Hats }

        /// <summary>
        /// Initialize and populate the clothing lists from game data.
        /// </summary>
        /// <param name="monitor">SMAPI monitor for logging.</param>
        /// <param name="filterManager">Optional filter manager for mod detection during logging.</param>
        public OutfitCategoryManager(IMonitor monitor, OutfitFilterManager? filterManager = null)
        {
            this.monitor = monitor;
            this.filterManager = filterManager;
            LoadShirts();
            LoadPants();
            LoadHats();
        }

        /// <summary>Load all shirt IDs from Game1.shirtData.</summary>
        private void LoadShirts()
        {
            ShirtIds.Clear();
            foreach (var id in Game1.shirtData.Keys)
            {
                ShirtIds.Add(id);
            }
        }

        /// <summary>Load all pants IDs from Game1.pantsData.</summary>
        private void LoadPants()
        {
            PantsIds.Clear();
            foreach (var id in Game1.pantsData.Keys)
            {
                PantsIds.Add(id);
            }
        }

        /// <summary>Load all hat IDs from DataLoader.Hats (including "-1" for no hat).</summary>
        private void LoadHats()
        {
            HatIds.Clear();
            HatIds.Add("-1"); // no hat option (always valid)
            foreach (var id in DataLoader.Hats(Game1.content).Keys)
            {
                HatIds.Add(id);
            }
        }

        /// <summary>
        /// Get the display name for an item at the given list index.
        /// </summary>
        public string? GetItemDisplayName(int categoryIndex)
        {
            string? qualifiedId = GetQualifiedItemId(categoryIndex);
            if (qualifiedId == null)
                return null;

            switch (CurrentCategory)
            {
                case Category.Shirts:
                    if (Game1.shirtData.TryGetValue(ShirtIds[categoryIndex], out var shirtData))
                        return shirtData.DisplayName ?? ShirtIds[categoryIndex];
                    break;

                case Category.Pants:
                    if (Game1.pantsData.TryGetValue(PantsIds[categoryIndex], out var pantsData))
                        return pantsData.DisplayName ?? PantsIds[categoryIndex];
                    break;

                case Category.Hats:
                    try
                    {
                        var itemData = ItemRegistry.GetDataOrErrorItem(qualifiedId);
                        return itemData.DisplayName ?? HatIds[categoryIndex];
                    }
                    catch
                    {
                        return HatIds[categoryIndex];
                    }
            }
            return null;
        }

        /// <summary>
        /// Get the total number of items in the current category.
        /// </summary>
        public int GetCurrentListCount()
        {
            return CurrentCategory switch
            {
                Category.Shirts => ShirtIds.Count,
                Category.Pants => PantsIds.Count,
                Category.Hats => HatIds.Count,
                _ => 0
            };
        }

        /// <summary>Get the qualified item ID for an item at the given index in the current category.</summary>
        public string? GetQualifiedItemId(int listIndex)
        {
            switch (CurrentCategory)
            {
                case Category.Shirts:
                    if (listIndex >= 0 && listIndex < ShirtIds.Count)
                        return "(S)" + ShirtIds[listIndex];
                    break;

                case Category.Pants:
                    if (listIndex >= 0 && listIndex < PantsIds.Count)
                        return "(P)" + PantsIds[listIndex];
                    break;

                case Category.Hats:
                    if (listIndex >= 0 && listIndex < HatIds.Count)
                    {
                        string hatId = HatIds[listIndex];
                        if (!string.IsNullOrEmpty(hatId) && hatId != "-1")
                            return "(H)" + hatId;
                    }
                    break;
            }
            return null;
        }
    }
}
