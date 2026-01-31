using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using static OutfitRoom.OutfitLayoutConstants;

namespace OutfitRoom
{
    /// <summary>
    /// Builds and manages UI components for the OutfitMenu.
    /// </summary>
    public class OutfitUIBuilder
    {
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

        // Saved message display
        private float savedMessageTimer = 0f;

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
            SLOT_SIZE = ItemSlotSize;
            COLUMNS = ItemGridColumns;

            // Calculate grid dimensions
            int gridWidth = COLUMNS * SLOT_SIZE + (COLUMNS - 1) * ItemSlotGap;
            int scrollAreaWidth = ScrollArrowButtonSize + ContentBoxPadding;

            // Calculate menu width based on actual content
            int contentWidth = CharacterPreviewWidth + GapBetweenPreviewAndGrid + gridWidth + ContentBoxPadding * 2 + scrollAreaWidth;
            Width = contentWidth + MenuSidePadding * 2;
            Height = MenuTotalHeight;

            // Center menu on screen
            X = (Game1.uiViewport.Width - Width) / 2;
            Y = (Game1.uiViewport.Height - Height) / 2;

            // Calculate panel positions (centered within menu)
            int contentStartX = X + MenuSidePadding;
            int leftPanelCenterX = contentStartX + CharacterPreviewWidth / 2;
            int rightPanelX = contentStartX + CharacterPreviewWidth + GapBetweenPreviewAndGrid;

            // Vertical layout
            int tabY = Y + ContentBoxPadding + TitleAreaHeight;
            int gridY = tabY + TabAndButtonHeight + ContentBoxPadding;

            // Calculate visible rows based on available height
            int availableHeight = Height - (gridY - Y) - ContentBoxPadding - BottomButtonAreaHeight;
            VISIBLE_ROWS = Math.Max(1, (availableHeight + ItemSlotGap) / (SLOT_SIZE + ItemSlotGap));
            VISIBLE_ITEMS = VISIBLE_ROWS * COLUMNS;

            // Portrait box - centered in left panel area
            int portraitY = Y + ContentBoxPadding + TitleAreaHeight + (Height - TitleAreaHeight - ContentBoxPadding * 2 - BottomButtonAreaHeight - CharacterPreviewHeight) / 2;
            PortraitBox = new Rectangle(
                leftPanelCenterX - CharacterPreviewWidth / 2,
                portraitY,
                CharacterPreviewWidth,
                CharacterPreviewHeight
            );

            // Category tabs - aligned with grid
            ShirtsTab = new ClickableComponent(
                new Rectangle(rightPanelX, tabY, TabAndButtonWidth, TabAndButtonHeight),
                "Shirts"
            );
            PantsTab = new ClickableComponent(
                new Rectangle(rightPanelX + TabAndButtonWidth + TabAndButtonGap, tabY, TabAndButtonWidth, TabAndButtonHeight),
                "Pants"
            );
            HatsTab = new ClickableComponent(
                new Rectangle(rightPanelX + (TabAndButtonWidth + TabAndButtonGap) * 2, tabY, TabAndButtonWidth, TabAndButtonHeight),
                "Hats"
            );

            // Item slots grid
            int gridHeight = VISIBLE_ROWS * SLOT_SIZE + Math.Max(0, VISIBLE_ROWS - 1) * ItemSlotGap;
            ItemSlots.Clear();
            for (int row = 0; row < VISIBLE_ROWS; row++)
            {
                for (int col = 0; col < COLUMNS; col++)
                {
                    int slotX = rightPanelX + col * (SLOT_SIZE + ItemSlotGap);
                    int slotY = gridY + row * (SLOT_SIZE + ItemSlotGap);
                    ItemSlots.Add(new ClickableComponent(
                        new Rectangle(slotX, slotY, SLOT_SIZE, SLOT_SIZE),
                        (row * COLUMNS + col).ToString()
                    ));
                }
            }

