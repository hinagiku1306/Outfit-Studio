using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using static FittingRoom.OutfitLayoutConstants;

namespace FittingRoom
{
    /// <summary>
    /// Templates overlay menu (placeholder for UI testing).
    /// </summary>
    public class TemplatesOverlay : IClickableMenu
    {
        private readonly TemplatesUIBuilder uiBuilder;
        private bool shouldClose = false;

        // Placeholder template data
        private readonly List<string> templateNames = new()
        {
            "★ Spring Picnic",
            "▢ Mines Run (Iron)",
            "▢ Rainy Day Cozy",
            "▢ Beach Day",
            "▢ Festival Formal"
        };

        private int selectedIndex = 0;

        public TemplatesOverlay()
        {
            width = TemplatesOverlayWidth;
            height = TemplatesOverlayHeight;
            xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
            yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;

            uiBuilder = new TemplatesUIBuilder(xPositionOnScreen, yPositionOnScreen, width, height);
        }

        public override bool readyToClose() => shouldClose;

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            // Close button
            if (uiBuilder.CloseButton.containsPoint(x, y))
            {
                shouldClose = true;
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }

            // Template list selection
            for (int i = 0; i < uiBuilder.TemplateListItems.Count && i < templateNames.Count; i++)
            {
                if (uiBuilder.TemplateListItems[i].containsPoint(x, y))
                {
                    selectedIndex = i;
                    if (playSound) Game1.playSound("smallSelect");
                    return;
                }
            }

            // All buttons are placeholders - just play sound
            if (uiBuilder.IsAnyButtonClicked(x, y))
            {
                if (playSound) Game1.playSound("smallSelect");
            }
        }

        public override void receiveKeyPress(Keys key)
        {
            if (key == Keys.Escape || Game1.options.menuButton.Contains(new InputButton(key)))
            {
                shouldClose = true;
                Game1.playSound("bigDeSelect");
            }
        }

        public override void draw(SpriteBatch b)
        {
            // Draw dimmed background
            b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
                Color.Black * BackgroundOverlayOpacity);

            // Draw menu box
            Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height,
                speaker: false, drawOnlyBox: true);

            // Draw title
            Vector2 titleSize = Game1.dialogueFont.MeasureString(TranslationCache.TemplatesTitle);
            Utility.drawTextWithShadow(b, TranslationCache.TemplatesTitle, Game1.dialogueFont,
                new Vector2(xPositionOnScreen + (width - titleSize.X) / 2, yPositionOnScreen + ContentBoxPadding),
                Game1.textColor);

            // Draw UI components
            uiBuilder.DrawLeftPanel(b, templateNames, selectedIndex);
            uiBuilder.DrawRightPanel(b, selectedIndex >= 0 && selectedIndex < templateNames.Count
                ? templateNames[selectedIndex] : "");
            uiBuilder.DrawButtons(b);
            uiBuilder.DrawCloseButton(b);

            // Draw mouse cursor
            drawMouse(b);
        }
    }
}
