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

            // Portrait box - centered with buttons as a group
            int totalGroupHeight = CharacterPreviewHeight + GapBetweenPortraitAndButtons + (TabAndButtonHeight * 2) + TabAndButtonGap;
            int availableVerticalSpace = Height - (ContentBoxPadding + TitleAreaHeight) - ContentBoxPadding;
            int portraitY = Y + ContentBoxPadding + TitleAreaHeight + (availableVerticalSpace - totalGroupHeight) / 2;

            PortraitBox = new Rectangle(
                leftPanelCenterX - CharacterPreviewWidth / 2,
                portraitY,
                CharacterPreviewWidth,
                CharacterPreviewHeight
            );

            // Category tabs - center-aligned with grid
            int totalTabsWidth = TabAndButtonWidth * 3 + TabAndButtonGap * 2;
            int tabsStartX = rightPanelX + (gridWidth - totalTabsWidth) / 2 - 10;

            ShirtsTab = new ClickableComponent(
                new Rectangle(tabsStartX, tabY, TabAndButtonWidth, TabAndButtonHeight),
                "Shirts"
            );
            PantsTab = new ClickableComponent(
                new Rectangle(tabsStartX + TabAndButtonWidth + TabAndButtonGap, tabY, TabAndButtonWidth, TabAndButtonHeight),
                "Pants"
            );
            HatsTab = new ClickableComponent(
                new Rectangle(tabsStartX + (TabAndButtonWidth + TabAndButtonGap) * 2, tabY, TabAndButtonWidth, TabAndButtonHeight),
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
            int scrollButtonX = rightPanelX + gridWidth + ContentBoxPadding + ScrollArrowLeftGap;
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

            // Apply and Reset buttons - stacked vertically below the portrait
            int buttonsStartX = leftPanelCenterX - TabAndButtonWidth / 2;
            int applyButtonY = PortraitBox.Bottom + GapBetweenPortraitAndButtons;
            int resetButtonY = applyButtonY + TabAndButtonHeight + TabAndButtonGap;

            ApplyButton = new ClickableTextureComponent(
                new Rectangle(buttonsStartX, applyButtonY, TabAndButtonWidth, TabAndButtonHeight),
                Game1.mouseCursors,
                new Rectangle(432, 439, 9, 9),
                4f
            );
            ResetButton = new ClickableTextureComponent(
                new Rectangle(buttonsStartX, resetButtonY, TabAndButtonWidth, TabAndButtonHeight),
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
        /// Draws a category tab with text.
        /// </summary>
        public void DrawTabWithText(SpriteBatch b, ClickableComponent tab, string label, bool isActive)
        {
            // Keep box (and its shadow) at full opacity, only dim the text when inactive
            Color textColor = isActive ? Game1.textColor : Game1.textColor * TabOpacity;

            IClickableMenu.drawTextureBox(b, tab.bounds.X, tab.bounds.Y,
                tab.bounds.Width, tab.bounds.Height, Color.White);

            Vector2 labelSize = Game1.smallFont.MeasureString(label);
            Utility.drawTextWithShadow(b, label, Game1.smallFont,
                new Vector2(
                    tab.bounds.X + (tab.bounds.Width - labelSize.X) / 2,
                    tab.bounds.Y + (tab.bounds.Height - labelSize.Y) / 2
                ),
                textColor);
        }

        /// <summary>
        /// Draws a category tab with an item sprite icon.
        /// </summary>
        public void DrawTabWithSprite(SpriteBatch b, ClickableComponent tab, Texture2D texture, Rectangle sourceRect, bool isActive, float spriteScale = 2f)
        {
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            bool isHovered = tab.containsPoint(mouseX, mouseY);

            if (isHovered)
            {
                Game1.mouseCursor = 1; // Hand cursor
            }

            Color boxColor = isActive ? Color.White : Color.White * 0.5f;

            IClickableMenu.drawTextureBox(b, tab.bounds.X, tab.bounds.Y,
                tab.bounds.Width, tab.bounds.Height, boxColor);

            // Calculate sprite size and position to center it
            float scaledWidth = sourceRect.Width * spriteScale;
            float scaledHeight = sourceRect.Height * spriteScale;
            Vector2 spritePos = new Vector2(
                tab.bounds.X + (tab.bounds.Width - scaledWidth) / 2,
                tab.bounds.Y + (tab.bounds.Height - scaledHeight) / 2
            );

            // Draw the sprite centered in the tab
            b.Draw(texture, spritePos, sourceRect, Color.White, 0f, Vector2.Zero, spriteScale, SpriteEffects.None, 0.87f);
        }

        /// <summary>
        /// Draws the player preview (background and farmer).
        /// </summary>
        public void DrawPlayerPreview(SpriteBatch b)
        {
            // Draw sky background (day or night based on time)
            b.Draw((Game1.timeOfDay >= NightTimeStartHour) ? Game1.nightbg : Game1.daybg, PortraitBox, Color.White);

            // Draw farmer anchored from bottom (matches vanilla CharacterCustomization)
            Vector2 farmerPos = new Vector2(
                PortraitBox.Center.X - FarmerPreviewCenterOffset,
                PortraitBox.Bottom - FarmerPreviewBottomOffset
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
                    Game1.mouseCursor = 1; // Hand cursor
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
                    Game1.mouseCursor = 1; // Hand cursor
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
        /// Helper method to draw a text button with label and hover effect (same as tab style).
        /// </summary>
        private void DrawTextButton(SpriteBatch b, ClickableComponent button, string label, Color? hoverColor = null)
        {
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            bool isHovered = button.containsPoint(mouseX, mouseY);

            // Change cursor when hovering
            if (isHovered)
            {
                Game1.mouseCursor = 1; // Hand cursor
            }

            // Draw button box (same as tabs)
            IClickableMenu.drawTextureBox(b, button.bounds.X, button.bounds.Y,
                button.bounds.Width, button.bounds.Height, Color.White);

            // Draw centered text
            Vector2 textSize = Game1.smallFont.MeasureString(label);
            Vector2 textPos = new Vector2(
                button.bounds.X + (button.bounds.Width - textSize.X) / 2,
                button.bounds.Y + (button.bounds.Height - textSize.Y) / 2
            );

            // Use hover color if hovering, otherwise default text color
            Color textColor = (isHovered && hoverColor.HasValue) ? hoverColor.Value : Game1.textColor;
            Utility.drawTextWithShadow(b, label, Game1.smallFont, textPos, textColor);
        }

        /// <summary>
        /// Draws the apply button with its label and hover effect.
        /// </summary>
        public void DrawApplyButton(SpriteBatch b)
        {
            DrawTextButton(b, ApplyButton, "Apply", null);
        }

        /// <summary>
        /// Draws the reset button with its label and hover effect.
        /// </summary>
        public void DrawResetButton(SpriteBatch b)
        {
            DrawTextButton(b, ResetButton, "Reset", null);
        }

        /// <summary>
        /// Draws the close button with hover effect.
        /// </summary>
        public void DrawCloseButton(SpriteBatch b)
        {
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            bool isHovered = CloseButton.containsPoint(mouseX, mouseY);

            if (isHovered)
            {
                Game1.mouseCursor = 1; // Hand cursor
                float originalScale = CloseButton.scale;
                CloseButton.scale = originalScale + ButtonHoverScaleIncrease;
                CloseButton.draw(b);
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

        /// <summary>
        /// Checks if the mouse is hovering over any clickable UI element.
        /// </summary>
        public bool IsHoveringClickable(int mouseX, int mouseY)
        {
            if (ShirtsTab.containsPoint(mouseX, mouseY) ||
                PantsTab.containsPoint(mouseX, mouseY) ||
                HatsTab.containsPoint(mouseX, mouseY) ||
                ApplyButton.containsPoint(mouseX, mouseY) ||
                ResetButton.containsPoint(mouseX, mouseY) ||
                CloseButton.containsPoint(mouseX, mouseY) ||
                ScrollUpButton.containsPoint(mouseX, mouseY) ||
                ScrollDownButton.containsPoint(mouseX, mouseY))
            {
                return true;
            }

            foreach (var slot in ItemSlots)
            {
                if (slot.containsPoint(mouseX, mouseY))
                    return true;
            }

            return false;
        }
    }
}
