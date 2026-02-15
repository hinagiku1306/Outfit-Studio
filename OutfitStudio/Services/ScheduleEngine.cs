using System;
using System.Collections.Generic;
using System.Linq;
using OutfitStudio.Models;
using StardewValley;
using StardewValley.GameData;
using StardewValley.TokenizableStrings;

namespace OutfitStudio.Services
{
    public enum EvaluationTrigger { DayStarted, Warped }

    internal record EvaluationContext(
        string CurrentSeason,
        HashSet<string> TodaysFestivalIds,
        string CurrentWeather,
        string CurrentLocationName,
        bool IsOutdoors,
        bool IsActiveFestival,
        bool IsAtPassiveFestivalLocation,
        bool IsWeddingDay,
        EvaluationTrigger Trigger)
    {
        internal string ToSignature()
        {
            bool weddingFlag = IsWeddingDay && Trigger == EvaluationTrigger.DayStarted;
            return $"{CurrentSeason}|{CurrentWeather}|{CurrentLocationName}|{IsOutdoors}|{IsActiveFestival}|{IsAtPassiveFestivalLocation}|{weddingFlag}";
        }
    }

    public class ScheduleEngine
    {
        private record CachedContext(
            List<string> WinningRuleIds,
            int WinningPriority,
            bool IsNoMatch);

        private readonly ScheduleStore scheduleStore;
        private readonly OutfitSetStore outfitSetStore;
        private readonly ScheduleEvalLog? evalLog;
        private readonly Func<bool> getConsistentTiebreaks;
        private readonly Func<bool> getLockManualOutfit;
        private readonly Random random = new();

        private Dictionary<string, string>? cachedActiveFestivals;
        private Dictionary<string, PassiveFestivalData>? cachedPassiveFestivals;
        private string? lastAppliedOutfitId;
        private readonly Dictionary<string, CachedContext> contextCache = new();
        private readonly Dictionary<string, string> ruleDayCache = new();
        private readonly Dictionary<string, string> tiebreakCache = new();

        internal int ContextCacheCount => contextCache.Count;
        internal int RuleDayCacheCount => ruleDayCache.Count;
        internal int TiebreakCacheCount => tiebreakCache.Count;
        internal bool RuleDayCacheContains(string ruleId) => ruleDayCache.ContainsKey(ruleId);
        internal bool TiebreakCacheContainsRule(string ruleId) =>
            tiebreakCache.Keys.Any(k => k.Contains(ruleId));

        internal void SeedRuleDayCache(string ruleId, string outfitId) => ruleDayCache[ruleId] = outfitId;
        internal void SeedContextCache(string signature, List<string> ruleIds, int priority) =>
            contextCache[signature] = new CachedContext(ruleIds, priority, IsNoMatch: false);
        private string? lastContextSignature;
        private string? lastManualOutfitId;
        private readonly Dictionary<string, string> manualContextOutfits = new();

        public ScheduleEngine(ScheduleStore scheduleStore, OutfitSetStore outfitSetStore)
            : this(scheduleStore, outfitSetStore, null)
        {
        }

        internal ScheduleEngine(ScheduleStore scheduleStore, OutfitSetStore outfitSetStore,
            ScheduleEvalLog? evalLog, Func<bool>? getConsistentTiebreaks = null,
            Func<bool>? getLockManualOutfit = null)
        {
            this.scheduleStore = scheduleStore;
            this.outfitSetStore = outfitSetStore;
            this.evalLog = evalLog;
            this.getConsistentTiebreaks = getConsistentTiebreaks ?? (() => true);
            this.getLockManualOutfit = getLockManualOutfit ?? (() => true);

            ScheduleDebug.TraceLog = msg => DebugLogger.Trace(msg);
            ScheduleDebug.DebugLog = msg => DebugLogger.Trace(msg);
        }

        internal ScheduleEngine(Func<bool> getConsistentTiebreaks, Func<bool>? getLockManualOutfit = null)
        {
            this.scheduleStore = null!;
            this.outfitSetStore = null!;
            this.evalLog = null;
            this.getConsistentTiebreaks = getConsistentTiebreaks;
            this.getLockManualOutfit = getLockManualOutfit ?? (() => true);
        }

        public void InvalidateFestivalCache()
        {
            cachedActiveFestivals = null;
            cachedPassiveFestivals = null;
        }

        public void InvalidateContextCache()
        {
            contextCache.Clear();
            ruleDayCache.Clear();
            tiebreakCache.Clear();
        }

        public void InvalidateForRule(string ruleId)
        {
            contextCache.Clear();
            ruleDayCache.Remove(ruleId);
            var staleKeys = tiebreakCache.Keys.Where(k => k.Contains(ruleId)).ToList();
            foreach (var key in staleKeys)
                tiebreakCache.Remove(key);
        }

        public void InvalidateForSetsChanged()
        {
            ruleDayCache.Clear();
        }

        public void ResetForNewDay()
        {
            lastAppliedOutfitId = null;
            lastManualOutfitId = null;
            manualContextOutfits.Clear();
            contextCache.Clear();
            ruleDayCache.Clear();
            tiebreakCache.Clear();
            evalLog?.Clear();
        }

