using System;
using System.Collections.Generic;

namespace OutfitStudio.Models
{
    public enum EvalCacheOutcome { Cached, NoMatch }
    public enum RuleMatchResult { Matched, FailSeason, FailFestival, FailFestivalLocation, FailWeather, FailLocation, FailArea, FailWedding, EmptyPool }

    public class RuleEvalEntry
    {
        public string RuleName { get; set; } = "";
        public int Priority { get; set; }
        public RuleMatchResult MatchResult { get; set; }
        public string FailDetail { get; set; } = "";
        public bool IsSpecialEvent { get; set; }
        public int PoolSize { get; set; } = -1;
    }

    public class ScheduleEvalEntry
    {
        public DateTime Timestamp { get; set; }
        public int GameDay { get; set; }
        public string GameSeason { get; set; } = "";

        // Trigger
        public string Trigger { get; set; } = "";

        // Context snapshot
        public string Season { get; set; } = "";
        public string Weather { get; set; } = "";
        public string LocationName { get; set; } = "";
        public string LocationDisplayName { get; set; } = "";
        public bool IsOutdoors { get; set; }
        public bool IsActiveFestival { get; set; }
        public bool IsAtPassiveFestivalLocation { get; set; }
        public bool IsWeddingDay { get; set; }
        public List<string> TodaysFestivalIds { get; set; } = new();

        // Cache
        public EvalCacheOutcome? CacheOutcome { get; set; }
        public string? ManualOverrideOutfitId { get; set; }
        public string? ManualOverrideOutfitName { get; set; }

        // Rules (only populated on fresh evaluation; cached entries show only winning rule)
        public int TotalRules { get; set; }
        public int EnabledRules { get; set; }
        public List<RuleEvalEntry> RuleResults { get; set; } = new();

        // Winner
        public string? WinningRuleName { get; set; }
        public int? WinningPriority { get; set; }
        public int CandidateCount { get; set; }
        public bool WasTiebreak { get; set; }
        public bool IsConsistentTiebreak { get; set; }
        public List<string>? TiedRuleNames { get; set; }
        public bool WinnerAdvanceOnWarp { get; set; }

        // Outfit
        public string? ChosenOutfitName { get; set; }
        public bool WasAlreadyApplied { get; set; }
        public bool UsedDayCache { get; set; }
        public bool IsNewRotationOutfit { get; set; }

        // Rotation
        public int QueueSizeAfter { get; set; }
        public int WinnerPoolSize { get; set; }
        public bool WasReshuffled { get; set; }
        public string? LastUsedOutfitName { get; set; }
    }
}
