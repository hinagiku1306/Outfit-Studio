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

        // Lookup tooltip labels
        public static string LookupShirt { get; private set; } = "";
        public static string LookupPants { get; private set; } = "";
        public static string LookupHat { get; private set; } = "";

        // Wardrobe overlay
        public static string WardrobeFilter { get; private set; } = "";
        public static string WardrobeSearch { get; private set; } = "";
        public static string WardrobeTags { get; private set; } = "";
        public static string WardrobeButtonApplyClose { get; private set; } = "";
        public static string WardrobeButtonEdit { get; private set; } = "";
        public static string WardrobeButtonDelete { get; private set; } = "";

        // Save Set overlay
        public static string SaveSetNameLabel { get; private set; } = "";
        public static string SaveSetNamePlaceholder { get; private set; } = "";
        public static string SaveSetFavorite { get; private set; } = "";
        public static string SaveSetButtonSave { get; private set; } = "";
        public static string SaveSetButtonCancel { get; private set; } = "";
        public static string SaveSetLocalOnly { get; private set; } = "";
        public static string SaveSetTagsLabel { get; private set; } = "";

        // Tag picker popup
        public static string TagsPopupTitle { get; private set; } = "";
        public static string TagsPopupTitleEdit { get; private set; } = "";
        public static string TagsPopupCustom { get; private set; } = "";
        public static string TagsPopupDelete { get; private set; } = "";

        // Tag comparison (always case-insensitive)
        public static readonly StringComparer TagComparer = StringComparer.OrdinalIgnoreCase;
        public static readonly StringComparison TagComparison = StringComparison.OrdinalIgnoreCase;

        // Predefined tag translations
        private static Dictionary<string, string> predefinedTagTranslations = new();

        // Wardrobe overlay - additional
        public static string WardrobeNoSets { get; private set; } = "";
        public static string WardrobeSelectToView { get; private set; } = "";
        public static string WardrobeScopeGlobal { get; private set; } = "";
        public static string WardrobeScopeLocal { get; private set; } = "";
        public static string WardrobeItemNone { get; private set; } = "";

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

        // Delete confirmation
        public static string DeleteConfirmQuestion { get; private set; } = "";
        public static string DeleteConfirmYes { get; private set; } = "";
        public static string DeleteConfirmNo { get; private set; } = "";

        // Config (GMCM)
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
        public static string ConfigAutoOpenTagMenuName { get; private set; } = "";
        public static string ConfigAutoOpenTagMenuTooltip { get; private set; } = "";
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

            LookupShirt = i18n.Get("menu.lookup.shirt");
            LookupPants = i18n.Get("menu.lookup.pants");
            LookupHat = i18n.Get("menu.lookup.hat");

            WardrobeFilter = i18n.Get("wardrobe.filter");
            WardrobeSearch = i18n.Get("wardrobe.search");
            WardrobeTags = i18n.Get("wardrobe.tags");
            WardrobeButtonApplyClose = i18n.Get("wardrobe.button.apply-close");
            WardrobeButtonEdit = i18n.Get("wardrobe.button.edit");
            WardrobeButtonDelete = i18n.Get("wardrobe.button.delete");

            SaveSetNameLabel = i18n.Get("saveset.name-label");
            SaveSetNamePlaceholder = i18n.Get("saveset.name-placeholder");
            SaveSetFavorite = i18n.Get("saveset.favorite");
            SaveSetButtonSave = i18n.Get("saveset.button.save");
            SaveSetButtonCancel = i18n.Get("saveset.button.cancel");
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
            WardrobeFilterFavorites = i18n.Get("wardrobe.filter.favorites");
            WardrobeFilterGlobal = i18n.Get("wardrobe.filter.global");
            WardrobeFilterLocal = i18n.Get("wardrobe.filter.local");
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
            ConfigAutoOpenTagMenuName = i18n.Get("config.auto-open-tag-menu.name");
            ConfigAutoOpenTagMenuTooltip = i18n.Get("config.auto-open-tag-menu.tooltip");
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

            // TODO: Clean up test tags from saved data: Festival, Work, Formal, Casual
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

            char[] chars = text.ToLower().ToCharArray();
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