        public void SetManualOutfit(string outfitId)
        {
            lastManualOutfitId = outfitId;
            if (lastContextSignature != null)
                manualContextOutfits[lastContextSignature] = outfitId;
            lastAppliedOutfitId = outfitId;
        }

        internal void SetLastContextSignature(string signature)
        {
            lastContextSignature = signature;
        }

        internal bool TryApplyManualOverride(string signature, ScheduleEvalEntry? logEntry)
        {
            var targetOutfitId = ResolveManualOutfitId(signature);
            if (targetOutfitId == null)
                return false;

            var manualSet = outfitSetStore.GetById(targetOutfitId);
            if (manualSet != null && manualSet.Id != lastAppliedOutfitId)
            {
                outfitSetStore.ApplySet(manualSet);
                lastAppliedOutfitId = manualSet.Id;
            }

            if (logEntry != null)
                logEntry.CacheOutcome = EvalCacheOutcome.NoMatch;

            return true;
        }

        internal string? ResolveManualOutfitId(string signature)
        {
            if (getLockManualOutfit())
                return lastManualOutfitId;

            manualContextOutfits.TryGetValue(signature, out var outfitId);
            return outfitId;
        }

        internal static string GetCurrentWeather()
        {
            if (Game1.currentLocation == null)
                return "Sun";

            string weather = Game1.currentLocation.GetWeather().Weather;

            if (string.IsNullOrEmpty(weather) || weather == "Festival" || weather == "Wedding")
                return "Sun";

            return weather;
        }

        public void Evaluate(EvaluationTrigger trigger)
        {
            if (!scheduleStore.IsEnabled)
                return;

            EnsureFestivalDataLoaded();
            var ctx = new EvaluationContext(
                CurrentSeason: Game1.season.ToString(),
                TodaysFestivalIds: GetTodaysFestivalIds(),
                CurrentWeather: GetCurrentWeather(),
                CurrentLocationName: Game1.currentLocation?.Name ?? "",
                IsOutdoors: Game1.currentLocation?.IsOutdoors ?? false,
                IsActiveFestival: Game1.isFestival(),
                IsAtPassiveFestivalLocation: IsPlayerAtPassiveFestivalLocation(),
                IsWeddingDay: Game1.weddingToday,
                Trigger: trigger);
            string signature = ctx.ToSignature();
            lastContextSignature = signature;

            ScheduleEvalEntry? logEntry = null;
            if (evalLog != null)
            {
                logEntry = evalLog.CreateEntry();
                logEntry.Trigger = trigger.ToString();
                logEntry.GameDay = Game1.dayOfMonth;
                logEntry.GameSeason = ctx.CurrentSeason;
                logEntry.Season = ctx.CurrentSeason;
                logEntry.Weather = ctx.CurrentWeather;
                logEntry.LocationName = ctx.CurrentLocationName;
                logEntry.LocationDisplayName = Game1.currentLocation?.DisplayName ?? ctx.CurrentLocationName;
                logEntry.IsOutdoors = ctx.IsOutdoors;
                logEntry.IsActiveFestival = ctx.IsActiveFestival;
                logEntry.IsAtPassiveFestivalLocation = ctx.IsAtPassiveFestivalLocation;
                logEntry.IsWeddingDay = ctx.IsWeddingDay;
                var passiveNames = cachedPassiveFestivals?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => TokenParser.ParseText(kvp.Value.DisplayName));
                logEntry.TodaysFestivalIds = ctx.TodaysFestivalIds
                    .Select(id => ResolveFestivalDisplayName(id, cachedActiveFestivals, passiveNames))
                    .ToList();
                logEntry.ManualOverrideOutfitId = lastManualOutfitId;
                if (lastManualOutfitId != null)
                    logEntry.ManualOverrideOutfitName = outfitSetStore.GetById(lastManualOutfitId)?.Name;

                var allRules = scheduleStore.GetRules();
                logEntry.TotalRules = allRules.Count;
                logEntry.EnabledRules = allRules.Count(r => r.IsEnabled);
            }

            DebugLogger.Trace("");
            DebugLogger.Trace($"[Schedule] ══ {trigger} → {ctx.CurrentLocationName} ══");
            DebugLogger.Trace($"[Schedule]   {ctx.CurrentSeason} | {ctx.CurrentWeather} | {(ctx.IsOutdoors ? "Outdoors" : "Indoors")}{(ctx.IsActiveFestival ? " | Festival" : "")}{(ctx.IsAtPassiveFestivalLocation ? " | PassiveFestival" : "")}{(ctx.IsWeddingDay ? " | Wedding" : "")}");

            if (TryApplyManualOverride(signature, logEntry))
                return;

