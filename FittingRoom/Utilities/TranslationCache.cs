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
            MessageSaved = i18n.Get("menu.messages.applied");

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
        }
    }
}
