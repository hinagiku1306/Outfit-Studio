using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace OutfitRoom
{
    public class ModEntry : Mod
    {
        private ModConfig config;
        private OutfitMenu menu;

        public override void Entry(IModHelper helper)
        {
            config = helper.ReadConfig<ModConfig>();
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
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
                    name: () => "Toggle Menu Key",
                    tooltip: () => "Press this key to open/close the outfit tester menu.",
                    getValue: () => config.ToggleMenuKey,
                    setValue: value => config.ToggleMenuKey = value
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    name: () => "Enable Outfit Saving",
                    tooltip: () => "Allow saving and loading of outfits.",
                    getValue: () => config.EnableOutfitSaving,
                    setValue: value => config.EnableOutfitSaving = value
                );

                gmcmApi.AddNumberOption(
                    mod: ModManifest,
                    name: () => "Max Saved Outfits",
                    tooltip: () => "Maximum number of outfits you can save.",
                    getValue: () => config.MaxSavedOutfits,
                    setValue: value => config.MaxSavedOutfits = value,
                    min: 1,
                    max: 50
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    name: () => "Show Preview",
                    tooltip: () => "Show a preview of the outfit before applying.",
                    getValue: () => config.ShowPreview,
                    setValue: value => config.ShowPreview = value
                );
            }
        }

        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
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
                    menu = new OutfitMenu(this);
                    Game1.activeClickableMenu = menu;
                }
            }
        }

        internal ModConfig GetConfig() => config;
    }
}