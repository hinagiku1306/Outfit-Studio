using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace OutfitStudio
{
    public class ModConfig
    {
        public KeybindList ToggleMenuKey { get; set; } = KeybindList.Parse("O");
        public KeybindList ToggleItemInfoKey { get; set; } = KeybindList.Parse("End");
        public bool ShowItemInfo { get; set; } = true;
        public bool CloseOnClickOutside { get; set; } = false;
        public bool ResetFilterOnTabSwitch { get; set; } = false;
        public bool ResetSearchOnTabSwitch { get; set; } = false;
        public bool ShowTooltip { get; set; } = true;
        public bool AutoOpenTagMenu { get; set; } = true;
        public bool AutoFocusSearchBar { get; set; } = true;
        public bool ArrowKeyScrolling { get; set; } = true;
        public bool AutoOpenDyeColorMenu { get; set; } = true;
        public bool ResetMatchAllOnOpen { get; set; } = false;
        public bool ResetShowInvalidOnOpen { get; set; } = false;
        public string DefaultSearchScope { get; set; } = "Set";

        // Grid layout configuration
        public int VisibleRows { get; set; } = OutfitLayoutConstants.DefaultVisibleRows;
        public int VisibleColumns { get; set; } = OutfitLayoutConstants.DefaultVisibleColumns;
        public int SlotSize { get; set; } = OutfitLayoutConstants.DefaultSlotSize;

        // Dev-only: Enable debug/trace logging (manually edit config.json)
        public bool EnableDebugLogging { get; set; } = false;
    }
}