            // Scroll buttons (right side of the grid, vertically stacked)
            int scrollButtonX = rightPanelX + gridWidth + ContentBoxPadding;
            int totalScrollHeight = ScrollArrowButtonSize * 2 + ScrollArrowVerticalGap;
            int scrollButtonStartY = gridY + (gridHeight - totalScrollHeight) / 2;

            ScrollUpButton = new ClickableTextureComponent(
                new Rectangle(scrollButtonX, scrollButtonStartY, ScrollArrowButtonSize, ScrollArrowButtonSize),
                Game1.mouseCursors,
                new Rectangle(421, 459, 12, 11),
                4f
            );
            ScrollDownButton = new ClickableTextureComponent(
                new Rectangle(scrollButtonX, scrollButtonStartY + ScrollArrowButtonSize + ScrollArrowVerticalGap, ScrollArrowButtonSize, ScrollArrowButtonSize),
                Game1.mouseCursors,
                new Rectangle(421, 472, 12, 11),
                4f
            );

            // Apply and Reset buttons - side by side below the portrait
            int totalButtonsWidth = TabAndButtonWidth * 2 + TabAndButtonGap;
            int buttonsStartX = leftPanelCenterX - totalButtonsWidth / 2;
            int buttonsY = Y + Height - ContentBoxPadding - TabAndButtonHeight - CloseButtonEdgeMargin;

            ApplyButton = new ClickableTextureComponent(
                new Rectangle(buttonsStartX, buttonsY, TabAndButtonWidth, TabAndButtonHeight),
                Game1.mouseCursors,
                new Rectangle(432, 439, 9, 9),
                4f
            );
            ResetButton = new ClickableTextureComponent(
                new Rectangle(buttonsStartX + TabAndButtonWidth + TabAndButtonGap, buttonsY, TabAndButtonWidth, TabAndButtonHeight),
                Game1.mouseCursors,
                new Rectangle(432, 439, 9, 9),
                4f
            );

            // Close button (top right)
            CloseButton = new ClickableTextureComponent(
                new Rectangle(X + Width - CloseButtonSize - CloseButtonEdgeMargin, Y + CloseButtonEdgeMargin, CloseButtonSize, CloseButtonSize),
                Game1.mouseCursors,
                new Rectangle(337, 494, 12, 12),
                4f
            );
        }

        /// <summary>
        /// Draws a category tab using the same style as Apply/Reset buttons with hover effect.
        /// </summary>
        public void DrawTab(SpriteBatch b, ClickableComponent tab, string label, bool isActive)
        {
            bool isHovered = tab.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            Color boxColor;
            if (isActive)
                boxColor = Color.Orange;
            else if (isHovered)
                boxColor = Color.Wheat;
            else
                boxColor = Color.White;

            IClickableMenu.drawTextureBox(b, tab.bounds.X, tab.bounds.Y,
                tab.bounds.Width, tab.bounds.Height, boxColor);

            Vector2 labelSize = Game1.smallFont.MeasureString(label);
            Utility.drawTextWithShadow(b, label, Game1.smallFont,
                new Vector2(
                    tab.bounds.X + (tab.bounds.Width - labelSize.X) / 2,
                    tab.bounds.Y + (tab.bounds.Height - labelSize.Y) / 2
                ),
                Game1.textColor);
        }

