using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using static OutfitRoom.OutfitLayoutConstants;

namespace OutfitRoom
{
    /// <summary>
    /// Responsible for drawing clothing item sprites in the menu.
    /// </summary>
    public class OutfitItemRenderer
    {
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
            Rectangle slot, List<string> shirtIds, List<string> pantsIds, List<int> hatIds)
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
        /// </summary>
        private void DrawItemUsingVanillaMethod(SpriteBatch b, string qualifiedId, Rectangle slot)
        {
            // Check if the item ID exists before creating
            if (!ItemRegistry.Exists(qualifiedId))
            {
                // Log missing item instead of drawing placeholder
                LogMissingItem(qualifiedId, "Item does not exist in registry");
                return;
            }

            Item item = ItemRegistry.Create(qualifiedId);
            if (item == null)
            {
                LogMissingItem(qualifiedId, "Failed to create item");
                return;
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
        /// </summary>
        private void LogMissingItem(string qualifiedId, string reason)
        {
            // Parse item type and ID
            string itemType = qualifiedId.StartsWith("(S)") ? "Shirt" :
                            qualifiedId.StartsWith("(P)") ? "Pants" :
                            qualifiedId.StartsWith("(H)") ? "Hat" : "Unknown";
            string itemId = qualifiedId.Length > 3 ? qualifiedId[3..] : qualifiedId;

            // Log to console (uses standard output which SMAPI captures)
            Console.WriteLine($"[OutfitRoom] Skipping missing item: {itemType} '{itemId}' - {reason}");
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
            List<string> shirtIds, List<string> pantsIds, List<int> hatIds)
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
                        int hatId = hatIds[listIndex];
                        if (hatId >= 0)
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
            List<string> shirtIds, List<string> pantsIds, List<int> hatIds)
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
