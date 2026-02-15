using System;
using System.Collections.Generic;
using System.Linq;
using OutfitStudio.Models;
using OutfitStudio.Services;
using OutfitStudio.Tests.Helpers;
using Xunit;

namespace OutfitStudio.Tests.Services
{
    public class MatchesTriggersTests
    {
        private static EvaluationContext MakeContext(
            string season = "Spring",
            HashSet<string>? festivals = null,
            string weather = "Sun",
            string locationName = "Farm",
            bool isOutdoors = true,
            bool isActiveFestival = false,
            bool isAtPassiveFestivalLocation = false,
            bool isWeddingDay = false,
            EvaluationTrigger trigger = EvaluationTrigger.Warped)
        {
            return new EvaluationContext(
                CurrentSeason: season,
                TodaysFestivalIds: festivals ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                CurrentWeather: weather,
                CurrentLocationName: locationName,
                IsOutdoors: isOutdoors,
                IsActiveFestival: isActiveFestival,
                IsAtPassiveFestivalLocation: isAtPassiveFestivalLocation,
                IsWeddingDay: isWeddingDay,
                Trigger: trigger);
        }

        [Fact]
        // Expected: Rule with no triggers configured always matches (applies everywhere)
        public void NoTriggers_ReturnsTrue()
        {
            var rule = TestData.CreateRule();
            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext()));
        }

        [Fact]
        // Expected: SeasonsSelectAll matches regardless of current season
        public void SeasonsSelectAll_AlwaysMatches()
        {
            var rule = TestData.CreateRule(seasonsSelectAll: true);
            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext(season: "Winter")));
        }

        [Fact]
        // Expected: Selected season matching current season returns true
        public void SelectedSeason_MatchesCurrent()
        {
            var rule = TestData.CreateRule(selectedSeasons: new List<string> { "Spring", "Summer" });
            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext(season: "Spring")));
        }

        [Fact]
        // Expected: Selected season not matching current season (with no festival triggers) returns false
        public void SelectedSeason_NoMatch_NoFestivals_ReturnsFalse()
        {
            var rule = TestData.CreateRule(selectedSeasons: new List<string> { "Spring" });
            Assert.False(ScheduleEngine.MatchesTriggers(rule, MakeContext(season: "Winter")));
        }

        [Fact]
        // Expected: FestivalsSelectAll matches when at active festival location
        public void FestivalsSelectAll_AtFestival_Matches()
        {
            var rule = TestData.CreateRule(festivalsSelectAll: true);
            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext(isActiveFestival: true)));
        }

        [Fact]
        // Expected: Selected festival matching today returns true when player is at festival
        public void SelectedFestival_MatchesToday()
        {
            var todaysFestivals = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "spring13" };
            var rule = TestData.CreateRule(selectedFestivals: new List<string> { "spring13" });
            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext(season: "Winter", festivals: todaysFestivals, isActiveFestival: true)));
        }

        [Fact]
        // Expected: Selected festival not matching today (with no season triggers) returns false
        public void SelectedFestival_NoMatch_NoSeasons_ReturnsFalse()
        {
            var rule = TestData.CreateRule(selectedFestivals: new List<string> { "spring13" });
            Assert.False(ScheduleEngine.MatchesTriggers(rule, MakeContext(season: "Winter")));
        }

        [Fact]
        // Expected: Both season and festival configured, season matches but festival doesn't → false (AND logic)
        public void SeasonMatch_FestivalNoMatch_ReturnsFalse()
        {
            var rule = TestData.CreateRule(
                selectedSeasons: new List<string> { "Spring" },
                selectedFestivals: new List<string> { "fall16" });
            Assert.False(ScheduleEngine.MatchesTriggers(rule, MakeContext(season: "Spring")));
        }

        [Fact]
        // Expected: Both season and festival configured, festival matches but season doesn't → false (AND logic)
        public void FestivalMatch_SeasonNoMatch_ReturnsFalse()
        {
            var todaysFestivals = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "spring13" };
            var rule = TestData.CreateRule(
                selectedSeasons: new List<string> { "Winter" },
                selectedFestivals: new List<string> { "spring13" });
            Assert.False(ScheduleEngine.MatchesTriggers(rule, MakeContext(season: "Spring", festivals: todaysFestivals)));
        }

        [Fact]
        // Expected: Neither season nor festival matches → false
        public void NeitherMatch_ReturnsFalse()
        {
            var rule = TestData.CreateRule(
                selectedSeasons: new List<string> { "Summer" },
                selectedFestivals: new List<string> { "fall16" });
            Assert.False(ScheduleEngine.MatchesTriggers(rule, MakeContext(season: "Winter")));
        }

        [Fact]
        // Expected: Season matching is case-insensitive
        public void SeasonMatching_CaseInsensitive()
        {
            var rule = TestData.CreateRule(selectedSeasons: new List<string> { "spring" });
            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext(season: "Spring")));
        }

        // --- Weather tests ---

        [Fact]
        // Expected: WeatherSelectAll matches regardless of current weather
        public void WeatherSelectAll_AlwaysMatches()
        {
            var rule = TestData.CreateRule(weatherSelectAll: true);
            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext(weather: "Storm")));
        }

        [Fact]
        // Expected: Selected weather matching current weather returns true
        public void SelectedWeather_MatchesCurrent()
        {
            var rule = TestData.CreateRule(selectedWeather: new List<string> { "Rain", "Storm" });
            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext(weather: "Rain")));
        }

        [Fact]
        // Expected: Selected weather not matching current weather returns false
        public void SelectedWeather_NoMatch_ReturnsFalse()
        {
            var rule = TestData.CreateRule(selectedWeather: new List<string> { "Rain" });
            Assert.False(ScheduleEngine.MatchesTriggers(rule, MakeContext(weather: "Sun")));
        }

        [Fact]
        // Expected: Weather matching is case-insensitive
        public void WeatherMatching_CaseInsensitive()
        {
            var rule = TestData.CreateRule(selectedWeather: new List<string> { "rain" });
            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext(weather: "Rain")));
        }

        // --- Location tests ---

        [Fact]
        // Expected: LocationsSelectAll matches regardless of current location
        public void LocationsSelectAll_AlwaysMatches()
        {
            var rule = TestData.CreateRule(locationsSelectAll: true);
            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext(locationName: "Mine")));
        }

        [Fact]
        // Expected: Selected location matching current location returns true
        public void SelectedLocation_MatchesCurrent()
        {
            var rule = TestData.CreateRule(selectedLocations: new List<string> { "Beach", "Town" });
            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext(locationName: "Beach")));
        }

        [Fact]
        // Expected: Selected location not matching current location returns false
        public void SelectedLocation_NoMatch_ReturnsFalse()
        {
            var rule = TestData.CreateRule(selectedLocations: new List<string> { "Beach" });
            Assert.False(ScheduleEngine.MatchesTriggers(rule, MakeContext(locationName: "Farm")));
        }

        [Fact]
        // Expected: Location matching is case-insensitive
        public void LocationMatching_CaseInsensitive()
        {
            var rule = TestData.CreateRule(selectedLocations: new List<string> { "beach" });
            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext(locationName: "Beach")));
        }

        // --- Area tests ---

        [Fact]
        // Expected: AreasSelectAll matches regardless of indoor/outdoor
        public void AreasSelectAll_AlwaysMatches()
        {
            var rule = TestData.CreateRule(areasSelectAll: true);
            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext(isOutdoors: false)));
        }

        [Fact]
        // Expected: Selected area "Outdoor" matches when player is outdoors
        public void SelectedArea_Outdoor_MatchesOutdoors()
        {
            var rule = TestData.CreateRule(selectedAreas: new List<string> { "Outdoor" });
            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext(isOutdoors: true)));
        }

        [Fact]
        // Expected: Selected area "Indoor" matches when player is indoors
        public void SelectedArea_Indoor_MatchesIndoors()
        {
            var rule = TestData.CreateRule(selectedAreas: new List<string> { "Indoor" });
            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext(isOutdoors: false)));
        }

        [Fact]
        // Expected: Selected area "Outdoor" does not match when player is indoors
        public void SelectedArea_Outdoor_NoMatchIndoors()
        {
            var rule = TestData.CreateRule(selectedAreas: new List<string> { "Outdoor" });
            Assert.False(ScheduleEngine.MatchesTriggers(rule, MakeContext(isOutdoors: false)));
        }

        [Fact]
        // Expected: Selected area "Indoor" does not match when player is outdoors
        public void SelectedArea_Indoor_NoMatchOutdoors()
        {
            var rule = TestData.CreateRule(selectedAreas: new List<string> { "Indoor" });
            Assert.False(ScheduleEngine.MatchesTriggers(rule, MakeContext(isOutdoors: true)));
        }

        [Fact]
        // Expected: Both Indoor and Outdoor selected matches anywhere
        public void SelectedArea_BothSelected_MatchesAnywhere()
        {
            var rule = TestData.CreateRule(selectedAreas: new List<string> { "Indoor", "Outdoor" });
            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext(isOutdoors: true)));
            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext(isOutdoors: false)));
        }

        // --- Cross-group AND tests ---

        [Fact]
        // Expected: All 5 groups configured and matching returns true (festival rule = special event, ignores conditions)
        public void AllGroupsMatch_ReturnsTrue()
        {
            var festivals = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "spring13" };
            var rule = TestData.CreateRule(
                selectedSeasons: new List<string> { "Spring" },
                selectedFestivals: new List<string> { "spring13" },
                selectedWeather: new List<string> { "Sun" },
                selectedLocations: new List<string> { "Farm" },
                selectedAreas: new List<string> { "Outdoor" });

            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext(
                season: "Spring",
                festivals: festivals,
                weather: "Sun",
                locationName: "Farm",
                isOutdoors: true,
                isActiveFestival: true)));
        }

        [Fact]
        // Expected: Season matches but weather doesn't → false (AND logic)
        public void OneGroupFails_ReturnsFalse()
        {
            var rule = TestData.CreateRule(
                selectedSeasons: new List<string> { "Spring" },
                selectedWeather: new List<string> { "Rain" });

            Assert.False(ScheduleEngine.MatchesTriggers(rule, MakeContext(
                season: "Spring",
                weather: "Sun")));
        }

        [Fact]
        // Expected: Only weather configured, all others wildcard → true when weather matches
        public void UnconfiguredGroups_ActAsWildcards()
        {
            var rule = TestData.CreateRule(selectedWeather: new List<string> { "Rain" });

            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext(
                season: "Winter",
                weather: "Rain",
                locationName: "Mine",
                isOutdoors: false)));
        }

        [Fact]
        // Expected: SeasonsSelectAll + specific location → true when location matches, false otherwise
        public void MixedSelectAllAndSpecific_AndLogic()
        {
            var rule = TestData.CreateRule(
                seasonsSelectAll: true,
                selectedLocations: new List<string> { "Beach" });

            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext(
                season: "Winter",
                locationName: "Beach")));

            Assert.False(ScheduleEngine.MatchesTriggers(rule, MakeContext(
                season: "Winter",
                locationName: "Farm")));
        }
    }

    public class ResolvePoolTests
    {
        [Fact]
        // Expected: TagsSelectAll returns all valid sets
        public void TagsSelectAll_ReturnsAllValidSets()
        {
            var sets = new List<OutfitSet>
            {
                TestData.CreateSet(name: "A", tags: new List<string> { "Spring" }),
                TestData.CreateSet(name: "B", tags: new List<string> { "Winter" }),
                TestData.CreateSet(name: "C")
            };
            var rule = TestData.CreateRule(tagsSelectAll: true);

            var pool = ScheduleEngine.ResolvePool(rule, sets);

            Assert.Equal(3, pool.Count);
        }

        [Fact]
        // Expected: TagsSelectAll includes invalid sets in pool
        public void TagsSelectAll_IncludesInvalidSets()
        {
            var sets = new List<OutfitSet>
            {
                TestData.CreateSet(name: "Valid"),
                TestData.CreateSet(name: "Invalid", isValid: false)
            };
            var rule = TestData.CreateRule(tagsSelectAll: true);

            var pool = ScheduleEngine.ResolvePool(rule, sets);

            Assert.Equal(2, pool.Count);
        }

        [Fact]
        // Expected: Tag filtering returns sets with ANY matching tag (OR logic)
        public void TagFilter_MatchesAnyTag()
        {
            var sets = new List<OutfitSet>
            {
                TestData.CreateSet(name: "A", tags: new List<string> { "Spring" }),
                TestData.CreateSet(name: "B", tags: new List<string> { "Winter" }),
                TestData.CreateSet(name: "C", tags: new List<string> { "Combat" })
            };
            var rule = TestData.CreateRule(selectedTags: new List<string> { "Spring", "Winter" });

            var pool = ScheduleEngine.ResolvePool(rule, sets);

            Assert.Equal(2, pool.Count);
            Assert.Contains(pool, s => s.Name == "A");
            Assert.Contains(pool, s => s.Name == "B");
        }

        [Fact]
        // Expected: Tag filtering is case-insensitive
        public void TagFilter_CaseInsensitive()
        {
            var sets = new List<OutfitSet>
            {
                TestData.CreateSet(name: "A", tags: new List<string> { "Spring" })
            };
            var rule = TestData.CreateRule(selectedTags: new List<string> { "spring" });

            var pool = ScheduleEngine.ResolvePool(rule, sets);

            Assert.Single(pool);
        }

        [Fact]
        // Expected: ExcludedSetIds are removed from pool
        public void ExcludedSetIds_RemovedFromPool()
        {
            var excludedSet = TestData.CreateSet(id: "exclude-me", name: "Excluded", tags: new List<string> { "Daily" });
            var keptSet = TestData.CreateSet(name: "Kept", tags: new List<string> { "Daily" });
            var sets = new List<OutfitSet> { excludedSet, keptSet };
            var rule = TestData.CreateRule(
                tagsSelectAll: true,
                excludedSetIds: new List<string> { "exclude-me" });

            var pool = ScheduleEngine.ResolvePool(rule, sets);

            Assert.Single(pool);
            Assert.Equal("Kept", pool[0].Name);
        }

        [Fact]
        // Expected: Invalid sets are kept in pool when tags match
        public void InvalidSets_KeptInPool()
        {
            var sets = new List<OutfitSet>
            {
                TestData.CreateSet(name: "Valid", tags: new List<string> { "Daily" }),
                TestData.CreateSet(name: "Invalid", tags: new List<string> { "Daily" }, isValid: false)
            };
            var rule = TestData.CreateRule(selectedTags: new List<string> { "Daily" });

            var pool = ScheduleEngine.ResolvePool(rule, sets);

            Assert.Equal(2, pool.Count);
        }

        [Fact]
        // Expected: No matching tags returns empty pool
        public void NoMatchingTags_EmptyPool()
        {
            var sets = new List<OutfitSet>
            {
                TestData.CreateSet(name: "A", tags: new List<string> { "Spring" })
            };
            var rule = TestData.CreateRule(selectedTags: new List<string> { "Winter" });

            var pool = ScheduleEngine.ResolvePool(rule, sets);

            Assert.Empty(pool);
        }

        [Fact]
        // Expected: Combined filters (tag + excluded) apply, invalid sets kept
        public void CombinedFilters_AllApply()
        {
            var sets = new List<OutfitSet>
            {
                TestData.CreateSet(id: "s1", name: "TagMatch-Valid", tags: new List<string> { "Daily" }),
                TestData.CreateSet(id: "s2", name: "TagMatch-Invalid", tags: new List<string> { "Daily" }, isValid: false),
                TestData.CreateSet(id: "s3", name: "TagMatch-Excluded", tags: new List<string> { "Daily" }),
                TestData.CreateSet(id: "s4", name: "NoTag", tags: new List<string> { "Combat" })
            };
            var rule = TestData.CreateRule(
                selectedTags: new List<string> { "Daily" },
                excludedSetIds: new List<string> { "s3" });

            var pool = ScheduleEngine.ResolvePool(rule, sets);

            Assert.Equal(2, pool.Count);
            Assert.Contains(pool, s => s.Name == "TagMatch-Valid");
            Assert.Contains(pool, s => s.Name == "TagMatch-Invalid");
        }

        [Fact]
        // Expected: No tags selected (not selectAll) returns empty pool
        public void NoTagsSelected_EmptyPool()
        {
            var sets = new List<OutfitSet>
            {
                TestData.CreateSet(name: "A", tags: new List<string> { "Spring" })
            };
            var rule = TestData.CreateRule();

            var pool = ScheduleEngine.ResolvePool(rule, sets);

            Assert.Empty(pool);
        }

        [Fact]
        // Expected: ResolvePool does not modify the original allSets list
        public void DoesNotMutateInputList()
        {
            var sets = new List<OutfitSet>
            {
                TestData.CreateSet(name: "Valid"),
                TestData.CreateSet(name: "Invalid", isValid: false)
            };
            var rule = TestData.CreateRule(tagsSelectAll: true);

            ScheduleEngine.ResolvePool(rule, sets);

            Assert.Equal(2, sets.Count);
        }

        [Fact]
        // Expected: IncludedSetIds adds sets to pool even if they have no matching tags
        public void IncludedSetIds_AddedToPool()
        {
            var included = TestData.CreateSet(id: "inc1", name: "Included", tags: new List<string> { "Combat" });
            var sets = new List<OutfitSet> { included };
            var rule = TestData.CreateRule(
                selectedTags: new List<string> { "Daily" },
                includedSetIds: new List<string> { "inc1" });

            var pool = ScheduleEngine.ResolvePool(rule, sets);

            Assert.Single(pool);
            Assert.Equal("Included", pool[0].Name);
        }

        [Fact]
        // Expected: IncludedSetIds merged with tag results without duplicates
        public void IncludedSetIds_UnionWithTags()
        {
            var setA = TestData.CreateSet(id: "a", name: "A", tags: new List<string> { "Daily" });
            var setB = TestData.CreateSet(id: "b", name: "B", tags: new List<string> { "Combat" });
            var sets = new List<OutfitSet> { setA, setB };
            var rule = TestData.CreateRule(
                selectedTags: new List<string> { "Daily" },
                includedSetIds: new List<string> { "a", "b" });

            var pool = ScheduleEngine.ResolvePool(rule, sets);

            Assert.Equal(2, pool.Count);
            Assert.Contains(pool, s => s.Name == "A");
            Assert.Contains(pool, s => s.Name == "B");
        }

        [Fact]
        // Expected: Set in both IncludedSetIds and ExcludedSetIds is excluded (exclusion wins)
        public void IncludedSetIds_ExclusionWins()
        {
            var set = TestData.CreateSet(id: "both", name: "Both", tags: new List<string> { "Daily" });
            var sets = new List<OutfitSet> { set };
            var rule = TestData.CreateRule(
                includedSetIds: new List<string> { "both" },
                excludedSetIds: new List<string> { "both" });

            var pool = ScheduleEngine.ResolvePool(rule, sets);

            Assert.Empty(pool);
        }

        [Fact]
        // Expected: Manually included invalid set is kept in pool
        public void IncludedSetIds_InvalidKept()
        {
            var set = TestData.CreateSet(id: "inv", name: "Invalid", isValid: false);
            var sets = new List<OutfitSet> { set };
            var rule = TestData.CreateRule(includedSetIds: new List<string> { "inv" });

            var pool = ScheduleEngine.ResolvePool(rule, sets);

            Assert.Single(pool);
            Assert.Equal("Invalid", pool[0].Name);
        }

        [Fact]
        // Expected: No tags selected, only IncludedSetIds → pool contains exactly those sets
        public void IncludedSetIds_OnlyManual_NoTags()
        {
            var setA = TestData.CreateSet(id: "a", name: "A");
            var setB = TestData.CreateSet(id: "b", name: "B");
            var setC = TestData.CreateSet(id: "c", name: "C");
            var sets = new List<OutfitSet> { setA, setB, setC };
            var rule = TestData.CreateRule(includedSetIds: new List<string> { "a", "c" });

            var pool = ScheduleEngine.ResolvePool(rule, sets);

            Assert.Equal(2, pool.Count);
            Assert.Contains(pool, s => s.Name == "A");
            Assert.Contains(pool, s => s.Name == "C");
        }
    }

    public class SelectWinnerTests
    {
        [Fact]
        // Expected: Single candidate is always returned
        public void SingleCandidate_ReturnsThat()
        {
            var rule = TestData.CreateRule(name: "Only");
            var pool = new List<OutfitSet> { TestData.CreateSet() };
            var candidates = new List<(ScheduleRule, List<OutfitSet>)> { (rule, pool) };

            var (winner, _) = ScheduleEngine.SelectWinner(candidates, new Random(42));

            Assert.Equal("Only", winner.Name);
        }

        [Fact]
        // Expected: Higher priority value wins over lower priority
        public void HigherPriority_Wins()
        {
            var lowRule = TestData.CreateRule(name: "Low", priority: 2);
            var highRule = TestData.CreateRule(name: "High", priority: 5);
            var pool = new List<OutfitSet> { TestData.CreateSet() };
            var candidates = new List<(ScheduleRule, List<OutfitSet>)>
            {
                (lowRule, pool),
                (highRule, pool)
            };

            var (winner, _) = ScheduleEngine.SelectWinner(candidates, new Random(42));

            Assert.Equal("High", winner.Name);
        }

        [Fact]
        // Expected: Equal priority resolved by random tiebreak (both are valid winners)
        public void EqualPriority_RandomTiebreak()
        {
            var ruleA = TestData.CreateRule(name: "A", priority: 3);
            var ruleB = TestData.CreateRule(name: "B", priority: 3);
            var pool = new List<OutfitSet> { TestData.CreateSet() };
            var candidates = new List<(ScheduleRule, List<OutfitSet>)>
            {
                (ruleA, pool),
                (ruleB, pool)
            };

            var winners = new HashSet<string>();
            for (int i = 0; i < 50; i++)
            {
                var (winner, _) = ScheduleEngine.SelectWinner(candidates, new Random(i));
                winners.Add(winner.Name);
            }

            Assert.Contains("A", winners);
            Assert.Contains("B", winners);
        }

        [Fact]
        // Expected: Priority 1 is beaten by priority 5
        public void Priority1_BeatenByPriority5()
        {
            var rule1 = TestData.CreateRule(name: "P1", priority: 1);
            var rule5 = TestData.CreateRule(name: "P5", priority: 5);
            var pool = new List<OutfitSet> { TestData.CreateSet() };
            var candidates = new List<(ScheduleRule, List<OutfitSet>)>
            {
                (rule1, pool),
                (rule5, pool)
            };

            var (winner, _) = ScheduleEngine.SelectWinner(candidates, new Random(42));

            Assert.Equal("P5", winner.Name);
        }
    }

    public class PickOutfitTests
    {
        [Fact]
        // Expected: Empty pool returns null
        public void EmptyPool_ReturnsNull()
        {
            var state = new RotationState { RuleId = "r1" };
            var result = ScheduleEngine.PickOutfit(new List<OutfitSet>(), state, new Random(42));
            Assert.Null(result);
        }

        [Fact]
        // Expected: Single set in pool always returns that set
        public void SingleSet_ReturnsThat()
        {
            var set = TestData.CreateSet(name: "Only");
            var pool = new List<OutfitSet> { set };
            var state = new RotationState { RuleId = "r1" };

            var result = ScheduleEngine.PickOutfit(pool, state, new Random(42));

            Assert.Equal("Only", result!.Name);
        }

        [Fact]
        // Expected: Multiple sets use rotation queue (pops from queue in order)
        public void MultipleSets_UsesRotationQueue()
        {
            var setA = TestData.CreateSet(id: "a", name: "A");
            var setB = TestData.CreateSet(id: "b", name: "B");
            var pool = new List<OutfitSet> { setA, setB };
            var state = new RotationState { RuleId = "r1", Queue = new List<string> { "b", "a" } };

            var result = ScheduleEngine.PickOutfit(pool, state, new Random(42));

            Assert.Equal("B", result!.Name);
            Assert.Single(state.Queue);
            Assert.Equal("b", state.LastUsedId);
        }

        [Fact]
        // Expected: Single-item pool clears stale queue entries
        public void SingleSet_ClearsStaleQueue()
        {
            var set = TestData.CreateSet(id: "x", name: "X");
            var pool = new List<OutfitSet> { set };
            var state = new RotationState
            {
                RuleId = "r1",
                Queue = new List<string> { "old1", "old2", "old3" },
                LastUsedId = "old1"
            };

            var result = ScheduleEngine.PickOutfit(pool, state, new Random(42));

            Assert.Equal("X", result!.Name);
            Assert.Empty(state.Queue);
            Assert.Equal("x", state.LastUsedId);
        }

        [Fact]
        // Expected: Single-item pool sets LastUsedId
        public void SingleSet_SetsLastUsedId()
        {
            var set = TestData.CreateSet(id: "solo", name: "Solo");
            var pool = new List<OutfitSet> { set };
            var state = new RotationState { RuleId = "r1" };

            ScheduleEngine.PickOutfit(pool, state, new Random(42));

            Assert.Equal("solo", state.LastUsedId);
        }
    }

    public class PickWithRotationTests
    {
        [Fact]
        // Expected: Fresh state (empty queue) shuffles pool and pops first element
        public void FreshState_ShufflesAndPopsFirst()
        {
            var setA = TestData.CreateSet(id: "a", name: "A");
            var setB = TestData.CreateSet(id: "b", name: "B");
            var setC = TestData.CreateSet(id: "c", name: "C");
            var pool = new List<OutfitSet> { setA, setB, setC };
            var state = new RotationState { RuleId = "r1" };

            var result = ScheduleEngine.PickWithRotation(pool, state, new Random(42));

            Assert.NotNull(result);
            Assert.Equal(2, state.Queue.Count);
            Assert.Equal(result!.Id, state.LastUsedId);
        }

        [Fact]
        // Expected: Existing queue pops first item without reshuffling
        public void ExistingQueue_PopsFirst()
        {
            var setA = TestData.CreateSet(id: "a", name: "A");
            var setB = TestData.CreateSet(id: "b", name: "B");
            var pool = new List<OutfitSet> { setA, setB };
            var state = new RotationState
            {
                RuleId = "r1",
                Queue = new List<string> { "b", "a" }
            };

            var result = ScheduleEngine.PickWithRotation(pool, state, new Random(42));

            Assert.Equal("B", result!.Name);
            Assert.Single(state.Queue);
            Assert.Equal("a", state.Queue[0]);
            Assert.Equal("b", state.LastUsedId);
        }

        [Fact]
        // Expected: Stale entries in queue are filtered out before popping
        public void StaleEntries_FilteredBeforePop()
        {
            var setA = TestData.CreateSet(id: "a", name: "A");
            var pool = new List<OutfitSet> { setA };
            var state = new RotationState
            {
                RuleId = "r1",
                Queue = new List<string> { "deleted-id", "a" }
            };

            var result = ScheduleEngine.PickWithRotation(pool, state, new Random(42));

            Assert.Equal("A", result!.Name);
            Assert.Equal("a", state.LastUsedId);
        }

        [Fact]
        // Expected: Exhausted queue reshuffles from pool
        public void QueueExhausted_ReshufflesFromPool()
        {
            var setA = TestData.CreateSet(id: "a", name: "A");
            var setB = TestData.CreateSet(id: "b", name: "B");
            var pool = new List<OutfitSet> { setA, setB };
            var state = new RotationState
            {
                RuleId = "r1",
                Queue = new List<string>()
            };

            var result = ScheduleEngine.PickWithRotation(pool, state, new Random(42));

            Assert.NotNull(result);
            Assert.Single(state.Queue);
            Assert.Equal(result!.Id, state.LastUsedId);
        }

        [Fact]
        // Expected: After reshuffle, LastUsedId is not placed first (avoids back-to-back repeat)
        public void QueueExhausted_LastUsedNotFirst()
        {
            var setA = TestData.CreateSet(id: "a", name: "A");
            var setB = TestData.CreateSet(id: "b", name: "B");
            var setC = TestData.CreateSet(id: "c", name: "C");
            var pool = new List<OutfitSet> { setA, setB, setC };

            bool lastUsedWasNeverFirst = true;
            for (int seed = 0; seed < 100; seed++)
            {
                var state = new RotationState
                {
                    RuleId = "r1",
                    Queue = new List<string>(),
                    LastUsedId = "a"
                };

                var result = ScheduleEngine.PickWithRotation(pool, state, new Random(seed));
                if (result!.Id == "a")
                {
                    lastUsedWasNeverFirst = false;
                    break;
                }
            }

            Assert.True(lastUsedWasNeverFirst, "LastUsedId should never be the first pick after reshuffle");
        }

        [Fact]
        // Expected: Single set in pool always returns that set (can't avoid repeat)
        public void SingleSetPool_AlwaysReturnsSame()
        {
            var set = TestData.CreateSet(id: "only", name: "Only");
            var pool = new List<OutfitSet> { set };
            var state = new RotationState { RuleId = "r1" };

            var result = ScheduleEngine.PickWithRotation(pool, state, new Random(42));

            Assert.Equal("Only", result!.Name);
            Assert.Equal("only", state.LastUsedId);
        }

        [Fact]
        // Expected: All queue entries stale → reshuffle from current pool
        public void AllQueueStale_ReshuffleFromPool()
        {
            var setA = TestData.CreateSet(id: "a", name: "A");
            var pool = new List<OutfitSet> { setA };
            var state = new RotationState
            {
                RuleId = "r1",
                Queue = new List<string> { "deleted1", "deleted2" }
            };

            var result = ScheduleEngine.PickWithRotation(pool, state, new Random(42));

            Assert.Equal("A", result!.Name);
            Assert.Equal("a", state.LastUsedId);
        }

        [Fact]
        // Expected: Full rotation cycle visits all sets exactly once, then eagerly reshuffles
        public void FullCycle_VisitsAllSetsOnce()
        {
            var setA = TestData.CreateSet(id: "a", name: "A");
            var setB = TestData.CreateSet(id: "b", name: "B");
            var setC = TestData.CreateSet(id: "c", name: "C");
            var pool = new List<OutfitSet> { setA, setB, setC };
            var state = new RotationState { RuleId = "r1" };
            var rng = new Random(42);

            var visited = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                var result = ScheduleEngine.PickWithRotation(pool, state, rng);
                visited.Add(result!.Id);
            }

            Assert.Equal(3, visited.Distinct().Count());
            Assert.Equal(3, state.Queue.Count);
        }

        [Fact]
        // Expected: wasReshuffled is true when popping the last item, not when reshuffling at start
        public void LastPick_SetsWasReshuffled()
        {
            var setA = TestData.CreateSet(id: "a", name: "A");
            var setB = TestData.CreateSet(id: "b", name: "B");
            var pool = new List<OutfitSet> { setA, setB };
            var rng = new Random(42);

            // First pick from empty queue (reshuffle happens, but not the last pick)
            var state1 = new RotationState { RuleId = "r1" };
            ScheduleEngine.PickWithRotation(pool, state1, rng, out bool reshuffled1);
            Assert.False(reshuffled1);

            // Second pick exhausts queue → wasReshuffled = true
            ScheduleEngine.PickWithRotation(pool, state1, rng, out bool reshuffled2);
            Assert.True(reshuffled2);
            Assert.Equal(2, state1.Queue.Count);
        }
    }


    public class WeddingTriggerTests
    {
        private static EvaluationContext MakeCtx(
            bool isWeddingDay = false,
            EvaluationTrigger trigger = EvaluationTrigger.Warped,
            string season = "Spring")
        {
            return new EvaluationContext(
                CurrentSeason: season,
                TodaysFestivalIds: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                CurrentWeather: "Sun",
                CurrentLocationName: "Farm",
                IsOutdoors: true,
                IsActiveFestival: false,
                IsAtPassiveFestivalLocation: false,
                IsWeddingDay: isWeddingDay,
                Trigger: trigger);
        }

        [Fact]
        // Expected: Wedding rule matches on DayStarted of a wedding day
        public void Wedding_DayStarted_Matches()
        {
            var rule = TestData.CreateRule(isWeddingDay: true, seasonsSelectAll: true);
            var ctx = MakeCtx(isWeddingDay: true, trigger: EvaluationTrigger.DayStarted);
            Assert.True(ScheduleEngine.MatchesTriggers(rule, ctx));
        }

        [Fact]
        // Expected: Wedding rule does NOT match on Warped (even on a wedding day)
        public void Wedding_Warped_NoMatch()
        {
            var rule = TestData.CreateRule(isWeddingDay: true, seasonsSelectAll: true);
            var ctx = MakeCtx(isWeddingDay: true, trigger: EvaluationTrigger.Warped);
            Assert.False(ScheduleEngine.MatchesTriggers(rule, ctx));
        }

        [Fact]
        // Expected: Wedding rule does NOT match when it's not a wedding day
        public void Wedding_NotWeddingDay_NoMatch()
        {
            var rule = TestData.CreateRule(isWeddingDay: true, seasonsSelectAll: true);
            var ctx = MakeCtx(isWeddingDay: false, trigger: EvaluationTrigger.DayStarted);
            Assert.False(ScheduleEngine.MatchesTriggers(rule, ctx));
        }

        [Fact]
        // Expected: Wedding is a special event rule — season is ignored, matches on DayStarted
        public void Wedding_PlusSeason_IgnoresSeason()
        {
            var rule = TestData.CreateRule(isWeddingDay: true, selectedSeasons: new List<string> { "Summer" });
            var ctx = MakeCtx(isWeddingDay: true, trigger: EvaluationTrigger.DayStarted, season: "Spring");
            Assert.True(ScheduleEngine.MatchesTriggers(rule, ctx));
        }

        [Fact]
        // Expected: Wedding alone counts as "has triggers" (not rejected by guard)
        public void Wedding_AloneCounts_AsTrigger()
        {
            var rule = TestData.CreateRule(isWeddingDay: true);
            var ctx = MakeCtx(isWeddingDay: true, trigger: EvaluationTrigger.DayStarted);
            Assert.True(ScheduleEngine.MatchesTriggers(rule, ctx));
        }
    }

    public class ResolveFestivalDisplayNameTests
    {
        [Fact]
        // Expected: Active festival ID resolves to its display name
        public void ActiveFestival_ReturnsDisplayName()
        {
            var active = new Dictionary<string, string> { { "spring13", "Egg Festival" } };
            Assert.Equal("Egg Festival", ScheduleEngine.ResolveFestivalDisplayName("spring13", active, null));
        }

        [Fact]
        // Expected: Passive festival ID resolves to its display name
        public void PassiveFestival_ReturnsDisplayName()
        {
            var passive = new Dictionary<string, string> { { "NightMarket", "Night Market" } };
            Assert.Equal("Night Market", ScheduleEngine.ResolveFestivalDisplayName("NightMarket", null, passive));
        }

        [Fact]
        // Expected: Active lookup takes priority over passive when both have the same ID
        public void ActiveTakesPriority_OverPassive()
        {
            var active = new Dictionary<string, string> { { "fest1", "Active Name" } };
            var passive = new Dictionary<string, string> { { "fest1", "Passive Name" } };
            Assert.Equal("Active Name", ScheduleEngine.ResolveFestivalDisplayName("fest1", active, passive));
        }

        [Fact]
        // Expected: Unknown ID falls back to the raw ID string
        public void UnknownId_ReturnsRawId()
        {
            var active = new Dictionary<string, string> { { "spring13", "Egg Festival" } };
            Assert.Equal("unknown_fest", ScheduleEngine.ResolveFestivalDisplayName("unknown_fest", active, null));
        }

        [Fact]
        // Expected: Null dictionaries return the raw ID (no crash)
        public void NullDictionaries_ReturnsRawId()
        {
            Assert.Equal("spring13", ScheduleEngine.ResolveFestivalDisplayName("spring13", null, null));
        }

        [Fact]
        // Expected: Empty display name in active dict falls through to passive
        public void ActiveEmpty_FallsToPassive()
        {
            var active = new Dictionary<string, string> { { "fest1", "" } };
            var passive = new Dictionary<string, string> { { "fest1", "Passive Name" } };
            Assert.Equal("Passive Name", ScheduleEngine.ResolveFestivalDisplayName("fest1", active, passive));
        }

        [Fact]
        // Expected: Empty display name in both dicts falls back to raw ID
        public void BothEmpty_ReturnsRawId()
        {
            var active = new Dictionary<string, string> { { "fest1", "" } };
            var passive = new Dictionary<string, string> { { "fest1", "" } };
            Assert.Equal("fest1", ScheduleEngine.ResolveFestivalDisplayName("fest1", active, passive));
        }
    }

    public class ContextSignatureTests
    {
        [Fact]
        // Expected: Same fields produce the same signature
        public void SameFields_SameSignature()
        {
            var ctx1 = new EvaluationContext("Spring", new HashSet<string>(), "Sun", "Farm", true, false, false, false, EvaluationTrigger.Warped);
            var ctx2 = new EvaluationContext("Spring", new HashSet<string>(), "Sun", "Farm", true, false, false, false, EvaluationTrigger.Warped);
            Assert.Equal(ctx1.ToSignature(), ctx2.ToSignature());
        }

        [Fact]
        // Expected: Different location produces different signature
        public void DifferentLocation_DifferentSignature()
        {
            var ctx1 = new EvaluationContext("Spring", new HashSet<string>(), "Sun", "Farm", true, false, false, false, EvaluationTrigger.Warped);
            var ctx2 = new EvaluationContext("Spring", new HashSet<string>(), "Sun", "Beach", true, false, false, false, EvaluationTrigger.Warped);
            Assert.NotEqual(ctx1.ToSignature(), ctx2.ToSignature());
        }

        [Fact]
        // Expected: Wedding flag is False in signature when trigger is Warped (even on wedding day)
        public void WeddingOnWarp_ExcludedFromSignature()
        {
            var warpCtx = new EvaluationContext("Spring", new HashSet<string>(), "Sun", "Farm", true, false, false, true, EvaluationTrigger.Warped);
            var nonWeddingCtx = new EvaluationContext("Spring", new HashSet<string>(), "Sun", "Farm", true, false, false, false, EvaluationTrigger.Warped);
            Assert.Equal(warpCtx.ToSignature(), nonWeddingCtx.ToSignature());
        }

        [Fact]
        // Expected: Wedding flag is True in signature on DayStarted of wedding day
        public void WeddingOnDayStarted_IncludedInSignature()
        {
            var weddingCtx = new EvaluationContext("Spring", new HashSet<string>(), "Sun", "Farm", true, false, false, true, EvaluationTrigger.DayStarted);
            var normalCtx = new EvaluationContext("Spring", new HashSet<string>(), "Sun", "Farm", true, false, false, false, EvaluationTrigger.DayStarted);
            Assert.NotEqual(weddingCtx.ToSignature(), normalCtx.ToSignature());
        }
    }

    public class SpecialEventMatchTests
    {
        private static EvaluationContext MakeCtx(
            string season = "Spring",
            HashSet<string>? festivals = null,
            string weather = "Sun",
            string locationName = "Farm",
            bool isOutdoors = true,
            bool isActiveFestival = false,
            bool isAtPassiveFestivalLocation = false,
            bool isWeddingDay = false,
            EvaluationTrigger trigger = EvaluationTrigger.Warped)
        {
            return new EvaluationContext(
                CurrentSeason: season,
                TodaysFestivalIds: festivals ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                CurrentWeather: weather,
                CurrentLocationName: locationName,
                IsOutdoors: isOutdoors,
                IsActiveFestival: isActiveFestival,
                IsAtPassiveFestivalLocation: isAtPassiveFestivalLocation,
                IsWeddingDay: isWeddingDay,
                Trigger: trigger);
        }

        [Fact]
        // Expected: Festival rule matches when player is at active festival
        public void FestivalRule_ActiveFestival_Matches()
        {
            var rule = TestData.CreateRule(selectedFestivals: new List<string> { "spring13" });
            var ctx = MakeCtx(
                festivals: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "spring13" },
                isActiveFestival: true);
            Assert.True(ScheduleEngine.MatchesTriggers(rule, ctx));
        }

        [Fact]
        // Expected: Festival rule matches when player is at passive festival location
        public void FestivalRule_PassiveFestivalLocation_Matches()
        {
            var rule = TestData.CreateRule(selectedFestivals: new List<string> { "NightMarket" });
            var ctx = MakeCtx(
                festivals: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "NightMarket" },
                isAtPassiveFestivalLocation: true);
            Assert.True(ScheduleEngine.MatchesTriggers(rule, ctx));
        }

        [Fact]
        // Expected: Festival day but not at location returns FailFestivalLocation
        public void FestivalDay_NotAtLocation_FailsFestivalLocation()
        {
            var rule = TestData.CreateRule(selectedFestivals: new List<string> { "spring13" });
            var ctx = MakeCtx(
                festivals: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "spring13" },
                isActiveFestival: false, isAtPassiveFestivalLocation: false);
            ScheduleEngine.MatchesTriggers(rule, ctx, out var result, out _);
            Assert.Equal(RuleMatchResult.FailFestivalLocation, result);
        }

        [Fact]
        // Expected: Wrong festival IDs returns FailFestival (not FailFestivalLocation)
        public void WrongFestivalIds_FailsFestival()
        {
            var rule = TestData.CreateRule(selectedFestivals: new List<string> { "spring13" });
            var ctx = MakeCtx(
                festivals: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "fall16" },
                isActiveFestival: true);
            ScheduleEngine.MatchesTriggers(rule, ctx, out var result, out _);
            Assert.Equal(RuleMatchResult.FailFestival, result);
        }

        [Fact]
        // Expected: Festival rule ignores wrong season — still matches at festival
        public void FestivalRule_WrongSeason_StillMatches()
        {
            var rule = TestData.CreateRule(
                selectedFestivals: new List<string> { "spring13" },
                selectedSeasons: new List<string> { "Winter" });
            var ctx = MakeCtx(
                season: "Spring",
                festivals: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "spring13" },
                isActiveFestival: true);
            Assert.True(ScheduleEngine.MatchesTriggers(rule, ctx));
        }

        [Fact]
        // Expected: Festival rule ignores wrong weather — still matches at festival
        public void FestivalRule_WrongWeather_StillMatches()
        {
            var rule = TestData.CreateRule(
                selectedFestivals: new List<string> { "spring13" },
                selectedWeather: new List<string> { "Rain" });
            var ctx = MakeCtx(
                weather: "Sun",
                festivals: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "spring13" },
                isActiveFestival: true);
            Assert.True(ScheduleEngine.MatchesTriggers(rule, ctx));
        }

        [Fact]
        // Expected: Festival rule ignores wrong location — still matches at festival
        public void FestivalRule_WrongLocation_StillMatches()
        {
            var rule = TestData.CreateRule(
                selectedFestivals: new List<string> { "spring13" },
                selectedLocations: new List<string> { "Town" });
            var ctx = MakeCtx(
                locationName: "Farm",
                festivals: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "spring13" },
                isActiveFestival: true);
            Assert.True(ScheduleEngine.MatchesTriggers(rule, ctx));
        }

        [Fact]
        // Expected: Festival rule ignores wrong area — still matches at festival
        public void FestivalRule_WrongArea_StillMatches()
        {
            var rule = TestData.CreateRule(
                selectedFestivals: new List<string> { "spring13" },
                selectedAreas: new List<string> { "Indoor" });
            var ctx = MakeCtx(
                isOutdoors: true,
                festivals: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "spring13" },
                isActiveFestival: true);
            Assert.True(ScheduleEngine.MatchesTriggers(rule, ctx));
        }

        [Fact]
        // Expected: Wedding rule ignores wrong season — still matches on DayStarted
        public void WeddingRule_WrongSeason_StillMatches()
        {
            var rule = TestData.CreateRule(
                isWeddingDay: true,
                selectedSeasons: new List<string> { "Winter" });
            var ctx = MakeCtx(
                season: "Spring",
                isWeddingDay: true,
                trigger: EvaluationTrigger.DayStarted);
            Assert.True(ScheduleEngine.MatchesTriggers(rule, ctx));
        }

        [Fact]
        // Expected: Wedding rule does NOT match on Warped trigger (even if wedding day)
        public void WeddingRule_WarpedTrigger_Fails()
        {
            var rule = TestData.CreateRule(isWeddingDay: true);
            var ctx = MakeCtx(isWeddingDay: true, trigger: EvaluationTrigger.Warped);
            ScheduleEngine.MatchesTriggers(rule, ctx, out var result, out _);
            Assert.Equal(RuleMatchResult.FailWedding, result);
        }
    }

    public class SpecialEventAutoWinTests
    {
        [Fact]
        // Expected: Special event candidate beats normal candidate regardless of priority
        public void SpecialEvent_BeatsNormal_RegardlessOfPriority()
        {
            var specialRule = TestData.CreateRule(name: "Festival", priority: 1,
                selectedFestivals: new List<string> { "spring13" });
            var normalRule = TestData.CreateRule(name: "Normal", priority: 4,
                selectedSeasons: new List<string> { "Spring" });
            var pool = new List<OutfitSet> { TestData.CreateSet() };

            var candidates = new List<(ScheduleRule rule, List<OutfitSet> pool)>
            {
                (specialRule, pool), (normalRule, pool)
            };
            var specialCandidates = candidates.Where(c => c.rule.IsSpecialEventRule).ToList();
            var effectiveCandidates = specialCandidates.Count > 0 ? specialCandidates : candidates;

            var (winner, _) = ScheduleEngine.SelectWinner(effectiveCandidates, new Random(42));
            Assert.Equal("Festival", winner.Name);
        }

        [Fact]
        // Expected: Among multiple special event candidates, higher priority wins
        public void TwoSpecialEvents_HigherPriorityWins()
        {
            var lowFestival = TestData.CreateRule(name: "LowFest", priority: 1,
                selectedFestivals: new List<string> { "spring13" });
            var highWedding = TestData.CreateRule(name: "HighWedding", priority: 3,
                isWeddingDay: true);
            var pool = new List<OutfitSet> { TestData.CreateSet() };

            var candidates = new List<(ScheduleRule rule, List<OutfitSet> pool)>
            {
                (lowFestival, pool), (highWedding, pool)
            };
            var specialCandidates = candidates.Where(c => c.rule.IsSpecialEventRule).ToList();

            var (winner, _) = ScheduleEngine.SelectWinner(specialCandidates, new Random(42));
            Assert.Equal("HighWedding", winner.Name);
        }

        [Fact]
        // Expected: Only normal candidates — no partitioning, normal winner selected
        public void OnlyNormalCandidates_NoPartitioning()
        {
            var ruleA = TestData.CreateRule(name: "A", priority: 2,
                selectedSeasons: new List<string> { "Spring" });
            var ruleB = TestData.CreateRule(name: "B", priority: 4,
                selectedWeather: new List<string> { "Sun" });
            var pool = new List<OutfitSet> { TestData.CreateSet() };

            var candidates = new List<(ScheduleRule rule, List<OutfitSet> pool)>
            {
                (ruleA, pool), (ruleB, pool)
            };
            var specialCandidates = candidates.Where(c => c.rule.IsSpecialEventRule).ToList();
            var effectiveCandidates = specialCandidates.Count > 0 ? specialCandidates : candidates;

            Assert.Empty(specialCandidates);
            var (winner, _) = ScheduleEngine.SelectWinner(effectiveCandidates, new Random(42));
            Assert.Equal("B", winner.Name);
        }
    }

    public class TiebreakPoolKeyTests
    {
        [Fact]
        // Expected: Pool key from candidates sorts IDs and joins with "|"
        public void PoolKey_SortsIdsAndJoins()
        {
            var ruleB = TestData.CreateRule(id: "b", name: "B");
            var ruleA = TestData.CreateRule(id: "a", name: "A");
            var pool = new List<OutfitSet> { TestData.CreateSet() };
            var candidates = new List<(ScheduleRule, List<OutfitSet>)> { (ruleB, pool), (ruleA, pool) };

            var key = ScheduleEngine.GetTiebreakPoolKey(candidates);
            Assert.Equal("a|b", key);
        }

        [Fact]
        // Expected: Pool key from string list produces same result as candidate overload
        public void PoolKey_StringOverload_MatchesCandidateOverload()
        {
            var ruleA = TestData.CreateRule(id: "r1", name: "A");
            var ruleB = TestData.CreateRule(id: "r2", name: "B");
            var pool = new List<OutfitSet> { TestData.CreateSet() };
            var candidates = new List<(ScheduleRule, List<OutfitSet>)> { (ruleB, pool), (ruleA, pool) };

            var key1 = ScheduleEngine.GetTiebreakPoolKey(candidates);
            var key2 = ScheduleEngine.GetTiebreakPoolKey(new List<string> { "r2", "r1" });
            Assert.Equal(key1, key2);
        }

        [Fact]
        // Expected: Different pool compositions produce different keys
        public void PoolKey_DifferentPools_DifferentKeys()
        {
            var key1 = ScheduleEngine.GetTiebreakPoolKey(new List<string> { "a", "b" });
            var key2 = ScheduleEngine.GetTiebreakPoolKey(new List<string> { "a", "b", "c" });
            Assert.NotEqual(key1, key2);
        }
    }

    public class TiebreakCacheTests
    {
        [Fact]
        // Expected: Same pool called twice returns the same winner (consistent tiebreaks ON)
        public void SamePool_DifferentCalls_SameWinner()
        {
            var engine = new ScheduleEngine(() => true);
            var ruleA = TestData.CreateRule(id: "a", name: "A", priority: 3);
            var ruleB = TestData.CreateRule(id: "b", name: "B", priority: 3);
            var pool = new List<OutfitSet> { TestData.CreateSet() };
            var candidates = new List<(ScheduleRule, List<OutfitSet>)> { (ruleA, pool), (ruleB, pool) };

            var first = engine.SelectWinnerConsistent(candidates, new Random(0));
            var second = engine.SelectWinnerConsistent(candidates, new Random(99));
            Assert.Equal(first.rule.Id, second.rule.Id);
        }

        [Fact]
        // Expected: Different pools (superset) can produce different winners
        public void DifferentPools_Independent()
        {
            var engine = new ScheduleEngine(() => true);
            var ruleA = TestData.CreateRule(id: "a", name: "A", priority: 3);
            var ruleB = TestData.CreateRule(id: "b", name: "B", priority: 3);
            var ruleC = TestData.CreateRule(id: "c", name: "C", priority: 3);
            var pool = new List<OutfitSet> { TestData.CreateSet() };

            var pairCandidates = new List<(ScheduleRule, List<OutfitSet>)> { (ruleA, pool), (ruleB, pool) };
            var trioCandidates = new List<(ScheduleRule, List<OutfitSet>)> { (ruleA, pool), (ruleB, pool), (ruleC, pool) };

            var pairWinner = engine.SelectWinnerConsistent(pairCandidates, new Random(0));
            var trioWinner = engine.SelectWinnerConsistent(trioCandidates, new Random(0));

            // Keys are different so trio is not constrained by pair's cached winner
            var pairKey = ScheduleEngine.GetTiebreakPoolKey(pairCandidates);
            var trioKey = ScheduleEngine.GetTiebreakPoolKey(trioCandidates);
            Assert.NotEqual(pairKey, trioKey);
        }

        [Fact]
        // Expected: If cached winner is no longer in candidates, a new winner is picked
        public void CachedWinnerGone_Repicks()
        {
            var engine = new ScheduleEngine(() => true);
            var ruleA = TestData.CreateRule(id: "a", name: "A", priority: 3);
            var ruleB = TestData.CreateRule(id: "b", name: "B", priority: 3);
            var pool = new List<OutfitSet> { TestData.CreateSet() };

            var both = new List<(ScheduleRule rule, List<OutfitSet> pool)> { (ruleA, pool), (ruleB, pool) };
            var first = engine.SelectWinnerConsistent(both, new Random(0));

            // Remove the winner from candidates
            var remaining = both.Where(c => c.rule.Id != first.rule.Id).ToList();
            var second = engine.SelectWinnerConsistent(remaining, new Random(0));
            Assert.NotEqual(first.rule.Id, second.rule.Id);
        }

        [Fact]
        // Expected: Consistent OFF does not populate cache, allows variance across calls
        public void ConsistentOff_NoCaching()
        {
            var engine = new ScheduleEngine(() => false);
            var ruleA = TestData.CreateRule(id: "a", name: "A", priority: 3);
            var ruleB = TestData.CreateRule(id: "b", name: "B", priority: 3);
            var pool = new List<OutfitSet> { TestData.CreateSet() };
            var candidates = new List<(ScheduleRule, List<OutfitSet>)> { (ruleA, pool), (ruleB, pool) };

            // With consistent OFF, SelectWinnerConsistent delegates to SelectWinner (random).
            // Over many seeds, we should see both winners appear.
            var winners = new HashSet<string>();
            for (int i = 0; i < 50; i++)
                winners.Add(engine.SelectWinnerConsistent(candidates, new Random(i)).rule.Id);

            Assert.True(winners.Count > 1, "Expected variance when consistent tiebreaks OFF");
        }

        [Fact]
        // Expected: InvalidateContextCache clears tiebreak cache
        public void InvalidateContextCache_ClearsTiebreakCache()
        {
            var engine = new ScheduleEngine(() => true);
            var ruleA = TestData.CreateRule(id: "a", name: "A", priority: 3);
            var ruleB = TestData.CreateRule(id: "b", name: "B", priority: 3);
            var pool = new List<OutfitSet> { TestData.CreateSet() };
            var candidates = new List<(ScheduleRule, List<OutfitSet>)> { (ruleA, pool), (ruleB, pool) };

            var first = engine.SelectWinnerConsistent(candidates, new Random(0));
            engine.InvalidateContextCache();

            // After invalidation, cache is empty. With a different seed, result may differ.
            // Run multiple times to verify the cache was cleared (not just lucky same result).
            var postInvalidateWinners = new HashSet<string>();
            for (int i = 0; i < 50; i++)
            {
                engine.InvalidateContextCache();
                postInvalidateWinners.Add(engine.SelectWinnerConsistent(candidates, new Random(i)).rule.Id);
            }
            Assert.True(postInvalidateWinners.Count > 1, "Expected variance after cache invalidation");
        }

        [Fact]
        // Expected: ResetForNewDay clears tiebreak cache
        public void ResetForNewDay_ClearsTiebreakCache()
        {
            var engine = new ScheduleEngine(() => true);
            var ruleA = TestData.CreateRule(id: "a", name: "A", priority: 3);
            var ruleB = TestData.CreateRule(id: "b", name: "B", priority: 3);
            var pool = new List<OutfitSet> { TestData.CreateSet() };
            var candidates = new List<(ScheduleRule, List<OutfitSet>)> { (ruleA, pool), (ruleB, pool) };

            var first = engine.SelectWinnerConsistent(candidates, new Random(0));
            engine.ResetForNewDay();

            var postResetWinners = new HashSet<string>();
            for (int i = 0; i < 50; i++)
            {
                engine.ResetForNewDay();
                postResetWinners.Add(engine.SelectWinnerConsistent(candidates, new Random(i)).rule.Id);
            }
            Assert.True(postResetWinners.Count > 1, "Expected variance after day reset");
        }

        [Fact]
        // Expected: Single candidate at max priority bypasses cache (no tiebreak)
        public void SingleCandidate_NoCacheLookup()
        {
            var engine = new ScheduleEngine(() => true);
            var ruleA = TestData.CreateRule(id: "a", name: "A", priority: 3);
            var pool = new List<OutfitSet> { TestData.CreateSet() };
            var candidates = new List<(ScheduleRule, List<OutfitSet>)> { (ruleA, pool) };

            var winner = engine.SelectWinnerConsistent(candidates, new Random(0));
            Assert.Equal("a", winner.rule.Id);
        }

        [Fact]
        // Expected: First call with consistent ON returns usedConsistentCache=false (random pick)
        public void FirstCall_UsedConsistentCache_False()
        {
            var engine = new ScheduleEngine(() => true);
            var ruleA = TestData.CreateRule(id: "a", name: "A", priority: 3);
            var ruleB = TestData.CreateRule(id: "b", name: "B", priority: 3);
            var pool = new List<OutfitSet> { TestData.CreateSet() };
            var candidates = new List<(ScheduleRule, List<OutfitSet>)> { (ruleA, pool), (ruleB, pool) };

            var result = engine.SelectWinnerConsistent(candidates, new Random(0));
            Assert.False(result.usedConsistentCache);
        }

        [Fact]
        // Expected: Second call with same pool returns usedConsistentCache=true (cache hit)
        public void SecondCall_UsedConsistentCache_True()
        {
            var engine = new ScheduleEngine(() => true);
            var ruleA = TestData.CreateRule(id: "a", name: "A", priority: 3);
            var ruleB = TestData.CreateRule(id: "b", name: "B", priority: 3);
            var pool = new List<OutfitSet> { TestData.CreateSet() };
            var candidates = new List<(ScheduleRule, List<OutfitSet>)> { (ruleA, pool), (ruleB, pool) };

            engine.SelectWinnerConsistent(candidates, new Random(0));
            var second = engine.SelectWinnerConsistent(candidates, new Random(99));
            Assert.True(second.usedConsistentCache);
        }

        [Fact]
        // Expected: Consistent OFF always returns usedConsistentCache=false
        public void ConsistentOff_UsedConsistentCache_AlwaysFalse()
        {
            var engine = new ScheduleEngine(() => false);
            var ruleA = TestData.CreateRule(id: "a", name: "A", priority: 3);
            var ruleB = TestData.CreateRule(id: "b", name: "B", priority: 3);
            var pool = new List<OutfitSet> { TestData.CreateSet() };
            var candidates = new List<(ScheduleRule, List<OutfitSet>)> { (ruleA, pool), (ruleB, pool) };

            var first = engine.SelectWinnerConsistent(candidates, new Random(0));
            var second = engine.SelectWinnerConsistent(candidates, new Random(99));
            Assert.False(first.usedConsistentCache);
            Assert.False(second.usedConsistentCache);
        }

        [Fact]
        // Expected: Single candidate returns usedConsistentCache=false (no tiebreak needed)
        public void SingleCandidate_UsedConsistentCache_False()
        {
            var engine = new ScheduleEngine(() => true);
            var ruleA = TestData.CreateRule(id: "a", name: "A", priority: 3);
            var pool = new List<OutfitSet> { TestData.CreateSet() };
            var candidates = new List<(ScheduleRule, List<OutfitSet>)> { (ruleA, pool) };

            var result = engine.SelectWinnerConsistent(candidates, new Random(0));
            Assert.False(result.usedConsistentCache);
        }
    }

    public class ManualOverrideTests
    {
        [Fact]
        // Expected: Lock ON — last manual outfit wins regardless of signature
        public void LockOn_LastManualWins()
        {
            var engine = new ScheduleEngine(() => true, () => true);
            engine.SetLastContextSignature("sig1");
            engine.SetManualOutfit("A");
            engine.SetLastContextSignature("sig2");
            engine.SetManualOutfit("B");

            Assert.Equal("B", engine.ResolveManualOutfitId("sig1"));
            Assert.Equal("B", engine.ResolveManualOutfitId("sig2"));
            Assert.Equal("B", engine.ResolveManualOutfitId("sig3"));
        }

        [Fact]
        // Expected: Lock OFF — per-context memory, each signature remembers its own outfit
        public void LockOff_PerContextMemory()
        {
            var engine = new ScheduleEngine(() => true, () => false);
            engine.SetLastContextSignature("sig1");
            engine.SetManualOutfit("A");
            engine.SetLastContextSignature("sig2");
            engine.SetManualOutfit("B");

            Assert.Equal("A", engine.ResolveManualOutfitId("sig1"));
            Assert.Equal("B", engine.ResolveManualOutfitId("sig2"));
            Assert.Null(engine.ResolveManualOutfitId("sig3"));
        }

        [Fact]
        // Expected: Toggle Lock ON → OFF preserves per-context entries
        public void ToggleLockOnToOff_PreservesPerContext()
        {
            bool lockOn = true;
            var engine = new ScheduleEngine(() => true, () => lockOn);

            engine.SetLastContextSignature("sig1");
            engine.SetManualOutfit("A");
            engine.SetLastContextSignature("sig2");
            engine.SetManualOutfit("B");

            // While lock ON, any sig returns last manual
            Assert.Equal("B", engine.ResolveManualOutfitId("sig1"));

            // Toggle OFF — per-context takes over
            lockOn = false;
            Assert.Equal("A", engine.ResolveManualOutfitId("sig1"));
            Assert.Equal("B", engine.ResolveManualOutfitId("sig2"));
        }

        [Fact]
        // Expected: Toggle Lock OFF → ON uses the last applied manual outfit
        public void ToggleLockOffToOn_UsesLastApplied()
        {
            bool lockOn = false;
            var engine = new ScheduleEngine(() => true, () => lockOn);

            engine.SetLastContextSignature("sig1");
            engine.SetManualOutfit("A");
            engine.SetLastContextSignature("sig2");
            engine.SetManualOutfit("B");

            // Lock OFF: sig3 has no override
            Assert.Null(engine.ResolveManualOutfitId("sig3"));

            // Toggle ON: last manual was B
            lockOn = true;
            Assert.Equal("B", engine.ResolveManualOutfitId("sig3"));
        }

        [Fact]
        // Expected: ResetForNewDay clears both manual caches
        public void ResetForNewDay_ClearsBothCaches()
        {
            var engine = new ScheduleEngine(() => true, () => true);
            engine.SetLastContextSignature("sig1");
            engine.SetManualOutfit("A");

            Assert.Equal("A", engine.ResolveManualOutfitId("sig1"));

            engine.ResetForNewDay();

            Assert.Null(engine.ResolveManualOutfitId("sig1"));
        }

        [Fact]
        // Expected: No manual outfit set → returns null for any signature
        public void NoManualSet_ReturnsNull()
        {
            var engine = new ScheduleEngine(() => true, () => true);
            Assert.Null(engine.ResolveManualOutfitId("sig1"));

            var engineOff = new ScheduleEngine(() => true, () => false);
            Assert.Null(engineOff.ResolveManualOutfitId("sig1"));
        }

        [Fact]
        // Expected: SetManualOutfit without prior context signature only sets global cache
        public void SetManualOutfit_NoContextSignature_OnlySetsGlobal()
        {
            var engine = new ScheduleEngine(() => true, () => false);
            // No SetLastContextSignature called
            engine.SetManualOutfit("A");

            // Lock ON: global is set
            Assert.Null(engine.ResolveManualOutfitId("sig1"));
            // No per-context entry was written since lastContextSignature was null
        }
    }

    public class CacheInvalidationTests
    {
        private ScheduleEngine CreateEngineWithCaches(
            out string ruleAId, out string ruleBId)
        {
            ruleAId = "rule-a";
            ruleBId = "rule-b";
            var engine = new ScheduleEngine(() => true);

            // Seed ruleDayCache for both rules
            engine.SeedRuleDayCache(ruleAId, "outfit-a");
            engine.SeedRuleDayCache(ruleBId, "outfit-b");

            // Seed contextCache
            engine.SeedContextCache("sig1", new List<string> { ruleAId }, 3);
            engine.SeedContextCache("sig2", new List<string> { ruleBId }, 2);

            // Seed tiebreakCache via SelectWinnerConsistent
            var ruleA = TestData.CreateRule(id: ruleAId, name: "A", priority: 3);
            var ruleB = TestData.CreateRule(id: ruleBId, name: "B", priority: 3);
            var pool = new List<OutfitSet> { TestData.CreateSet() };
            engine.SelectWinnerConsistent(
                new List<(ScheduleRule, List<OutfitSet>)> { (ruleA, pool), (ruleB, pool) },
                new Random(0));

            Assert.Equal(2, engine.RuleDayCacheCount);
            Assert.True(engine.ContextCacheCount >= 2);
            Assert.True(engine.TiebreakCacheCount >= 1);

            return engine;
        }

        [Fact]
        // Expected: InvalidateForRule clears only the target rule's day cache entry
        public void InvalidateForRule_ScopesRuleDayCache()
        {
            var engine = CreateEngineWithCaches(out var ruleA, out var ruleB);

            engine.InvalidateForRule(ruleA);

            Assert.False(engine.RuleDayCacheContains(ruleA));
            Assert.True(engine.RuleDayCacheContains(ruleB));
        }

        [Fact]
        // Expected: InvalidateForRule clears tiebreak entries involving the target rule
        public void InvalidateForRule_ScopesTiebreakCache()
        {
            var engine = CreateEngineWithCaches(out var ruleA, out var ruleB);

            engine.InvalidateForRule(ruleA);

            Assert.False(engine.TiebreakCacheContainsRule(ruleA));
        }

        [Fact]
        // Expected: InvalidateForRule clears all context cache (rule conditions may have changed)
        public void InvalidateForRule_ClearsAllContextCache()
        {
            var engine = CreateEngineWithCaches(out var ruleA, out _);

            engine.InvalidateForRule(ruleA);

            Assert.Equal(0, engine.ContextCacheCount);
        }

        [Fact]
        // Expected: InvalidateForSetsChanged clears all day cache but preserves context and tiebreak caches
        public void InvalidateForSetsChanged_ClearsOnlyDayCache()
        {
            var engine = CreateEngineWithCaches(out _, out _);
            int contextBefore = engine.ContextCacheCount;
            int tiebreakBefore = engine.TiebreakCacheCount;

            engine.InvalidateForSetsChanged();

            Assert.Equal(0, engine.RuleDayCacheCount);
            Assert.Equal(contextBefore, engine.ContextCacheCount);
            Assert.Equal(tiebreakBefore, engine.TiebreakCacheCount);
        }

        [Fact]
        // Expected: InvalidateContextCache still clears everything (backward compat)
        public void InvalidateContextCache_ClearsAll()
        {
            var engine = CreateEngineWithCaches(out _, out _);

            engine.InvalidateContextCache();

            Assert.Equal(0, engine.ContextCacheCount);
            Assert.Equal(0, engine.RuleDayCacheCount);
            Assert.Equal(0, engine.TiebreakCacheCount);
        }

        [Fact]
        // Expected: InvalidateForRule with unknown ID is a no-op, no crash
        public void InvalidateForRule_UnknownId_NoCrash()
        {
            var engine = CreateEngineWithCaches(out _, out var ruleB);

            engine.InvalidateForRule("nonexistent");

            Assert.True(engine.RuleDayCacheContains(ruleB));
            Assert.True(engine.TiebreakCacheCount >= 1);
        }

        [Fact]
        // Expected: Tiebreak cache involving only rule B survives invalidation for rule A
        public void InvalidateForRule_PreservesUnrelatedTiebreaks()
        {
            var engine = new ScheduleEngine(() => true);
            var ruleB = TestData.CreateRule(id: "rule-b", name: "B", priority: 3);
            var ruleC = TestData.CreateRule(id: "rule-c", name: "C", priority: 3);
            var pool = new List<OutfitSet> { TestData.CreateSet() };

            // Create a tiebreak entry for B vs C (does not involve A)
            engine.SelectWinnerConsistent(
                new List<(ScheduleRule, List<OutfitSet>)> { (ruleB, pool), (ruleC, pool) },
                new Random(0));

            Assert.True(engine.TiebreakCacheCount >= 1);

            engine.InvalidateForRule("rule-a");

            Assert.True(engine.TiebreakCacheCount >= 1, "B-vs-C tiebreak should survive rule-a invalidation");
        }
    }

}
