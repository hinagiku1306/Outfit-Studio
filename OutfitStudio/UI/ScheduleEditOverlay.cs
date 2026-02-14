using System;
using System.Collections.Generic;
using System.Linq;
using OutfitStudio.Models;
using OutfitStudio.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.GameData;
using StardewValley.Menus;
using StardewValley.TokenizableStrings;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    public class ScheduleEditOverlay : IClickableMenu
    {
        private static readonly string[] SeasonValues = { "Spring", "Summer", "Fall", "Winter" };
        private static readonly string[] AreaValues = { "Outdoor", "Indoor" };

        private readonly IClickableMenu parentMenu;
        private readonly ScheduleEditUIBuilder uiBuilder;
        private readonly ScheduleStore scheduleStore;
        private readonly OutfitSetStore outfitSetStore;
        private readonly ScheduleRule? editingRule;
        private readonly Action onSaveComplete;

        // State - triggers
        private HashSet<string> selectedSeasons = new(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> selectedWeather = new(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> selectedAreas = new(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> selectedLocations = new(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> selectedFestivals = new(StringComparer.OrdinalIgnoreCase);
        private bool isWeddingDay;

        // State - other
        private bool tagsSelectAll;
        private HashSet<string> selectedTags = new(TranslationCache.TagComparer);
        private List<string> excludedSetIds = new();
        private int priority = 2;
        private bool advanceOnWarp;

        // Priority dropdown
        private bool priorityDropdownOpen;
        private List<ClickableComponent> priorityOptions = new();

        // Rotate dropdown
        private bool rotateDropdownOpen;
        private List<ClickableComponent> rotateOptions = new();

        // Dropdown data
        private List<string> seasonOptionNames = new();
        private List<(string id, string displayName)> weatherEntries = new();
        private List<string> weatherOptionNames = new();
        private List<string> areaOptionNames = new();
        private List<(string name, string displayName)> locationEntries = new();
        private List<string> locationOptionNames = new();
        private List<(string id, string displayName)> festivalEntries = new();
        private List<string> festivalOptionNames = new();

        // Dropdown state
        private bool seasonsDropdownOpen;
        private bool weatherDropdownOpen;
        private bool areasDropdownOpen;
        private bool locationsDropdownOpen;
        private bool festivalsDropdownOpen;

        private int seasonsScrollIndex;
        private int weatherScrollIndex;
        private int areasScrollIndex;
        private int locationsScrollIndex;
        private int festivalsScrollIndex;

        private List<ClickableComponent> visibleSeasonsOptions = new();
        private List<ClickableComponent> visibleWeatherOptions = new();
        private List<ClickableComponent> visibleAreasOptions = new();
        private List<ClickableComponent> visibleLocationsOptions = new();
        private List<ClickableComponent> visibleFestivalsOptions = new();

        // Search-type dropdown state (Festival + Location)
        private TextBox? searchTextBox;
        private string activeSearchDropdown = "";
        private string searchText = "";
        private List<(string id, string displayName)> filteredFestivalEntries = new();
        private List<string> filteredFestivalOptionNames = new();
        private List<(string name, string displayName)> filteredLocationEntries = new();
        private List<string> filteredLocationOptionNames = new();

        // State - included sets
        private List<string> includedSetIds = new();

        // Tag picker (select-only, opens to the left)
        private readonly TagPickerManager tagPicker;

        // Set picker (opens to the right)
        private readonly ScheduleSetPickerPanel setPicker;

        // Child overlay
        private SetPreviewOverlay? previewOverlay;

        private readonly ContinuousScrollHandler scrollHandler = new();

        // Cached pool counts (avoid per-frame ResolvePool)
        private int? cachedTotalOutfits;
        private int? cachedRemaining;

        public bool IsEditing => editingRule != null;

        public ScheduleEditOverlay(IClickableMenu parentMenu, ScheduleStore scheduleStore, OutfitSetStore outfitSetStore, ScheduleRule? editingRule, Action onSaveComplete)
        {
            this.parentMenu = parentMenu;
            this.scheduleStore = scheduleStore;
            this.outfitSetStore = outfitSetStore;
            this.editingRule = editingRule;
            this.onSaveComplete = onSaveComplete;

            uiBuilder = new ScheduleEditUIBuilder();
            tagPicker = new TagPickerManager(outfitSetStore, selectOnly: true);
            setPicker = new ScheduleSetPickerPanel(outfitSetStore, OnSetPickerChanged);

            LoadDropdownData();

            if (editingRule != null)
            {
                selectedSeasons = new HashSet<string>(editingRule.SelectedSeasons, StringComparer.OrdinalIgnoreCase);
                selectedWeather = new HashSet<string>(editingRule.SelectedWeather, StringComparer.OrdinalIgnoreCase);
                selectedAreas = new HashSet<string>(editingRule.SelectedAreas, StringComparer.OrdinalIgnoreCase);
                selectedLocations = new HashSet<string>(editingRule.SelectedLocations, StringComparer.OrdinalIgnoreCase);
                selectedFestivals = new HashSet<string>(editingRule.SelectedFestivals, StringComparer.OrdinalIgnoreCase);
                isWeddingDay = editingRule.IsWeddingDay;
                tagsSelectAll = editingRule.TagsSelectAll;
                selectedTags = new HashSet<string>(editingRule.SelectedTags, TranslationCache.TagComparer);
                excludedSetIds = new List<string>(editingRule.ExcludedSetIds);
                includedSetIds = new List<string>(editingRule.IncludedSetIds);
                priority = editingRule.Priority;
                advanceOnWarp = editingRule.AdvanceOnWarp;
            }

            Game1.keyboardDispatcher.Subscriber = null;

            uiBuilder.Recalculate();
            uiBuilder.BuildPriorityOptions(priorityOptions);
            uiBuilder.BuildRotateOptions(rotateOptions);
            width = uiBuilder.Width;
            height = uiBuilder.Height;
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;

            if (ModEntry.Config.AutoOpenTagMenu)
                OpenTagPicker();
        }

        private void LoadDropdownData()
        {
            // Seasons (no "All")
            foreach (var s in SeasonValues)
                seasonOptionNames.Add(s);

            // Weather (from Game1 constants, excluding Festival/Wedding which are handled separately)
            weatherEntries.Add((Game1.weather_sunny, TranslationCache.ScheduleEditWeatherSunny));
            weatherEntries.Add((Game1.weather_rain, TranslationCache.ScheduleEditWeatherRainy));
            weatherEntries.Add((Game1.weather_lightning, TranslationCache.ScheduleEditWeatherStormy));
            weatherEntries.Add((Game1.weather_snow, TranslationCache.ScheduleEditWeatherSnowy));
            weatherEntries.Add((Game1.weather_debris, TranslationCache.ScheduleEditWeatherWindy));
            weatherEntries.Add((Game1.weather_green_rain, TranslationCache.ScheduleEditWeatherGreenRain));

            foreach (var entry in weatherEntries)
                weatherOptionNames.Add(entry.displayName);

            // Area
            areaOptionNames.Add(TranslationCache.ScheduleEditAreaOutdoor);
            areaOptionNames.Add(TranslationCache.ScheduleEditAreaIndoor);

            // Location (from game data, with display names)
            try
            {
                if (Game1.locations != null)
                {
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var loc in Game1.locations)
                    {
                        if (string.IsNullOrEmpty(loc?.Name) || !seen.Add(loc.Name))
                            continue;
                        string display = loc.DisplayName ?? loc.Name;
                        locationEntries.Add((loc.Name, display));
                    }
                    locationEntries.Sort((a, b) => string.Compare(a.displayName, b.displayName, StringComparison.OrdinalIgnoreCase));
                }
            }
            catch { }

            foreach (var entry in locationEntries)
                locationOptionNames.Add(entry.displayName);

            // Festivals (no "All", filter bad entries)
            try
            {
                var activeFestivals = DataLoader.Festivals_FestivalDates(Game1.content);
                foreach (var (key, name) in activeFestivals)
                {
                    if (!string.IsNullOrEmpty(name) && !name.StartsWith("LocalizedString"))
                        festivalEntries.Add((key, name));
                }

                var passiveFestivals = DataLoader.PassiveFestivals(Game1.content);
                foreach (var (key, data) in passiveFestivals)
                {
                    string parsed = TokenParser.ParseText(data.DisplayName);
                    if (!string.IsNullOrEmpty(parsed))
                        festivalEntries.Add((key, parsed));
                }

                festivalEntries.Sort((a, b) => string.Compare(a.displayName, b.displayName, StringComparison.OrdinalIgnoreCase));
            }
            catch { }

            foreach (var entry in festivalEntries)
                festivalOptionNames.Add(entry.displayName);
        }

        // --- Click handling ---

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            if (previewOverlay != null)
            {
                previewOverlay.receiveLeftClick(x, y, playSound);
                if (previewOverlay.readyToClose())
                    previewOverlay = null;
                return;
            }

            // Toggle off pickers when clicking their own [+] button
            if (tagPicker.IsOpen && uiBuilder.TagsAddButton.containsPoint(x, y))
            {
                tagPicker.Close();
                if (playSound) Game1.playSound("smallSelect");
                return;
            }
            if (setPicker.IsOpen && uiBuilder.SetsAddButton.containsPoint(x, y))
            {
                setPicker.Close();
                if (playSound) Game1.playSound("smallSelect");
                return;
            }

            // Forward to tag picker
            if (tagPicker.IsOpen)
            {
                bool consumed;
                tagPicker.HandleClick(x, y, out consumed);
                if (consumed)
                    return;
                // Click was outside picker — close it and continue
                tagPicker.Close();
            }

            // Forward to set picker
            if (setPicker.IsOpen)
            {
                bool consumed;
                setPicker.HandleClick(x, y, out consumed);
                if (consumed)
                    return;
                setPicker.Close();
            }

            // Close X button
            if (uiBuilder.CloseButton.containsPoint(x, y))
            {
                CloseOverlay();
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            // Click outside
            if (!isWithinBounds(x, y) && !IsInAnyDropdownPanel(x, y) && !IsInAnyPicker(x, y) && ModEntry.Config.CloseOnClickOutside)
            {
                CloseOverlay();
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            // Clear buttons (always accessible)
            if (HandleClearButtonClick(x, y, playSound))
                return;

            bool anyDropdownOpen = IsAnyDropdownOpen();

            if (anyDropdownOpen)
            {
                // Dropdown options first (they may overlap with bottom buttons)
                if (TryHandleDropdownOptionClick(x, y, playSound))
                    return;

                // Bottom buttons
                if (HandleBottomButtonClick(x, y, playSound))
                    return;

                // Dropdown bar clicks (toggle/switch)
                if (HandleDropdownBarClick(x, y, playSound))
                    return;

                // Click elsewhere closes dropdown
                CloseAllDropdowns();
                return;
            }

            // Dropdown bar toggles
            if (HandleDropdownBarClick(x, y, playSound))
                return;

            // Wedding checkbox
            if (IsInWeddingRow(x, y))
            {
                isWeddingDay = !isWeddingDay;
                if (playSound) Game1.playSound("drumkit6");
                return;
            }

            // Tags [+] button
            if (uiBuilder.TagsAddButton.containsPoint(x, y))
            {
                OpenTagPicker();
                if (playSound) Game1.playSound("smallSelect");
                return;
            }

            // Sets [+] button
            if (uiBuilder.SetsAddButton.containsPoint(x, y))
            {
                OpenSetPicker();
                if (playSound) Game1.playSound("smallSelect");
                return;
            }

            // Bottom buttons
            HandleBottomButtonClick(x, y, playSound);
        }

        private bool IsInWeddingRow(int x, int y)
        {
            int rowY = uiBuilder.WeddingCheckbox.bounds.Y - (TabAndButtonHeight - ScheduleCheckboxSize) / 2;
            return y >= rowY && y < rowY + TabAndButtonHeight
                && x >= uiBuilder.RightColBarX;
        }

        public override void leftClickHeld(int x, int y)
        {
            base.leftClickHeld(x, y);

            if (previewOverlay != null)
                return;
        }

        public override void receiveKeyPress(Keys key)
        {
            if (previewOverlay != null)
            {
                previewOverlay.receiveKeyPress(key);
                if (previewOverlay.readyToClose())
                    previewOverlay = null;
                return;
            }

            if (tagPicker.IsOpen)
            {
                tagPicker.HandleKeyPress(key);
                return;
            }

            if (activeSearchDropdown != "" && key != Keys.Escape)
                return;

            if (ModEntry.Config.ArrowKeyScrolling
                && (key == Keys.Up || key == Keys.Down || key == Keys.Left || key == Keys.Right))
            {
                if (IsAnyTriggerDropdownOpen())
                {
                    int amount = key switch
                    {
                        Keys.Up => -1,
                        Keys.Down => 1,
                        Keys.Left => -ScheduleEditDropdownMaxVisible,
                        Keys.Right => ScheduleEditDropdownMaxVisible,
                        _ => 0
                    };
                    if (ApplyOpenDropdownScroll(amount))
                        Game1.playSound("shiny4");
                    return;
                }

                if (setPicker.IsOpen)
                {
                    if (setPicker.HandleKeyPress(key))
                        Game1.playSound("shiny4");
                    return;
                }
            }

            if (key == Keys.Escape)
            {
                if (setPicker.IsOpen)
                {
                    setPicker.Close();
                    Game1.playSound("bigDeSelect");
                    return;
                }

                if (IsAnyDropdownOpen())
                {
                    CloseAllDropdowns();
                    Game1.playSound("bigDeSelect");
                    return;
                }

                CloseOverlay();
                Game1.playSound("bigDeSelect");
                return;
            }

            if (Game1.options.doesInputListContain(Game1.options.menuButton, key))
                return;
        }

        public override void receiveScrollWheelAction(int direction)
        {
            if (previewOverlay != null)
            {
                previewOverlay.receiveScrollWheelAction(direction);
                return;
            }

            if (tagPicker.IsOpen)
            {
                tagPicker.HandleScrollWheel(direction);
                return;
            }

            if (setPicker.IsOpen)
            {
                setPicker.HandleScrollWheel(direction);
                return;
            }

            if (seasonsDropdownOpen)
            {
                ScrollDropdown(ref seasonsScrollIndex, seasonOptionNames.Count, direction);
                RebuildVisibleOptions(uiBuilder.SeasonsDropdownBar, seasonOptionNames, seasonsScrollIndex, visibleSeasonsOptions);
            }
            else if (weatherDropdownOpen)
            {
                ScrollDropdown(ref weatherScrollIndex, weatherOptionNames.Count, direction);
                RebuildVisibleOptions(uiBuilder.WeatherDropdownBar, weatherOptionNames, weatherScrollIndex, visibleWeatherOptions);
            }
            else if (areasDropdownOpen)
            {
                ScrollDropdown(ref areasScrollIndex, areaOptionNames.Count, direction);
                RebuildVisibleOptions(uiBuilder.AreasDropdownBar, areaOptionNames, areasScrollIndex, visibleAreasOptions);
            }
            else if (locationsDropdownOpen)
            {
                ScrollDropdown(ref locationsScrollIndex, filteredLocationOptionNames.Count, direction);
                RebuildVisibleOptions(uiBuilder.LocationsDropdownBar, filteredLocationOptionNames, locationsScrollIndex, visibleLocationsOptions);
            }
            else if (festivalsDropdownOpen)
            {
                ScrollDropdown(ref festivalsScrollIndex, filteredFestivalOptionNames.Count, direction);
                RebuildVisibleOptions(uiBuilder.FestivalsDropdownBar, filteredFestivalOptionNames, festivalsScrollIndex, visibleFestivalsOptions);
            }
        }

        public override void update(GameTime time)
        {
            base.update(time);
            tagPicker.Update();
            previewOverlay?.update(time);

            if (searchTextBox != null && activeSearchDropdown != "")
            {
                searchTextBox.Update();
                searchTextBox.Selected = true;
                if (searchTextBox.Text != searchText)
                {
                    searchText = searchTextBox.Text;
                    RebuildFilteredOptions();
                }
            }

            bool dropdownActive = IsAnyTriggerDropdownOpen();
            setPicker.Update(time, isScrollActive: !dropdownActive && !tagPicker.IsOpen && previewOverlay == null);

            if (ModEntry.Config.ArrowKeyScrolling && dropdownActive && previewOverlay == null && !tagPicker.IsOpen)
            {
                int scrollAmount = scrollHandler.Update(time, ScheduleEditDropdownMaxVisible, out bool shouldPlaySound);
                if (scrollAmount != 0 && ApplyOpenDropdownScroll(scrollAmount) && shouldPlaySound)
                    Game1.playSound("shiny4");
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

            // Draw parent behind
            if (parentMenu is ScheduleMenu scheduleMenu)
                scheduleMenu.IsOverlayBlocking = true;

            parentMenu.draw(b);

            if (parentMenu is ScheduleMenu scheduleMenuAfter)
                scheduleMenuAfter.IsOverlayBlocking = false;

            UIHelpers.SuppressHover = oldSuppressHover;
            if (previewOverlay != null) UIHelpers.SuppressHover = true;

            // Dim overlay
            b.Draw(Game1.fadeToBlackRect,
                new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
                Color.Black * BackgroundOverlayOpacity);

            // Content box
            UIHelpers.DrawTextureBox(b, uiBuilder.ContentBoxBounds.X, uiBuilder.ContentBoxBounds.Y,
                uiBuilder.ContentBoxBounds.Width, uiBuilder.ContentBoxBounds.Height, Color.White);

            int totalOutfits = GetTotalOutfits();
            int remaining = GetRemaining();
            uiBuilder.DrawTotalOutfitsRow(b, totalOutfits, remaining);
            uiBuilder.DrawConditionsHeader(b);

            // Hover suppression behind open dropdown panels
            bool anyDropdownOpen = IsAnyDropdownOpen();
            if (anyDropdownOpen)
            {
                int mx = Game1.getMouseX(), my = Game1.getMouseY();
                if (IsInAnyDropdownPanel(mx, my))
                    UIHelpers.SuppressHover = true;
            }

            // Ordered summary text
            string seasonPlaceholder = $"{TranslationCache.ScheduleEditSeason} ({TranslationCache.ScheduleEditAny})";
            string weatherPlaceholder = $"{TranslationCache.ScheduleEditWeather} ({TranslationCache.ScheduleEditAny})";
            string areaPlaceholder = $"{TranslationCache.ScheduleEditArea} ({TranslationCache.ScheduleEditAny})";
            string locationPlaceholder = $"{TranslationCache.ScheduleEditLocation} ({TranslationCache.ScheduleEditAny})";
            string festivalPlaceholder = $"{TranslationCache.ScheduleEditFestival} ({TranslationCache.ScheduleEditAny})";

            var orderedSeasons = SeasonValues.Where(s => selectedSeasons.Contains(s)).ToList();
            var orderedWeather = weatherEntries.Where(e => selectedWeather.Contains(e.id)).Select(e => e.displayName).ToList();
            var orderedAreas = AreaValues.Where(a => selectedAreas.Contains(a)).Select(GetAreaDisplayName).ToList();
            var orderedLocations = locationEntries.Where(e => selectedLocations.Contains(e.name)).Select(e => e.displayName).ToList();
            var orderedFestivals = festivalEntries.Where(e => selectedFestivals.Contains(e.id)).Select(e => e.displayName).ToList();

            string seasonsSummary = GetSummaryText(orderedSeasons, uiBuilder.SeasonsDropdownBar);
            string weatherSummary = GetSummaryText(orderedWeather, uiBuilder.WeatherDropdownBar);
            string areasSummary = GetSummaryText(orderedAreas, uiBuilder.AreasDropdownBar);
            string locationsSummary = GetSummaryText(orderedLocations, uiBuilder.LocationsDropdownBar);
            string festivalsSummary = GetSummaryText(orderedFestivals, uiBuilder.FestivalsDropdownBar);

            // Conditions row 1: Season | Weather
            UIHelpers.DrawDropdownButton(b, uiBuilder.SeasonsDropdownBar.bounds, seasonsSummary, seasonsDropdownOpen,
                placeholder: seasonPlaceholder, clearButton: uiBuilder.SeasonsClearButton, hasValue: selectedSeasons.Count > 0);
            UIHelpers.DrawDropdownButton(b, uiBuilder.WeatherDropdownBar.bounds, weatherSummary, weatherDropdownOpen,
                placeholder: weatherPlaceholder, clearButton: uiBuilder.WeatherClearButton, hasValue: selectedWeather.Count > 0);

            // Conditions row 2: Area | Location
            UIHelpers.DrawDropdownButton(b, uiBuilder.AreasDropdownBar.bounds, areasSummary, areasDropdownOpen,
                placeholder: areaPlaceholder, clearButton: uiBuilder.AreasClearButton, hasValue: selectedAreas.Count > 0);

            if (locationsDropdownOpen)
                UIHelpers.DrawInputBar(b, uiBuilder.LocationsDropdownBar.bounds, searchText, true, placeholder: locationPlaceholder);
            else
                UIHelpers.DrawDropdownButton(b, uiBuilder.LocationsDropdownBar.bounds, locationsSummary, false,
                    placeholder: locationPlaceholder, clearButton: uiBuilder.LocationsClearButton, hasValue: selectedLocations.Count > 0);

            // Special Events header
            uiBuilder.DrawSpecialEventsHeader(b);

            // Special Events row: Festival | Wedding
            if (festivalsDropdownOpen)
                UIHelpers.DrawInputBar(b, uiBuilder.FestivalsDropdownBar.bounds, searchText, true, placeholder: festivalPlaceholder);
            else
                UIHelpers.DrawDropdownButton(b, uiBuilder.FestivalsDropdownBar.bounds, festivalsSummary, false,
                    placeholder: festivalPlaceholder, clearButton: uiBuilder.FestivalsClearButton, hasValue: selectedFestivals.Count > 0);

            uiBuilder.DrawWeddingRow(b, isWeddingDay);

            // Tags/Sets rows
            uiBuilder.DrawTagsRow(b, tagsSelectAll ? outfitSetStore.GetAllTags().Count : selectedTags.Count, tagPicker.IsOpen);
            uiBuilder.DrawSetsRow(b, includedSetIds.Count, setPicker.IsOpen);

            // Priority (right-aligned label)
            string priorityText = GetPriorityDisplayName(priority);
            string priorityLabel = TranslationCache.ScheduleEditPriority;
            int priorityLabelW = (int)Game1.smallFont.MeasureString(priorityLabel).X;
            int priorityLabelX = uiBuilder.OptionsLabelRightX - priorityLabelW;
            UIHelpers.DrawDropdownButton(b, uiBuilder.PriorityDropdownBar.bounds, priorityText, priorityDropdownOpen,
                label: priorityLabel, labelX: priorityLabelX);

            // Rotate (right-aligned label)
            string rotateText = advanceOnWarp
                ? TranslationCache.ScheduleEditRotateOnLocationChange
                : TranslationCache.ScheduleEditRotateOnceADay;
            string rotateLabel = TranslationCache.ScheduleEditRotate;
            int rotateLabelW = (int)Game1.smallFont.MeasureString(rotateLabel).X;
            int rotateLabelX = uiBuilder.OptionsLabelRightX - rotateLabelW;
            UIHelpers.DrawDropdownButton(b, uiBuilder.RotateDropdownBar.bounds, rotateText, rotateDropdownOpen,
                label: rotateLabel, labelX: rotateLabelX);

            uiBuilder.DrawBottomButtons(b);
            uiBuilder.DrawCloseButton(b);

            // Bar hover tooltip
            string? barTooltip = GetBarHoverTooltip();

            // Draw open dropdown on top
            UIHelpers.SuppressHover = false;
            string? dropdownTooltip = null;
            if (seasonsDropdownOpen)
                dropdownTooltip = UIHelpers.DrawMultiSelectDropdownOptions(b, uiBuilder.SeasonsDropdownBar.bounds,
                    visibleSeasonsOptions, seasonsScrollIndex, ScheduleEditDropdownMaxVisible,
                    isChecked: opt => selectedSeasons.Contains(opt.name), panelPaddingV: ScheduleEditDropdownPanelPadding);
            else if (weatherDropdownOpen)
                dropdownTooltip = UIHelpers.DrawMultiSelectDropdownOptions(b, uiBuilder.WeatherDropdownBar.bounds,
                    visibleWeatherOptions, weatherScrollIndex, ScheduleEditDropdownMaxVisible,
                    isChecked: opt => weatherEntries.Exists(e => e.displayName == opt.name && selectedWeather.Contains(e.id)),
                    panelPaddingV: ScheduleEditDropdownPanelPadding);
            else if (areasDropdownOpen)
                dropdownTooltip = UIHelpers.DrawMultiSelectDropdownOptions(b, uiBuilder.AreasDropdownBar.bounds,
                    visibleAreasOptions, areasScrollIndex, ScheduleEditDropdownMaxVisible,
                    isChecked: opt => { int idx = areaOptionNames.IndexOf(opt.name); return idx >= 0 && selectedAreas.Contains(AreaValues[idx]); },
                    panelPaddingV: ScheduleEditDropdownPanelPadding);
            else if (locationsDropdownOpen)
                dropdownTooltip = UIHelpers.DrawMultiSelectDropdownOptions(b, uiBuilder.LocationsDropdownBar.bounds,
                    visibleLocationsOptions, locationsScrollIndex, ScheduleEditDropdownMaxVisible,
                    isChecked: opt => filteredLocationEntries.Exists(e => e.displayName == opt.name && selectedLocations.Contains(e.name)),
                    panelPaddingV: ScheduleEditDropdownPanelPadding);
            else if (festivalsDropdownOpen)
                dropdownTooltip = UIHelpers.DrawMultiSelectDropdownOptions(b, uiBuilder.FestivalsDropdownBar.bounds,
                    visibleFestivalsOptions, festivalsScrollIndex, ScheduleEditDropdownMaxVisible,
                    isChecked: opt => filteredFestivalEntries.Exists(e => e.displayName == opt.name && selectedFestivals.Contains(e.id)),
                    panelPaddingV: ScheduleEditDropdownPanelPadding);
            else if (priorityDropdownOpen)
                UIHelpers.DrawDropdownOptions(b, uiBuilder.PriorityDropdownBar.bounds, priorityOptions, 0, priorityOptions.Count,
                    isSelected: opt => opt.name == priorityText, panelPaddingV: ScheduleEditDropdownPanelPadding);
            else if (rotateDropdownOpen)
                UIHelpers.DrawDropdownOptions(b, uiBuilder.RotateDropdownBar.bounds, rotateOptions, 0, rotateOptions.Count,
                    isSelected: opt => opt.name == rotateText, panelPaddingV: ScheduleEditDropdownPanelPadding);

            // Tooltip (dropdown takes priority over bar hover)
            string? tooltip = dropdownTooltip ?? barTooltip;
            if (tooltip != null && ModEntry.Config.ShowTooltip)
            {
                UIHelpers.DrawWrappedTooltip(b, tooltip);
            }

            // Draw side panels
            tagPicker.Draw(b);
            setPicker.Draw(b);

            UIHelpers.SuppressHover = false;
            if (previewOverlay != null)
                previewOverlay.draw(b);
            else
                drawMouse(b);
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

            RebuildAllOpenDropdowns();
            if (priorityDropdownOpen)
                uiBuilder.BuildPriorityOptions(priorityOptions);
            if (rotateDropdownOpen)
                uiBuilder.BuildRotateOptions(rotateOptions);

            if (tagPicker.IsOpen)
                tagPicker.UpdateParentBounds(uiBuilder.ContentBoxBounds);
            if (setPicker.IsOpen)
                setPicker.UpdateParentBounds(uiBuilder.ContentBoxBounds);

            previewOverlay?.gameWindowSizeChanged(oldBounds, newBounds);
        }

        // --- Summary text ---

        private string GetSummaryText(List<string> displayNames, ClickableComponent bar)
        {
            if (displayNames.Count == 0) return "";
            int maxWidth = bar.bounds.Width - 40 - ClearButtonSize;
            return UIHelpers.FormatTagsWithCount(displayNames, "", maxWidth,
                s => Game1.smallFont.MeasureString(s).X, "");
        }

        private string GetWeatherDisplayName(string value)
        {
            var entry = weatherEntries.FirstOrDefault(e => e.id.Equals(value, StringComparison.OrdinalIgnoreCase));
            return entry.displayName ?? value;
        }

        private string GetAreaDisplayName(string value)
        {
            if (value.Equals("Outdoor", StringComparison.OrdinalIgnoreCase)) return TranslationCache.ScheduleEditAreaOutdoor;
            if (value.Equals("Indoor", StringComparison.OrdinalIgnoreCase)) return TranslationCache.ScheduleEditAreaIndoor;
            return value;
        }

        private List<string> GetSelectedLocationNames()
        {
            return locationEntries
                .Where(e => selectedLocations.Contains(e.name))
                .Select(e => e.displayName)
                .ToList();
        }

        private List<string> GetSelectedFestivalNames()
        {
            return festivalEntries
                .Where(e => selectedFestivals.Contains(e.id))
                .Select(e => e.displayName)
                .ToList();
        }

        // --- Bar hover tooltip ---

        private string? GetBarHoverTooltip()
        {
            if (IsAnyDropdownOpen()) return null;
            int mx = Game1.getMouseX(), my = Game1.getMouseY();

            if (uiBuilder.SeasonsDropdownBar.containsPoint(mx, my) && selectedSeasons.Count > 1)
                return string.Join(", ", selectedSeasons);
            if (uiBuilder.WeatherDropdownBar.containsPoint(mx, my) && selectedWeather.Count > 1)
                return string.Join(", ", selectedWeather.Select(GetWeatherDisplayName));
            if (uiBuilder.AreasDropdownBar.containsPoint(mx, my) && selectedAreas.Count > 1)
                return string.Join(", ", selectedAreas.Select(GetAreaDisplayName));
            if (uiBuilder.LocationsDropdownBar.containsPoint(mx, my) && selectedLocations.Count > 1)
                return string.Join(", ", GetSelectedLocationNames());
            if (uiBuilder.FestivalsDropdownBar.containsPoint(mx, my) && selectedFestivals.Count > 1)
                return string.Join(", ", GetSelectedFestivalNames());

            return null;
        }

        // --- Clear button handling ---

        private bool HandleClearButtonClick(int x, int y, bool playSound)
        {
            if (selectedSeasons.Count > 0 && uiBuilder.SeasonsClearButton.containsPoint(x, y))
            {
                selectedSeasons.Clear();
                if (playSound) Game1.playSound("coin");
                return true;
            }
            if (selectedWeather.Count > 0 && uiBuilder.WeatherClearButton.containsPoint(x, y))
            {
                selectedWeather.Clear();
                if (playSound) Game1.playSound("coin");
                return true;
            }
            if (selectedAreas.Count > 0 && uiBuilder.AreasClearButton.containsPoint(x, y))
            {
                selectedAreas.Clear();
                if (playSound) Game1.playSound("coin");
                return true;
            }
            if (selectedLocations.Count > 0 && uiBuilder.LocationsClearButton.containsPoint(x, y))
            {
                selectedLocations.Clear();
                if (playSound) Game1.playSound("coin");
                return true;
            }
            if (selectedFestivals.Count > 0 && uiBuilder.FestivalsClearButton.containsPoint(x, y))
            {
                selectedFestivals.Clear();
                if (playSound) Game1.playSound("coin");
                return true;
            }
            return false;
        }

        // --- Dropdown bar click handling ---

        private bool HandleDropdownBarClick(int x, int y, bool playSound)
        {
            if (TryToggleDropdown(uiBuilder.SeasonsDropdownBar, ref seasonsDropdownOpen, x, y, playSound,
                () => { seasonsScrollIndex = 0; RebuildVisibleOptions(uiBuilder.SeasonsDropdownBar, seasonOptionNames, seasonsScrollIndex, visibleSeasonsOptions); }))
                return true;

            if (TryToggleDropdown(uiBuilder.WeatherDropdownBar, ref weatherDropdownOpen, x, y, playSound,
                () => { weatherScrollIndex = 0; RebuildVisibleOptions(uiBuilder.WeatherDropdownBar, weatherOptionNames, weatherScrollIndex, visibleWeatherOptions); }))
                return true;

            if (TryToggleDropdown(uiBuilder.AreasDropdownBar, ref areasDropdownOpen, x, y, playSound,
                () => { areasScrollIndex = 0; RebuildVisibleOptions(uiBuilder.AreasDropdownBar, areaOptionNames, areasScrollIndex, visibleAreasOptions); }))
                return true;

            if (locationsDropdownOpen && uiBuilder.LocationsDropdownBar.containsPoint(x, y))
                return true;
            if (TryToggleDropdown(uiBuilder.LocationsDropdownBar, ref locationsDropdownOpen, x, y, playSound,
                () => { OpenSearchDropdown("locations"); }))
                return true;

            if (festivalsDropdownOpen && uiBuilder.FestivalsDropdownBar.containsPoint(x, y))
                return true;
            if (TryToggleDropdown(uiBuilder.FestivalsDropdownBar, ref festivalsDropdownOpen, x, y, playSound,
                () => { OpenSearchDropdown("festivals"); }))
                return true;

            if (TryToggleDropdown(uiBuilder.PriorityDropdownBar, ref priorityDropdownOpen, x, y, playSound,
                () => uiBuilder.BuildPriorityOptions(priorityOptions)))
                return true;

            if (TryToggleDropdown(uiBuilder.RotateDropdownBar, ref rotateDropdownOpen, x, y, playSound,
                () => uiBuilder.BuildRotateOptions(rotateOptions)))
                return true;

            return false;
        }

        private bool TryToggleDropdown(ClickableComponent bar, ref bool isOpen, int x, int y, bool playSound, Action onOpen)
        {
            if (!bar.containsPoint(x, y))
                return false;

            bool wasOpen = isOpen;
            CloseAllDropdowns();
            if (!wasOpen)
            {
                isOpen = true;
                onOpen();
            }
            if (playSound) Game1.playSound("shwip");
            return true;
        }

        // --- Dropdown option click handling ---

        private bool TryHandleDropdownOptionClick(int x, int y, bool playSound)
        {
            if (seasonsDropdownOpen)
                return TryClickOption(visibleSeasonsOptions, seasonsScrollIndex, HandleSeasonOptionClick, x, y, playSound);
            if (weatherDropdownOpen)
                return TryClickOption(visibleWeatherOptions, weatherScrollIndex, HandleWeatherOptionClick, x, y, playSound);
            if (areasDropdownOpen)
                return TryClickOption(visibleAreasOptions, areasScrollIndex, HandleAreaOptionClick, x, y, playSound);
            if (locationsDropdownOpen)
                return TryClickOption(visibleLocationsOptions, locationsScrollIndex, HandleLocationOptionClick, x, y, playSound);
            if (festivalsDropdownOpen)
                return TryClickOption(visibleFestivalsOptions, festivalsScrollIndex, HandleFestivalOptionClick, x, y, playSound);
            if (priorityDropdownOpen)
            {
                for (int i = 0; i < priorityOptions.Count; i++)
                {
                    if (priorityOptions[i].containsPoint(x, y))
                    {
                        priority = 3 - i; // 0→High(3), 1→Medium(2), 2→Low(1)
                        priorityDropdownOpen = false;
                        if (playSound) Game1.playSound("drumkit6");
                        return true;
                    }
                }
                return false;
            }
            if (rotateDropdownOpen)
            {
                for (int i = 0; i < rotateOptions.Count; i++)
                {
                    if (rotateOptions[i].containsPoint(x, y))
                    {
                        advanceOnWarp = (i == 0);
                        rotateDropdownOpen = false;
                        if (playSound) Game1.playSound("drumkit6");
                        return true;
                    }
                }
                return false;
            }

            return false;
        }

        private bool TryClickOption(List<ClickableComponent> visibleOptions, int scrollIndex, Action<int> handler, int x, int y, bool playSound)
        {
            for (int i = 0; i < visibleOptions.Count; i++)
            {
                if (visibleOptions[i].containsPoint(x, y))
                {
                    handler(scrollIndex + i);
                    if (playSound) Game1.playSound("drumkit6");
                    return true;
                }
            }
            return false;
        }

        private void HandleSeasonOptionClick(int dataIndex)
        {
            string season = SeasonValues[dataIndex];
            if (selectedSeasons.Contains(season))
                selectedSeasons.Remove(season);
            else
                selectedSeasons.Add(season);
        }

        private void HandleWeatherOptionClick(int dataIndex)
        {
            string weather = weatherEntries[dataIndex].id;
            if (selectedWeather.Contains(weather))
                selectedWeather.Remove(weather);
            else
                selectedWeather.Add(weather);
        }

        private void HandleAreaOptionClick(int dataIndex)
        {
            string area = AreaValues[dataIndex];
            if (selectedAreas.Contains(area))
                selectedAreas.Remove(area);
            else
                selectedAreas.Add(area);
        }

        private void HandleLocationOptionClick(int dataIndex)
        {
            string location = filteredLocationEntries[dataIndex].name;
            if (selectedLocations.Contains(location))
                selectedLocations.Remove(location);
            else
                selectedLocations.Add(location);
        }

        private void HandleFestivalOptionClick(int dataIndex)
        {
            string festivalId = filteredFestivalEntries[dataIndex].id;
            if (selectedFestivals.Contains(festivalId))
                selectedFestivals.Remove(festivalId);
            else
                selectedFestivals.Add(festivalId);
        }

        // --- Dropdown helpers ---

        private bool IsAnyDropdownOpen()
        {
            return seasonsDropdownOpen || weatherDropdownOpen || areasDropdownOpen
                || locationsDropdownOpen || festivalsDropdownOpen || priorityDropdownOpen || rotateDropdownOpen;
        }

        private bool IsAnyTriggerDropdownOpen()
        {
            return seasonsDropdownOpen || weatherDropdownOpen || areasDropdownOpen
                || locationsDropdownOpen || festivalsDropdownOpen;
        }

        private bool ApplyOpenDropdownScroll(int amount)
        {
            if (seasonsDropdownOpen)
                return ApplyDropdownScroll(ref seasonsScrollIndex, seasonOptionNames.Count, amount,
                    () => RebuildVisibleOptions(uiBuilder.SeasonsDropdownBar, seasonOptionNames, seasonsScrollIndex, visibleSeasonsOptions));
            if (weatherDropdownOpen)
                return ApplyDropdownScroll(ref weatherScrollIndex, weatherOptionNames.Count, amount,
                    () => RebuildVisibleOptions(uiBuilder.WeatherDropdownBar, weatherOptionNames, weatherScrollIndex, visibleWeatherOptions));
            if (areasDropdownOpen)
                return ApplyDropdownScroll(ref areasScrollIndex, areaOptionNames.Count, amount,
                    () => RebuildVisibleOptions(uiBuilder.AreasDropdownBar, areaOptionNames, areasScrollIndex, visibleAreasOptions));
            if (locationsDropdownOpen)
                return ApplyDropdownScroll(ref locationsScrollIndex, filteredLocationOptionNames.Count, amount,
                    () => RebuildVisibleOptions(uiBuilder.LocationsDropdownBar, filteredLocationOptionNames, locationsScrollIndex, visibleLocationsOptions));
            if (festivalsDropdownOpen)
                return ApplyDropdownScroll(ref festivalsScrollIndex, filteredFestivalOptionNames.Count, amount,
                    () => RebuildVisibleOptions(uiBuilder.FestivalsDropdownBar, filteredFestivalOptionNames, festivalsScrollIndex, visibleFestivalsOptions));
            return false;
        }

        private bool ApplyDropdownScroll(ref int scrollIndex, int totalCount, int amount, Action rebuild)
        {
            int maxScroll = Math.Max(0, totalCount - ScheduleEditDropdownMaxVisible);
            int newIndex = Math.Clamp(scrollIndex + amount, 0, maxScroll);
            if (newIndex == scrollIndex)
                return false;
            scrollIndex = newIndex;
            rebuild();
            return true;
        }

        private void CloseAllDropdowns()
        {
            seasonsDropdownOpen = false;
            weatherDropdownOpen = false;
            areasDropdownOpen = false;
            locationsDropdownOpen = false;
            festivalsDropdownOpen = false;
            priorityDropdownOpen = false;
            rotateDropdownOpen = false;

            if (activeSearchDropdown != "")
            {
                activeSearchDropdown = "";
                searchText = "";
                searchTextBox = null;
                Game1.keyboardDispatcher.Subscriber = null;
            }
        }

        private void OpenSearchDropdown(string dropdown)
        {
            searchText = "";
            searchTextBox = new TextBox(null, null, Game1.smallFont, Game1.textColor);
            searchTextBox.Text = "";
            searchTextBox.Selected = true;
            Game1.keyboardDispatcher.Subscriber = searchTextBox;
            activeSearchDropdown = dropdown;
            RebuildFilteredOptions();
        }

        private void RebuildFilteredOptions()
        {
            if (activeSearchDropdown == "festivals")
            {
                filteredFestivalEntries = string.IsNullOrEmpty(searchText)
                    ? new List<(string, string)>(festivalEntries)
                    : festivalEntries.Where(e => e.displayName.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
                filteredFestivalOptionNames = filteredFestivalEntries.Select(e => e.displayName).ToList();
                festivalsScrollIndex = 0;
                RebuildVisibleOptions(uiBuilder.FestivalsDropdownBar, filteredFestivalOptionNames, festivalsScrollIndex, visibleFestivalsOptions);
            }
            else if (activeSearchDropdown == "locations")
            {
                filteredLocationEntries = string.IsNullOrEmpty(searchText)
                    ? new List<(string, string)>(locationEntries)
                    : locationEntries.Where(e => e.displayName.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
                filteredLocationOptionNames = filteredLocationEntries.Select(e => e.displayName).ToList();
                locationsScrollIndex = 0;
                RebuildVisibleOptions(uiBuilder.LocationsDropdownBar, filteredLocationOptionNames, locationsScrollIndex, visibleLocationsOptions);
            }
        }

        private bool IsInAnyDropdownPanel(int x, int y)
        {
            if (seasonsDropdownOpen && uiBuilder.GetDropdownPanelBounds(uiBuilder.SeasonsDropdownBar, visibleSeasonsOptions.Count).Contains(x, y))
                return true;
            if (weatherDropdownOpen && uiBuilder.GetDropdownPanelBounds(uiBuilder.WeatherDropdownBar, visibleWeatherOptions.Count).Contains(x, y))
                return true;
            if (areasDropdownOpen && uiBuilder.GetDropdownPanelBounds(uiBuilder.AreasDropdownBar, visibleAreasOptions.Count).Contains(x, y))
                return true;
            if (locationsDropdownOpen && uiBuilder.GetDropdownPanelBounds(uiBuilder.LocationsDropdownBar, visibleLocationsOptions.Count).Contains(x, y))
                return true;
            if (festivalsDropdownOpen && uiBuilder.GetDropdownPanelBounds(uiBuilder.FestivalsDropdownBar, visibleFestivalsOptions.Count).Contains(x, y))
                return true;
            if (priorityDropdownOpen && uiBuilder.GetPriorityDropdownPanelBounds(priorityOptions.Count).Contains(x, y))
                return true;
            if (rotateDropdownOpen && uiBuilder.GetRotateDropdownPanelBounds(rotateOptions.Count).Contains(x, y))
                return true;
            return false;
        }

        private void ScrollDropdown(ref int scrollIndex, int totalCount, int direction)
        {
            int maxVisible = ScheduleEditDropdownMaxVisible;
            int maxScroll = Math.Max(0, totalCount - maxVisible);

            if (direction > 0)
                scrollIndex = Math.Max(0, scrollIndex - 1);
            else if (direction < 0)
                scrollIndex = Math.Min(maxScroll, scrollIndex + 1);
        }

        private void RebuildVisibleOptions(ClickableComponent bar, List<string> names, int scrollIndex, List<ClickableComponent> outOptions)
        {
            uiBuilder.BuildDropdownOptions(bar, names, scrollIndex, outOptions);
        }

        private void RebuildAllOpenDropdowns()
        {
            if (seasonsDropdownOpen) RebuildVisibleOptions(uiBuilder.SeasonsDropdownBar, seasonOptionNames, seasonsScrollIndex, visibleSeasonsOptions);
            if (weatherDropdownOpen) RebuildVisibleOptions(uiBuilder.WeatherDropdownBar, weatherOptionNames, weatherScrollIndex, visibleWeatherOptions);
            if (areasDropdownOpen) RebuildVisibleOptions(uiBuilder.AreasDropdownBar, areaOptionNames, areasScrollIndex, visibleAreasOptions);
            if (locationsDropdownOpen) RebuildVisibleOptions(uiBuilder.LocationsDropdownBar, filteredLocationOptionNames, locationsScrollIndex, visibleLocationsOptions);
            if (festivalsDropdownOpen) RebuildVisibleOptions(uiBuilder.FestivalsDropdownBar, filteredFestivalOptionNames, festivalsScrollIndex, visibleFestivalsOptions);
        }

        // --- Bottom buttons ---

        private bool HandleBottomButtonClick(int x, int y, bool playSound)
        {
            if (uiBuilder.PreviewButton.containsPoint(x, y))
            {
                OpenPreviewOverlay();
                if (playSound) Game1.playSound("bigSelect");
                return true;
            }

            if (uiBuilder.CancelButton.containsPoint(x, y))
            {
                CloseOverlay();
                if (playSound) Game1.playSound("bigDeSelect");
                return true;
            }

            return false;
        }

        // --- Save (called from SetPreviewOverlay in Phase 5) ---

        private void HandleSave()
        {
            ScheduleRule rule;
            if (editingRule != null)
            {
                rule = editingRule;
            }
            else
            {
                rule = new ScheduleRule();
                rule.IsEnabled = true;
            }

            rule.Name = GenerateRuleName();
            rule.SeasonsSelectAll = false;
            rule.SelectedSeasons = new List<string>(selectedSeasons);
            rule.WeatherSelectAll = false;
            rule.SelectedWeather = new List<string>(selectedWeather);
            rule.AreasSelectAll = false;
            rule.SelectedAreas = new List<string>(selectedAreas);
            rule.LocationsSelectAll = false;
            rule.SelectedLocations = new List<string>(selectedLocations);
            rule.FestivalsSelectAll = false;
            rule.SelectedFestivals = new List<string>(selectedFestivals);
            rule.IsWeddingDay = isWeddingDay;
            rule.TagsSelectAll = tagsSelectAll;
            rule.SelectedTags = new List<string>(selectedTags);
            rule.IncludedSetIds = new List<string>(includedSetIds);
            rule.ExcludedSetIds = new List<string>(excludedSetIds);
            rule.Priority = priority;
            rule.AdvanceOnWarp = advanceOnWarp;

            if (editingRule != null)
            {
                scheduleStore.UpdateRule(rule);
            }
            else
            {
                scheduleStore.AddRule(rule);
            }

            onSaveComplete?.Invoke();
            CloseOverlay();
        }

        private static string GetPriorityDisplayName(int priority)
        {
            if (priority >= 3) return TranslationCache.ScheduleEditPriorityHigh;
            if (priority <= 1) return TranslationCache.ScheduleEditPriorityLow;
            return TranslationCache.ScheduleEditPriorityMedium;
        }

        private string GenerateRuleName()
        {
            return UIHelpers.GenerateRuleName(
                selectedSeasons,
                selectedWeather.Select(GetWeatherDisplayName),
                selectedAreas.Select(GetAreaDisplayName),
                GetSelectedLocationNames(),
                GetSelectedFestivalNames(),
                isWeddingDay,
                TranslationCache.ScheduleEditWedding);
        }

        // --- Total outfits count ---

        private void InvalidatePoolCache()
        {
            cachedTotalOutfits = null;
            cachedRemaining = null;
        }

        private int GetTotalOutfits()
        {
            if (cachedTotalOutfits == null)
            {
                var tempRule = new ScheduleRule
                {
                    TagsSelectAll = tagsSelectAll,
                    SelectedTags = new List<string>(selectedTags),
                    IncludedSetIds = new List<string>(includedSetIds),
                    ExcludedSetIds = new List<string>(excludedSetIds)
                };
                cachedTotalOutfits = ScheduleEngine.ResolvePool(tempRule, outfitSetStore.GetAllSets()).Count;
            }
            return cachedTotalOutfits.Value;
        }

        private int GetRemaining()
        {
            if (cachedRemaining == null)
            {
                int total = GetTotalOutfits();
                if (editingRule == null)
                {
                    cachedRemaining = total;
                }
                else
                {
                    var state = scheduleStore.GetRotationState(editingRule.Id);
                    if (state == null)
                    {
                        cachedRemaining = total;
                    }
                    else
                    {
                        var poolIds = new HashSet<string>(
                            ScheduleEngine.ResolvePool(
                                new ScheduleRule
                                {
                                    TagsSelectAll = tagsSelectAll,
                                    SelectedTags = new List<string>(selectedTags),
                                    IncludedSetIds = new List<string>(includedSetIds),
                                    ExcludedSetIds = new List<string>(excludedSetIds)
                                },
                                outfitSetStore.GetAllSets()
                            ).Select(s => s.Id)
                        );
                        cachedRemaining = state.Queue.Count(id => poolIds.Contains(id));
                    }
                }
            }
            return cachedRemaining.Value;
        }

        // --- Tag/Set picker ---

        private void OpenTagPicker()
        {
            CloseAllDropdowns();
            setPicker.Close();
            tagPicker.Open(
                uiBuilder.ContentBoxBounds,
                selectedTags,
                tagsSelectAll,
                tags =>
                {
                    selectedTags = new HashSet<string>(tags, TranslationCache.TagComparer);
                    InvalidatePoolCache();
                    if (setPicker.IsOpen)
                        setPicker.UpdateState(selectedTags, tagsSelectAll, includedSetIds, excludedSetIds);
                },
                allSelected =>
                {
                    tagsSelectAll = allSelected;
                    InvalidatePoolCache();
                    if (setPicker.IsOpen)
                        setPicker.UpdateState(selectedTags, tagsSelectAll, includedSetIds, excludedSetIds);
                },
                openToLeft: true
            );
        }

        private void OpenSetPicker()
        {
            CloseAllDropdowns();
            tagPicker.Close();
            setPicker.Open(
                uiBuilder.ContentBoxBounds,
                selectedTags,
                tagsSelectAll,
                includedSetIds,
                excludedSetIds
            );
        }

        private void OnSetPickerChanged()
        {
            InvalidatePoolCache();
        }

        private bool IsInAnyPicker(int x, int y)
        {
            if (tagPicker.IsOpen && tagPicker.Bounds.Contains(x, y))
                return true;
            if (setPicker.IsOpen && setPicker.Bounds.Contains(x, y))
                return true;
            return false;
        }

        // --- Preview overlay ---

        private void OpenPreviewOverlay()
        {
            CloseAllDropdowns();
            tagPicker.Close();
            setPicker.Close();

            var triggerLines = BuildTriggerLines();

            previewOverlay = new SetPreviewOverlay(
                outfitSetStore,
                scheduleStore,
                editingRule,
                new List<string>(selectedTags),
                tagsSelectAll,
                new List<string>(excludedSetIds),
                newExcluded => { excludedSetIds = new List<string>(newExcluded); InvalidatePoolCache(); },
                HandleSave,
                triggerLines
            );
        }

        private List<string> BuildTriggerLines()
        {
            var lines = new List<string>();

            if (selectedSeasons.Count > 0)
                lines.Add(string.Join(", ", selectedSeasons));
            if (selectedWeather.Count > 0)
                lines.Add(string.Join(", ", selectedWeather.Select(GetWeatherDisplayName)));
            if (selectedAreas.Count > 0)
                lines.Add(string.Join(", ", selectedAreas.Select(GetAreaDisplayName)));
            if (selectedLocations.Count > 0)
                lines.Add(string.Join(", ", GetSelectedLocationNames()));
            if (selectedFestivals.Count > 0)
                lines.Add(string.Join(", ", GetSelectedFestivalNames()));
            if (isWeddingDay)
                lines.Add(TranslationCache.ScheduleEditWedding);

            return lines;
        }

        // --- Close ---

        private void CloseOverlay()
        {
            tagPicker.Close();
            setPicker.Close();
            Game1.activeClickableMenu = parentMenu;
        }
    }
}
