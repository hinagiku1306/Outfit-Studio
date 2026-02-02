using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using static FittingRoom.OutfitLayoutConstants;

namespace FittingRoom
{
    /// <summary>
    /// Helper class for drawing tooltips and dropdown options in the outfit menu.
    /// </summary>
    public class OutfitDrawingHelper
    {
        private readonly OutfitUIBuilder uiBuilder;
        private readonly OutfitDropdownManager dropdownManager;
        private readonly OutfitState state;
        private readonly ModEntry mod;

        private readonly Dictionary<string, string> truncatedTextCache = new();
        private int lastTruncationWidth = -1;

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

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            // Calculate total dropdown height based on VISIBLE items
            int visibleCount = 0;
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].visible)
                    visibleCount++;
            }
            if (visibleCount == 0)
                return;

            // Get dropdown position from the dropdown button
            int totalHeight = visibleCount * options[0].bounds.Height;
            int dropdownX = uiBuilder.ModFilterDropdown.bounds.X;
            int dropdownY = uiBuilder.ModFilterDropdown.bounds.Bottom;
            int dropdownWidth = uiBuilder.ModFilterDropdown.bounds.Width;

            // Draw dropdown background
            var bgColor = Color.Wheat;
            b.Draw(Game1.staminaRect, new Rectangle(dropdownX, dropdownY, dropdownWidth, totalHeight), bgColor);

            // Draw border
            var borderColor = Color.Black * 0.5f;
            int borderWidth = 1;
            b.Draw(Game1.staminaRect, new Rectangle(dropdownX, dropdownY, dropdownWidth, borderWidth), borderColor); // Top
            b.Draw(Game1.staminaRect, new Rectangle(dropdownX, dropdownY + totalHeight - borderWidth, dropdownWidth, borderWidth), borderColor); // Bottom
            b.Draw(Game1.staminaRect, new Rectangle(dropdownX, dropdownY, borderWidth, totalHeight), borderColor); // Left
            b.Draw(Game1.staminaRect, new Rectangle(dropdownX + dropdownWidth - borderWidth, dropdownY, borderWidth, totalHeight), borderColor); // Right

            // Draw scroll indicators
            bool canScrollUp = dropdownManager.FirstVisibleIndex > 0;
            bool canScrollDown = dropdownManager.FirstVisibleIndex + dropdownManager.MaxVisibleItems < options.Count;

            if (canScrollUp)
            {
                Rectangle upArrowSource = new Rectangle(421, 459, 11, 12);
                Vector2 upArrowPos = new Vector2(dropdownX + dropdownWidth - 20, dropdownY + 4);
                b.Draw(Game1.mouseCursors, upArrowPos, upArrowSource, Color.White, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 1f);
            }

            if (canScrollDown)
            {
                Rectangle downArrowSource = new Rectangle(421, 472, 11, 12);
                Vector2 downArrowPos = new Vector2(dropdownX + dropdownWidth - 20, dropdownY + totalHeight - 20);
                b.Draw(Game1.mouseCursors, downArrowPos, downArrowSource, Color.White, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 1f);
            }

            // Clear truncation cache if width changed
            int maxTextWidth = options[0].bounds.Width - FilterTextPadding * 2;
            if (lastTruncationWidth != maxTextWidth)
            {
                truncatedTextCache.Clear();
                lastTruncationWidth = maxTextWidth;
            }

            // Iterate without LINQ to avoid allocations
            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (!option.visible)
                    continue;

                bool isHovered = option.containsPoint(mouseX, mouseY);

                if (isHovered)
                {
                    b.Draw(Game1.staminaRect, option.bounds, Color.White * 0.5f);
                }

                // Get truncated text from cache or compute it
                string fullText = option.name;
                string displayText;
                bool isTruncated;

                if (truncatedTextCache.TryGetValue(fullText, out var cached))
                {
                    displayText = cached;
                    isTruncated = displayText != fullText;
                }
                else
                {
                    displayText = fullText;
                    Vector2 textSize = Game1.smallFont.MeasureString(displayText);
                    isTruncated = false;

                    if (textSize.X > maxTextWidth)
                    {
                        isTruncated = true;
                        // Binary search would be faster, but simple truncation is still better than before
                        while (textSize.X > maxTextWidth && displayText.Length > 3)
                        {
                            displayText = displayText.Substring(0, displayText.Length - 1);
                            textSize = Game1.smallFont.MeasureString(displayText + "...");
                        }
                        displayText += "...";
                    }
                    truncatedTextCache[fullText] = displayText;
                }

                Vector2 finalTextSize = Game1.smallFont.MeasureString(displayText);
                Vector2 textPos = new Vector2(
                    option.bounds.X + 12,
                    option.bounds.Y + (option.bounds.Height - finalTextSize.Y) / 2
                );

                b.DrawString(Game1.smallFont, displayText, textPos,
                    isHovered ? Color.Black : Game1.textColor);

                if (isHovered && isTruncated && mod.GetConfig().ShowFilterTooltip)
                {
                    HoveredTruncatedFilterText = fullText;
                }
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
