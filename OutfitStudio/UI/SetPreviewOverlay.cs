using System;
using System.Collections.Generic;
using System.Linq;
using OutfitStudio.Models;
using OutfitStudio.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    public class SetPreviewOverlay : IClickableMenu
    {
        private readonly SetPreviewUIBuilder uiBuilder;
        private readonly OutfitSetStore outfitSetStore;
        private readonly ScheduleStore scheduleStore;
        private readonly ScheduleRule? editingRule;
        private readonly List<string> selectedTags;
        private readonly bool tagsSelectAll;
        private readonly Action<List<string>> onExcludedChanged;
        private readonly Action onSave;

        private bool shouldClose;
        private List<string> excludedSetIds;
        private List<OutfitSet> allMatchingSets = new();
        private List<string> setDisplayNames = new();
        private int selectedIndex = -1;
        private int scrollOffset;
        private int previewDirection = 2;

        private RenderTarget2D? previewRenderTarget;
        private SpriteBatch? previewSpriteBatch;
        private bool previewDirty = true;
        private string? lastPreviewSetId;
        private int lastPreviewDirection = -1;

        private static readonly FarmerSprite.AnimationFrame[] DirectionFrames = new[]
        {
            new FarmerSprite.AnimationFrame(12, 0, secondaryArm: false, flip: false), // Up
            new FarmerSprite.AnimationFrame(6, 0, secondaryArm: false, flip: false),  // Right
            new FarmerSprite.AnimationFrame(0, 0, secondaryArm: false, flip: false),  // Down
            new FarmerSprite.AnimationFrame(6, 0, secondaryArm: false, flip: true),   // Left
        };
        private const int PreviewFarmerScale = 4;

        public SetPreviewOverlay(
            OutfitSetStore outfitSetStore,
            ScheduleStore scheduleStore,
            ScheduleRule? editingRule,
            List<string> selectedTags,
            bool tagsSelectAll,
            List<string> excludedSetIds,
            Action<List<string>> onExcludedChanged,
            Action onSave,
            List<string> triggerLines)
        {
            this.outfitSetStore = outfitSetStore;
            this.scheduleStore = scheduleStore;
            this.editingRule = editingRule;
            this.selectedTags = selectedTags;
            this.tagsSelectAll = tagsSelectAll;
            this.excludedSetIds = new List<string>(excludedSetIds);
            this.onExcludedChanged = onExcludedChanged;
            this.onSave = onSave;

            Game1.keyboardDispatcher.Subscriber = null;

            uiBuilder = new SetPreviewUIBuilder(triggerLines);
            uiBuilder.Recalculate();

            RefreshMatchingSets();

            width = uiBuilder.Width;
            height = uiBuilder.Height;
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;
        }

        private void RefreshMatchingSets()
        {
            if (tagsSelectAll)
            {
                allMatchingSets = outfitSetStore.GetAllSets()
                    .Where(s => s.IsValid)
                    .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            else
            {
                var tagSet = new HashSet<string>(selectedTags, TranslationCache.TagComparer);
                allMatchingSets = outfitSetStore.GetAllSets()
                    .Where(s => s.IsValid && s.Tags.Any(t => tagSet.Contains(t)))
                    .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            setDisplayNames = allMatchingSets.Select(s => s.Name).ToList();
            scrollOffset = UIHelpers.ClampScrollOffset(scrollOffset, allMatchingSets.Count, SetPreviewMaxVisibleSets);
            uiBuilder.BuildSetListItems(setDisplayNames, scrollOffset);

            if (selectedIndex >= allMatchingSets.Count)
                selectedIndex = -1;
        }

        private int ActiveCount => allMatchingSets.Count(s => !excludedSetIds.Contains(s.Id));

        public override bool readyToClose() => shouldClose;

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            if (uiBuilder.CloseXButton.containsPoint(x, y))
            {
                shouldClose = true;
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            if (uiBuilder.SaveButton.containsPoint(x, y))
            {
                onSave();
                shouldClose = true;
                if (playSound) Game1.playSound("bigSelect");
                return;
            }

            if (uiBuilder.CancelButton.containsPoint(x, y))
            {
                shouldClose = true;
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            if (!isWithinBounds(x, y) && ModEntry.Config.CloseOnClickOutside)
            {
                shouldClose = true;
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            // Direction arrows
            if (uiBuilder.LeftArrowButton.containsPoint(x, y))
            {
                previewDirection = (previewDirection + 1) % 4;
                previewDirty = true;
                if (playSound) Game1.playSound("shwip");
                return;
            }

            if (uiBuilder.RightArrowButton.containsPoint(x, y))
            {
                previewDirection = (previewDirection + 3) % 4;
                previewDirty = true;
                if (playSound) Game1.playSound("shwip");
                return;
            }

            for (int i = 0; i < uiBuilder.ExcludeButtons.Count; i++)
            {
                if (uiBuilder.ExcludeButtons[i].containsPoint(x, y))
                {
                    int dataIndex = scrollOffset + i;
                    if (dataIndex < allMatchingSets.Count)
                    {
                        ToggleExclusion(dataIndex);
                        if (playSound) Game1.playSound("drumkit6");
                    }
                    return;
                }
            }

            for (int i = 0; i < uiBuilder.SetListItems.Count; i++)
            {
                if (uiBuilder.SetListItems[i].containsPoint(x, y))
                {
                    int dataIndex = scrollOffset + i;
                    if (dataIndex < allMatchingSets.Count)
                    {
                        selectedIndex = dataIndex;
                        previewDirty = true;
                        if (playSound) Game1.playSound("smallSelect");
                    }
                    return;
                }
            }
        }

        private void ToggleExclusion(int dataIndex)
        {
            var set = allMatchingSets[dataIndex];
            if (excludedSetIds.Contains(set.Id))
            {
                excludedSetIds.Remove(set.Id);

                if (editingRule != null)
                {
                    var state = scheduleStore.GetRotationState(editingRule.Id);
                    if (state != null)
                    {
                        int insertIndex = state.Queue.Count > 0 ? new Random().Next(state.Queue.Count + 1) : 0;
                        state.Queue.Insert(insertIndex, set.Id);
                        scheduleStore.SetRotationState(editingRule.Id, state);
                    }
                }
            }
            else
            {
                excludedSetIds.Add(set.Id);
            }

            onExcludedChanged(new List<string>(excludedSetIds));
        }

        public override void receiveKeyPress(Keys key)
        {
            if (key == Keys.Escape)
            {
                shouldClose = true;
                Game1.playSound("bigDeSelect");
                return;
            }

            if (Game1.options.doesInputListContain(Game1.options.menuButton, key))
                return;
        }

        public override void receiveScrollWheelAction(int direction)
        {
            int maxScroll = Math.Max(0, allMatchingSets.Count - SetPreviewMaxVisibleSets);

            if (direction > 0)
                scrollOffset = Math.Max(0, scrollOffset - 1);
            else if (direction < 0)
                scrollOffset = Math.Min(maxScroll, scrollOffset + 1);

            uiBuilder.BuildSetListItems(setDisplayNames, scrollOffset);
        }

        public override void update(GameTime time)
        {
            base.update(time);
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect,
                new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
                Color.Black * BackgroundOverlayOpacity);

            uiBuilder.DrawContentBox(b);
            uiBuilder.DrawButtonBox(b);

            OutfitSet? previewSet = selectedIndex >= 0 && selectedIndex < allMatchingSets.Count
                ? allMatchingSets[selectedIndex]
                : null;

            if (previewDirty || lastPreviewSetId != previewSet?.Id || lastPreviewDirection != previewDirection)
            {
                RenderPreviewToTarget(previewSet);
                lastPreviewSetId = previewSet?.Id;
                lastPreviewDirection = previewDirection;
                previewDirty = false;
            }

            uiBuilder.DrawPreview(b, previewRenderTarget);
            uiBuilder.DrawArrowButtons(b);
            uiBuilder.DrawTriggerPanel(b);
            uiBuilder.DrawHeader(b, ActiveCount, allMatchingSets.Count);
            uiBuilder.DrawDivider(b);

            string? tooltip = uiBuilder.DrawSetList(b,
                dataIndex => dataIndex < allMatchingSets.Count && excludedSetIds.Contains(allMatchingSets[dataIndex].Id),
                dataIndex => dataIndex == selectedIndex,
                scrollOffset, allMatchingSets.Count);

            uiBuilder.DrawCloseXButton(b);
            uiBuilder.DrawBottomButtons(b);

            if (tooltip != null && ModEntry.Config.ShowTooltip)
            {
                UIHelpers.DrawWrappedTooltip(b, tooltip);
            }

            drawMouse(b);
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);

            uiBuilder.Recalculate();
            uiBuilder.BuildSetListItems(setDisplayNames, scrollOffset);

            width = uiBuilder.Width;
            height = uiBuilder.Height;
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;
        }

        private void InitializePreviewResources()
        {
            if (previewRenderTarget == null || previewRenderTarget.IsDisposed)
            {
                previewRenderTarget = new RenderTarget2D(
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

            if (previewSpriteBatch == null || previewSpriteBatch.IsDisposed)
            {
                previewSpriteBatch = new SpriteBatch(Game1.graphics.GraphicsDevice);
            }
        }

        private void RenderPreviewToTarget(OutfitSet? set)
        {
            InitializePreviewResources();

            var renderTargets = Game1.graphics.GraphicsDevice.GetRenderTargets();
            Game1.graphics.GraphicsDevice.SetRenderTarget(previewRenderTarget);
            Game1.graphics.GraphicsDevice.Clear(Color.Transparent);

            var savedShirt = Game1.player.shirtItem.Value;
            var savedPants = Game1.player.pantsItem.Value;
            var savedHat = Game1.player.hat.Value;
            int originalEyes = Game1.player.currentEyes;

            try
            {
                Game1.player.currentEyes = 0;

                if (set != null)
                {
                    if (!string.IsNullOrEmpty(set.ShirtId) && outfitSetStore.IsItemValid(set.ShirtId, "(S)"))
                    {
                        var shirt = ItemRegistry.Create<Clothing>("(S)" + set.ShirtId);
                        ColorHelper.ApplyColor(shirt, set.ShirtColor);
                        Game1.player.shirtItem.Value = shirt;
                    }
                    else
                        Game1.player.shirtItem.Value = null;

                    if (!string.IsNullOrEmpty(set.PantsId) && outfitSetStore.IsItemValid(set.PantsId, "(P)"))
                    {
                        var pants = ItemRegistry.Create<Clothing>("(P)" + set.PantsId);
                        ColorHelper.ApplyColor(pants, set.PantsColor);
                        Game1.player.pantsItem.Value = pants;
                    }
                    else
                        Game1.player.pantsItem.Value = null;

                    if (!string.IsNullOrEmpty(set.HatId) && outfitSetStore.IsItemValid(set.HatId, "(H)"))
                        Game1.player.hat.Value = ItemRegistry.Create<Hat>("(H)" + set.HatId);
                    else
                        Game1.player.hat.Value = null;
                }

                Game1.player.FarmerRenderer.MarkSpriteDirty();

                int frameIndex = DirectionFrames[previewDirection].frame;
                int baseY = Game1.player.bathingClothes.Value ? 576 : 0;
                int sourceX = (frameIndex * 16) % 96;
                int sourceY = baseY + (frameIndex * 16) / 96 * 32;
                Rectangle sourceRect = new Rectangle(sourceX, sourceY, FarmerSpriteWidth, FarmerSpriteHeight);

                int scaledWidth = FarmerSpriteWidth * PreviewFarmerScale;
                int scaledHeight = FarmerSpriteHeight * PreviewFarmerScale;
                Vector2 centeredPosition = new Vector2(
                    (SaveSetPreviewWidth - scaledWidth) / 2,
                    (SaveSetPreviewHeight - scaledHeight) / 2
                );

                previewSpriteBatch!.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

                FarmerRenderer.isDrawingForUI = true;
                Game1.player.FarmerRenderer.draw(
                    previewSpriteBatch,
                    DirectionFrames[previewDirection],
                    DirectionFrames[previewDirection].frame,
                    sourceRect,
                    centeredPosition,
                    Vector2.Zero,
                    0.8f,
                    previewDirection,
                    Color.White,
                    0f,
                    1f,
                    Game1.player
                );

                FarmerRenderer.isDrawingForUI = false;
                previewSpriteBatch.End();
            }
            finally
            {
                Game1.player.shirtItem.Value = savedShirt;
                Game1.player.pantsItem.Value = savedPants;
                Game1.player.hat.Value = savedHat;
                Game1.player.currentEyes = originalEyes;
                Game1.player.FarmerRenderer.MarkSpriteDirty();
            }

            Game1.graphics.GraphicsDevice.SetRenderTargets(renderTargets);
        }

        protected override void cleanupBeforeExit()
        {
            base.cleanupBeforeExit();
            UIHelpers.SafeDispose(ref previewRenderTarget, ref previewSpriteBatch);
        }
    }
}
