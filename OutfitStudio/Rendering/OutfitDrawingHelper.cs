using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    /// <summary>
    /// Helper class for drawing tooltips and dropdown options in the outfit menu.
    /// </summary>
    public class OutfitDrawingHelper
    {
        private const int DropdownPanelYOffset = -4;

        private readonly OutfitUIBuilder uiBuilder;
        private readonly OutfitDropdownManager dropdownManager;
        private readonly OutfitState state;
        private readonly ModEntry mod;

        public string? HoveredTruncatedFilterText { get; private set; }

        public OutfitDrawingHelper(
            OutfitUIBuilder uiBuilder,
            OutfitDropdownManager dropdownManager,
            OutfitState state,
            ModEntry mod)
        {
            this.uiBuilder = uiBuilder;
            this.dropdownManager = dropdownManager;
            this.state = state;
            this.mod = mod;
        }

        public void DrawDropdownOptions(SpriteBatch b)
        {
            HoveredTruncatedFilterText = null;

            var options = dropdownManager.Options;
            if (options.Count == 0 || uiBuilder.ModFilterDropdown == null)
                return;

            Rectangle panelAnchor = uiBuilder.ModFilterDropdown.bounds;
            panelAnchor.Y += DropdownPanelYOffset;

            string? hoveredText = UIHelpers.DrawDropdownOptions(
                b,
                panelAnchor,
                options,
                dropdownManager.FirstVisibleIndex,
                dropdownManager.MaxVisibleItems,
                isSelected: null,
                enableTruncation: true
            );

            // Only show tooltip if config allows it
            if (hoveredText != null && mod.GetConfig().ShowTooltip)
            {
                HoveredTruncatedFilterText = hoveredText;
            }
        }

        public void DrawFilterTooltip(SpriteBatch b, string filterText)
        {
            Vector2 textSize = Game1.smallFont.MeasureString(filterText);
            int tooltipWidth = (int)textSize.X + TooltipPadding * 2;
            int tooltipHeight = (int)textSize.Y + TooltipPadding * 2;

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            int tooltipX = mouseX + TooltipPadding * 2;
            int tooltipY = mouseY + TooltipPadding * 2;

            if (tooltipX + tooltipWidth > Game1.uiViewport.Width)
                tooltipX = mouseX - tooltipWidth - 8;
            if (tooltipY + tooltipHeight > Game1.uiViewport.Height)
                tooltipY = mouseY - tooltipHeight - 8;

            IClickableMenu.drawTextureBox(b, tooltipX, tooltipY, tooltipWidth, tooltipHeight, Color.White);
            Utility.drawTextWithShadow(b, filterText, Game1.smallFont, new Vector2(tooltipX + TooltipPadding, tooltipY + TooltipPadding), Game1.textColor);
        }

        public void DrawLookupTooltip(SpriteBatch b)
        {
            string shirtName = GetCurrentEquippedName(OutfitCategoryManager.Category.Shirts);
            string pantsName = GetCurrentEquippedName(OutfitCategoryManager.Category.Pants);
            string hatName = GetCurrentEquippedName(OutfitCategoryManager.Category.Hats);

            // Add "+" prefix for unsaved items
            string shirtPrefix = state.IsShirtUnsaved ? "+ " : "";
            string pantsPrefix = state.IsPantsUnsaved ? "+ " : "";
            string hatPrefix = state.IsHatUnsaved ? "+ " : "";

            int maxTooltipWidth = uiBuilder.Width / 2;
            int contentWidth = maxTooltipWidth - TooltipPadding * 2;

            string shirtLine = UIHelpers.TruncateText($"{shirtPrefix}{TranslationCache.LookupShirt}: {shirtName}", contentWidth);
            string pantsLine = UIHelpers.TruncateText($"{pantsPrefix}{TranslationCache.LookupPants}: {pantsName}", contentWidth);
            string hatLine = UIHelpers.TruncateText($"{hatPrefix}{TranslationCache.LookupHat}: {hatName}", contentWidth);

            Vector2 shirtSize = Game1.smallFont.MeasureString(shirtLine);
            Vector2 pantsSize = Game1.smallFont.MeasureString(pantsLine);
            Vector2 hatSize = Game1.smallFont.MeasureString(hatLine);

            int tooltipWidth = maxTooltipWidth;
            int tooltipHeight = (int)(shirtSize.Y + pantsSize.Y + hatSize.Y) + 32;

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            int tooltipX = mouseX + TooltipPadding * 2;
            int tooltipY = mouseY + TooltipPadding * 2;

            if (tooltipX + tooltipWidth > Game1.uiViewport.Width)
                tooltipX = mouseX - tooltipWidth - 8;
            if (tooltipY + tooltipHeight > Game1.uiViewport.Height)
                tooltipY = mouseY - tooltipHeight - 8;

            IClickableMenu.drawTextureBox(b, tooltipX, tooltipY, tooltipWidth, tooltipHeight, Color.White);

            int textX = tooltipX + TooltipPadding;
            int textY = tooltipY + TooltipPadding;
            int lineHeight = (int)shirtSize.Y;

            // Draw each line with bold effect if unsaved
            DrawLookupLine(b, shirtLine, new Vector2(textX, textY), state.IsShirtUnsaved);
            textY += lineHeight;
            DrawLookupLine(b, pantsLine, new Vector2(textX, textY), state.IsPantsUnsaved);
            textY += lineHeight;
            DrawLookupLine(b, hatLine, new Vector2(textX, textY), state.IsHatUnsaved);
        }

        private void DrawLookupLine(SpriteBatch b, string text, Vector2 position, bool isBold)
        {
            if (isBold)
            {
                Utility.drawTextWithShadow(b, text, Game1.smallFont, position + new Vector2(-1, 0), Game1.textColor);
                Utility.drawTextWithShadow(b, text, Game1.smallFont, position + new Vector2(1, 0), Game1.textColor);
                Utility.drawTextWithShadow(b, text, Game1.smallFont, position, Game1.textColor);
            }
            else
            {
                Utility.drawTextWithShadow(b, text, Game1.smallFont, position, Game1.textColor);
            }
        }

        public string GetCurrentEquippedName(OutfitCategoryManager.Category category)
        {
            switch (category)
            {
                case OutfitCategoryManager.Category.Shirts:
                    var shirt = Game1.player.shirtItem.Value;
                    if (shirt == null)
                        return TranslationCache.ItemNoShirt;
                    return shirt.DisplayName ?? OutfitState.GetClothingId(shirt);

                case OutfitCategoryManager.Category.Pants:
                    var pants = Game1.player.pantsItem.Value;
                    if (pants == null)
                        return TranslationCache.ItemNoPants;
                    return pants.DisplayName ?? OutfitState.GetClothingId(pants);

                case OutfitCategoryManager.Category.Hats:
                    var hat = Game1.player.hat.Value;
                    if (hat == null)
                        return TranslationCache.ItemNoHat;

                    string hatId = hat.ItemId;
                    if (string.IsNullOrEmpty(hatId))
                        return TranslationCache.ItemNoHat;

                    string qualifiedId = hatId.StartsWith("(H)") ? hatId : "(H)" + hatId;
                    try
                    {
                        var itemData = ItemRegistry.GetDataOrErrorItem(qualifiedId);
                        return itemData.DisplayName ?? hatId;
                    }
                    catch
                    {
                        return hatId;
                    }

                default:
                    return "";
            }
        }
    }
}