            if (contextCache.TryGetValue(signature, out var cached))
            {
                if (cached.IsNoMatch)
                {
                    DebugLogger.Trace("[Schedule]   Cache: no prior match → manual fallback");
                    if (logEntry != null)
                        logEntry.CacheOutcome = EvalCacheOutcome.NoMatch;
                    return;
                }

                ScheduleRule? cachedRule = null;
                bool cachedTiebreakWasConsistent = false;
                if (cached.WinningRuleIds.Count == 1)
                {
                    cachedRule = scheduleStore.GetRuleById(cached.WinningRuleIds[0]);
                }
                else if (cached.WinningRuleIds.Count > 1)
                {
                    bool hitTiebreakCache = false;
                    if (getConsistentTiebreaks())
                    {
                        var poolKey = GetTiebreakPoolKey(cached.WinningRuleIds);
                        if (tiebreakCache.TryGetValue(poolKey, out var winnerId))
                        {
                            cachedRule = scheduleStore.GetRuleById(winnerId);
                            hitTiebreakCache = cachedRule != null;
                        }
                        cachedRule ??= scheduleStore.GetRuleById(cached.WinningRuleIds[random.Next(cached.WinningRuleIds.Count)]);
                    }
                    else
                    {
                        cachedRule = scheduleStore.GetRuleById(cached.WinningRuleIds[random.Next(cached.WinningRuleIds.Count)]);
                    }
                    cachedTiebreakWasConsistent = hitTiebreakCache;
                }

                if (cachedRule == null || !cachedRule.IsEnabled)
                {
                    DebugLogger.Trace("[Schedule]   Cache evicted (rule gone/disabled) → re-evaluating");
                    contextCache.Remove(signature);
                }
                else
                {
                    if (logEntry != null)
                    {
                        logEntry.CacheOutcome = EvalCacheOutcome.Cached;
                        logEntry.WinningRuleName = cachedRule.Name;
                        logEntry.WinningPriority = cached.WinningPriority;
                        logEntry.WinnerAdvanceOnWarp = cachedRule.AdvanceOnWarp;
                        logEntry.CandidateCount = cached.WinningRuleIds.Count;
                        logEntry.WasTiebreak = cached.WinningRuleIds.Count > 1;
                        logEntry.IsConsistentTiebreak = cachedTiebreakWasConsistent;
                        if (logEntry.WasTiebreak)
                        {
                            logEntry.TiedRuleNames = new List<string>();
                            foreach (var id in cached.WinningRuleIds)
                            {
                                var r = scheduleStore.GetRuleById(id);
                                if (r != null) logEntry.TiedRuleNames.Add(r.Name);
                            }
                        }
                        logEntry.WinnerPoolSize = -1;
                    }

                    OutfitSet? cachedSet = null;
                    RotationState? rotState = null;

                    if (cachedRule.AdvanceOnWarp && trigger == EvaluationTrigger.Warped)
                    {
                        var sets = outfitSetStore.GetAllSets();
                        var pool = ResolvePool(cachedRule, sets);
                        if (pool.Count == 0)
                        {
                            DebugLogger.Trace("[Schedule]   Cache hit → AdvanceOnWarp pool empty → manual fallback");
                            contextCache.Remove(signature);
                            if (logEntry != null)
                                logEntry.CacheOutcome = EvalCacheOutcome.NoMatch;
                            return;
                        }

                        rotState = scheduleStore.GetRotationState(cachedRule.Id) ?? new RotationState { RuleId = cachedRule.Id };
                        if (logEntry != null)
                        {
                            logEntry.LastUsedOutfitName = rotState.LastUsedId != null ? (outfitSetStore.GetById(rotState.LastUsedId)?.Name) : null;
                            logEntry.WinnerPoolSize = pool.Count;
                        }

                        cachedSet = PickOutfit(pool, rotState, random, out bool wasReshuffled);
                        if (cachedSet == null) return;

                        if (logEntry != null)
                        {
                            logEntry.IsNewRotationOutfit = true;
                            logEntry.QueueSizeAfter = rotState.Queue.Count;
                            logEntry.WasReshuffled = wasReshuffled;
                        }

                        DebugLogger.Trace($"[Schedule]   Cache hit → AdvanceOnWarp → new outfit '{cachedSet.Name}'");
                    }
                    else if (!cachedRule.AdvanceOnWarp && ruleDayCache.TryGetValue(cachedRule.Id, out var cachedDayId))
                    {
                        cachedSet = outfitSetStore.GetById(cachedDayId);
                        if (cachedSet == null)
                        {
                            ruleDayCache.Remove(cachedRule.Id);
                            DebugLogger.Trace("[Schedule]   Cache hit → day cache stale (outfit deleted)");
                        }
                        else
                        {
                            if (logEntry != null)
                                logEntry.UsedDayCache = true;
                            DebugLogger.Trace("[Schedule]   Cache hit → day cache reuse");
                        }
                    }

                    if (cachedSet == null && !cachedRule.AdvanceOnWarp && lastAppliedOutfitId != null)
                    {
                        var sets = outfitSetStore.GetAllSets();
                        var pool = ResolvePool(cachedRule, sets);
                        var reuse = pool.FirstOrDefault(s => s.Id == lastAppliedOutfitId);
                        if (reuse != null)
                        {
                            cachedSet = reuse;
                            ruleDayCache[cachedRule.Id] = reuse.Id;
                            if (logEntry != null)
                            {
                                logEntry.UsedDayCache = true;
                                logEntry.WinnerPoolSize = pool.Count;
                            }
                            DebugLogger.Trace("[Schedule]   Cache hit → day cache seeded from current outfit");
                        }
                    }

                    if (cachedSet == null && !(cachedRule.AdvanceOnWarp && trigger == EvaluationTrigger.Warped))
                    {
                        var sets = outfitSetStore.GetAllSets();
                        var pool = ResolvePool(cachedRule, sets);
                        if (pool.Count == 0)
                        {
                            DebugLogger.Trace("[Schedule]   Cache hit → pool empty → manual fallback");
                            contextCache.Remove(signature);
                            if (logEntry != null)
                                logEntry.CacheOutcome = EvalCacheOutcome.NoMatch;
                            return;
                        }

                        rotState = scheduleStore.GetRotationState(cachedRule.Id) ?? new RotationState { RuleId = cachedRule.Id };
                        if (logEntry != null)
                        {
                            logEntry.LastUsedOutfitName = rotState.LastUsedId != null ? (outfitSetStore.GetById(rotState.LastUsedId)?.Name) : null;
                            logEntry.WinnerPoolSize = pool.Count;
                        }

                        cachedSet = PickOutfit(pool, rotState, random, out bool wasReshuffled);
                        if (cachedSet == null) return;

                        if (logEntry != null)
                        {
                            logEntry.QueueSizeAfter = rotState.Queue.Count;
                            logEntry.WasReshuffled = wasReshuffled;
                        }

                        if (!cachedRule.AdvanceOnWarp)
                            ruleDayCache[cachedRule.Id] = cachedSet.Id;

                        DebugLogger.Trace($"[Schedule]   Cache hit → first time rule → '{cachedSet.Name}'");
                    }

                    if (logEntry != null)
                    {
                        foreach (var id in cached.WinningRuleIds)
                        {
                            var r = scheduleStore.GetRuleById(id);
                            if (r == null) continue;
                            logEntry.RuleResults.Add(new RuleEvalEntry
                            {
                                RuleName = r.Name,
                                Priority = r.Priority,
                                MatchResult = RuleMatchResult.Matched,
                                PoolSize = r.Id == cachedRule.Id ? logEntry.WinnerPoolSize : -1
                            });
                        }
                    }

                    bool changed = false;
                    if (rotState != null)
                    {
                        scheduleStore.SetRotationState(cachedRule.Id, rotState);
                        changed = true;
                    }

                    if (cachedSet != null && cachedSet.Id != lastAppliedOutfitId)
                    {
                        outfitSetStore.ApplySet(cachedSet);
                        lastAppliedOutfitId = cachedSet.Id;
                        changed = true;

                        if (logEntry != null)
                        {
                            logEntry.ChosenOutfitName = cachedSet.Name;
                            logEntry.WasAlreadyApplied = false;
                        }
                    }
                    else if (cachedSet != null)
                    {
                        if (logEntry != null)
                        {
                            logEntry.ChosenOutfitName = cachedSet.Name;
                            logEntry.WasAlreadyApplied = true;
                        }
                        DebugLogger.Trace("[Schedule]   Cache hit → already applied");
                    }

                    if (changed)
                        scheduleStore.SaveLocalData();

                    return;
                }
            }

