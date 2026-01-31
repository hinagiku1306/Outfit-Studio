using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace OutfitRoom
{
    /// <summary>
    /// Responsible for drawing clothing item sprites in the menu.
    /// </summary>
    public class OutfitItemRenderer
    {
        /// <summary>
        /// Draws a clothing item sprite in the given slot rectangle.
        /// </summary>
        /// <param name="b">SpriteBatch to draw with.</param>
        /// <param name="category">Category of the item.</param>
        /// <param name="listIndex">Index in the category list.</param>
        /// <param name="slot">Rectangle defining the slot area.</param>
        /// <param name="isSelected">Whether the item is currently selected.</param>
        /// <param name="shirtIds">List of shirt IDs (for shirts category).</param>
        /// <param name="pantsIds">List of pants IDs (for pants category).</param>
        /// <param name="hatIds">List of hat IDs (for hats category).</param>
        public void DrawItemSprite(SpriteBatch b, OutfitCategoryManager.Category category, int listIndex,
            Rectangle slot, bool isSelected, System.Collections.Generic.List<string> shirtIds,
            System.Collections.Generic.List<string> pantsIds, System.Collections.Generic.List<int> hatIds)
        {
            string qualifiedId = GetQualifiedItemId(category, listIndex, shirtIds, pantsIds, hatIds);

            if (qualifiedId != null)
            {
                DrawQualifiedItem(b, qualifiedId, slot, isSelected);
            }
            else if (category == OutfitCategoryManager.Category.Hats && listIndex == 0)
            {
                // Draw "No Hat" indicator (empty slot)
                DrawNoHatIndicator(b, slot);
            }
        }

        /// <summary>
        /// Draws a qualified item (shirt, pants, or hat) in the slot.
        /// </summary>
        private void DrawQualifiedItem(SpriteBatch b, string qualifiedId, Rectangle slot, bool isSelected)
        {
            ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(qualifiedId);
            Texture2D texture = itemData.GetTexture();
            Rectangle sourceRect = itemData.GetSourceRect();

            int padding = Math.Max(2, slot.Height / 6);
            int maxWidth = Math.Max(1, slot.Width - padding * 2);
            int maxHeight = Math.Max(1, slot.Height - padding * 2);

            // Calculate scale to fit in slot while maintaining aspect ratio
            float scale = Math.Min(
                (float)maxWidth / sourceRect.Width,
                (float)maxHeight / sourceRect.Height
            );

            // Center sprite in slot
            Vector2 position = new Vector2(
                slot.X + (slot.Width - sourceRect.Width * scale) / 2f,
                slot.Y + (slot.Height - sourceRect.Height * scale) / 2f
            );

            // Draw with selection tint
            Color tint = isSelected ? Color.White : Color.White * 0.9f;
            b.Draw(texture, position, sourceRect, tint, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.9f);
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
            System.Collections.Generic.List<string> shirtIds, System.Collections.Generic.List<string> pantsIds,
            System.Collections.Generic.List<int> hatIds)
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
