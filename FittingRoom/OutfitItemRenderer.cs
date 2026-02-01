using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using static FittingRoom.OutfitLayoutConstants;

namespace FittingRoom
{
    /// <summary>
    /// Responsible for drawing clothing item sprites in the menu.
    /// </summary>
    public class OutfitItemRenderer
    {
        /// <summary>
        /// Tracks which missing items have already been logged to prevent spam.
        /// </summary>
        private static readonly HashSet<string> loggedMissingItems = new();

        /// <summary>SMAPI monitor for logging.</summary>
        private readonly IMonitor monitor;

        /// <summary>SMAPI mod registry for looking up mod information.</summary>
        private readonly IModRegistry modRegistry;

        /// <summary>
        /// Creates a new item renderer.
        /// </summary>
        public OutfitItemRenderer(IMonitor monitor, IModRegistry modRegistry)
        {
            this.monitor = monitor;
            this.modRegistry = modRegistry;
        }
        /// <summary>
        /// Draws a clothing item sprite in the given slot rectangle using vanilla inventory rendering.
        /// </summary>
        /// <param name="b">SpriteBatch to draw with.</param>
        /// <param name="category">Category of the item.</param>
        /// <param name="listIndex">Index in the category list.</param>
        /// <param name="slot">Rectangle defining the slot area.</param>
        /// <param name="shirtIds">List of shirt IDs (for shirts category).</param>
        /// <param name="pantsIds">List of pants IDs (for pants category).</param>
        /// <param name="hatIds">List of hat IDs (for hats category).</param>
        public void DrawItemSprite(SpriteBatch b, OutfitCategoryManager.Category category, int listIndex,
            Rectangle slot, List<string> shirtIds, List<string> pantsIds, List<string> hatIds)
        {
            string? qualifiedId = GetQualifiedItemId(category, listIndex, shirtIds, pantsIds, hatIds);

            if (qualifiedId != null)
            {
                DrawItemUsingVanillaMethod(b, qualifiedId, slot);
            }
            else if (category == OutfitCategoryManager.Category.Hats && listIndex == 0)
            {
                // Draw "No Hat" indicator (empty slot)
                DrawNoHatIndicator(b, slot);
            }
        }

        /// <summary>
        /// Draws an item using the vanilla drawInMenu method like inventory slots.
        /// Items that don't exist or fail to create are skipped entirely (not drawn).
        /// </summary>
        private void DrawItemUsingVanillaMethod(SpriteBatch b, string qualifiedId, Rectangle slot)
        {
            // Check if the item ID exists before creating
            if (!ItemRegistry.Exists(qualifiedId))
            {
                return; // Don't draw anything
            }

            Item item = ItemRegistry.Create(qualifiedId);
            if (item == null)
            {
                return; // Don't draw anything
            }

            // Center the item in the slot
            int offsetX = (slot.Width - DrawnItemSize) / 2;
            int offsetY = (slot.Height - DrawnItemSize) / 2;
            Vector2 position = new Vector2(slot.X + offsetX, slot.Y + offsetY);

            // Use vanilla drawInMenu - renders at standard inventory size
            item.drawInMenu(b, position, 1f);
        }

        /// <summary>
        /// Logs information about a missing item to the console.
        /// Only logs each unique item once to prevent spam.
        /// </summary>
        private void LogMissingItem(string qualifiedId, string reason)
        {
            // Only log each missing item once
            if (!loggedMissingItems.Add(qualifiedId))
            {
                return; // Already logged this item
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
            monitor.Log($"Skipped missing item: {itemType} '{itemName}' (ID: {itemId}) from mod {modDisplayText} - {reason}", LogLevel.Trace);
        }

        /// <summary>
        /// Draws an "X" for the no‑hat slot.
        /// </summary>
        private void DrawNoHatIndicator(SpriteBatch b, Rectangle slot)
        {
            Vector2 textPos = new Vector2(
                slot.X + (slot.Width - Game1.smallFont.MeasureString("X").X) / 2,
                slot.Y + (slot.Height - Game1.smallFont.LineSpacing) / 2
            );
            Utility.drawTextWithShadow(b, "X", Game1.smallFont, textPos, Color.Gray);
        }

        /// <summary>
        /// Returns the qualified item ID for the given category and index, or null if none.
        /// </summary>
        private string? GetQualifiedItemId(OutfitCategoryManager.Category category, int listIndex,
            List<string> shirtIds, List<string> pantsIds, List<string> hatIds)
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
                        if (!string.IsNullOrEmpty(hatId) && hatId != "-1")
                            return "(H)" + hatId;
                    }
                    break;
            }
            return null;
        }

        /// <summary>
        /// Gets the sprite texture and source rectangle for a tab icon.
        /// Uses a representative sprite for each category.
        /// </summary>
        public (Texture2D texture, Rectangle sourceRect) GetTabSpriteInfo(OutfitCategoryManager.Category category,
            List<string> shirtIds, List<string> pantsIds, List<string> hatIds)
        {
            switch (category)
            {
                case OutfitCategoryManager.Category.Shirts:
                    // Use the first shirt sprite (top-left of shirt texture)
                    return (FarmerRenderer.shirtsTexture, new Rectangle(0, 0, 8, 8));

                case OutfitCategoryManager.Category.Pants:
                    // Use the first pants sprite (top-left of pants texture)
                    return (FarmerRenderer.pantsTexture, new Rectangle(0, 0, 16, 16));

                case OutfitCategoryManager.Category.Hats:
                    // Use the first hat sprite (index 0 in hat texture - the red cap)
                    return (FarmerRenderer.hatsTexture, new Rectangle(0, 0, 20, 20));

                default:
                    return (Game1.staminaRect, new Rectangle(0, 0, 1, 1));
            }
        }
    }
}
