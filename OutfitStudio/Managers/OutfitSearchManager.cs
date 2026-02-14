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
            if (uiBuilder.SearchBar == null) return;
            bool hasText = !string.IsNullOrEmpty(searchBox.Text);
            UIHelpers.DrawInputBar(b, uiBuilder.SearchBar.bounds,
                searchBox.Text ?? "", searchBox.Selected,
                placeholder: TranslationCache.WardrobeSearch,
                clearButton: hasText ? uiBuilder.SearchClearButton : null);
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
