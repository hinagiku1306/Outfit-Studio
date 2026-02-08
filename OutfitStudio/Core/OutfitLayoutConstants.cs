using Microsoft.Xna.Framework;

namespace OutfitStudio
{
    /// <summary>
    /// Contains all layout constants for the OutfitMenu UI.
    /// Organized by section-based layout system for maintainability.
    /// </summary>
    public static class OutfitLayoutConstants
    {
        // ============================================================
        // SPACING SYSTEM
        // ============================================================

        /// <summary>Horizontal padding from menu edge to content area.</summary>
        public const int BorderPaddingH = 50;

        /// <summary>Extra right padding for menu balance.</summary>
        public const int BorderPaddingRightExtra = 20;

        /// <summary>Vertical padding from menu top edge to content area.</summary>
        public const int BorderPaddingV = 40;

        /// <summary>Vertical padding from content area to menu bottom edge.</summary>
        public const int BorderPaddingVBottom = 30;

        /// <summary>Horizontal gap between major sections (e.g., left panel to right panel).</summary>
        public const int SectionGapH = 50;

        /// <summary>Vertical gap between major sections.</summary>
        public const int SectionGapV = 30;

        /// <summary>Vertical gap between item grid and bottom buttons (Apply/Reset).</summary>
        public const int GridToButtonGap = 35;

        /// <summary>Gap between elements within a section.</summary>
        public const int ElementGap = 8;

        /// <summary>General inner padding used around content boxes.</summary>
        public const int ContentBoxPadding = 16;

        // ============================================================
        // SECTION HEIGHTS
        // ============================================================

        /// <summary>Height of the tab row section.</summary>
        public const int TabSectionHeight = 48;

        // ============================================================
        // GRID CONFIGURATION (User Configurable via GMCM)
        // ============================================================

        /// <summary>Default number of visible rows in item grid.</summary>
        public const int DefaultVisibleRows = 4;

        /// <summary>Minimum visible rows allowed.</summary>
        public const int MinVisibleRows = 3;

        /// <summary>Maximum visible rows allowed.</summary>
        public const int MaxVisibleRows = 6;

        /// <summary>Default number of visible columns in item grid.</summary>
        public const int DefaultVisibleColumns = 5;

        /// <summary>Minimum visible columns allowed.</summary>
        public const int MinVisibleColumns = 3;

        /// <summary>Maximum visible columns allowed.</summary>
        public const int MaxVisibleColumns = 7;

        /// <summary>Default size of each item slot (width and height).</summary>
        public const int DefaultSlotSize = 80;

        /// <summary>Minimum slot size allowed (slightly larger than DrawnItemSize).</summary>
        public const int MinSlotSize = 68;

        /// <summary>Maximum slot size allowed.</summary>
        public const int MaxSlotSize = 85;

        /// <summary>Gap between item slots in the grid.</summary>
        public const int ItemSlotGap = 4;

        /// <summary>Size of items when drawn via drawInMenu (vanilla standard).</summary>
        public const int DrawnItemSize = 64;

        // ============================================================
        // LEFT PANEL (Character Preview)
        // ============================================================

        /// <summary>Width of the left panel containing character preview.</summary>
        public const int LeftPanelWidth = 180;

        /// <summary>Gap between left panel and right panel (item grid).</summary>
        public const int PanelGap = 40;

        /// <summary>Width of the character preview background box.</summary>
        public const int CharacterPreviewWidth = (int)(128 * FarmerSpriteScale);

        /// <summary>Height of the character preview background box.</summary>
        public const int CharacterPreviewHeight = (int)(192 * FarmerSpriteScale);

        /// <summary>Gap between portrait box and buttons below it.</summary>
        public const int GapBetweenPortraitAndButtons = 16;

        /// <summary>Source rectangle width for background texture (vanilla standard).</summary>
        public const int BackgroundSourceWidth = 64;

