using System;
using System.Collections.Generic;
using StardewModdingAPI;

namespace OutfitStudio
{
    public static class TranslationCache
    {
        public static string CommonSave { get; private set; } = "";
        public static string CommonCancel { get; private set; } = "";
        public static string CommonReset { get; private set; } = "";
        public static string CommonFavorite { get; private set; } = "";
        public static string CommonNone { get; private set; } = "";

        public static string MenuTitle { get; private set; } = "";
        public static string TabAll { get; private set; } = "";
        public static string TabShirts { get; private set; } = "";
        public static string TabPants { get; private set; } = "";
        public static string TabHats { get; private set; } = "";
        public static string ButtonApply { get; private set; } = "";
        public static string ButtonNewOutfit { get; private set; } = "";
        public static string ButtonOutfits { get; private set; } = "";
        public static string ButtonSchedules { get; private set; } = "";
        public static string MessageApplied { get; private set; } = "";
        public static string MessageSaved { get; private set; } = "";

        public static string FilterAll { get; private set; } = "";
        public static string FilterVanilla { get; private set; } = "";
        public static string FilterUnknown { get; private set; } = "";

        public static string ItemNoHat { get; private set; } = "";
        public static string ItemModInfoTemplate { get; private set; } = ""; // "Mod: {{modName}}"

        public static string LookupShirt { get; private set; } = "";
        public static string LookupPants { get; private set; } = "";
        public static string LookupHat { get; private set; } = "";

        public static string WardrobeSearch { get; private set; } = "";
        public static string WardrobeTags { get; private set; } = "";
        public static string WardrobeButtonApplyClose { get; private set; } = "";
        public static string WardrobeButtonEdit { get; private set; } = "";
        public static string WardrobeButtonDelete { get; private set; } = "";

        public static string SaveSetNameLabel { get; private set; } = "";
        public static string SaveSetNamePlaceholder { get; private set; } = "";
        public static string SaveSetLocalOnly { get; private set; } = "";
        public static string SaveSetTagsLabel { get; private set; } = "";

        public static string TagsPopupTitle { get; private set; } = "";
        public static string TagsPopupTitleEdit { get; private set; } = "";
        public static string TagsPopupCustom { get; private set; } = "";
        public static string TagsPopupDelete { get; private set; } = "";

        // Tag comparison (always case-insensitive)
        public static readonly StringComparer TagComparer = StringComparer.OrdinalIgnoreCase;
        public static readonly StringComparison TagComparison = StringComparison.OrdinalIgnoreCase;

        private static Dictionary<string, string> predefinedTagTranslations = new();

        public static string WardrobeNoSets { get; private set; } = "";
        public static string WardrobeSelectToView { get; private set; } = "";
        public static string WardrobeScopeGlobal { get; private set; } = "";
        public static string WardrobeScopeLocal { get; private set; } = "";
        public static string WardrobeItemNone { get; private set; } = "";

        public static string WardrobeFilterSearchSet { get; private set; } = "";
        public static string WardrobeFilterSearchItem { get; private set; } = "";
        public static string WardrobeFilterSearchAll { get; private set; } = "";
        public static string WardrobeFilterTags { get; private set; } = "";
        public static string WardrobeFilterFilter { get; private set; } = "";
        public static string WardrobeFilterMatchAll { get; private set; } = "";
        public static string WardrobeFilterShowInvalid { get; private set; } = "";
        public static string WardrobeFilterGlobal { get; private set; } = "";
        public static string WardrobeFilterLocal { get; private set; } = "";
        public static string WardrobeFilterInvalid { get; private set; } = "";
        public static string WardrobeFilterNoResults { get; private set; } = "";

        public static string DeleteConfirmQuestion { get; private set; } = "";
        public static string DeleteConfirmYes { get; private set; } = "";
        public static string DeleteConfirmNo { get; private set; } = "";

