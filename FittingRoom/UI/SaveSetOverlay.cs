using System;
using FittingRoom.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using static FittingRoom.OutfitLayoutConstants;

namespace FittingRoom
{
    public class SaveSetOverlay : IClickableMenu
    {
        private readonly IClickableMenu parentMenu;
        private readonly SaveSetUIBuilder uiBuilder;
        private readonly TemplateManager templateManager;
        private readonly SaveSetDropdownManager tagDropdownManager;
        private readonly Action onSaveComplete;

        private readonly TextBox nameTextBox;
        private bool isFavorite = false;

        private bool includeShirt = true;
        private bool includePants = true;
        private bool includeHat = true;

        private readonly string? capturedShirtId;
        private readonly string? capturedPantsId;
        private readonly string? capturedHatId;

        // RenderTarget2D for character preview (matching main menu pattern)
        private RenderTarget2D? farmerRenderTarget = null;
        private SpriteBatch? farmerSpriteBatch = null;
        private bool previewDirty = true;

        private static readonly FarmerSprite.AnimationFrame FrontFacingFrame = new FarmerSprite.AnimationFrame(0, 0, secondaryArm: false, flip: false);
        private const int FarmerSpriteWidth = 16;
        private const int FarmerSpriteHeight = 32;
        private const int FarmerRenderScale = 4;

        // Cached item objects for drawing
        private Clothing? cachedShirt;
        private Clothing? cachedPants;
        private Hat? cachedHat;

        public SaveSetOverlay(IClickableMenu parentMenu, TemplateManager templateManager, Action onSaveComplete)
        {
            this.parentMenu = parentMenu ?? throw new ArgumentNullException(nameof(parentMenu));
            this.templateManager = templateManager ?? throw new ArgumentNullException(nameof(templateManager));
            this.onSaveComplete = onSaveComplete ?? throw new ArgumentNullException(nameof(onSaveComplete));

            width = SaveSetOverlayWidth;
            height = SaveSetUIBuilder.CalculateRequiredHeight();

            uiBuilder = new SaveSetUIBuilder(width, height);
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;
            tagDropdownManager = new SaveSetDropdownManager(uiBuilder);

            // Initialize text box
            nameTextBox = new TextBox(
                Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                null,
                Game1.smallFont,
                Game1.textColor)
            {
                Text = "",
                Selected = true
            };
            UpdateTextBoxBounds();

            // Capture current outfit
            capturedShirtId = OutfitState.GetClothingId(Game1.player.shirtItem.Value);
            capturedPantsId = OutfitState.GetClothingId(Game1.player.pantsItem.Value);
            capturedHatId = OutfitState.GetHatIdFromItem(Game1.player.hat.Value);

            // Set initial checkbox states based on what's equipped
            includeShirt = !string.IsNullOrEmpty(capturedShirtId) && capturedShirtId != NoShirtId;
            includePants = !string.IsNullOrEmpty(capturedPantsId) && capturedPantsId != NoPantsId;
            includeHat = !string.IsNullOrEmpty(capturedHatId) && capturedHatId != NoHatId;

            // Cache item objects for drawing
            CacheItemObjects();
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);

            parentMenu.gameWindowSizeChanged(oldBounds, newBounds);

            uiBuilder.Recalculate();
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;

            UpdateTextBoxBounds();

            if (tagDropdownManager.IsOpen)
            {
                tagDropdownManager.BuildOptions();
            }
        }

        private void CacheItemObjects()
        {
            // Cache shirt
            if (HasShirt() && capturedShirtId != null)
            {
                string qualifiedId = "(S)" + capturedShirtId;
                cachedShirt = ItemRegistry.Create<Clothing>(qualifiedId);
            }

            // Cache pants
            if (HasPants() && capturedPantsId != null)
            {
                string qualifiedId = "(P)" + capturedPantsId;
                cachedPants = ItemRegistry.Create<Clothing>(qualifiedId);
            }

            // Cache hat
            if (HasHat() && capturedHatId != null)
            {
                string qualifiedId = "(H)" + capturedHatId;
                cachedHat = ItemRegistry.Create<Hat>(qualifiedId);
            }
        }