            var rules = scheduleStore.GetRules();
            var enabledRules = rules.Where(r => r.IsEnabled).ToList();
            DebugLogger.Trace($"[Schedule]   Rules: {enabledRules.Count}/{rules.Count} enabled");

            if (enabledRules.Count == 0)
            {
                DebugLogger.Trace("[Schedule]   No enabled rules → manual fallback");
                contextCache[signature] = new CachedContext(new List<string>(), 0, IsNoMatch: true);
                if (logEntry != null)
                    logEntry.CacheOutcome = EvalCacheOutcome.NoMatch;
                return;
            }

            var allSets = outfitSetStore.GetAllSets();
            var candidates = new List<(ScheduleRule rule, List<OutfitSet> pool)>();

            foreach (var rule in enabledRules)
            {
                bool matched = MatchesTriggers(rule, ctx, out var matchResult, out var failDetail);

                RuleEvalEntry? ruleEntry = null;
                if (logEntry != null)
                    ruleEntry = new RuleEvalEntry
                    {
                        RuleName = rule.Name,
                        Priority = rule.Priority,
                        MatchResult = matchResult,
                        FailDetail = failDetail,
                        IsSpecialEvent = rule.IsSpecialEventRule
                    };

                if (!matched)
                {
                    ScheduleDebug.Trace($"[Schedule]     '{rule.Name}' → no match");
                    if (ruleEntry != null) logEntry!.RuleResults.Add(ruleEntry);
                    continue;
                }

                var pool = ResolvePool(rule, allSets);
                if (ruleEntry != null) ruleEntry.PoolSize = pool.Count;

                if (pool.Count == 0)
                {
                    if (ruleEntry != null) ruleEntry.MatchResult = RuleMatchResult.EmptyPool;
                    ScheduleDebug.Trace($"[Schedule]     '{rule.Name}' → matched, 0 sets");
                    if (ruleEntry != null) logEntry!.RuleResults.Add(ruleEntry);
                    continue;
                }

                DebugLogger.Trace($"[Schedule]     '{rule.Name}' → {pool.Count} sets ({GetPriorityLabel(rule.Priority)})");
                candidates.Add((rule, pool));
                if (ruleEntry != null) logEntry!.RuleResults.Add(ruleEntry);
            }

