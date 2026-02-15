using System.Collections.Generic;
using OutfitStudio.Models;
using Xunit;
using static OutfitStudio.OutfitLayoutConstants;

namespace OutfitStudio.Tests.UI
{
    public class CalculateExpandedHeightTests
    {
        [Fact]
        // Expected: Zero rule section height produces base expanded height with no rules section
        public void ZeroRuleSection_ProducesBaseHeight()
        {
            int result = ScheduleDebugLogUIBuilder.CalculateExpandedHeight(0);

            int expected = ScheduleDebugCollapsedRowHeight
                + ScheduleDebugExpandedSectionPad
                + ScheduleDebugSectionHeaderHeight + 3 * ScheduleDebugContextLineHeight
                + ScheduleDebugSectionGap
                + ScheduleDebugSectionHeaderHeight + 5 * ScheduleDebugContextLineHeight
                + ScheduleDebugExpandedSectionPad;

            Assert.Equal(expected, result);
        }

        [Fact]
        // Expected: Positive rule section height adds gap + header + content to base height
        public void PositiveRuleSection_AddsRulesSection()
        {
            int ruleSectionHeight = 100;
            int result = ScheduleDebugLogUIBuilder.CalculateExpandedHeight(ruleSectionHeight);

            int baseHeight = ScheduleDebugLogUIBuilder.CalculateExpandedHeight(0);
            int expected = baseHeight
                + ScheduleDebugSectionGap + ScheduleDebugSectionHeaderHeight + ruleSectionHeight;

            Assert.Equal(expected, result);
        }

        [Fact]
        // Expected: Height increases linearly with rule section height
        public void HeightIncreasesLinearly()
        {
            int h1 = ScheduleDebugLogUIBuilder.CalculateExpandedHeight(50);
            int h2 = ScheduleDebugLogUIBuilder.CalculateExpandedHeight(100);

            Assert.Equal(50, h2 - h1);
        }
    }

    public class CalculateRuleSectionHeightTests
    {
        [Fact]
        // Expected: Empty rules with manual override returns 0
        public void ManualOverride_NoRules_ReturnsZero()
        {
            var rules = new List<RuleEvalEntry>();
            int result = ScheduleDebugLogUIBuilder.CalculateRuleSectionHeight(rules, isManualOverride: true);
            Assert.Equal(0, result);
        }

        [Fact]
        // Expected: Empty rules without manual override returns at least one line height
        public void NoRules_NoOverride_ReturnsMinHeight()
        {
            var rules = new List<RuleEvalEntry>();
            int result = ScheduleDebugLogUIBuilder.CalculateRuleSectionHeight(rules);

            int minHeight = ScheduleDebugMinExpandedRules * ScheduleDebugContextLineHeight;
            int expected = System.Math.Max(minHeight, ScheduleDebugContextLineHeight);
            Assert.Equal(expected, result);
        }

        [Fact]
        // Expected: Single matched rule returns one line height (below min, so clamped to min)
        public void SingleMatchedRule_ReturnsMinHeight()
        {
            var rules = new List<RuleEvalEntry>
            {
                new() { RuleName = "Test", Priority = 3, MatchResult = RuleMatchResult.Matched }
            };
            int result = ScheduleDebugLogUIBuilder.CalculateRuleSectionHeight(rules);

            // Only matched, no failed → returns matched lines directly (no min clamp for matched-only)
            Assert.Equal(ScheduleDebugContextLineHeight, result);
        }

        [Fact]
        // Expected: Multiple matched rules return proportional height
        public void MultipleMatchedRules_ReturnsProportionalHeight()
        {
            var rules = new List<RuleEvalEntry>
            {
                new() { RuleName = "A", Priority = 3, MatchResult = RuleMatchResult.Matched },
                new() { RuleName = "B", Priority = 2, MatchResult = RuleMatchResult.Matched },
                new() { RuleName = "C", Priority = 1, MatchResult = RuleMatchResult.Matched },
            };
            int result = ScheduleDebugLogUIBuilder.CalculateRuleSectionHeight(rules);

            Assert.Equal(3 * ScheduleDebugContextLineHeight, result);
        }

        [Fact]
        // Expected: Single failed rule produces priority group header + rule line, clamped to min
        public void SingleFailedRule_ReturnsAtLeastMinHeight()
        {
            var rules = new List<RuleEvalEntry>
            {
                new() { RuleName = "Fail", Priority = 3, MatchResult = RuleMatchResult.FailSeason }
            };
            int result = ScheduleDebugLogUIBuilder.CalculateRuleSectionHeight(rules);

            int minHeight = ScheduleDebugMinExpandedRules * ScheduleDebugContextLineHeight;
            Assert.True(result >= minHeight);
        }

        [Fact]
        // Expected: Mixed matched and failed rules includes both sections
        public void MixedMatchedAndFailed_IncludesBoth()
        {
            var rules = new List<RuleEvalEntry>
            {
                new() { RuleName = "Win", Priority = 3, MatchResult = RuleMatchResult.Matched },
                new() { RuleName = "Lose", Priority = 2, MatchResult = RuleMatchResult.FailWeather }
            };
            int result = ScheduleDebugLogUIBuilder.CalculateRuleSectionHeight(rules);

            // matched line + priority group gap + header line + header-to-rules gap + failed line
            int matchedOnly = ScheduleDebugLogUIBuilder.CalculateRuleSectionHeight(
                new List<RuleEvalEntry> { rules[0] });
            Assert.True(result > matchedOnly);
        }

        [Fact]
        // Expected: Failed rules across multiple priority groups get separate headers
        public void MultiplePriorityGroups_GetSeparateHeaders()
        {
            var rules = new List<RuleEvalEntry>
            {
                new() { RuleName = "HighFail", Priority = 3, MatchResult = RuleMatchResult.FailSeason },
                new() { RuleName = "MedFail", Priority = 2, MatchResult = RuleMatchResult.FailWeather },
                new() { RuleName = "LowFail", Priority = 1, MatchResult = RuleMatchResult.FailLocation },
            };

            var singleGroup = new List<RuleEvalEntry>
            {
                new() { RuleName = "HighFail", Priority = 3, MatchResult = RuleMatchResult.FailSeason },
            };

            int multiResult = ScheduleDebugLogUIBuilder.CalculateRuleSectionHeight(rules);
            int singleResult = ScheduleDebugLogUIBuilder.CalculateRuleSectionHeight(singleGroup);

            Assert.True(multiResult > singleResult);
        }
    }

    public class GetPriorityShortLabelTests
    {
        [Theory]
        [InlineData(3, "H")]
        [InlineData(4, "H")]
        [InlineData(10, "H")]
        // Expected: Priority >= 3 returns "H"
        public void HighPriority_ReturnsH(int priority, string expected)
        {
            Assert.Equal(expected, ScheduleDebugLogUIBuilder.GetPriorityShortLabel(priority));
        }

        [Fact]
        // Expected: Priority 2 returns "M"
        public void MediumPriority_ReturnsM()
        {
            Assert.Equal("M", ScheduleDebugLogUIBuilder.GetPriorityShortLabel(2));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(0)]
        [InlineData(-1)]
        // Expected: Priority < 2 returns "L"
        public void LowPriority_ReturnsL(int priority)
        {
            Assert.Equal("L", ScheduleDebugLogUIBuilder.GetPriorityShortLabel(priority));
        }
    }
}
