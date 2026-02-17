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
        private static IModHelper StaticHelper = null!;

        private OutfitFilterManager? filterManager;
        private OutfitCategoryManager? categoryManager;
        private OutfitSetStore? outfitSetStore;
        private ScheduleStore? scheduleStore;
        private ScheduleEngine? scheduleEngine;
        private ScheduleEvalLog? scheduleEvalLog;

        public override void Entry(IModHelper helper)
        {
            config = helper.ReadConfig<ModConfig>();
            Config = config;
            StaticHelper = helper;

            DebugLogger.Initialize(Monitor, config);
            TranslationCache.Initialize(helper.Translation);
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.DayEnding += OnDayEnding;
            helper.Events.Player.Warped += OnWarped;
            helper.Events.Content.LocaleChanged += OnLocaleChanged;
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            filterManager = new OutfitFilterManager(Monitor, Helper);
            categoryManager = new OutfitCategoryManager(Monitor, filterManager);
            filterManager.BuildModMapping(categoryManager.ShirtIds, categoryManager.PantsIds, categoryManager.HatIds);

            outfitSetStore?.LoadLocalData();
            scheduleStore?.LoadLocalData();
            scheduleEngine?.InvalidateFestivalCache();
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            outfitSetStore?.ClearLocalData();
            scheduleStore?.ClearLocalData();
            scheduleEngine?.ResetForNewDay();
        }

        private void OnLocaleChanged(object? sender, LocaleChangedEventArgs e)
        {
            TranslationCache.Initialize(Helper.Translation);
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {

            outfitSetStore = new OutfitSetStore(Helper, Monitor);
            outfitSetStore.LoadGlobalData();

            scheduleStore = new ScheduleStore(Helper, outfitSetStore);
            scheduleEvalLog = new ScheduleEvalLog();
            scheduleEngine = new ScheduleEngine(scheduleStore, outfitSetStore, scheduleEvalLog, () => Config.ConsistentTiebreaks, () => Config.LockManualOutfit);
            scheduleStore.OnRulesChanged = (ruleId) => scheduleEngine.InvalidateForRule(ruleId);
            outfitSetStore.OnSetsChanged = () =>
            {
                scheduleStore.PruneStaleSelectedSetIds();
                scheduleEngine.InvalidateForSetsChanged();
            };

            var gmcmApi = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcmApi != null)
            {
                gmcmApi.Register(
                    mod: ModManifest,
                    reset: () => { config = new ModConfig(); Config = config; },
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

                gmcmApi.AddKeybindList(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigToggleWardrobeKeyName,
                    tooltip: () => TranslationCache.ConfigToggleWardrobeKeyTooltip,
                    getValue: () => config.ToggleWardrobeKey,
                    setValue: value => config.ToggleWardrobeKey = value
                );

                gmcmApi.AddKeybindList(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigToggleScheduleKeyName,
                    tooltip: () => TranslationCache.ConfigToggleScheduleKeyTooltip,
                    getValue: () => config.ToggleScheduleKey,
                    setValue: value => config.ToggleScheduleKey = value
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

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigIncludeHairInOutfitSetsName,
                    tooltip: () => TranslationCache.ConfigIncludeHairInOutfitSetsTooltip,
                    getValue: () => config.IncludeHairInOutfitSets,
                    setValue: value => config.IncludeHairInOutfitSets = value
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

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigShowScheduleDebugLogName,
                    tooltip: () => TranslationCache.ConfigShowScheduleDebugLogTooltip,
                    getValue: () => config.ShowScheduleDebugLog,
                    setValue: value => config.ShowScheduleDebugLog = value
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

                gmcmApi.AddSectionTitle(
                    mod: ModManifest,
                    text: () => TranslationCache.ConfigScheduleSection
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigConsistentTiebreaksName,
                    tooltip: () => TranslationCache.ConfigConsistentTiebreaksTooltip,
                    getValue: () => config.ConsistentTiebreaks,
                    setValue: value =>
                    {
                        bool changed = config.ConsistentTiebreaks != value;
                        config.ConsistentTiebreaks = value;
                        if (changed) scheduleEngine?.InvalidateContextCache();
                    }
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigLockManualOutfitName,
                    tooltip: () => TranslationCache.ConfigLockManualOutfitTooltip,
                    getValue: () => config.LockManualOutfit,
                    setValue: value => config.LockManualOutfit = value
                );

                gmcmApi.AddNumberOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigDefaultPriorityName,
                    tooltip: () => TranslationCache.ConfigDefaultPriorityTooltip,
                    getValue: () => config.DefaultPriority,
                    setValue: value => config.DefaultPriority = value,
                    min: 1,
                    max: 3,
                    formatValue: value => value switch
                    {
                        1 => TranslationCache.ScheduleEditPriorityLow,
                        3 => TranslationCache.ScheduleEditPriorityHigh,
                        _ => TranslationCache.ScheduleEditPriorityMedium
                    }
                );

                gmcmApi.AddTextOption(
                    mod: ModManifest,
                    name: () => TranslationCache.ConfigDefaultRotationName,
                    tooltip: () => TranslationCache.ConfigDefaultRotationTooltip,
                    getValue: () => config.DefaultAdvanceOnWarp ? "OnWarp" : "Daily",
                    setValue: value => config.DefaultAdvanceOnWarp = value == "OnWarp",
                    allowedValues: new[] { "Daily", "OnWarp" },
                    formatAllowedValue: value => value switch
                    {
                        "OnWarp" => TranslationCache.ScheduleEditRotateOnLocationChange,
                        _ => TranslationCache.ScheduleEditRotateOnceADay
                    }
                );

            }
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            scheduleEngine?.ResetForNewDay();
            scheduleEngine?.Evaluate(EvaluationTrigger.DayStarted);
        }

        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            scheduleStore?.SaveLocalData();
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (!e.IsLocalPlayer) return;
            if (e.NewLocation.Name == "Temp") return;
            scheduleEngine?.Evaluate(EvaluationTrigger.Warped);
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

            if (config.ToggleWardrobeKey.JustPressed())
            {
                if (Game1.activeClickableMenu is WardrobeOverlay)
                {
                    Game1.exitActiveMenu();
                }
                else if (Game1.activeClickableMenu is OutfitMenu || Game1.activeClickableMenu == null)
                {
                    if (outfitSetStore == null)
                        return;

                    var parent = Game1.activeClickableMenu as OutfitMenu;
                    Game1.activeClickableMenu = new WardrobeOverlay(outfitSetStore, parent,
                        onOutfitApplied: set =>
                        {
                            if (parent != null)
                            {
                                parent.NotifyOutfitApplied(set);
                            }
                            else
                            {
                                bool includeHair = Config.IncludeHairInOutfitSets;
                                scheduleEngine?.SetManualOutfit(ManualOutfitSnapshot.FromOutfitSet(set, includeHair));
                            }
                        });
                    Game1.playSound("bigSelect");
                }
            }

            if (config.ToggleScheduleKey.JustPressed())
            {
                if (Game1.activeClickableMenu is ScheduleMenu)
                {
                    Game1.exitActiveMenu();
                }
                else if (Game1.activeClickableMenu is OutfitMenu || Game1.activeClickableMenu == null)
                {
                    if (outfitSetStore == null || scheduleStore == null)
                        return;

                    var parent = Game1.activeClickableMenu as OutfitMenu;
                    Game1.activeClickableMenu = new ScheduleMenu(parent, this, outfitSetStore, scheduleStore);
                    Game1.playSound("bigSelect");
                }
            }
        }

        internal ScheduleStore? GetScheduleStore() => scheduleStore;

        internal ScheduleEngine? GetScheduleEngine() => scheduleEngine;

        internal ScheduleEvalLog? GetScheduleEvalLog() => scheduleEvalLog;

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

        internal static void PersistConfig() => StaticHelper.WriteConfig(Config);
    }
}
