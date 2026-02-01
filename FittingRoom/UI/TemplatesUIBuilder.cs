using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using static FittingRoom.OutfitLayoutConstants;

namespace FittingRoom
{
    /// <summary>
    /// Builds UI layout for Templates overlay (placeholder).
    /// </summary>
    public class TemplatesUIBuilder
    {
        public Rectangle LeftPanel { get; private set; }
        public Rectangle RightPanel { get; private set; }
        public ClickableComponent FilterDropdown { get; private set; } = null!;
        public ClickableComponent SortDropdown { get; private set; } = null!;
        public ClickableComponent SearchBar { get; private set; } = null!;
        public List<ClickableComponent> TemplateListItems { get; private set; } = new();
        public Rectangle PreviewBox { get; private set; }

        public ClickableComponent LoadButton { get; private set; } = null!;
        public ClickableComponent ApplyCloseButton { get; private set; } = null!;
        public ClickableComponent NewButton { get; private set; } = null!;
        public ClickableComponent DuplicateButton { get; private set; } = null!;
        public ClickableComponent RenameButton { get; private set; } = null!;
        public ClickableComponent DeleteButton { get; private set; } = null!;
        public ClickableComponent FavoriteButton { get; private set; } = null!;
        public ClickableTextureComponent CloseButton { get; private set; } = null!;

        private readonly int X, Y, Width, Height;

        public TemplatesUIBuilder(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;

            CalculateLayout();
        }

        private void CalculateLayout()
        {
            int contentY = Y + ContentBoxPadding + TitleAreaHeight;
            int panelHeight = Height - TitleAreaHeight - TemplatesBottomButtonHeight - (ContentBoxPadding * 3);

            // Left panel
            LeftPanel = new Rectangle(
                X + ContentBoxPadding,
                contentY,
                TemplatesLeftPanelWidth,
                panelHeight
            );

            // Right panel
            RightPanel = new Rectangle(
                LeftPanel.Right + TemplatesPanelGap,
                contentY,
                TemplatesRightPanelWidth,
                panelHeight
            );

            // Filter/Sort/Search in left panel
            int controlY = LeftPanel.Y;
            FilterDropdown = new ClickableComponent(
                new Rectangle(LeftPanel.X, controlY, 100, TabAndButtonHeight),
                "filter"
            );
            SortDropdown = new ClickableComponent(
                new Rectangle(FilterDropdown.bounds.Right + 8, controlY, 100, TabAndButtonHeight),
                "sort"
            );
            SearchBar = new ClickableComponent(
                new Rectangle(SortDropdown.bounds.Right + 8, controlY,
                    LeftPanel.Right - SortDropdown.bounds.Right - 8, TabAndButtonHeight),
                "search"
            );

            // Template list items
            int listY = SearchBar.bounds.Bottom + ContentBoxPadding;
            int listHeight = LeftPanel.Bottom - listY;
            int maxItems = listHeight / TemplatesListItemHeight;

            TemplateListItems.Clear();
            for (int i = 0; i < maxItems; i++)
            {
                TemplateListItems.Add(new ClickableComponent(
                    new Rectangle(LeftPanel.X, listY + (i * TemplatesListItemHeight),
                        LeftPanel.Width, TemplatesListItemHeight),
                    $"template_{i}"
                ));
            }

            // Preview box in right panel
            int previewX = RightPanel.X + (RightPanel.Width - TemplatesPreviewSize) / 2;
            PreviewBox = new Rectangle(previewX, RightPanel.Y, TemplatesPreviewSize, TemplatesPreviewSize);

            // Bottom buttons
            int buttonY = Y + Height - TemplatesBottomButtonHeight + ContentBoxPadding;
            int buttonWidth = 120;

            LoadButton = new ClickableComponent(
                new Rectangle(X + ContentBoxPadding, buttonY, buttonWidth, TabAndButtonHeight),
                "load"
            );
            ApplyCloseButton = new ClickableComponent(
                new Rectangle(LoadButton.bounds.Right + BottomButtonGap, buttonY, 150, TabAndButtonHeight),
                "applyClose"
            );

            int rightButtonsX = X + Width - ContentBoxPadding - buttonWidth;
            DeleteButton = new ClickableComponent(
                new Rectangle(rightButtonsX, buttonY, buttonWidth, TabAndButtonHeight),
                "delete"
            );
            RenameButton = new ClickableComponent(
                new Rectangle(rightButtonsX - buttonWidth - BottomButtonGap, buttonY, buttonWidth, TabAndButtonHeight),
                "rename"
            );
            DuplicateButton = new ClickableComponent(
                new Rectangle(RenameButton.bounds.X - buttonWidth - BottomButtonGap, buttonY, buttonWidth, TabAndButtonHeight),
                "duplicate"
            );
            NewButton = new ClickableComponent(
                new Rectangle(DuplicateButton.bounds.X - buttonWidth - BottomButtonGap, buttonY, buttonWidth, TabAndButtonHeight),
                "new"
            );

            FavoriteButton = new ClickableComponent(
                new Rectangle(X + ContentBoxPadding, buttonY + TabAndButtonHeight + 8, 140, TabAndButtonHeight),
                "favorite"
            );

            // Close button
            CloseButton = new ClickableTextureComponent(
                new Rectangle(X + Width - CloseButtonSize - CloseButtonEdgeMargin,
                    Y + CloseButtonEdgeMargin, CloseButtonSize, CloseButtonSize),
                Game1.mouseCursors,
                new Rectangle(337, 494, 12, 12),
                4f
            );
        }

