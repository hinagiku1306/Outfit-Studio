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
                // Build description, skipping empty/whitespace-only lines
                string fullDescription = "";
                if (!string.IsNullOrWhiteSpace(description))
                {
                    fullDescription = description.Trim();
                }

                // Append mod name if present (skip vanilla items)
                if (!string.IsNullOrEmpty(modName) && modName != TranslationCache.FilterVanilla)
                {
                    string modLine = TranslationCache.ItemModInfoTemplate.Replace("{{modName}}", modName);
                    if (!string.IsNullOrWhiteSpace(fullDescription))
                        fullDescription += "\n\n" + modLine;
                    else
                        fullDescription = modLine;
                }

                // Use the vanilla drawHoverText that includes name, divider, and description
                IClickableMenu.drawHoverText(b, fullDescription, Game1.smallFont, 0, 0, -1, itemName, -1, null, actualItem);
            }
            else if (!string.IsNullOrEmpty(itemName))
            {
                // Fallback for items without actual item instance (like No Hat)
                string hoverText = itemName;
                if (!string.IsNullOrWhiteSpace(description))
                {
                    hoverText += "\n" + description.Trim();
                }
                if (!string.IsNullOrEmpty(modName) && modName != TranslationCache.FilterVanilla)
                {
                    hoverText += "\n\n" + TranslationCache.ItemModInfoTemplate.Replace("{{modName}}", modName);
                }
                IClickableMenu.drawToolTip(b, hoverText, "", null);
            }
        }

        public void DrawTooltipForAllCategory(
            SpriteBatch b,
            OutfitCategoryManager.Category itemCategory,
            string itemId)
        {
            var (itemName, description, modName, actualItem) = GetItemDataByCategory(itemCategory, itemId);

            if (actualItem != null)
            {
                string fullDescription = "";
                if (!string.IsNullOrWhiteSpace(description))
                {
                    fullDescription = description.Trim();
                }

                if (!string.IsNullOrEmpty(modName) && modName != TranslationCache.FilterVanilla)
                {
                    string modLine = TranslationCache.ItemModInfoTemplate.Replace("{{modName}}", modName);
                    if (!string.IsNullOrWhiteSpace(fullDescription))
                        fullDescription += "\n\n" + modLine;
                    else
                        fullDescription = modLine;
                }

                IClickableMenu.drawHoverText(b, fullDescription, Game1.smallFont, 0, 0, -1, itemName, -1, null, actualItem);
            }
            else if (!string.IsNullOrEmpty(itemName))
            {
                string hoverText = itemName;
                if (!string.IsNullOrWhiteSpace(description))
                {
                    hoverText += "\n" + description.Trim();
                }
                if (!string.IsNullOrEmpty(modName) && modName != TranslationCache.FilterVanilla)
                {
                    hoverText += "\n\n" + TranslationCache.ItemModInfoTemplate.Replace("{{modName}}", modName);
                }
                IClickableMenu.drawToolTip(b, hoverText, "", null);
            }
        }

        private (string itemName, string description, string modName, Item? item) GetItemDataByCategory(
            OutfitCategoryManager.Category itemCategory,
            string itemId)
        {
            string itemName = "";
            string description = "";
            string modName = "";
            Item? actualItem = null;

            switch (itemCategory)
            {
                case OutfitCategoryManager.Category.Shirts:
                    {
                        string qualifiedId = "(S)" + itemId;
                        actualItem = ItemRegistry.Create(qualifiedId);
                        if (actualItem != null)
                        {
                            itemName = actualItem.DisplayName;
                            description = actualItem.getDescription();
                        }
                        modName = filterManager.GetModNameForItem(itemId);
                    }
                    break;

                case OutfitCategoryManager.Category.Pants:
                    {
                        string qualifiedId = "(P)" + itemId;
                        actualItem = ItemRegistry.Create(qualifiedId);
                        if (actualItem != null)
                        {
                            itemName = actualItem.DisplayName;
                            description = actualItem.getDescription();
                        }
                        modName = filterManager.GetModNameForItem(itemId);
                    }
                    break;

                case OutfitCategoryManager.Category.Hats:
                    if (!string.IsNullOrEmpty(itemId) && itemId != OutfitLayoutConstants.NoHatId)
                    {
                        string qualifiedId = "(H)" + itemId;
                        actualItem = ItemRegistry.Create(qualifiedId);
                        if (actualItem != null)
                        {
                            itemName = actualItem.DisplayName;
                            description = actualItem.getDescription();
                        }
                        modName = filterManager.GetModNameForHat(itemId);
                    }
                    else
                    {
                        itemName = TranslationCache.ItemNoHat;
                        description = "";
                    }
                    break;
            }

            return (itemName, description, modName, actualItem);
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