        /// <summary>Source rectangle height for background texture (vanilla standard).</summary>
        public const int BackgroundSourceHeight = 96;

        /// <summary>Master scale for character preview (affects both background and farmer).</summary>
        public const float CharacterPreviewScale = 1f;

        /// <summary>Farmer sprite scale.</summary>
        public const float FarmerSpriteScale = 1.2f;

        /// <summary>Width of farmer sprite at standard scale.</summary>
        public const int FarmerSpriteWidth = 16;

        /// <summary>Height of farmer sprite at standard scale.</summary>
        public const int FarmerSpriteHeight = 32;

        /// <summary>Layer depth for farmer sprite rendering.</summary>
        public const float FarmerSpriteLayerDepth = 0.8f;

        // ============================================================
        // TABS AND BUTTONS
        // ============================================================

        /// <summary>Width of category tabs and action buttons (deprecated, use dynamic sizing).</summary>
        public const int TabAndButtonWidth = 110;

        /// <summary>Height of category tabs and action buttons.</summary>
        public const int TabAndButtonHeight = 60;

        /// <summary>Horizontal gap between adjacent tabs.</summary>
        public const int TabAndButtonGap = 8;

        /// <summary>Gap between bottom buttons.</summary>
        public const int BottomButtonGap = 16;

        /// <summary>Padding around text areas.</summary>
        public const int TextPadding = 15;

        /// <summary>Height reserved for the menu title text area.</summary>
        public const int TitleAreaHeight = 48;

        /// <summary>Tab opacity when inactive.</summary>
        public const float TabOpacity = 0.2f;

        // ============================================================
        // DIRECTION ARROWS (Left Panel)
        // ============================================================

        /// <summary>Native width of arrow sprite in source texture.</summary>
        public const int ArrowNativeWidth = 12;

        /// <summary>Native height of arrow sprite in source texture.</summary>
        public const int ArrowNativeHeight = 11;

        /// <summary>Scale factor for direction arrows (0.8x of original 4x scale).</summary>
        public const float ArrowScale = 3.2f;

        /// <summary>Gap between left and right arrow buttons.</summary>
        public const int ArrowGap = 12;

        // ============================================================
        // SCROLL ARROWS & CLOSE
        // ============================================================

        /// <summary>Size of scroll arrow buttons (width and height).</summary>
        public const int ScrollArrowButtonSize = 44;

        /// <summary>Scale for grid scroll arrows.</summary>
        public const float GridScrollArrowScale = 2.5f;

        /// <summary>Gap between grid box right edge and scroll arrows.</summary>
        public const int GridScrollArrowGap = 8;

        /// <summary>Size of the close button (width and height).</summary>
        public const int CloseButtonSize = 48;

        /// <summary>Margin from menu edge to close button.</summary>
        public const int CloseButtonEdgeMargin = 8;

        // ============================================================
        // HOVER EFFECTS
        // ============================================================

        /// <summary>Scale increase when hovering over buttons.</summary>
        public const float ButtonHoveringScale = 1.05f;

        /// <summary>Default hover effect color for tabs and buttons.</summary>
        public static readonly Color HoverEffectColor = Color.Wheat * 0.3f;

        // ============================================================
        // SAVED MESSAGE
        // ============================================================

        /// <summary>Duration to show "Saved!" message in milliseconds.</summary>
        public const float SavedMessageDurationMs = 1000f;

        /// <summary>Vertical offset above portrait box for saved message.</summary>
        public const int SavedMessageOffsetAbovePreview = 8;

        // ============================================================
        // OVERLAY
        // ============================================================

        /// <summary>Opacity of the dark background overlay (0-1).</summary>
        public const float BackgroundOverlayOpacity = 0.6f;

        // ============================================================
        // SPECIAL ITEM IDs
        // ============================================================

        /// <summary>Item ID representing "no hat" option.</summary>
        public const string NoHatId = "-1";

        /// <summary>Item ID representing "no shirt" (unequipped).</summary>
        public const string NoShirtId = "-1";