        /// <summary>
        /// Draws the player preview (background and farmer).
        /// </summary>
        public void DrawPlayerPreview(SpriteBatch b)
        {
            // Draw sky background (day or night based on time)
            b.Draw((Game1.timeOfDay >= NightTimeStartHour) ? Game1.nightbg : Game1.daybg, PortraitBox, Color.White);

            // Draw farmer at standard scale
            Vector2 farmerPos = new Vector2(
                PortraitBox.Center.X - FarmerSpriteWidth / 2,
                PortraitBox.Center.Y - FarmerSpriteHeight / 2
            );

            FarmerRenderer.isDrawingForUI = true;
            Game1.player.FarmerRenderer.draw(
                b,
                new FarmerSprite.AnimationFrame(0, Game1.player.bathingClothes.Value ? 108 : 0, secondaryArm: false, flip: false),
                Game1.player.bathingClothes.Value ? 108 : 0,
                new Rectangle(0, Game1.player.bathingClothes.Value ? 576 : 0, 16, 32),
                farmerPos,
                Vector2.Zero,
                FarmerSpriteLayerDepth,
                2,
                Color.White,
                0f,
                FarmerSpriteScale,
                Game1.player
            );

            // Apply night overlay if needed (same as vanilla)
            if (Game1.timeOfDay >= NightTimeStartHour)
            {
                Game1.player.FarmerRenderer.draw(
                    b,
                    new FarmerSprite.AnimationFrame(0, Game1.player.bathingClothes.Value ? 108 : 0, secondaryArm: false, flip: false),
                    Game1.player.bathingClothes.Value ? 108 : 0,
                    new Rectangle(0, Game1.player.bathingClothes.Value ? 576 : 0, 16, 32),
                    farmerPos,
                    Vector2.Zero,
                    FarmerSpriteLayerDepth,
                    2,
                    Color.DarkBlue * 0.3f,
                    0f,
                    FarmerSpriteScale,
                    Game1.player
                );
            }
            FarmerRenderer.isDrawingForUI = false;
        }

        /// <summary>
        /// Draws the item list background and scroll buttons if needed.
        /// </summary>
        public void DrawItemList(SpriteBatch b, int scrollOffset, int totalItems)
        {
            if (ItemSlots.Count == 0)
                return;

            int listX = ItemSlots[0].bounds.X;
            int listY = ItemSlots[0].bounds.Y;
            int listWidth = (ItemSlots[COLUMNS - 1].bounds.Right - ItemSlots[0].bounds.Left);
            int listHeight = VISIBLE_ROWS * SLOT_SIZE + Math.Max(0, VISIBLE_ROWS - 1) * ItemSlotGap;

            // Draw background box
            IClickableMenu.drawTextureBox(b, listX - ContentBoxPadding, listY - ContentBoxPadding,
                listWidth + ContentBoxPadding * 2, listHeight + ContentBoxPadding * 2, Color.White);

            // Draw scroll buttons with hover effects (only if needed)
            int totalRows = Math.Max(1, (int)Math.Ceiling(totalItems / (float)COLUMNS));
            int maxScroll = Math.Max(0, totalRows - VISIBLE_ROWS);
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            if (scrollOffset > 0)
            {
                bool upHovered = ScrollUpButton.containsPoint(mouseX, mouseY);
                if (upHovered)
                {
                    float originalScale = ScrollUpButton.scale;
                    ScrollUpButton.scale = originalScale + ButtonHoverScaleIncrease;
                    ScrollUpButton.draw(b, Color.Yellow, 0.86f);
                    ScrollUpButton.scale = originalScale;
                }
                else
                {
                    ScrollUpButton.draw(b);
                }
            }
            if (scrollOffset < maxScroll)
            {
                bool downHovered = ScrollDownButton.containsPoint(mouseX, mouseY);
                if (downHovered)
                {
                    float originalScale = ScrollDownButton.scale;
                    ScrollDownButton.scale = originalScale + ButtonHoverScaleIncrease;
                    ScrollDownButton.draw(b, Color.Yellow, 0.86f);
                    ScrollDownButton.scale = originalScale;
                }
                else
                {
                    ScrollDownButton.draw(b);
                }
            }
        }

