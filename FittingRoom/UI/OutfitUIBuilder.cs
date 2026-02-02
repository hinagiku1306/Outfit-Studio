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
        public ClickableComponent AllTab { get; private set; } = null!;
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
        public ClickableComponent? FilterClearButton { get; private set; } = null;
        public ClickableComponent? SearchClearButton { get; private set; } = null;
        public ClickableComponent? LookupButton { get; private set; } = null;

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
            int allTabWidth = UIHelpers.CalculateButtonWidth(TranslationCache.TabAll);
            int shirtTabWidth = UIHelpers.CalculateButtonWidth(TranslationCache.TabShirts);
            int pantsTabWidth = UIHelpers.CalculateButtonWidth(TranslationCache.TabPants);
            int hatsTabWidth = UIHelpers.CalculateButtonWidth(TranslationCache.TabHats);

            int totalTabsWidth = allTabWidth + shirtTabWidth + pantsTabWidth + hatsTabWidth + TabAndButtonGap * 3;
            int tabsStartX = X + (Width - totalTabsWidth) / 2;

            AllTab = new ClickableComponent(
                new Rectangle(tabsStartX, tabY, allTabWidth, TabAndButtonHeight),
                TranslationCache.TabAll
            );
            ShirtsTab = new ClickableComponent(
                new Rectangle(tabsStartX + allTabWidth + TabAndButtonGap, tabY, shirtTabWidth, TabAndButtonHeight),
                TranslationCache.TabShirts
            );
            PantsTab = new ClickableComponent(
                new Rectangle(tabsStartX + allTabWidth + shirtTabWidth + TabAndButtonGap * 2, tabY, pantsTabWidth, TabAndButtonHeight),
                TranslationCache.TabPants
            );
            HatsTab = new ClickableComponent(
                new Rectangle(tabsStartX + allTabWidth + shirtTabWidth + pantsTabWidth + TabAndButtonGap * 3, tabY, hatsTabWidth, TabAndButtonHeight),
                TranslationCache.TabHats
            );

            // Content panels start below tabs
            int contentY = tabY + TabAndButtonHeight + GapBetweenTabsAndItemGrid;

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

            // New/Outfits buttons below arrows (centered in left panel)
            int newOutfitButtonWidth = UIHelpers.CalculateButtonWidth(TranslationCache.ButtonNewOutfit);
            int outfitsButtonWidth = UIHelpers.CalculateButtonWidth(TranslationCache.ButtonOutfits);
            int leftButtonWidth = Math.Max(newOutfitButtonWidth, outfitsButtonWidth);

            int leftButtonsStartX = leftPanelCenterX - leftButtonWidth / 2;
            int newOutfitButtonY = arrowY + arrowHeight + GapBetweenPortraitAndButtons;
            int outfitsButtonY = newOutfitButtonY + TabAndButtonHeight + TabAndButtonGap;

            SaveButton = new ClickableComponent(
                new Rectangle(leftButtonsStartX, newOutfitButtonY, leftButtonWidth, TabAndButtonHeight),
                TranslationCache.ButtonNewOutfit
            );
            TemplatesButton = new ClickableComponent(
                new Rectangle(leftButtonsStartX, outfitsButtonY, leftButtonWidth, TabAndButtonHeight),
                TranslationCache.ButtonOutfits
            );

            // Right panel: Filter/Search controls at top, then item grid
            int filterSearchY = contentY;

            // Mod filter dropdown
            ModFilterDropdown = new ClickableComponent(
                new Rectangle(rightPanelX, filterSearchY, FilterDropdownWidth, TabAndButtonHeight),
                "ModFilterDropdown"
            );

            // Filter clear button (positioned inside dropdown, right side)
            FilterClearButton = new ClickableComponent(
                new Rectangle(
                    rightPanelX + FilterDropdownWidth - ClearButtonRightMargin - ClearButtonSize,
                    filterSearchY + (TabAndButtonHeight - ClearButtonSize) / 2,
                    ClearButtonSize,
                    ClearButtonSize
                ),
                "FilterClear"
            );

            // Search bar next to filter
            int searchBarWidth = gridWidth - FilterDropdownWidth - FilterSearchGap;
            SearchBar = new ClickableComponent(
                new Rectangle(rightPanelX + FilterDropdownWidth + FilterSearchGap, filterSearchY, searchBarWidth, TabAndButtonHeight),
                "SearchBar"
            );

            // Search clear button
            SearchClearButton = new ClickableComponent(
                new Rectangle(
                    rightPanelX + FilterDropdownWidth + FilterSearchGap + searchBarWidth - ClearButtonRightMargin - ClearButtonSize,
                    filterSearchY + (TabAndButtonHeight - ClearButtonSize) / 2,
                    ClearButtonSize,
                    ClearButtonSize
                ),
                "SearchClear"
            );

            // Lookup button
            LookupButton = new ClickableComponent(
                new Rectangle(
                    PortraitBox.Right - LookupIconSize - LookupIconMargin,
                    PortraitBox.Y + LookupIconMargin,
                    LookupIconSize,
                    LookupIconSize
                ),
                "Lookup"
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

            // Bottom buttons: Apply and Reset (aligned to bottom-right)
            int bottomButtonY = Y + Height - BottomButtonAreaHeight - MenuBottomPadding + (BottomButtonAreaHeight - TabAndButtonHeight) / 2;

            int applyButtonWidth = UIHelpers.CalculateButtonWidth(TranslationCache.ButtonApply);
            int resetButtonWidth = UIHelpers.CalculateButtonWidth(TranslationCache.ButtonReset);
            int bottomButtonWidth = Math.Max(applyButtonWidth, resetButtonWidth);

            ResetButton = new ClickableComponent(
                new Rectangle(X + Width - MenuSidePadding - bottomButtonWidth, bottomButtonY, bottomButtonWidth, TabAndButtonHeight),
                TranslationCache.ButtonReset
            );

            ApplyButton = new ClickableComponent(
                new Rectangle(ResetButton.bounds.X - bottomButtonWidth - BottomButtonGap, bottomButtonY, bottomButtonWidth, TabAndButtonHeight),
                TranslationCache.ButtonApply
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
        /// Draws a category tab with text and bold effect on hover.
        /// </summary>
        public void DrawTabWithText(SpriteBatch b, ClickableComponent tab, string label, bool isActive)
        {
            Color textColor = isActive ? Game1.textColor : Game1.textColor * TabOpacity;
            bool isHovered = tab.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            IClickableMenu.drawTextureBox(b, tab.bounds.X, tab.bounds.Y,
                tab.bounds.Width, tab.bounds.Height, Color.White);

            Vector2 labelSize = Game1.smallFont.MeasureString(label);
            Vector2 textPos = new Vector2(
                tab.bounds.X + (tab.bounds.Width - labelSize.X) / 2,
                tab.bounds.Y + (tab.bounds.Height - labelSize.Y) / 2
            );

            if (isHovered)
            {
                Utility.drawTextWithShadow(b, label, Game1.smallFont, textPos + new Vector2(-1, 0), textColor * 0.8f);
                Utility.drawTextWithShadow(b, label, Game1.smallFont, textPos, textColor);
            }
            else
            {
                Utility.drawTextWithShadow(b, label, Game1.smallFont, textPos, textColor);
            }
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
        /// Draws all left panel buttons: direction arrows, new outfit, and outfits.
        /// </summary>
        public void DrawLeftPanelButtons(SpriteBatch b)
        {
            UIHelpers.DrawTextureButton(b, LeftArrowButton);
            UIHelpers.DrawTextureButton(b, RightArrowButton);

            UIHelpers.DrawTextButton(b, SaveButton, TranslationCache.ButtonNewOutfit);
            UIHelpers.DrawTextButton(b, TemplatesButton, TranslationCache.ButtonOutfits);
        }

        /// <summary>
        /// Draws the bottom panel buttons: apply and reset.
        /// </summary>
        public void DrawBottomButtons(SpriteBatch b)
        {
            UIHelpers.DrawTextButton(b, ApplyButton, TranslationCache.ButtonApply);
            UIHelpers.DrawTextButton(b, ResetButton, TranslationCache.ButtonReset);
        }

        #endregion

        /// <summary>
        /// Rotates the preview direction clockwise
        /// </summary>
        public void RotatePreviewLeft()
        {
            previewDirection = (previewDirection + 1) % 4;
        }

        /// <summary>
        /// Rotates the preview direction counter-clockwise
        /// </summary>
        public void RotatePreviewRight()
        {
            previewDirection = (previewDirection + 3) % 4;
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
            bool isHovered = bounds.Contains(mouseX, mouseY) && !isOpen;
            bool hasFilter = !string.IsNullOrEmpty(currentFilter);

            IClickableMenu.drawTextureBox(b, bounds.X, bounds.Y, bounds.Width, bounds.Height,
                isOpen ? Color.Wheat : Color.White);

            string displayText = hasFilter ? currentFilter! : TranslationCache.FilterAll;

            Vector2 textSize = Game1.smallFont.MeasureString(displayText);
            int maxTextWidth = bounds.Width - ClearButtonSize - ClearButtonRightMargin - 28;
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
                bounds.X + 20,
                bounds.Y + (bounds.Height - textSize.Y) / 2
            );

            if (isHovered)
            {
                Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPos + new Vector2(-1, 0), Game1.textColor * 0.8f);
                Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPos, Game1.textColor);
            }
            else
            {
                Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPos, Game1.textColor);
            }

            if (hasFilter && FilterClearButton != null)
            {
                DrawClearButton(b, FilterClearButton);
            }
        }

        private void DrawDropdownArrow(SpriteBatch b, Rectangle buttonBounds)
        {
            Rectangle sourceRect = new Rectangle(421, 472, 11, 12);
            float scale = 2f;
            Vector2 center = new Vector2(
                buttonBounds.X + buttonBounds.Width / 2,
                buttonBounds.Y + buttonBounds.Height / 2
            );
            Vector2 origin = new Vector2(5.5f, 6);

            b.Draw(
                Game1.mouseCursors,
                center,
                sourceRect,
                Color.White,
                0f,
                origin,
                scale,
                SpriteEffects.None,
                1f
            );
        }

        /// <summary>Draws the search bar background.</summary>
        /// <param name="b">SpriteBatch for drawing.</param>
        /// <param name="isFocused">Whether the search bar is focused.</param>
        /// <param name="hasText">Whether the search bar has text (to show clear button).</param>
        public void DrawSearchBar(SpriteBatch b, bool isFocused, bool hasText = false)
        {
            if (SearchBar == null)
                return;

            IClickableMenu.drawTextureBox(b, SearchBar.bounds.X, SearchBar.bounds.Y,
                SearchBar.bounds.Width, SearchBar.bounds.Height, Color.White);

            // Draw clear button if there's search text
            if (hasText && SearchClearButton != null)
            {
                DrawClearButton(b, SearchClearButton);
            }
        }

        /// <summary>Draws a small X clear button.</summary>
        private void DrawClearButton(SpriteBatch b, ClickableComponent button)
        {
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            bool isHovered = button.containsPoint(mouseX, mouseY);

            Rectangle sourceRect = new Rectangle(337, 494, 12, 12);
            float scale = isHovered ? 2.2f : 2f;
            Vector2 center = new Vector2(
                button.bounds.X + button.bounds.Width / 2,
                button.bounds.Y + button.bounds.Height / 2
            );
            Vector2 origin = new Vector2(6, 6);

            b.Draw(
                Game1.mouseCursors,
                center,
                sourceRect,
                Color.White,
                0f,
                origin,
                scale,
                SpriteEffects.None,
                1f
            );
        }

        /// <summary>Draws the lookup icon on the character preview.</summary>
        public void DrawLookupIcon(SpriteBatch b)
        {
            if (LookupButton == null)
                return;

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            bool isHovered = LookupButton.containsPoint(mouseX, mouseY);

            // Draw magnifying glass icon using game cursors
            Rectangle sourceRect = new Rectangle(240, 192, 16, 16);

            b.Draw(
                Game1.mouseCursors,
                new Vector2(LookupButton.bounds.X, LookupButton.bounds.Y),
                sourceRect,
                Color.White,
                0f,
                Vector2.Zero,
                2f,
                SpriteEffects.None,
                1f
            );
        }

        /// <summary>
        /// Checks if the mouse is hovering over any clickable UI element.
        /// </summary>
        public bool IsHoveringClickable(int mouseX, int mouseY)
        {
            if (AllTab.containsPoint(mouseX, mouseY) ||
                ShirtsTab.containsPoint(mouseX, mouseY) ||
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
