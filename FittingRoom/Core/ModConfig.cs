using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace FittingRoom
{
    public class ModConfig
    {
        public KeybindList ToggleMenuKey { get; set; } = KeybindList.Parse("O");
        public KeybindList ToggleItemInfoKey { get; set; } = KeybindList.Parse("I");
        public bool ShowItemInfo { get; set; } = false;
        public bool ResetFilterOnTabSwitch { get; set; } = false;
        public bool ResetSearchOnTabSwitch { get; set; } = false;
        public bool ShowFilterTooltip { get; set; } = true;

        // Grid layout configuration
        public int VisibleRows { get; set; } = OutfitLayoutConstants.DefaultVisibleRows;
        public int VisibleColumns { get; set; } = OutfitLayoutConstants.DefaultVisibleColumns;
        public int SlotSize { get; set; } = OutfitLayoutConstants.DefaultSlotSize;

        // Dev-only: Enable debug/trace logging (manually edit config.json)
        public bool EnableDebugLogging { get; set; } = false;
    }
}
