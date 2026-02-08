using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    public class OutfitUIBuilder
    {
        private readonly int configuredRows;
        private readonly int configuredColumns;
        private readonly int configuredSlotSize;

        public int SLOT_SIZE { get; private set; }
        public int VISIBLE_ITEMS { get; private set; }
        public int VISIBLE_ROWS { get; private set; }
        public int COLUMNS { get; private set; }

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

        public ClickableComponent SaveButton { get; private set; } = null!;
        public ClickableComponent WardrobeButton { get; private set; } = null!;

        public ClickableTextureComponent DyeColorButton { get; private set; } = null!;

        public ClickableTextureComponent GearButton { get; private set; } = null!;

        public ClickableTextureComponent LeftArrowButton { get; private set; } = null!;
        public ClickableTextureComponent RightArrowButton { get; private set; } = null!;
        private int previewDirection = 2;

        public ClickableTextureComponent GridScrollUpButton { get; private set; } = null!;
        public ClickableTextureComponent GridScrollDownButton { get; private set; } = null!;

        public Rectangle EquippedTextArea { get; private set; }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        private float savedMessageTimer = 0f;
        private bool showingSavedMessage = false;

        private RenderTarget2D? farmerRenderTarget = null;
        private SpriteBatch? farmerSpriteBatch = null;
        private bool previewDirty = true;

        private static readonly Rectangle FarmerSourceRect = new Rectangle(0, 0, 16, 32);
        private static readonly Rectangle FarmerBathingSourceRect = new Rectangle(0, 576, 16, 32);
        private static readonly Vector2 FarmerRenderPosition = new Vector2(32, 32);

        private static readonly FarmerSprite.AnimationFrame[] DirectionFrames = new[]
        {
            new FarmerSprite.AnimationFrame(12, 0, secondaryArm: false, flip: false), // 0 = Up
            new FarmerSprite.AnimationFrame(6, 0, secondaryArm: false, flip: false),  // 1 = Right
            new FarmerSprite.AnimationFrame(0, 0, secondaryArm: false, flip: false),  // 2 = Down
            new FarmerSprite.AnimationFrame(6, 0, secondaryArm: false, flip: true),   // 3 = Left
        };
        private const int FarmerRenderWidth = BackgroundSourceWidth * 2;  // 128
        private const int FarmerRenderHeight = BackgroundSourceHeight * 2; // 192

        public OutfitUIBuilder(ModConfig config)
        {
            configuredRows = Math.Clamp(config.VisibleRows, MinVisibleRows, MaxVisibleRows);
            configuredColumns = Math.Clamp(config.VisibleColumns, MinVisibleColumns, MaxVisibleColumns);
            configuredSlotSize = Math.Clamp(config.SlotSize, MinSlotSize, MaxSlotSize);
            Recalculate();
        }

        private int CalculateLeftPanelHeight()
        {
            int arrowHeight = (int)(ArrowNativeHeight * ArrowScale);
            int arrowRowHeight = arrowHeight + ElementGap;
            return CharacterPreviewHeight + arrowRowHeight + GapBetweenPortraitAndButtons
                   + (TabAndButtonHeight * 2) + ElementGap;
        }

        private int CalculateRightPanelHeight()
        {
            int gridHeight = VISIBLE_ROWS * SLOT_SIZE + (VISIBLE_ROWS - 1) * ItemSlotGap;
            return TabAndButtonHeight + ElementGap + gridHeight + GridToButtonGap + TabAndButtonHeight;
        }

        private int CalculateContentSectionHeight()
        {
            return Math.Max(CalculateRightPanelHeight(), CalculateLeftPanelHeight());
        }

        public int CalculateRequiredHeight()
        {
            return BorderPaddingV
                   + TabSectionHeight
                   + SectionGapV
                   + CalculateContentSectionHeight()
                   + BorderPaddingVBottom;
        }

        public int CalculateRequiredWidth()
        {
            int gridWidth = COLUMNS * SLOT_SIZE + (COLUMNS - 1) * ItemSlotGap;
            int contentWidth = LeftPanelWidth + SectionGapH + gridWidth;

            return BorderPaddingH + contentWidth + BorderPaddingH + BorderPaddingRightExtra;
        }

        public void Recalculate()
        {
            SLOT_SIZE = configuredSlotSize;
            COLUMNS = configuredColumns;
            VISIBLE_ROWS = configuredRows;
            VISIBLE_ITEMS = VISIBLE_ROWS * COLUMNS;

            Width = CalculateRequiredWidth();
            Height = CalculateRequiredHeight();

            X = (Game1.uiViewport.Width - Width) / 2;
            Y = (Game1.uiViewport.Height - Height) / 2;

            int tabSectionY = Y + BorderPaddingV;
            int tabCenterY = tabSectionY + (TabSectionHeight - TabAndButtonHeight) / 2;
            PositionTabs(tabCenterY);

            int contentSectionY = tabSectionY + TabSectionHeight + SectionGapV;
            int contentSectionHeight = CalculateContentSectionHeight();
            PositionContentSection(contentSectionY, contentSectionHeight);

            PositionCloseButton();

            PositionFloatingButtons();
        }

        private void PositionTabs(int tabY)
        {
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
        }

        private void PositionContentSection(int sectionY, int sectionHeight)
        {
            int gridWidth = COLUMNS * SLOT_SIZE + (COLUMNS - 1) * ItemSlotGap;
            int contentGroupWidth = LeftPanelWidth + SectionGapH + gridWidth;

            int usableWidth = Width - BorderPaddingRightExtra;
            int contentStartX = X + (usableWidth - contentGroupWidth) / 2;
            int leftPanelCenterX = contentStartX + LeftPanelWidth / 2;
            int rightPanelX = contentStartX + LeftPanelWidth + SectionGapH;

            int leftPanelHeight = CalculateLeftPanelHeight();
            int leftPanelY = sectionY + (sectionHeight - leftPanelHeight) / 2;
            PositionLeftPanel(leftPanelCenterX, leftPanelY);

            int rightPanelHeight = CalculateRightPanelHeight();
            int rightPanelY = sectionY + (sectionHeight - rightPanelHeight) / 2;
            PositionRightPanel(rightPanelX, rightPanelY, gridWidth);
        }

        private void PositionLeftPanel(int centerX, int startY)
        {
            PortraitBox = new Rectangle(
                centerX - CharacterPreviewWidth / 2,
                startY,
                CharacterPreviewWidth,
                CharacterPreviewHeight
            );

            int arrowWidth = (int)(ArrowNativeWidth * ArrowScale);
            int arrowHeight = (int)(ArrowNativeHeight * ArrowScale);
            int arrowY = PortraitBox.Bottom + ElementGap;
            int totalArrowsWidth = arrowWidth * 2 + ArrowGap;
            int arrowsStartX = centerX - totalArrowsWidth / 2;

            LeftArrowButton = new ClickableTextureComponent(
                new Rectangle(arrowsStartX, arrowY, arrowWidth, arrowHeight),
                Game1.mouseCursors,
                new Rectangle(352, 495, ArrowNativeWidth, ArrowNativeHeight),
                ArrowScale
            );
            RightArrowButton = new ClickableTextureComponent(
                new Rectangle(arrowsStartX + arrowWidth + ArrowGap, arrowY, arrowWidth, arrowHeight),
                Game1.mouseCursors,
                new Rectangle(365, 495, ArrowNativeWidth, ArrowNativeHeight),
                ArrowScale
            );

            LookupButton = new ClickableComponent(
                new Rectangle(
                    PortraitBox.Right - LookupIconSize - LookupIconMargin,
                    PortraitBox.Y + LookupIconMargin,
                    LookupIconSize,
                    LookupIconSize
                ),
                "Lookup"
            );

            int newOutfitButtonWidth = UIHelpers.CalculateButtonWidth(TranslationCache.ButtonNewOutfit);
            int outfitsButtonWidth = UIHelpers.CalculateButtonWidth(TranslationCache.ButtonOutfits);
            int leftButtonWidth = Math.Max(newOutfitButtonWidth, outfitsButtonWidth);

            int leftButtonsStartX = centerX - leftButtonWidth / 2;
            int saveButtonY = arrowY + arrowHeight + GapBetweenPortraitAndButtons;
            int wardrobeButtonY = saveButtonY + TabAndButtonHeight + ElementGap;

            SaveButton = new ClickableComponent(
                new Rectangle(leftButtonsStartX, saveButtonY, leftButtonWidth, TabAndButtonHeight),
                TranslationCache.ButtonNewOutfit
            );
            WardrobeButton = new ClickableComponent(
                new Rectangle(leftButtonsStartX, wardrobeButtonY, leftButtonWidth, TabAndButtonHeight),
                TranslationCache.ButtonOutfits
            );
        }

        private void PositionRightPanel(int panelX, int startY, int gridWidth)
        {
            int halfWidth = (gridWidth - FilterSearchGap) / 2;

            ModFilterDropdown = new ClickableComponent(
                new Rectangle(panelX, startY, halfWidth, TabAndButtonHeight),
                "ModFilterDropdown"
            );

            FilterClearButton = new ClickableComponent(
                new Rectangle(
                    panelX + halfWidth - ClearButtonRightMargin - ClearButtonSize,
                    startY + (TabAndButtonHeight - ClearButtonSize) / 2,
                    ClearButtonSize,
                    ClearButtonSize
                ),
                "FilterClear"
            );

            int searchBarX = panelX + halfWidth + FilterSearchGap;
            SearchBar = new ClickableComponent(
                new Rectangle(searchBarX, startY, halfWidth, TabAndButtonHeight),
                "SearchBar"
            );

            SearchClearButton = new ClickableComponent(
                new Rectangle(
                    searchBarX + halfWidth - ClearButtonRightMargin - ClearButtonSize,
                    startY + (TabAndButtonHeight - ClearButtonSize) / 2,
                    ClearButtonSize,
                    ClearButtonSize
                ),
                "SearchClear"
            );

            int gridY = startY + TabAndButtonHeight + ElementGap;

            ItemSlots.Clear();
            for (int row = 0; row < VISIBLE_ROWS; row++)
            {
                for (int col = 0; col < COLUMNS; col++)
                {
                    int slotX = panelX + col * (SLOT_SIZE + ItemSlotGap);
                    int slotY = gridY + row * (SLOT_SIZE + ItemSlotGap);
                    ItemSlots.Add(new ClickableComponent(
                        new Rectangle(slotX, slotY, SLOT_SIZE, SLOT_SIZE),
                        (row * COLUMNS + col).ToString()
                    ));
                }
            }

            int gridHeight = VISIBLE_ROWS * SLOT_SIZE + (VISIBLE_ROWS - 1) * ItemSlotGap;
            int bottomButtonsY = gridY + gridHeight + GridToButtonGap;

            int applyWidth = UIHelpers.CalculateButtonWidth(TranslationCache.ButtonApply);
            int resetWidth = UIHelpers.CalculateButtonWidth(TranslationCache.CommonReset);

            int resetX = panelX + gridWidth - resetWidth;
            int applyX = resetX - ElementGap - applyWidth;

            ApplyButton = new ClickableComponent(
                new Rectangle(applyX, bottomButtonsY, applyWidth, TabAndButtonHeight),
                TranslationCache.ButtonApply
            );

            ResetButton = new ClickableComponent(
                new Rectangle(resetX, bottomButtonsY, resetWidth, TabAndButtonHeight),
                TranslationCache.CommonReset
            );

            int scrollArrowW = (int)(11 * GridScrollArrowScale);
            int scrollArrowH = (int)(12 * GridScrollArrowScale);
            int gridBoxRight = panelX + gridWidth + ContentBoxPadding;
            int scrollArrowX = gridBoxRight + GridScrollArrowGap;

            GridScrollUpButton = new ClickableTextureComponent(
                new Rectangle(scrollArrowX, gridY, scrollArrowW, scrollArrowH),
                Game1.mouseCursors,
                new Rectangle(421, 459, 11, 12),
                GridScrollArrowScale
            );
            GridScrollDownButton = new ClickableTextureComponent(
                new Rectangle(scrollArrowX, gridY + gridHeight - scrollArrowH, scrollArrowW, scrollArrowH),
                Game1.mouseCursors,
                new Rectangle(421, 472, 11, 12),
                GridScrollArrowScale
            );
        }

        private void PositionCloseButton()
        {
            CloseButton = new ClickableTextureComponent(
                new Rectangle(X + Width - CloseButtonSize - CloseButtonEdgeMargin, Y + CloseButtonEdgeMargin, CloseButtonSize, CloseButtonSize),
                Game1.mouseCursors,
                new Rectangle(337, 494, 12, 12),
                4f
            );
        }

        private void PositionFloatingButtons()
        {
            int buttonSize = (int)(CloseButtonSize * 1.1f);
            int dyePanelHeight = DyeColorManager.ComputePanelHeight();
            int dyePanelTopY = Y + (Height - dyePanelHeight) / 2;

            int buttonX = X + Width + 5;
            int dyeColorBtnY = dyePanelTopY - 5 - buttonSize;
            int gearBtnY = dyeColorBtnY - 5 - buttonSize;

            GearButton = new ClickableTextureComponent(
                new Rectangle(buttonX, gearBtnY, buttonSize, buttonSize),
                Game1.mouseCursors,
                new Rectangle(30, 428, 10, 10),
                buttonSize / 10f
            );

            DyeColorButton = new ClickableTextureComponent(
                new Rectangle(buttonX, dyeColorBtnY, buttonSize, buttonSize),
                Game1.objectSpriteSheet,
                new Rectangle(32, 48, 16, 16),
                buttonSize / 16f
            );
        }

        public void DrawTabWithText(SpriteBatch b, ClickableComponent tab, string label, bool isActive)
        {
            Color textColor = isActive ? Game1.textColor : Game1.textColor * TabOpacity;
            bool isHovered = tab.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            UIHelpers.DrawTextureBox(b, tab.bounds.X, tab.bounds.Y,
                tab.bounds.Width, tab.bounds.Height, isActive ? Color.White : Color.White * 0.8f);

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

        public void MarkPreviewDirty()
        {
            previewDirty = true;
        }

        public void DrawPlayerPreview(SpriteBatch b)
        {
            b.Draw(Game1.daybg, PortraitBox, Color.White);

            InitializeFarmerRenderResources();

            if (previewDirty)
            {
                RenderFarmerToTarget();
                previewDirty = false;
            }

            b.Draw(farmerRenderTarget, PortraitBox, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
        }

        private void RenderFarmerToTarget()
        {
            var renderTargets = Game1.graphics.GraphicsDevice.GetRenderTargets();
            Game1.graphics.GraphicsDevice.SetRenderTarget(farmerRenderTarget);
            Game1.graphics.GraphicsDevice.Clear(Color.Transparent);

            int frameIndex = DirectionFrames[previewDirection].frame;
            int baseY = Game1.player.bathingClothes.Value ? 576 : 0;
            int sourceX = (frameIndex * 16) % 96;
            int sourceY = baseY + (frameIndex * 16) / 96 * 32;
            Rectangle sourceRect = new Rectangle(sourceX, sourceY, FarmerSpriteWidth, FarmerSpriteHeight);

            int originalEyes = Game1.player.currentEyes;
            Game1.player.currentEyes = 0;

            farmerSpriteBatch!.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

            FarmerRenderer.isDrawingForUI = true;
            DrawFarmerToRenderTarget(sourceRect, Color.White);

            FarmerRenderer.isDrawingForUI = false;

            farmerSpriteBatch.End();

            Game1.player.currentEyes = originalEyes;

            Game1.graphics.GraphicsDevice.SetRenderTargets(renderTargets);
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

        public void DrawItemList(SpriteBatch b, int scrollOffset, int totalItems)
        {
            if (ItemSlots.Count == 0)
                return;

            int listX = ItemSlots[0].bounds.X;
            int listY = ItemSlots[0].bounds.Y;
            int listWidth = (ItemSlots[COLUMNS - 1].bounds.Right - ItemSlots[0].bounds.Left);
            int listHeight = VISIBLE_ROWS * SLOT_SIZE + Math.Max(0, VISIBLE_ROWS - 1) * ItemSlotGap;

            UIHelpers.DrawTextureBox(b, listX - ContentBoxPadding, listY - ContentBoxPadding,
                listWidth + ContentBoxPadding * 2, listHeight + ContentBoxPadding * 2, Color.White);

            int totalRows = Math.Max(1, (int)Math.Ceiling(totalItems / (float)COLUMNS));
            int maxScroll = Math.Max(0, totalRows - VISIBLE_ROWS);

            if (scrollOffset > 0)
            {
                UIHelpers.DrawTextureButton(b, GridScrollUpButton);
            }

            if (scrollOffset < maxScroll)
            {
                UIHelpers.DrawTextureButton(b, GridScrollDownButton);
            }
        }

        #region Button Drawing

        public void DrawLeftPanelButtons(SpriteBatch b)
        {
            UIHelpers.DrawTextureButton(b, LeftArrowButton);
            UIHelpers.DrawTextureButton(b, RightArrowButton);

            UIHelpers.DrawTextButton(b, SaveButton, TranslationCache.ButtonNewOutfit);
            UIHelpers.DrawTextButton(b, WardrobeButton, TranslationCache.ButtonOutfits);
        }

        public void DrawBottomButtons(SpriteBatch b)
        {
            UIHelpers.DrawTextButton(b, ApplyButton, TranslationCache.ButtonApply);
            UIHelpers.DrawTextButton(b, ResetButton, TranslationCache.CommonReset);
        }

        public void DrawFloatingButtons(SpriteBatch b)
        {
            DrawGearButton(b);
            DrawDyeColorButton(b);
        }

        private void DrawDyeColorButton(SpriteBatch b)
        {
            bool isHovered = DyeColorButton.containsPoint(Game1.getMouseX(), Game1.getMouseY());
            float buttonScale = isHovered ? ButtonHoveringScale : 1f;

            int bgSize = (int)(DyeColorButton.bounds.Width * buttonScale);
            int bgX = DyeColorButton.bounds.X + (DyeColorButton.bounds.Width - bgSize) / 2;
            int bgY = DyeColorButton.bounds.Y + (DyeColorButton.bounds.Height - bgSize) / 2;

            UIHelpers.DrawTextureBox(b, bgX, bgY, bgSize, bgSize, Color.White, 1f, 4, 0.6f);

            Vector2 iconCenter = new Vector2(
                DyeColorButton.bounds.X + DyeColorButton.bounds.Width / 2,
                DyeColorButton.bounds.Y + DyeColorButton.bounds.Height / 2
            );
            Rectangle sourceRect = new Rectangle(32, 48, 16, 16);
            float iconScale = (bgSize / 16f) * 0.6f;
            Vector2 origin = new Vector2(8, 8);
            b.Draw(Game1.objectSpriteSheet, iconCenter, sourceRect, Color.White, 0f,
                origin, iconScale, SpriteEffects.None, 1f);
        }

        #endregion

        public void RotatePreviewLeft()
        {
            previewDirection = (previewDirection + 1) % 4;
            previewDirty = true;
        }

        public void RotatePreviewRight()
        {
            previewDirection = (previewDirection + 3) % 4;
            previewDirty = true;
        }

        public void DrawCloseButton(SpriteBatch b)
        {
            UIHelpers.DrawTextureButton(b, CloseButton);
        }

        public void DrawGearButton(SpriteBatch b)
        {
            bool isHovered = GearButton.containsPoint(Game1.getMouseX(), Game1.getMouseY());
            float buttonScale = isHovered ? ButtonHoveringScale : 1f;

            int bgSize = (int)(GearButton.bounds.Width * buttonScale);
            int bgX = GearButton.bounds.X + (GearButton.bounds.Width - bgSize) / 2;
            int bgY = GearButton.bounds.Y + (GearButton.bounds.Height - bgSize) / 2;

            UIHelpers.DrawTextureBox(b, bgX, bgY, bgSize, bgSize, Color.White, 1f, 4, 0.6f);

            Vector2 iconCenter = new Vector2(
                GearButton.bounds.X + GearButton.bounds.Width / 2,
                GearButton.bounds.Y + GearButton.bounds.Height / 2
            );
            Rectangle editSource = new Rectangle(30, 428, 10, 10);
            float iconScale = (bgSize / 10f) * 0.6f;
            Vector2 origin = new Vector2(5, 5);
            b.Draw(Game1.mouseCursors, iconCenter, editSource, Color.White, 0f,
                origin, iconScale, SpriteEffects.None, 1f);
        }

        public void DrawTitle(SpriteBatch b)
        {
            string title = TranslationCache.MenuTitle;
            Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
            Utility.drawTextWithShadow(b, title, Game1.dialogueFont,
                new Vector2(X + (Width - titleSize.X) / 2, Y + ContentBoxPadding),
                Game1.textColor);
        }

        public void ShowAppliedMessage()
        {
            savedMessageTimer = SavedMessageDurationMs;
            showingSavedMessage = false;
        }

        public void ShowSavedMessage()
        {
            savedMessageTimer = SavedMessageDurationMs;
            showingSavedMessage = true;
        }

        public void Update(float elapsedMilliseconds)
        {
            if (savedMessageTimer > 0)
            {
                savedMessageTimer -= elapsedMilliseconds;
            }
        }

        public void DrawSavedMessage(SpriteBatch b)
        {
            if (savedMessageTimer > 0)
            {
                string message = showingSavedMessage ? TranslationCache.MessageSaved : TranslationCache.MessageApplied;
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
        public void DrawModFilterDropdown(SpriteBatch b, string? currentFilter, bool isOpen)
        {
            if (ModFilterDropdown == null)
                return;

            bool hasFilter = !string.IsNullOrEmpty(currentFilter);
            string displayText = hasFilter ? currentFilter! : TranslationCache.FilterAll;

            UIHelpers.DrawDropdownButton(
                b,
                ModFilterDropdown.bounds,
                displayText,
                isOpen,
                clearButton: FilterClearButton,
                hasValue: hasFilter,
                drawClearButton: UIHelpers.DrawClearButton
            );
        }

        /// <summary>Draws the search bar background.</summary>
        public void DrawSearchBar(SpriteBatch b, bool isFocused, bool hasText = false)
        {
            if (SearchBar == null)
                return;

            UIHelpers.DrawTextureBox(b, SearchBar.bounds.X, SearchBar.bounds.Y,
                SearchBar.bounds.Width, SearchBar.bounds.Height, Color.White);

            if (hasText && SearchClearButton != null)
            {
                UIHelpers.DrawClearButton(b, SearchClearButton);
            }
        }

        /// <summary>Draws the lookup icon on the character preview.</summary>
        public void DrawLookupIcon(SpriteBatch b)
        {
            if (LookupButton == null)
                return;

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            bool isHovered = LookupButton.containsPoint(mouseX, mouseY);

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
                DyeColorButton.containsPoint(mouseX, mouseY) ||
                CloseButton.containsPoint(mouseX, mouseY) ||
                GearButton.containsPoint(mouseX, mouseY) ||
                LeftArrowButton.containsPoint(mouseX, mouseY) ||
                RightArrowButton.containsPoint(mouseX, mouseY) ||
                GridScrollUpButton.containsPoint(mouseX, mouseY) ||
                GridScrollDownButton.containsPoint(mouseX, mouseY))
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
