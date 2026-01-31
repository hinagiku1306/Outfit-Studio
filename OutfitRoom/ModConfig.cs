using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace OutfitRoom
{
    public class ModConfig
    {
        public KeybindList ToggleMenuKey { get; set; } = KeybindList.Parse("O");
        public bool EnableOutfitSaving { get; set; } = true;
        public int MaxSavedOutfits { get; set; } = 10;
        public bool ShowPreview { get; set; } = true;
    }
}