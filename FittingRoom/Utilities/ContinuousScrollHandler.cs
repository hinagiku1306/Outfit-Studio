using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FittingRoom
{
    /// <summary>
    /// Handles continuous scrolling when keys are held down.
    /// </summary>
    public class ContinuousScrollHandler
    {
        private int scrollHoldTimer = 0;
        private int lastScrollTime = 0;
        private readonly int initialDelay;
        private readonly int repeatDelay;

        /// <summary>
        /// Creates a new continuous scroll handler.
        /// </summary>
        /// <param name="initialDelay">Delay before continuous scrolling starts (ms)</param>
        /// <param name="repeatDelay">Delay between scroll ticks (ms)</param>
        public ContinuousScrollHandler(int initialDelay = 400, int repeatDelay = 100)
        {
            this.initialDelay = initialDelay;
            this.repeatDelay = repeatDelay;
        }

        /// <summary>
        /// Updates the continuous scroll state and returns scroll amount if scrolling should occur.
        /// </summary>
        /// <param name="time">Game time</param>
        /// <param name="visibleRows">Number of visible rows for page scrolling</param>
        /// <param name="shouldPlaySound">True if a scroll occurred and sound should play</param>
        /// <returns>Scroll amount (0 = no scroll, negative = up, positive = down)</returns>
        public int Update(GameTime time, int visibleRows, out bool shouldPlaySound)
        {
            shouldPlaySound = false;
            var keyboard = Keyboard.GetState();

            bool scrollKeyHeld = false;
            int scrollDirection = 0; // -1 for up, 1 for down, -visibleRows for page up, +visibleRows for page down

            // Check if any scroll keys are held
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
                scrollDirection = -visibleRows; // Page up
            }
            else if (keyboard.IsKeyDown(Keys.Right) || keyboard.IsKeyDown(Keys.D))
            {
                scrollKeyHeld = true;
                scrollDirection = visibleRows; // Page down
            }

            if (scrollKeyHeld)
            {
                scrollHoldTimer += (int)time.ElapsedGameTime.TotalMilliseconds;

                // Only start continuous scrolling after initial delay (to avoid double-trigger with receiveKeyPress)
                if (scrollHoldTimer >= initialDelay)
                {
                    // Check if enough time has passed since last scroll
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
                // Reset timers when no scroll keys are held
                Reset();
            }

            return 0; // No scroll
        }

        /// <summary>
        /// Resets the scroll timers.
        /// </summary>
        public void Reset()
        {
            scrollHoldTimer = 0;
            lastScrollTime = 0;
        }
    }
}