        private void UpdateTextBoxBounds()
        {
            if (uiBuilder.NameInputArea == null)
                return;

            Rectangle bounds = uiBuilder.NameInputArea.bounds;
            nameTextBox.X = bounds.X + 12;
            nameTextBox.Y = bounds.Y + ((bounds.Height - 48) / 2);
            nameTextBox.Width = bounds.Width - 24;
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            // Handle dropdown first if open
            if (tagDropdownManager.IsOpen)
            {
                tagDropdownManager.HandleClick(x, y, out bool clickedOption);
                if (clickedOption)
                {
                    if (playSound) Game1.playSound("smallSelect");
                    return;
                }

                // Clicked outside dropdown, close it
                tagDropdownManager.Close();
                return;
            }

            // Close button
            if (uiBuilder.CloseButton.containsPoint(x, y))
            {
                CloseOverlay();
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            // Cancel button
            if (uiBuilder.CancelButton.containsPoint(x, y))
            {
                CloseOverlay();
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            // Save button
            if (uiBuilder.SaveButton.containsPoint(x, y))
            {
                HandleSave(playSound);
                return;
            }

            // Tag dropdown
            if (uiBuilder.TagDropdownButton.containsPoint(x, y))
            {
                tagDropdownManager.Toggle();
                if (playSound) Game1.playSound("smallSelect");
                return;
            }

            // Favorite checkbox
            if (uiBuilder.FavoriteCheckbox.containsPoint(x, y))
            {
                isFavorite = !isFavorite;
                if (playSound) Game1.playSound("smallSelect");
                return;
            }

            // Item slot clicks (toggle inclusion)
            if (uiBuilder.ShirtSlot.Contains(x, y) && HasShirt())
            {
                includeShirt = !includeShirt;
                previewDirty = true;
                if (playSound) Game1.playSound("smallSelect");
                return;
            }

            if (uiBuilder.PantsSlot.Contains(x, y) && HasPants())
            {
                includePants = !includePants;
                previewDirty = true;
                if (playSound) Game1.playSound("smallSelect");
                return;
            }

            if (uiBuilder.HatSlot.Contains(x, y) && HasHat())
            {
                includeHat = !includeHat;
                previewDirty = true;
                if (playSound) Game1.playSound("smallSelect");
                return;
            }

            // Name input area - keep textbox selected
            if (uiBuilder.NameInputArea.containsPoint(x, y))
            {
                nameTextBox.Selected = true;
                return;
            }
        }

        public override void receiveKeyPress(Keys key)
        {
            // Handle dropdown input first
            if (tagDropdownManager.IsOpen)
            {
                if (tagDropdownManager.HandleKeyPress(key))
                    return;
            }

            if (key == Keys.Escape)
            {
                CloseOverlay();
                Game1.playSound("bigDeSelect");
                return;
            }

            if (key == Keys.Enter)
            {
                HandleSave(true);
                return;
            }

            // Block menu button from closing
            if (Game1.options.doesInputListContain(Game1.options.menuButton, key))
            {
                return;
            }
        }

        public override void receiveScrollWheelAction(int direction)
        {
            if (tagDropdownManager.IsOpen)
            {
                if (tagDropdownManager.HandleScrollWheel(direction))
                {
                    Game1.playSound("shiny4");
                }
            }
        }

        public override void update(GameTime time)
        {
            base.update(time);
            nameTextBox.Update();
            nameTextBox.Selected = true;
        }

        private void HandleSave(bool playSound)
        {
            string name = nameTextBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(name))
            {
                if (playSound) Game1.playSound("cancel");
                return;
            }

            templateManager.CreateFromCurrentOutfit(
                name,
                tagDropdownManager.SelectedTag,
                isFavorite,
                includeShirt,
                includePants,
                includeHat
            );

            if (playSound) Game1.playSound("coin");
            onSaveComplete();
            CloseOverlay();
        }

        private void CloseOverlay()
        {
            // Restore parent menu as active menu
            Game1.activeClickableMenu = parentMenu;
        }

        private bool HasShirt() => !string.IsNullOrEmpty(capturedShirtId) && capturedShirtId != NoShirtId;
        private bool HasPants() => !string.IsNullOrEmpty(capturedPantsId) && capturedPantsId != NoPantsId;
        private bool HasHat() => !string.IsNullOrEmpty(capturedHatId) && capturedHatId != NoHatId;

        public override void draw(SpriteBatch b)
        {
            if (parentMenu is OutfitMenu outfitMenu)
            {
                outfitMenu.IsOverlayBlocking = true;
            }

            parentMenu.draw(b);

            if (parentMenu is OutfitMenu outfitMenuAfter)
            {
                outfitMenuAfter.IsOverlayBlocking = false;
            }

            b.Draw(Game1.fadeToBlackRect,
                new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
                Color.Black * BackgroundOverlayOpacity);

            UIHelpers.DrawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

            bool showPlaceholder = string.IsNullOrEmpty(nameTextBox.Text);
            uiBuilder.DrawNameInput(b, nameTextBox.Text ?? "", showPlaceholder);
            uiBuilder.DrawNameCursor(b, nameTextBox.Text ?? "", nameTextBox.Selected);

            uiBuilder.DrawPreviewBackground(b);
            DrawCharacterPreview(b);

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            uiBuilder.DrawItemSlot(b, uiBuilder.HatSlot, includeHat, HasHat(), mouseX, mouseY);
            uiBuilder.DrawItemSlot(b, uiBuilder.ShirtSlot, includeShirt, HasShirt(), mouseX, mouseY);
            uiBuilder.DrawItemSlot(b, uiBuilder.PantsSlot, includePants, HasPants(), mouseX, mouseY);
            DrawItemSprites(b, includeShirt, includePants, includeHat);

            uiBuilder.DrawTagDropdown(b, tagDropdownManager.SelectedTagDisplay, tagDropdownManager.IsOpen);
            uiBuilder.DrawFavoriteCheckbox(b, isFavorite);

            uiBuilder.DrawButtons(b);
            uiBuilder.DrawCloseButton(b);

            if (tagDropdownManager.IsOpen)
            {
                uiBuilder.DrawDropdownOptions(b, tagDropdownManager.Options,
                    tagDropdownManager.FirstVisibleIndex, tagDropdownManager.MaxVisibleItems,
                    tagDropdownManager.SelectedTag);
            }

            DrawItemTooltips(b, mouseX, mouseY);

            drawMouse(b);
        }

        private void DrawCharacterPreview(SpriteBatch b)
        {
            InitializeFarmerRenderResources();

            if (previewDirty)
            {
                RenderFarmerToTarget();
                previewDirty = false;
            }

            b.Draw(farmerRenderTarget, uiBuilder.PreviewBox, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.5f);
        }

        private void InitializeFarmerRenderResources()
        {
            if (farmerRenderTarget == null || farmerRenderTarget.IsDisposed)
            {
                farmerRenderTarget = new RenderTarget2D(
                    Game1.graphics.GraphicsDevice,
                    SaveSetPreviewWidth,
                    SaveSetPreviewHeight,
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

        private void RenderFarmerToTarget()
        {
            var renderTargets = Game1.graphics.GraphicsDevice.GetRenderTargets();
            Game1.graphics.GraphicsDevice.SetRenderTarget(farmerRenderTarget);
            Game1.graphics.GraphicsDevice.Clear(Color.Transparent);

            int baseY = Game1.player.bathingClothes.Value ? 576 : 0;
            Rectangle sourceRect = new Rectangle(0, baseY, FarmerSpriteWidth, FarmerSpriteHeight);

            int scaledWidth = FarmerSpriteWidth * FarmerRenderScale;
            int scaledHeight = FarmerSpriteHeight * FarmerRenderScale;
            Vector2 centeredPosition = new Vector2(
                (SaveSetPreviewWidth - scaledWidth) / 2,
                (SaveSetPreviewHeight - scaledHeight) / 2
            );

            int originalEyes = Game1.player.currentEyes;
            Game1.player.currentEyes = 0;

            // Temporarily hide excluded items for preview
            var savedShirt = Game1.player.shirtItem.Value;
            var savedPants = Game1.player.pantsItem.Value;
            var savedHat = Game1.player.hat.Value;

            try
            {
                if (!includeShirt)
                    Game1.player.shirtItem.Value = null;
                if (!includePants)
                    Game1.player.pantsItem.Value = null;
                if (!includeHat)
                    Game1.player.hat.Value = null;

                Game1.player.FarmerRenderer.MarkSpriteDirty();

                farmerSpriteBatch!.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

                FarmerRenderer.isDrawingForUI = true;
                Game1.player.FarmerRenderer.draw(
                    farmerSpriteBatch,
                    FrontFacingFrame,
                    0,
                    sourceRect,
                    centeredPosition,
                    Vector2.Zero,
                    0.8f,
                    2,
                    Color.White,
                    0f,
                    1f,
                    Game1.player
                );

                if (Game1.timeOfDay >= NightTimeStartHour)
                {
                    Game1.player.FarmerRenderer.draw(
                        farmerSpriteBatch,
                        FrontFacingFrame,
                        0,
                        sourceRect,
                        centeredPosition,
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

                farmerSpriteBatch.End();
            }
            finally
            {
                // Restore original equipment
                Game1.player.shirtItem.Value = savedShirt;
                Game1.player.pantsItem.Value = savedPants;
                Game1.player.hat.Value = savedHat;
                Game1.player.FarmerRenderer.MarkSpriteDirty();
            }

            Game1.player.currentEyes = originalEyes;

            Game1.graphics.GraphicsDevice.SetRenderTargets(renderTargets);
        }

        private void DrawItemSprites(SpriteBatch b, bool shirtIncluded, bool pantsIncluded, bool hatIncluded)
        {
            const float includedTransparency = 1f;
            const float excludedTransparency = 0.4f;

            // Draw items in z-order: Hat (back) → Shirt → Pants (front)
            if (HasHat() && cachedHat != null)
            {
                Rectangle slot = uiBuilder.HatSlot;
                float transparency = hatIncluded ? includedTransparency : excludedTransparency;
                Vector2 drawPos = new Vector2(
                    slot.X + (slot.Width - DrawnItemSize) / 2,
                    slot.Y + (slot.Height - DrawnItemSize) / 2
                );
                cachedHat.drawInMenu(b, drawPos, 1f, transparency, 0.9f, StackDrawType.Hide);
            }

            if (HasShirt() && cachedShirt != null)
            {
                Rectangle slot = uiBuilder.ShirtSlot;
                float transparency = shirtIncluded ? includedTransparency : excludedTransparency;
                Vector2 drawPos = new Vector2(
                    slot.X + (slot.Width - DrawnItemSize) / 2,
                    slot.Y + (slot.Height - DrawnItemSize) / 2
                );
                cachedShirt.drawInMenu(b, drawPos, 1f, transparency, 0.9f, StackDrawType.Hide);
            }

            if (HasPants() && cachedPants != null)
            {
                Rectangle slot = uiBuilder.PantsSlot;
                float transparency = pantsIncluded ? includedTransparency : excludedTransparency;
                Vector2 drawPos = new Vector2(
                    slot.X + (slot.Width - DrawnItemSize) / 2,
                    slot.Y + (slot.Height - DrawnItemSize) / 2
                );
                cachedPants.drawInMenu(b, drawPos, 1f, transparency, 0.9f, StackDrawType.Hide);
            }
        }

        private void DrawItemTooltips(SpriteBatch b, int mouseX, int mouseY)
        {
            // Shirt tooltip
            if (uiBuilder.ShirtSlot.Contains(mouseX, mouseY) && HasShirt() && cachedShirt != null)
            {
                IClickableMenu.drawToolTip(b, cachedShirt.getDescription(), cachedShirt.DisplayName, cachedShirt);
            }
            // Pants tooltip
            else if (uiBuilder.PantsSlot.Contains(mouseX, mouseY) && HasPants() && cachedPants != null)
            {
                IClickableMenu.drawToolTip(b, cachedPants.getDescription(), cachedPants.DisplayName, cachedPants);
            }
            // Hat tooltip
            else if (uiBuilder.HatSlot.Contains(mouseX, mouseY) && HasHat() && cachedHat != null)
            {
                IClickableMenu.drawToolTip(b, cachedHat.getDescription(), cachedHat.DisplayName, cachedHat);
            }
        }

        protected override void cleanupBeforeExit()
        {
            base.cleanupBeforeExit();

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
