using System;
using FittingRoom.Services;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace FittingRoom
{
    public class ModEntry : Mod
    {
        private ModConfig config = null!;
        private OutfitMenu? menu;

        // Cached managers (initialized once when game launches)
        private OutfitFilterManager? filterManager;
        private OutfitCategoryManager? categoryManager;
        private TemplateManager? templateManager;

        public override void Entry(IModHelper helper)
        {
            config = helper.ReadConfig<ModConfig>();

            // Initialize debug logger early
            DebugLogger.Initialize(Monitor, config);

            // Initialize translation cache early to avoid repeated I/O calls
            TranslationCache.Initialize(helper.Translation);

            helper.Events.Input.ButtonsChanged += OnButtonsChanged;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            // Initialize managers when save is loaded (game data is now available)
            filterManager = new OutfitFilterManager(Monitor, Helper);
            categoryManager = new OutfitCategoryManager(Monitor, filterManager);
            filterManager.BuildModMapping(categoryManager.ShirtIds, categoryManager.PantsIds, categoryManager.HatIds);
            templateManager = new TemplateManager(Helper);
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // Set up Generic Mod Config Menu integration if available
            var gmcmApi = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcmApi != null)
            {
                gmcmApi.Register(
                    mod: ModManifest,
                    reset: () => config = new ModConfig(),
                    save: () => Helper.WriteConfig(config)
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
                    name: () => TranslationCache.ConfigResetFilterOnTabSwitchName,
                    tooltip: () => TranslationCache.ConfigResetFilterOnTabSwitchTooltip,
                    getValue: () => config.ResetFilterOnTabSwitch,
                    setValue: value => config.ResetFilterOnTabSwitch = value
);

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigResetSearchOnTabSwitchName,
                    tooltip: () => TranslationCache.ConfigResetSearchOnTabSwitchTooltip,
                    getValue: () => config.ResetSearchOnTabSwitch,
                    setValue: value => config.ResetSearchOnTabSwitch = value
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigShowFilterTooltipName,
                    tooltip: () => TranslationCache.ConfigShowFilterTooltipTooltip,
                    getValue: () => config.ShowFilterTooltip,
                    setValue: value => config.ShowFilterTooltip = value
                );

                // Grid Layout section
                gmcmApi.AddSectionTitle(
                    mod: ModManifest,
                    text: () => TranslationCache.ConfigGridLayoutSection
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
                    // Ensure managers are initialized
                    if (categoryManager == null || filterManager == null || templateManager == null)
                    {
                        Monitor.Log("Managers not initialized yet. This shouldn't happen.", LogLevel.Warn);
                        return;
                    }

                    menu = new OutfitMenu(this, categoryManager, filterManager, templateManager, config.ShowItemInfo);
                    Game1.activeClickableMenu = menu;
                }
            }
        }

        internal ModConfig GetConfig() => config;

        /// <summary>
        /// Updates the stored item info preference and saves to config.
        /// </summary>
        internal void SetShowItemInfoPreference(bool value)
        {
            config.ShowItemInfo = value;
            Helper.WriteConfig(config);
        }
    }
}
