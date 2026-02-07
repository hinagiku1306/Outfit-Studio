using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;

namespace OutfitStudio
{
    /// <summary>
    /// Manages clothing categories (shirts, pants, hats) and their item lists.
    /// </summary>
    public class OutfitCategoryManager
    {
        private readonly IMonitor monitor;
        private readonly OutfitFilterManager? filterManager;

        public Category CurrentCategory { get; set; } = Category.Shirts;

        public readonly List<string> ShirtIds = new();
        public readonly List<string> PantsIds = new();
        public readonly List<string> HatIds = new(); // Includes NoHatId for no hat
        public readonly List<(Category ItemCategory, string ItemId)> AllItemIds = new();

        public enum Category { All, Shirts, Pants, Hats }

        public OutfitCategoryManager(IMonitor monitor, OutfitFilterManager? filterManager = null)
        {
            this.monitor = monitor;
            this.filterManager = filterManager;
            LoadShirts();
            LoadPants();
            LoadHats();
            BuildAllItemsList();
        }

        private void BuildAllItemsList()
        {
            AllItemIds.Clear();
            foreach (var id in ShirtIds)
                AllItemIds.Add((Category.Shirts, id));
            foreach (var id in PantsIds)
                AllItemIds.Add((Category.Pants, id));
            foreach (var id in HatIds)
                AllItemIds.Add((Category.Hats, id));
        }

        private void LoadShirts()
        {
            ShirtIds.Clear();
            foreach (var id in Game1.shirtData.Keys)
            {
                ShirtIds.Add(id);
            }
        }

        private void LoadPants()
        {
            PantsIds.Clear();
            foreach (var id in Game1.pantsData.Keys)
            {
                PantsIds.Add(id);
            }
        }

        private void LoadHats()
        {
            HatIds.Clear();
            HatIds.Add(OutfitLayoutConstants.NoHatId);
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
                Category.All => AllItemIds.Count,
                Category.Shirts => ShirtIds.Count,
                Category.Pants => PantsIds.Count,
                Category.Hats => HatIds.Count,
                _ => 0
            };
        }

        /// <summary>Get the qualified item ID for an item at the given index in the current category.</summary>
        public string? GetQualifiedItemId(int listIndex)
        {
            return ItemIdHelper.GetQualifiedItemId(CurrentCategory, listIndex, ShirtIds, PantsIds, HatIds);
        }
    }
}
