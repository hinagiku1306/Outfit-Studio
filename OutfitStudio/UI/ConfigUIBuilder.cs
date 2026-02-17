using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    public class ConfigUIBuilder
    {
        private const int FixedTopHeight = ConfigBorderPadding;
        private const int FixedBottomHeight = 0;

        public ClickableTextureComponent CloseButton { get; private set; } = null!;

        public ClickableComponent ToggleMenuKeyArea { get; private set; } = null!;
        public ClickableComponent ToggleItemInfoKeyArea { get; private set; } = null!;
        public ClickableComponent ToggleWardrobeKeyArea { get; private set; } = null!;
        public ClickableComponent ToggleScheduleKeyArea { get; private set; } = null!;

        public ClickableComponent ShowTooltipCheckbox { get; private set; } = null!;
        public ClickableComponent CloseOnClickOutsideCheckbox { get; private set; } = null!;
        public ClickableComponent AutoOpenTagMenuCheckbox { get; private set; } = null!;
        public ClickableComponent AutoFocusSearchBarCheckbox { get; private set; } = null!;
        public ClickableComponent ArrowKeyScrollingCheckbox { get; private set; } = null!;
        public ClickableComponent IncludeHairInOutfitSetsCheckbox { get; private set; } = null!;
        public ClickableComponent AutoOpenDyeColorMenuCheckbox { get; private set; } = null!;
        public ClickableComponent ResetFilterCheckbox { get; private set; } = null!;
        public ClickableComponent ResetSearchCheckbox { get; private set; } = null!;
        public ClickableComponent ResetMatchAllCheckbox { get; private set; } = null!;
        public ClickableComponent ResetShowInvalidCheckbox { get; private set; } = null!;
        public ClickableComponent ShowScheduleDebugLogCheckbox { get; private set; } = null!;
        public ClickableComponent ConsistentTiebreaksCheckbox { get; private set; } = null!;
        public ClickableComponent LockManualOutfitCheckbox { get; private set; } = null!;

        public DiscreteSlider VisibleRowsSlider { get; private set; } = null!;
        public DiscreteSlider VisibleColumnsSlider { get; private set; } = null!;
        public DiscreteSlider SlotSizeSlider { get; private set; } = null!;

        public ClickableComponent SearchScopeDropdown { get; private set; } = null!;
        public Rectangle SearchScopePanelAnchor { get; private set; }
        public List<ClickableComponent> SearchScopeOptions { get; private set; } = new();
        private int searchScopePanelW;

        public ClickableComponent DefaultPriorityDropdown { get; private set; } = null!;
        public Rectangle DefaultPriorityPanelAnchor { get; private set; }
        public List<ClickableComponent> DefaultPriorityOptions { get; private set; } = new();
        private int priorityPanelW;

        public ClickableComponent DefaultRotationDropdown { get; private set; } = null!;
        public Rectangle DefaultRotationPanelAnchor { get; private set; }
        public List<ClickableComponent> DefaultRotationOptions { get; private set; } = new();
        private int rotationPanelW;

        public ClickableComponent SaveButton { get; private set; } = null!;
        public ClickableComponent CancelButton { get; private set; } = null!;

        public int GeneralHeaderY { get; private set; }
        public int MainMenuHeaderY { get; private set; }
        public int WardrobeHeaderY { get; private set; }
        public int ScheduleHeaderY { get; private set; }


        private int[] rowYPositions = Array.Empty<int>();
        private string[] rowLabels = Array.Empty<string>();
        private string[] rowTooltips = Array.Empty<string>();

        public int ScrollOffset { get; set; }
        public int MaxScrollOffset { get; private set; }
        public bool IsScrollable => MaxScrollOffset > 0;
        public Rectangle ContentClipRect { get; private set; }

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public Rectangle TitleBoxBounds { get; private set; }
        public Rectangle ContentBoxBounds { get; private set; }
        public Rectangle ButtonBoxBounds { get; private set; }

        private int contentX;
        private int contentWidth;
        private int controlX;

        public ConfigUIBuilder()
        {
            Width = ConfigOverlayWidth;
            Recalculate();
        }

        public void Recalculate()
        {
            int totalRows = 24;
            int totalSections = 4;

            int scrollableContentNatural = (ConfigSectionHeaderHeight * totalSections)
                                           + (ConfigRowHeight * totalRows);

            Vector2 titleSize = Game1.dialogueFont.MeasureString(TranslationCache.ConfigSettingsTitle);
            int titleBoxHeight = ConfigTitleTopPadding + (int)titleSize.Y + ConfigTitleBottomPadding;

            int buttonBoxFullHeight = ConfigButtonBoxHeight + ConfigBoxGap;
            int naturalHeight = FixedTopHeight + titleBoxHeight + scrollableContentNatural + buttonBoxFullHeight + FixedBottomHeight;
            int maxHeight = ConfigMaxHeight;
            Height = Math.Min(naturalHeight, maxHeight);

            int availableForContent = Height - FixedTopHeight - titleBoxHeight - buttonBoxFullHeight - FixedBottomHeight;
            int visibleContentHeight = availableForContent - ConfigBorderPadding * 2;
            visibleContentHeight = (visibleContentHeight / ConfigRowHeight) * ConfigRowHeight;
            availableForContent = visibleContentHeight + ConfigBorderPadding * 2;
            Height = FixedTopHeight + titleBoxHeight + availableForContent + buttonBoxFullHeight + FixedBottomHeight;
            MaxScrollOffset = Math.Max(0, scrollableContentNatural - visibleContentHeight);
            ClampScrollOffset();

            X = (Game1.uiViewport.Width - Width) / 2;
            Y = (Game1.uiViewport.Height - Height) / 2;

            int currentY = Y + ConfigBorderPadding;

            int titleBoxWidth = (int)titleSize.X + ConfigTitleSidePadding * 2;
            int titleBoxX = X + (Width - titleBoxWidth) / 2;
            TitleBoxBounds = new Rectangle(titleBoxX, currentY, titleBoxWidth, titleBoxHeight);
            currentY += titleBoxHeight;

            ContentBoxBounds = new Rectangle(X, currentY, Width, availableForContent);
            ContentClipRect = new Rectangle(X + ConfigBorderPadding, currentY + ConfigBorderPadding,
                Width - ConfigBorderPadding * 2 + ConfigScrollArrowRightExtend, availableForContent - ConfigBorderPadding * 2);
            currentY += availableForContent + ConfigBoxGap;

            ButtonBoxBounds = new Rectangle(X, currentY, Width, ConfigButtonBoxHeight);

            contentX = X + ConfigBorderPadding;
            contentWidth = Width - ConfigBorderPadding * 2;
            controlX = contentX + contentWidth - ConfigControlRightPadding;

            CalculateLayout();
        }

        public void ClampScrollOffset()
        {
            ScrollOffset = Math.Clamp(ScrollOffset, 0, MaxScrollOffset);
        }

        private void CalculateLayout()
        {
            var tooltipsList = new List<string>();
            var labelsList = new List<string>();
            var rowYList = new List<int>();

            int rowX = contentX + ConfigRowIndent;
            int currentY = ContentBoxBounds.Y + ConfigBorderPadding - ScrollOffset;

            GeneralHeaderY = currentY;
            currentY += ConfigSectionHeaderHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigToggleMenuKeyName);
            tooltipsList.Add(TranslationCache.ConfigToggleMenuKeyTooltip);
            ToggleMenuKeyArea = CreateKeybindArea(currentY, "ToggleMenuKey");
            currentY += ConfigRowHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigToggleItemInfoKeyName);
            tooltipsList.Add(TranslationCache.ConfigToggleItemInfoKeyTooltip);
            ToggleItemInfoKeyArea = CreateKeybindArea(currentY, "ToggleItemInfoKey");
            currentY += ConfigRowHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigToggleWardrobeKeyName);
            tooltipsList.Add(TranslationCache.ConfigToggleWardrobeKeyTooltip);
            ToggleWardrobeKeyArea = CreateKeybindArea(currentY, "ToggleWardrobeKey");
            currentY += ConfigRowHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigToggleScheduleKeyName);
            tooltipsList.Add(TranslationCache.ConfigToggleScheduleKeyTooltip);
            ToggleScheduleKeyArea = CreateKeybindArea(currentY, "ToggleScheduleKey");
            currentY += ConfigRowHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigIncludeHairInOutfitSetsName);
            tooltipsList.Add(TranslationCache.ConfigIncludeHairInOutfitSetsTooltip);
            IncludeHairInOutfitSetsCheckbox = CreateCheckbox(currentY, "IncludeHairInOutfitSets");
            currentY += ConfigRowHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigShowTooltipName);
            tooltipsList.Add(TranslationCache.ConfigShowTooltipTooltip);
            ShowTooltipCheckbox = CreateCheckbox(currentY, "ShowTooltip");
            currentY += ConfigRowHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigCloseOnClickOutsideName);
            tooltipsList.Add(TranslationCache.ConfigCloseOnClickOutsideTooltip);
            CloseOnClickOutsideCheckbox = CreateCheckbox(currentY, "CloseOnClickOutside");
            currentY += ConfigRowHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigAutoOpenTagMenuName);
            tooltipsList.Add(TranslationCache.ConfigAutoOpenTagMenuTooltip);
            AutoOpenTagMenuCheckbox = CreateCheckbox(currentY, "AutoOpenTagMenu");
            currentY += ConfigRowHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigAutoFocusSearchBarName);
            tooltipsList.Add(TranslationCache.ConfigAutoFocusSearchBarTooltip);
            AutoFocusSearchBarCheckbox = CreateCheckbox(currentY, "AutoFocusSearchBar");
            currentY += ConfigRowHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigArrowKeyScrollingName);
            tooltipsList.Add(TranslationCache.ConfigArrowKeyScrollingTooltip);
            ArrowKeyScrollingCheckbox = CreateCheckbox(currentY, "ArrowKeyScrolling");
            currentY += ConfigRowHeight;

            MainMenuHeaderY = currentY;
            currentY += ConfigSectionHeaderHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigResetFilterOnTabSwitchName);
            tooltipsList.Add(TranslationCache.ConfigResetFilterOnTabSwitchTooltip);
            ResetFilterCheckbox = CreateCheckbox(currentY, "ResetFilter");
            currentY += ConfigRowHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigResetSearchOnTabSwitchName);
            tooltipsList.Add(TranslationCache.ConfigResetSearchOnTabSwitchTooltip);
            ResetSearchCheckbox = CreateCheckbox(currentY, "ResetSearch");
            currentY += ConfigRowHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigAutoOpenDyeColorMenuName);
            tooltipsList.Add(TranslationCache.ConfigAutoOpenDyeColorMenuTooltip);
            AutoOpenDyeColorMenuCheckbox = CreateCheckbox(currentY, "AutoOpenDyeColorMenu");
            currentY += ConfigRowHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigVisibleRowsName);
            tooltipsList.Add(TranslationCache.ConfigVisibleRowsTooltip);
            VisibleRowsSlider = CreateDiscreteSlider(currentY, MinVisibleRows, MaxVisibleRows,
                VisibleRowsSlider?.Value ?? DefaultVisibleRows);
            currentY += ConfigRowHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigVisibleColumnsName);
            tooltipsList.Add(TranslationCache.ConfigVisibleColumnsTooltip);
            VisibleColumnsSlider = CreateDiscreteSlider(currentY, MinVisibleColumns, MaxVisibleColumns,
                VisibleColumnsSlider?.Value ?? DefaultVisibleColumns);
            currentY += ConfigRowHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigSlotSizeName);
            tooltipsList.Add(TranslationCache.ConfigSlotSizeTooltip);
            SlotSizeSlider = CreateDiscreteSlider(currentY, MinSlotSize, MaxSlotSize,
                SlotSizeSlider?.Value ?? DefaultSlotSize);
            currentY += ConfigRowHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigShowScheduleDebugLogName);
            tooltipsList.Add(TranslationCache.ConfigShowScheduleDebugLogTooltip);
            ShowScheduleDebugLogCheckbox = CreateCheckbox(currentY, "ShowScheduleDebugLog");
            currentY += ConfigRowHeight;

            WardrobeHeaderY = currentY;
            currentY += ConfigSectionHeaderHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigResetMatchAllOnOpenName);
            tooltipsList.Add(TranslationCache.ConfigResetMatchAllOnOpenTooltip);
            ResetMatchAllCheckbox = CreateCheckbox(currentY, "ResetMatchAll");
            currentY += ConfigRowHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigResetShowInvalidOnOpenName);
            tooltipsList.Add(TranslationCache.ConfigResetShowInvalidOnOpenTooltip);
            ResetShowInvalidCheckbox = CreateCheckbox(currentY, "ResetShowInvalid");
            currentY += ConfigRowHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigDefaultSearchScopeName);
            tooltipsList.Add(TranslationCache.ConfigDefaultSearchScopeTooltip);
            int dropdownX = controlX - ConfigDropdownMinWidth;
            SearchScopeDropdown = new ClickableComponent(
                new Rectangle(dropdownX, currentY, ConfigDropdownMinWidth, ConfigRowHeight),
                "SearchScopeDropdown"
            );

            string[] scopeLabels = {
                TranslationCache.WardrobeFilterSearchSet,
                TranslationCache.WardrobeFilterSearchItem,
                TranslationCache.WardrobeFilterSearchAll
            };
            int maxLabelWidth = 0;
            foreach (var lbl in scopeLabels)
                maxLabelWidth = Math.Max(maxLabelWidth, (int)Game1.smallFont.MeasureString(lbl).X);
            searchScopePanelW = CalculateDropdownPanelWidth(maxLabelWidth, ConfigDropdownMaxWidth);
            SearchScopePanelAnchor = new Rectangle(controlX - searchScopePanelW, currentY, searchScopePanelW, ConfigRowHeight);

            BuildSearchScopeOptions();
            currentY += ConfigRowHeight;

            ScheduleHeaderY = currentY;
            currentY += ConfigSectionHeaderHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigConsistentTiebreaksName);
            tooltipsList.Add(TranslationCache.ConfigConsistentTiebreaksTooltip);
            ConsistentTiebreaksCheckbox = CreateCheckbox(currentY, "ConsistentTiebreaks");
            currentY += ConfigRowHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigLockManualOutfitName);
            tooltipsList.Add(TranslationCache.ConfigLockManualOutfitTooltip);
            LockManualOutfitCheckbox = CreateCheckbox(currentY, "LockManualOutfit");
            currentY += ConfigRowHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigDefaultPriorityName);
            tooltipsList.Add(TranslationCache.ConfigDefaultPriorityTooltip);
            DefaultPriorityDropdown = new ClickableComponent(
                new Rectangle(dropdownX, currentY, ConfigDropdownMinWidth, ConfigRowHeight),
                "DefaultPriorityDropdown"
            );
            {
                string[] priorityLabels = {
                    TranslationCache.ScheduleEditPriorityLow,
                    TranslationCache.ScheduleEditPriorityMedium,
                    TranslationCache.ScheduleEditPriorityHigh
                };
                int maxPriorityWidth = 0;
                foreach (var lbl in priorityLabels)
                    maxPriorityWidth = Math.Max(maxPriorityWidth, (int)Game1.smallFont.MeasureString(lbl).X);
                priorityPanelW = CalculateDropdownPanelWidth(maxPriorityWidth, ConfigDropdownMaxWidth);
                DefaultPriorityPanelAnchor = new Rectangle(controlX - priorityPanelW, currentY, priorityPanelW, ConfigRowHeight);
            }
            BuildDefaultPriorityOptions();
            currentY += ConfigRowHeight;

            rowYList.Add(currentY);
            labelsList.Add(TranslationCache.ConfigDefaultRotationName);
            tooltipsList.Add(TranslationCache.ConfigDefaultRotationTooltip);
            DefaultRotationDropdown = new ClickableComponent(
                new Rectangle(dropdownX, currentY, ConfigDropdownMinWidth, ConfigRowHeight),
                "DefaultRotationDropdown"
            );
            {
                string[] rotationLabels = {
                    TranslationCache.ScheduleEditRotateOnceADay,
                    TranslationCache.ScheduleEditRotateOnLocationChange
                };
                int maxRotationWidth = 0;
                foreach (var lbl in rotationLabels)
                    maxRotationWidth = Math.Max(maxRotationWidth, (int)Game1.smallFont.MeasureString(lbl).X);
                rotationPanelW = CalculateDropdownPanelWidth(maxRotationWidth, ConfigDropdownMaxWidth);
                DefaultRotationPanelAnchor = new Rectangle(controlX - rotationPanelW, currentY, rotationPanelW, ConfigRowHeight);
            }
            BuildDefaultRotationOptions();
            currentY += ConfigRowHeight;

            rowYPositions = rowYList.ToArray();
            rowLabels = labelsList.ToArray();
            rowTooltips = tooltipsList.ToArray();

            int buttonY = ButtonBoxBounds.Y + (ConfigButtonBoxHeight - TabAndButtonHeight) / 2;
            int maxButtonWidth = (Width - ConfigBottomButtonGap) / 2;
            int saveWidth = UIHelpers.CalculateButtonWidth(TranslationCache.CommonSave, maxButtonWidth);
            int cancelWidth = UIHelpers.CalculateButtonWidth(TranslationCache.CommonCancel, maxButtonWidth);
            int totalBtnWidth = saveWidth + cancelWidth + ConfigBottomButtonGap;
            int btnStartX = X + (Width - totalBtnWidth) / 2;

            SaveButton = new ClickableComponent(
                new Rectangle(btnStartX, buttonY, saveWidth, TabAndButtonHeight),
                "save"
            );
            CancelButton = new ClickableComponent(
                new Rectangle(btnStartX + saveWidth + ConfigBottomButtonGap, buttonY, cancelWidth, TabAndButtonHeight),
                "cancel"
            );

            CloseButton = new ClickableTextureComponent(
                new Rectangle(X + Width - CloseButtonSize - CloseButtonEdgeMargin,
                    Y + CloseButtonEdgeMargin, CloseButtonSize, CloseButtonSize),
                Game1.mouseCursors,
                new Rectangle(337, 494, 12, 12),
                4f
            );
        }

        private void BuildSearchScopeOptions()
        {
            SearchScopeOptions.Clear();
            string[] values = { "Set", "Item", "All" };
            string[] labels = {
                TranslationCache.WardrobeFilterSearchSet,
                TranslationCache.WardrobeFilterSearchItem,
                TranslationCache.WardrobeFilterSearchAll
            };

            int optionHeight = ConfigRowHeight;
            int baseY = SearchScopePanelAnchor.Bottom + ConfigDropdownTopPadding;
            for (int i = 0; i < values.Length; i++)
            {
                SearchScopeOptions.Add(new ClickableComponent(
                    new Rectangle(SearchScopePanelAnchor.X, baseY + i * optionHeight,
                        SearchScopePanelAnchor.Width, optionHeight),
                    values[i]
                ) { label = labels[i] });
            }
        }

        private void BuildDefaultPriorityOptions()
        {
            DefaultPriorityOptions.Clear();
            string[] labels = {
                TranslationCache.ScheduleEditPriorityLow,
                TranslationCache.ScheduleEditPriorityMedium,
                TranslationCache.ScheduleEditPriorityHigh
            };
            string[] values = { "1", "2", "3" };

            int optionHeight = ConfigRowHeight;
            int baseY = DefaultPriorityPanelAnchor.Bottom + ConfigDropdownTopPadding;
            for (int i = 0; i < labels.Length; i++)
            {
                DefaultPriorityOptions.Add(new ClickableComponent(
                    new Rectangle(DefaultPriorityPanelAnchor.X, baseY + i * optionHeight,
                        DefaultPriorityPanelAnchor.Width, optionHeight),
                    labels[i]
                ) { label = values[i] });
            }
        }

        private void BuildDefaultRotationOptions()
        {
            DefaultRotationOptions.Clear();
            string[] values = { "false", "true" };
            string[] labels = {
                TranslationCache.ScheduleEditRotateOnceADay,
                TranslationCache.ScheduleEditRotateOnLocationChange
            };

            int optionHeight = ConfigRowHeight;
            int baseY = DefaultRotationPanelAnchor.Bottom + ConfigDropdownTopPadding;
            for (int i = 0; i < values.Length; i++)
            {
                DefaultRotationOptions.Add(new ClickableComponent(
                    new Rectangle(DefaultRotationPanelAnchor.X, baseY + i * optionHeight,
                        DefaultRotationPanelAnchor.Width, optionHeight),
                    labels[i]
                ) { label = values[i] });
            }
        }

        internal static int CalculateCenteredPanelX(int controlX, int valueWidth, int panelWidth)
        {
            return controlX - valueWidth / 2 - panelWidth / 2;
        }

        internal static int CalculateDropdownPanelWidth(int maxOptionTextWidth, int maxWidth)
        {
            return Math.Min(maxWidth, maxOptionTextWidth + 32);
        }

        public void UpdateSearchScopePanelPosition(string displayValue)
        {
            int valueW = (int)Game1.smallFont.MeasureString(displayValue).X;
            int panelX = CalculateCenteredPanelX(controlX, valueW, searchScopePanelW);
            SearchScopePanelAnchor = new Rectangle(panelX, SearchScopePanelAnchor.Y, searchScopePanelW, ConfigRowHeight);
            BuildSearchScopeOptions();
        }

        public void UpdatePriorityPanelPosition(string displayValue)
        {
            int valueW = (int)Game1.smallFont.MeasureString(displayValue).X;
            int panelX = CalculateCenteredPanelX(controlX, valueW, priorityPanelW);
            DefaultPriorityPanelAnchor = new Rectangle(panelX, DefaultPriorityPanelAnchor.Y, priorityPanelW, ConfigRowHeight);
            BuildDefaultPriorityOptions();
        }

        public void UpdateRotationPanelPosition(string displayValue)
        {
            int valueW = (int)Game1.smallFont.MeasureString(displayValue).X;
            int panelX = CalculateCenteredPanelX(controlX, valueW, rotationPanelW);
            DefaultRotationPanelAnchor = new Rectangle(panelX, DefaultRotationPanelAnchor.Y, rotationPanelW, ConfigRowHeight);
            BuildDefaultRotationOptions();
        }

        private ClickableComponent CreateCheckbox(int rowY, string name)
        {
            int checkY = rowY + (ConfigRowHeight - ConfigCheckboxSize) / 2;
            return new ClickableComponent(
                new Rectangle(controlX - ConfigCheckboxSize, checkY, ConfigCheckboxSize, ConfigCheckboxSize),
                name
            );
        }

        private ClickableComponent CreateKeybindArea(int rowY, string name)
        {
            return new ClickableComponent(
                new Rectangle(controlX - ConfigKeybindBoxWidth, rowY, ConfigKeybindBoxWidth, ConfigRowHeight),
                name
            );
        }

        private DiscreteSlider CreateDiscreteSlider(int rowY, int min, int max, int initialValue)
        {
            int sliderY = rowY + (ConfigRowHeight - ConfigSliderHeight) / 2;
            int sliderX = controlX - ConfigSliderWidth - ConfigSliderValueLabelGap - 30;
            return new DiscreteSlider(sliderX, sliderY, ConfigSliderWidth, ConfigSliderHeight, min, max, initialValue);
        }

        public void DrawTitle(SpriteBatch b)
        {
            string title = TranslationCache.ConfigSettingsTitle;
            Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
            Utility.drawTextWithShadow(b, title, Game1.dialogueFont,
                new Vector2(TitleBoxBounds.X + (TitleBoxBounds.Width - titleSize.X) / 2,
                    TitleBoxBounds.Y + ConfigTitleTopPadding),
                Game1.textColor);
        }

        public void DrawSectionHeader(SpriteBatch b, int headerY, string text)
        {
            float textHeight = Game1.smallFont.MeasureString(text).Y;
            int textY = headerY + (int)((ConfigSectionHeaderHeight - textHeight) / 2);
            Vector2 pos = new Vector2(contentX, textY);
            Utility.drawTextWithShadow(b, text, Game1.smallFont, pos, Game1.textColor);
            Utility.drawTextWithShadow(b, text, Game1.smallFont, pos + new Vector2(1, 0), Game1.textColor);
        }

        public void DrawCheckboxRow(SpriteBatch b, string label, bool isChecked, ClickableComponent checkbox)
        {
            int rowY = checkbox.bounds.Y - (ConfigRowHeight - ConfigCheckboxSize) / 2;
            DrawRowLabel(b, label, rowY);

            Rectangle sourceRect = isChecked ? UIHelpers.CheckedSourceRect : UIHelpers.UncheckedSourceRect;
            b.Draw(Game1.mouseCursors,
                new Vector2(checkbox.bounds.X, checkbox.bounds.Y),
                sourceRect, Color.White, 0f, Vector2.Zero, ConfigCheckboxScale, SpriteEffects.None, 1f);
        }

        public void DrawKeybindRow(SpriteBatch b, string label, KeybindList keybind, ClickableComponent area, bool isListening)
        {
            DrawRowLabel(b, label, area.bounds.Y);

            string displayText = isListening
                ? TranslationCache.ConfigKeybindListening
                : keybind.ToString();

            Vector2 textSize = Game1.smallFont.MeasureString(displayText);
            float textX = controlX - textSize.X;
            float textY = area.bounds.Y + (ConfigRowHeight - textSize.Y) / 2;
            Vector2 textPos = new Vector2(textX, textY);

            Color textColor = isListening ? Color.Gray : Game1.textColor;
            bool isHovered = !isListening && area.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            if (isHovered)
            {
                Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPos + new Vector2(-1, 0), textColor * 0.8f);
                Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPos, textColor);
            }
            else
            {
                Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPos, textColor);
            }
        }

        public void DrawSliderRow(SpriteBatch b, string label, DiscreteSlider slider)
        {
            int rowY = slider.Bounds.Y - (ConfigRowHeight - ConfigSliderHeight) / 2;
            DrawRowLabel(b, label, rowY);

            slider.Draw(b);

            string valueText = slider.Value.ToString();
            Vector2 valueSize = Game1.smallFont.MeasureString(valueText);
            int valueLabelX = slider.Bounds.Right + ConfigSliderValueLabelGap;
            int valueLabelY = rowY + (int)((ConfigRowHeight - valueSize.Y) / 2);
            Utility.drawTextWithShadow(b, valueText, Game1.smallFont,
                new Vector2(valueLabelX, valueLabelY), Game1.textColor);
        }

        public void DrawSearchScopeRow(SpriteBatch b, string label, string currentValue, bool isOpen)
        {
            DrawDropdownRow(b, label, FormatSearchScope(currentValue), SearchScopeDropdown, isOpen);
        }

        public void DrawSearchScopeDropdownOptions(SpriteBatch b, string currentValue)
        {
            UIHelpers.DrawDropdownOptions(
                b,
                SearchScopePanelAnchor,
                SearchScopeOptions,
                firstVisibleIndex: 0,
                maxVisibleItems: ConfigDropdownMaxVisible,
                isSelected: option => option.name == currentValue,
                enableTruncation: true,
                panelPaddingV: ConfigDropdownTopPadding
            );
        }

        public void DrawDefaultPriorityRow(SpriteBatch b, string label, string displayValue, bool isOpen)
        {
            DrawDropdownRow(b, label, displayValue, DefaultPriorityDropdown, isOpen);
        }

        public void DrawDefaultPriorityDropdownOptions(SpriteBatch b, string currentValue)
        {
            UIHelpers.DrawDropdownOptions(
                b,
                DefaultPriorityPanelAnchor,
                DefaultPriorityOptions,
                firstVisibleIndex: 0,
                maxVisibleItems: ConfigDropdownMaxVisible,
                isSelected: option => option.label == currentValue,
                enableTruncation: true,
                panelPaddingV: ConfigDropdownTopPadding
            );
        }

        public void DrawDefaultRotationRow(SpriteBatch b, string label, string displayValue, bool isOpen)
        {
            DrawDropdownRow(b, label, displayValue, DefaultRotationDropdown, isOpen);
        }

        public void DrawDefaultRotationDropdownOptions(SpriteBatch b, string currentValue)
        {
            UIHelpers.DrawDropdownOptions(
                b,
                DefaultRotationPanelAnchor,
                DefaultRotationOptions,
                firstVisibleIndex: 0,
                maxVisibleItems: ConfigDropdownMaxVisible,
                isSelected: option => option.label == currentValue,
                enableTruncation: true,
                panelPaddingV: ConfigDropdownTopPadding
            );
        }

        private void DrawDropdownRow(SpriteBatch b, string label, string displayValue, ClickableComponent dropdown, bool isOpen)
        {
            int rowY = dropdown.bounds.Y;
            DrawRowLabel(b, label, rowY);

            Vector2 textSize = Game1.smallFont.MeasureString(displayValue);
            float textX = controlX - textSize.X;
            float textY = rowY + (ConfigRowHeight - textSize.Y) / 2;
            Vector2 textPos = new Vector2(textX, textY);

            bool isHovered = !isOpen && dropdown.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            if (isHovered)
            {
                Utility.drawTextWithShadow(b, displayValue, Game1.smallFont, textPos + new Vector2(-1, 0), Game1.textColor * 0.8f);
                Utility.drawTextWithShadow(b, displayValue, Game1.smallFont, textPos, Game1.textColor);
            }
            else
            {
                Utility.drawTextWithShadow(b, displayValue, Game1.smallFont, textPos, Game1.textColor);
            }
        }

        public void DrawButtons(SpriteBatch b)
        {
            UIHelpers.DrawTextButton(b, SaveButton, TranslationCache.CommonSave);
            UIHelpers.DrawTextButton(b, CancelButton, TranslationCache.CommonCancel);
        }

        public void DrawCloseButton(SpriteBatch b)
        {
            UIHelpers.DrawTextureButton(b, CloseButton);
        }

        public void DrawScrollIndicators(SpriteBatch b)
        {
            if (!IsScrollable) return;

            int arrowW = (int)(11 * ConfigScrollArrowScale);
            int arrowH = (int)(12 * ConfigScrollArrowScale);
            int arrowX = contentX + contentWidth - arrowW + ConfigScrollArrowRightExtend;

            if (ScrollOffset > 0)
            {
                int arrowY = ContentClipRect.Top + 4;
                b.Draw(Game1.mouseCursors, new Vector2(arrowX, arrowY),
                    UIHelpers.UpScrollArrowSourceRect, Color.White, 0f, Vector2.Zero,
                    ConfigScrollArrowScale, SpriteEffects.None, 1f);
            }

            if (ScrollOffset < MaxScrollOffset)
            {
                int arrowY = ContentClipRect.Bottom - arrowH - 4;
                b.Draw(Game1.mouseCursors, new Vector2(arrowX, arrowY),
                    UIHelpers.DownScrollArrowSourceRect, Color.White, 0f, Vector2.Zero,
                    ConfigScrollArrowScale, SpriteEffects.None, 1f);
            }
        }

        public string? GetHoveredTooltip(int mouseX, int mouseY)
        {
            if (IsScrollable && !ContentClipRect.Contains(mouseX, mouseY))
                return null;

            int labelX = contentX + ConfigRowIndent;
            for (int i = 0; i < rowYPositions.Length; i++)
            {
                int labelWidth = (int)Game1.smallFont.MeasureString(rowLabels[i]).X;
                Rectangle labelBounds = new Rectangle(labelX, rowYPositions[i], labelWidth, ConfigRowHeight);
                if (labelBounds.Contains(mouseX, mouseY))
                    return rowTooltips[i];
            }
            return null;
        }

        private void DrawRowLabel(SpriteBatch b, string label, int rowY)
        {
            float textHeight = Game1.smallFont.MeasureString(label).Y;
            int textY = rowY + (int)((ConfigRowHeight - textHeight) / 2);
            Utility.drawTextWithShadow(b, label, Game1.smallFont,
                new Vector2(contentX + ConfigRowIndent, textY), Game1.textColor);
        }

        public static string FormatSearchScope(string value)
        {
            return value switch
            {
                "Set" => TranslationCache.WardrobeFilterSearchSet,
                "Item" => TranslationCache.WardrobeFilterSearchItem,
                "All" => TranslationCache.WardrobeFilterSearchAll,
                _ => value
            };
        }
    }
}