        public static string ConfigToggleMenuKeyName { get; private set; } = "";
        public static string ConfigToggleMenuKeyTooltip { get; private set; } = "";
        public static string ConfigToggleItemInfoKeyName { get; private set; } = "";
        public static string ConfigToggleItemInfoKeyTooltip { get; private set; } = "";
        public static string ConfigResetFilterOnTabSwitchName { get; private set; } = "";
        public static string ConfigResetFilterOnTabSwitchTooltip { get; private set; } = "";
        public static string ConfigResetSearchOnTabSwitchName { get; private set; } = "";
        public static string ConfigResetSearchOnTabSwitchTooltip { get; private set; } = "";
        public static string ConfigShowTooltipName { get; private set; } = "";
        public static string ConfigShowTooltipTooltip { get; private set; } = "";
        public static string ConfigCloseOnClickOutsideName { get; private set; } = "";
        public static string ConfigCloseOnClickOutsideTooltip { get; private set; } = "";
        public static string ConfigAutoOpenTagMenuName { get; private set; } = "";
        public static string ConfigAutoOpenTagMenuTooltip { get; private set; } = "";
        public static string ConfigAutoFocusSearchBarName { get; private set; } = "";
        public static string ConfigAutoFocusSearchBarTooltip { get; private set; } = "";
        public static string ConfigArrowKeyScrollingName { get; private set; } = "";
        public static string ConfigArrowKeyScrollingTooltip { get; private set; } = "";
        public static string ConfigAutoOpenDyeColorMenuName { get; private set; } = "";
        public static string ConfigAutoOpenDyeColorMenuTooltip { get; private set; } = "";
        public static string ConfigResetMatchAllOnOpenName { get; private set; } = "";
        public static string ConfigResetMatchAllOnOpenTooltip { get; private set; } = "";
        public static string ConfigResetShowInvalidOnOpenName { get; private set; } = "";
        public static string ConfigResetShowInvalidOnOpenTooltip { get; private set; } = "";
        public static string ConfigDefaultSearchScopeName { get; private set; } = "";
        public static string ConfigDefaultSearchScopeTooltip { get; private set; } = "";
        public static string ConfigGeneralSection { get; private set; } = "";
        public static string ConfigMainMenuSection { get; private set; } = "";
        public static string ConfigWardrobeMenuSection { get; private set; } = "";
        public static string ConfigVisibleRowsName { get; private set; } = "";
        public static string ConfigVisibleRowsTooltip { get; private set; } = "";
        public static string ConfigVisibleColumnsName { get; private set; } = "";
        public static string ConfigVisibleColumnsTooltip { get; private set; } = "";
        public static string ConfigSlotSizeName { get; private set; } = "";
        public static string ConfigSlotSizeTooltip { get; private set; } = "";

        public static string ScheduleSearchPlaceholder { get; private set; } = "";
        public static string SchedulePriorityPlaceholder { get; private set; } = "";

        public static string ScheduleTitle { get; private set; } = "";
        public static string ScheduleMasterToggle { get; private set; } = "";
        public static string ScheduleMasterEnabled { get; private set; } = "";
        public static string ScheduleMasterDisabled { get; private set; } = "";
        public static string ScheduleNoRules { get; private set; } = "";
        public static string ScheduleAlways { get; private set; } = "";
        public static string ScheduleNew { get; private set; } = "";
        public static string ScheduleClose { get; private set; } = "";

        public static string ScheduleEditConditions { get; private set; } = "";
        public static string ScheduleEditSpecialEvents { get; private set; } = "";
        public static string ScheduleEditSeason { get; private set; } = "";
        public static string ScheduleEditFestival { get; private set; } = "";
        public static string ScheduleEditTags { get; private set; } = "";
        public static string ScheduleEditAny { get; private set; } = "";
        public static string ScheduleEditPreview { get; private set; } = "";
        public static string ScheduleEditPriority { get; private set; } = "";
        public static string ScheduleEditPriorityHigh { get; private set; } = "";
        public static string ScheduleEditPriorityMedium { get; private set; } = "";
        public static string ScheduleEditPriorityLow { get; private set; } = "";
        public static string ScheduleEditRotate { get; private set; } = "";
        public static string ScheduleEditRotateOnceADay { get; private set; } = "";
        public static string ScheduleEditRotateOnLocationChange { get; private set; } = "";
        public static string ScheduleEditWeather { get; private set; } = "";
        public static string ScheduleEditLocation { get; private set; } = "";
        public static string ScheduleEditArea { get; private set; } = "";
        public static string ScheduleEditWedding { get; private set; } = "";
        public static string ScheduleEditWeatherSunny { get; private set; } = "";
        public static string ScheduleEditWeatherRainy { get; private set; } = "";
        public static string ScheduleEditWeatherStormy { get; private set; } = "";
        public static string ScheduleEditWeatherSnowy { get; private set; } = "";
        public static string ScheduleEditWeatherWindy { get; private set; } = "";
        public static string ScheduleEditWeatherGreenRain { get; private set; } = "";
        public static string ScheduleEditAreaOutdoor { get; private set; } = "";
        public static string ScheduleEditAreaIndoor { get; private set; } = "";
        public static string ScheduleEditSets { get; private set; } = "";
        public static string ScheduleEditTotalOutfits { get; private set; } = "";
        public static string ScheduleInfoOutfits { get; private set; } = "";
        public static string ScheduleInfoDaily { get; private set; } = "";
        public static string ScheduleEditRemainingBeforeReset { get; private set; } = "";
        public static string ScheduleEditDeleteRuleConfirm { get; private set; } = "";

