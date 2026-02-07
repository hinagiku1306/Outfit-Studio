using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace OutfitStudio
{
    /// <summary>
    /// Manages the search bar TextBox state and interactions.
    /// </summary>
    public class OutfitSearchManager
    {
        // Dependencies
        private readonly OutfitUIBuilder uiBuilder;

        // TextBox control
        private readonly TextBox searchBox;

        // State tracking
        private string lastSearchText = "";

        /// <summary>
        /// Gets the current search text.
        /// </summary>
        public string CurrentSearchText => searchBox.Text;

        /// <summary>
        /// Gets whether the search text has changed since last update.
        /// </summary>
        public bool HasSearchTextChanged { get; private set; }

        /// <summary>
        /// Gets whether the TextBox is currently focused.
        /// </summary>
        public bool IsFocused => searchBox.Selected;

        public OutfitSearchManager(OutfitUIBuilder uiBuilder, OutfitState state)
        {
            this.uiBuilder = uiBuilder ?? throw new ArgumentNullException(nameof(uiBuilder));

            // Initialize TextBox with Stardew Valley's default textbox texture
            searchBox = new TextBox(
                Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                null,
                Game1.smallFont,
                Game1.textColor)
            {
                Text = "",
                Selected = true // Always keep selected so user can type anytime
            };

            // Position will be set in UpdateBounds()
            UpdateBounds();
        }

        /// <summary>Updates TextBox bounds to match SearchBar component.</summary>
        public void UpdateBounds()
        {
            if (uiBuilder.SearchBar == null)
                return;

            Rectangle bounds = uiBuilder.SearchBar.bounds;

            searchBox.X = bounds.X + 16;
            searchBox.Y = bounds.Y + ((bounds.Height - 48) / 2);
            searchBox.Width = bounds.Width - 32;
        }

        /// <summary>Updates search state and detects text changes.</summary>
        public void Update(bool allowFocus = true)
        {
            if (allowFocus)
            {
                searchBox.Update();
                searchBox.Selected = true;
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

        /// <summary>Draws the search text and blinking cursor.</summary>
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

        /// <summary>Unfocuses the TextBox (no-op, search box stays focused).</summary>
        public void Unfocus()
        {
        }

        /// <summary>Sets search text programmatically without triggering filter rebuild.</summary>
        public void SetText(string text)
        {
            searchBox.Text = text ?? "";
            lastSearchText = searchBox.Text;
            HasSearchTextChanged = false;
        }

        /// <summary>Clears the search text.</summary>
        public void Clear()
        {
            searchBox.Text = "";
            lastSearchText = "";
            HasSearchTextChanged = true;
        }

        /// <summary>Checks if a point is within the search bar bounds.</summary>
        public bool IsPointInBounds(int x, int y)
        {
            return uiBuilder.SearchBar != null && uiBuilder.SearchBar.containsPoint(x, y);
        }
    }
}
