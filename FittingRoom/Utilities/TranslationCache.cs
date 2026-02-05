using System;
using System.Collections.Generic;
using StardewModdingAPI;

namespace FittingRoom
{
    /// <summary>
    /// Centralized cache for all UI translations.
    /// Initialized once at mod startup to avoid repeated I/O calls.
    /// </summary>
    public static class TranslationCache
    {
        // Menu UI
        public static string MenuTitle { get; private set; } = "";
        public static string TabAll { get; private set; } = "";
        public static string TabShirts { get; private set; } = "";
        public static string TabPants { get; private set; } = "";
        public static string TabHats { get; private set; } = "";
        public static string ButtonApply { get; private set; } = "";
        public static string ButtonReset { get; private set; } = "";
        public static string ButtonNewOutfit { get; private set; } = "";
        public static string ButtonOutfits { get; private set; } = "";
        public static string MessageApplied { get; private set; } = "";
        public static string MessageSaved { get; private set; } = "";

        // Filter options
        public static string FilterAll { get; private set; } = "";
        public static string FilterVanilla { get; private set; } = "";
        public static string FilterUnknown { get; private set; } = "";

        // Item info
        public static string ItemNoHat { get; private set; } = "";
        public static string ItemNoShirt { get; private set; } = "";
        public static string ItemNoPants { get; private set; } = "";
        public static string ItemModInfoTemplate { get; private set; } = ""; // "Mod: {{modName}}"
        public static string ItemNoHatSymbol { get; private set; } = "";

        // Lookup tooltip labels
        public static string LookupShirt { get; private set; } = "";
        public static string LookupPants { get; private set; } = "";
        public static string LookupHat { get; private set; } = "";

        // Templates overlay
        public static string TemplatesTitle { get; private set; } = "";
        public static string TemplatesFilter { get; private set; } = "";
        public static string TemplatesSort { get; private set; } = "";
        public static string TemplatesSearch { get; private set; } = "";
        public static string TemplatesName { get; private set; } = "";
        public static string TemplatesTags { get; private set; } = "";
        public static string TemplatesShirt { get; private set; } = "";
        public static string TemplatesPants { get; private set; } = "";
        public static string TemplatesHat { get; private set; } = "";
        public static string TemplatesButtonLoad { get; private set; } = "";
        public static string TemplatesButtonApplyClose { get; private set; } = "";
        public static string TemplatesButtonNew { get; private set; } = "";
        public static string TemplatesButtonDuplicate { get; private set; } = "";
        public static string TemplatesButtonRename { get; private set; } = "";
        public static string TemplatesButtonDelete { get; private set; } = "";
        public static string TemplatesButtonFavorite { get; private set; } = "";
        public static string TemplatesDetailsLabel { get; private set; } = "";
        public static string TemplatesPreviewLabel { get; private set; } = "";

        // Save Set overlay
        public static string SaveSetTitle { get; private set; } = "";
        public static string SaveSetNameLabel { get; private set; } = "";
        public static string SaveSetNamePlaceholder { get; private set; } = "";
        public static string SaveSetTagLabel { get; private set; } = "";
        public static string SaveSetTagNone { get; private set; } = "";
        public static string SaveSetTagSpring { get; private set; } = "";
        public static string SaveSetTagSummer { get; private set; } = "";
        public static string SaveSetTagFall { get; private set; } = "";
        public static string SaveSetTagWinter { get; private set; } = "";
        public static string SaveSetTagFestival { get; private set; } = "";
        public static string SaveSetTagWork { get; private set; } = "";
        public static string SaveSetTagFormal { get; private set; } = "";
        public static string SaveSetTagCasual { get; private set; } = "";
        public static string SaveSetFavorite { get; private set; } = "";
        public static string SaveSetIncludeShirt { get; private set; } = "";
        public static string SaveSetIncludePants { get; private set; } = "";
        public static string SaveSetIncludeHat { get; private set; } = "";
        public static string SaveSetButtonSave { get; private set; } = "";
        public static string SaveSetButtonCancel { get; private set; } = "";
        public static string SaveSetMessageSaved { get; private set; } = "";
        public static string SaveSetMessageNameRequired { get; private set; } = "";
        public static string SaveSetLocalOnly { get; private set; } = "";
        public static string SaveSetAddTags { get; private set; } = "";
        public static string SaveSetHideTags { get; private set; } = "";
        public static string SaveSetTagsLabel { get; private set; } = "";