        /// <summary>Item ID representing "no pants" (unequipped).</summary>
        public const string NoPantsId = "-1";

        // ============================================================
        // FILTER & SEARCH
        // ============================================================

        /// <summary>Gap between filter dropdown and search bar.</summary>
        public const int FilterSearchGap = 12;

        /// <summary>Fixed width for filter dropdown.</summary>
        public const int FilterDropdownWidth = 200;

        /// <summary>Padding between text and its box.</summary>
        public const int FilterTextPadding = 12;

        // ============================================================
        // CLEAR BUTTON (for dropdown and search bar)
        // ============================================================

        /// <summary>Size of clear button icon.</summary>
        public const int ClearButtonSize = 24;

        /// <summary>Margin from right edge to clear button.</summary>
        public const int ClearButtonRightMargin = 12;

        public const int DiceButtonSize = 40;
        public const int DiceButtonGap = 10;

        // ============================================================
        // LOOKUP ICON
        // ============================================================

        /// <summary>Size of the lookup icon button.</summary>
        public const int LookupIconSize = 32;

        /// <summary>Margin from portrait box edge to lookup icon.</summary>
        public const int LookupIconMargin = 4;

        // ============================================================
        // TOOLTIP
        // ============================================================

        /// <summary>Text padding for tooltip box.</summary>
        public const int TooltipPadding = 16;

        // ============================================================
        // WARDROBE OVERLAY DIMENSIONS
        // ============================================================
        // Note: Overlay width/height and panel widths are calculated dynamically in WardrobeUIBuilder.

        /// <summary>Gap between left and right panels.</summary>
        public const int WardrobePanelGap = 50;

        /// <summary>Height of each outfit list item.</summary>
        public const int WardrobeListItemHeight = 48;

        /// <summary>Size of preview box in wardrobe.</summary>
        public const int WardrobePreviewSize = 256;

        /// <summary>Height of bottom button area in wardrobe.</summary>
        public const int WardrobeBottomButtonHeight = 70;

        /// <summary>Height of each row in filter bar.</summary>
        public const int WardrobeFilterRowHeight = 44;

        /// <summary>Gap between filter bar elements.</summary>
        public const int WardrobeFilterGap = 8;

        /// <summary>Size of checkbox in filter bar.</summary>
        public const int WardrobeCheckboxSize = 29;

        /// <summary>Max visible items in filter dropdowns.</summary>
        public const int WardrobeDropdownMaxVisible = 5;

        // ============================================================
        // SAVE SET OVERLAY DIMENSIONS
        // ============================================================

        /// <summary>Total width of save set overlay window.</summary>
        public const int SaveSetOverlayWidth = 600;

        /// <summary>Padding above and below each section.</summary>
        public const int SaveSetSectionPadding = 12;

        /// <summary>Character preview width (matches day/night bg).</summary>
        public const int SaveSetPreviewWidth = 140;

        /// <summary>Character preview height (matches day/night bg).</summary>
        public const int SaveSetPreviewHeight = 192;

        /// <summary>Size of item slots for clothing sprites.</summary>
        public const int SaveSetItemSlotSize = 70;

        /// <summary>Gap between item slots.</summary>
        public const int SaveSetItemSlotGap = 4;

        /// <summary>Gap between character preview and item slots.</summary>
        public const int SaveSetPreviewToSlotsGap = 25;

        /// <summary>Size of favorite checkbox.</summary>
        public const int SaveSetCheckboxSize = 36;

        /// <summary>Scale for direction arrows in wardrobe preview (matches wardrobe scroll arrow size).</summary>
        public const float WardrobeArrowScale = 2.75f;

        /// <summary>Gap between Save and Cancel buttons.</summary>
        public const int SaveSetButtonGap = 20;

        /// <summary>Border padding for texture box content area.</summary>
        public const int SaveSetBorderPadding = 30;

