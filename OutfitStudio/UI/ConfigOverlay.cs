using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    public class ConfigOverlay : IClickableMenu
    {
        private static readonly RasterizerState ScissorEnabled = new RasterizerState { ScissorTestEnable = true };

        private readonly IClickableMenu parentMenu;
        private readonly ConfigUIBuilder uiBuilder;
        private readonly ModEntry mod;

        private KeybindList toggleMenuKey;
        private KeybindList toggleItemInfoKey;
        private bool showTooltip;
        private bool closeOnClickOutside;
        private bool autoOpenTagMenu;
        private bool autoFocusSearchBar;
        private bool arrowKeyScrolling;
        private bool autoOpenDyeColorMenu;
        private bool resetFilterOnTabSwitch;
        private bool resetSearchOnTabSwitch;
        private int visibleRows;
        private int visibleColumns;
        private int slotSize;
        private bool resetMatchAllOnOpen;
        private bool resetShowInvalidOnOpen;
        private string defaultSearchScope;
        private bool showScheduleDebugLog;
        private bool consistentTiebreaks;
        private bool lockManualOutfit;

        private readonly int originalRows;
        private readonly int originalColumns;
        private readonly int originalSlotSize;

        private string? listeningForKeybind;

        private bool searchScopeDropdownOpen;

        public ConfigOverlay(IClickableMenu parentMenu, ModEntry mod)
        {
            this.parentMenu = parentMenu;
            this.mod = mod;
            uiBuilder = new ConfigUIBuilder();

            var config = mod.GetConfig();
            toggleMenuKey = KeybindList.Parse(config.ToggleMenuKey.ToString());
            toggleItemInfoKey = KeybindList.Parse(config.ToggleItemInfoKey.ToString());
            showTooltip = config.ShowTooltip;
            closeOnClickOutside = config.CloseOnClickOutside;
            autoOpenTagMenu = config.AutoOpenTagMenu;
            autoFocusSearchBar = config.AutoFocusSearchBar;
            arrowKeyScrolling = config.ArrowKeyScrolling;
            autoOpenDyeColorMenu = config.AutoOpenDyeColorMenu;
            resetFilterOnTabSwitch = config.ResetFilterOnTabSwitch;
            resetSearchOnTabSwitch = config.ResetSearchOnTabSwitch;
            visibleRows = config.VisibleRows;
            visibleColumns = config.VisibleColumns;
            slotSize = config.SlotSize;
            resetMatchAllOnOpen = config.ResetMatchAllOnOpen;
            resetShowInvalidOnOpen = config.ResetShowInvalidOnOpen;
            defaultSearchScope = config.DefaultSearchScope;
            showScheduleDebugLog = config.ShowScheduleDebugLog;
            consistentTiebreaks = config.ConsistentTiebreaks;
            lockManualOutfit = config.LockManualOutfit;

            originalRows = visibleRows;
            originalColumns = visibleColumns;
            originalSlotSize = slotSize;

            Game1.keyboardDispatcher.Subscriber = null;

            uiBuilder.VisibleRowsSlider.Value = visibleRows;
            uiBuilder.VisibleColumnsSlider.Value = visibleColumns;
            uiBuilder.SlotSizeSlider.Value = slotSize;

            width = uiBuilder.Width;
            height = uiBuilder.Height;
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            bool clickInContentArea = uiBuilder.ContentClipRect.Contains(x, y);

            if (listeningForKeybind != null)
            {
                bool clickedSameArea = clickInContentArea &&
                    ((listeningForKeybind == "ToggleMenuKey" && IsInRowOf(uiBuilder.ToggleMenuKeyArea, x, y)) ||
                     (listeningForKeybind == "ToggleItemInfoKey" && IsInRowOf(uiBuilder.ToggleItemInfoKeyArea, x, y)));

                if (!clickedSameArea)
                {
                    listeningForKeybind = null;
                    if (playSound) Game1.playSound("smallSelect");
                }
                return;
            }

            if (uiBuilder.CloseButton.containsPoint(x, y))
            {
                CloseOverlay();
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            if (!isWithinBounds(x, y) && ModEntry.Config.CloseOnClickOutside)
            {
                bool clickedDropdownPanel = searchScopeDropdownOpen &&
                    uiBuilder.SearchScopeOptions.Any(o => o.containsPoint(x, y));

                if (!clickedDropdownPanel)
                {
                    CloseOverlay();
                    if (playSound) Game1.playSound("bigDeSelect");
                    return;
                }
            }

            if (searchScopeDropdownOpen)
            {
                foreach (var option in uiBuilder.SearchScopeOptions)
                {
                    if (option.containsPoint(x, y))
                    {
                        defaultSearchScope = option.name;
                        searchScopeDropdownOpen = false;
                        if (playSound) Game1.playSound("smallSelect");
                        return;
                    }
                }

                if (clickInContentArea && IsInRowOf(uiBuilder.SearchScopeDropdown, x, y))
                {
                    searchScopeDropdownOpen = false;
                    if (playSound) Game1.playSound("smallSelect");
                    return;
                }

                searchScopeDropdownOpen = false;
                return;
            }

            if (uiBuilder.SaveButton.containsPoint(x, y))
            {
                HandleSave();
                if (playSound) Game1.playSound("coin");
                return;
            }
            if (uiBuilder.CancelButton.containsPoint(x, y))
            {
                CloseOverlay();
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            if (!clickInContentArea)
                return;

            if (IsInRowOf(uiBuilder.ToggleMenuKeyArea, x, y))
            {
                listeningForKeybind = "ToggleMenuKey";
                if (playSound) Game1.playSound("smallSelect");
                return;
            }
            if (IsInRowOf(uiBuilder.ToggleItemInfoKeyArea, x, y))
            {
                listeningForKeybind = "ToggleItemInfoKey";
                if (playSound) Game1.playSound("smallSelect");
                return;
            }

            if (TryToggleCheckbox(uiBuilder.ShowTooltipCheckbox, x, y, ref showTooltip, playSound)) return;
            if (TryToggleCheckbox(uiBuilder.CloseOnClickOutsideCheckbox, x, y, ref closeOnClickOutside, playSound)) return;
            if (TryToggleCheckbox(uiBuilder.AutoOpenTagMenuCheckbox, x, y, ref autoOpenTagMenu, playSound)) return;
            if (TryToggleCheckbox(uiBuilder.AutoFocusSearchBarCheckbox, x, y, ref autoFocusSearchBar, playSound)) return;
            if (TryToggleCheckbox(uiBuilder.ArrowKeyScrollingCheckbox, x, y, ref arrowKeyScrolling, playSound)) return;
            if (TryToggleCheckbox(uiBuilder.AutoOpenDyeColorMenuCheckbox, x, y, ref autoOpenDyeColorMenu, playSound)) return;
            if (TryToggleCheckbox(uiBuilder.ResetFilterCheckbox, x, y, ref resetFilterOnTabSwitch, playSound)) return;
            if (TryToggleCheckbox(uiBuilder.ResetSearchCheckbox, x, y, ref resetSearchOnTabSwitch, playSound)) return;
            if (TryToggleCheckbox(uiBuilder.ResetMatchAllCheckbox, x, y, ref resetMatchAllOnOpen, playSound)) return;
            if (TryToggleCheckbox(uiBuilder.ResetShowInvalidCheckbox, x, y, ref resetShowInvalidOnOpen, playSound)) return;
            if (TryToggleCheckbox(uiBuilder.ShowScheduleDebugLogCheckbox, x, y, ref showScheduleDebugLog, playSound)) return;
            if (TryToggleCheckbox(uiBuilder.ConsistentTiebreaksCheckbox, x, y, ref consistentTiebreaks, playSound)) return;
            if (TryToggleCheckbox(uiBuilder.LockManualOutfitCheckbox, x, y, ref lockManualOutfit, playSound)) return;

            if (IsInRowOf(uiBuilder.SearchScopeDropdown, x, y))
            {
                searchScopeDropdownOpen = !searchScopeDropdownOpen;
                if (playSound) Game1.playSound("smallSelect");
                return;
            }
        }

        public override void leftClickHeld(int x, int y)
        {
            base.leftClickHeld(x, y);

            uiBuilder.VisibleRowsSlider.Click(x, y);
            uiBuilder.VisibleColumnsSlider.Click(x, y);
            uiBuilder.SlotSizeSlider.Click(x, y);

            visibleRows = uiBuilder.VisibleRowsSlider.Value;
            visibleColumns = uiBuilder.VisibleColumnsSlider.Value;
            slotSize = uiBuilder.SlotSizeSlider.Value;
        }

        public override void receiveKeyPress(Keys key)
        {
            if (listeningForKeybind != null)
            {
                if (key == Keys.Escape)
                {
                    listeningForKeybind = null;
                    Game1.playSound("bigDeSelect");
                    return;
                }

                SButton button = (SButton)key;
                KeybindList newKeybind = KeybindList.Parse(button.ToString());

                if (listeningForKeybind == "ToggleMenuKey")
                    toggleMenuKey = newKeybind;
                else if (listeningForKeybind == "ToggleItemInfoKey")
                    toggleItemInfoKey = newKeybind;

                listeningForKeybind = null;
                Game1.playSound("coin");
                return;
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
            if (!uiBuilder.IsScrollable)
                return;

            int oldOffset = uiBuilder.ScrollOffset;

            if (direction > 0)
                uiBuilder.ScrollOffset -= ConfigRowHeight;
            else if (direction < 0)
                uiBuilder.ScrollOffset += ConfigRowHeight;

            uiBuilder.ClampScrollOffset();

            if (uiBuilder.ScrollOffset != oldOffset)
            {
                searchScopeDropdownOpen = false;
                uiBuilder.Recalculate();
                Game1.playSound("shiny4");
            }
        }

        public override void update(GameTime time)
        {
            base.update(time);

            if (parentMenu is OutfitMenu outfitMenu)
                outfitMenu.HandleItemInfoToggle();
        }

        public override void draw(SpriteBatch b)
        {
            bool oldSuppressHover = UIHelpers.SuppressHover;
            UIHelpers.SuppressHover = true;

            if (parentMenu is OutfitMenu outfitMenu)
                outfitMenu.IsOverlayBlocking = true;

            parentMenu.draw(b);

            if (parentMenu is OutfitMenu outfitMenuAfter)
                outfitMenuAfter.IsOverlayBlocking = false;

            UIHelpers.SuppressHover = oldSuppressHover;

            b.Draw(Game1.fadeToBlackRect,
                new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
                Color.Black * BackgroundOverlayOpacity);

            UIHelpers.DrawTextureBox(b, uiBuilder.TitleBoxBounds.X, uiBuilder.TitleBoxBounds.Y,
                uiBuilder.TitleBoxBounds.Width, uiBuilder.TitleBoxBounds.Height, Color.White);

            UIHelpers.DrawTextureBox(b, uiBuilder.ContentBoxBounds.X, uiBuilder.ContentBoxBounds.Y,
                uiBuilder.ContentBoxBounds.Width, uiBuilder.ContentBoxBounds.Height, Color.White);

            uiBuilder.DrawTitle(b);

            if (uiBuilder.IsScrollable)
            {
                var oldScissorRect = b.GraphicsDevice.ScissorRectangle;
                b.End();
                b.GraphicsDevice.ScissorRectangle = uiBuilder.ContentClipRect;
                b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, ScissorEnabled);

                DrawContent(b);

                b.End();
                b.GraphicsDevice.ScissorRectangle = oldScissorRect;
                b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            }
            else
            {
                DrawContent(b);
            }

            uiBuilder.DrawScrollIndicators(b);

            uiBuilder.DrawButtons(b);
            uiBuilder.DrawCloseButton(b);

            if (searchScopeDropdownOpen)
            {
                uiBuilder.DrawSearchScopeDropdownOptions(b, defaultSearchScope);
            }

            if (!searchScopeDropdownOpen)
            {
                int mouseX = Game1.getMouseX();
                int mouseY = Game1.getMouseY();
                string? tooltip = uiBuilder.GetHoveredTooltip(mouseX, mouseY);
                if (tooltip != null)
                {
                    string wrappedTooltip = Game1.parseText(tooltip, Game1.smallFont, 450);
                    IClickableMenu.drawHoverText(b, wrappedTooltip, Game1.smallFont);
                }
            }

            drawMouse(b);
        }

        private void DrawContent(SpriteBatch b)
        {
            uiBuilder.DrawSectionHeader(b, uiBuilder.GeneralHeaderY, TranslationCache.ConfigGeneralSection);
            uiBuilder.DrawKeybindRow(b, TranslationCache.ConfigToggleMenuKeyName, toggleMenuKey,
                uiBuilder.ToggleMenuKeyArea, listeningForKeybind == "ToggleMenuKey");
            uiBuilder.DrawKeybindRow(b, TranslationCache.ConfigToggleItemInfoKeyName, toggleItemInfoKey,
                uiBuilder.ToggleItemInfoKeyArea, listeningForKeybind == "ToggleItemInfoKey");
            uiBuilder.DrawCheckboxRow(b, TranslationCache.ConfigShowTooltipName, showTooltip, uiBuilder.ShowTooltipCheckbox);
            uiBuilder.DrawCheckboxRow(b, TranslationCache.ConfigCloseOnClickOutsideName, closeOnClickOutside, uiBuilder.CloseOnClickOutsideCheckbox);
            uiBuilder.DrawCheckboxRow(b, TranslationCache.ConfigAutoOpenTagMenuName, autoOpenTagMenu, uiBuilder.AutoOpenTagMenuCheckbox);
            uiBuilder.DrawCheckboxRow(b, TranslationCache.ConfigAutoFocusSearchBarName, autoFocusSearchBar, uiBuilder.AutoFocusSearchBarCheckbox);
            uiBuilder.DrawCheckboxRow(b, TranslationCache.ConfigArrowKeyScrollingName, arrowKeyScrolling, uiBuilder.ArrowKeyScrollingCheckbox);

            uiBuilder.DrawSectionHeader(b, uiBuilder.MainMenuHeaderY, TranslationCache.ConfigMainMenuSection);
            uiBuilder.DrawCheckboxRow(b, TranslationCache.ConfigResetFilterOnTabSwitchName, resetFilterOnTabSwitch, uiBuilder.ResetFilterCheckbox);
            uiBuilder.DrawCheckboxRow(b, TranslationCache.ConfigResetSearchOnTabSwitchName, resetSearchOnTabSwitch, uiBuilder.ResetSearchCheckbox);
            uiBuilder.DrawCheckboxRow(b, TranslationCache.ConfigAutoOpenDyeColorMenuName, autoOpenDyeColorMenu, uiBuilder.AutoOpenDyeColorMenuCheckbox);

            uiBuilder.DrawSliderRow(b, TranslationCache.ConfigVisibleRowsName, uiBuilder.VisibleRowsSlider);
            uiBuilder.DrawSliderRow(b, TranslationCache.ConfigVisibleColumnsName, uiBuilder.VisibleColumnsSlider);
            uiBuilder.DrawSliderRow(b, TranslationCache.ConfigSlotSizeName, uiBuilder.SlotSizeSlider);
            uiBuilder.DrawCheckboxRow(b, TranslationCache.ConfigShowScheduleDebugLogName, showScheduleDebugLog, uiBuilder.ShowScheduleDebugLogCheckbox);

            uiBuilder.DrawSectionHeader(b, uiBuilder.WardrobeHeaderY, TranslationCache.ConfigWardrobeMenuSection);
            uiBuilder.DrawCheckboxRow(b, TranslationCache.ConfigResetMatchAllOnOpenName, resetMatchAllOnOpen, uiBuilder.ResetMatchAllCheckbox);
            uiBuilder.DrawCheckboxRow(b, TranslationCache.ConfigResetShowInvalidOnOpenName, resetShowInvalidOnOpen, uiBuilder.ResetShowInvalidCheckbox);
            uiBuilder.DrawSearchScopeRow(b, TranslationCache.ConfigDefaultSearchScopeName, defaultSearchScope, searchScopeDropdownOpen);

            uiBuilder.DrawSectionHeader(b, uiBuilder.ScheduleHeaderY, TranslationCache.ConfigScheduleSection);
            uiBuilder.DrawCheckboxRow(b, TranslationCache.ConfigConsistentTiebreaksName, consistentTiebreaks, uiBuilder.ConsistentTiebreaksCheckbox);
            uiBuilder.DrawCheckboxRow(b, TranslationCache.ConfigLockManualOutfitName, lockManualOutfit, uiBuilder.LockManualOutfitCheckbox);
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            parentMenu.gameWindowSizeChanged(oldBounds, newBounds);

            uiBuilder.Recalculate();
            width = uiBuilder.Width;
            height = uiBuilder.Height;
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;
        }

        private bool IsInRowOf(ClickableComponent control, int x, int y)
        {
            int rowY = control.bounds.Height < ConfigRowHeight
                ? control.bounds.Y - (ConfigRowHeight - control.bounds.Height) / 2
                : control.bounds.Y;
            return x >= uiBuilder.ContentClipRect.X
                && x < uiBuilder.ContentClipRect.Right
                && y >= rowY
                && y < rowY + ConfigRowHeight;
        }

        private bool TryToggleCheckbox(ClickableComponent checkbox, int x, int y, ref bool value, bool playSound)
        {
            if (!IsInRowOf(checkbox, x, y))
                return false;

            value = !value;
            if (playSound) Game1.playSound("drumkit6");
            return true;
        }

        private void HandleSave()
        {
            var config = mod.GetConfig();
            config.ToggleMenuKey = toggleMenuKey;
            config.ToggleItemInfoKey = toggleItemInfoKey;
            config.ShowTooltip = showTooltip;
            config.CloseOnClickOutside = closeOnClickOutside;
            config.AutoOpenTagMenu = autoOpenTagMenu;
            config.AutoFocusSearchBar = autoFocusSearchBar;
            config.ArrowKeyScrolling = arrowKeyScrolling;
            config.AutoOpenDyeColorMenu = autoOpenDyeColorMenu;
            config.ResetFilterOnTabSwitch = resetFilterOnTabSwitch;
            config.ResetSearchOnTabSwitch = resetSearchOnTabSwitch;
            visibleRows = uiBuilder.VisibleRowsSlider.Value;
            visibleColumns = uiBuilder.VisibleColumnsSlider.Value;
            slotSize = uiBuilder.SlotSizeSlider.Value;
            config.VisibleRows = visibleRows;
            config.VisibleColumns = visibleColumns;
            config.SlotSize = slotSize;
            config.ResetMatchAllOnOpen = resetMatchAllOnOpen;
            config.ResetShowInvalidOnOpen = resetShowInvalidOnOpen;
            config.DefaultSearchScope = defaultSearchScope;
            config.ShowScheduleDebugLog = showScheduleDebugLog;

            bool tiebreaksChanged = config.ConsistentTiebreaks != consistentTiebreaks;
            config.ConsistentTiebreaks = consistentTiebreaks;
            if (tiebreaksChanged)
                mod.GetScheduleEngine()?.InvalidateContextCache();

            config.LockManualOutfit = lockManualOutfit;

            mod.SaveConfig();

            bool gridChanged = visibleRows != originalRows
                               || visibleColumns != originalColumns
                               || slotSize != originalSlotSize;

            if (gridChanged)
            {
                mod.ReopenMenu();
            }
            else
            {
                Game1.activeClickableMenu = parentMenu;
            }
        }

        private void CloseOverlay()
        {
            Game1.activeClickableMenu = parentMenu;
        }

    }
}
