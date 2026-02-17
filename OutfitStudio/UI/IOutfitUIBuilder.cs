using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley.Menus;

namespace OutfitStudio
{
    public interface IOutfitUIBuilder
    {
        int SLOT_SIZE { get; }
        int VISIBLE_ITEMS { get; }
        int VISIBLE_ROWS { get; }
        int COLUMNS { get; }

        ClickableComponent HairTab { get; }
        ClickableComponent AllTab { get; }
        ClickableComponent ShirtsTab { get; }
        ClickableComponent PantsTab { get; }
        ClickableComponent HatsTab { get; }

        ClickableTextureComponent CloseButton { get; }
        ClickableComponent? ApplyButton { get; }
        ClickableComponent ResetButton { get; }
        ClickableComponent HideHatCheckbox { get; }

        ClickableComponent? SaveButton { get; }
        ClickableComponent? WardrobeButton { get; }
        ClickableTextureComponent? GearButton { get; }
        ClickableComponent? ScheduleButton { get; }
        ClickableComponent? DebugLogButton { get; }

        ClickableTextureComponent DyeColorButton { get; }

        ClickableTextureComponent LeftArrowButton { get; }
        ClickableTextureComponent RightArrowButton { get; }

        ClickableTextureComponent GridScrollUpButton { get; }
        ClickableTextureComponent GridScrollDownButton { get; }

        ClickableComponent? ModFilterDropdown { get; }
        ClickableComponent? SearchBar { get; }
        ClickableComponent? FilterClearButton { get; }
        ClickableComponent? SearchClearButton { get; }
        ClickableComponent? LookupButton { get; }

        List<ClickableComponent> ItemSlots { get; }
        Rectangle PortraitBox { get; }
        Rectangle EquippedTextArea { get; }

        int X { get; }
        int Y { get; }
        int Width { get; }
        int Height { get; }

        void RotatePreviewLeft();
        void RotatePreviewRight();
    }
}
