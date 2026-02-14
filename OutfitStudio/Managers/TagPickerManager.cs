using System;
using System.Collections.Generic;
using System.Linq;
using OutfitStudio.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    public class TagPickerManager
    {
        private const int MaxTagsPerSet = 5;
        private const int MaxTagLength = 30;
        private const int PopupWidth = 300;
        private const int PopupPadding = 35;
        private const int OptionHeight = 36;
        private const int MaxVisibleOptions = 6;
        private const int CustomInputHeight = 40;
        private const int DividerPadding = 12;
        private const int CustomSectionTopPadding = 22;
        private const int BottomPadding = 36;
        private const int CustomLabelHeight = 36;
        private const float ScrollArrowScale = 2.5f;
        private const int ScrollArrowRightMargin = 48;

        private readonly OutfitSetStore store;
        private readonly bool selectOnly;
        private bool showAllOption;
        private bool allSelected;
        private const string AllOptionKey = "__ALL__";

        private bool isOpen;
        private Rectangle popupBounds;
        private List<ClickableComponent> tagOptions = new();
        private HashSet<string> selectedTags = new();
        private int firstVisibleIndex;

        private TextBox? customTagTextBox;
        private ClickableComponent? addCustomButton;
        private ClickableComponent? upArrowButton;
        private ClickableComponent? downArrowButton;
        private int tagListStartY;
        private List<string> truncatedTagTexts = new();
        private List<string> fullTagTexts = new();
        private string? hoverTooltip;

        private bool isEditMode;
        private HashSet<string> tagsMarkedForDeletion = new();
        private ClickableComponent? editModeButton;
        private ClickableComponent? deleteConfirmButton;
        private List<ClickableComponent> tagDeleteButtons = new();

        private Action<HashSet<string>>? onComplete;

        private Rectangle parentBounds;

        private bool customInputFocused;

        public bool IsOpen => isOpen;
        public Rectangle Bounds => popupBounds;
        public bool IsCustomInputFocused => customInputFocused;

        public void DeselectCustomInput()
        {
            customInputFocused = false;
            if (customTagTextBox != null)
                customTagTextBox.Selected = false;
        }

        private Action<bool>? onAllChanged;
        private bool openToLeft;

        public TagPickerManager(OutfitSetStore store, bool selectOnly = false)
        {
            this.store = store;
            this.selectOnly = selectOnly;
        }

        public void Open(Rectangle parentBounds, HashSet<string> currentTags, Action<HashSet<string>> onComplete)
        {
            Open(parentBounds, currentTags, false, onComplete, null);
        }

        public void Open(Rectangle parentBounds, HashSet<string> currentTags, bool currentAllSelected, Action<HashSet<string>> onComplete, Action<bool>? onAllChanged, bool openToLeft = false)
        {
            this.parentBounds = parentBounds;
            this.onComplete = onComplete;
            this.onAllChanged = onAllChanged;
            this.openToLeft = openToLeft;
            selectedTags = new HashSet<string>(currentTags, TranslationCache.TagComparer);
            showAllOption = selectOnly;
            allSelected = currentAllSelected;
            firstVisibleIndex = 0;
            isOpen = true;

            CalculateLayout();
            BuildOptions();
            if (!selectOnly)
                InitializeCustomTagInput();
        }

        public void Close()
        {
            isOpen = false;
            isEditMode = false;
            customInputFocused = false;
            tagsMarkedForDeletion.Clear();
            customTagTextBox = null;
        }

        private void CalculateLayout()
        {
            int titleHeight = (int)Game1.smallFont.MeasureString("A").Y;
            int tagListHeight = MaxVisibleOptions * OptionHeight;

            int totalHeight;
            if (selectOnly)
            {
                totalHeight = PopupPadding
                              + titleHeight + DividerPadding
                              + DividerPadding
                              + tagListHeight
                              + BottomPadding;
            }
            else
            {
                totalHeight = PopupPadding
                              + titleHeight + DividerPadding
                              + DividerPadding
                              + tagListHeight
                              + CustomSectionTopPadding
                              + CustomLabelHeight
                              + CustomInputHeight
                              + BottomPadding;
            }

            int popupX = openToLeft ? parentBounds.X - PopupWidth : parentBounds.Right;
            int popupY = parentBounds.Y + (parentBounds.Height - totalHeight) / 2;

            popupBounds = new Rectangle(popupX, popupY, PopupWidth, totalHeight);

            if (!selectOnly)
            {
                int editBtnSize = (int)(CloseButtonSize * 1.1f);
                int editBtnX = popupBounds.Right - editBtnSize / 2 - 12;
                int editBtnY = popupBounds.Y - editBtnSize / 2 + 12;
                editModeButton = new ClickableComponent(
                    new Rectangle(editBtnX, editBtnY, editBtnSize, editBtnSize),
                    "editMode"
                );
            }
            else
            {
                editModeButton = null;
            }
        }

        private void BuildOptions()
        {
            tagOptions.Clear();
            tagDeleteButtons.Clear();
            truncatedTagTexts.Clear();
            fullTagTexts.Clear();

            var storeTags = store.GetAllTags();
            var allTags = new List<string>();
            if (showAllOption)
                allTags.Add(AllOptionKey);
            allTags.AddRange(storeTags);

            int titleHeight = (int)Game1.smallFont.MeasureString("A").Y;
            int optionsY = popupBounds.Y + PopupPadding + titleHeight + DividerPadding * 2;
            tagListStartY = optionsY;

            int optionWidth = popupBounds.Width - (PopupPadding * 2);
            int arrowWidth = (int)(ArrowNativeWidth * ScrollArrowScale);
            int scrollArrowIntrusion = arrowWidth / 2 + ScrollArrowRightMargin - PopupPadding;
            int leftUsed = isEditMode ? 4 : (4 + SaveSetLocalOnlyCheckboxSize + 8);
            int rightReserved = isEditMode
                ? (8 + TagDeleteButtonSize + scrollArrowIntrusion + 4)
                : (scrollArrowIntrusion + 8);
            int textMaxWidth = optionWidth - leftUsed - rightReserved;

            for (int i = 0; i < allTags.Count; i++)
            {
                string tag = allTags[i];
                string displayTag = tag == AllOptionKey ? TranslationCache.FilterAll : TranslationCache.GetTagDisplayName(tag);
                fullTagTexts.Add(displayTag);
                truncatedTagTexts.Add(TruncateText(displayTag, Game1.smallFont, textMaxWidth));
            }

            int maxFirstVisible = Math.Max(0, allTags.Count - MaxVisibleOptions);
            firstVisibleIndex = Math.Clamp(firstVisibleIndex, 0, maxFirstVisible);

            for (int i = 0; i < allTags.Count; i++)
            {
                bool isVisible = i >= firstVisibleIndex && i < firstVisibleIndex + MaxVisibleOptions;
                int rowY = optionsY + (i - firstVisibleIndex) * OptionHeight;

                var option = new ClickableComponent(
                    new Rectangle(
                        popupBounds.X + PopupPadding,
                        rowY,
                        optionWidth,
                        OptionHeight
                    ),
                    allTags[i]
                )
                {
                    visible = isVisible
                };
                tagOptions.Add(option);

                int textEndX = option.bounds.X + 4 + (int)Game1.smallFont.MeasureString(truncatedTagTexts[i]).X;
                int deleteBtnX = textEndX + 8;
                int deleteBtnY = rowY + (OptionHeight - TagDeleteButtonSize) / 2;
                var deleteButton = new ClickableComponent(
                    new Rectangle(deleteBtnX, deleteBtnY, TagDeleteButtonSize, TagDeleteButtonSize),
                    $"delete:{allTags[i]}"
                )
                {
                    visible = isVisible && isEditMode
                };
                tagDeleteButtons.Add(deleteButton);
            }

            int tagListHeight = MaxVisibleOptions * OptionHeight;
            int arrowHeight = (int)(ArrowNativeHeight * ScrollArrowScale);
            int arrowX = popupBounds.Right - ScrollArrowRightMargin - arrowWidth / 2;

            upArrowButton = new ClickableComponent(
                new Rectangle(arrowX, optionsY, arrowWidth, arrowHeight),
                "upArrow"
            );

            downArrowButton = new ClickableComponent(
                new Rectangle(arrowX, optionsY + tagListHeight - arrowHeight, arrowWidth, arrowHeight),
                "downArrow"
            );
        }

        private static string TruncateText(string text, SpriteFont font, int maxWidth)
        {
            if (font.MeasureString(text).X <= maxWidth)
                return text;

            string ellipsis = "...";
            float ellipsisWidth = font.MeasureString(ellipsis).X;

            for (int len = text.Length - 1; len > 0; len--)
            {
                if (font.MeasureString(text.Substring(0, len)).X + ellipsisWidth <= maxWidth)
                    return text.Substring(0, len) + ellipsis;
            }

            return ellipsis;
        }

        private void InitializeCustomTagInput()
        {
            int customSectionY = tagListStartY + MaxVisibleOptions * OptionHeight + CustomSectionTopPadding;
            int inputY = customSectionY + CustomLabelHeight;
            int addButtonWidth = 50;
            int addButtonHeight = CustomInputHeight + 4;
            int inputX = popupBounds.X + PopupPadding - 10;
            int inputWidth = popupBounds.Width - (PopupPadding * 2) - addButtonWidth - 8 + 10;

            customTagTextBox = new TextBox(
                Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                null,
                Game1.smallFont,
                Game1.textColor)
            {
                X = inputX,
                Y = inputY,
                Width = inputWidth,
                Text = ""
            };

            addCustomButton = new ClickableComponent(
                new Rectangle(
                    inputX + inputWidth + 8,
                    inputY - 2,
                    addButtonWidth,
                    addButtonHeight
                ),
                "addCustom"
            );
        }

        public bool HandleClick(int x, int y, out bool consumed)
        {
            consumed = false;

            if (!isOpen)
                return false;

            if (!selectOnly && editModeButton != null && editModeButton.containsPoint(x, y))
            {
                consumed = true;
                if (isEditMode)
                {
                    ExitEditMode();
                }
                else
                {
                    EnterEditMode();
                }
                return true;
            }

            if (isEditMode && deleteConfirmButton != null && deleteConfirmButton.containsPoint(x, y))
            {
                consumed = true;
                CommitTagDeletions();
                return true;
            }

            if (!popupBounds.Contains(x, y))
                return false;

            consumed = true;

            if (isEditMode)
            {
                for (int i = 0; i < tagDeleteButtons.Count; i++)
                {
                    var deleteBtn = tagDeleteButtons[i];
                    if (deleteBtn.visible && deleteBtn.containsPoint(x, y))
                    {
                        string tag = deleteBtn.name.Replace("delete:", "");
                        ToggleTagForDeletion(tag);
                        Game1.playSound("smallSelect");
                        return true;
                    }
                }
                return true;
            }

            foreach (var option in tagOptions)
            {
                if (option.visible && option.containsPoint(x, y))
                {
                    string tag = option.name;

                    if (tag == AllOptionKey)
                    {
                        allSelected = !allSelected;
                        onAllChanged?.Invoke(allSelected);
                    }
                    else if (allSelected)
                    {
                        // Tags are unclickable when "All" is selected
                    }
                    else if (selectedTags.Contains(tag))
                    {
                        selectedTags.Remove(tag);
                    }
                    else if (selectOnly || selectedTags.Count < MaxTagsPerSet)
                    {
                        selectedTags.Add(tag);
                    }

                    onComplete?.Invoke(selectedTags);
                    Game1.playSound("smallSelect");
                    return true;
                }
            }

            if (!selectOnly && addCustomButton != null && addCustomButton.containsPoint(x, y) && !isEditMode)
            {
                TryAddCustomTag();
                return true;
            }

            if (!selectOnly && customTagTextBox != null && !isEditMode)
            {
                Rectangle textBoxBounds = new Rectangle(
                    customTagTextBox.X,
                    customTagTextBox.Y,
                    customTagTextBox.Width,
                    CustomInputHeight
                );

                if (textBoxBounds.Contains(x, y))
                {
                    customInputFocused = true;
                    customTagTextBox.Selected = true;
                }
            }

            return true;
        }

        private void TryAddCustomTag()
        {
            if (customTagTextBox == null)
                return;

            string newTag = customTagTextBox.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(newTag))
                return;

            if (newTag.Length > MaxTagLength)
            {
                newTag = newTag.Substring(0, MaxTagLength);
            }

            bool tagAdded = false;

            if (store.GetAllTags().Any(t => t.Equals(newTag, TranslationCache.TagComparison)))
            {
                if (!selectedTags.Contains(newTag) && selectedTags.Count < MaxTagsPerSet)
                {
                    var existingTag = store.GetAllTags().First(t => t.Equals(newTag, TranslationCache.TagComparison));
                    selectedTags.Add(existingTag);
                    tagAdded = true;
                }
            }
            else
            {
                string titleCased = TranslationCache.ToTitleCase(newTag);
                store.AddTag(titleCased);
                if (selectedTags.Count < MaxTagsPerSet)
                {
                    selectedTags.Add(titleCased);
                    tagAdded = true;
                }
                BuildOptions();
            }

            if (tagAdded)
            {
                onComplete?.Invoke(selectedTags);
            }

            customTagTextBox.Text = "";
            Game1.playSound("coin");
        }

        private void EnterEditMode()
        {
            isEditMode = true;
            tagsMarkedForDeletion.Clear();
            BuildOptions();
            InitializeDeleteConfirmButton();
            Game1.playSound("smallSelect");
        }

        private void ExitEditMode()
        {
            isEditMode = false;
            tagsMarkedForDeletion.Clear();
            deleteConfirmButton = null;
            BuildOptions();
            Game1.playSound("bigDeSelect");
        }

        private void InitializeDeleteConfirmButton()
        {
            int customSectionY = tagListStartY + MaxVisibleOptions * OptionHeight + CustomSectionTopPadding;
            int availableHeight = CustomLabelHeight + CustomInputHeight;

            Vector2 textSize = Game1.smallFont.MeasureString(TranslationCache.TagsPopupDelete);
            int buttonWidth = (int)textSize.X + TextPadding * 3;
            int buttonHeight = (int)textSize.Y + TextPadding * 2;
            int buttonX = popupBounds.X + (popupBounds.Width - buttonWidth) / 2;
            int buttonY = customSectionY + availableHeight - buttonHeight;

            deleteConfirmButton = new ClickableComponent(
                new Rectangle(buttonX, buttonY, buttonWidth, buttonHeight),
                "deleteConfirm"
            );
        }

        private void ToggleTagForDeletion(string tag)
        {
            if (tagsMarkedForDeletion.Contains(tag))
                tagsMarkedForDeletion.Remove(tag);
            else
                tagsMarkedForDeletion.Add(tag);
        }

        private void CommitTagDeletions()
        {
            if (tagsMarkedForDeletion.Count == 0)
            {
                ExitEditMode();
                return;
            }

            foreach (var tag in tagsMarkedForDeletion)
                selectedTags.Remove(tag);

            store.RemoveTags(tagsMarkedForDeletion);
            onComplete?.Invoke(selectedTags);
            Game1.playSound("coin");

            isEditMode = false;
            tagsMarkedForDeletion.Clear();
            deleteConfirmButton = null;
            BuildOptions();
        }

        private int TotalOptionCount => store.GetAllTags().Count + (showAllOption ? 1 : 0);

        public bool HandleScrollWheel(int direction)
        {
            if (!isOpen)
                return false;

            if (direction > 0 && firstVisibleIndex > 0)
            {
                firstVisibleIndex--;
                BuildOptions();
                return true;
            }
            else if (direction < 0 && firstVisibleIndex < Math.Max(0, TotalOptionCount - MaxVisibleOptions))
            {
                firstVisibleIndex++;
                BuildOptions();
                return true;
            }

            return false;
        }

        public bool HandleKeyPress(Keys key)
        {
            if (!isOpen)
                return false;

            if (key == Keys.Escape)
            {
                if (isEditMode)
                {
                    isEditMode = false;
                    tagsMarkedForDeletion.Clear();
                    BuildOptions();
                    Game1.playSound("bigDeSelect");
                    return true;
                }

                Close();
                return false;
            }

            if (!selectOnly && key == Keys.Enter && customTagTextBox != null && customTagTextBox.Selected && !isEditMode)
            {
                TryAddCustomTag();
                return true;
            }

            return true;
        }

        public void Update()
        {
            if (!selectOnly && !isEditMode && customTagTextBox != null)
            {
                customTagTextBox.Update();
                customTagTextBox.Selected = customInputFocused;
            }
        }

        public void UpdateParentBounds(Rectangle newParentBounds)
        {
            parentBounds = newParentBounds;
            Recalculate();
        }

        private void Recalculate()
        {
            CalculateLayout();
            BuildOptions();
            if (!selectOnly)
                InitializeCustomTagInput();
        }

        public void Draw(SpriteBatch b)
        {
            if (!isOpen)
                return;

            hoverTooltip = null;

            UIHelpers.DrawTextureBox(b, popupBounds.X, popupBounds.Y,
                popupBounds.Width, popupBounds.Height, Color.White);

            int titleHeight = (int)Game1.smallFont.MeasureString("A").Y;

            Vector2 titlePos = new Vector2(
                popupBounds.X + PopupPadding,
                popupBounds.Y + PopupPadding
            );
            string title = isEditMode ? TranslationCache.TagsPopupTitleEdit : TranslationCache.TagsPopupTitle;
            Utility.drawTextWithShadow(b, title, Game1.smallFont,
                titlePos, Game1.textColor);

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            if (!selectOnly)
                DrawEditModeButton(b, mouseX, mouseY);

            DrawTagOptions(b, mouseX, mouseY);

            int arrowHeight = (int)(ArrowNativeHeight * ScrollArrowScale);

            if (firstVisibleIndex > 0 && upArrowButton != null)
            {
                Rectangle upArrowSource = new Rectangle(421, 459, 11, 12);
                Vector2 upArrowPos = new Vector2(upArrowButton.bounds.X, tagListStartY + 4);
                b.Draw(Game1.mouseCursors, upArrowPos, upArrowSource, Color.White, 0f,
                    Vector2.Zero, ScrollArrowScale, SpriteEffects.None, 1f);
            }

            if (firstVisibleIndex < Math.Max(0, TotalOptionCount - MaxVisibleOptions) && downArrowButton != null)
            {
                Rectangle downArrowSource = new Rectangle(421, 472, 11, 12);
                Vector2 downArrowPos = new Vector2(
                    downArrowButton.bounds.X,
                    tagListStartY + MaxVisibleOptions * OptionHeight - arrowHeight - 8
                );
                b.Draw(Game1.mouseCursors, downArrowPos, downArrowSource, Color.White, 0f,
                    Vector2.Zero, ScrollArrowScale, SpriteEffects.None, 1f);
            }

            if (!selectOnly)
            {
                DrawCustomSection(b, mouseX, mouseY, MaxVisibleOptions * OptionHeight);

                if (!isEditMode)
                {
                    string countText = $"{selectedTags.Count}/{MaxTagsPerSet}";
                    Vector2 countSize = Game1.smallFont.MeasureString(countText);
                    Vector2 countPos = new Vector2(
                        popupBounds.Right - PopupPadding - EditModeButtonSize - 8 - countSize.X,
                        popupBounds.Y + PopupPadding
                    );
                    Color countColor = selectedTags.Count >= MaxTagsPerSet ? Color.Orange : Color.Gray;
                    Utility.drawTextWithShadow(b, countText, Game1.smallFont, countPos, countColor);
                }
            }

            if (hoverTooltip != null && ModEntry.Config.ShowTooltip)
            {
                string text = hoverTooltip.Contains(' ')
                    ? Game1.parseText(hoverTooltip, Game1.smallFont, 300)
                    : hoverTooltip;
                IClickableMenu.drawHoverText(b, text, Game1.smallFont);
            }
        }

        private void DrawEditModeButton(SpriteBatch b, int mouseX, int mouseY)
        {
            if (editModeButton == null)
                return;

            bool isHovered = editModeButton.containsPoint(mouseX, mouseY);
            float buttonScale = isHovered ? 1.1f : 1f;

            int bgSize = (int)(editModeButton.bounds.Width * buttonScale);
            int bgX = editModeButton.bounds.X + (editModeButton.bounds.Width - bgSize) / 2;
            int bgY = editModeButton.bounds.Y + (editModeButton.bounds.Height - bgSize) / 2;

            Vector2 iconCenter = new Vector2(
                editModeButton.bounds.X + editModeButton.bounds.Width / 2,
                editModeButton.bounds.Y + editModeButton.bounds.Height / 2
            );

            if (isEditMode)
            {
                Rectangle closeSource = new Rectangle(337, 494, 12, 12);
                float closeScale = (bgSize / 12f) * 0.9f;
                Vector2 closeOrigin = new Vector2(6, 6);
                b.Draw(Game1.mouseCursors, iconCenter, closeSource, Color.White, 0f,
                    closeOrigin, closeScale, SpriteEffects.None, 1f);
            }
            else
            {
                UIHelpers.DrawTextureBox(b, bgX, bgY, bgSize, bgSize, Color.White);

                Rectangle editSource = new Rectangle(30, 428, 10, 10);
                float iconScale = (bgSize / 10f) * 0.6f;
                Vector2 origin = new Vector2(5, 5);
                b.Draw(Game1.mouseCursors, iconCenter, editSource, Color.White, 0f,
                    origin, iconScale, SpriteEffects.None, 1f);
            }
        }

        private void DrawTagOptions(SpriteBatch b, int mouseX, int mouseY)
        {
            for (int i = 0; i < tagOptions.Count; i++)
            {
                var option = tagOptions[i];
                if (!option.visible)
                    continue;

                string tag = option.name;
                bool isSelected = selectedTags.Contains(tag);
                bool isHovered = option.containsPoint(mouseX, mouseY);
                bool atLimit = selectedTags.Count >= MaxTagsPerSet && !isSelected;
                bool isMarkedForDeletion = tagsMarkedForDeletion.Contains(tag);

                float rowOpacity = isMarkedForDeletion ? MarkedForDeletionOpacity : 1f;
                string displayText = i < truncatedTagTexts.Count ? truncatedTagTexts[i] : TranslationCache.GetTagDisplayName(tag);
                string fullText = i < fullTagTexts.Count ? fullTagTexts[i] : TranslationCache.GetTagDisplayName(tag);

                if (isEditMode)
                {
                    float textHeight = Game1.smallFont.MeasureString(displayText).Y;
                    Vector2 textPos = new Vector2(
                        option.bounds.X + 4,
                        option.bounds.Y + (option.bounds.Height - textHeight) / 2
                    );
                    Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPos, Game1.textColor * rowOpacity);

                    if (i < tagDeleteButtons.Count)
                    {
                        var deleteBtn = tagDeleteButtons[i];
                        if (deleteBtn.visible)
                        {
                            DrawDeleteButton(b, deleteBtn, isMarkedForDeletion, mouseX, mouseY, rowOpacity);
                        }
                    }
                }
                else
                {
                    bool isAllOption = tag == AllOptionKey;
                    bool isChecked = isAllOption ? allSelected : isSelected;
                    bool isDimmed = allSelected && !isAllOption;

                    if (isHovered && !atLimit && !isDimmed)
                    {
                        b.Draw(Game1.staminaRect, option.bounds, Color.Wheat * 0.3f);
                    }

                    Rectangle checkboxSource = isChecked
                        ? new Rectangle(236, 425, 9, 9)
                        : new Rectangle(227, 425, 9, 9);

                    float checkboxOpacity = isDimmed ? 0.4f : 1f;
                    int checkboxY = option.bounds.Y + (option.bounds.Height - SaveSetLocalOnlyCheckboxSize) / 2;
                    b.Draw(Game1.mouseCursors,
                        new Vector2(option.bounds.X + 4, checkboxY),
                        checkboxSource, Color.White * checkboxOpacity, 0f, Vector2.Zero, SaveSetLocalOnlyCheckboxScale, SpriteEffects.None, 1f);

                    float textHeight = Game1.smallFont.MeasureString(displayText).Y;
                    Vector2 textPos = new Vector2(
                        option.bounds.X + 4 + SaveSetLocalOnlyCheckboxSize + 8,
                        option.bounds.Y + (option.bounds.Height - textHeight) / 2
                    );

                    Color textColor = isDimmed ? Game1.textColor * 0.4f : (atLimit ? Color.Gray : Game1.textColor);
                    Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPos, textColor);
                }

                if (isHovered && displayText != fullText)
                    hoverTooltip = fullText;
            }
        }

        private void DrawDeleteButton(SpriteBatch b, ClickableComponent button, bool isMarked, int mouseX, int mouseY, float rowOpacity)
        {
            bool isHovered = button.containsPoint(mouseX, mouseY);
            float scale = isHovered ? 2.0f : 1.8f;

            Rectangle sourceRect = new Rectangle(337, 494, 12, 12);

            Vector2 center = new Vector2(
                button.bounds.X + button.bounds.Width / 2,
                button.bounds.Y + button.bounds.Height / 2
            );
            Vector2 origin = new Vector2(6, 6);

            b.Draw(Game1.mouseCursors, center, sourceRect, Color.White * rowOpacity, 0f,
                origin, scale, SpriteEffects.None, 1f);
        }

        private void DrawCustomSection(SpriteBatch b, int mouseX, int mouseY, int tagListHeight)
        {
            if (isEditMode)
            {
                DrawDeleteConfirmButton(b, mouseX, mouseY, tagListHeight);
                return;
            }

            if (customTagTextBox == null || addCustomButton == null)
                return;

            int customLabelY = tagListStartY + tagListHeight + CustomSectionTopPadding;
            Vector2 labelPos = new Vector2(
                popupBounds.X + PopupPadding,
                customLabelY
            );
            Utility.drawTextWithShadow(b, TranslationCache.TagsPopupCustom, Game1.smallFont,
                labelPos, Game1.textColor);

            customTagTextBox.Draw(b);

            bool isAddButtonHovered = addCustomButton.containsPoint(mouseX, mouseY);
            UIHelpers.DrawTextureBox(b, addCustomButton.bounds.X, addCustomButton.bounds.Y,
                addCustomButton.bounds.Width, addCustomButton.bounds.Height,
                Color.White, shadowOffset: 2, shadowOpacity: 0.3f);

            if (isAddButtonHovered)
            {
                b.Draw(Game1.staminaRect, addCustomButton.bounds, HoverEffectColor);
            }

            Vector2 addTextSize = Game1.smallFont.MeasureString("+");
            Vector2 addTextPos = UIHelpers.GetVisualCenter(addCustomButton.bounds, addTextSize);
            Utility.drawTextWithShadow(b, "+", Game1.smallFont, addTextPos, Game1.textColor);
        }

        private void DrawDeleteConfirmButton(SpriteBatch b, int mouseX, int mouseY, int tagListHeight)
        {
            if (deleteConfirmButton == null)
                return;

            UIHelpers.DrawTextButton(b, deleteConfirmButton, TranslationCache.TagsPopupDelete);
        }
    }
}
