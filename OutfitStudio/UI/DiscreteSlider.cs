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

            Value = CalculateValueFromClick(x, Bounds.X, Bounds.Width, HandleWidth, Min, Max);
        }

        internal static int CalculateValueFromClick(
            int clickX, int boundsX, int boundsWidth, int handleWidth, int min, int max)
        {
            int trackWidth = boundsWidth - handleWidth;
            float fraction = (float)(clickX - boundsX - handleWidth / 2) / trackWidth;
            fraction = Math.Clamp(fraction, 0f, 1f);

            int value = min + (int)Math.Round(fraction * (max - min), MidpointRounding.AwayFromZero);
            return Math.Clamp(value, min, max);
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
