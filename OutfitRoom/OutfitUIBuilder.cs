using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace OutfitRoom
{
    /// <summary>
    /// Builds and manages UI components for the OutfitMenu.
    /// </summary>
    public class OutfitUIBuilder
    {
        // Fixed layout constants
        private const int MENU_HEIGHT = 700;
        private const int SLOT_SIZE_BASE = 72;
        private const int SLOT_GAP = 4;
        private const int TAB_WIDTH = 80;
        private const int TAB_HEIGHT = 40;
        private const int TAB_GAP_BASE = 8;
        private const int PADDING = 16;
        private const int OUTFIT_PANEL_COLUMNS = 5;

        // Computed layout values
        public int SLOT_SIZE { get; private set; }
        public int VISIBLE_ITEMS { get; private set; }
        public int VISIBLE_ROWS { get; private set; }
        public int COLUMNS { get; private set; }

        // UI Components
        public ClickableComponent ShirtsTab { get; private set; } = null!;
        public ClickableComponent PantsTab { get; private set; } = null!;
        public ClickableComponent HatsTab { get; private set; } = null!;
        public ClickableTextureComponent ScrollUpButton { get; private set; } = null!;
        public ClickableTextureComponent ScrollDownButton { get; private set; } = null!;
        public ClickableTextureComponent ResetButton { get; private set; } = null!;
        public ClickableTextureComponent ApplyButton { get; private set; } = null!;
        public ClickableTextureComponent CloseButton { get; private set; } = null!;
        public List<ClickableComponent> ItemSlots { get; private set; } = new();
        public Rectangle PortraitBox { get; private set; }

        // Menu position and dimensions
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        /// <summary>
        /// Creates a new UI builder and initializes all UI components.
        /// </summary>
        public OutfitUIBuilder()
        {
            Recalculate();
        }

        /// <summary>
        /// Recalculates all UI component positions based on current viewport.
        /// Call this when the game window is resized.
        /// </summary>
        public void Recalculate()
        {
            // Use fixed slot size like vanilla inventory
            SLOT_SIZE = SLOT_SIZE_BASE;
            COLUMNS = OUTFIT_PANEL_COLUMNS;

            // Calculate actual content dimensions first
            int gridWidth = COLUMNS * SLOT_SIZE + (COLUMNS - 1) * SLOT_GAP;  // 336
            int portraitWidth = 128;
            int portraitHeight = 192;
            int gapBetweenPanels = 100;
            int sidePadding = 70;

            // Calculate menu width based on actual content
            int contentWidth = portraitWidth + gapBetweenPanels + gridWidth + PADDING * 2;
            Width = contentWidth + sidePadding * 2;
            Height = MENU_HEIGHT;

            // Center menu on screen
            X = (Game1.uiViewport.Width - Width) / 2;
            Y = (Game1.uiViewport.Height - Height) / 2;

            // Calculate panel positions (centered within menu)
            int contentStartX = X + sidePadding;
            int leftPanelCenterX = contentStartX + portraitWidth / 2;
            int rightPanelX = contentStartX + portraitWidth + gapBetweenPanels;

            // Vertical layout
            int titleHeight = 48;
            int tabY = Y + PADDING + titleHeight;
            int gridY = tabY + TAB_HEIGHT + PADDING;

            // Calculate visible rows based on available height
            int availableHeight = Height - (gridY - Y) - PADDING - 60;
            VISIBLE_ROWS = Math.Max(1, (availableHeight + SLOT_GAP) / (SLOT_SIZE + SLOT_GAP));
            VISIBLE_ITEMS = VISIBLE_ROWS * COLUMNS;

            // Portrait box - centered in left panel area
            int portraitY = Y + PADDING + titleHeight + (Height - titleHeight - PADDING * 2 - 60 - portraitHeight) / 2;
            PortraitBox = new Rectangle(
                leftPanelCenterX - portraitWidth / 2,
                portraitY,
                portraitWidth,
                portraitHeight
            );

            // Category tabs - aligned with grid
            ShirtsTab = new ClickableComponent(
                new Rectangle(rightPanelX, tabY, TAB_WIDTH, TAB_HEIGHT),
                "Shirts"
            );
            PantsTab = new ClickableComponent(
                new Rectangle(rightPanelX + TAB_WIDTH + TAB_GAP_BASE, tabY, TAB_WIDTH, TAB_HEIGHT),
                "Pants"
            );
            HatsTab = new ClickableComponent(
                new Rectangle(rightPanelX + (TAB_WIDTH + TAB_GAP_BASE) * 2, tabY, TAB_WIDTH, TAB_HEIGHT),
                "Hats"
            );

            // Item slots grid
            int gridHeight = VISIBLE_ROWS * SLOT_SIZE + Math.Max(0, VISIBLE_ROWS - 1) * SLOT_GAP;
            ItemSlots.Clear();
            for (int row = 0; row < VISIBLE_ROWS; row++)
            {
                for (int col = 0; col < COLUMNS; col++)
                {
                    int slotX = rightPanelX + col * (SLOT_SIZE + SLOT_GAP);
                    int slotY = gridY + row * (SLOT_SIZE + SLOT_GAP);
                    ItemSlots.Add(new ClickableComponent(
                        new Rectangle(slotX, slotY, SLOT_SIZE, SLOT_SIZE),
                        (row * COLUMNS + col).ToString()
                    ));
                }
            }

            // Scroll buttons (above and below the grid)
            int scrollButtonSize = 44;
            ScrollUpButton = new ClickableTextureComponent(
                new Rectangle(rightPanelX + gridWidth / 2 - scrollButtonSize / 2, gridY - scrollButtonSize - 4, scrollButtonSize, scrollButtonSize),
                Game1.mouseCursors,
                new Rectangle(421, 459, 12, 11),
                4f
            );
            ScrollDownButton = new ClickableTextureComponent(
                new Rectangle(rightPanelX + gridWidth / 2 - scrollButtonSize / 2, gridY + gridHeight + 4, scrollButtonSize, scrollButtonSize),
                Game1.mouseCursors,
                new Rectangle(421, 472, 12, 11),
                4f
            );

            // Apply and Reset buttons - side by side below the portrait
            int buttonWidth = 110;
            int buttonHeight = 60;
            int buttonGap = 8;
            int totalButtonsWidth = buttonWidth * 2 + buttonGap;
            int buttonsStartX = leftPanelCenterX - totalButtonsWidth / 2;
            int buttonsY = Y + Height - PADDING - buttonHeight - 8;

            ApplyButton = new ClickableTextureComponent(
                new Rectangle(buttonsStartX, buttonsY, buttonWidth, buttonHeight),
                Game1.mouseCursors,
                new Rectangle(432, 439, 9, 9),
                4f
            );
            ResetButton = new ClickableTextureComponent(
                new Rectangle(buttonsStartX + buttonWidth + buttonGap, buttonsY, buttonWidth, buttonHeight),
                Game1.mouseCursors,
                new Rectangle(432, 439, 9, 9),
                4f
            );

            // Close button (top right)
            int closeSize = 48;
            CloseButton = new ClickableTextureComponent(
                new Rectangle(X + Width - closeSize - 8, Y + 8, closeSize, closeSize),
                Game1.mouseCursors,
                new Rectangle(337, 494, 12, 12),
                4f
            );
        }

        /// <summary>
        /// Draws a category tab.
        /// </summary>
        /// <param name="b">SpriteBatch to draw with.</param>
        /// <param name="tab">The tab component.</param>
        /// <param name="label">The label text.</param>
        /// <param name="isActive">Whether the tab is currently selected.</param>
        public void DrawTab(SpriteBatch b, ClickableComponent tab, string label, bool isActive)
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

        /// <summary>
        /// Draws the player preview (background and farmer).
        /// </summary>
        /// <param name="b">SpriteBatch to draw with.</param>
        public void DrawPlayerPreview(SpriteBatch b)
        {
            // Draw sky background (day or night based on time)
            b.Draw((Game1.timeOfDay >= 1900) ? Game1.nightbg : Game1.daybg, PortraitBox, Color.White);

            // Draw farmer using vanilla approach from InventoryPage
            // Farmer sprite is 16x32 at base, renders at ~64x128 pixels (4x internal zoom)
            // Position is top-left corner since origin is Vector2.Zero
            Vector2 farmerPos = new Vector2(
                PortraitBox.Center.X - 32,  // Center horizontally (64px wide / 2)
                PortraitBox.Center.Y - 64   // Center vertically (128px tall / 2)
            );

            FarmerRenderer.isDrawingForUI = true;
            Game1.player.FarmerRenderer.draw(
                b,
                new FarmerSprite.AnimationFrame(0, Game1.player.bathingClothes.Value ? 108 : 0, secondaryArm: false, flip: false),
                Game1.player.bathingClothes.Value ? 108 : 0,
                new Rectangle(0, Game1.player.bathingClothes.Value ? 576 : 0, 16, 32),
                farmerPos,
                Vector2.Zero,
                0.8f,
                2,
                Color.White,
                0f,
                1f,
                Game1.player
            );

            // Apply night overlay if needed (same as vanilla)
            if (Game1.timeOfDay >= 1900)
            {
                Game1.player.FarmerRenderer.draw(
                    b,
                    new FarmerSprite.AnimationFrame(0, Game1.player.bathingClothes.Value ? 108 : 0, secondaryArm: false, flip: false),
                    Game1.player.bathingClothes.Value ? 108 : 0,
                    new Rectangle(0, Game1.player.bathingClothes.Value ? 576 : 0, 16, 32),
                    farmerPos,
                    Vector2.Zero,
                    0.8f,
                    2,
                    Color.DarkBlue * 0.3f,
                    0f,
                    1f,
                    Game1.player
                );
            }
            FarmerRenderer.isDrawingForUI = false;
        }

        /// <summary>
        /// Draws the item list background and scroll buttons if needed.
        /// </summary>
        /// <param name="b">SpriteBatch to draw with.</param>
        /// <param name="scrollOffset">Current scroll offset.</param>
        /// <param name="totalItems">Total number of items in current category.</param>
        public void DrawItemList(SpriteBatch b, int scrollOffset, int totalItems)
        {
            if (ItemSlots.Count == 0)
                return;

            int listX = ItemSlots[0].bounds.X;
            int listY = ItemSlots[0].bounds.Y;
            int listWidth = (ItemSlots[COLUMNS - 1].bounds.Right - ItemSlots[0].bounds.Left);
            int listHeight = VISIBLE_ROWS * SLOT_SIZE + Math.Max(0, VISIBLE_ROWS - 1) * SLOT_GAP;

            // Draw background box
            IClickableMenu.drawTextureBox(b, listX - PADDING, listY - PADDING,
                listWidth + PADDING * 2, listHeight + PADDING * 2, Color.White);

            // Draw scroll buttons (only if needed)
            int totalRows = Math.Max(1, (int)Math.Ceiling(totalItems / (float)COLUMNS));
            int maxScroll = Math.Max(0, totalRows - VISIBLE_ROWS);
            if (scrollOffset > 0)
                ScrollUpButton.draw(b);
            if (scrollOffset < maxScroll)
                ScrollDownButton.draw(b);
        }

        /// <summary>
        /// Draws the apply button with its label.
        /// </summary>
        /// <param name="b">SpriteBatch to draw with.</param>
        public void DrawApplyButton(SpriteBatch b)
        {
            // Draw button background using texture box
            IClickableMenu.drawTextureBox(b, ApplyButton.bounds.X, ApplyButton.bounds.Y,
                ApplyButton.bounds.Width, ApplyButton.bounds.Height, Color.White);

            // Draw "Apply" text centered in button
            string applyText = "Apply";
            Vector2 textSize = Game1.smallFont.MeasureString(applyText);
            Vector2 textPos = new Vector2(
                ApplyButton.bounds.X + (ApplyButton.bounds.Width - textSize.X) / 2,
                ApplyButton.bounds.Y + (ApplyButton.bounds.Height - textSize.Y) / 2
            );
            Utility.drawTextWithShadow(b, applyText, Game1.smallFont, textPos, Game1.textColor);
        }

        /// <summary>
        /// Draws the reset button with its label.
        /// </summary>
        /// <param name="b">SpriteBatch to draw with.</param>
        public void DrawResetButton(SpriteBatch b)
        {
            // Draw button background using texture box
            IClickableMenu.drawTextureBox(b, ResetButton.bounds.X, ResetButton.bounds.Y,
                ResetButton.bounds.Width, ResetButton.bounds.Height, Color.White);

            // Draw "Reset" text centered in button
            string resetText = "Reset";
            Vector2 textSize = Game1.smallFont.MeasureString(resetText);
            Vector2 textPos = new Vector2(
                ResetButton.bounds.X + (ResetButton.bounds.Width - textSize.X) / 2,
                ResetButton.bounds.Y + (ResetButton.bounds.Height - textSize.Y) / 2
            );
            Utility.drawTextWithShadow(b, resetText, Game1.smallFont, textPos, Game1.textColor);
        }

        /// <summary>
        /// Draws the close button.
        /// </summary>
        /// <param name="b">SpriteBatch to draw with.</param>
        public void DrawCloseButton(SpriteBatch b)
        {
            CloseButton.draw(b);
        }

        /// <summary>
        /// Draws the menu title.
        /// </summary>
        /// <param name="b">SpriteBatch to draw with.</param>
        public void DrawTitle(SpriteBatch b)
        {
            string title = "Outfit Room";
            Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
            Utility.drawTextWithShadow(b, title, Game1.dialogueFont,
                new Vector2(X + (Width - titleSize.X) / 2, Y + PADDING),
                Game1.textColor);
        }

        /// <summary>
        /// Draws the semi‑transparent background overlay.
        /// </summary>
        /// <param name="b">SpriteBatch to draw with.</param>
        public static void DrawOverlay(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.6f);
        }
    }
}
