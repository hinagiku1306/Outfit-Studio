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
        // Base layout constants (at 1x UI scale)
        private const int BASE_MENU_WIDTH = 700;
        private const int BASE_MENU_HEIGHT = 500;
        private const int BASE_LEFT_PANEL_WIDTH = 200;
        private const int BASE_RIGHT_PANEL_X_OFFSET = 240;
        private const int BASE_ITEM_WIDTH = 400;
        private const int BASE_ITEM_HEIGHT = 48;
        private const int BASE_TAB_GAP = 12;
        private const int BASE_ITEM_SLOT_GAP = 4;
        private const int BASE_LIST_PADDING = 12;
        private const int BASE_BUTTON_LABEL_GAP = 4;
        private const int BASE_TAB_WIDTH = 100;
        private const int BASE_TAB_HEIGHT = 40;
        private const int BASE_TITLE_HEIGHT = 50;
        private const int BASE_TAB_MARGIN = 16;
        private const int BASE_BOTTOM_MARGIN = 80;

        // Scaled layout values (computed at construction)
        public int ITEM_HEIGHT { get; private set; }
        public int VISIBLE_ITEMS { get; private set; }
        public int TAB_GAP { get; private set; }
        public int ITEM_SLOT_GAP { get; private set; }
        public int LIST_PADDING { get; private set; }
        public int BUTTON_LABEL_GAP { get; private set; }

        // UI Components
        public ClickableComponent ShirtsTab { get; private set; } = null!;
        public ClickableComponent PantsTab { get; private set; } = null!;
        public ClickableComponent HatsTab { get; private set; } = null!;
        public ClickableTextureComponent ScrollUpButton { get; private set; } = null!;
        public ClickableTextureComponent ScrollDownButton { get; private set; } = null!;
        public ClickableTextureComponent ResetButton { get; private set; } = null!;
        public ClickableTextureComponent CloseButton { get; private set; } = null!;
        public List<ClickableComponent> ItemSlots { get; private set; } = new();
        public Rectangle PortraitBox { get; private set; }

        // Menu position and dimensions
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        private float uiScale = 1f;
        private int titlePaddingTop;

        /// <summary>
        /// Calculates menu dimensions based on viewport and UI scale.
        /// </summary>
        public static (int width, int height, int x, int y) CalculateMenuDimensions()
        {
            // Get viewport dimensions (already accounts for UI scale)
            int viewportWidth = Game1.uiViewport.Width;
            int viewportHeight = Game1.uiViewport.Height;

            // Calculate menu size with margins (80% of viewport, capped at base size)
            int menuWidth = Math.Min(BASE_MENU_WIDTH, (int)(viewportWidth * 0.85f));
            int menuHeight = Math.Min(BASE_MENU_HEIGHT, (int)(viewportHeight * 0.85f));

            // Center on screen
            int x = (viewportWidth - menuWidth) / 2;
            int y = (viewportHeight - menuHeight) / 2;

            return (menuWidth, menuHeight, x, y);
        }

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
            var (menuWidth, menuHeight, menuX, menuY) = CalculateMenuDimensions();

            X = menuX;
            Y = menuY;
            Width = menuWidth;
            Height = menuHeight;

            float scaleX = (float)Width / BASE_MENU_WIDTH;
            float scaleY = (float)Height / BASE_MENU_HEIGHT;
            uiScale = Math.Min(scaleX, scaleY);

            // Initialize scaled spacing constants
            TAB_GAP = Scale(BASE_TAB_GAP, uiScale);
            ITEM_SLOT_GAP = Scale(BASE_ITEM_SLOT_GAP, uiScale);
            LIST_PADDING = Scale(BASE_LIST_PADDING, uiScale);
            BUTTON_LABEL_GAP = Scale(BASE_BUTTON_LABEL_GAP, uiScale);
            ITEM_HEIGHT = Scale(BASE_ITEM_HEIGHT, uiScale);
            titlePaddingTop = Scale(16, uiScale);

            // Calculate available height for item list
            int titleHeight = Scale(BASE_TITLE_HEIGHT, uiScale);
            int tabHeight = Scale(BASE_TAB_HEIGHT, uiScale);
            int tabMargin = Scale(BASE_TAB_MARGIN, uiScale);
            int bottomMargin = Scale(BASE_BOTTOM_MARGIN, uiScale);

            int listStartY = titleHeight + tabHeight + tabMargin;
            int availableHeight = Height - listStartY - bottomMargin - LIST_PADDING * 2;

            // Calculate how many items can fit
            VISIBLE_ITEMS = Math.Max(1, availableHeight / ITEM_HEIGHT);

            // Scale right panel offset based on menu width
            int leftPanelWidth = Scale(BASE_LEFT_PANEL_WIDTH, uiScale);
            int rightPanelXOffset = Scale(BASE_RIGHT_PANEL_X_OFFSET, uiScale);
            int panelGap = Math.Max(1, rightPanelXOffset - leftPanelWidth);

            // Player portrait box (left panel, centered)
            int portraitWidth = Scale(128, uiScale);
            int portraitHeight = Scale(192, uiScale);
            PortraitBox = new Rectangle(
                X + (leftPanelWidth - portraitWidth) / 2 + Scale(20, uiScale),
                Y + titleHeight + Scale(20, uiScale),
                portraitWidth,
                portraitHeight
            );

            // Category tabs (right panel top)
            int tabY = Y + titleHeight;
            int tabWidth = Scale(BASE_TAB_WIDTH, uiScale);
            int rightPanelX = X + rightPanelXOffset;

            ShirtsTab = new ClickableComponent(
                new Rectangle(rightPanelX, tabY, tabWidth, tabHeight),
                "Shirts"
            );
            PantsTab = new ClickableComponent(
                new Rectangle(rightPanelX + tabWidth + TAB_GAP, tabY, tabWidth, tabHeight),
                "Pants"
            );
            HatsTab = new ClickableComponent(
                new Rectangle(rightPanelX + (tabWidth + TAB_GAP) * 2, tabY, tabWidth, tabHeight),
                "Hats"
            );

            // Item list dimensions
            int listX = rightPanelX;
            int listY = tabY + tabHeight + tabMargin;
            int listWidth = Math.Max(1, Math.Min(Scale(BASE_ITEM_WIDTH, uiScale), Width - rightPanelXOffset - panelGap));
            int listHeight = VISIBLE_ITEMS * ITEM_HEIGHT;

            // Scroll buttons
            int scrollButtonWidth = Scale(48, uiScale);
            int scrollButtonHeight = Scale(44, uiScale);
            float scrollButtonScale = 4f * uiScale;
            ScrollUpButton = new ClickableTextureComponent(
                new Rectangle(listX + listWidth / 2 - scrollButtonWidth / 2, listY - Scale(10, uiScale), scrollButtonWidth, scrollButtonHeight),
                Game1.mouseCursors,
                new Rectangle(421, 459, 12, 11),
                scrollButtonScale
            );
            ScrollDownButton = new ClickableTextureComponent(
                new Rectangle(listX + listWidth / 2 - scrollButtonWidth / 2, listY + listHeight + Scale(10, uiScale), scrollButtonWidth, scrollButtonHeight),
                Game1.mouseCursors,
                new Rectangle(421, 472, 12, 11),
                scrollButtonScale
            );

            // Item slots - clear and rebuild
            ItemSlots.Clear();
            for (int i = 0; i < VISIBLE_ITEMS; i++)
            {
                ItemSlots.Add(new ClickableComponent(
                    new Rectangle(listX, listY + i * ITEM_HEIGHT, listWidth, ITEM_HEIGHT - ITEM_SLOT_GAP),
                    i.ToString()
                ));
            }

            // Reset button (left panel bottom)
            int resetWidth = Scale(84, uiScale);
            int resetHeight = Scale(44, uiScale);
            ResetButton = new ClickableTextureComponent(
                new Rectangle(X + leftPanelWidth / 2 - resetWidth / 2, Y + Height - Scale(70, uiScale), resetWidth, resetHeight),
                Game1.mouseCursors,
                new Rectangle(294, 428, 21, 11),
                4f * uiScale
            );

            // Close button (top right)
            int closeSize = Scale(48, uiScale);
            CloseButton = new ClickableTextureComponent(
                new Rectangle(X + Width - closeSize - Scale(12, uiScale), Y + Scale(10, uiScale), closeSize, closeSize),
                Game1.mouseCursors,
                new Rectangle(337, 494, 12, 12),
                4f * uiScale
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
            // Draw sky background
            b.Draw(Game1.daybg, PortraitBox, Color.White);

            // Draw farmer
            Vector2 farmerSize = new Vector2(16 * 0.8f * uiScale, 32 * 0.8f * uiScale);
            Vector2 farmerPos = new Vector2(
                PortraitBox.X + (PortraitBox.Width - farmerSize.X) / 2f,
                PortraitBox.Y + (PortraitBox.Height - farmerSize.Y) / 2f
            );
            FarmerRenderer.isDrawingForUI = true;
            Game1.player.FarmerRenderer.draw(
                b,
                new FarmerSprite.AnimationFrame(0, 0, false, false),
                0,
                new Rectangle(0, 0, 16, 32),
                farmerPos,
                Vector2.Zero,
                0.8f * uiScale,
                2,
                Color.White,
                0f,
                1f,
                Game1.player
            );
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
            int listWidth = ItemSlots[0].bounds.Width;
            int listHeight = ItemSlots.Count * ITEM_HEIGHT;

            // Draw background box
            IClickableMenu.drawTextureBox(b, listX - LIST_PADDING, listY - LIST_PADDING,
                listWidth + LIST_PADDING * 2, listHeight + LIST_PADDING * 2, Color.White);

            // Draw scroll buttons (only if needed)
            if (scrollOffset > 0)
                ScrollUpButton.draw(b);
            if (scrollOffset < totalItems - ItemSlots.Count)
                ScrollDownButton.draw(b);
        }

        /// <summary>
        /// Draws the reset button with its label.
        /// </summary>
        /// <param name="b">SpriteBatch to draw with.</param>
        public void DrawResetButton(SpriteBatch b)
        {
            ResetButton.draw(b);
            Vector2 resetLabelSize = Game1.smallFont.MeasureString("Reset");
            Utility.drawTextWithShadow(b, "Reset", Game1.smallFont,
                new Vector2(
                    ResetButton.bounds.X + (ResetButton.bounds.Width - resetLabelSize.X) / 2,
                    ResetButton.bounds.Y + ResetButton.bounds.Height + BUTTON_LABEL_GAP
                ),
                Game1.textColor);
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
                new Vector2(X + (Width - titleSize.X) / 2, Y + titlePaddingTop),
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

        private static int Scale(int value, float scale)
        {
            return Math.Max(1, (int)Math.Round(value * scale));
        }
    }
}
