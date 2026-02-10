using System;
using OutfitStudio.Services;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace OutfitStudio
{
    public class ModEntry : Mod
    {
        private ModConfig config = null!;
        private OutfitMenu? menu;

        public static ModConfig Config { get; private set; } = null!;

        private OutfitFilterManager? filterManager;
        private OutfitCategoryManager? categoryManager;
        private OutfitSetStore? outfitSetStore;

        public override void Entry(IModHelper helper)
        {
            config = helper.ReadConfig<ModConfig>();
            Config = config;

            DebugLogger.Initialize(Monitor, config);
            TranslationCache.Initialize(helper.Translation);
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.Content.LocaleChanged += OnLocaleChanged;
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            filterManager = new OutfitFilterManager(Monitor, Helper);
            categoryManager = new OutfitCategoryManager(Monitor, filterManager);
            filterManager.BuildModMapping(categoryManager.ShirtIds, categoryManager.PantsIds, categoryManager.HatIds);

            outfitSetStore?.LoadLocalData();
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            outfitSetStore?.ClearLocalData();
        }

        private void OnLocaleChanged(object? sender, LocaleChangedEventArgs e)
        {
            TranslationCache.Initialize(Helper.Translation);
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {

            outfitSetStore = new OutfitSetStore(Helper, Monitor);
            outfitSetStore.LoadGlobalData();

            var gmcmApi = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcmApi != null)
            {
                gmcmApi.Register(
                    mod: ModManifest,
                    reset: () => config = new ModConfig(),
                    save: () => Helper.WriteConfig(config)
                );

                gmcmApi.AddSectionTitle(
                    mod: ModManifest,
                    text: () => TranslationCache.ConfigGeneralSection
                );

                gmcmApi.AddKeybindList(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigToggleMenuKeyName,
                    tooltip: () => TranslationCache.ConfigToggleMenuKeyTooltip,
                    getValue: () => config.ToggleMenuKey,
                    setValue: value => config.ToggleMenuKey = value
                );

                gmcmApi.AddKeybindList(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigToggleItemInfoKeyName,
                    tooltip: () => TranslationCache.ConfigToggleItemInfoKeyTooltip,
                    getValue: () => config.ToggleItemInfoKey,
                    setValue: value => config.ToggleItemInfoKey = value
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigShowTooltipName,
                    tooltip: () => TranslationCache.ConfigShowTooltipTooltip,
                    getValue: () => config.ShowTooltip,
                    setValue: value => config.ShowTooltip = value
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigCloseOnClickOutsideName,
                    tooltip: () => TranslationCache.ConfigCloseOnClickOutsideTooltip,
                    getValue: () => config.CloseOnClickOutside,
                    setValue: value => config.CloseOnClickOutside = value
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigAutoOpenTagMenuName,
                    tooltip: () => TranslationCache.ConfigAutoOpenTagMenuTooltip,
                    getValue: () => config.AutoOpenTagMenu,
                    setValue: value => config.AutoOpenTagMenu = value
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigAutoFocusSearchBarName,
                    tooltip: () => TranslationCache.ConfigAutoFocusSearchBarTooltip,
                    getValue: () => config.AutoFocusSearchBar,
                    setValue: value => config.AutoFocusSearchBar = value
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigArrowKeyScrollingName,
                    tooltip: () => TranslationCache.ConfigArrowKeyScrollingTooltip,
                    getValue: () => config.ArrowKeyScrolling,
                    setValue: value => config.ArrowKeyScrolling = value
                );

                gmcmApi.AddSectionTitle(
                    mod: ModManifest,
                    text: () => TranslationCache.ConfigMainMenuSection
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigResetFilterOnTabSwitchName,
                    tooltip: () => TranslationCache.ConfigResetFilterOnTabSwitchTooltip,
                    getValue: () => config.ResetFilterOnTabSwitch,
                    setValue: value => config.ResetFilterOnTabSwitch = value
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigAutoOpenDyeColorMenuName,
                    tooltip: () => TranslationCache.ConfigAutoOpenDyeColorMenuTooltip,
                    getValue: () => config.AutoOpenDyeColorMenu,
                    setValue: value => config.AutoOpenDyeColorMenu = value
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigResetSearchOnTabSwitchName,
                    tooltip: () => TranslationCache.ConfigResetSearchOnTabSwitchTooltip,
                    getValue: () => config.ResetSearchOnTabSwitch,
                    setValue: value => config.ResetSearchOnTabSwitch = value
                );

                gmcmApi.AddNumberOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigVisibleRowsName,
                    tooltip: () => TranslationCache.ConfigVisibleRowsTooltip,
                    getValue: () => config.VisibleRows,
                    setValue: value => config.VisibleRows = value,
                    min: OutfitLayoutConstants.MinVisibleRows,
                    max: OutfitLayoutConstants.MaxVisibleRows
                );

                gmcmApi.AddNumberOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigVisibleColumnsName,
                    tooltip: () => TranslationCache.ConfigVisibleColumnsTooltip,
                    getValue: () => config.VisibleColumns,
                    setValue: value => config.VisibleColumns = value,
                    min: OutfitLayoutConstants.MinVisibleColumns,
                    max: OutfitLayoutConstants.MaxVisibleColumns
                );

                gmcmApi.AddNumberOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigSlotSizeName,
                    tooltip: () => TranslationCache.ConfigSlotSizeTooltip,
                    getValue: () => config.SlotSize,
                    setValue: value => config.SlotSize = value,
                    min: OutfitLayoutConstants.MinSlotSize,
                    max: OutfitLayoutConstants.MaxSlotSize
                );

                gmcmApi.AddSectionTitle(
                    mod: ModManifest,
                    text: () => TranslationCache.ConfigWardrobeMenuSection
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigResetMatchAllOnOpenName,
                    tooltip: () => TranslationCache.ConfigResetMatchAllOnOpenTooltip,
                    getValue: () => config.ResetMatchAllOnOpen,
                    setValue: value => config.ResetMatchAllOnOpen = value
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigResetShowInvalidOnOpenName,
                    tooltip: () => TranslationCache.ConfigResetShowInvalidOnOpenTooltip,
                    getValue: () => config.ResetShowInvalidOnOpen,
                    setValue: value => config.ResetShowInvalidOnOpen = value
                );

                gmcmApi.AddTextOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigDefaultSearchScopeName,
                    tooltip: () => TranslationCache.ConfigDefaultSearchScopeTooltip,
                    getValue: () => config.DefaultSearchScope,
                    setValue: value => config.DefaultSearchScope = value,
                    allowedValues: new[] { "Set", "Item", "All" },
                    formatAllowedValue: value => value switch
                    {
                        "Set" => TranslationCache.WardrobeFilterSearchSet,
                        "Item" => TranslationCache.WardrobeFilterSearchItem,
                        "All" => TranslationCache.WardrobeFilterSearchAll,
                        _ => value
                    }
                );
            }
        }

        private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
        {
            if (!Context.IsWorldReady || !Context.IsPlayerFree)
                return;

            if (config.ToggleMenuKey.JustPressed())
            {
                if (Game1.activeClickableMenu is OutfitMenu)
                {
                    Game1.exitActiveMenu();
                }
                else
                {
                    if (categoryManager == null || filterManager == null || outfitSetStore == null)
                    {
                        Monitor.Log("Managers not initialized yet. This shouldn't happen.", LogLevel.Warn);
                        return;
                    }

                    menu = new OutfitMenu(this, categoryManager, filterManager, outfitSetStore, config.ShowItemInfo);
                    Game1.activeClickableMenu = menu;
                }
            }
        }

        internal ModConfig GetConfig() => config;

        internal void SaveConfig() => Helper.WriteConfig(config);

        internal void ReopenMenu()
        {
            if (categoryManager == null || filterManager == null || outfitSetStore == null)
                return;

            Game1.exitActiveMenu();
            menu = new OutfitMenu(this, categoryManager, filterManager, outfitSetStore, config.ShowItemInfo);
            Game1.activeClickableMenu = menu;
        }

        internal void SetShowItemInfoPreference(bool value)
        {
            config.ShowItemInfo = value;
            Helper.WriteConfig(config);
        }
    }
}