        /// <summary>Save Set slot color when excluded or empty.</summary>
        public static readonly Color SaveSetExcludedItemSlotColor = Color.Black * 0.3f;

        /// <summary>Small Button height.</summary>
        public const int SmallButtonHeight = 50;

        /// <summary>Left padding before the Tags label in SaveSet overlay.</summary>
        public const int SaveSetTagsLeftPadding = 40;

        /// <summary>Scale for LocalOnly checkbox (0.8x of standard 4f scale).</summary>
        public const float SaveSetLocalOnlyCheckboxScale = 3.2f;

        /// <summary>Size of LocalOnly checkbox (9 * scale).</summary>
        public const int SaveSetLocalOnlyCheckboxSize = 29;

        // ============================================================
        // TEXTURE BOX VISUAL CENTERING
        // ============================================================

        /// <summary>
        /// Vertical offset to visually center content in texture boxes.
        /// The standard menu texture (0,256,60,60) has asymmetric borders:
        /// top=36px, bottom=40px. This 4px difference shifts visual center down by 2px.
        /// Positive value shifts content DOWN to compensate.
        /// </summary>
        public const int TextureBoxVisualOffsetY = 2;

        // ============================================================
        // MENU TEXTURE SOURCE RECTANGLES
        // ============================================================

        /// <summary>Source rectangle for standard dialog/menu box 9-slice in Game1.menuTexture.</summary>
        public static readonly Rectangle MenuBoxSourceRect = new Rectangle(0, 256, 60, 60);

        // ============================================================
        // TAG EDIT MODE
        // ============================================================

        /// <summary>Size of the edit mode toggle button.</summary>
        public const int EditModeButtonSize = 28;

        /// <summary>Size of tag delete button (X icon).</summary>
        public const int TagDeleteButtonSize = 20;

        /// <summary>Opacity for disabled sections in edit mode.</summary>
        public const float DisabledSectionOpacity = 0.5f;

        /// <summary>Opacity for tags marked for deletion.</summary>
        public const float MarkedForDeletionOpacity = 0.2f;

        // ============================================================
        // CONFIG OVERLAY
        // ============================================================

        public const int ConfigOverlayWidth = 650;
        public const int ConfigBorderPadding = 30;
        public const int ConfigRowHeight = 44;
        public const int ConfigSectionHeaderHeight = 44;
        public const int ConfigSectionGap = 20;
        public const int ConfigRowIndent = 20;
        public const int ConfigLabelWidth = 350;
        public const int ConfigControlRightPadding = 30;

        public const float ConfigCheckboxScale = 3.2f;
        public const int ConfigCheckboxSize = 29;

        public const int ConfigSliderWidth = 200;
        public const int ConfigSliderHeight = 24;
        public const int ConfigSliderValueLabelGap = 15;

        public const int ConfigKeybindBoxWidth = 140;

        public const int ConfigDropdownWidth = 85;
        public const int ConfigDropdownHeight = 40;
        public const int ConfigDropdownMaxVisible = 3;

        public const int ConfigGearButtonSize = 40;
        public const int ConfigGearButtonGap = 5;

        public const int ConfigBottomButtonGap = 16;

        public const int ConfigMaxHeight = 700;
        public const float ConfigScrollArrowScale = 2f;
        public const int ConfigScrollArrowRightExtend = 5;

        public const int ConfigTitleTopPadding = 15;
        public const int ConfigTitleBottomPadding = 10;
        public const int ConfigTitleSidePadding = 30;
        public const int ConfigButtonBoxHeight = 70;
        public const int ConfigBoxGap = 10;

        // ============================================================
        // DYE COLOR PANEL
        // ============================================================

        public const int DyeColorPanelWidth = 280;
        public const int DyeColorPanelPadding = 30;
        public const int DyeColorSliderTrackHeight = 4;
        public const int DyeColorSliderRowHeight = 28;
        public const int DyeColorSliderGap = 12;
        public const int DyeColorGradientChunks = 24;
    }
}
