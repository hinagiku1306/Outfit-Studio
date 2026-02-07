using System;
using System.Collections.Generic;
using System.Linq;
using FittingRoom.Models;
using FittingRoom.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using StardewModdingAPI;
using StardewValley.Objects;
using static FittingRoom.OutfitLayoutConstants;

namespace FittingRoom
{
    public class SaveSetOverlay : IClickableMenu
    {
        private readonly IClickableMenu parentMenu;
        private readonly SaveSetUIBuilder uiBuilder;
        private readonly OutfitSetStore store;
        private readonly TagPickerManager tagPickerManager;
        private readonly Action onSaveComplete;
        private readonly OutfitSet? editingSet;

        private readonly TextBox nameTextBox;
        private bool isFavorite;
        private bool isLocalOnly;
        private HashSet<string> selectedTags = new();

        private bool includeShirt = true;
        private bool includePants = true;
        private bool includeHat = true;

        private readonly string? capturedShirtId;
        private readonly string? capturedPantsId;
        private readonly string? capturedHatId;

        private RenderTarget2D? farmerRenderTarget;
        private SpriteBatch? farmerSpriteBatch;
        private bool previewDirty = true;

        private float nameBoxJiggleTimer;
        private const float NameBoxJiggleDuration = 300f;
        private const float NameBoxJiggleIntensity = 4f;

        private static readonly FarmerSprite.AnimationFrame FrontFacingFrame = new(0, 0, secondaryArm: false, flip: false);
        private const int FarmerSpriteWidth = 16;
        private const int FarmerSpriteHeight = 32;
        private const int FarmerRenderScale = 4;

        private Clothing? cachedShirt;
        private Clothing? cachedPants;
        private Hat? cachedHat;

        public bool IsEditing => editingSet != null;

        public SaveSetOverlay(IClickableMenu parentMenu, OutfitSetStore store, Action onSaveComplete, OutfitSet? editingSet = null)
        {
            this.parentMenu = parentMenu ?? throw new ArgumentNullException(nameof(parentMenu));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.onSaveComplete = onSaveComplete ?? throw new ArgumentNullException(nameof(onSaveComplete));
            this.editingSet = editingSet;

            tagPickerManager = new TagPickerManager(store);

            width = SaveSetOverlayWidth;
            height = SaveSetUIBuilder.CalculateRequiredHeight();

            uiBuilder = new SaveSetUIBuilder(width, height);
            uiBuilder.UpdateTagsRowLayout();
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;

            if (editingSet != null)
            {
                capturedShirtId = editingSet.ShirtId;
                capturedPantsId = editingSet.PantsId;
                capturedHatId = editingSet.HatId;

                includeShirt = !string.IsNullOrEmpty(capturedShirtId);
                includePants = !string.IsNullOrEmpty(capturedPantsId);
                includeHat = !string.IsNullOrEmpty(capturedHatId);

                isFavorite = editingSet.IsFavorite;
                isLocalOnly = !editingSet.IsGlobal;
                selectedTags = new HashSet<string>(editingSet.Tags);
            }
            else
            {
                capturedShirtId = OutfitState.GetClothingId(Game1.player.shirtItem.Value);
                capturedPantsId = OutfitState.GetClothingId(Game1.player.pantsItem.Value);
                capturedHatId = OutfitState.GetHatIdFromItem(Game1.player.hat.Value);

                includeShirt = !string.IsNullOrEmpty(capturedShirtId) && capturedShirtId != NoShirtId;
                includePants = !string.IsNullOrEmpty(capturedPantsId) && capturedPantsId != NoPantsId;
                includeHat = !string.IsNullOrEmpty(capturedHatId) && capturedHatId != NoHatId;
            }

            nameTextBox = new TextBox(
                Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                null,
                Game1.smallFont,
                Game1.textColor)
            {
                Text = editingSet?.Name ?? "",
                Selected = true
            };
            UpdateTextBoxBounds();

            CacheItemObjects();

            if (ModEntry.Config.AutoOpenTagMenu)
            {
                OpenTagPicker();
            }
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);

            parentMenu.gameWindowSizeChanged(oldBounds, newBounds);

            uiBuilder.Recalculate();
            uiBuilder.UpdateTagsRowLayout();
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;

            UpdateTextBoxBounds();

