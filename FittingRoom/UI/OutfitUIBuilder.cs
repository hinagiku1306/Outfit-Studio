using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using static FittingRoom.OutfitLayoutConstants;

namespace FittingRoom
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
        public ClickableComponent ResetButton { get; private set; } = null!;
        public ClickableComponent ApplyButton { get; private set; } = null!;
        public ClickableTextureComponent CloseButton { get; private set; } = null!;
        public List<ClickableComponent> ItemSlots { get; private set; } = new();
        public Rectangle PortraitBox { get; private set; }
        public ClickableComponent? ModFilterDropdown { get; private set; } = null;
        public ClickableComponent? SearchBar { get; private set; } = null;

        // New buttons for revamped layout
        public ClickableComponent SaveButton { get; private set; } = null!;
        public ClickableComponent TemplatesButton { get; private set; } = null!;

        // Direction preview arrows
        public ClickableTextureComponent LeftArrowButton { get; private set; } = null!;
        public ClickableTextureComponent RightArrowButton { get; private set; } = null!;
        private int previewDirection = 2; // 0=Up, 1=Right, 2=Down, 3=Left (default: Down)

        // Equipped items text area
        public Rectangle EquippedTextArea { get; private set; }

        // Menu position and dimensions
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        // Saved message display
        private float savedMessageTimer = 0f;

        // Farmer preview rendering resources
        private RenderTarget2D? farmerRenderTarget = null;
        private SpriteBatch? farmerSpriteBatch = null;

        // Farmer preview constants (cached for performance)
        private static readonly Rectangle FarmerSourceRect = new Rectangle(0, 0, 16, 32);
        private static readonly Rectangle FarmerBathingSourceRect = new Rectangle(0, 576, 16, 32);
        private static readonly Vector2 FarmerRenderPosition = new Vector2(32, 32);

        // Standing animation frames for each direction (frame index, duration, secondaryArm, flip)
        // Frame indices: 0=down, 6=right, 12=up, 6+flip=left
        private static readonly FarmerSprite.AnimationFrame[] DirectionFrames = new[]
        {
            new FarmerSprite.AnimationFrame(12, 0, secondaryArm: false, flip: false), // 0 = Up
            new FarmerSprite.AnimationFrame(6, 0, secondaryArm: false, flip: false),  // 1 = Right
            new FarmerSprite.AnimationFrame(0, 0, secondaryArm: false, flip: false),  // 2 = Down
            new FarmerSprite.AnimationFrame(6, 0, secondaryArm: false, flip: true),   // 3 = Left
        };
        private const int FarmerRenderWidth = BackgroundSourceWidth * 2;  // 128
        private const int FarmerRenderHeight = BackgroundSourceHeight * 2; // 192

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

            // Calculate menu width based on actual content
            int contentWidth = CharacterPreviewWidth + GapBetweenPreviewAndGrid + gridWidth + ContentBoxPadding * 2;
            Width = contentWidth + MenuSidePadding * 2;
            Height = MenuTotalHeight;

            // Center menu on screen
            X = (Game1.uiViewport.Width - Width) / 2;
            Y = (Game1.uiViewport.Height - Height) / 2;

            // Calculate panel positions (centered within menu)
            int contentStartX = X + MenuSidePadding;
            int leftPanelCenterX = contentStartX + CharacterPreviewWidth / 2;
            int rightPanelX = contentStartX + CharacterPreviewWidth + GapBetweenPreviewAndGrid;

            int tabY = Y + ContentBoxPadding + MenuTopPadding + TabMarginTop;

            // Calculate dynamic widths for tabs based on text
            int shirtTabWidth = UIHelpers.CalculateButtonWidth(TranslationCache.TabShirts);
            int pantsTabWidth = UIHelpers.CalculateButtonWidth(TranslationCache.TabPants);
            int hatsTabWidth = UIHelpers.CalculateButtonWidth(TranslationCache.TabHats);

            int totalTabsWidth = shirtTabWidth + pantsTabWidth + hatsTabWidth + TabAndButtonGap * 2;
            int tabsStartX = X + (Width - totalTabsWidth) / 2;

            ShirtsTab = new ClickableComponent(
                new Rectangle(tabsStartX, tabY, shirtTabWidth, TabAndButtonHeight),
                TranslationCache.TabShirts
            );
            PantsTab = new ClickableComponent(
                new Rectangle(tabsStartX + shirtTabWidth + TabAndButtonGap, tabY, pantsTabWidth, TabAndButtonHeight),
                TranslationCache.TabPants
            );
            HatsTab = new ClickableComponent(
                new Rectangle(tabsStartX + shirtTabWidth + pantsTabWidth + TabAndButtonGap * 2, tabY, hatsTabWidth, TabAndButtonHeight),
                TranslationCache.TabHats
            );

            // Content panels start below tabs
            int contentY = tabY + TabAndButtonHeight + ContentBoxPadding;

            // Left panel: Portrait centered vertically with arrows and Apply/Reset buttons below
            // Calculate total group height for vertical centering (includes arrow row)
            const int arrowRowHeight = ScrollArrowButtonSize + 8;
            int totalGroupHeight = CharacterPreviewHeight + arrowRowHeight + GapBetweenPortraitAndButtons + (TabAndButtonHeight * 2) + TabAndButtonGap;
            int availableVerticalSpace = Height - (contentY - Y) - ContentBoxPadding - MenuTopPadding;
            int portraitY = contentY + (availableVerticalSpace - totalGroupHeight) / 2;

            PortraitBox = new Rectangle(
                leftPanelCenterX - CharacterPreviewWidth / 2,
                portraitY,
                CharacterPreviewWidth,
                CharacterPreviewHeight
            );

            // Direction arrows below portrait (centered horizontally)
            const int arrowScale = 4;
            const int arrowWidth = 12 * arrowScale;  // 48px
            const int arrowHeight = 11 * arrowScale; // 44px
            const int arrowGap = 16;
            int arrowY = PortraitBox.Bottom + 8;
            int totalArrowsWidth = arrowWidth * 2 + arrowGap;
            int arrowsStartX = leftPanelCenterX - totalArrowsWidth / 2;

            LeftArrowButton = new ClickableTextureComponent(
                new Rectangle(arrowsStartX, arrowY, arrowWidth, arrowHeight),
                Game1.mouseCursors,
                new Rectangle(352, 495, 12, 11),
                arrowScale
            );
            RightArrowButton = new ClickableTextureComponent(
                new Rectangle(arrowsStartX + arrowWidth + arrowGap, arrowY, arrowWidth, arrowHeight),
                Game1.mouseCursors,
                new Rectangle(365, 495, 12, 11),
                arrowScale
            );

            // Apply and Reset buttons below arrows (centered in left panel)
            // Calculate dynamic widths and use max so both buttons are same width
            int applyButtonWidth = UIHelpers.CalculateButtonWidth(TranslationCache.ButtonApply);
            int resetButtonWidth = UIHelpers.CalculateButtonWidth(TranslationCache.ButtonReset);
            int buttonWidth = Math.Max(applyButtonWidth, resetButtonWidth);

            int buttonsStartX = leftPanelCenterX - buttonWidth / 2;
            int applyButtonY = arrowY + arrowHeight + GapBetweenPortraitAndButtons;
            int resetButtonY = applyButtonY + TabAndButtonHeight + TabAndButtonGap;

            ApplyButton = new ClickableComponent(
                new Rectangle(buttonsStartX, applyButtonY, buttonWidth, TabAndButtonHeight),
                TranslationCache.ButtonApply
            );
            ResetButton = new ClickableComponent(
                new Rectangle(buttonsStartX, resetButtonY, buttonWidth, TabAndButtonHeight),
                TranslationCache.ButtonReset
            );

            // Right panel: Filter/Search controls at top, then item grid
            int filterSearchY = contentY;

            // Mod filter dropdown
            ModFilterDropdown = new ClickableComponent(
                new Rectangle(rightPanelX, filterSearchY, FilterDropdownWidth, TabAndButtonHeight),
                "ModFilterDropdown"
            );

            // Search bar next to filter
            int searchBarWidth = gridWidth - FilterDropdownWidth - FilterSearchGap;
            SearchBar = new ClickableComponent(
                new Rectangle(rightPanelX + FilterDropdownWidth + FilterSearchGap, filterSearchY, searchBarWidth, TabAndButtonHeight),
                "SearchBar"
            );

            // Item grid starts below filter/search
            int gridY = filterSearchY + FilterSearchAreaHeight;

            // Calculate visible rows based on available height
            int availableHeight = Height - (gridY - Y) - ContentBoxPadding - BottomButtonAreaHeight;
            VISIBLE_ROWS = Math.Max(1, (availableHeight + ItemSlotGap) / (SLOT_SIZE + ItemSlotGap));
            VISIBLE_ITEMS = VISIBLE_ROWS * COLUMNS;

            // Item slots grid
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

            // Bottom buttons: Save and Templates (aligned to bottom-right) - dynamic widths
            // Center buttons vertically within the BottomButtonAreaHeight
            int bottomButtonY = Y + Height - BottomButtonAreaHeight - MenuBottomPadding + (BottomButtonAreaHeight - TabAndButtonHeight) / 2;

            // Calculate dynamic widths based on text
            int templatesButtonWidth = UIHelpers.CalculateButtonWidth(TranslationCache.ButtonTemplates);
            int saveButtonWidth = UIHelpers.CalculateButtonWidth(TranslationCache.ButtonSave);

            // Templates button on the right
            TemplatesButton = new ClickableComponent(
                new Rectangle(X + Width - MenuSidePadding - templatesButtonWidth, bottomButtonY, templatesButtonWidth, TabAndButtonHeight),
                TranslationCache.ButtonTemplates
            );

            // Save button to the left of Templates
            SaveButton = new ClickableComponent(
                new Rectangle(TemplatesButton.bounds.X - saveButtonWidth - BottomButtonGap, bottomButtonY, saveButtonWidth, TabAndButtonHeight),
                TranslationCache.ButtonSave
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

            // Draw hover effect
            if (tab.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                b.Draw(Game1.staminaRect, tab.bounds, HoverEffectColor);
            }

            Vector2 labelSize = Game1.smallFont.MeasureString(label);
            Utility.drawTextWithShadow(b, label, Game1.smallFont,
                new Vector2(
                    tab.bounds.X + (tab.bounds.Width - labelSize.X) / 2,
                    tab.bounds.Y + (tab.bounds.Height - labelSize.Y) / 2
                ),
                textColor);
        }

        /// <summary>
        /// Draws the player preview (background and farmer).
        /// Uses RenderTarget2D to render farmer at 1x scale then scales the result.
        /// </summary>
        public void DrawPlayerPreview(SpriteBatch b)
        {
            b.Draw((Game1.timeOfDay >= NightTimeStartHour) ? Game1.nightbg : Game1.daybg, PortraitBox, Color.White);

            InitializeFarmerRenderResources();

            var renderTargets = Game1.graphics.GraphicsDevice.GetRenderTargets();
            Game1.graphics.GraphicsDevice.SetRenderTarget(farmerRenderTarget);
            Game1.graphics.GraphicsDevice.Clear(Color.Transparent);

            // Calculate source rect based on direction frame
            // Spritesheet is 96px wide (6 frames per row), each frame is 16x32
            int frameIndex = DirectionFrames[previewDirection].frame;
            int baseY = Game1.player.bathingClothes.Value ? 576 : 0;
            int sourceX = (frameIndex * 16) % 96;
            int sourceY = baseY + (frameIndex * 16) / 96 * 32;
            Rectangle sourceRect = new Rectangle(sourceX, sourceY, FarmerSpriteWidth, FarmerSpriteHeight);

            farmerSpriteBatch!.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

            FarmerRenderer.isDrawingForUI = true;
            DrawFarmerToRenderTarget(sourceRect, Color.White);

            if (Game1.timeOfDay >= NightTimeStartHour)
            {
                DrawFarmerToRenderTarget(sourceRect, Color.DarkBlue * 0.3f);
            }
            FarmerRenderer.isDrawingForUI = false;

            farmerSpriteBatch.End();

            Game1.graphics.GraphicsDevice.SetRenderTargets(renderTargets);

            b.Draw(farmerRenderTarget, PortraitBox, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
        }

        private void InitializeFarmerRenderResources()
        {
            if (farmerRenderTarget == null || farmerRenderTarget.IsDisposed)
            {
                farmerRenderTarget = new RenderTarget2D(
                    Game1.graphics.GraphicsDevice,
                    FarmerRenderWidth,
                    FarmerRenderHeight,
                    false,
                    SurfaceFormat.Color,
                    DepthFormat.None,
                    0,
                    RenderTargetUsage.DiscardContents
                );
            }

            if (farmerSpriteBatch == null || farmerSpriteBatch.IsDisposed)
            {
                farmerSpriteBatch = new SpriteBatch(Game1.graphics.GraphicsDevice);
            }
        }

        private void DrawFarmerToRenderTarget(Rectangle sourceRect, Color color)
        {
            Game1.player.FarmerRenderer.draw(
                farmerSpriteBatch!,
                DirectionFrames[previewDirection],
                DirectionFrames[previewDirection].frame,
                sourceRect,
                FarmerRenderPosition,
                Vector2.Zero,
                FarmerSpriteLayerDepth,
                previewDirection,
                color,
                0f,
                1f,
                Game1.player
            );
        }

        /// <summary>
        /// Draws the item list background.
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
        }

        #region Button Drawing

        /// <summary>
        /// Draws all left panel buttons: direction arrows, apply, and reset.
        /// </summary>
        public void DrawLeftPanelButtons(SpriteBatch b)
        {
            // Direction arrows
            UIHelpers.DrawTextureButton(b, LeftArrowButton);
            UIHelpers.DrawTextureButton(b, RightArrowButton);

            // Apply and Reset buttons
            UIHelpers.DrawTextButton(b, ApplyButton, TranslationCache.ButtonApply);
            UIHelpers.DrawTextButton(b, ResetButton, TranslationCache.ButtonReset);
        }

        /// <summary>
        /// Draws the bottom panel buttons: save and templates.
        /// </summary>
        public void DrawBottomButtons(SpriteBatch b)
        {
            UIHelpers.DrawTextButton(b, SaveButton, TranslationCache.ButtonSave);
            UIHelpers.DrawTextButton(b, TemplatesButton, TranslationCache.ButtonTemplates);
        }

        #endregion

        /// <summary>
        /// Rotates the preview direction counter-clockwise (left arrow).
        /// </summary>
        public void RotatePreviewLeft()
        {
            previewDirection = (previewDirection + 3) % 4;
        }

        /// <summary>
        /// Rotates the preview direction clockwise (right arrow).
        /// </summary>
        public void RotatePreviewRight()
        {
            previewDirection = (previewDirection + 1) % 4;
        }

        /// <summary>
        /// Draws the close button with hover effect.
        /// </summary>
        public void DrawCloseButton(SpriteBatch b)
        {
            UIHelpers.DrawTextureButton(b, CloseButton);
        }

        /// <summary>
        /// Draws the menu title.
        /// </summary>
        public void DrawTitle(SpriteBatch b)
        {
            string title = TranslationCache.MenuTitle;
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
                string message = TranslationCache.MessageSaved;
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
        /// Draws the mod filter dropdown button.
        /// </summary>
        /// <param name="b">SpriteBatch for drawing.</param>
        /// <param name="currentFilter">The currently selected mod filter, or null for "All Items".</param>
        /// <param name="isOpen">Whether the dropdown is currently open.</param>
        public void DrawModFilterDropdown(SpriteBatch b, string? currentFilter, bool isOpen)
        {
            if (ModFilterDropdown == null)
                return;

            Rectangle bounds = ModFilterDropdown.bounds;
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            bool isHovered = bounds.Contains(mouseX, mouseY);

            // Draw dropdown button background
            IClickableMenu.drawTextureBox(b, bounds.X, bounds.Y, bounds.Width, bounds.Height,
                isOpen ? Color.Wheat : Color.White);

            // Draw current selection text
            string displayText = string.IsNullOrEmpty(currentFilter) ? TranslationCache.FilterAll : $"{currentFilter}";

            // Truncate text if too long
            Vector2 textSize = Game1.smallFont.MeasureString(displayText);
            int maxTextWidth = bounds.Width - 48;
            if (textSize.X > maxTextWidth)
            {
                while (textSize.X > maxTextWidth && displayText.Length > 10)
                {
                    displayText = displayText.Substring(0, displayText.Length - 1);
                    textSize = Game1.smallFont.MeasureString(displayText + "...");
                }
                displayText += "...";
            }

            Vector2 textPos = new Vector2(
                bounds.X + 20, // Increased left padding for better spacing
                bounds.Y + (bounds.Height - textSize.Y) / 2
            );

            Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPos, Game1.textColor);
        }

        /// <summary>Draws the search bar background.</summary>
        public void DrawSearchBar(SpriteBatch b, bool isFocused)
        {
            if (SearchBar == null)
                return;

            IClickableMenu.drawTextureBox(b, SearchBar.bounds.X, SearchBar.bounds.Y,
                SearchBar.bounds.Width, SearchBar.bounds.Height, Color.White);
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
                LeftArrowButton.containsPoint(mouseX, mouseY) ||
                RightArrowButton.containsPoint(mouseX, mouseY))
            {
                return true;
            }

            if (ModFilterDropdown != null && ModFilterDropdown.containsPoint(mouseX, mouseY))
            {
                return true;
            }

            if (SearchBar != null && SearchBar.containsPoint(mouseX, mouseY))
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

        /// <summary>
        /// Cleans up resources used by the UI builder.
        /// </summary>
        /// <summary>
        /// Draws the equipped items text (Shirt, Pants, Hat names) below preview.
        /// </summary>
        public void DrawEquippedItemsText(SpriteBatch b, string shirtName, string pantsName, string hatName)
        {
            int textX = EquippedTextArea.X + TextPadding;
            int textY = EquippedTextArea.Y + TextPadding;

            // Draw "Equipped:" label
            Utility.drawTextWithShadow(b, "Equipped:", Game1.smallFont,
                new Vector2(textX, textY), Game1.textColor);

            // Draw item names with truncation if needed
            int maxWidth = EquippedTextArea.Width - (TextPadding * 2);

            textY += EquippedTextLineHeight;
            string shirtText = UIHelpers.TruncateText($"Shirt: {shirtName}", maxWidth);
            Utility.drawTextWithShadow(b, shirtText, Game1.smallFont,
                new Vector2(textX, textY), Game1.textColor);

            textY += EquippedTextLineHeight;
            string pantsText = UIHelpers.TruncateText($"Pants: {pantsName}", maxWidth);
            Utility.drawTextWithShadow(b, pantsText, Game1.smallFont,
                new Vector2(textX, textY), Game1.textColor);

            textY += EquippedTextLineHeight;
            string hatText = UIHelpers.TruncateText($"Hat: {hatName}", maxWidth);
            Utility.drawTextWithShadow(b, hatText, Game1.smallFont,
                new Vector2(textX, textY), Game1.textColor);
        }

        public void Cleanup()
        {
            if (farmerRenderTarget != null && !farmerRenderTarget.IsDisposed)
            {
                farmerRenderTarget.Dispose();
                farmerRenderTarget = null;
            }

            if (farmerSpriteBatch != null && !farmerSpriteBatch.IsDisposed)
            {
                farmerSpriteBatch.Dispose();
                farmerSpriteBatch = null;
            }
        }
    }
}