        public static string SetPreviewActiveSets { get; private set; } = "";
        public static string SetPreviewTriggersHeader { get; private set; } = "";

        public static string DyeColorTitle { get; private set; } = "";

        public static string ConfigSettingsTitle { get; private set; } = "";
        public static string ConfigKeybindListening { get; private set; } = "";

        public static void Initialize(ITranslationHelper i18n)
        {
            CommonSave = i18n.Get("common.save");
            CommonCancel = i18n.Get("common.cancel");
            CommonReset = i18n.Get("common.reset");
            CommonFavorite = i18n.Get("common.favorite");
            CommonNone = i18n.Get("common.none");

            MenuTitle = i18n.Get("menu.title");
            TabAll = i18n.Get("menu.tabs.all");
            TabShirts = i18n.Get("menu.tabs.shirts");
            TabPants = i18n.Get("menu.tabs.pants");
            TabHats = i18n.Get("menu.tabs.hats");
            ButtonApply = i18n.Get("menu.buttons.apply");
            ButtonNewOutfit = i18n.Get("menu.buttons.new-outfit");
            ButtonOutfits = i18n.Get("menu.buttons.wardrobe");
            ButtonSchedules = i18n.Get("menu.buttons.schedules");
            MessageApplied = i18n.Get("menu.messages.applied");
            MessageSaved = i18n.Get("menu.messages.saved");

            FilterAll = i18n.Get("menu.filter.all");
            FilterVanilla = i18n.Get("menu.filter.vanilla");
            FilterUnknown = i18n.Get("menu.filter.unknown");

            ItemNoHat = i18n.Get("menu.item.no-hat");
            ItemModInfoTemplate = i18n.Get("menu.item.mod-info");

            LookupShirt = i18n.Get("menu.lookup.shirt");
            LookupPants = i18n.Get("menu.lookup.pants");
            LookupHat = i18n.Get("menu.lookup.hat");

            WardrobeSearch = i18n.Get("wardrobe.search");
            WardrobeTags = i18n.Get("wardrobe.tags");
            WardrobeButtonApplyClose = i18n.Get("wardrobe.button.apply-close");
            WardrobeButtonEdit = i18n.Get("wardrobe.button.edit");
            WardrobeButtonDelete = i18n.Get("wardrobe.button.delete");

            SaveSetNameLabel = i18n.Get("saveset.name-label");
            SaveSetNamePlaceholder = i18n.Get("saveset.name-placeholder");
            SaveSetLocalOnly = i18n.Get("saveset.local-only");
            SaveSetTagsLabel = i18n.Get("saveset.tags-label");

            TagsPopupTitle = i18n.Get("tags.popup.title");
            TagsPopupTitleEdit = i18n.Get("tags.popup.title.edit");
            TagsPopupCustom = i18n.Get("tags.popup.custom");
            TagsPopupDelete = i18n.Get("tags.popup.delete");

            InitializePredefinedTagTranslations(i18n);

            WardrobeNoSets = i18n.Get("wardrobe.no-sets");
            WardrobeSelectToView = i18n.Get("wardrobe.select-to-view");
            WardrobeScopeGlobal = i18n.Get("wardrobe.scope.global");
            WardrobeScopeLocal = i18n.Get("wardrobe.scope.local");
            WardrobeItemNone = i18n.Get("wardrobe.item.none");

            WardrobeFilterSearchSet = i18n.Get("wardrobe.filter.search.set");
            WardrobeFilterSearchItem = i18n.Get("wardrobe.filter.search.item");
            WardrobeFilterSearchAll = i18n.Get("wardrobe.filter.search.all");
            WardrobeFilterTags = i18n.Get("wardrobe.filter.tags");
            WardrobeFilterFilter = i18n.Get("wardrobe.filter.filter");
            WardrobeFilterMatchAll = i18n.Get("wardrobe.filter.matchAll");
            WardrobeFilterShowInvalid = i18n.Get("wardrobe.filter.showInvalid");
            WardrobeFilterGlobal = i18n.Get("wardrobe.filter.global");
            WardrobeFilterLocal = i18n.Get("wardrobe.filter.local");
            WardrobeFilterInvalid = i18n.Get("wardrobe.filter.invalid");
            WardrobeFilterNoResults = i18n.Get("wardrobe.filter.noResults");

            DeleteConfirmQuestion = i18n.Get("wardrobe.delete.confirm-question");
            DeleteConfirmYes = i18n.Get("wardrobe.delete.yes");
            DeleteConfirmNo = i18n.Get("wardrobe.delete.no");

            ConfigToggleMenuKeyName = i18n.Get("config.toggle-menu-key.name");
            ConfigToggleMenuKeyTooltip = i18n.Get("config.toggle-menu-key.tooltip");
            ConfigToggleItemInfoKeyName = i18n.Get("config.toggle-item-info-key.name");
            ConfigToggleItemInfoKeyTooltip = i18n.Get("config.toggle-item-info-key.tooltip");
            ConfigResetFilterOnTabSwitchName = i18n.Get("config.reset-filter-on-tab-switch.name");
            ConfigResetFilterOnTabSwitchTooltip = i18n.Get("config.reset-filter-on-tab-switch.tooltip");
            ConfigResetSearchOnTabSwitchName = i18n.Get("config.reset-search-on-tab-switch.name");
            ConfigResetSearchOnTabSwitchTooltip = i18n.Get("config.reset-search-on-tab-switch.tooltip");
            ConfigShowTooltipName = i18n.Get("config.show-tooltip.name");
            ConfigShowTooltipTooltip = i18n.Get("config.show-tooltip.tooltip");
            ConfigCloseOnClickOutsideName = i18n.Get("config.close-on-click-outside.name");
            ConfigCloseOnClickOutsideTooltip = i18n.Get("config.close-on-click-outside.tooltip");
            ConfigAutoOpenTagMenuName = i18n.Get("config.auto-open-tag-menu.name");
            ConfigAutoOpenTagMenuTooltip = i18n.Get("config.auto-open-tag-menu.tooltip");
            ConfigAutoFocusSearchBarName = i18n.Get("config.auto-focus-search-bar.name");
            ConfigAutoFocusSearchBarTooltip = i18n.Get("config.auto-focus-search-bar.tooltip");
            ConfigArrowKeyScrollingName = i18n.Get("config.arrow-key-scrolling.name");
            ConfigArrowKeyScrollingTooltip = i18n.Get("config.arrow-key-scrolling.tooltip");
            ConfigAutoOpenDyeColorMenuName = i18n.Get("config.auto-open-dye-color-menu.name");
            ConfigAutoOpenDyeColorMenuTooltip = i18n.Get("config.auto-open-dye-color-menu.tooltip");
            ConfigResetMatchAllOnOpenName = i18n.Get("config.reset-match-all-on-open.name");
            ConfigResetMatchAllOnOpenTooltip = i18n.Get("config.reset-match-all-on-open.tooltip");
            ConfigResetShowInvalidOnOpenName = i18n.Get("config.reset-show-invalid-on-open.name");
            ConfigResetShowInvalidOnOpenTooltip = i18n.Get("config.reset-show-invalid-on-open.tooltip");
            ConfigDefaultSearchScopeName = i18n.Get("config.default-search-scope.name");
            ConfigDefaultSearchScopeTooltip = i18n.Get("config.default-search-scope.tooltip");
            ConfigGeneralSection = i18n.Get("config.general.section");
            ConfigMainMenuSection = i18n.Get("config.main-menu.section");
            ConfigWardrobeMenuSection = i18n.Get("config.wardrobe-menu.section");
            ConfigVisibleRowsName = i18n.Get("config.visible-rows.name");
            ConfigVisibleRowsTooltip = i18n.Get("config.visible-rows.tooltip");
            ConfigVisibleColumnsName = i18n.Get("config.visible-columns.name");
            ConfigVisibleColumnsTooltip = i18n.Get("config.visible-columns.tooltip");
            ConfigSlotSizeName = i18n.Get("config.slot-size.name");
            ConfigSlotSizeTooltip = i18n.Get("config.slot-size.tooltip");

            ScheduleSearchPlaceholder = i18n.Get("schedule.search.placeholder");
            SchedulePriorityPlaceholder = i18n.Get("schedule.priority.placeholder");

            ScheduleTitle = i18n.Get("schedule.title");
            ScheduleMasterToggle = i18n.Get("schedule.master-toggle");
            ScheduleMasterEnabled = i18n.Get("schedule.master-enabled");
            ScheduleMasterDisabled = i18n.Get("schedule.master-disabled");
            ScheduleNoRules = i18n.Get("schedule.no-rules");
            ScheduleAlways = i18n.Get("schedule.always");
            ScheduleNew = i18n.Get("schedule.new");
            ScheduleClose = i18n.Get("schedule.close");

            ScheduleEditConditions = i18n.Get("schedule.edit.conditions");
            ScheduleEditSpecialEvents = i18n.Get("schedule.edit.special-events");
            ScheduleEditSeason = i18n.Get("schedule.edit.season");
            ScheduleEditFestival = i18n.Get("schedule.edit.festival");
            ScheduleEditTags = i18n.Get("schedule.edit.tags");
            ScheduleEditAny = i18n.Get("schedule.edit.any");
            ScheduleEditPreview = i18n.Get("schedule.edit.preview");
            ScheduleEditPriority = i18n.Get("schedule.edit.priority");
            ScheduleEditPriorityHigh = i18n.Get("schedule.edit.priority.high");
            ScheduleEditPriorityMedium = i18n.Get("schedule.edit.priority.medium");
            ScheduleEditPriorityLow = i18n.Get("schedule.edit.priority.low");
            ScheduleEditRotate = i18n.Get("schedule.edit.rotate");
            ScheduleEditRotateOnceADay = i18n.Get("schedule.edit.rotate.once-a-day");
            ScheduleEditRotateOnLocationChange = i18n.Get("schedule.edit.rotate.on-location-change");
            ScheduleEditWeather = i18n.Get("schedule.edit.weather");
            ScheduleEditLocation = i18n.Get("schedule.edit.location");
            ScheduleEditArea = i18n.Get("schedule.edit.area");
            ScheduleEditWedding = i18n.Get("schedule.edit.wedding");
            ScheduleEditWeatherSunny = i18n.Get("schedule.edit.weather.sunny");
            ScheduleEditWeatherRainy = i18n.Get("schedule.edit.weather.rainy");
            ScheduleEditWeatherStormy = i18n.Get("schedule.edit.weather.stormy");
            ScheduleEditWeatherSnowy = i18n.Get("schedule.edit.weather.snowy");
            ScheduleEditWeatherWindy = i18n.Get("schedule.edit.weather.windy");
            ScheduleEditWeatherGreenRain = i18n.Get("schedule.edit.weather.green-rain");
            ScheduleEditAreaOutdoor = i18n.Get("schedule.edit.area.outdoor");
            ScheduleEditAreaIndoor = i18n.Get("schedule.edit.area.indoor");
            ScheduleEditSets = i18n.Get("schedule.edit.sets");
            ScheduleEditTotalOutfits = i18n.Get("schedule.edit.total-outfits");
            ScheduleInfoOutfits = i18n.Get("schedule.info.outfits");
            ScheduleInfoDaily = i18n.Get("schedule.info.daily");
            ScheduleEditRemainingBeforeReset = i18n.Get("schedule.edit.remaining-before-reset");
            ScheduleEditDeleteRuleConfirm = i18n.Get("schedule.edit.delete-rule-confirm");

            SetPreviewActiveSets = i18n.Get("schedule.preview.active-sets");
            SetPreviewTriggersHeader = i18n.Get("schedule.preview.triggers-header");

            DyeColorTitle = i18n.Get("dyecolor.title");

            ConfigSettingsTitle = i18n.Get("config.settings.title");
            ConfigKeybindListening = i18n.Get("config.settings.keybind-listening");

        }