            if (candidates.Count == 0)
            {
                DebugLogger.Trace("[Schedule]   No rules matched → manual fallback");
                contextCache[signature] = new CachedContext(new List<string>(), 0, IsNoMatch: true);
                if (logEntry != null)
                    logEntry.CacheOutcome = EvalCacheOutcome.NoMatch;
                return;
            }

            var specialCandidates = candidates.Where(c => c.rule.IsSpecialEventRule).ToList();
            var effectiveCandidates = specialCandidates.Count > 0 ? specialCandidates : candidates;
            bool specialEventAutoWin = specialCandidates.Count > 0 && candidates.Count > specialCandidates.Count;

            var (winnerRule, winnerPool, usedConsistentCache) = SelectWinnerConsistent(effectiveCandidates, random);
            DebugLogger.Trace($"[Schedule]   WINNER: '{winnerRule.Name}' {GetPriorityLabel(winnerRule.Priority)}{(effectiveCandidates.Count > 1 ? $" (of {effectiveCandidates.Count})" : "")}{(specialEventAutoWin ? " [special event auto-win]" : "")}");

            if (logEntry != null)
            {
                logEntry.WinningRuleName = winnerRule.Name;
                logEntry.WinningPriority = winnerRule.Priority;
                logEntry.CandidateCount = effectiveCandidates.Count;
                logEntry.WinnerPoolSize = winnerPool.Count;
                int topPri = effectiveCandidates.Max(c => c.rule.Priority);
                logEntry.WasTiebreak = effectiveCandidates.Count(c => c.rule.Priority == topPri) > 1;
                logEntry.IsConsistentTiebreak = logEntry.WasTiebreak && usedConsistentCache;
                if (logEntry.WasTiebreak)
                    logEntry.TiedRuleNames = effectiveCandidates.Where(c => c.rule.Priority == topPri).Select(c => c.rule.Name).ToList();
                logEntry.SpecialEventAutoWin = specialEventAutoWin;
                logEntry.WinnerAdvanceOnWarp = winnerRule.AdvanceOnWarp;
            }

            OutfitSet? chosenSet = null;
            RotationState? state = null;

            if (!winnerRule.AdvanceOnWarp && ruleDayCache.TryGetValue(winnerRule.Id, out var dayOutfitId))
            {
                DebugLogger.Trace("[Schedule]   Day cache → reusing previous outfit");
                chosenSet = outfitSetStore.GetById(dayOutfitId);
                if (logEntry != null)
                    logEntry.UsedDayCache = true;
            }

            if (chosenSet == null && !winnerRule.AdvanceOnWarp && lastAppliedOutfitId != null)
            {
                var reuse = winnerPool.FirstOrDefault(s => s.Id == lastAppliedOutfitId);
                if (reuse != null)
                {
                    chosenSet = reuse;
                    ruleDayCache[winnerRule.Id] = reuse.Id;
                    if (logEntry != null)
                        logEntry.UsedDayCache = true;
                    DebugLogger.Trace("[Schedule]   Day cache → seeded from current outfit");
                }
            }

            if (chosenSet == null)
            {
                state = scheduleStore.GetRotationState(winnerRule.Id) ?? new RotationState { RuleId = winnerRule.Id };
                string lastUsedName = state.LastUsedId != null ? (outfitSetStore.GetById(state.LastUsedId)?.Name ?? "?") : "none";
                DebugLogger.Trace($"[Schedule]   Rotation: {state.Queue.Count} queued, last='{lastUsedName}'");

                if (logEntry != null)
                {
                    logEntry.LastUsedOutfitName = state.LastUsedId != null ? (outfitSetStore.GetById(state.LastUsedId)?.Name) : null;
                }

                chosenSet = PickOutfit(winnerPool, state, random, out bool wasReshuffled);
                if (chosenSet == null)
                    return;

                if (logEntry != null)
                {
                    logEntry.QueueSizeAfter = state.Queue.Count;
                    logEntry.WasReshuffled = wasReshuffled;
                }

                if (!winnerRule.AdvanceOnWarp)
                    ruleDayCache[winnerRule.Id] = chosenSet.Id;
            }

            int maxPriority = candidates.Max(c => c.rule.Priority);
            var tiedRules = candidates.Where(c => c.rule.Priority == maxPriority).ToList();

            List<string> cachedIds = tiedRules.Count == 1
                ? new List<string> { winnerRule.Id }
                : tiedRules.Select(c => c.rule.Id).ToList();

            contextCache[signature] = new CachedContext(cachedIds, maxPriority, IsNoMatch: false);

            bool stateChanged = false;
            if (state != null)
            {
                scheduleStore.SetRotationState(winnerRule.Id, state);
                stateChanged = true;
            }

            if (chosenSet.Id != lastAppliedOutfitId)
            {
                outfitSetStore.ApplySet(chosenSet);
                lastAppliedOutfitId = chosenSet.Id;
                stateChanged = true;

                string appliedSuffix = state != null ? $" ({state.Queue.Count} left)" : "";
                DebugLogger.Trace($"[Schedule]   >> Applied '{chosenSet.Name}'{appliedSuffix}");

                if (logEntry != null)
                {
                    logEntry.ChosenOutfitName = chosenSet.Name;
                    logEntry.WasAlreadyApplied = false;
                }
            }
            else
            {
                DebugLogger.Trace($"[Schedule]   Already wearing '{chosenSet.Name}'");

                if (logEntry != null)
                {
                    logEntry.ChosenOutfitName = chosenSet.Name;
                    logEntry.WasAlreadyApplied = true;
                }
            }

