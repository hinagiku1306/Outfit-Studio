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

        private bool IsSpecialEventMode => selectedFestivals.Count > 0 || isWeddingDay;

        // State - other
        private List<string> selectedSetIds = new();
        private readonly List<string>? originalSetIds;
        private int priority;
        private bool advanceOnWarp;

        // Priority dropdown
        private bool priorityDropdownOpen;
        private List<ClickableComponent> priorityOptions = new();

        // Rotation dropdown
        private bool rotationDropdownOpen;
        private List<ClickableComponent> rotationOptions = new();

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

        // Child overlay
        private ScheduleOutfitOverlay? previewOverlay;

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

            uiBuilder = new ScheduleEditUIBuilder { IsEditing = IsEditing };

            LoadDropdownData();

            priority = ModEntry.Config.DefaultPriority;
            advanceOnWarp = ModEntry.Config.DefaultAdvanceOnWarp;

            if (editingRule != null)
            {
                selectedSeasons = new HashSet<string>(editingRule.SelectedSeasons, StringComparer.OrdinalIgnoreCase);
                selectedWeather = new HashSet<string>(editingRule.SelectedWeather, StringComparer.OrdinalIgnoreCase);
                selectedAreas = new HashSet<string>(editingRule.SelectedAreas, StringComparer.OrdinalIgnoreCase);
                selectedLocations = new HashSet<string>(editingRule.SelectedLocations, StringComparer.OrdinalIgnoreCase);
                selectedFestivals = new HashSet<string>(editingRule.SelectedFestivals, StringComparer.OrdinalIgnoreCase);
                isWeddingDay = editingRule.IsWeddingDay;
                if (IsSpecialEventMode)
                {
                    selectedSeasons.Clear();
                    selectedWeather.Clear();
                    selectedAreas.Clear();
                    selectedLocations.Clear();
                }
                selectedSetIds = new List<string>(editingRule.SelectedSetIds);
                originalSetIds = new List<string>(editingRule.SelectedSetIds);
                priority = editingRule.Priority;
                advanceOnWarp = editingRule.AdvanceOnWarp;
            }

            Game1.keyboardDispatcher.Subscriber = null;

            uiBuilder.Recalculate();
            uiBuilder.BuildPriorityOptions(priorityOptions);
            uiBuilder.BuildRotationOptions(rotationOptions);
            width = uiBuilder.Width;
            height = uiBuilder.Height;
            xPositionOnScreen = uiBuilder.X;
            yPositionOnScreen = uiBuilder.Y;

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
                        string display = UIHelpers.ResolveDisplayName(loc.DisplayName, loc.Name);
                        locationEntries.Add((loc.Name, display));
                    }

                    // Disambiguate locations sharing the same display name (e.g. Farm / FarmHouse)
                    var displayCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    foreach (var entry in locationEntries)
                    {
                        displayCounts.TryGetValue(entry.displayName, out int count);
                        displayCounts[entry.displayName] = count + 1;
                    }
                    for (int i = 0; i < locationEntries.Count; i++)
                    {
                        var (name, display) = locationEntries[i];
                        if (displayCounts.TryGetValue(display, out int count) && count > 1)
                            locationEntries[i] = (name, $"{display} ({name})");
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
                    if (string.IsNullOrEmpty(name))
                        continue;
                    string display = UIHelpers.ResolveDisplayName(TokenParser.ParseText(name), name);
                    festivalEntries.Add((key, display));
                }

                var passiveFestivals = DataLoader.PassiveFestivals(Game1.content);
                foreach (var (key, data) in passiveFestivals)
                {
                    string display = UIHelpers.ResolveDisplayName(TokenParser.ParseText(data.DisplayName), key);
                    festivalEntries.Add((key, display));
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
                    ClosePreviewOverlay();
                return;
            }

            // Close X button
            if (uiBuilder.CloseButton.containsPoint(x, y))
            {
                CloseOverlay();
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            // Click outside
            if (!isWithinBounds(x, y) && !IsInAnyDropdownPanel(x, y) && ModEntry.Config.CloseOnClickOutside)
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
                if (IsSpecialEventMode) ClearConditions();
                InvalidatePoolCache();
                if (playSound) Game1.playSound("drumkit6");
                return;
            }

            // Bottom buttons
            HandleBottomButtonClick(x, y, playSound);
        }

        private bool IsInWeddingRow(int x, int y)
        {
            return y >= uiBuilder.WeddingCheckbox.bounds.Y
                && y < uiBuilder.WeddingCheckbox.bounds.Y + ScheduleCheckboxSize
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
                    ClosePreviewOverlay();
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

            }

            if (key == Keys.Escape)
            {
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

            if (ModEntry.Config.ArrowKeyScrolling && dropdownActive && previewOverlay == null)
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

            {
                int totalOutfits = selectedSetIds.Count;
                int remaining = IsEditing ? GetRemaining() : 0;
                uiBuilder.DrawTotalOutfitsRow(b, totalOutfits, remaining, showRemaining: IsEditing);
            }
            string? nameTooltip = uiBuilder.DrawNameRow(b, GenerateRuleName());

            float conditionsOpacity = IsSpecialEventMode ? ScheduleEditInactiveOpacity : 1f;
            uiBuilder.DrawConditionsHeader(b, conditionsOpacity);

            // Hover suppression behind open dropdown panels
            bool anyDropdownOpen = IsAnyDropdownOpen();
            if (anyDropdownOpen)
            {
                int mx = Game1.getMouseX(), my = Game1.getMouseY();
                if (IsInAnyDropdownPanel(mx, my))
                    UIHelpers.SuppressHover = true;
            }

            // Ordered summary text (show first matching option in dropdown order)
            string seasonPlaceholder = TranslationCache.ScheduleEditSeason;
            string weatherPlaceholder = TranslationCache.ScheduleEditWeather;
            string areaPlaceholder = TranslationCache.ScheduleEditArea;
            string locationPlaceholder = TranslationCache.ScheduleEditLocation;
            string festivalPlaceholder = TranslationCache.ScheduleEditFestival;

            int seasonMaxW = uiBuilder.SeasonsDropdownBar.bounds.Width - FilterTextPadding * 2 - ClearButtonSize - ClearButtonRightMargin;
            string seasonFirst = SeasonValues.FirstOrDefault(s => selectedSeasons.Contains(s)) ?? "";
            string seasonsSummary = UIHelpers.BuildCountSummary(seasonFirst, selectedSeasons.Count, seasonMaxW);

            int weatherMaxW = uiBuilder.WeatherDropdownBar.bounds.Width - FilterTextPadding * 2 - ClearButtonSize - ClearButtonRightMargin;
            var weatherMatch = weatherEntries.FirstOrDefault(e => selectedWeather.Contains(e.id));
            string weatherSummary = UIHelpers.BuildCountSummary(weatherMatch.displayName ?? "", selectedWeather.Count, weatherMaxW);

            int areaMaxW = uiBuilder.AreasDropdownBar.bounds.Width - FilterTextPadding * 2 - ClearButtonSize - ClearButtonRightMargin;
            int areaIdx = Array.FindIndex(AreaValues, a => selectedAreas.Contains(a));
            string areaFirst = areaIdx >= 0 ? areaOptionNames[areaIdx] : "";
            string areasSummary = UIHelpers.BuildCountSummary(areaFirst, selectedAreas.Count, areaMaxW);

            int locationMaxW = uiBuilder.LocationsDropdownBar.bounds.Width - FilterTextPadding * 2 - ClearButtonSize - ClearButtonRightMargin;
            var locationMatch = locationEntries.FirstOrDefault(e => selectedLocations.Contains(e.name));
            string locationsSummary = UIHelpers.BuildCountSummary(locationMatch.displayName ?? "", selectedLocations.Count, locationMaxW);

            int festivalMaxW = uiBuilder.FestivalsDropdownBar.bounds.Width - FilterTextPadding * 2 - ClearButtonSize - ClearButtonRightMargin;
            var festivalMatch = festivalEntries.FirstOrDefault(e => selectedFestivals.Contains(e.id));
            string festivalsSummary = UIHelpers.BuildCountSummary(festivalMatch.displayName ?? "", selectedFestivals.Count, festivalMaxW);

            // Conditions row 1: Season | Weather | Area
            UIHelpers.DrawDropdownButton(b, uiBuilder.SeasonsDropdownBar.bounds, seasonsSummary, seasonsDropdownOpen,
                placeholder: seasonPlaceholder, clearButton: uiBuilder.SeasonsClearButton, hasValue: selectedSeasons.Count > 0,
                shadowOffset: 3, shadowOpacity: 0.2f, opacity: conditionsOpacity);
            UIHelpers.DrawDropdownButton(b, uiBuilder.WeatherDropdownBar.bounds, weatherSummary, weatherDropdownOpen,
                placeholder: weatherPlaceholder, clearButton: uiBuilder.WeatherClearButton, hasValue: selectedWeather.Count > 0,
                shadowOffset: 3, shadowOpacity: 0.2f, opacity: conditionsOpacity);
            UIHelpers.DrawDropdownButton(b, uiBuilder.AreasDropdownBar.bounds, areasSummary, areasDropdownOpen,
                placeholder: areaPlaceholder, clearButton: uiBuilder.AreasClearButton, hasValue: selectedAreas.Count > 0,
                shadowOffset: 3, shadowOpacity: 0.2f, opacity: conditionsOpacity);

            // Conditions row 2: Location
            if (locationsDropdownOpen)
                UIHelpers.DrawInputBar(b, uiBuilder.LocationsDropdownBar.bounds, searchText, true, placeholder: locationPlaceholder,
                    shadowOffset: 3, shadowOpacity: 0.2f);
            else
                UIHelpers.DrawDropdownButton(b, uiBuilder.LocationsDropdownBar.bounds, locationsSummary, false,
                    placeholder: locationPlaceholder, clearButton: uiBuilder.LocationsClearButton, hasValue: selectedLocations.Count > 0,
                    shadowOffset: 3, shadowOpacity: 0.2f, opacity: conditionsOpacity);

            // Special Events header
            uiBuilder.DrawSpecialEventsHeader(b);

            // Wedding row
            uiBuilder.DrawWeddingRow(b, isWeddingDay);

            // Festival row
            if (festivalsDropdownOpen)
                UIHelpers.DrawInputBar(b, uiBuilder.FestivalsDropdownBar.bounds, searchText, true, placeholder: festivalPlaceholder,
                    shadowOffset: 3, shadowOpacity: 0.2f);
            else
                UIHelpers.DrawDropdownButton(b, uiBuilder.FestivalsDropdownBar.bounds, festivalsSummary, false,
                    placeholder: festivalPlaceholder, clearButton: uiBuilder.FestivalsClearButton, hasValue: selectedFestivals.Count > 0,
                    shadowOffset: 3, shadowOpacity: 0.2f);

            // Behavior section
            uiBuilder.DrawBehaviorHeader(b);

            string priorityText = IsSpecialEventMode
                ? TranslationCache.ScheduleEditPrioritySpecial
                : GetPriorityDisplayName(priority);
            uiBuilder.DrawPriorityRow(b, priorityText, priorityDropdownOpen, isSpecial: IsSpecialEventMode);

            string rotationText = advanceOnWarp
                ? TranslationCache.ScheduleEditRotateOnLocationChange
                : TranslationCache.ScheduleEditRotateOnceADay;
            uiBuilder.DrawRotationRow(b, rotationText, rotationDropdownOpen);

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
                    isChecked: opt => selectedSeasons.Contains(opt.name), panelPaddingV: ScheduleEditDropdownPanelPadding,
                    totalItemCount: seasonOptionNames.Count, arrowYNudge: 4, arrowXNudge: 2);
            else if (weatherDropdownOpen)
                dropdownTooltip = UIHelpers.DrawMultiSelectDropdownOptions(b, uiBuilder.WeatherDropdownBar.bounds,
                    visibleWeatherOptions, weatherScrollIndex, ScheduleEditDropdownMaxVisible,
                    isChecked: opt => weatherEntries.Exists(e => e.displayName == opt.name && selectedWeather.Contains(e.id)),
                    panelPaddingV: ScheduleEditDropdownPanelPadding,
                    totalItemCount: weatherOptionNames.Count, arrowYNudge: 4, arrowXNudge: 2);
            else if (areasDropdownOpen)
                dropdownTooltip = UIHelpers.DrawMultiSelectDropdownOptions(b, uiBuilder.AreasDropdownBar.bounds,
                    visibleAreasOptions, areasScrollIndex, ScheduleEditDropdownMaxVisible,
                    isChecked: opt => { int idx = areaOptionNames.IndexOf(opt.name); return idx >= 0 && selectedAreas.Contains(AreaValues[idx]); },
                    panelPaddingV: ScheduleEditDropdownPanelPadding,
                    totalItemCount: areaOptionNames.Count, arrowYNudge: 4, arrowXNudge: 2);
            else if (locationsDropdownOpen)
                dropdownTooltip = UIHelpers.DrawMultiSelectDropdownOptions(b, uiBuilder.LocationsDropdownBar.bounds,
                    visibleLocationsOptions, locationsScrollIndex, ScheduleEditDropdownMaxVisible,
                    isChecked: opt => filteredLocationEntries.Exists(e => e.displayName == opt.name && selectedLocations.Contains(e.name)),
                    panelPaddingV: ScheduleEditDropdownPanelPadding,
                    totalItemCount: filteredLocationOptionNames.Count, arrowYNudge: 4, arrowXNudge: 2);
            else if (festivalsDropdownOpen)
                dropdownTooltip = UIHelpers.DrawMultiSelectDropdownOptions(b, uiBuilder.FestivalsDropdownBar.bounds,
                    visibleFestivalsOptions, festivalsScrollIndex, ScheduleEditDropdownMaxVisible,
                    isChecked: opt => filteredFestivalEntries.Exists(e => e.displayName == opt.name && selectedFestivals.Contains(e.id)),
                    panelPaddingV: ScheduleEditDropdownPanelPadding,
                    totalItemCount: filteredFestivalOptionNames.Count, arrowYNudge: 4, arrowXNudge: 2);
            else if (priorityDropdownOpen)
                UIHelpers.DrawDropdownOptions(b, uiBuilder.PriorityPanelAnchor, priorityOptions, 0, priorityOptions.Count,
                    isSelected: opt => opt.name == priorityText, panelPaddingV: ScheduleEditDropdownPanelPadding, arrowYNudge: 4, arrowXNudge: 2);
            else if (rotationDropdownOpen)
                UIHelpers.DrawDropdownOptions(b, uiBuilder.RotationPanelAnchor, rotationOptions, 0, rotationOptions.Count,
                    isSelected: opt => opt.name == rotationText, panelPaddingV: ScheduleEditDropdownPanelPadding, arrowYNudge: 4, arrowXNudge: 2);

            // Tooltip (dropdown > bar > name row)
            string? tooltip = dropdownTooltip ?? barTooltip ?? nameTooltip;
            if (tooltip != null && ModEntry.Config.ShowTooltip)
            {
                UIHelpers.DrawWrappedTooltip(b, tooltip);
            }

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
            UpdateSearchTextBoxWidth();
            if (priorityDropdownOpen)
                uiBuilder.BuildPriorityOptions(priorityOptions);
            if (rotationDropdownOpen)
                uiBuilder.BuildRotationOptions(rotationOptions);

            previewOverlay?.gameWindowSizeChanged(oldBounds, newBounds);
        }

        // --- Display name helpers ---

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

            if (uiBuilder.SeasonsDropdownBar.containsPoint(mx, my) && selectedSeasons.Count > 0)
                return string.Join(", ", SeasonValues.Where(s => selectedSeasons.Contains(s)));
            if (uiBuilder.WeatherDropdownBar.containsPoint(mx, my) && selectedWeather.Count > 0)
                return string.Join(", ", weatherEntries.Where(e => selectedWeather.Contains(e.id)).Select(e => e.displayName));
            if (uiBuilder.AreasDropdownBar.containsPoint(mx, my) && selectedAreas.Count > 0)
                return string.Join(", ", AreaValues.Where(a => selectedAreas.Contains(a)).Select(GetAreaDisplayName));
            if (uiBuilder.LocationsDropdownBar.containsPoint(mx, my) && selectedLocations.Count > 0)
                return string.Join(", ", GetSelectedLocationNames());
            if (uiBuilder.FestivalsDropdownBar.containsPoint(mx, my) && selectedFestivals.Count > 0)
                return string.Join(", ", GetSelectedFestivalNames());

            return null;
        }

        // --- Clear button handling ---

        private bool HandleClearButtonClick(int x, int y, bool playSound)
        {
            if (!IsSpecialEventMode)
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
            if (!IsSpecialEventMode)
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

                if (TryToggleDropdown(uiBuilder.LocationsDropdownBar, ref locationsDropdownOpen, x, y, playSound,
                    () => { OpenSearchDropdown("locations"); }))
                    return true;
            }

            if (TryToggleDropdown(uiBuilder.FestivalsDropdownBar, ref festivalsDropdownOpen, x, y, playSound,
                () => { OpenSearchDropdown("festivals"); }))
                return true;

            if (!IsSpecialEventMode && TryToggleDropdown(uiBuilder.PriorityClickArea, ref priorityDropdownOpen, x, y, playSound,
                () => uiBuilder.BuildPriorityOptions(priorityOptions)))
                return true;

            if (TryToggleDropdown(uiBuilder.RotationClickArea, ref rotationDropdownOpen, x, y, playSound,
                () => uiBuilder.BuildRotationOptions(rotationOptions)))
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
            if (rotationDropdownOpen)
            {
                for (int i = 0; i < rotationOptions.Count; i++)
                {
                    if (rotationOptions[i].containsPoint(x, y))
                    {
                        advanceOnWarp = (i == 0);
                        rotationDropdownOpen = false;
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
            if (IsSpecialEventMode) ClearConditions();
        }

        // --- Dropdown helpers ---

        private bool IsAnyDropdownOpen()
        {
            return seasonsDropdownOpen || weatherDropdownOpen || areasDropdownOpen
                || locationsDropdownOpen || festivalsDropdownOpen || priorityDropdownOpen || rotationDropdownOpen;
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
            rotationDropdownOpen = false;

            if (activeSearchDropdown != "")
            {
                activeSearchDropdown = "";
                searchText = "";
                searchTextBox = null;
                Game1.keyboardDispatcher.Subscriber = null;
            }
        }

        private void CloseConditionDropdowns()
        {
            seasonsDropdownOpen = false;
            weatherDropdownOpen = false;
            areasDropdownOpen = false;
            locationsDropdownOpen = false;
        }

        private void ClearConditions()
        {
            selectedSeasons.Clear();
            selectedWeather.Clear();
            selectedAreas.Clear();
            selectedLocations.Clear();
            CloseConditionDropdowns();
        }

        private void OpenSearchDropdown(string dropdown)
        {
            searchText = "";
            activeSearchDropdown = dropdown;

            var bar = dropdown == "locations" ? uiBuilder.LocationsDropdownBar : uiBuilder.FestivalsDropdownBar;
            int maxTextWidth = UIHelpers.CalculateInputBarMaxTextWidth(bar.bounds.Width, hasClearButton: false);

            searchTextBox = new TextBox(null, null, Game1.smallFont, Game1.textColor);
            searchTextBox.Width = maxTextWidth + 21;
            searchTextBox.Text = "";
            searchTextBox.Selected = true;
            Game1.keyboardDispatcher.Subscriber = searchTextBox;

            RebuildFilteredOptions();
        }

        private void UpdateSearchTextBoxWidth()
        {
            if (searchTextBox == null || activeSearchDropdown == "")
                return;
            var bar = activeSearchDropdown == "locations" ? uiBuilder.LocationsDropdownBar : uiBuilder.FestivalsDropdownBar;
            searchTextBox.Width = UIHelpers.CalculateInputBarMaxTextWidth(bar.bounds.Width, hasClearButton: false) + 21;
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
            if (rotationDropdownOpen && uiBuilder.GetRotationDropdownPanelBounds(rotationOptions.Count).Contains(x, y))
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
            if (uiBuilder.OutfitsAddButton.containsPoint(x, y))
            {
                OpenPreviewOverlay();
                if (playSound) Game1.playSound("bigSelect");
                return true;
            }

            if (uiBuilder.SaveButton.containsPoint(x, y))
            {
                HandleSave();
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

        // --- Save ---

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
            rule.SelectedSetIds = new List<string>(selectedSetIds);
            rule.Priority = priority;
            rule.AdvanceOnWarp = advanceOnWarp;

            if (editingRule != null)
            {
                scheduleStore.UpdateRule(rule, originalSetIds!);
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
                SeasonValues.Where(s => selectedSeasons.Contains(s)),
                weatherEntries.Where(e => selectedWeather.Contains(e.id)).Select(e => e.displayName),
                AreaValues.Where(a => selectedAreas.Contains(a)).Select(GetAreaDisplayName),
                GetSelectedLocationNames(),
                GetSelectedFestivalNames(),
                isWeddingDay,
                TranslationCache.ScheduleEditWedding,
                SeasonValues.Length,
                weatherEntries.Count,
                areaOptionNames.Count);
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
                    SelectedSetIds = new List<string>(selectedSetIds)
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
                if (editingRule == null || originalSetIds == null)
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
                        var simulated = new RotationState
                        {
                            RuleId = state.RuleId,
                            Queue = new List<string>(state.Queue),
                            LastUsedId = state.LastUsedId
                        };
                        ScheduleStore.SyncQueueWithSetIds(simulated, originalSetIds, selectedSetIds, new Random());
                        cachedRemaining = simulated.Queue.Count;
                    }
                }
            }
            return cachedRemaining.Value;
        }

        // --- Preview overlay ---

        private void OpenPreviewOverlay()
        {
            CloseAllDropdowns();

            previewOverlay = new ScheduleOutfitOverlay(
                outfitSetStore,
                scheduleStore,
                editingRule,
                new List<string>(selectedSetIds),
                originalSetIds,
                newSelected => { selectedSetIds = new List<string>(newSelected); InvalidatePoolCache(); }
            );
        }

        private void ClosePreviewOverlay()
        {
            previewOverlay?.Cleanup();
            previewOverlay = null;
            Game1.keyboardDispatcher.Subscriber = null;
        }

        // --- Close ---

        private void CloseOverlay()
        {
            Game1.activeClickableMenu = parentMenu;
        }
    }
}
