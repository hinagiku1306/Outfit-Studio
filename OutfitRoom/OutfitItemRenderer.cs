using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

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
            string qualifiedId = GetQualifiedItemId(category, listIndex, shirtIds, pantsIds, hatIds);

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
            Item item = ItemRegistry.Create(qualifiedId);
            if (item == null)
                return;

            // Calculate position to center the item in the slot
            // drawInMenu expects top-left position; standard scale is 1f for 64x64 slots
            Vector2 position = new Vector2(slot.X, slot.Y);

            // Use vanilla drawInMenu - renders at standard inventory size
            item.drawInMenu(b, position, 1f);
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
        private string GetQualifiedItemId(OutfitCategoryManager.Category category, int listIndex,
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
    }
}
