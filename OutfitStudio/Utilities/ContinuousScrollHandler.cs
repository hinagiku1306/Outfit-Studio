using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace OutfitStudio
{
    public class ContinuousScrollHandler
    {
        private int scrollHoldTimer = 0;
        private int lastScrollTime = 0;
        private readonly int initialDelay;
        private readonly int repeatDelay;

        public ContinuousScrollHandler(int initialDelay = 400, int repeatDelay = 100)
        {
            this.initialDelay = initialDelay;
            this.repeatDelay = repeatDelay;
        }

        // Returns scroll amount: -1=up, 1=down, -visibleRows=page up, +visibleRows=page down
        public int Update(GameTime time, int visibleRows, out bool shouldPlaySound)
        {
            shouldPlaySound = false;
            var keyboard = Keyboard.GetState();

            bool scrollKeyHeld = false;
            int scrollDirection = 0;

            if (keyboard.IsKeyDown(Keys.Up) || keyboard.IsKeyDown(Keys.W))
            {
                scrollKeyHeld = true;
                scrollDirection = -1;
            }
            else if (keyboard.IsKeyDown(Keys.Down) || keyboard.IsKeyDown(Keys.S))
            {
                scrollKeyHeld = true;
                scrollDirection = 1;
            }
            else if (keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.A))
            {
                scrollKeyHeld = true;
                scrollDirection = -visibleRows;
            }
            else if (keyboard.IsKeyDown(Keys.Right) || keyboard.IsKeyDown(Keys.D))
            {
                scrollKeyHeld = true;
                scrollDirection = visibleRows;
            }

            if (scrollKeyHeld)
            {
                scrollHoldTimer += (int)time.ElapsedGameTime.TotalMilliseconds;

                // Initial delay avoids double-trigger with receiveKeyPress
                if (scrollHoldTimer >= initialDelay)
                {
                    int timeSinceLastScroll = scrollHoldTimer - lastScrollTime;
                    if (timeSinceLastScroll >= repeatDelay)
                    {
                        lastScrollTime = scrollHoldTimer;
                        shouldPlaySound = true;
                        return scrollDirection;
                    }
                }
            }
            else
            {
                Reset();
            }

            return 0;
        }

        public void Reset()
        {
            scrollHoldTimer = 0;
            lastScrollTime = 0;
        }
    }
}
