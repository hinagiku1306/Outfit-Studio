using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OutfitStudio.Models;
using StardewValley;
using StardewValley.Menus;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio
{
    public class ScheduleDebugLogUIBuilder
    {
        public Rectangle TitleBoxBounds { get; private set; }
        public Rectangle ContentBoxBounds { get; private set; }
        public ClickableTextureComponent CloseButton { get; private set; } = null!;
        public ClickableComponent CloseMenuButton { get; private set; } = null!;
        public ClickableComponent CollapseButton { get; private set; } = null!;
        public Rectangle EntryListClipRect { get; private set; }

        public int ScrollOffset { get; set; }
        public int MaxScrollOffset { get; private set; }
        public bool IsScrollable => MaxScrollOffset > 0;

        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        private int contentX;
        private int contentWidth;
        private int entryListY;
        private int entryListHeight;
        private readonly List<int> entryYOffsets = new();
        private readonly List<int> entryHeights = new();

        private const int SectionHeaderLeftExtend = 10;
        private const int ContextColumnGap = 40;
        private const float FailIconScale = 1.5f;
        private static readonly Rectangle FailIconSourceRect = new(337, 494, 12, 12);
        private const float DebugSpeedIconScale = 3f;
        private static readonly int DebugSpeedIconSize = (int)(10 * DebugSpeedIconScale);
        private static readonly Rectangle SpeedIconSourceRect = new(130, 428, 10, 10);

        public ScheduleDebugLogUIBuilder()
        {
            Width = ScheduleDebugLogWidth;
            Recalculate(0, new Dictionary<int, int>());
        }

        public void Recalculate(int entryCount, Dictionary<int, int> expandedRuleHeights)
        {
            entryYOffsets.Clear();
            entryHeights.Clear();

            int totalListHeight = 0;
            for (int i = 0; i < entryCount; i++)
            {
                if (i > 0) totalListHeight += ScheduleDebugItemGap;
                entryYOffsets.Add(totalListHeight);

                int h = expandedRuleHeights.TryGetValue(i, out int ruleSectionHeight)
                    ? CalculateExpandedHeight(ruleSectionHeight)
                    : ScheduleDebugCollapsedRowHeight;
                entryHeights.Add(h);
                totalListHeight += h;
            }

            int maxVisibleHeight = ScheduleDebugMaxVisibleEntries * ScheduleDebugCollapsedRowHeight
                + (ScheduleDebugMaxVisibleEntries - 1) * ScheduleDebugItemGap;
            entryListHeight = entryCount > 0 ? Math.Min(maxVisibleHeight, totalListHeight) : maxVisibleHeight;

            MaxScrollOffset = Math.Max(0, totalListHeight - entryListHeight);
            ClampScrollOffset();

            Vector2 titleSize = Game1.dialogueFont.MeasureString(TranslationCache.ScheduleDebugTitle);
            int titleBoxHeight = ScheduleTitleTopPadding + (int)titleSize.Y + ScheduleTitleBottomPadding;

            int contentInner = ScheduleDebugTopPadding + entryListHeight
                + ScheduleDebugListToButtonGap + TabAndButtonHeight + ScheduleDebugBottomPadding;
            int totalHeight = titleBoxHeight + contentInner;

            Height = totalHeight;
            X = (Game1.uiViewport.Width - Width) / 2;
            Y = (Game1.uiViewport.Height - Height) / 2 - 10;

            int currentY = Y;

            int titleBoxWidth = (int)titleSize.X + ScheduleTitleSidePadding * 2;
            int titleBoxX = X + (Width - titleBoxWidth) / 2;
            TitleBoxBounds = new Rectangle(titleBoxX, currentY, titleBoxWidth, titleBoxHeight);
            currentY += titleBoxHeight;

            ContentBoxBounds = new Rectangle(X, currentY, Width, contentInner);

            contentX = ContentBoxBounds.X + ScheduleDebugBorderPadding;
            contentWidth = ContentBoxBounds.Width - ScheduleDebugBorderPadding * 2 - 20;

            entryListY = ContentBoxBounds.Y + ScheduleDebugTopPadding;
            EntryListClipRect = new Rectangle(ContentBoxBounds.X, entryListY, ContentBoxBounds.Width, entryListHeight);

            int buttonsY = entryListY + entryListHeight + ScheduleDebugListToButtonGap;
            int maxBtnWidth = (Width - ScheduleDebugBorderPadding * 2 - ScheduleDebugButtonGap) / 2;
            int collapseWidth = UIHelpers.CalculateButtonWidth(TranslationCache.ScheduleDebugCollapse, maxBtnWidth);
            int closeWidth = UIHelpers.CalculateButtonWidth(TranslationCache.ScheduleClose, maxBtnWidth);
            int btnWidth = Math.Max(collapseWidth, closeWidth);

            int totalBtnWidth = btnWidth + ScheduleDebugButtonGap + btnWidth;
            int btnStartX = X + (Width - totalBtnWidth) / 2;

            CollapseButton = new ClickableComponent(
                new Rectangle(btnStartX, buttonsY, btnWidth, TabAndButtonHeight), "Collapse");
            CloseMenuButton = new ClickableComponent(
                new Rectangle(btnStartX + btnWidth + ScheduleDebugButtonGap, buttonsY, btnWidth, TabAndButtonHeight), "Close");

            CloseButton = new ClickableTextureComponent(
                new Rectangle(X + Width - CloseButtonSize - CloseButtonEdgeMargin,
                    Y + CloseButtonEdgeMargin, CloseButtonSize, CloseButtonSize),
                Game1.mouseCursors, new Rectangle(337, 494, 12, 12), 4f);
        }

        public void ClampScrollOffset()
        {
            ScrollOffset = Math.Clamp(ScrollOffset, 0, MaxScrollOffset);
        }

        public void EnsureEntryVisible(int entryIndex)
        {
            if (entryIndex < 0 || entryIndex >= entryYOffsets.Count) return;

            int entryTop = entryYOffsets[entryIndex];
            int entryBottom = entryTop + entryHeights[entryIndex];
            int visibleBottom = ScrollOffset + entryListHeight;

            if (entryBottom > visibleBottom)
                ScrollOffset = Math.Min(entryBottom - entryListHeight, entryTop);
            else if (entryTop < ScrollOffset)
                ScrollOffset = entryTop;

            ClampScrollOffset();
        }

        internal static int CalculateExpandedHeight(int ruleSectionHeight)
        {
            int rulesSection = ruleSectionHeight > 0
                ? ScheduleDebugSectionGap + ScheduleDebugSectionHeaderHeight + ruleSectionHeight
                : 0;

            return ScheduleDebugCollapsedRowHeight
                + ScheduleDebugExpandedSectionPad
                // Context: 3 lines
                + ScheduleDebugSectionHeaderHeight + 3 * ScheduleDebugContextLineHeight
                + ScheduleDebugSectionGap
                // Result: 5 lines (Selected, Reason, Outfit, Status, Rotation)
                + ScheduleDebugSectionHeaderHeight + 5 * ScheduleDebugContextLineHeight
                // Rules section (0 for manual override, otherwise header + content)
                + rulesSection
                + ScheduleDebugExpandedSectionPad;
        }

        public int GetEntryIndexAtPoint(int x, int y)
        {
            if (!EntryListClipRect.Contains(x, y))
                return -1;

            int adjustedY = y - entryListY + ScrollOffset;

            for (int i = 0; i < entryYOffsets.Count; i++)
            {
                int entryTop = entryYOffsets[i];
                int entryBottom = entryTop + entryHeights[i];
                if (adjustedY >= entryTop && adjustedY < entryBottom)
                    return i;
            }
            return -1;
        }

        public bool IsClickOnCollapsedSummary(int x, int y, int entryIndex)
        {
            if (entryIndex < 0 || entryIndex >= entryYOffsets.Count)
                return false;

            int adjustedY = y - entryListY + ScrollOffset;
            int entryTop = entryYOffsets[entryIndex];
            return adjustedY >= entryTop && adjustedY < entryTop + ScheduleDebugCollapsedRowHeight;
        }

        public int GetEntryScreenY(int entryIndex)
        {
            if (entryIndex < 0 || entryIndex >= entryYOffsets.Count)
                return entryListY;
            return entryListY + entryYOffsets[entryIndex] - ScrollOffset;
        }

        public int GetEntryHeight(int entryIndex)
        {
            if (entryIndex < 0 || entryIndex >= entryHeights.Count)
                return ScheduleDebugCollapsedRowHeight;
            return entryHeights[entryIndex];
        }

        // --- Drawing ---

        public void DrawTitle(SpriteBatch b)
        {
            string title = TranslationCache.ScheduleDebugTitle;
            Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
            Utility.drawTextWithShadow(b, title, Game1.dialogueFont,
                new Vector2(TitleBoxBounds.X + (TitleBoxBounds.Width - titleSize.X) / 2,
                    TitleBoxBounds.Y + ScheduleTitleTopPadding),
                Game1.textColor);
        }

        public string? DrawCollapsedEntry(SpriteBatch b, int rowY, int entryHeight,
            ScheduleEvalEntry entry, bool isExpanded, bool isHovered, int mouseX, int mouseY)
        {
            string? hoveredTooltip = null;

            if (isHovered && !isExpanded)
            {
                var hoverRect = Rectangle.Intersect(
                    new Rectangle(contentX, rowY, contentWidth, ScheduleDebugCollapsedRowHeight),
                    EntryListClipRect);
                if (hoverRect.Width > 0 && hoverRect.Height > 0)
                    b.Draw(Game1.staminaRect, hoverRect, Color.Wheat * 0.3f);
            }

            if (isExpanded)
            {
                var bgRect = Rectangle.Intersect(
                    new Rectangle(contentX, rowY, contentWidth, entryHeight),
                    EntryListClipRect);
                if (bgRect.Width > 0 && bgRect.Height > 0)
                    b.Draw(Game1.staminaRect, bgRect, Color.Wheat * 0.15f);
            }

            int line1Y = rowY + 6;
            int chevronX = contentX + ScheduleDebugChevronLeftMargin;

            string chevron = isExpanded ? "v" : ">";
            Utility.drawTextWithShadow(b, chevron, Game1.smallFont,
                new Vector2(chevronX, line1Y), Game1.textColor);

            int textStartX = chevronX + ScheduleDebugChevronSize + 10;

            string locationDisplay = !string.IsNullOrEmpty(entry.LocationDisplayName)
                ? entry.LocationDisplayName : entry.LocationName;
            string line1Text = entry.Trigger == "DayStarted"
                ? $"{TranslationCache.ScheduleDebugTriggerDayStarted} | {locationDisplay}"
                : locationDisplay;

            int maxTitleWidth = contentX + contentWidth - textStartX;
            string line1Display = UIHelpers.TruncateText(line1Text, maxTitleWidth);
            Utility.drawTextWithShadow(b, line1Display, Game1.smallFont,
                new Vector2(textStartX, line1Y), Game1.textColor);

            if (line1Display != line1Text && isHovered
                && mouseX >= textStartX && mouseX < textStartX + maxTitleWidth
                && mouseY >= line1Y && mouseY < line1Y + (int)Game1.smallFont.MeasureString("T").Y)
                hoveredTooltip = line1Text;

            int line2Y = line1Y + ScheduleDebugTitleToInfoGap + (int)Game1.smallFont.MeasureString("T").Y;
            int line2X = contentX + ScheduleDebugLine2Indent;
            int line2MaxWidth = contentWidth - ScheduleDebugLine2Indent;

            string rulePart = entry.WinningRuleName != null
                ? $"\"{entry.WinningRuleName}\"" : "\u2014";
            string outfitPart = entry.ChosenOutfitName ?? entry.ManualOverrideOutfitName ?? "\u2014";
            string arrow = " ~ ";
            int arrowWidth = (int)Game1.smallFont.MeasureString(arrow).X;
            int speedIconSpace = entry.WinnerAdvanceOnWarp ? (4 + DebugSpeedIconSize + 2) : 0;
            int halfWidth = (line2MaxWidth - arrowWidth - speedIconSpace) / 2;

            string ruleDisplay = UIHelpers.TruncateText(rulePart, halfWidth);
            string outfitDisplay = UIHelpers.TruncateText(outfitPart, halfWidth);
            string line2Full = $"{rulePart}{arrow}{outfitPart}";

            Color line2Color = Game1.textColor * 0.8f;
            Utility.drawTextWithShadow(b, ruleDisplay, Game1.smallFont,
                new Vector2(line2X, line2Y), line2Color);

            float cursorX = line2X + Game1.smallFont.MeasureString(ruleDisplay).X;
            if (entry.WinnerAdvanceOnWarp)
            {
                int iconY = line2Y + (ScheduleDebugContextLineHeight - 4 - DebugSpeedIconSize) / 2 + 1;
                DrawSpeedIcon(b, (int)cursorX + 4, iconY);
                cursorX += speedIconSpace;
            }

            Utility.drawTextWithShadow(b, arrow, Game1.smallFont,
                new Vector2(cursorX, line2Y), line2Color);
            cursorX += arrowWidth;

            Utility.drawTextWithShadow(b, outfitDisplay, Game1.smallFont,
                new Vector2(cursorX, line2Y), line2Color);

            string line2Display = ruleDisplay + arrow + outfitDisplay;
            if (line2Display != line2Full && isHovered
                && mouseX >= line2X && mouseX < line2X + line2MaxWidth
                && mouseY >= line2Y && mouseY < line2Y + (int)Game1.smallFont.MeasureString("T").Y)
                hoveredTooltip = line2Full;

            return hoveredTooltip;
        }

        public string? DrawExpandedSections(SpriteBatch b, int rowY, ScheduleEvalEntry entry,
            int mouseX, int mouseY)
        {
            string? tooltip = null;
            int sectionX = contentX + ScheduleDebugRuleIndent;
            int sectionWidth = contentWidth - ScheduleDebugRuleIndent;
            int headerX = sectionX - SectionHeaderLeftExtend;
            int headerWidth = sectionWidth + SectionHeaderLeftExtend;
            int lineMaxWidth = sectionWidth - 8;
            int currentY = rowY + ScheduleDebugCollapsedRowHeight + ScheduleDebugExpandedSectionPad;

            // --- Context ---
            DrawSectionHeader(b, headerX, currentY, headerWidth, TranslationCache.ScheduleDebugContext);
            currentY += ScheduleDebugSectionHeaderHeight;

            string areaText = entry.IsOutdoors
                ? TranslationCache.ScheduleEditAreaOutdoor
                : TranslationCache.ScheduleEditAreaIndoor;
            string festivalText = entry.TodaysFestivalIds.Count > 0
                ? string.Join(", ", entry.TodaysFestivalIds) : "\u2014";
            string weddingText = entry.IsWeddingDay
                ? TranslationCache.DeleteConfirmYes : TranslationCache.DeleteConfirmNo;

            string? ctxTip;
            ctxTip = DrawContextLine(b, sectionX, currentY, sectionWidth,
                TranslationCache.ScheduleEditSeason, entry.Season,
                TranslationCache.ScheduleEditWeather, entry.Weather, mouseX, mouseY);
            if (ctxTip != null) tooltip = ctxTip;
            currentY += ScheduleDebugContextLineHeight;

            ctxTip = DrawContextLine(b, sectionX, currentY, sectionWidth,
                TranslationCache.ScheduleEditLocation, entry.LocationName,
                TranslationCache.ScheduleEditArea, areaText, mouseX, mouseY);
            if (ctxTip != null) tooltip = ctxTip;
            currentY += ScheduleDebugContextLineHeight;

            ctxTip = DrawContextLine(b, sectionX, currentY, sectionWidth,
                TranslationCache.ScheduleEditFestival, festivalText,
                TranslationCache.ScheduleEditWedding, weddingText, mouseX, mouseY);
            if (ctxTip != null) tooltip = ctxTip;
            currentY += ScheduleDebugContextLineHeight + ScheduleDebugSectionGap;

            // --- Result ---
            DrawSectionHeader(b, headerX, currentY, headerWidth, TranslationCache.ScheduleDebugResult);
            currentY += ScheduleDebugSectionHeaderHeight;

            int lineX = sectionX + 8;

            // SELECTED line
            if (entry.WinningRuleName != null)
            {
                string selectedLabel = TranslationCache.ScheduleDebugLabelSelected + ": ";
                float selectedLabelW = Game1.smallFont.MeasureString(selectedLabel).X;
                string quotedName = $"\"{entry.WinningRuleName}\"";
                int speedIconSpace = entry.WinnerAdvanceOnWarp ? (4 + DebugSpeedIconSize + 2) : 0;

                int nameMaxW = lineMaxWidth - (int)selectedLabelW - speedIconSpace;
                string nameDisplay = UIHelpers.TruncateText(quotedName, nameMaxW);
                string textBefore = selectedLabel + nameDisplay;

                Utility.drawTextWithShadow(b, textBefore, Game1.smallFont,
                    new Vector2(lineX, currentY + 2), Game1.textColor);

                if (entry.WinnerAdvanceOnWarp)
                {
                    float afterNameX = lineX + Game1.smallFont.MeasureString(textBefore).X;
                    int iconY = currentY + 2 + (ScheduleDebugContextLineHeight - 4 - DebugSpeedIconSize) / 2 + 1;
                    DrawSpeedIcon(b, (int)afterNameX + 4, iconY);
                }

                if (nameDisplay != quotedName && IsHoveringLine(lineX, currentY, lineMaxWidth, mouseX, mouseY))
                    tooltip = entry.WinningRuleName;
            }
            else
            {
                Utility.drawTextWithShadow(b, TranslationCache.ScheduleDebugLabelSelected + ": \u2014", Game1.smallFont,
                    new Vector2(lineX, currentY + 2), Game1.textColor);
            }
            currentY += ScheduleDebugContextLineHeight;

            // Reason line
            {
                string reasonLabel = TranslationCache.ScheduleDebugLabelReason + ": ";
                string reasonValue;
                if (entry.ManualOverrideOutfitName != null)
                {
                    reasonValue = TranslationCache.ScheduleDebugReasonManual;
                }
                else if (entry.WinningRuleName == null)
                {
                    reasonValue = TranslationCache.ScheduleDebugNoMatch;
                }
                else if (entry.WasTiebreak)
                {
                    reasonValue = entry.IsConsistentTiebreak
                        ? TranslationCache.ScheduleDebugTiebreakConsistent
                        : TranslationCache.ScheduleDebugTiebreakRandom;
                }
                else if (entry.CacheOutcome == EvalCacheOutcome.Cached)
                {
                    reasonValue = "\u2014";
                }
                else
                {
                    int matchedCount = 0;
                    foreach (var r in entry.RuleResults)
                        if (r.MatchResult == RuleMatchResult.Matched) matchedCount++;
                    reasonValue = matchedCount > 1 ? TranslationCache.ScheduleEditPriority : "\u2014";
                }

                Utility.drawTextWithShadow(b, reasonLabel, Game1.smallFont,
                    new Vector2(lineX, currentY + 2), Game1.textColor);
                float reasonValueX = lineX + Game1.smallFont.MeasureString(reasonLabel).X;
                Color reasonColor = entry.WinningRuleName == null && entry.ManualOverrideOutfitName == null
                    ? Color.IndianRed
                    : Game1.textColor;
                Utility.drawTextWithShadow(b, reasonValue, Game1.smallFont,
                    new Vector2(reasonValueX, currentY + 2), reasonColor);

                if (entry.WasTiebreak && entry.TiedRuleNames is { Count: > 0 }
                    && IsHoveringLine(lineX, currentY, lineMaxWidth, mouseX, mouseY))
                {
                    tooltip = string.Join(", ", entry.TiedRuleNames);
                }
            }
            currentY += ScheduleDebugContextLineHeight;

            // Outfit line
            string outfitPrefix = TranslationCache.ScheduleDebugLabelOutfit + ": ";
            string? outfitName = entry.ChosenOutfitName ?? entry.ManualOverrideOutfitName;
            if (outfitName != null)
            {
                float outfitLabelW = Game1.smallFont.MeasureString(outfitPrefix).X;
                string quotedOutfit = $"\"{outfitName}\"";
                int nameMaxW = lineMaxWidth - (int)outfitLabelW;
                string nameDisplay = UIHelpers.TruncateText(quotedOutfit, nameMaxW);

                Utility.drawTextWithShadow(b, outfitPrefix, Game1.smallFont,
                    new Vector2(lineX, currentY + 2), Game1.textColor);
                float cx = lineX + outfitLabelW;

                Utility.drawTextWithShadow(b, nameDisplay, Game1.smallFont,
                    new Vector2(cx, currentY + 2), Game1.textColor);

                if (nameDisplay != quotedOutfit && IsHoveringLine(lineX, currentY, lineMaxWidth, mouseX, mouseY))
                    tooltip = outfitName;
            }
            else
            {
                Utility.drawTextWithShadow(b, outfitPrefix + "\u2014", Game1.smallFont,
                    new Vector2(lineX, currentY + 2), Game1.textColor);
            }
            currentY += ScheduleDebugContextLineHeight;

            // Status line
            string statusPrefix = TranslationCache.ScheduleDebugLabelStatus + ": ";
            string statusLine;
            if (entry.ChosenOutfitName != null)
            {
                string statusText = entry.WasAlreadyApplied
                    ? TranslationCache.ScheduleDebugNoChange
                    : TranslationCache.ScheduleDebugApplied;
                statusLine = statusPrefix + statusText;
            }
            else
            {
                statusLine = statusPrefix + "\u2014";
            }
            Utility.drawTextWithShadow(b, statusLine, Game1.smallFont,
                new Vector2(lineX, currentY + 2), Game1.textColor);
            currentY += ScheduleDebugContextLineHeight;

            // Rotation line
            string rotationPrefix = TranslationCache.ScheduleDebugLabelRotation + ": ";
            string rotationLine;
            if (entry.CacheOutcome == EvalCacheOutcome.Cached && !entry.IsNewRotationOutfit)
            {
                rotationLine = rotationPrefix + "\u2014";
            }
            else if (entry.WasReshuffled)
            {
                rotationLine = rotationPrefix
                    + TranslationCache.ScheduleDebugRotationReshuffled
                        .Replace("{{remaining}}", entry.QueueSizeAfter.ToString())
                        .Replace("{{total}}", entry.WinnerPoolSize.ToString());
            }
            else if (entry.QueueSizeAfter > 0)
            {
                rotationLine = rotationPrefix
                    + TranslationCache.ScheduleDebugRotationRemaining
                        .Replace("{{remaining}}", entry.QueueSizeAfter.ToString())
                        .Replace("{{total}}", entry.WinnerPoolSize.ToString());
            }
            else
            {
                rotationLine = rotationPrefix + "\u2014";
            }
            Utility.drawTextWithShadow(b, rotationLine, Game1.smallFont,
                new Vector2(lineX, currentY + 2), Game1.textColor);
            currentY += ScheduleDebugContextLineHeight;

            // --- Rules ---
            bool isManualOverride = entry.ManualOverrideOutfitName != null && entry.RuleResults.Count == 0;
            if (!isManualOverride)
            {
                currentY += ScheduleDebugSectionGap;
                DrawRulesHeader(b, headerX, currentY, headerWidth,
                    entry.EnabledRules, entry.TotalRules);
                currentY += ScheduleDebugSectionHeaderHeight;
            }

            if (isManualOverride)
            {
                // Manual override — no rules evaluated, skip section entirely
            }
            else if (entry.CacheOutcome == EvalCacheOutcome.Cached && entry.RuleResults.Count > 0)
            {
                foreach (var rule in entry.RuleResults)
                {
                    string? ruleTip = DrawMatchedRuleSummary(b, lineX, currentY, lineMaxWidth, rule, mouseX, mouseY);
                    if (ruleTip != null) tooltip = ruleTip;
                    currentY += ScheduleDebugContextLineHeight;
                }
            }
            else if (entry.RuleResults.Count == 0)
            {
                Utility.drawTextWithShadow(b, "\u2014", Game1.smallFont,
                    new Vector2(lineX, currentY + 2), Game1.textColor * 0.5f);
                currentY += ScheduleDebugContextLineHeight;
            }
            else
            {
                foreach (var rule in entry.RuleResults
                    .Where(r => r.MatchResult == RuleMatchResult.Matched)
                    .OrderByDescending(r => r.Priority))
                {
                    string? ruleTip = DrawMatchedRuleSummary(b, lineX, currentY, lineMaxWidth, rule, mouseX, mouseY);
                    if (ruleTip != null) tooltip = ruleTip;
                    currentY += ScheduleDebugContextLineHeight;
                }

                var failedRules = entry.RuleResults
                    .Where(r => r.MatchResult != RuleMatchResult.Matched)
                    .ToList();

                foreach (var (minPri, label) in new[] {
                    (4, TranslationCache.ScheduleDebugPriorityGroupSpecial),
                    (3, TranslationCache.ScheduleDebugPriorityGroupHigh),
                    (2, TranslationCache.ScheduleDebugPriorityGroupMedium),
                    (1, TranslationCache.ScheduleDebugPriorityGroupLow) })
                {
                    var group = failedRules.Where(r => GetPriorityGroup(r.Priority) == minPri).ToList();
                    if (group.Count == 0) continue;

                    currentY += PriorityGroupGap;
                    Utility.drawTextWithShadow(b, label, Game1.smallFont,
                        new Vector2(lineX, currentY + 2), Game1.textColor * 0.6f);
                    currentY += ScheduleDebugContextLineHeight + PriorityHeaderToRulesGap;

                    int indentedX = lineX + PriorityRuleExtraIndent;
                    int indentedMaxWidth = lineMaxWidth - PriorityRuleExtraIndent;
                    foreach (var rule in group)
                    {
                        string? ruleTip = DrawFailedRuleLine(b, indentedX, currentY, indentedMaxWidth, rule, mouseX, mouseY);
                        if (ruleTip != null) tooltip = ruleTip;
                        currentY += ScheduleDebugContextLineHeight;
                    }
                }
            }

            return tooltip;
        }

        private static void DrawSectionHeader(SpriteBatch b, int x, int y, int width, string text)
        {
            b.Draw(Game1.staminaRect,
                new Rectangle(x, y + ScheduleDebugSectionHeaderHeight - 2, width, 1),
                Color.Gray * 0.4f);

            Vector2 pos = new Vector2(x + 8, y + 4);
            Utility.drawTextWithShadow(b, text, Game1.smallFont,
                pos + new Vector2(-1, 0), Game1.textColor * 0.8f);
            Utility.drawTextWithShadow(b, text, Game1.smallFont, pos, Game1.textColor);
        }

        private static void DrawRulesHeader(SpriteBatch b, int x, int y, int width,
            int enabled, int total)
        {
            b.Draw(Game1.staminaRect,
                new Rectangle(x, y + ScheduleDebugSectionHeaderHeight - 2, width, 1),
                Color.Gray * 0.4f);

            Vector2 pos = new Vector2(x + 8, y + 4);
            string detail = TranslationCache.ScheduleDebugEnabledOfTotal
                .Replace("{{enabled}}", enabled.ToString())
                .Replace("{{total}}", total.ToString());
            string text = $"{TranslationCache.ScheduleDebugSchedules} ({detail})";

            Utility.drawTextWithShadow(b, text, Game1.smallFont,
                pos + new Vector2(-1, 0), Game1.textColor * 0.8f);
            Utility.drawTextWithShadow(b, text, Game1.smallFont, pos, Game1.textColor);
        }

        private static void DrawSpeedIcon(SpriteBatch b, int x, int y, Color? tint = null)
        {
            b.Draw(Game1.mouseCursors, new Vector2(x, y),
                SpeedIconSourceRect, tint ?? Color.White, 0f, Vector2.Zero,
                DebugSpeedIconScale, SpriteEffects.FlipHorizontally, 1f);
        }

        private static string? DrawContextLine(SpriteBatch b, int x, int y, int width,
            string leftLabel, string leftValue, string rightLabel, string rightValue,
            int mouseX, int mouseY)
        {
            string? tooltip = null;
            int leftStart = x + 8;
            int colWidth = (width - 8 - ContextColumnGap) / 2;
            int rightStart = leftStart + colWidth + ContextColumnGap;

            string leftFull = $"{leftLabel}: {leftValue}";
            string rightFull = $"{rightLabel}: {rightValue}";
            string leftDisplay = UIHelpers.TruncateText(leftFull, colWidth);
            string rightDisplay = UIHelpers.TruncateText(rightFull, colWidth);

            Utility.drawTextWithShadow(b, leftDisplay, Game1.smallFont,
                new Vector2(leftStart, y + 2), Game1.textColor * 0.9f);
            Utility.drawTextWithShadow(b, rightDisplay, Game1.smallFont,
                new Vector2(rightStart, y + 2), Game1.textColor * 0.9f);

            if (mouseY >= y && mouseY < y + ScheduleDebugContextLineHeight)
            {
                if (leftDisplay != leftFull && mouseX >= leftStart && mouseX < leftStart + colWidth)
                    tooltip = leftValue;
                else if (rightDisplay != rightFull && mouseX >= rightStart && mouseX < rightStart + colWidth)
                    tooltip = rightValue;
            }

            return tooltip;
        }

        private static string? DrawMatchedRuleSummary(SpriteBatch b, int lineX, int y,
            int maxWidth, RuleEvalEntry rule, int mouseX, int mouseY)
        {
            int lineY = y + 2;
            string quotedName = $"\"{rule.RuleName}\"";
            string prioritySuffix = $" ({GetPriorityLabel(rule.Priority)})";
            float suffixW = Game1.smallFont.MeasureString(prioritySuffix).X;
            int nameMaxW = maxWidth - (int)suffixW;
            string nameDisplay = UIHelpers.TruncateText(quotedName, nameMaxW);
            string displayText = nameDisplay + prioritySuffix;

            Utility.drawTextWithShadow(b, displayText, Game1.smallFont,
                new Vector2(lineX, lineY), Color.ForestGreen);

            if (nameDisplay != quotedName && IsHoveringLine(lineX, y, maxWidth, mouseX, mouseY))
                return rule.RuleName;
            return null;
        }

        private static string? DrawFailedRuleLine(SpriteBatch b, int lineX, int y,
            int maxWidth, RuleEvalEntry rule, int mouseX, int mouseY)
        {
            int lineY = y + 2;
            string quotedName = $"\"{rule.RuleName}\"";
            string failLabel = GetFailLabel(rule.MatchResult);
            string separator = " -- ";
            float separatorW = Game1.smallFont.MeasureString(separator).X;
            int xIconSize = (int)(FailIconSourceRect.Width * FailIconScale);
            int xIconGap = 6;
            float failLabelW = Game1.smallFont.MeasureString(failLabel).X;
            float suffixTotalW = separatorW + xIconSize + xIconGap + failLabelW;

            int nameMaxW = maxWidth - (int)suffixTotalW;
            string nameDisplay = UIHelpers.TruncateText(quotedName, nameMaxW);
            string prefixText = nameDisplay + separator;

            Color failColor = Color.IndianRed * 0.8f;
            Utility.drawTextWithShadow(b, nameDisplay, Game1.smallFont,
                new Vector2(lineX, lineY), Game1.textColor * 0.8f);

            float nameWidth = Game1.smallFont.MeasureString(nameDisplay).X;
            Utility.drawTextWithShadow(b, separator, Game1.smallFont,
                new Vector2(lineX + nameWidth, lineY), Game1.textColor * 0.5f);

            float textWidth = Game1.smallFont.MeasureString(prefixText).X;
            int iconX = lineX + (int)textWidth;
            int iconY = lineY + (ScheduleDebugContextLineHeight - 4 - xIconSize) / 2 + 2;
            b.Draw(Game1.mouseCursors, new Vector2(iconX, iconY),
                FailIconSourceRect, failColor, 0f, Vector2.Zero,
                FailIconScale, SpriteEffects.None, 1f);

            float failX = iconX + xIconSize + xIconGap;
            Utility.drawTextWithShadow(b, failLabel, Game1.smallFont,
                new Vector2(failX, lineY), failColor);

            if (nameDisplay != quotedName && IsHoveringLine(lineX, y, maxWidth, mouseX, mouseY))
                return rule.RuleName;
            return null;
        }

        private static bool IsHoveringLine(int lineX, int lineY, int width, int mouseX, int mouseY)
        {
            return mouseX >= lineX && mouseX < lineX + width
                && mouseY >= lineY && mouseY < lineY + ScheduleDebugContextLineHeight;
        }

        internal static string GetPriorityShortLabel(int priority)
        {
            return priority switch
            {
                >= 4 => "S",
                3 => "H",
                2 => "M",
                _ => "L"
            };
        }

        private static string GetFailLabel(RuleMatchResult result)
        {
            return result switch
            {
                RuleMatchResult.FailSeason => TranslationCache.ScheduleEditSeason,
                RuleMatchResult.FailFestival => TranslationCache.ScheduleEditFestival,
                RuleMatchResult.FailFestivalLocation => TranslationCache.ScheduleDebugFailFestivalLocation,
                RuleMatchResult.FailWeather => TranslationCache.ScheduleEditWeather,
                RuleMatchResult.FailLocation => TranslationCache.ScheduleEditLocation,
                RuleMatchResult.FailArea => TranslationCache.ScheduleEditArea,
                RuleMatchResult.FailWedding => TranslationCache.ScheduleEditWedding,
                RuleMatchResult.EmptyPool => TranslationCache.ScheduleDebugFailEmptyPool,
                _ => ""
            };
        }

        private static string GetPriorityLabel(int priority)
        {
            return priority switch
            {
                >= 4 => TranslationCache.ScheduleEditPrioritySpecial,
                3 => TranslationCache.ScheduleEditPriorityHigh,
                2 => TranslationCache.ScheduleEditPriorityMedium,
                _ => TranslationCache.ScheduleEditPriorityLow
            };
        }

        private static int GetPriorityGroup(int priority) => priority >= 4 ? 4 : priority >= 3 ? 3 : priority == 2 ? 2 : 1;

        private const int PriorityGroupGap = 8;
        private const int PriorityHeaderToRulesGap = 2;
        private const int PriorityRuleExtraIndent = 20;

        internal static int CalculateRuleSectionHeight(IReadOnlyList<RuleEvalEntry> rules, bool isManualOverride = false)
        {
            if (rules.Count == 0 && isManualOverride)
                return 0;

            if (rules.Count == 0)
                return ScheduleDebugContextLineHeight;

            int matched = 0, failSpecial = 0, failHigh = 0, failMed = 0, failLow = 0;
            foreach (var r in rules)
            {
                if (r.MatchResult == RuleMatchResult.Matched) matched++;
                else if (r.Priority >= 4) failSpecial++;
                else if (r.Priority >= 3) failHigh++;
                else if (r.Priority == 2) failMed++;
                else failLow++;
            }

            int totalPx = matched * ScheduleDebugContextLineHeight;

            foreach (int count in new[] { failSpecial, failHigh, failMed, failLow })
            {
                if (count == 0) continue;
                totalPx += PriorityGroupGap;
                totalPx += ScheduleDebugContextLineHeight + PriorityHeaderToRulesGap;
                totalPx += count * ScheduleDebugContextLineHeight;
            }

            return totalPx;
        }

        public void DrawNoEntriesText(SpriteBatch b)
        {
            string text = TranslationCache.ScheduleDebugNoEntries;
            Vector2 textSize = Game1.smallFont.MeasureString(text);
            float textX = contentX + (contentWidth - textSize.X) / 2;
            float textY = entryListY + (entryListHeight - textSize.Y) / 2;
            Utility.drawTextWithShadow(b, text, Game1.smallFont,
                new Vector2(textX, textY), Game1.textColor * 0.5f);
        }

        public void DrawButtons(SpriteBatch b)
        {
            UIHelpers.DrawTextButton(b, CollapseButton, TranslationCache.ScheduleDebugCollapse);
            UIHelpers.DrawTextButton(b, CloseMenuButton, TranslationCache.ScheduleClose);
        }

        public void DrawCloseButton(SpriteBatch b)
        {
            UIHelpers.DrawTextureButton(b, CloseButton);
        }

        public void DrawScrollIndicators(SpriteBatch b)
        {
            if (!IsScrollable) return;

            int arrowW = (int)(UIHelpers.UpScrollArrowSourceRect.Width * ScheduleScrollArrowScale);
            int arrowH = (int)(UIHelpers.UpScrollArrowSourceRect.Height * ScheduleScrollArrowScale);
            int rightPaddingStart = contentX + contentWidth;
            int rightPaddingWidth = ScheduleDebugBorderPadding;
            int arrowX = rightPaddingStart + (rightPaddingWidth - arrowW) / 2;

            if (ScrollOffset > 0)
            {
                int upY = entryListY + (ScheduleDebugCollapsedRowHeight - arrowH) / 2;
                b.Draw(Game1.mouseCursors, new Vector2(arrowX, upY),
                    UIHelpers.UpScrollArrowSourceRect, Color.White, 0f, Vector2.Zero,
                    ScheduleScrollArrowScale, SpriteEffects.None, 1f);
            }

            if (ScrollOffset < MaxScrollOffset)
            {
                int downY = entryListY + entryListHeight - ScheduleDebugCollapsedRowHeight
                    + (ScheduleDebugCollapsedRowHeight - arrowH) / 2;
                b.Draw(Game1.mouseCursors, new Vector2(arrowX, downY),
                    UIHelpers.DownScrollArrowSourceRect, Color.White, 0f, Vector2.Zero,
                    ScheduleScrollArrowScale, SpriteEffects.None, 1f);
            }
        }

        public void DrawEntrySeparator(SpriteBatch b, int separatorY)
        {
            b.Draw(Game1.staminaRect,
                new Rectangle(contentX, separatorY, contentWidth, 1),
                Color.Gray * 0.2f);
        }
    }
}