        public void DrawLeftPanel(SpriteBatch b, List<string> templates, int selectedIndex)
        {
            // Draw panel outline
            IClickableMenu.drawTextureBox(b, LeftPanel.X - 8, LeftPanel.Y - 8,
                LeftPanel.Width + 16, LeftPanel.Height + 16, Color.White);

            // Draw filter/sort/search placeholders
            DrawTextButton(b, FilterDropdown, TranslationCache.TemplatesFilter + " ▾");
            DrawTextButton(b, SortDropdown, TranslationCache.TemplatesSort + " ▾");
            DrawTextBox(b, SearchBar, TranslationCache.TemplatesSearch);

            // Draw separator
            int separatorY = SearchBar.bounds.Bottom + ContentBoxPadding / 2;
            b.Draw(Game1.staminaRect, new Rectangle(LeftPanel.X, separatorY, LeftPanel.Width, 2), Color.Gray * 0.3f);

            // Draw template list
            for (int i = 0; i < TemplateListItems.Count && i < templates.Count; i++)
            {
                var item = TemplateListItems[i];
                Color bgColor = i == selectedIndex ? Color.LightBlue * 0.3f : Color.Transparent;

                if (bgColor != Color.Transparent)
                    b.Draw(Game1.staminaRect, item.bounds, bgColor);

                Utility.drawTextWithShadow(b, templates[i], Game1.smallFont,
                    new Vector2(item.bounds.X + 8, item.bounds.Y + 12), Game1.textColor);
            }
        }