        /// <summary>
        /// Draws the apply button with its label and hover effect.
        /// </summary>
        public void DrawApplyButton(SpriteBatch b)
        {
            bool isHovered = ApplyButton.containsPoint(Game1.getMouseX(), Game1.getMouseY());
            Color boxColor = isHovered ? Color.LightGreen : Color.White;

            IClickableMenu.drawTextureBox(b, ApplyButton.bounds.X, ApplyButton.bounds.Y,
                ApplyButton.bounds.Width, ApplyButton.bounds.Height, boxColor);

            string applyText = "Apply";
            Vector2 textSize = Game1.smallFont.MeasureString(applyText);
            Vector2 textPos = new Vector2(
                ApplyButton.bounds.X + (ApplyButton.bounds.Width - textSize.X) / 2,
                ApplyButton.bounds.Y + (ApplyButton.bounds.Height - textSize.Y) / 2
            );
            Color textColor = isHovered ? Color.DarkGreen : Game1.textColor;
            Utility.drawTextWithShadow(b, applyText, Game1.smallFont, textPos, textColor);
        }

        /// <summary>
        /// Draws the reset button with its label and hover effect.
        /// </summary>
        public void DrawResetButton(SpriteBatch b)
        {
            bool isHovered = ResetButton.containsPoint(Game1.getMouseX(), Game1.getMouseY());
            Color boxColor = isHovered ? Color.LightCoral : Color.White;

            IClickableMenu.drawTextureBox(b, ResetButton.bounds.X, ResetButton.bounds.Y,
                ResetButton.bounds.Width, ResetButton.bounds.Height, boxColor);

            string resetText = "Reset";
            Vector2 textSize = Game1.smallFont.MeasureString(resetText);
            Vector2 textPos = new Vector2(
                ResetButton.bounds.X + (ResetButton.bounds.Width - textSize.X) / 2,
                ResetButton.bounds.Y + (ResetButton.bounds.Height - textSize.Y) / 2
            );
            Color textColor = isHovered ? Color.DarkRed : Game1.textColor;
            Utility.drawTextWithShadow(b, resetText, Game1.smallFont, textPos, textColor);
        }

        /// <summary>
        /// Draws the close button with hover effect.
        /// </summary>
        public void DrawCloseButton(SpriteBatch b)
        {
            bool isHovered = CloseButton.containsPoint(Game1.getMouseX(), Game1.getMouseY());
            if (isHovered)
            {
                float originalScale = CloseButton.scale;
                CloseButton.scale = originalScale + ButtonHoverScaleIncrease;
                CloseButton.draw(b, Color.Red, 0.86f);
                CloseButton.scale = originalScale;
            }
            else
            {
                CloseButton.draw(b);
            }
        }

        /// <summary>
        /// Draws the menu title.
        /// </summary>
        public void DrawTitle(SpriteBatch b)
        {
            string title = "Outfit Room";
            Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
            Utility.drawTextWithShadow(b, title, Game1.dialogueFont,
                new Vector2(X + (Width - titleSize.X) / 2, Y + ContentBoxPadding),
                Game1.textColor);
        }

        /// <summary>
        /// Triggers the "Saved!" message to display.
        /// </summary>
        public void ShowSavedMessage()
        {
            savedMessageTimer = SavedMessageDurationMs;
        }

        /// <summary>
        /// Updates the saved message timer.
        /// </summary>
        public void Update(float elapsedMilliseconds)
        {
            if (savedMessageTimer > 0)
            {
                savedMessageTimer -= elapsedMilliseconds;
            }
        }

        /// <summary>
        /// Draws the "Saved!" message above the character preview if active.
        /// </summary>
        public void DrawSavedMessage(SpriteBatch b)
        {
            if (savedMessageTimer > 0)
            {
                string message = "Saved!";
                Vector2 textSize = Game1.smallFont.MeasureString(message);
                Vector2 textPos = new Vector2(
                    PortraitBox.Center.X - textSize.X / 2,
                    PortraitBox.Y - textSize.Y - SavedMessageOffsetAbovePreview
                );
                Utility.drawTextWithShadow(b, message, Game1.smallFont, textPos, Game1.textColor);
            }
        }

        /// <summary>
        /// Draws the semi-transparent background overlay.
        /// </summary>
        public static void DrawOverlay(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * BackgroundOverlayOpacity);
        }
    }
}
