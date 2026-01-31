using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Objects;

namespace OutfitRoom
{
    public class OutfitMenu : IClickableMenu
    {
        // Category enum
        private enum Category { Shirts, Pants, Hats }
        private Category currentCategory = Category.Shirts;

        // Layout constants
        private const int MENU_WIDTH = 900;
        private const int MENU_HEIGHT = 550;
        private const int LEFT_PANEL_WIDTH = 280;
        private const int RIGHT_PANEL_X_OFFSET = 320;
        private const int COLUMNS_PER_ROW = 5;
        private const int ROWS_VISIBLE = 3;
        private const int VISIBLE_ITEMS = COLUMNS_PER_ROW * ROWS_VISIBLE; // 15
        private const int ITEM_WIDTH = 80;
        private const int ITEM_HEIGHT = 48;

        // Spacing constants
        private const int TAB_GAP = 12;
        private const int ITEM_SLOT_GAP = 4;
        private const int LIST_PADDING = 12;
        private const int SPRITE_SIZE = 40;
        private const int BUTTON_LABEL_GAP = 4;

        // Clothing data
        private readonly List<string> shirtIds = new();
        private readonly List<string> pantsIds = new();
        private readonly List<int> hatIds = new();

        // Current selection indices
        private int shirtIndex;
        private int pantsIndex;
        private int hatIndex;

        // Original outfit (for reset)
        private readonly string originalShirt;
        private readonly string originalPants;
        private readonly int originalHat;

        // Saved outfit (for Set)
        private string savedShirt;
        private string savedPants;
        private int savedHat;

        // Scroll state
        private int scrollOffset = 0;

        // UI Components
        private readonly ClickableComponent shirtsTab;
        private readonly ClickableComponent pantsTab;
        private readonly ClickableComponent hatsTab;
        private readonly ClickableTextureComponent scrollUpButton;
        private readonly ClickableTextureComponent scrollDownButton;
        private readonly ClickableTextureComponent resetButton;
        private readonly ClickableTextureComponent setButton;
        private readonly ClickableTextureComponent closeButton;
        private readonly List<ClickableComponent> itemSlots = new();

        // Player preview area
        private Rectangle portraitBox;

        public OutfitMenu(ModEntry mod)
        {
            // Set up menu dimensions
            width = MENU_WIDTH;
            height = MENU_HEIGHT;
            Vector2 origin = Utility.getTopLeftPositionForCenteringOnScreen(width, height);
            xPositionOnScreen = (int)origin.X;
            yPositionOnScreen = (int)origin.Y;

            // Store original outfit for reset
            originalShirt = Game1.player.shirt.Value;
            originalPants = Game1.player.pants.Value;
            originalHat = GetHatIdFromItem(Game1.player.hat.Value);

            // Populate clothing lists
            foreach (var id in Game1.shirtData.Keys)
                shirtIds.Add(id);
            foreach (var id in Game1.pantsData.Keys)
                pantsIds.Add(id);

            // Populate hat list (-1 = no hat, then 0 to max)
            hatIds.Add(-1);
            int maxHats = FarmerRenderer.hatsTexture.Height / 80 * 12;
            for (int i = 0; i < maxHats; i++)
                hatIds.Add(i);

            // Find current indices
            shirtIndex = Math.Max(0, shirtIds.IndexOf(Game1.player.shirt.Value));
            pantsIndex = Math.Max(0, pantsIds.IndexOf(Game1.player.pants.Value));
            hatIndex = Math.Max(0, hatIds.IndexOf(originalHat));

            // Player portrait box (left panel)
            portraitBox = new Rectangle(
                xPositionOnScreen + 40,
                yPositionOnScreen + 100,
                128,
                192
            );

            // Category tabs (right panel top)
            int tabY = yPositionOnScreen + 60;
            int tabWidth = 100;
            int tabHeight = 40;
            int rightPanelX = xPositionOnScreen + RIGHT_PANEL_X_OFFSET;

            shirtsTab = new ClickableComponent(
                new Rectangle(rightPanelX, tabY, tabWidth, tabHeight),
                "Shirts"
            );
            pantsTab = new ClickableComponent(
                new Rectangle(rightPanelX + tabWidth + TAB_GAP, tabY, tabWidth, tabHeight),
                "Pants"
            );
            hatsTab = new ClickableComponent(
                new Rectangle(rightPanelX + (tabWidth + TAB_GAP) * 2, tabY, tabWidth, tabHeight),
                "Hats"
            );

            // Scroll buttons
            int listX = rightPanelX;
            int listY = tabY + tabHeight + 20;
            int listWidth = 400;

            scrollUpButton = new ClickableTextureComponent(
                new Rectangle(listX + listWidth / 2 - 24, listY - 10, 48, 44),
                Game1.mouseCursors,
                new Rectangle(421, 459, 12, 11),
                4f
            );
            scrollDownButton = new ClickableTextureComponent(
                new Rectangle(listX + listWidth / 2 - 24, listY + VISIBLE_ITEMS * ITEM_HEIGHT + 10, 48, 44),
                Game1.mouseCursors,
                new Rectangle(421, 472, 12, 11),
                4f
            );

            // Item slots
            for (int i = 0; i < VISIBLE_ITEMS; i++)
            {
                itemSlots.Add(new ClickableComponent(
                    new Rectangle(listX, listY + i * ITEM_HEIGHT, listWidth, ITEM_HEIGHT - ITEM_SLOT_GAP),
                    i.ToString()
                ));
            }

            // Reset button (left panel bottom) - bounds match visual size at 4f scale
            resetButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + 60, yPositionOnScreen + height - 100, 84, 44),
                Game1.mouseCursors,
                new Rectangle(294, 428, 21, 11),
                4f
            );