        public void DrawRightPanel(SpriteBatch b, string selectedName)
        {
            // Draw panel outline
            IClickableMenu.drawTextureBox(b, RightPanel.X - 8, RightPanel.Y - 8,
                RightPanel.Width + 16, RightPanel.Height + 16, Color.White);

            // Draw "Template Details" label
            Utility.drawTextWithShadow(b, TranslationCache.TemplatesDetailsLabel, Game1.smallFont,
                new Vector2(RightPanel.X + 8, RightPanel.Y + 8), Game1.textColor);

            // Draw preview box
            IClickableMenu.drawTextureBox(b, PreviewBox.X - 4, PreviewBox.Y - 4 + 32,
                PreviewBox.Width + 8, PreviewBox.Height + 8, Color.White);
            Utility.drawTextWithShadow(b, TranslationCache.TemplatesPreviewLabel, Game1.smallFont,
                new Vector2(PreviewBox.X + 8, PreviewBox.Y + PreviewBox.Height / 2 + 32), Color.Gray);

            // Draw details (placeholder)
            int detailsY = PreviewBox.Bottom + ContentBoxPadding + 32;
            Utility.drawTextWithShadow(b, $"{TranslationCache.TemplatesName} {selectedName}", Game1.smallFont,
                new Vector2(RightPanel.X + 8, detailsY), Game1.textColor);

            detailsY += 32;
            Utility.drawTextWithShadow(b, $"{TranslationCache.TemplatesTags} [____][+]", Game1.smallFont,
                new Vector2(RightPanel.X + 8, detailsY), Game1.textColor);

            detailsY += 48;
            Utility.drawTextWithShadow(b, $"{TranslationCache.TemplatesShirt} _________", Game1.smallFont,
                new Vector2(RightPanel.X + 8, detailsY), Game1.textColor);

            detailsY += 28;
            Utility.drawTextWithShadow(b, $"{TranslationCache.TemplatesPants} _________", Game1.smallFont,
                new Vector2(RightPanel.X + 8, detailsY), Game1.textColor);

            detailsY += 28;
            Utility.drawTextWithShadow(b, $"{TranslationCache.TemplatesHat} _________", Game1.smallFont,
                new Vector2(RightPanel.X + 8, detailsY), Game1.textColor);
        }

        public void DrawButtons(SpriteBatch b)
        {
            DrawTextButton(b, LoadButton, TranslationCache.TemplatesButtonLoad);
            DrawTextButton(b, ApplyCloseButton, TranslationCache.TemplatesButtonApplyClose);
            DrawTextButton(b, NewButton, TranslationCache.TemplatesButtonNew);
            DrawTextButton(b, DuplicateButton, TranslationCache.TemplatesButtonDuplicate);
            DrawTextButton(b, RenameButton, TranslationCache.TemplatesButtonRename);
            DrawTextButton(b, DeleteButton, TranslationCache.TemplatesButtonDelete);
            DrawTextButton(b, FavoriteButton, TranslationCache.TemplatesButtonFavorite);
        }

        public void DrawCloseButton(SpriteBatch b)
        {
            CloseButton.draw(b);
        }

        public bool IsAnyButtonClicked(int x, int y)
        {
            return LoadButton.containsPoint(x, y) || ApplyCloseButton.containsPoint(x, y) ||
                   NewButton.containsPoint(x, y) || DuplicateButton.containsPoint(x, y) ||
                   RenameButton.containsPoint(x, y) || DeleteButton.containsPoint(x, y) ||
                   FavoriteButton.containsPoint(x, y);
        }

        private void DrawTextButton(SpriteBatch b, ClickableComponent button, string label)
        {
            IClickableMenu.drawTextureBox(b, button.bounds.X, button.bounds.Y,
                button.bounds.Width, button.bounds.Height, Color.White);

            Vector2 textSize = Game1.smallFont.MeasureString(label);
            Utility.drawTextWithShadow(b, label, Game1.smallFont,
                new Vector2(button.bounds.X + (button.bounds.Width - textSize.X) / 2,
                    button.bounds.Y + (button.bounds.Height - textSize.Y) / 2),
                Game1.textColor);
        }

        private void DrawTextBox(SpriteBatch b, ClickableComponent box, string placeholder)
        {
            IClickableMenu.drawTextureBox(b, box.bounds.X, box.bounds.Y,
                box.bounds.Width, box.bounds.Height, Color.White);

            Utility.drawTextWithShadow(b, placeholder, Game1.smallFont,
                new Vector2(box.bounds.X + 8, box.bounds.Y + 12), Color.Gray);
        }
    }
}
