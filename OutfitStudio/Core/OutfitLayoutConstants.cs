using Microsoft.Xna.Framework;

namespace OutfitStudio
{
    public static class OutfitLayoutConstants
    {
        public const int BorderPaddingH = 50;
        public const int BorderPaddingRightExtra = 20;
        public const int BorderPaddingV = 40;
        public const int BorderPaddingVBottom = 30;
        public const int SectionGapH = 50;
        public const int SectionGapV = 30;
        public const int GridToButtonGap = 35;
        public const int ElementGap = 8;
        public const int ContentBoxPadding = 16;

        public const int TabSectionHeight = 48;

        public const int DefaultVisibleRows = 4;
        public const int MinVisibleRows = 3;
        public const int MaxVisibleRows = 6;
        public const int DefaultVisibleColumns = 5;
        public const int MinVisibleColumns = 3;
        public const int MaxVisibleColumns = 7;
        public const int DefaultSlotSize = 80;
        public const int MinSlotSize = 68;
        public const int MaxSlotSize = 85;
        public const int ItemSlotGap = 4;
        public const int DrawnItemSize = 64;

        public const int LeftPanelWidth = 180;
        public const int PanelGap = 40;
        public const int CharacterPreviewWidth = (int)(128 * FarmerSpriteScale);
        public const int CharacterPreviewHeight = (int)(192 * FarmerSpriteScale);
        public const int GapBetweenPortraitAndButtons = 16;
        public const int BackgroundSourceWidth = 64;
        public const int BackgroundSourceHeight = 96;
        public const float CharacterPreviewScale = 1f;
        public const float FarmerSpriteScale = 1.2f;
        public const int FarmerSpriteWidth = 16;
        public const int FarmerSpriteHeight = 32;
        public const float FarmerSpriteLayerDepth = 0.8f;

        public const int TabAndButtonWidth = 110;
        public const int TabAndButtonHeight = 60;
        public const int TabAndButtonGap = 8;
        public const int BottomButtonGap = 16;
        public const int TextPadding = 15;
        public const int TitleAreaHeight = 48;
        public const float TabOpacity = 0.2f;

        public const int ArrowNativeWidth = 12;
        public const int ArrowNativeHeight = 11;
        public const float ArrowScale = 3.2f;
        public const int ArrowGap = 12;

        public const int ScrollArrowButtonSize = 44;
        public const float GridScrollArrowScale = 2.5f;
        public const int GridScrollArrowGap = 8;
        public const int CloseButtonSize = 48;
        public const int CloseButtonEdgeMargin = 8;

        public const float ButtonHoveringScale = 1.05f;
        public static readonly Color HoverEffectColor = Color.Wheat * 0.3f;

        public const float SavedMessageDurationMs = 1000f;
        public const int SavedMessageOffsetAbovePreview = 8;

        public const float BackgroundOverlayOpacity = 0.6f;

        public const string NoHatId = "-1";
        public const string NoShirtId = "-1";
        public const string NoPantsId = "-1";

        public const int FilterSearchGap = 12;
        public const int FilterDropdownWidth = 200;
        public const int FilterTextPadding = 12;

        public const int ClearButtonSize = 24;
        public const int ClearButtonRightMargin = 12;
        public const int DropdownArrowPad = 8;
        public const float DropdownArrowScale = 1.5f;
        public const int InputBarTextStartX = 20;
        public const int InputBarCaretWidth = 4;
        public const int DiceButtonSize = 40;
        public const int DiceButtonGap = 10;

        public const int LookupIconSize = 32;
        public const int LookupIconMargin = 4;

        public const int TooltipPadding = 16;

        public const int WardrobePanelGap = 50;
        public const int WardrobeListItemHeight = 48;
        public const int WardrobePreviewSize = 256;
        public const int WardrobeBottomButtonHeight = 70;
        public const int WardrobeFilterRowHeight = 44;
        public const int WardrobeFilterGap = 8;
        public const int WardrobeCheckboxSize = 29;
        public const int WardrobeDropdownMaxVisible = 5;

