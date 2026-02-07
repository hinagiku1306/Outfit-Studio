using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    /// <summary>
    /// Draws clothing item sprites in the menu using vanilla inventory rendering.
    /// </summary>
    public class OutfitItemRenderer
    {
        private static readonly HashSet<string> loggedMissingItems = new();
        private readonly Dictionary<string, Item?> itemCache = new();

        private readonly IMonitor monitor;
        private readonly IModRegistry modRegistry;

        public OutfitItemRenderer(IMonitor monitor, IModRegistry modRegistry)
        {
            this.monitor = monitor;
            this.modRegistry = modRegistry;
        }

        public void DrawItemSprite(SpriteBatch b, OutfitCategoryManager.Category category, int listIndex,
            Rectangle slot, List<string> shirtIds, List<string> pantsIds, List<string> hatIds)
        {
            string? qualifiedId = GetQualifiedItemId(category, listIndex, shirtIds, pantsIds, hatIds);

            if (qualifiedId != null)
            {
                DrawItem(b, qualifiedId, slot);
            }
            else if (category == OutfitCategoryManager.Category.Hats && listIndex == 0)
            {
                DrawNoHatIndicator(b, slot);
            }
        }

        public void DrawItemFromAllCategory(SpriteBatch b, OutfitCategoryManager.Category itemCategory, string itemId, Rectangle slot)
        {
            if (itemCategory == OutfitCategoryManager.Category.Hats && ItemIdHelper.IsNoHatId(itemId))
            {
                DrawNoHatIndicator(b, slot);
                return;
            }

            string? qualifiedId = ItemIdHelper.GetQualifiedId(itemId, itemCategory);
            if (!string.IsNullOrEmpty(qualifiedId))
            {
                DrawItem(b, qualifiedId, slot);
            }
        }

        public void ClearCache()
        {
            itemCache.Clear();
        }

        private Item? GetCachedItem(string qualifiedId)
        {
            if (itemCache.TryGetValue(qualifiedId, out var item))
                return item;

            if (!ItemRegistry.Exists(qualifiedId))
            {
                itemCache[qualifiedId] = null;
                return null;
            }

            item = ItemRegistry.Create(qualifiedId);
            itemCache[qualifiedId] = item;
            return item;
        }

        private void DrawItem(SpriteBatch b, string qualifiedId, Rectangle slot)
        {
            var item = GetCachedItem(qualifiedId);
            if (item == null)
                return;

            int offsetX = (slot.Width - DrawnItemSize) / 2;
            int offsetY = (slot.Height - DrawnItemSize) / 2;
            Vector2 position = new Vector2(slot.X + offsetX, slot.Y + offsetY);

            item.drawInMenu(b, position, 1f);
        }

        // Logs each unique missing item once to prevent spam
        private void LogMissingItem(string qualifiedId, string reason)
        {
            if (!loggedMissingItems.Add(qualifiedId))
            {
                return;
            }

            string UNKNOWN = "Unknown";
            // Parse item type and ID
            string itemType = qualifiedId.StartsWith("(S)") ? "Shirt" :
                            qualifiedId.StartsWith("(P)") ? "Pants" :
                            qualifiedId.StartsWith("(H)") ? "Hat" : UNKNOWN;
            string itemId = qualifiedId.Length > 3 ? qualifiedId[3..] : qualifiedId;

            // Try to get the item name and source mod from registry
            string itemName = itemId;
            string modSource = UNKNOWN;
            string modName = UNKNOWN;

            try
            {
                var itemData = ItemRegistry.GetDataOrErrorItem(qualifiedId);
                if (itemData != null)
                {
                    // Get display name if available
                    if (!string.IsNullOrEmpty(itemData.DisplayName))
                    {
                        itemName = itemData.DisplayName;
                    }

                    // Try to determine mod source from item data
                    // Items added by mods typically have a mod ID in their qualified ID or data
                    if (!string.IsNullOrEmpty(itemData.QualifiedItemId))
                    {
                        // Check if this is a modded item by looking for mod prefix pattern
                        string rawId = itemData.QualifiedItemId;
                        if (rawId.StartsWith('(') && rawId.Length > 3)
                        {
                            rawId = rawId[3..]; // Remove the qualifier like "(S)"
                        }

                        // Check if the ID contains a mod prefix (common pattern: ModId_ItemId or ModId.ItemId)
                        if (rawId.Contains('_') || rawId.Contains('.'))
                        {
                            char separator = rawId.Contains('_') ? '_' : '.';
                            string potentialModId = rawId.Split(separator)[0];

                            // Try to look up this mod in the registry
                            var modInfo = modRegistry.Get(potentialModId);
                            if (modInfo != null)
                            {
                                modSource = potentialModId;
                                modName = modInfo.Manifest.Name;
                            }
                            else
                            {
                                modSource = potentialModId; // Use the ID even if we can't find the mod
                            }
                        }
                        // Check if this looks like a vanilla numeric ID
                        else if (int.TryParse(rawId, out _))
                        {
                            modSource = "Vanilla";
                            modName = "Stardew Valley";
                        }
                    }
                }
            }
            catch
            {
                // If we can't get item data, fall back to ID parsing
                if (itemId.Contains('_'))
                {
                    string potentialModId = itemId.Split('_')[0];
                    var modInfo = modRegistry.Get(potentialModId);
                    if (modInfo != null)
                    {
                        modSource = potentialModId;
                        modName = modInfo.Manifest.Name;
                    }
                }
                else if (int.TryParse(itemId, out _))
                {
                    modSource = "Vanilla";
                    modName = "Stardew Valley";
                }
            }

            // Log using SMAPI Monitor
            string modDisplayText = modName != UNKNOWN && modName != modSource
                ? $"'{modName}' ({modSource})"
                : $"'{modSource}'";
            DebugLogger.Log($"Skipped missing item: {itemType} '{itemName}' (ID: {itemId}) from mod {modDisplayText} - {reason}", LogLevel.Trace);
        }

        private void DrawNoHatIndicator(SpriteBatch b, Rectangle slot)
        {
            string symbol = "X";
            Vector2 textPos = new Vector2(
                slot.X + (slot.Width - Game1.smallFont.MeasureString(symbol).X) / 2,
                slot.Y + (slot.Height - Game1.smallFont.LineSpacing) / 2
            );
            Utility.drawTextWithShadow(b, symbol, Game1.smallFont, textPos, Color.Gray);
        }

        private string? GetQualifiedItemId(OutfitCategoryManager.Category category, int listIndex,
            List<string> shirtIds, List<string> pantsIds, List<string> hatIds)
        {
            return ItemIdHelper.GetQualifiedItemId(category, listIndex, shirtIds, pantsIds, hatIds);
        }

        // Returns a representative sprite for each category tab icon
        public (Texture2D texture, Rectangle sourceRect) GetTabSpriteInfo(OutfitCategoryManager.Category category,
            List<string> shirtIds, List<string> pantsIds, List<string> hatIds)
        {
            switch (category)
            {
                case OutfitCategoryManager.Category.Shirts:
                    return (FarmerRenderer.shirtsTexture, new Rectangle(0, 0, 8, 8));

                case OutfitCategoryManager.Category.Pants:
                    return (FarmerRenderer.pantsTexture, new Rectangle(0, 0, 16, 16));

                case OutfitCategoryManager.Category.Hats:
                    return (FarmerRenderer.hatsTexture, new Rectangle(0, 0, 20, 20));

                default:
                    return (Game1.staminaRect, new Rectangle(0, 0, 1, 1));
            }
        }
    }
}
