using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OutfitStudio.Models;
using OutfitStudio.Services;
using StardewValley;
using StardewValley.Menus;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    public class ScheduleDebugLogOverlay : IClickableMenu
    {
        private static readonly RasterizerState ScissorEnabled = new RasterizerState { ScissorTestEnable = true };

        private readonly IClickableMenu parentMenu;
        private readonly ScheduleDebugLogUIBuilder uiBuilder;
        private readonly ScheduleEvalLog evalLog;

        private readonly HashSet<int> expandedEntries = new();
        private readonly ContinuousScrollHandler scrollHandler = new();

        internal ScheduleDebugLogOverlay(IClickableMenu parentMenu, ScheduleEvalLog evalLog)
        {
            this.parentMenu = parentMenu;
            this.evalLog = evalLog;
            uiBuilder = new ScheduleDebugLogUIBuilder();

            Game1.keyboardDispatcher.Subscriber = null;

            RecalculateAndSync();
        }

        private void RecalculateAndSync()
        {
            var entries = evalLog.Entries;
            var expandedRuleHeights = new Dictionary<int, int>();
            foreach (int idx in expandedEntries)
            {
                if (idx >= 0 && idx < entries.Count)
                {
                    var e = entries[idx];
                    bool isManual = e.ManualOverrideOutfitName != null;
                    expandedRuleHeights[idx] = ScheduleDebugLogUIBuilder.CalculateRuleSectionHeight(e.RuleResults, isManual);
                }
            }

            uiBuilder.Recalculate(entries.Count, expandedRuleHeights);
            width = uiBuilder.Width;
            height = uiBuilder.Height;
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            if (uiBuilder.CloseButton.containsPoint(x, y))
            {
                CloseOverlay();
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            if (!isWithinBounds(x, y) && ModEntry.Config.CloseOnClickOutside)
            {
                CloseOverlay();
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            if (uiBuilder.CollapseButton.containsPoint(x, y))
            {
                if (expandedEntries.Count > 0)
                {
                    expandedEntries.Clear();
                    RecalculateAndSync();
                    uiBuilder.ScrollOffset = 0;
                    if (playSound) Game1.playSound("smallSelect");
                }
                return;
            }

            if (uiBuilder.CloseMenuButton.containsPoint(x, y))
            {
                CloseOverlay();
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            int clickedIndex = uiBuilder.GetEntryIndexAtPoint(x, y);
            if (clickedIndex >= 0)
            {
                if (uiBuilder.IsClickOnCollapsedSummary(x, y, clickedIndex))
                {
                    int screenYBefore = uiBuilder.GetEntryScreenY(clickedIndex);

                    if (expandedEntries.Contains(clickedIndex))
                        expandedEntries.Remove(clickedIndex);
                    else
                        expandedEntries.Add(clickedIndex);

                    RecalculateAndSync();

                    int screenYAfter = uiBuilder.GetEntryScreenY(clickedIndex);
                    uiBuilder.ScrollOffset += screenYAfter - screenYBefore;
                    uiBuilder.ClampScrollOffset();

                    if (playSound) Game1.playSound("smallSelect");
                }
            }
        }

        public override void receiveKeyPress(Keys key)
        {
            if (ModEntry.Config.ArrowKeyScrolling && uiBuilder.IsScrollable)
            {
                int stride = ScheduleDebugCollapsedRowHeight + ScheduleDebugItemGap;
                int visibleRows = Math.Max(1, uiBuilder.EntryListClipRect.Height / stride);

                int scrollAmount = key switch
                {
                    Keys.Up => -stride,
                    Keys.Down => stride,
                    Keys.Left => -stride * visibleRows,
                    Keys.Right => stride * visibleRows,
                    _ => 0
                };

                if (scrollAmount != 0)
                {
                    int oldOffset = uiBuilder.ScrollOffset;
                    uiBuilder.ScrollOffset += scrollAmount;
                    uiBuilder.ClampScrollOffset();
                    if (uiBuilder.ScrollOffset != oldOffset)
                        Game1.playSound("shiny4");
                    return;
                }
            }

            if (key == Keys.Escape)
            {
                CloseOverlay();
                Game1.playSound("bigDeSelect");
                return;
            }

            if (Game1.options.doesInputListContain(Game1.options.menuButton, key))
                return;
        }

        public override void receiveScrollWheelAction(int direction)
        {
            if (!uiBuilder.IsScrollable) return;

            int stride = ScheduleDebugCollapsedRowHeight + ScheduleDebugItemGap;
            int oldOffset = uiBuilder.ScrollOffset;

            if (direction > 0)
                uiBuilder.ScrollOffset -= stride;
            else if (direction < 0)
                uiBuilder.ScrollOffset += stride;

            uiBuilder.ClampScrollOffset();

            if (uiBuilder.ScrollOffset != oldOffset)
                Game1.playSound("shiny4");
        }

        public override void update(GameTime time)
        {
            base.update(time);

            if (ModEntry.Config.ArrowKeyScrolling && uiBuilder.IsScrollable)
            {
                int stride = ScheduleDebugCollapsedRowHeight + ScheduleDebugItemGap;
                int visibleRows = Math.Max(1, uiBuilder.EntryListClipRect.Height / stride);
                int scrollAmount = scrollHandler.Update(time, visibleRows, out bool shouldPlaySound);
                if (scrollAmount != 0)
                {
                    int oldOffset = uiBuilder.ScrollOffset;
                    uiBuilder.ScrollOffset += scrollAmount * stride;
                    uiBuilder.ClampScrollOffset();
                    if (uiBuilder.ScrollOffset != oldOffset && shouldPlaySound)
                        Game1.playSound("shiny4");
                }
            }
            else
            {
                scrollHandler.Reset();
            }
        }

        public override void draw(SpriteBatch b)
        {
            bool oldSuppressHover = UIHelpers.SuppressHover;
            UIHelpers.SuppressHover = true;

            if (parentMenu is ScheduleMenu scheduleMenu)
                scheduleMenu.IsOverlayBlocking = true;

            parentMenu.draw(b);

            if (parentMenu is ScheduleMenu scheduleMenuAfter)
                scheduleMenuAfter.IsOverlayBlocking = false;

            UIHelpers.SuppressHover = oldSuppressHover;

            b.Draw(Game1.fadeToBlackRect,
                new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
                Color.Black * BackgroundOverlayOpacity);

            UIHelpers.DrawTextureBox(b, uiBuilder.TitleBoxBounds.X, uiBuilder.TitleBoxBounds.Y,
                uiBuilder.TitleBoxBounds.Width, uiBuilder.TitleBoxBounds.Height, Color.White);

            UIHelpers.DrawTextureBox(b, uiBuilder.ContentBoxBounds.X, uiBuilder.ContentBoxBounds.Y,
                uiBuilder.ContentBoxBounds.Width, uiBuilder.ContentBoxBounds.Height, Color.White);

            uiBuilder.DrawTitle(b);

            string? hoveredTooltip = null;
            var entries = evalLog.Entries;

            if (entries.Count == 0)
            {
                uiBuilder.DrawNoEntriesText(b);
            }
            else
            {
                var oldScissorRect = b.GraphicsDevice.ScissorRectangle;
                b.End();
                b.GraphicsDevice.ScissorRectangle = uiBuilder.EntryListClipRect;
                b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, ScissorEnabled);

                hoveredTooltip = DrawEntryList(b, entries);

                b.End();
                b.GraphicsDevice.ScissorRectangle = oldScissorRect;
                b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            }

            uiBuilder.DrawScrollIndicators(b);
            uiBuilder.DrawButtons(b);
            uiBuilder.DrawCloseButton(b);

            if (hoveredTooltip != null && ModEntry.Config.ShowTooltip)
                UIHelpers.DrawWrappedTooltip(b, hoveredTooltip);

            drawMouse(b);
        }

        private string? DrawEntryList(SpriteBatch b, IReadOnlyList<ScheduleEvalEntry> entries)
        {
            string? tooltip = null;
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            for (int i = 0; i < entries.Count; i++)
            {
                int entryScreenY = uiBuilder.GetEntryScreenY(i);
                int entryHeight = uiBuilder.GetEntryHeight(i);

                if (entryScreenY + entryHeight < uiBuilder.EntryListClipRect.Y)
                    continue;
                if (entryScreenY > uiBuilder.EntryListClipRect.Bottom)
                    break;

                bool isExpanded = expandedEntries.Contains(i);
                bool isHovered = uiBuilder.EntryListClipRect.Contains(mouseX, mouseY)
                    && uiBuilder.GetEntryIndexAtPoint(mouseX, mouseY) == i;

                string? rowTooltip = uiBuilder.DrawCollapsedEntry(b, entryScreenY, entryHeight,
                    entries[i], isExpanded, isHovered, mouseX, mouseY);
                if (rowTooltip != null)
                    tooltip = rowTooltip;

                if (isExpanded)
                {
                    string? expandedTip = uiBuilder.DrawExpandedSections(b, entryScreenY,
                        entries[i], mouseX, mouseY);
                    if (expandedTip != null)
                        tooltip = expandedTip;
                }

                if (i < entries.Count - 1)
                {
                    int separatorY = entryScreenY + entryHeight + ScheduleDebugItemGap / 2;
                    uiBuilder.DrawEntrySeparator(b, separatorY);
                }
            }

            return tooltip;
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            parentMenu.gameWindowSizeChanged(oldBounds, newBounds);
            RecalculateAndSync();
        }

        private void CloseOverlay()
        {
            Game1.activeClickableMenu = parentMenu;
        }
    }
}