        // Tag picker popup
        public static string TagsPopupTitle { get; private set; } = "";
        public static string TagsPopupTitleEdit { get; private set; } = "";
        public static string TagsPopupCustom { get; private set; } = "";
        public static string TagsPopupAdd { get; private set; } = "";
        public static string TagsPopupDelete { get; private set; } = "";
        public static string TagsMaxReached { get; private set; } = "";

        // Predefined tag translations
        private static Dictionary<string, string> predefinedTagTranslations = new(StringComparer.OrdinalIgnoreCase);

        // Templates overlay - additional
        public static string TemplatesNoSets { get; private set; } = "";
        public static string TemplatesSelectToView { get; private set; } = "";
        public static string TemplatesScopeGlobal { get; private set; } = "";
        public static string TemplatesScopeLocal { get; private set; } = "";
        public static string TemplatesItemNone { get; private set; } = "";
        public static string TemplatesItemMissing { get; private set; } = "";
        public static string TemplatesEnterName { get; private set; } = "";

        // Templates filter bar
        public static string TemplatesFilterSearchSet { get; private set; } = "";
        public static string TemplatesFilterSearchItem { get; private set; } = "";
        public static string TemplatesFilterSearchAll { get; private set; } = "";
        public static string TemplatesFilterTags { get; private set; } = "";
        public static string TemplatesFilterFilter { get; private set; } = "";
        public static string TemplatesFilterMatchAll { get; private set; } = "";
        public static string TemplatesFilterShowInvalid { get; private set; } = "";
        public static string TemplatesFilterFavorites { get; private set; } = "";
        public static string TemplatesFilterGlobal { get; private set; } = "";
        public static string TemplatesFilterLocal { get; private set; } = "";
        public static string TemplatesFilterNoResults { get; private set; } = "";
        public static string TemplatesFilterClearSearch { get; private set; } = "";

        // Config (optional - only needed if GMCM is used)
        public static string ConfigToggleMenuKeyName { get; private set; } = "";
        public static string ConfigToggleMenuKeyTooltip { get; private set; } = "";
        public static string ConfigToggleItemInfoKeyName { get; private set; } = "";
        public static string ConfigToggleItemInfoKeyTooltip { get; private set; } = "";
        public static string ConfigResetFilterOnTabSwitchName { get; private set; } = "";
        public static string ConfigResetFilterOnTabSwitchTooltip { get; private set; } = "";
        public static string ConfigResetSearchOnTabSwitchName { get; private set; } = "";
        public static string ConfigResetSearchOnTabSwitchTooltip { get; private set; } = "";
        public static string ConfigShowFilterTooltipName { get; private set; } = "";
        public static string ConfigShowFilterTooltipTooltip { get; private set; } = "";
        public static string ConfigAutoOpenTagMenuName { get; private set; } = "";
        public static string ConfigAutoOpenTagMenuTooltip { get; private set; } = "";
        public static string ConfigGridLayoutSection { get; private set; } = "";
        public static string ConfigVisibleRowsName { get; private set; } = "";
        public static string ConfigVisibleRowsTooltip { get; private set; } = "";
        public static string ConfigVisibleColumnsName { get; private set; } = "";
        public static string ConfigVisibleColumnsTooltip { get; private set; } = "";
        public static string ConfigSlotSizeName { get; private set; } = "";
        public static string ConfigSlotSizeTooltip { get; private set; } = "";

