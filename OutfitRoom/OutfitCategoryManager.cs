using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;

namespace OutfitRoom
{
    /// <summary>
    /// Manages clothing categories (shirts, pants, hats) and their item lists.
    /// </summary>
    public class OutfitCategoryManager
    {
        /// <summary>
        /// Tracks which invalid items have been logged to prevent spam.
        /// </summary>
        private static readonly HashSet<string> loggedInvalidItems = new();

        /// <summary>SMAPI monitor for logging.</summary>
        private readonly IMonitor monitor;

        /// <summary>The current selected category.</summary>
        public Category CurrentCategory { get; set; } = Category.Shirts;

        /// <summary>Available shirt IDs.</summary>
        public readonly List<string> ShirtIds = new();

        /// <summary>Available pants IDs.</summary>
        public readonly List<string> PantsIds = new();

        /// <summary>Available hat IDs (including -1 for no hat).</summary>
        public readonly List<int> HatIds = new();

        /// <summary>Enum of clothing categories.</summary>
        public enum Category { Shirts, Pants, Hats }

        /// <summary>
        /// Initialize and populate the clothing lists from game data.
        /// </summary>
        public OutfitCategoryManager(IMonitor monitor)
        {
            this.monitor = monitor;
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
                string qualifiedId = "(S)" + id;
                if (ItemRegistry.Exists(qualifiedId))
                {
                    ShirtIds.Add(id);
                }
                else
                {
                    LogInvalidItem(qualifiedId, "Shirt", id);
                }
            }
        }

        /// <summary>Load all pants IDs from Game1.pantsData.</summary>
        private void LoadPants()
        {
            PantsIds.Clear();
            foreach (var id in Game1.pantsData.Keys)
            {
                string qualifiedId = "(P)" + id;
                if (ItemRegistry.Exists(qualifiedId))
                {
                    PantsIds.Add(id);
                }
                else
                {
                    LogInvalidItem(qualifiedId, "Pants", id);
                }
            }
        }

        /// <summary>Load all hat IDs (including -1 for no hat).</summary>
        private void LoadHats()
        {
            HatIds.Clear();
            HatIds.Add(-1); // no hat option (always valid)
            int maxHats = FarmerRenderer.hatsTexture.Height / 80 * 12;
            for (int i = 0; i < maxHats; i++)
            {
                string qualifiedId = "(H)" + i;
                if (ItemRegistry.Exists(qualifiedId))
                {
                    HatIds.Add(i);
                }
                else
                {
                    LogInvalidItem(qualifiedId, "Hat", i.ToString());
                }
            }
        }

        /// <summary>
        /// Logs invalid items that were filtered out during loading.
        /// Only logs each unique item once to prevent spam.
        /// </summary>
        private void LogInvalidItem(string qualifiedId, string itemType, string itemId)
        {
            // Only log each invalid item once
            if (!loggedInvalidItems.Add(qualifiedId))
            {
                return; // Already logged this item
            }

            // Try to get the item name and source mod
            string itemName = itemId;
            string modSource = "Unknown";

            // Check if this is a modded item (contains underscore, common pattern: ModId_ItemId)
            if (itemId.Contains('_'))
            {
                modSource = itemId.Split('_')[0];
            }
            // Check if this looks like a vanilla numeric ID
            else if (int.TryParse(itemId, out _))
            {
                modSource = "Vanilla (possibly)";
            }

            try
            {
                var itemData = ItemRegistry.GetDataOrErrorItem(qualifiedId);
                if (itemData != null && !string.IsNullOrEmpty(itemData.DisplayName))
                {
                    itemName = itemData.DisplayName;
                }
            }
            catch
            {
                // If we can't get display name, just use the ID
            }

            monitor.Log($"Filtered invalid item during load: {itemType} '{itemName}' (ID: {itemId}) from mod '{modSource}'", LogLevel.Trace);
        }

        /// <summary>Get the count of items in the current category.</summary>
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

        /// <summary>Get the display name for an item at the given index in the current category.</summary>
        public string GetItemDisplayName(int listIndex)
        {
            switch (CurrentCategory)
            {
                case Category.Shirts:
                    if (listIndex >= 0 && listIndex < ShirtIds.Count)
                    {
                        string id = ShirtIds[listIndex];
                        if (Game1.shirtData.TryGetValue(id, out var data))
                            return data.DisplayName ?? id;
                        return id;
                    }
                    break;

                case Category.Pants:
                    if (listIndex >= 0 && listIndex < PantsIds.Count)
                    {
                        string id = PantsIds[listIndex];
                        if (Game1.pantsData.TryGetValue(id, out var data))
                            return data.DisplayName ?? id;
                        return id;
                    }
                    break;

                case Category.Hats:
                    if (listIndex >= 0 && listIndex < HatIds.Count)
                    {
                        int hatId = HatIds[listIndex];
                        if (hatId == -1)
                            return "(No Hat)";
                        return $"Hat #{hatId}";
                    }
                    break;
            }
            return "???";
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
                        int hatId = HatIds[listIndex];
                        if (hatId >= 0)
                            return "(H)" + hatId;
                    }
                    break;
            }
            return null;
        }
    }
}