            if (stateChanged)
                scheduleStore.SaveLocalData();
        }

        internal static bool MatchesTriggers(ScheduleRule rule, EvaluationContext ctx)
            => MatchesTriggers(rule, ctx, out _, out _);

        internal static bool MatchesTriggers(ScheduleRule rule, EvaluationContext ctx, out RuleMatchResult result, out string failDetail)
        {
            result = RuleMatchResult.Matched;
            failDetail = "";

            bool seasonsConfigured = rule.SeasonsSelectAll || rule.SelectedSeasons.Count > 0;
            bool festivalsConfigured = rule.FestivalsSelectAll || rule.SelectedFestivals.Count > 0;
            bool weatherConfigured = rule.WeatherSelectAll || rule.SelectedWeather.Count > 0;
            bool locationsConfigured = rule.LocationsSelectAll || rule.SelectedLocations.Count > 0;
            bool areasConfigured = rule.AreasSelectAll || rule.SelectedAreas.Count > 0;
            bool weddingConfigured = rule.IsWeddingDay;

            if (!seasonsConfigured && !festivalsConfigured && !weatherConfigured && !locationsConfigured && !areasConfigured && !weddingConfigured)
                return true;

            // Special event rules: only check festival + wedding triggers, skip conditions
            if (rule.IsSpecialEventRule)
            {
                bool festivalMatch = false;
                bool weddingMatch = false;

                if (festivalsConfigured)
                {
                    bool isFestivalDay = rule.FestivalsSelectAll || rule.SelectedFestivals.Any(id => ctx.TodaysFestivalIds.Contains(id));
                    if (isFestivalDay)
                    {
                        if (ctx.IsActiveFestival || ctx.IsAtPassiveFestivalLocation)
                        {
                            festivalMatch = true;
                        }
                        else
                        {
                            failDetail = "festival day but not at location";
                            ScheduleDebug.Trace($"[Schedule]       FAIL festival location: {failDetail}");
                            result = RuleMatchResult.FailFestivalLocation;
                            return false;
                        }
                    }
                    else
                    {
                        failDetail = $"[{string.Join(", ", rule.SelectedFestivals)}] vs [{string.Join(", ", ctx.TodaysFestivalIds)}]";
                        ScheduleDebug.Trace($"[Schedule]       FAIL festival: {failDetail}");
                        result = RuleMatchResult.FailFestival;
                        return false;
                    }
                }

                if (weddingConfigured)
                {
                    if (ctx.IsWeddingDay && ctx.Trigger == EvaluationTrigger.DayStarted)
                    {
                        weddingMatch = true;
                    }
                    else
                    {
                        failDetail = $"day={ctx.IsWeddingDay}, trigger={ctx.Trigger}";
                        ScheduleDebug.Trace($"[Schedule]       FAIL wedding: {failDetail}");
                        result = RuleMatchResult.FailWedding;
                        return false;
                    }
                }

                return festivalMatch || weddingMatch;
            }

            // Normal rules: check all triggers
            if (seasonsConfigured &&
                !rule.SeasonsSelectAll &&
                !rule.SelectedSeasons.Any(s => s.Equals(ctx.CurrentSeason, StringComparison.OrdinalIgnoreCase)))
            {
                failDetail = $"[{string.Join(", ", rule.SelectedSeasons)}] vs '{ctx.CurrentSeason}'";
                ScheduleDebug.Trace($"[Schedule]       FAIL season: {failDetail}");
                result = RuleMatchResult.FailSeason;
                return false;
            }

            if (festivalsConfigured &&
                !rule.FestivalsSelectAll &&
                !rule.SelectedFestivals.Any(id => ctx.TodaysFestivalIds.Contains(id)))
            {
                failDetail = $"[{string.Join(", ", rule.SelectedFestivals)}] vs [{string.Join(", ", ctx.TodaysFestivalIds)}]";
                ScheduleDebug.Trace($"[Schedule]       FAIL festival: {failDetail}");
                result = RuleMatchResult.FailFestival;
                return false;
            }

            if (weatherConfigured &&
                !rule.WeatherSelectAll &&
                !rule.SelectedWeather.Any(w => w.Equals(ctx.CurrentWeather, StringComparison.OrdinalIgnoreCase)))
            {
                failDetail = $"[{string.Join(", ", rule.SelectedWeather)}] vs '{ctx.CurrentWeather}'";
                ScheduleDebug.Trace($"[Schedule]       FAIL weather: {failDetail}");
                result = RuleMatchResult.FailWeather;
                return false;
            }

            if (locationsConfigured &&
                !rule.LocationsSelectAll &&
                !rule.SelectedLocations.Any(l => l.Equals(ctx.CurrentLocationName, StringComparison.OrdinalIgnoreCase)))
            {
                failDetail = $"[{string.Join(", ", rule.SelectedLocations)}] vs '{ctx.CurrentLocationName}'";
                ScheduleDebug.Trace($"[Schedule]       FAIL location: {failDetail}");
                result = RuleMatchResult.FailLocation;
                return false;
            }

            if (areasConfigured &&
                !rule.AreasSelectAll &&
                !rule.SelectedAreas.Any(a =>
                    (a.Equals("Outdoor", StringComparison.OrdinalIgnoreCase) && ctx.IsOutdoors) ||
                    (a.Equals("Indoor", StringComparison.OrdinalIgnoreCase) && !ctx.IsOutdoors)))
            {
                failDetail = $"[{string.Join(", ", rule.SelectedAreas)}] vs {(ctx.IsOutdoors ? "Outdoor" : "Indoor")}";
                ScheduleDebug.Trace($"[Schedule]       FAIL area: {failDetail}");
                result = RuleMatchResult.FailArea;
                return false;
            }

            if (rule.IsWeddingDay && (!ctx.IsWeddingDay || ctx.Trigger != EvaluationTrigger.DayStarted))
            {
                failDetail = $"day={ctx.IsWeddingDay}, trigger={ctx.Trigger}";
                ScheduleDebug.Trace($"[Schedule]       FAIL wedding: {failDetail}");
                result = RuleMatchResult.FailWedding;
                return false;
            }

            return true;
        }