            if (tagPickerManager.IsOpen)
            {
                Rectangle overlayBounds = new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height);
                tagPickerManager.UpdateParentBounds(overlayBounds);
            }
        }

        private void CacheItemObjects()
        {
            if (HasShirt() && capturedShirtId != null)
            {
                string qualifiedId = "(S)" + capturedShirtId;
                cachedShirt = ItemRegistry.Create<Clothing>(qualifiedId);
            }

            if (HasPants() && capturedPantsId != null)
            {
                string qualifiedId = "(P)" + capturedPantsId;
                cachedPants = ItemRegistry.Create<Clothing>(qualifiedId);
            }

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
            if (tagPickerManager.IsOpen)
            {
                tagPickerManager.HandleClick(x, y, out bool consumed);
                if (consumed)
                    return;
            }

            if (!isWithinBounds(x, y))
            {
                CloseOverlay();
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            if (uiBuilder.CloseButton.containsPoint(x, y))
            {
                CloseOverlay();
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            if (uiBuilder.CancelButton.containsPoint(x, y))
            {
                CloseOverlay();
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            if (uiBuilder.SaveButton.containsPoint(x, y))
            {
                HandleSave(playSound);
                return;
            }

            if (uiBuilder.AddTagsButton != null && uiBuilder.AddTagsButton.containsPoint(x, y))
            {
                ToggleTagPicker();
                if (playSound) Game1.playSound("smallSelect");
                return;
            }

            if (uiBuilder.FavoriteCheckbox.containsPoint(x, y))
            {
                isFavorite = !isFavorite;
                if (playSound) Game1.playSound("smallSelect");
                return;
            }

            if (uiBuilder.LocalOnlyCheckbox != null && uiBuilder.LocalOnlyCheckbox.containsPoint(x, y))
            {
                if (Context.IsWorldReady)
                {
                    isLocalOnly = !isLocalOnly;
                    if (playSound) Game1.playSound("smallSelect");
                }
                return;
            }

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

            if (uiBuilder.NameInputArea.containsPoint(x, y))
            {
                tagPickerManager.DeselectCustomInput();
                nameTextBox.Selected = true;
            }
        }

        private void OpenTagPicker()
        {
            Rectangle overlayBounds = new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height);
            tagPickerManager.Open(overlayBounds, selectedTags, OnTagsChanged);
        }

        private void ToggleTagPicker()
        {
            if (tagPickerManager.IsOpen)
            {
                tagPickerManager.Close();
            }
            else
            {
                OpenTagPicker();
            }
        }

        private void OnTagsChanged(HashSet<string> newTags)
        {
            selectedTags = newTags;
        }

        public override void receiveKeyPress(Keys key)
        {
            if (tagPickerManager.IsOpen)
            {
                if (tagPickerManager.HandleKeyPress(key))
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

            if (Game1.options.doesInputListContain(Game1.options.menuButton, key))
            {
                return;
            }
        }

        public override void receiveScrollWheelAction(int direction)
        {
            if (tagPickerManager.IsOpen)
            {
                if (tagPickerManager.HandleScrollWheel(direction))
                {
                    Game1.playSound("shiny4");
                }
            }
        }

        public override void update(GameTime time)
        {
            base.update(time);
            nameTextBox.Update();

            nameTextBox.Selected = !tagPickerManager.IsCustomInputFocused;

            tagPickerManager.Update();

            // Allow item info toggle keybind to work in overlay
            if (parentMenu is OutfitMenu outfitMenu)
            {
                outfitMenu.HandleItemInfoToggle();
            }

            if (nameBoxJiggleTimer > 0)
            {
                nameBoxJiggleTimer -= (float)time.ElapsedGameTime.TotalMilliseconds;
            }
        }

        private void StartNameBoxJiggle()
        {
            nameBoxJiggleTimer = NameBoxJiggleDuration;
        }

        private int GetNameBoxJiggleOffset()
        {
            if (nameBoxJiggleTimer <= 0)
                return 0;

            float progress = nameBoxJiggleTimer / NameBoxJiggleDuration;
            return (int)(Math.Sin(progress * Math.PI * 6) * NameBoxJiggleIntensity * progress);
        }

        private void HandleSave(bool playSound)
        {
            string name = nameTextBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(name))
            {
                StartNameBoxJiggle();
                if (playSound) Game1.playSound("cancel");
                return;
            }

            string? shirtId = includeShirt ? capturedShirtId : null;
            string? pantsId = includePants ? capturedPantsId : null;
            string? hatId = includeHat ? capturedHatId : null;

            if (editingSet != null)
            {
                editingSet.Name = name;
                editingSet.Tags = selectedTags.ToList();
                editingSet.IsFavorite = isFavorite;
                editingSet.IsGlobal = !isLocalOnly;
                editingSet.ShirtId = shirtId;
                editingSet.PantsId = pantsId;
                editingSet.HatId = hatId;
                store.Update(editingSet);
            }
            else
            {
                store.CreateFromCurrentOutfit(
                    name,
                    selectedTags.ToList(),
                    isFavorite,
                    !isLocalOnly,
                    shirtId,
                    pantsId,
                    hatId,
                    useCurrentOutfit: false
                );
            }

            if (playSound) Game1.playSound("coin");
            onSaveComplete();
            CloseOverlay();
        }

        private void CloseOverlay()
        {
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
            int jiggleOffset = GetNameBoxJiggleOffset();
            uiBuilder.DrawNameInput(b, nameTextBox.Text ?? "", showPlaceholder, jiggleOffset);
            uiBuilder.DrawNameCursor(b, nameTextBox.Text ?? "", nameTextBox.Selected && !tagPickerManager.IsCustomInputFocused, jiggleOffset);

            uiBuilder.DrawPreviewBackground(b);
            DrawCharacterPreview(b);

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            uiBuilder.DrawItemSlot(b, uiBuilder.HatSlot, includeHat, HasHat(), mouseX, mouseY);
            uiBuilder.DrawItemSlot(b, uiBuilder.ShirtSlot, includeShirt, HasShirt(), mouseX, mouseY);
            uiBuilder.DrawItemSlot(b, uiBuilder.PantsSlot, includePants, HasPants(), mouseX, mouseY);
            DrawItemSprites(b, includeShirt, includePants, includeHat);

            uiBuilder.DrawTagsRow(b, mouseX, mouseY, tagPickerManager.IsOpen);
            uiBuilder.DrawFavoriteCheckbox(b, isFavorite, uiBuilder.FavoriteCheckbox.containsPoint(mouseX, mouseY));
            uiBuilder.DrawLocalOnlyCheckbox(b, isLocalOnly, Context.IsWorldReady,
                uiBuilder.LocalOnlyCheckbox?.containsPoint(mouseX, mouseY) ?? false);

            uiBuilder.DrawButtons(b);
            uiBuilder.DrawCloseButton(b);

            if (tagPickerManager.IsOpen)
            {
                tagPickerManager.Draw(b);
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

            var savedShirt = Game1.player.shirtItem.Value;
            var savedPants = Game1.player.pantsItem.Value;
            var savedHat = Game1.player.hat.Value;

            try
            {
                if (includeShirt && !string.IsNullOrEmpty(capturedShirtId) && store.IsItemValid(capturedShirtId, "(S)"))
                    Game1.player.shirtItem.Value = ItemRegistry.Create<Clothing>("(S)" + capturedShirtId);
                else
                    Game1.player.shirtItem.Value = null;

                if (includePants && !string.IsNullOrEmpty(capturedPantsId) && store.IsItemValid(capturedPantsId, "(P)"))
                    Game1.player.pantsItem.Value = ItemRegistry.Create<Clothing>("(P)" + capturedPantsId);
                else
                    Game1.player.pantsItem.Value = null;

                if (includeHat && !string.IsNullOrEmpty(capturedHatId) && store.IsItemValid(capturedHatId, "(H)"))
                    Game1.player.hat.Value = ItemRegistry.Create<Hat>("(H)" + capturedHatId);
                else
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

                FarmerRenderer.isDrawingForUI = false;

                farmerSpriteBatch.End();
            }
            finally
            {
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
            if (tagPickerManager.IsOpen)
                return;

            // Respect the ShowItemInfo config from parent menu
            if (parentMenu is OutfitMenu outfitMenu && !outfitMenu.ShowItemInfo)
                return;

            if (uiBuilder.ShirtSlot.Contains(mouseX, mouseY) && HasShirt() && cachedShirt != null)
            {
                IClickableMenu.drawToolTip(b, cachedShirt.getDescription(), cachedShirt.DisplayName, cachedShirt);
            }
            else if (uiBuilder.PantsSlot.Contains(mouseX, mouseY) && HasPants() && cachedPants != null)
            {
                IClickableMenu.drawToolTip(b, cachedPants.getDescription(), cachedPants.DisplayName, cachedPants);
            }
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