        /// <summary>
        /// Initialize all translations from the translation helper.
        /// Call this once in ModEntry.Entry() after SMAPI is ready.
        /// </summary>
        public static void Initialize(ITranslationHelper i18n)
        {
            MenuTitle = i18n.Get("menu.title");
            TabAll = i18n.Get("menu.tabs.all");
            TabShirts = i18n.Get("menu.tabs.shirts");
            TabPants = i18n.Get("menu.tabs.pants");
            TabHats = i18n.Get("menu.tabs.hats");
            ButtonApply = i18n.Get("menu.buttons.apply");
            ButtonReset = i18n.Get("menu.buttons.reset");
            ButtonNewOutfit = i18n.Get("menu.buttons.new-outfit");
            ButtonOutfits = i18n.Get("menu.buttons.wardrobe");
            MessageApplied = i18n.Get("menu.messages.applied");
            MessageSaved = i18n.Get("menu.messages.saved");

            FilterAll = i18n.Get("menu.filter.all");
            FilterVanilla = i18n.Get("menu.filter.vanilla");
            FilterUnknown = i18n.Get("menu.filter.unknown");

            ItemNoHat = i18n.Get("menu.item.no-hat");
            ItemNoShirt = i18n.Get("menu.item.no-shirt");
            ItemNoPants = i18n.Get("menu.item.no-pants");
            ItemModInfoTemplate = i18n.Get("menu.item.mod-info");
            ItemNoHatSymbol = i18n.Get("menu.item.no-hat-symbol");

            LookupShirt = i18n.Get("menu.lookup.shirt");
            LookupPants = i18n.Get("menu.lookup.pants");
            LookupHat = i18n.Get("menu.lookup.hat");

            TemplatesTitle = i18n.Get("templates.title");
            TemplatesFilter = i18n.Get("templates.filter");
            TemplatesSort = i18n.Get("templates.sort");
            TemplatesSearch = i18n.Get("templates.search");
            TemplatesName = i18n.Get("templates.name");
            TemplatesTags = i18n.Get("templates.tags");
            TemplatesShirt = i18n.Get("templates.shirt");
            TemplatesPants = i18n.Get("templates.pants");
            TemplatesHat = i18n.Get("templates.hat");
            TemplatesButtonLoad = i18n.Get("templates.button.load");
            TemplatesButtonApplyClose = i18n.Get("templates.button.apply-close");
            TemplatesButtonNew = i18n.Get("templates.button.new");
            TemplatesButtonDuplicate = i18n.Get("templates.button.duplicate");
            TemplatesButtonRename = i18n.Get("templates.button.rename");
            TemplatesButtonDelete = i18n.Get("templates.button.delete");
            TemplatesButtonFavorite = i18n.Get("templates.button.favorite");
            TemplatesDetailsLabel = i18n.Get("templates.details-label");
            TemplatesPreviewLabel = i18n.Get("templates.preview-label");

            SaveSetTitle = i18n.Get("saveset.title");
            SaveSetNameLabel = i18n.Get("saveset.name-label");
            SaveSetNamePlaceholder = i18n.Get("saveset.name-placeholder");
            SaveSetTagLabel = i18n.Get("saveset.tag-label");
            SaveSetTagNone = i18n.Get("saveset.tag.none");
            SaveSetTagSpring = i18n.Get("saveset.tag.spring");
            SaveSetTagSummer = i18n.Get("saveset.tag.summer");
            SaveSetTagFall = i18n.Get("saveset.tag.fall");
            SaveSetTagWinter = i18n.Get("saveset.tag.winter");
            SaveSetTagFestival = i18n.Get("saveset.tag.festival");
            SaveSetTagWork = i18n.Get("saveset.tag.work");
            SaveSetTagFormal = i18n.Get("saveset.tag.formal");
            SaveSetTagCasual = i18n.Get("saveset.tag.casual");
            SaveSetFavorite = i18n.Get("saveset.favorite");
            SaveSetIncludeShirt = i18n.Get("saveset.include-shirt");
            SaveSetIncludePants = i18n.Get("saveset.include-pants");
            SaveSetIncludeHat = i18n.Get("saveset.include-hat");
            SaveSetButtonSave = i18n.Get("saveset.button.save");
            SaveSetButtonCancel = i18n.Get("saveset.button.cancel");
            SaveSetMessageSaved = i18n.Get("saveset.message.saved");
            SaveSetMessageNameRequired = i18n.Get("saveset.message.name-required");
            SaveSetLocalOnly = i18n.Get("saveset.local-only");
            SaveSetAddTags = i18n.Get("saveset.add-tags");
            SaveSetHideTags = i18n.Get("saveset.hide-tags");
            SaveSetTagsLabel = i18n.Get("saveset.tags-label");

            TagsPopupTitle = i18n.Get("tags.popup.title");
            TagsPopupTitleEdit = i18n.Get("tags.popup.title.edit");
            TagsPopupCustom = i18n.Get("tags.popup.custom");
            TagsPopupAdd = i18n.Get("tags.popup.add");
            TagsPopupDelete = i18n.Get("tags.popup.delete");
            TagsMaxReached = i18n.Get("tags.max-reached");

            InitializePredefinedTagTranslations(i18n);

            TemplatesNoSets = i18n.Get("templates.no-sets");
            TemplatesSelectToView = i18n.Get("templates.select-to-view");
            TemplatesScopeGlobal = i18n.Get("templates.scope.global");
            TemplatesScopeLocal = i18n.Get("templates.scope.local");
            TemplatesItemNone = i18n.Get("templates.item.none");
            TemplatesItemMissing = i18n.Get("templates.item.missing");
            TemplatesEnterName = i18n.Get("templates.enter-name");

            TemplatesFilterSearchSet = i18n.Get("templates.filter.search.set");
            TemplatesFilterSearchItem = i18n.Get("templates.filter.search.item");
            TemplatesFilterSearchAll = i18n.Get("templates.filter.search.all");
            TemplatesFilterTags = i18n.Get("templates.filter.tags");
            TemplatesFilterFilter = i18n.Get("templates.filter.filter");
            TemplatesFilterMatchAll = i18n.Get("templates.filter.matchAll");
            TemplatesFilterShowInvalid = i18n.Get("templates.filter.showInvalid");
            TemplatesFilterFavorites = i18n.Get("templates.filter.favorites");
            TemplatesFilterGlobal = i18n.Get("templates.filter.global");
            TemplatesFilterLocal = i18n.Get("templates.filter.local");
            TemplatesFilterNoResults = i18n.Get("templates.filter.noResults");
            TemplatesFilterClearSearch = i18n.Get("templates.filter.clearSearch");

            ConfigToggleMenuKeyName = i18n.Get("config.toggle-menu-key.name");
            ConfigToggleMenuKeyTooltip = i18n.Get("config.toggle-menu-key.tooltip");
            ConfigToggleItemInfoKeyName = i18n.Get("config.toggle-item-info-key.name");
            ConfigToggleItemInfoKeyTooltip = i18n.Get("config.toggle-item-info-key.tooltip");
            ConfigResetFilterOnTabSwitchName = i18n.Get("config.reset-filter-on-tab-switch.name");
            ConfigResetFilterOnTabSwitchTooltip = i18n.Get("config.reset-filter-on-tab-switch.tooltip");
            ConfigResetSearchOnTabSwitchName = i18n.Get("config.reset-search-on-tab-switch.name");
            ConfigResetSearchOnTabSwitchTooltip = i18n.Get("config.reset-search-on-tab-switch.tooltip");
            ConfigShowFilterTooltipName = i18n.Get("config.show-filter-tooltip.name");
            ConfigShowFilterTooltipTooltip = i18n.Get("config.show-filter-tooltip.tooltip");
            ConfigAutoOpenTagMenuName = i18n.Get("config.auto-open-tag-menu.name");
            ConfigAutoOpenTagMenuTooltip = i18n.Get("config.auto-open-tag-menu.tooltip");
            ConfigGridLayoutSection = i18n.Get("config.grid-layout.section");
            ConfigVisibleRowsName = i18n.Get("config.visible-rows.name");
            ConfigVisibleRowsTooltip = i18n.Get("config.visible-rows.tooltip");
            ConfigVisibleColumnsName = i18n.Get("config.visible-columns.name");
            ConfigVisibleColumnsTooltip = i18n.Get("config.visible-columns.tooltip");
            ConfigSlotSizeName = i18n.Get("config.slot-size.name");
            ConfigSlotSizeTooltip = i18n.Get("config.slot-size.tooltip");
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
            predefinedTagTranslations["Festival"] = i18n.Get("saveset.tag.festival");
            predefinedTagTranslations["Work"] = i18n.Get("saveset.tag.work");
            predefinedTagTranslations["Formal"] = i18n.Get("saveset.tag.formal");
            predefinedTagTranslations["Casual"] = i18n.Get("saveset.tag.casual");
        }

        /// <summary>
        /// Gets the translated display name for a tag.
        /// Returns the original tag if no translation exists.
        /// </summary>
        public static string GetTagDisplayName(string tag)
        {
            return predefinedTagTranslations.TryGetValue(tag, out string? translated) ? translated : tag;
        }
    }
}
