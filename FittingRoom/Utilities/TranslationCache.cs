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

        // Wardrobe overlay
        public static string WardrobeTitle { get; private set; } = "";
        public static string WardrobeFilter { get; private set; } = "";
        public static string WardrobeSort { get; private set; } = "";
        public static string WardrobeSearch { get; private set; } = "";
        public static string WardrobeTags { get; private set; } = "";
        public static string WardrobeShirt { get; private set; } = "";
        public static string WardrobePants { get; private set; } = "";
        public static string WardrobeHat { get; private set; } = "";
        public static string WardrobeButtonLoad { get; private set; } = "";
        public static string WardrobeButtonApplyClose { get; private set; } = "";
        public static string WardrobeButtonNew { get; private set; } = "";
        public static string WardrobeButtonDuplicate { get; private set; } = "";
        public static string WardrobeButtonEdit { get; private set; } = "";
        public static string WardrobeButtonDelete { get; private set; } = "";
        public static string WardrobeButtonFavorite { get; private set; } = "";
        public static string WardrobeDetailsLabel { get; private set; } = "";
        public static string WardrobePreviewLabel { get; private set; } = "";

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

        // Wardrobe overlay - additional
        public static string WardrobeNoSets { get; private set; } = "";
        public static string WardrobeSelectToView { get; private set; } = "";
        public static string WardrobeScopeGlobal { get; private set; } = "";
        public static string WardrobeScopeLocal { get; private set; } = "";
        public static string WardrobeItemNone { get; private set; } = "";
        public static string WardrobeItemMissing { get; private set; } = "";
        public static string WardrobeEnterName { get; private set; } = "";

        // Wardrobe filter bar
        public static string WardrobeFilterSearchSet { get; private set; } = "";
        public static string WardrobeFilterSearchItem { get; private set; } = "";
        public static string WardrobeFilterSearchAll { get; private set; } = "";
        public static string WardrobeFilterTags { get; private set; } = "";
        public static string WardrobeFilterFilter { get; private set; } = "";
        public static string WardrobeFilterMatchAll { get; private set; } = "";
        public static string WardrobeFilterShowInvalid { get; private set; } = "";
        public static string WardrobeFilterFavorites { get; private set; } = "";
        public static string WardrobeFilterGlobal { get; private set; } = "";
        public static string WardrobeFilterLocal { get; private set; } = "";
        public static string WardrobeFilterNoResults { get; private set; } = "";
        public static string WardrobeFilterClearSearch { get; private set; } = "";

        // Delete confirmation
        public static string DeleteConfirmQuestion { get; private set; } = "";
        public static string DeleteConfirmYes { get; private set; } = "";
        public static string DeleteConfirmNo { get; private set; } = "";

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
        public static string ConfigResetShowInvalidOnOpenName { get; private set; } = "";
        public static string ConfigResetShowInvalidOnOpenTooltip { get; private set; } = "";
        public static string ConfigDefaultSearchScopeName { get; private set; } = "";
        public static string ConfigDefaultSearchScopeTooltip { get; private set; } = "";
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

            WardrobeTitle = i18n.Get("wardrobe.title");
            WardrobeFilter = i18n.Get("wardrobe.filter");
            WardrobeSort = i18n.Get("wardrobe.sort");
            WardrobeSearch = i18n.Get("wardrobe.search");
            WardrobeTags = i18n.Get("wardrobe.tags");
            WardrobeShirt = i18n.Get("wardrobe.shirt");
            WardrobePants = i18n.Get("wardrobe.pants");
            WardrobeHat = i18n.Get("wardrobe.hat");
            WardrobeButtonLoad = i18n.Get("wardrobe.button.load");
            WardrobeButtonApplyClose = i18n.Get("wardrobe.button.apply-close");
            WardrobeButtonNew = i18n.Get("wardrobe.button.new");
            WardrobeButtonDuplicate = i18n.Get("wardrobe.button.duplicate");
            WardrobeButtonEdit = i18n.Get("wardrobe.button.edit");
            WardrobeButtonDelete = i18n.Get("wardrobe.button.delete");
            WardrobeButtonFavorite = i18n.Get("wardrobe.button.favorite");
            WardrobeDetailsLabel = i18n.Get("wardrobe.details-label");
            WardrobePreviewLabel = i18n.Get("wardrobe.preview-label");

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

            WardrobeNoSets = i18n.Get("wardrobe.no-sets");
            WardrobeSelectToView = i18n.Get("wardrobe.select-to-view");
            WardrobeScopeGlobal = i18n.Get("wardrobe.scope.global");
            WardrobeScopeLocal = i18n.Get("wardrobe.scope.local");
            WardrobeItemNone = i18n.Get("wardrobe.item.none");
            WardrobeItemMissing = i18n.Get("wardrobe.item.missing");
            WardrobeEnterName = i18n.Get("wardrobe.enter-name");

            WardrobeFilterSearchSet = i18n.Get("wardrobe.filter.search.set");
            WardrobeFilterSearchItem = i18n.Get("wardrobe.filter.search.item");
            WardrobeFilterSearchAll = i18n.Get("wardrobe.filter.search.all");
            WardrobeFilterTags = i18n.Get("wardrobe.filter.tags");
            WardrobeFilterFilter = i18n.Get("wardrobe.filter.filter");
            WardrobeFilterMatchAll = i18n.Get("wardrobe.filter.matchAll");
            WardrobeFilterShowInvalid = i18n.Get("wardrobe.filter.showInvalid");
            WardrobeFilterFavorites = i18n.Get("wardrobe.filter.favorites");
            WardrobeFilterGlobal = i18n.Get("wardrobe.filter.global");
            WardrobeFilterLocal = i18n.Get("wardrobe.filter.local");
            WardrobeFilterNoResults = i18n.Get("wardrobe.filter.noResults");
            WardrobeFilterClearSearch = i18n.Get("wardrobe.filter.clearSearch");

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
            ConfigShowFilterTooltipName = i18n.Get("config.show-filter-tooltip.name");
            ConfigShowFilterTooltipTooltip = i18n.Get("config.show-filter-tooltip.tooltip");
            ConfigAutoOpenTagMenuName = i18n.Get("config.auto-open-tag-menu.name");
            ConfigAutoOpenTagMenuTooltip = i18n.Get("config.auto-open-tag-menu.tooltip");
            ConfigResetShowInvalidOnOpenName = i18n.Get("config.reset-show-invalid-on-open.name");
            ConfigResetShowInvalidOnOpenTooltip = i18n.Get("config.reset-show-invalid-on-open.tooltip");
            ConfigDefaultSearchScopeName = i18n.Get("config.default-search-scope.name");
            ConfigDefaultSearchScopeTooltip = i18n.Get("config.default-search-scope.tooltip");
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
