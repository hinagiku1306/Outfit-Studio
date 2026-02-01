using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace FittingRoom
{
    public class ModConfig
    {
        public KeybindList ToggleMenuKey { get; set; } = KeybindList.Parse("O");
        public KeybindList ToggleItemInfoKey { get; set; } = KeybindList.Parse("I");
        public bool ShowItemInfo { get; set; } = false;
    }
}
