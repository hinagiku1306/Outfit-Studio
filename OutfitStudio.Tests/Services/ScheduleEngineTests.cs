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
        // Expected: FestivalsSelectAll matches regardless of today's festivals
        public void FestivalsSelectAll_AlwaysMatches()
        {
            var rule = TestData.CreateRule(festivalsSelectAll: true);
            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext()));
        }

        [Fact]
        // Expected: Selected festival matching today returns true (seasons unconfigured = wildcard)
        public void SelectedFestival_MatchesToday()
        {
            var todaysFestivals = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "spring13" };
            var rule = TestData.CreateRule(selectedFestivals: new List<string> { "spring13" });
            Assert.True(ScheduleEngine.MatchesTriggers(rule, MakeContext(season: "Winter", festivals: todaysFestivals)));
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
        // Expected: All 5 groups configured and matching returns true
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
                isOutdoors: true)));
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
        // Expected: Full rotation cycle visits all sets exactly once before reshuffling
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
            Assert.Empty(state.Queue);
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
        // Expected: Wedding + Season combined — both must match
        public void Wedding_PlusSeason_BothMustMatch()
        {
            var rule = TestData.CreateRule(isWeddingDay: true, selectedSeasons: new List<string> { "Summer" });
            var ctx = MakeCtx(isWeddingDay: true, trigger: EvaluationTrigger.DayStarted, season: "Spring");
            Assert.False(ScheduleEngine.MatchesTriggers(rule, ctx));
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

    public class ShouldBypassCacheTests
    {
        [Fact]
        // Expected: Null winning rule never bypasses cache
        public void NullRule_ReturnsFalse()
        {
            Assert.False(ScheduleEngine.ShouldBypassCache(null, EvaluationTrigger.Warped));
        }

        [Fact]
        // Expected: AdvanceOnWarp rule with Warped trigger bypasses cache
        public void AdvanceOnWarp_Warped_ReturnsTrue()
        {
            var rule = TestData.CreateRule(advanceOnWarp: true);
            Assert.True(ScheduleEngine.ShouldBypassCache(rule, EvaluationTrigger.Warped));
        }

        [Fact]
        // Expected: AdvanceOnWarp rule with DayStarted trigger does NOT bypass cache
        public void AdvanceOnWarp_DayStarted_ReturnsFalse()
        {
            var rule = TestData.CreateRule(advanceOnWarp: true);
            Assert.False(ScheduleEngine.ShouldBypassCache(rule, EvaluationTrigger.DayStarted));
        }

        [Fact]
        // Expected: Non-AdvanceOnWarp rule with Warped trigger does NOT bypass cache
        public void NoAdvanceOnWarp_Warped_ReturnsFalse()
        {
            var rule = TestData.CreateRule(advanceOnWarp: false);
            Assert.False(ScheduleEngine.ShouldBypassCache(rule, EvaluationTrigger.Warped));
        }

        [Fact]
        // Expected: Non-AdvanceOnWarp rule with DayStarted trigger does NOT bypass cache
        public void NoAdvanceOnWarp_DayStarted_ReturnsFalse()
        {
            var rule = TestData.CreateRule(advanceOnWarp: false);
            Assert.False(ScheduleEngine.ShouldBypassCache(rule, EvaluationTrigger.DayStarted));
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
}
