using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    public class DyeColorManager
    {
        private bool isOpen;
        private int hueValue;
        private int satValue;
        private int valValue;
        private Color originalColor;
        private int activeSliderIndex = -1;

        private Rectangle panelBounds;
        private Rectangle hueBarBounds;
        private Rectangle satBarBounds;
        private Rectangle valBarBounds;
        private ClickableComponent resetButton = null!;
        private string? hoverTooltip;
        private int barOffsetX;

        private readonly Action onColorChanged;
        private readonly Func<OutfitCategoryManager.Category> getActiveCategory;

        public bool IsOpen => isOpen;
        public Rectangle Bounds => panelBounds;
        public bool IsDyeable { get; set; } = true;

        public DyeColorManager(Action onColorChanged, Func<OutfitCategoryManager.Category> getActiveCategory)
        {
            this.onColorChanged = onColorChanged;
            this.getActiveCategory = getActiveCategory;
        }

        public void Open(Rectangle menuBounds, Color currentItemColor)
        {
            isOpen = true;
            originalColor = currentItemColor;
            SetSlidersFromColor(currentItemColor);
            CalculateLayout(menuBounds);
        }

        public void Close()
        {
            isOpen = false;
            activeSliderIndex = -1;
        }

        public void Toggle(Rectangle menuBounds, Color currentItemColor)
        {
            if (isOpen)
                Close();
            else
                Open(menuBounds, currentItemColor);
        }

        public Color GetCurrentColor()
        {
            double hue = hueValue / 100.0 * 360.0;
            double sat = satValue / 100.0;
            double val = valValue / 100.0;
            return ColorPicker.HsvToRgb(hue, sat, val);
        }

        public void SetSlidersFromColor(Color color)
        {
            ColorPicker.RGBtoHSV(color.R, color.G, color.B, out float h, out float s, out float v);
            hueValue = (int)(h / 360f * 100f);
            satValue = (int)(s * 100f);
            valValue = (int)(v / 255f * 100f);
        }

        public void ApplyToActiveItem(OutfitCategoryManager.Category category)
        {
            Color color = GetCurrentColor();
            switch (category)
            {
                case OutfitCategoryManager.Category.Pants:
                    if (Game1.player.CanDyePants())
                        Game1.player.changePantsColor(color);
                    break;
                case OutfitCategoryManager.Category.Shirts:
                    if (Game1.player.CanDyeShirt())
                    {
                        Game1.player.shirtItem.Value.clothesColor.Set(color);
                        Game1.player.FarmerRenderer.MarkSpriteDirty();
                    }
                    break;
            }
            onColorChanged?.Invoke();
        }

        public void ResetSliders()
        {
            SetSlidersFromColor(originalColor);
            ApplyToActiveItem(getActiveCategory());
        }

        public void HandleClick(int x, int y, out bool consumed)
        {
            consumed = false;
            if (!isOpen)
                return;

            if (!panelBounds.Contains(x, y))
                return;

            consumed = true;

            if (!IsDyeable)
                return;

            if (resetButton.containsPoint(x, y))
            {
                ResetSliders();
                Game1.playSound("drumkit6");
                return;
            }

            TryStartSliderDrag(x, y);
        }

        private void TryStartSliderDrag(int x, int y)
        {
            // Use expanded hit areas for easier clicking
            int expandY = 8;

            Rectangle hueHit = new Rectangle(hueBarBounds.X, hueBarBounds.Y - expandY, hueBarBounds.Width, hueBarBounds.Height + expandY * 2);
            Rectangle satHit = new Rectangle(satBarBounds.X, satBarBounds.Y - expandY, satBarBounds.Width, satBarBounds.Height + expandY * 2);
            Rectangle valHit = new Rectangle(valBarBounds.X, valBarBounds.Y - expandY, valBarBounds.Width, valBarBounds.Height + expandY * 2);

            if (hueHit.Contains(x, y))
            {
                activeSliderIndex = 0;
                UpdateSliderFromX(x);
            }
            else if (satHit.Contains(x, y))
            {
                activeSliderIndex = 1;
                UpdateSliderFromX(x);
            }
            else if (valHit.Contains(x, y))
            {
                activeSliderIndex = 2;
                UpdateSliderFromX(x);
            }
        }

        public void HandleClickHeld(int x, int y)
        {
            if (!isOpen || !IsDyeable || activeSliderIndex < 0)
                return;

            UpdateSliderFromX(x);
        }

        public void HandleClickRelease()
        {
            activeSliderIndex = -1;
        }

        private void UpdateSliderFromX(int x)
        {
            Rectangle barBounds = activeSliderIndex switch
            {
                0 => hueBarBounds,
                1 => satBarBounds,
                2 => valBarBounds,
                _ => Rectangle.Empty
            };

            if (barBounds.IsEmpty)
                return;

            int relativeX = Math.Clamp(x - barBounds.X, 0, barBounds.Width);
            int newValue = (int)((float)relativeX / barBounds.Width * 100f);

            int currentValue = activeSliderIndex switch { 0 => hueValue, 1 => satValue, 2 => valValue, _ => -1 };
            if (newValue == currentValue)
                return;

            switch (activeSliderIndex)
            {
                case 0: hueValue = newValue; break;
                case 1: satValue = newValue; break;
                case 2: valValue = newValue; break;
            }

            ApplyToActiveItem(getActiveCategory());
        }

        public void UpdateParentBounds(Rectangle menuBounds)
        {
            CalculateLayout(menuBounds);
        }

        public static int ComputePanelHeight()
        {
            int titleHeight = (int)Game1.smallFont.MeasureString("A").Y;
            int sliderSectionHeight = 3 * DyeColorSliderRowHeight + 2 * DyeColorSliderGap;
            int titleToSliderGap = 10;
            int sliderToResetGap = 10;
            return DyeColorPanelPadding + titleHeight + titleToSliderGap
                   + sliderSectionHeight + sliderToResetGap
                   + TabAndButtonHeight + DyeColorPanelPadding;
        }

        private void CalculateLayout(Rectangle menuBounds)
        {
            int titleHeight = (int)Game1.smallFont.MeasureString("A").Y;
            int sliderSectionHeight = 3 * DyeColorSliderRowHeight + 2 * DyeColorSliderGap;

            int maxLabelWidth = Math.Max(
                (int)Game1.smallFont.MeasureString("H").X,
                Math.Max(
                    (int)Game1.smallFont.MeasureString("S").X,
                    (int)Game1.smallFont.MeasureString("V").X
                )
            );
            barOffsetX = maxLabelWidth + 10;

            int resetButtonWidth = UIHelpers.CalculateButtonWidth(TranslationCache.CommonReset);
            int resetButtonHeight = TabAndButtonHeight;

            int titleToSliderGap = 10;
            int sliderToResetGap = 10;

            int totalContentHeight = DyeColorPanelPadding
                                     + titleHeight + titleToSliderGap
                                     + sliderSectionHeight + sliderToResetGap
                                     + resetButtonHeight
                                     + DyeColorPanelPadding;

            int panelX = menuBounds.Right;
            int panelY = menuBounds.Y + (menuBounds.Height - totalContentHeight) / 2;

            if (panelX + DyeColorPanelWidth > Game1.uiViewport.Width)
                panelX = Game1.uiViewport.Width - DyeColorPanelWidth;

            panelBounds = new Rectangle(panelX, panelY, DyeColorPanelWidth, totalContentHeight);

            int contentX = panelBounds.X + DyeColorPanelPadding;
            int contentWidth = panelBounds.Width - DyeColorPanelPadding * 2;
            int currentY = panelBounds.Y + DyeColorPanelPadding + titleHeight + titleToSliderGap;

            hueBarBounds = new Rectangle(contentX + barOffsetX, currentY + (DyeColorSliderRowHeight - DyeColorSliderTrackHeight) / 2, contentWidth - barOffsetX, DyeColorSliderTrackHeight);
            currentY += DyeColorSliderRowHeight + DyeColorSliderGap;

            satBarBounds = new Rectangle(contentX + barOffsetX, currentY + (DyeColorSliderRowHeight - DyeColorSliderTrackHeight) / 2, contentWidth - barOffsetX, DyeColorSliderTrackHeight);
            currentY += DyeColorSliderRowHeight + DyeColorSliderGap;

            valBarBounds = new Rectangle(contentX + barOffsetX, currentY + (DyeColorSliderRowHeight - DyeColorSliderTrackHeight) / 2, contentWidth - barOffsetX, DyeColorSliderTrackHeight);
            currentY += DyeColorSliderRowHeight + sliderToResetGap;

            resetButton = new ClickableComponent(
                new Rectangle(
                    panelBounds.X + (panelBounds.Width - resetButtonWidth) / 2,
                    currentY,
                    resetButtonWidth,
                    resetButtonHeight
                ),
                "Reset"
            );
        }

        public void Draw(SpriteBatch b)
        {
            if (!isOpen)
                return;

            hoverTooltip = null;

            Color boxTint = IsDyeable ? Color.White : Color.White * 0.7f;
            Color textTint = IsDyeable ? Game1.textColor : Game1.textColor * TabOpacity;
            Color elementTint = IsDyeable ? Color.White : Color.White * 0.5f;

            UIHelpers.DrawTextureBox(b, panelBounds.X, panelBounds.Y,
                panelBounds.Width, panelBounds.Height, Color.White);

            Vector2 titlePos = new Vector2(
                panelBounds.X + DyeColorPanelPadding,
                panelBounds.Y + DyeColorPanelPadding
            );
            Utility.drawTextWithShadow(b, TranslationCache.DyeColorTitle, Game1.smallFont,
                titlePos, textTint);

            DrawSliderRow(b, "H", hueBarBounds, hueValue, 0, textTint, elementTint);
            DrawSliderRow(b, "S", satBarBounds, satValue, 1, textTint, elementTint);
            DrawSliderRow(b, "V", valBarBounds, valValue, 2, textTint, elementTint);

            if (IsDyeable)
            {
                UIHelpers.DrawTextButton(b, resetButton, TranslationCache.CommonReset);
            }
            else
            {
                UIHelpers.DrawTextureBox(b, resetButton.bounds.X, resetButton.bounds.Y,
                    resetButton.bounds.Width, resetButton.bounds.Height, boxTint);
                Vector2 textSize = Game1.smallFont.MeasureString(TranslationCache.CommonReset);
                Vector2 textPos = UIHelpers.GetVisualCenter(resetButton.bounds, textSize);
                Utility.drawTextWithShadow(b, TranslationCache.CommonReset, Game1.smallFont, textPos, textTint);
            }

            if (hoverTooltip != null && ModEntry.Config.ShowTooltip)
            {
                string wrapped = Game1.parseText(hoverTooltip, Game1.smallFont, 300);
                IClickableMenu.drawHoverText(b, wrapped, Game1.smallFont);
            }
        }

        private void DrawSliderRow(SpriteBatch b, string label, Rectangle barBounds, int value, int sliderIndex,
            Color textTint, Color elementTint)
        {
            Vector2 labelPos = new Vector2(
                barBounds.X - barOffsetX,
                barBounds.Y + (DyeColorSliderTrackHeight / 2) - Game1.smallFont.MeasureString(label).Y / 2
            );
            Utility.drawTextWithShadow(b, label, Game1.smallFont, labelPos, textTint);

            int chunkWidth = barBounds.Width / DyeColorGradientChunks;
            for (int i = 0; i < DyeColorGradientChunks; i++)
            {
                Color chunkColor = GetGradientChunkColor(sliderIndex, i) * (elementTint.A / 255f);
                Rectangle chunkRect = new Rectangle(
                    barBounds.X + i * chunkWidth,
                    barBounds.Y,
                    chunkWidth,
                    barBounds.Height
                );
                b.Draw(Game1.staminaRect, chunkRect, chunkColor);
            }

            float normalizedValue = value / 100f;
            int cursorX = barBounds.X + (int)(normalizedValue * barBounds.Width);
            int cursorY = barBounds.Y + barBounds.Height / 2;

            Rectangle cursorSource = new Rectangle(64, 256, 32, 32);
            Vector2 cursorOrigin = new Vector2(16, 9);
            b.Draw(Game1.mouseCursors, new Vector2(cursorX, cursorY), cursorSource,
                elementTint, 0f, cursorOrigin, 1f, SpriteEffects.None, 1f);
        }

        private Color GetGradientChunkColor(int sliderIndex, int chunkIndex)
        {
            float chunkRatio = (float)chunkIndex / DyeColorGradientChunks;
            return sliderIndex switch
            {
                0 => ColorPicker.HsvToRgb(chunkRatio * 360.0, 0.9, 0.9),
                1 => ColorPicker.HsvToRgb(hueValue / 100.0 * 360.0, chunkRatio, valValue / 100.0),
                2 => ColorPicker.HsvToRgb(hueValue / 100.0 * 360.0, satValue / 100.0, chunkRatio),
                _ => Color.White
            };
        }
    }
}