        internal static List<OutfitSet> ResolvePool(ScheduleRule rule, List<OutfitSet> allSets)
        {
            List<OutfitSet> pool;

            if (rule.TagsSelectAll)
            {
                pool = new List<OutfitSet>(allSets);
                ScheduleDebug.Trace($"[Schedule]       Pool: ALL tags → {pool.Count}");
            }
            else
            {
                var tagSet = new HashSet<string>(rule.SelectedTags, TranslationCache.TagComparer);
                pool = allSets
                    .Where(s => s.Tags.Any(t => tagSet.Contains(t)))
                    .ToList();
                ScheduleDebug.Trace($"[Schedule]       Pool: [{string.Join(", ", rule.SelectedTags)}] → {pool.Count} matched");
            }

            if (rule.IncludedSetIds.Count > 0)
            {
                var poolIds = new HashSet<string>(pool.Select(s => s.Id));
                int added = 0;
                foreach (var id in rule.IncludedSetIds)
                {
                    if (!poolIds.Contains(id))
                    {
                        var set = allSets.FirstOrDefault(s => s.Id == id);
                        if (set != null)
                        {
                            pool.Add(set);
                            added++;
                        }
                    }
                }
                if (added > 0)
                    ScheduleDebug.Trace($"[Schedule]       Pool: +{added} included → {pool.Count}");
            }

            var excluded = new HashSet<string>(rule.ExcludedSetIds);
            int beforeExclude = pool.Count;
            pool.RemoveAll(s => excluded.Contains(s.Id));
            int removed = beforeExclude - pool.Count;
            if (removed > 0)
                ScheduleDebug.Trace($"[Schedule]       Pool: removed {removed} excluded, {pool.Count} left");

            return pool;
        }


        internal static (ScheduleRule rule, List<OutfitSet> pool) SelectWinner(
            List<(ScheduleRule rule, List<OutfitSet> pool)> candidates,
            Random random)
        {
            int maxPriority = candidates.Max(c => c.rule.Priority);
            var filtered = candidates.Where(c => c.rule.Priority == maxPriority).ToList();

            if (filtered.Count < candidates.Count)
                ScheduleDebug.Debug($"[Schedule]   Priority filter: kept {filtered.Count} of {candidates.Count} candidates ({GetPriorityLabel(maxPriority)})");

            if (filtered.Count > 1)
                ScheduleDebug.Debug($"[Schedule]   Tie-break: {filtered.Count} at {GetPriorityLabel(maxPriority)}, random pick");

            return filtered[random.Next(filtered.Count)];
        }

        internal static string GetTiebreakPoolKey(List<(ScheduleRule rule, List<OutfitSet> pool)> candidates)
        {
            var ids = candidates.Select(c => c.rule.Id).OrderBy(id => id, StringComparer.Ordinal);
            return string.Join("|", ids);
        }

        internal static string GetTiebreakPoolKey(List<string> ruleIds)
        {
            var sorted = ruleIds.OrderBy(id => id, StringComparer.Ordinal);
            return string.Join("|", sorted);
        }

        internal (ScheduleRule rule, List<OutfitSet> pool, bool usedConsistentCache) SelectWinnerConsistent(
            List<(ScheduleRule rule, List<OutfitSet> pool)> candidates, Random random)
        {
            int maxPriority = candidates.Max(c => c.rule.Priority);
            var filtered = candidates.Where(c => c.rule.Priority == maxPriority).ToList();

            if (!getConsistentTiebreaks() || filtered.Count <= 1)
            {
                var result = SelectWinner(candidates, random);
                return (result.rule, result.pool, false);
            }

            var poolKey = GetTiebreakPoolKey(filtered);
            if (tiebreakCache.TryGetValue(poolKey, out var cachedWinnerId))
            {
                var cached = filtered.FirstOrDefault(c => c.rule.Id == cachedWinnerId);
                if (cached.rule != null)
                    return (cached.rule, cached.pool, true);
            }

            var winner = SelectWinner(candidates, random);
            tiebreakCache[poolKey] = winner.rule.Id;
            return (winner.rule, winner.pool, false);
        }