        public const int SaveSetOverlayWidth = 600;
        public const int SaveSetSectionPadding = 12;
        public const int SaveSetPreviewWidth = 140;
        public const int SaveSetPreviewHeight = 192;
        public const int SaveSetItemSlotSize = 70;
        public const int SaveSetItemSlotGap = 4;
        public const int SaveSetPreviewToSlotsGap = 25;
        public const int SaveSetCheckboxSize = 36;
        public const float WardrobeArrowScale = 2.75f;
        public const int SaveSetButtonGap = 20;
        public const int SaveSetBorderPadding = 30;
        public static readonly Color SaveSetExcludedItemSlotColor = Color.Black * 0.3f;
        public const int SmallButtonHeight = 50;
        public const int SaveSetTagsLeftPadding = 40;
        public const float SaveSetLocalOnlyCheckboxScale = 3.2f;
        public const int SaveSetLocalOnlyCheckboxSize = 29;

        // Menu texture (0,256,60,60) has asymmetric borders: top=36px, bottom=40px.
        // This 2px offset compensates for the visual center shift.
        public const int TextureBoxVisualOffsetY = 2;

        public static readonly Rectangle MenuBoxSourceRect = new Rectangle(0, 256, 60, 60);

        public const int EditModeButtonSize = 28;
        public const int TagDeleteButtonSize = 20;
        public const float DisabledSectionOpacity = 0.5f;
        public const float MarkedForDeletionOpacity = 0.2f;

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

        public const int ScheduleMenuWidth = 700;
        public const int ScheduleRuleRowHeight = 48;
        public const int ScheduleMaxVisibleRules = 4;
        public const int ScheduleInfoButtonSize = 28;
        public const int ScheduleCheckboxGap = 10;
        public const int ScheduleNameToInfoGap = 8;
        public const int ScheduleBorderPadding = 40;
        public const int ScheduleScrollArrowRightPadding = 10;
        public const int ScheduleNameIndent = 20;
        public const int ScheduleSectionGap = 28;
        public const int ScheduleItemGap = 6;
        public const int ScheduleMinVisibleRules = 4;
        public const int ScheduleBottomButtonGap = 16;
        public const int ScheduleButtonBoxHeight = 70;
        public const int ScheduleBoxGap = 10;
        public const int ScheduleTitleTopPadding = 15;
        public const int ScheduleTitleBottomPadding = 10;
        public const int ScheduleTitleSidePadding = 30;
        public const float ScheduleCheckboxScale = 3.2f;
        public const int ScheduleCheckboxSize = 29;
        public const float ScheduleScrollArrowScale = 2f;
        public const int ScheduleSearchBarHeight = 60;
        public const int SchedulePriorityDropdownGap = 12;
        public const int SchedulePriorityOptionHeight = 40;
        public const int ScheduleSearchTextStartX = 24;
        public const int ScheduleSearchPlaceholderStartX = 26;
        public const int ScheduleSearchDividerGap = 16;
        public const int ScheduleDividerHeight = 2;
        public const int ScheduleRuleListBottomPad = 10;

        public const int ScheduleEditWidth = 700;
        public const int ScheduleEditRowHeight = 48;
        public const int ScheduleEditLabelWidth = 110;
        public const int ScheduleEditDropdownMaxVisible = 5;
        public const int ScheduleEditDropdownOptionHeight = 40;
        public const int ScheduleEditSectionGap = 8;
        public const int ScheduleEditTriggerIndent = 20;
        public const int ScheduleEditColumnGap = 20;
        public const int ScheduleEditDropdownPanelPadding = 5;
        public const int ScheduleEditDeleteButtonSize = 20;
        public const int ScheduleEditSectionHeaderHeight = 30;
        public const int ScheduleEditBarRowGap = 10;
        public const int ScheduleEditOptionRowGap = 10;
        public const int ScheduleEditOptionsLabelToBarGap = 15;
        public const int ScheduleEditClearButtonLeftShift = 8;

        public const int SetPreviewWidth = 750;
        public const int SetPreviewPanelGap = 30;
        public const int SetPreviewRowHeight = 44;
        public const int SetPreviewMaxVisibleSets = 6;
        public const int SetPreviewHeaderHeight = 36;
        public const int SetPreviewNameToButtonGap = 8;
        public const int SetPreviewTriggerPanelWidth = 200;
        public const int SetPreviewTriggerIndent = 15;
        public const int SetPreviewTriggerLineHeight = 28;
        public const int SetPreviewDividerPadding = 4;

        public const int DyeColorPanelWidth = 280;
        public const int DyeColorPanelPadding = 30;
        public const int DyeColorSliderTrackHeight = 4;
        public const int DyeColorSliderRowHeight = 28;
        public const int DyeColorSliderGap = 12;
        public const int DyeColorGradientChunks = 24;
    }
}