        private static void InitializePredefinedTagTranslations(ITranslationHelper i18n)
        {
            predefinedTagTranslations.Clear();
            predefinedTagTranslations["Spring"] = i18n.Get("saveset.tag.spring");
            predefinedTagTranslations["Summer"] = i18n.Get("saveset.tag.summer");
            predefinedTagTranslations["Fall"] = i18n.Get("saveset.tag.fall");
            predefinedTagTranslations["Winter"] = i18n.Get("saveset.tag.winter");
            predefinedTagTranslations["Wedding"] = i18n.Get("saveset.tag.wedding");
            predefinedTagTranslations["Combat"] = i18n.Get("saveset.tag.combat");
            predefinedTagTranslations["Daily"] = i18n.Get("saveset.tag.daily");
        }

        public static string GetTagDisplayName(string tag)
        {
            if (predefinedTagTranslations.TryGetValue(tag, out string? translated))
                return translated;

            return ToTitleCase(tag);
        }

        public static string ToTitleCase(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            char[] chars = text.ToCharArray();
            bool newWord = true;
            for (int i = 0; i < chars.Length; i++)
            {
                if (char.IsWhiteSpace(chars[i]) || chars[i] == '-')
                {
                    newWord = true;
                }
                else if (newWord)
                {
                    chars[i] = char.ToUpper(chars[i]);
                    newWord = false;
                }
            }
            return new string(chars);
        }
    }
}
