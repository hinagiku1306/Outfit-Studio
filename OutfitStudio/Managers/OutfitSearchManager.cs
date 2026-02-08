using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace OutfitStudio
{
    public class OutfitSearchManager
    {
        private readonly OutfitUIBuilder uiBuilder;
        private readonly TextBox searchBox;
        private string lastSearchText = "";
        private bool searchBarFocused;

        public string CurrentSearchText => searchBox.Text;
        public bool HasSearchTextChanged { get; private set; }
        public bool IsFocused => searchBox.Selected;

        public OutfitSearchManager(OutfitUIBuilder uiBuilder, OutfitState state)
        {
            this.uiBuilder = uiBuilder ?? throw new ArgumentNullException(nameof(uiBuilder));
            this.searchBarFocused = ModEntry.Config.AutoFocusSearchBar;

            searchBox = new TextBox(
                Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                null,
                Game1.smallFont,
                Game1.textColor)
            {
                Text = "",
                Selected = ModEntry.Config.AutoFocusSearchBar
            };

            UpdateBounds();
        }

        public void UpdateBounds()
        {
            if (uiBuilder.SearchBar == null)
                return;

            Rectangle bounds = uiBuilder.SearchBar.bounds;

            searchBox.X = bounds.X + 16;
            searchBox.Y = bounds.Y + ((bounds.Height - 48) / 2);
            searchBox.Width = bounds.Width - 32;
        }

        public void Focus()
        {
            searchBarFocused = true;
        }

        public void Update(bool allowFocus = true)
        {
            if (allowFocus)
            {
                searchBox.Update();
                searchBox.Selected = ModEntry.Config.AutoFocusSearchBar || searchBarFocused;
            }
            else
            {
                searchBox.Selected = false;
            }

            if (searchBox.Text != lastSearchText)
            {
                HasSearchTextChanged = true;
                lastSearchText = searchBox.Text;
            }
            else
            {
                HasSearchTextChanged = false;
            }
        }

        public void Draw(SpriteBatch b)
        {
            if (uiBuilder.SearchBar == null)
                return;

            Rectangle bounds = uiBuilder.SearchBar.bounds;

            if (!string.IsNullOrEmpty(searchBox.Text))
            {
                Vector2 textPosition = new Vector2(bounds.X + 20, bounds.Y + (bounds.Height - Game1.smallFont.MeasureString(searchBox.Text).Y) / 2);
                Utility.drawTextWithShadow(b, searchBox.Text, Game1.smallFont, textPosition, Game1.textColor);
            }

            if (searchBox.Selected)
            {
                Vector2 textSize = string.IsNullOrEmpty(searchBox.Text)
                    ? Vector2.Zero
                    : Game1.smallFont.MeasureString(searchBox.Text);

                Vector2 textPosition = new Vector2(bounds.X + 20, bounds.Y + (bounds.Height - 32) / 2);
                Vector2 cursorPosition = new Vector2(textPosition.X + textSize.X, textPosition.Y);

                if ((int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 500) % 2 == 0)
                {
                    b.Draw(Game1.staminaRect, new Rectangle((int)cursorPosition.X, (int)cursorPosition.Y, 4, 32), Game1.textColor);
                }
            }
        }

        public void Unfocus()
        {
            searchBarFocused = false;
        }

        public void SetText(string text)
        {
            searchBox.Text = text ?? "";
            lastSearchText = searchBox.Text;
            HasSearchTextChanged = false;
        }

        public void Clear()
        {
            searchBox.Text = "";
            lastSearchText = "";
            HasSearchTextChanged = true;
        }

        public bool IsPointInBounds(int x, int y)
        {
            return uiBuilder.SearchBar != null && uiBuilder.SearchBar.containsPoint(x, y);
        }
    }
}
