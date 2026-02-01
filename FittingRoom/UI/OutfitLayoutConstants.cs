namespace FittingRoom
{
    /// <summary>
    /// Contains all layout constants for the OutfitMenu UI.
    /// Organized by component/purpose for easy maintenance.
    /// </summary>
    public static class OutfitLayoutConstants
    {
        // ============================================================
        // MENU DIMENSIONS
        // ============================================================

        /// <summary>Total height of the outfit menu in pixels.</summary>
        public const int MenuTotalHeight = 700;

        /// <summary>Horizontal padding from menu edge to content area.</summary>
        public const int MenuSidePadding = 70;

        /// <summary>General inner padding used around content boxes.</summary>
        public const int ContentBoxPadding = 16;

        /// <summary>Height reserved for the menu title text area.</summary>
        public const int TitleAreaHeight = 48;

        /// <summary>Height reserved at bottom for action buttons.</summary>
        public const int BottomButtonAreaHeight = 60;

        // ============================================================
        // CHARACTER PREVIEW PANEL (Left Side)
        // ============================================================

        /// <summary>Width of the character preview background box.</summary>
        public const int CharacterPreviewWidth = (int)(128 * FarmerSpriteScale);

        /// <summary>Height of the character preview background box.</summary>
        public const int CharacterPreviewHeight = (int)(192 * FarmerSpriteScale);

        /// <summary>Horizontal gap between character preview and item grid.</summary>
        public const int GapBetweenPreviewAndGrid = 100;

        /// <summary>Source rectangle width for background texture (vanilla standard).</summary>
        public const int BackgroundSourceWidth = 64;

        /// <summary>Source rectangle height for background texture (vanilla standard).</summary>
        public const int BackgroundSourceHeight = 96;

        /// <summary>Master scale for character preview (affects both background and farmer).</summary>
        public const float CharacterPreviewScale = 1f;

        /// <summary>Farmer sprite scale.</summary>
        public const float FarmerSpriteScale = 1.3f;

        /// <summary>Width of farmer sprite at standard scale.</summary>
        public const int FarmerSpriteWidth = 64;

        /// <summary>Height of farmer sprite at standard scale.</summary>
        public const int FarmerSpriteHeight = 128;

        /// <summary>Layer depth for farmer sprite rendering.</summary>
        public const float FarmerSpriteLayerDepth = 0.8f;

        /// <summary>Gap between portrait box and buttons below it.</summary>
        public const int GapBetweenPortraitAndButtons = 50;

        /// <summary>Time of day when night background starts (24-hour format * 100).</summary>
        public const int NightTimeStartHour = 1900;

        // ============================================================
        // ITEM GRID (Right Side)
        // ============================================================

        /// <summary>Size of each item slot in the grid (width and height).</summary>
        public const int ItemSlotSize = 85;

        /// <summary>Gap between item slots in the grid.</summary>
        public const int ItemSlotGap = 4;

        /// <summary>Number of columns in the item grid.</summary>
        public const int ItemGridColumns = 5;

        /// <summary>Size of items when drawn via drawInMenu (vanilla standard).</summary>
        public const int DrawnItemSize = 64;

        // ============================================================
        // CATEGORY TABS AND ACTION BUTTONS (Shared dimensions)
        // ============================================================

        /// <summary>Width of category tabs and action buttons (Apply/Reset).</summary>
        public const int TabAndButtonWidth = 110;

        /// <summary>Height of category tabs and action buttons.</summary>
        public const int TabAndButtonHeight = 60;

        /// <summary>Horizontal gap between adjacent tabs or buttons.</summary>
        public const int TabAndButtonGap = 8;

        /// <summary>Tab opacity.</summary>
        public const float TabOpacity = 0.3f;

        // ============================================================
        // SCROLL ARROWS
        // ============================================================

        /// <summary>Size of scroll arrow buttons (width and height).</summary>
        public const int ScrollArrowButtonSize = 44;

        /// <summary>Vertical gap between up and down scroll arrows.</summary>
        public const int ScrollArrowVerticalGap = 50;

        /// <summary>Left gap between outfit menu and arrow.</summary>
        public const int ScrollArrowLeftGap = 20;

        // ============================================================
        // CLOSE BUTTON
        // ============================================================

        /// <summary>Size of the close button (width and height).</summary>
        public const int CloseButtonSize = 48;

        /// <summary>Margin from menu edge to close button.</summary>
        public const int CloseButtonEdgeMargin = 8;

        // ============================================================
        // HOVER EFFECTS
        // ============================================================

        /// <summary>Scale increase when hovering over buttons.</summary>
        public const float ButtonHoverScaleIncrease = 0.5f;

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
    }
}
