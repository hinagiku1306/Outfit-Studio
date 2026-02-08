using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace OutfitStudio
{
    public class OutfitTooltipRenderer
    {
        private readonly OutfitFilterManager filterManager;
        private readonly OutfitCategoryManager categoryManager;
        private readonly Dictionary<string, Item?> itemCache = new();

        public OutfitTooltipRenderer(
            OutfitFilterManager filterManager,
            OutfitCategoryManager categoryManager)
        {
            this.filterManager = filterManager ?? throw new ArgumentNullException(nameof(filterManager));
            this.categoryManager = categoryManager ?? throw new ArgumentNullException(nameof(categoryManager));
        }

        public void ClearCache()
        {
            itemCache.Clear();
        }

        private Item? GetCachedItem(string qualifiedId)
        {
            if (itemCache.TryGetValue(qualifiedId, out var item))
                return item;

            item = ItemRegistry.Create(qualifiedId);
            itemCache[qualifiedId] = item;
            return item;
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

            if (itemCategory == OutfitCategoryManager.Category.Hats && ItemIdHelper.IsNoHatId(itemId))
            {
                itemName = TranslationCache.ItemNoHat;
                return (itemName, description, modName, actualItem);
            }

            string? qualifiedId = ItemIdHelper.GetQualifiedId(itemId, itemCategory);
            if (qualifiedId != null)
            {
                actualItem = GetCachedItem(qualifiedId);
                if (actualItem != null)
                {
                    itemName = actualItem.DisplayName;
                    description = actualItem.getDescription();
                }
            }

            // Determine mod name
            if (itemCategory == OutfitCategoryManager.Category.Hats)
                modName = filterManager.GetModNameForHat(itemId);
            else
                modName = filterManager.GetModNameForItem(itemId);

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

            string? qualifiedId = ItemIdHelper.GetQualifiedItemId(categoryManager.CurrentCategory, listIndex, shirtIds, pantsIds, hatIds);
            if (qualifiedId == null)
            {
                // This could be no hat case (listIndex == 0 for hats)
                if (categoryManager.CurrentCategory == OutfitCategoryManager.Category.Hats && listIndex == 0)
                {
                    itemName = TranslationCache.ItemNoHat;
                }
                return (itemName, description, modName, actualItem);
            }

            actualItem = GetCachedItem(qualifiedId);
            if (actualItem != null)
            {
                itemName = actualItem.DisplayName;
                description = actualItem.getDescription();
            }

            // Determine unqualified ID for mod name lookup
            string unqualifiedId = ItemIdHelper.GetUnqualifiedId(qualifiedId);
            if (categoryManager.CurrentCategory == OutfitCategoryManager.Category.Hats)
                modName = filterManager.GetModNameForHat(unqualifiedId);
            else
                modName = filterManager.GetModNameForItem(unqualifiedId);

            return (itemName, description, modName, actualItem);
        }
    }
}
