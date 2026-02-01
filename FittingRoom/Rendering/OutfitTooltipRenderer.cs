using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace FittingRoom
{
    /// <summary>
    /// Renders item tooltips with name, description, and mod source.
    /// </summary>
    public class OutfitTooltipRenderer
    {
        private readonly OutfitFilterManager filterManager;
        private readonly OutfitCategoryManager categoryManager;

        public OutfitTooltipRenderer(
            OutfitFilterManager filterManager,
            OutfitCategoryManager categoryManager)
        {
            this.filterManager = filterManager ?? throw new ArgumentNullException(nameof(filterManager));
            this.categoryManager = categoryManager ?? throw new ArgumentNullException(nameof(categoryManager));
        }

        public void DrawTooltip(
            SpriteBatch b,
            int listIndex,
            List<string> shirtIds,
            List<string> pantsIds,
            List<string> hatIds)
        {
            var (itemName, description, modName, actualItem) = GetItemData(listIndex, shirtIds, pantsIds, hatIds);

            // Draw using vanilla hover text method (for proper formatting with divider)
            if (actualItem != null)
            {
                // Append mod name to description if present (skip vanilla items)
                string fullDescription = description;
                if (!string.IsNullOrEmpty(modName) && modName != TranslationCache.FilterVanilla)
                {
                    fullDescription += "\n\n" + TranslationCache.ItemModInfoTemplate.Replace("{{modName}}", modName);
                }

                // Use the vanilla drawHoverText that includes name, divider, and description
                IClickableMenu.drawHoverText(b, fullDescription, Game1.smallFont, 0, 0, -1, itemName, -1, null, actualItem);
            }
            else if (!string.IsNullOrEmpty(itemName))
            {
                // Fallback for items without actual item instance (like No Hat)
                string hoverText = itemName;
                if (!string.IsNullOrEmpty(description))
                {
                    hoverText += "\n" + description;
                }
                if (!string.IsNullOrEmpty(modName) && modName != TranslationCache.FilterVanilla)
                {
                    hoverText += "\n\n" + TranslationCache.ItemModInfoTemplate.Replace("{{modName}}", modName);
                }
                IClickableMenu.drawToolTip(b, hoverText, "", null);
            }
        }

        private (string itemName, string description, string modName, Item? item) GetItemData(
            int listIndex,
            List<string> shirtIds,
            List<string> pantsIds,
            List<string> hatIds)
        {
            string itemName = "";
            string description = "";
            string modName = "";
            Item? actualItem = null;

            switch (categoryManager.CurrentCategory)
            {
                case OutfitCategoryManager.Category.Shirts:
                    if (listIndex >= 0 && listIndex < shirtIds.Count)
                    {
                        string id = shirtIds[listIndex];
                        string qualifiedId = "(S)" + id;
                        actualItem = ItemRegistry.Create(qualifiedId);
                        if (actualItem != null)
                        {
                            itemName = actualItem.DisplayName;
                            description = actualItem.getDescription();
                        }
                        modName = filterManager.GetModNameForItem(id);
                    }
                    break;

                case OutfitCategoryManager.Category.Pants:
                    if (listIndex >= 0 && listIndex < pantsIds.Count)
                    {
                        string id = pantsIds[listIndex];
                        string qualifiedId = "(P)" + id;
                        actualItem = ItemRegistry.Create(qualifiedId);
                        if (actualItem != null)
                        {
                            itemName = actualItem.DisplayName;
                            description = actualItem.getDescription();
                        }
                        modName = filterManager.GetModNameForItem(id);
                    }
                    break;

                case OutfitCategoryManager.Category.Hats:
                    if (listIndex >= 0 && listIndex < hatIds.Count)
                    {
                        string hatId = hatIds[listIndex];
                        if (!string.IsNullOrEmpty(hatId) && hatId != OutfitLayoutConstants.NoHatId)
                        {
                            string qualifiedId = "(H)" + hatId;
                            actualItem = ItemRegistry.Create(qualifiedId);
                            if (actualItem != null)
                            {
                                itemName = actualItem.DisplayName;
                                description = actualItem.getDescription();
                            }
                            modName = filterManager.GetModNameForHat(hatId);
                        }
                        else
                        {
                            itemName = TranslationCache.ItemNoHat;
                            description = "";
                        }
                    }
                    break;
            }

            return (itemName, description, modName, actualItem);
        }
    }
}