            // Close button (top right)
            closeButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + width - 60, yPositionOnScreen + 10, 48, 48),
                Game1.mouseCursors,
                new Rectangle(337, 494, 12, 12),
                4f
            );
        }

        private List<string> GetCurrentList()
        {
            return currentCategory switch
            {
                Category.Shirts => shirtIds,
                Category.Pants => pantsIds,
                _ => null
            };
        }

        private int GetCurrentListCount()
        {
            return currentCategory switch
            {
                Category.Shirts => shirtIds.Count,
                Category.Pants => pantsIds.Count,
                Category.Hats => hatIds.Count,
                _ => 0
            };
        }

        private int GetCurrentIndex()
        {
            return currentCategory switch
            {
                Category.Shirts => shirtIndex,
                Category.Pants => pantsIndex,
                Category.Hats => hatIndex,
                _ => 0
            };
        }

        private void SetCurrentIndex(int index)
        {
            switch (currentCategory)
            {
                case Category.Shirts:
                    shirtIndex = index;
                    break;
                case Category.Pants:
                    pantsIndex = index;
                    break;
                case Category.Hats:
                    hatIndex = index;
                    break;
            }
        }

        private string GetItemDisplayName(int listIndex)
        {
            switch (currentCategory)
            {
                case Category.Shirts:
                    if (listIndex >= 0 && listIndex < shirtIds.Count)
                    {
                        string id = shirtIds[listIndex];
                        if (Game1.shirtData.TryGetValue(id, out var data))
                            return data.DisplayName ?? id;
                        return id;
                    }
                    break;
                case Category.Pants:
                    if (listIndex >= 0 && listIndex < pantsIds.Count)
                    {
                        string id = pantsIds[listIndex];
                        if (Game1.pantsData.TryGetValue(id, out var data))
                            return data.DisplayName ?? id;
                        return id;
                    }
                    break;
                case Category.Hats:
                    if (listIndex >= 0 && listIndex < hatIds.Count)
                    {
                        int hatId = hatIds[listIndex];
                        if (hatId == -1)
                            return "(No Hat)";
                        return $"Hat #{hatId}";
                    }
                    break;
            }
            return "???";
        }

        private void DrawItemSprite(SpriteBatch b, int listIndex, Rectangle slot, bool isSelected)
        {
            string qualifiedId = null;

            switch (currentCategory)
            {
                case Category.Shirts:
                    if (listIndex >= 0 && listIndex < shirtIds.Count)
                        qualifiedId = "(S)" + shirtIds[listIndex];
                    break;
                case Category.Pants:
                    if (listIndex >= 0 && listIndex < pantsIds.Count)
                        qualifiedId = "(P)" + pantsIds[listIndex];
                    break;
                case Category.Hats:
                    if (listIndex >= 0 && listIndex < hatIds.Count)
                    {
                        int hatId = hatIds[listIndex];
                        if (hatId >= 0)
                            qualifiedId = "(H)" + hatId;
                    }
                    break;
            }

            if (qualifiedId != null)
            {
                ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(qualifiedId);
                Texture2D texture = itemData.GetTexture();
                Rectangle sourceRect = itemData.GetSourceRect();

                // Calculate scale to fit in slot while maintaining aspect ratio
                float scale = Math.Min(
                    (float)(SPRITE_SIZE - 8) / sourceRect.Width,
                    (float)(ITEM_HEIGHT - 8) / sourceRect.Height
                );

                // Center sprite in slot
                Vector2 position = new Vector2(
                    slot.X + (slot.Width - sourceRect.Width * scale) / 2,
                    slot.Y + (ITEM_HEIGHT - sourceRect.Height * scale) / 2
                );

                // Draw with selection tint
                Color tint = isSelected ? Color.White : Color.White * 0.9f;
                b.Draw(texture, position, sourceRect, tint, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.9f);
            }
            else if (currentCategory == Category.Hats && listIndex == 0)
            {
                // Draw "No Hat" indicator (empty slot)
                Vector2 textPos = new Vector2(
                    slot.X + (slot.Width - Game1.smallFont.MeasureString("X").X) / 2,
                    slot.Y + (ITEM_HEIGHT - Game1.smallFont.LineSpacing) / 2
                );
                Utility.drawTextWithShadow(b, "X", Game1.smallFont, textPos, Color.Gray);
            }
        }

        private void ApplyCurrentSelection()
        {
            switch (currentCategory)
            {
                case Category.Shirts:
                    if (shirtIndex >= 0 && shirtIndex < shirtIds.Count)
                    {
                        Game1.player.shirt.Value = shirtIds[shirtIndex];
                        Game1.player.FarmerRenderer.MarkSpriteDirty();
                    }
                    break;
                case Category.Pants:
                    if (pantsIndex >= 0 && pantsIndex < pantsIds.Count)
                    {
                        Game1.player.pants.Value = pantsIds[pantsIndex];
                        Game1.player.FarmerRenderer.MarkSpriteDirty();
                    }
                    break;
                case Category.Hats:
                    if (hatIndex >= 0 && hatIndex < hatIds.Count)
                    {
                        int hatId = hatIds[hatIndex];
                        if (hatId < 0)
                            Game1.player.hat.Value = null;
                        else
                            Game1.player.hat.Value = ItemRegistry.Create<Hat>("(H)" + hatId);
                    }
                    break;
            }
        }

        private void ResetOutfit()
        {
            // Reset shirt
            Game1.player.shirt.Value = originalShirt;

            // Reset pants
            Game1.player.pants.Value = originalPants;

            // Reset hat
            if (originalHat < 0)
                Game1.player.hat.Value = null;
            else
                Game1.player.hat.Value = ItemRegistry.Create<Hat>("(H)" + originalHat);

            Game1.player.FarmerRenderer.MarkSpriteDirty();

            // Reset indices
            shirtIndex = Math.Max(0, shirtIds.IndexOf(originalShirt));
            pantsIndex = Math.Max(0, pantsIds.IndexOf(originalPants));
            hatIndex = Math.Max(0, hatIds.IndexOf(originalHat));
            scrollOffset = 0;
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            // Category tabs
            if (shirtsTab.containsPoint(x, y))
            {
                currentCategory = Category.Shirts;
                scrollOffset = 0;
                if (playSound) Game1.playSound("smallSelect");
                return;
            }
            if (pantsTab.containsPoint(x, y))
            {
                currentCategory = Category.Pants;
                scrollOffset = 0;
                if (playSound) Game1.playSound("smallSelect");
                return;
            }
            if (hatsTab.containsPoint(x, y))
            {
                currentCategory = Category.Hats;
                scrollOffset = 0;
                if (playSound) Game1.playSound("smallSelect");
                return;
            }

            // Scroll buttons
            int maxScroll = Math.Max(0, GetCurrentListCount() - VISIBLE_ITEMS);
            if (scrollUpButton.containsPoint(x, y) && scrollOffset > 0)
            {
                scrollOffset--;
                if (playSound) Game1.playSound("shwip");
                return;
            }
            if (scrollDownButton.containsPoint(x, y) && scrollOffset < maxScroll)
            {
                scrollOffset++;
                if (playSound) Game1.playSound("shwip");
                return;
            }

            // Item slots
            for (int i = 0; i < itemSlots.Count; i++)
            {
                if (itemSlots[i].containsPoint(x, y))
                {
                    int listIndex = scrollOffset + i;
                    if (listIndex < GetCurrentListCount())
                    {
                        SetCurrentIndex(listIndex);
                        ApplyCurrentSelection();
                        if (playSound) Game1.playSound("stoneStep");
                    }
                    return;
                }
            }

            // Reset button
            if (resetButton.containsPoint(x, y))
            {
                ResetOutfit();
                if (playSound) Game1.playSound("drumkit6");
                return;
            }

            // Close button
            if (closeButton.containsPoint(x, y))
            {
                exitThisMenu();
                if (playSound) Game1.playSound("bigDeSelect");
            }
        }

        public override void receiveScrollWheelAction(int direction)
        {
            base.receiveScrollWheelAction(direction);

            int maxScroll = Math.Max(0, GetCurrentListCount() - VISIBLE_ITEMS);
            if (direction > 0 && scrollOffset > 0)
            {
                scrollOffset--;
                Game1.playSound("shiny4");
            }
            else if (direction < 0 && scrollOffset < maxScroll)
            {
                scrollOffset++;
                Game1.playSound("shiny4");
            }
        }

        public override void draw(SpriteBatch b)
        {
            // Draw semi-transparent background overlay
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.6f);

            // Draw menu box
            drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

            // Draw title
            string title = "Outfit Room";
            Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
            Utility.drawTextWithShadow(b, title, Game1.dialogueFont,
                new Vector2(xPositionOnScreen + (width - titleSize.X) / 2, yPositionOnScreen + 16),
                Game1.textColor);

            // === LEFT PANEL: Player Preview ===
            // Draw sky background
            b.Draw(Game1.daybg, new Vector2(portraitBox.X, portraitBox.Y), Color.White);

            // Draw farmer
            FarmerRenderer.isDrawingForUI = true;
            Game1.player.FarmerRenderer.draw(
                b,
                new FarmerSprite.AnimationFrame(0, 0, false, false),
                0,
                new Rectangle(0, 0, 16, 32),
                new Vector2(portraitBox.X + 32, portraitBox.Y + 32),
                Vector2.Zero,
                0.8f,
                2,
                Color.White,
                0f,
                1f,
                Game1.player
            );
            FarmerRenderer.isDrawingForUI = false;

            // Draw reset button
            resetButton.draw(b);
            Vector2 resetLabelSize = Game1.smallFont.MeasureString("Reset");
            Utility.drawTextWithShadow(b, "Reset", Game1.smallFont,
                new Vector2(
                    resetButton.bounds.X + (resetButton.bounds.Width - resetLabelSize.X) / 2,
                    resetButton.bounds.Y + resetButton.bounds.Height + BUTTON_LABEL_GAP
                ),
                Game1.textColor);

            // === RIGHT PANEL: Category Tabs & Item List ===
            // Draw category tabs
            DrawTab(b, shirtsTab, "Shirts", currentCategory == Category.Shirts);
            DrawTab(b, pantsTab, "Pants", currentCategory == Category.Pants);
            DrawTab(b, hatsTab, "Hats", currentCategory == Category.Hats);

            // Draw item list background
            int listX = itemSlots[0].bounds.X;
            int listY = itemSlots[0].bounds.Y;
            int listWidth = itemSlots[0].bounds.Width;
            int listHeight = VISIBLE_ITEMS * ITEM_HEIGHT;
            drawTextureBox(b, listX - LIST_PADDING, listY - LIST_PADDING, listWidth + LIST_PADDING * 2, listHeight + LIST_PADDING * 2, Color.White);

            // Draw items
            int currentIndex = GetCurrentIndex();
            int listCount = GetCurrentListCount();

            for (int i = 0; i < VISIBLE_ITEMS; i++)
            {
                int listIndex = scrollOffset + i;
                if (listIndex >= listCount)
                    break;

                Rectangle slot = itemSlots[i].bounds;
                bool isSelected = listIndex == currentIndex;

                // Highlight selected item
                if (isSelected)
                {
                    b.Draw(Game1.staminaRect, slot, Color.Orange * 0.4f);
                }

                // Hover highlight
                if (itemSlots[i].containsPoint(Game1.getMouseX(), Game1.getMouseY()))
                {
                    b.Draw(Game1.staminaRect, slot, Color.White * 0.2f);
                }

                // Draw item sprite (no text)
                DrawItemSprite(b, listIndex, slot, isSelected);
            }

            // Draw scroll buttons (only if needed)
            if (scrollOffset > 0)
                scrollUpButton.draw(b);
            if (scrollOffset < listCount - VISIBLE_ITEMS)
                scrollDownButton.draw(b);

            // Draw close button
            closeButton.draw(b);

            // Draw cursor
            drawMouse(b);
        }

        private void DrawTab(SpriteBatch b, ClickableComponent tab, string label, bool isActive)
        {
            Color bgColor = isActive ? Color.Orange : Color.Gray;
            b.Draw(Game1.staminaRect, tab.bounds, bgColor * 0.7f);

            // Draw border
            int borderWidth = 2;
            b.Draw(Game1.staminaRect, new Rectangle(tab.bounds.X, tab.bounds.Y, tab.bounds.Width, borderWidth), Color.Brown);
            b.Draw(Game1.staminaRect, new Rectangle(tab.bounds.X, tab.bounds.Bottom - borderWidth, tab.bounds.Width, borderWidth), Color.Brown);
            b.Draw(Game1.staminaRect, new Rectangle(tab.bounds.X, tab.bounds.Y, borderWidth, tab.bounds.Height), Color.Brown);
            b.Draw(Game1.staminaRect, new Rectangle(tab.bounds.Right - borderWidth, tab.bounds.Y, borderWidth, tab.bounds.Height), Color.Brown);

            // Draw label centered
            Vector2 labelSize = Game1.smallFont.MeasureString(label);
            Utility.drawTextWithShadow(b, label, Game1.smallFont,
                new Vector2(
                    tab.bounds.X + (tab.bounds.Width - labelSize.X) / 2,
                    tab.bounds.Y + (tab.bounds.Height - labelSize.Y) / 2
                ),
                isActive ? Color.White : Game1.textColor);
        }

        private static int GetHatIdFromItem(StardewValley.Objects.Hat hat)
        {
            if (hat == null)
                return -1;

            // ItemId is like "(H)5", extract the number
            string itemId = hat.ItemId;
            if (string.IsNullOrEmpty(itemId))
                return -1;

            // Remove the "(H)" prefix if present
            if (itemId.StartsWith("(H)"))
                itemId = itemId.Substring(3);

            if (int.TryParse(itemId, out int hatId))
                return hatId;

            return -1;
        }
    }
}
