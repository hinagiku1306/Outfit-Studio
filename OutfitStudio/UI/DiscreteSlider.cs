using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace OutfitStudio
{
    public class DiscreteSlider
    {
        private static readonly Rectangle BackgroundSourceRect = new Rectangle(403, 383, 6, 6);
        private static readonly Rectangle HandleSourceRect = new Rectangle(420, 441, 10, 6);

        private const float SpriteScale = 4f;
        private const int HandleWidth = (int)(10 * SpriteScale);

        public int Value { get; set; }
        public int Min { get; }
        public int Max { get; }
        public Rectangle Bounds { get; set; }

        public DiscreteSlider(int x, int y, int width, int height, int min, int max, int initialValue)
        {
            Min = min;
            Max = max;
            Value = Math.Clamp(initialValue, min, max);
            Bounds = new Rectangle(x, y, width, height);
        }

        public void Click(int x, int y)
        {
            if (!Bounds.Contains(x, y))
                return;

            int trackWidth = Bounds.Width - HandleWidth;
            float fraction = (float)(x - Bounds.X - HandleWidth / 2) / trackWidth;
            fraction = Math.Clamp(fraction, 0f, 1f);

            Value = Min + (int)Math.Round(fraction * (Max - Min), MidpointRounding.AwayFromZero);
            Value = Math.Clamp(Value, Min, Max);
        }

        public void Draw(SpriteBatch b)
        {
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, BackgroundSourceRect,
                Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, Color.White, SpriteScale, drawShadow: false);

            int trackWidth = Bounds.Width - HandleWidth;
            float handleFraction = (Max > Min) ? (float)(Value - Min) / (Max - Min) : 0f;
            float handleX = Bounds.X + trackWidth * handleFraction;

            b.Draw(Game1.mouseCursors, new Vector2(handleX, Bounds.Y), HandleSourceRect,
                Color.White, 0f, Vector2.Zero, SpriteScale, SpriteEffects.None, 0.9f);
        }
    }
}