        private static string GetPriorityLabel(int priority)
        {
            if (priority >= 3) return "High";
            if (priority <= 1) return "Low";
            return "Medium";
        }

        internal static OutfitSet? PickOutfit(List<OutfitSet> pool, RotationState state, Random random)
            => PickOutfit(pool, state, random, out _);

        internal static OutfitSet? PickOutfit(List<OutfitSet> pool, RotationState state, Random random, out bool wasReshuffled)
        {
            wasReshuffled = false;

            if (pool.Count == 0)
                return null;

            if (pool.Count == 1)
            {
                ScheduleDebug.Debug($"[Schedule]   Single set → '{pool[0].Name}'");
                state.Queue.Clear();
                state.LastUsedId = pool[0].Id;
                return pool[0];
            }

            return PickWithRotation(pool, state, random, out wasReshuffled);
        }

        internal static OutfitSet? PickWithRotation(List<OutfitSet> pool, RotationState state, Random random)
            => PickWithRotation(pool, state, random, out _);

        internal static OutfitSet? PickWithRotation(List<OutfitSet> pool, RotationState state, Random random, out bool wasReshuffled)
        {
            wasReshuffled = false;
            var poolIds = new HashSet<string>(pool.Select(s => s.Id));

            int staleCount = state.Queue.RemoveAll(id => !poolIds.Contains(id));
            if (staleCount > 0)
                ScheduleDebug.Debug($"[Schedule]   Rotation: pruned {staleCount} stale");

            if (state.Queue.Count == 0)
                ReshuffleQueue(pool, state, random);

            string chosenId = state.Queue[0];
            state.Queue.RemoveAt(0);
            state.LastUsedId = chosenId;

            if (state.Queue.Count == 0)
            {
                wasReshuffled = true;
                ReshuffleQueue(pool, state, random);
            }

            var chosen = pool.FirstOrDefault(s => s.Id == chosenId);
            return chosen;
        }

        private static void ReshuffleQueue(List<OutfitSet> pool, RotationState state, Random random)
        {
            ScheduleDebug.Debug($"[Schedule]   Rotation: reshuffling {pool.Count} sets");
            state.Queue = pool.Select(s => s.Id).ToList();
            ShuffleList(state.Queue, random);

            if (state.LastUsedId != null && state.Queue.Count > 1 && state.Queue[0] == state.LastUsedId)
            {
                int swapIndex = 1 + random.Next(state.Queue.Count - 1);
                (state.Queue[0], state.Queue[swapIndex]) = (state.Queue[swapIndex], state.Queue[0]);
                ScheduleDebug.Debug("[Schedule]   Rotation: anti-repeat swap");
            }
        }

        internal static void ShuffleList<T>(List<T> list, Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        internal static string ResolveFestivalDisplayName(string id,
            Dictionary<string, string>? activeFestivals,
            Dictionary<string, string>? passiveFestivalNames)
        {
            if (activeFestivals != null && activeFestivals.TryGetValue(id, out string? activeName)
                && !string.IsNullOrEmpty(activeName))
                return activeName;

            if (passiveFestivalNames != null && passiveFestivalNames.TryGetValue(id, out string? passiveName)
                && !string.IsNullOrEmpty(passiveName))
                return passiveName;

            return id;
        }

        private bool IsPlayerAtPassiveFestivalLocation()
        {
            if (cachedPassiveFestivals == null || Game1.currentLocation == null)
                return false;

            string locationName = Game1.currentLocation.Name;

            foreach (var (_, data) in cachedPassiveFestivals)
            {
                if (data.Season != Game1.season ||
                    Game1.dayOfMonth < data.StartDay ||
                    Game1.dayOfMonth > data.EndDay)
                    continue;

                if (Game1.timeOfDay >= data.StartTime &&
                    data.MapReplacements != null &&
                    data.MapReplacements.Keys.Any(k => k.Equals(locationName, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            return false;
        }

        private HashSet<string> GetTodaysFestivalIds()
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string todayKey = $"{Game1.currentSeason}{Game1.dayOfMonth}";

            if (cachedActiveFestivals != null && cachedActiveFestivals.ContainsKey(todayKey))
                ids.Add(todayKey);

            if (cachedPassiveFestivals != null)
            {
                foreach (var (key, data) in cachedPassiveFestivals)
                {
                    if (data.Season == Game1.season &&
                        Game1.dayOfMonth >= data.StartDay &&
                        Game1.dayOfMonth <= data.EndDay)
                    {
                        ids.Add(key);
                    }
                }
            }

            return ids;
        }

        private void EnsureFestivalDataLoaded()
        {
            if (cachedActiveFestivals != null)
                return;

            cachedActiveFestivals = DataLoader.Festivals_FestivalDates(Game1.content);
            cachedPassiveFestivals = DataLoader.PassiveFestivals(Game1.content);

            DebugLogger.Trace($"[Schedule] Loaded {cachedActiveFestivals.Count} active + {cachedPassiveFestivals.Count} passive festivals");
        }
    }
}
