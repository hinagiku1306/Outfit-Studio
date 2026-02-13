using System;
using System.Collections.Generic;
using System.Linq;
using OutfitStudio.Models;
using StardewValley;
using StardewValley.GameData;

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
        private record CachedEvaluation(string? OutfitId, string? WinningRuleId);

        private readonly ScheduleStore scheduleStore;
        private readonly OutfitSetStore outfitSetStore;
        private readonly Random random = new();

        private Dictionary<string, string>? cachedActiveFestivals;
        private Dictionary<string, PassiveFestivalData>? cachedPassiveFestivals;
        private string? lastAppliedOutfitId;
        private readonly Dictionary<string, CachedEvaluation> contextCache = new();
        private readonly Dictionary<string, string> ruleDayCache = new();
        private string? lastContextSignature;
        private string? manualDayOutfitId;

        public ScheduleEngine(ScheduleStore scheduleStore, OutfitSetStore outfitSetStore)
        {
            this.scheduleStore = scheduleStore;
            this.outfitSetStore = outfitSetStore;

            ScheduleDebug.TraceLog = msg => DebugLogger.Trace(msg);
            ScheduleDebug.DebugLog = msg => DebugLogger.Debug(msg);
        }

        public void InvalidateFestivalCache()
        {
            cachedActiveFestivals = null;
            cachedPassiveFestivals = null;
        }

        public void InvalidateContextCache()
        {
            contextCache.Clear();
        }

        public void ResetForNewDay()
        {
            lastAppliedOutfitId = null;
            manualDayOutfitId = null;
            contextCache.Clear();
            ruleDayCache.Clear();
        }

        public void SetManualOutfit(string outfitId)
        {
            manualDayOutfitId = outfitId;
            if (lastContextSignature != null)
                contextCache[lastContextSignature] = new CachedEvaluation(outfitId, null);
            lastAppliedOutfitId = outfitId;
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

            DebugLogger.Debug("");
            DebugLogger.Debug($"[Schedule] ══ {trigger} → {ctx.CurrentLocationName} ══");
            DebugLogger.Debug($"[Schedule]   {ctx.CurrentSeason} | {ctx.CurrentWeather} | {(ctx.IsOutdoors ? "Outdoors" : "Indoors")}{(ctx.IsActiveFestival ? " | Festival" : "")}{(ctx.IsAtPassiveFestivalLocation ? " | PassiveFestival" : "")}{(ctx.IsWeddingDay ? " | Wedding" : "")}");

            if (contextCache.TryGetValue(signature, out var cached))
            {
                if (cached.OutfitId == null)
                {
                    DebugLogger.Debug("[Schedule]   Cache: no prior match → manual fallback");
                    TryApplyManualFallback(signature);
                    return;
                }

                var cachedRule = cached.WinningRuleId != null ? scheduleStore.GetRuleById(cached.WinningRuleId) : null;
                if (ShouldBypassCache(cachedRule, trigger))
                {
                    DebugLogger.Debug($"[Schedule]   Cache bypassed (AdvanceOnWarp) for '{cachedRule!.Name}'");
                    contextCache.Remove(signature);
                }
                else
                {
                    if (cached.OutfitId != lastAppliedOutfitId)
                    {
                        var cachedSet = outfitSetStore.GetById(cached.OutfitId);
                        if (cachedSet != null)
                        {
                            DebugLogger.Debug($"[Schedule]   Cache hit → re-applying '{cachedSet.Name}'");
                            outfitSetStore.ApplySet(cachedSet);
                            lastAppliedOutfitId = cachedSet.Id;
                        }
                    }
                    else
                    {
                        DebugLogger.Debug("[Schedule]   Cache hit → already applied");
                    }
                    return;
                }
            }

            var rules = scheduleStore.GetRules();
            var enabledRules = rules.Where(r => r.IsEnabled).ToList();
            DebugLogger.Debug($"[Schedule]   Rules: {enabledRules.Count}/{rules.Count} enabled");
            if (enabledRules.Count == 0)
            {
                DebugLogger.Debug("[Schedule]   No enabled rules → manual fallback");
                contextCache[signature] = new CachedEvaluation(null, null);
                TryApplyManualFallback(signature);
                return;
            }

            var allSets = outfitSetStore.GetAllSets();
            var candidates = new List<(ScheduleRule rule, List<OutfitSet> pool)>();

            foreach (var rule in enabledRules)
            {
                if (!MatchesTriggers(rule, ctx))
                {
                    ScheduleDebug.Trace($"[Schedule]     '{rule.Name}' → no match");
                    continue;
                }

                var pool = ResolvePool(rule, allSets);
                if (pool.Count == 0)
                {
                    ScheduleDebug.Trace($"[Schedule]     '{rule.Name}' → matched, 0 sets");
                    continue;
                }

                DebugLogger.Debug($"[Schedule]     '{rule.Name}' → {pool.Count} sets ({GetPriorityLabel(rule.Priority)})");
                candidates.Add((rule, pool));
            }

            if (candidates.Count == 0)
            {
                DebugLogger.Debug("[Schedule]   No rules matched → manual fallback");
                contextCache[signature] = new CachedEvaluation(null, null);
                TryApplyManualFallback(signature);
                return;
            }

            var (winnerRule, winnerPool) = SelectWinner(candidates, random);
            DebugLogger.Debug($"[Schedule]   WINNER: '{winnerRule.Name}' {GetPriorityLabel(winnerRule.Priority)}{(candidates.Count > 1 ? $" (of {candidates.Count})" : "")}");

            OutfitSet? chosenSet = null;
            RotationState? state = null;

            if (!winnerRule.AdvanceOnWarp && ruleDayCache.TryGetValue(winnerRule.Id, out var dayOutfitId))
            {
                DebugLogger.Debug("[Schedule]   Day cache → reusing previous outfit");
                chosenSet = outfitSetStore.GetById(dayOutfitId);
            }

            if (chosenSet == null)
            {
                state = scheduleStore.GetRotationState(winnerRule.Id) ?? new RotationState { RuleId = winnerRule.Id };
                string lastUsedName = state.LastUsedId != null ? (outfitSetStore.GetById(state.LastUsedId)?.Name ?? "?") : "none";
                DebugLogger.Debug($"[Schedule]   Rotation: {state.Queue.Count} queued, last='{lastUsedName}'");

                chosenSet = PickOutfit(winnerPool, state, random);
                if (chosenSet == null)
                    return;

                if (!winnerRule.AdvanceOnWarp)
                    ruleDayCache[winnerRule.Id] = chosenSet.Id;
            }

            contextCache[signature] = new CachedEvaluation(chosenSet.Id, winnerRule.Id);

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
                DebugLogger.Debug($"[Schedule]   >> Applied '{chosenSet.Name}'{appliedSuffix}");
            }
            else
            {
                DebugLogger.Debug($"[Schedule]   Already wearing '{chosenSet.Name}'");
            }

            if (stateChanged)
                scheduleStore.SaveLocalData();
        }


        private void TryApplyManualFallback(string signature)
        {
            if (manualDayOutfitId == null || manualDayOutfitId == lastAppliedOutfitId)
                return;

            var set = outfitSetStore.GetById(manualDayOutfitId);
            if (set != null)
            {
                outfitSetStore.ApplySet(set);
                lastAppliedOutfitId = set.Id;
                contextCache[signature] = new CachedEvaluation(manualDayOutfitId, null);
            }
        }

        internal static bool MatchesTriggers(ScheduleRule rule, EvaluationContext ctx)
        {
            bool seasonsConfigured = rule.SeasonsSelectAll || rule.SelectedSeasons.Count > 0;
            bool festivalsConfigured = rule.FestivalsSelectAll || rule.SelectedFestivals.Count > 0;
            bool weatherConfigured = rule.WeatherSelectAll || rule.SelectedWeather.Count > 0;
            bool locationsConfigured = rule.LocationsSelectAll || rule.SelectedLocations.Count > 0;
            bool areasConfigured = rule.AreasSelectAll || rule.SelectedAreas.Count > 0;
            bool weddingConfigured = rule.IsWeddingDay;

            if (!seasonsConfigured && !festivalsConfigured && !weatherConfigured && !locationsConfigured && !areasConfigured && !weddingConfigured)
                return true;

            if (seasonsConfigured &&
                !rule.SeasonsSelectAll &&
                !rule.SelectedSeasons.Any(s => s.Equals(ctx.CurrentSeason, StringComparison.OrdinalIgnoreCase)))
            {
                ScheduleDebug.Trace($"[Schedule]       FAIL season: [{string.Join(", ", rule.SelectedSeasons)}] vs '{ctx.CurrentSeason}'");
                return false;
            }

            if (festivalsConfigured &&
                !rule.FestivalsSelectAll &&
                !rule.SelectedFestivals.Any(id => ctx.TodaysFestivalIds.Contains(id)))
            {
                ScheduleDebug.Trace($"[Schedule]       FAIL festival: [{string.Join(", ", rule.SelectedFestivals)}] vs [{string.Join(", ", ctx.TodaysFestivalIds)}]");
                return false;
            }

            if (weatherConfigured &&
                !rule.WeatherSelectAll &&
                !rule.SelectedWeather.Any(w => w.Equals(ctx.CurrentWeather, StringComparison.OrdinalIgnoreCase)))
            {
                ScheduleDebug.Trace($"[Schedule]       FAIL weather: [{string.Join(", ", rule.SelectedWeather)}] vs '{ctx.CurrentWeather}'");
                return false;
            }

            if (locationsConfigured &&
                !rule.LocationsSelectAll &&
                !rule.SelectedLocations.Any(l => l.Equals(ctx.CurrentLocationName, StringComparison.OrdinalIgnoreCase)))
            {
                ScheduleDebug.Trace($"[Schedule]       FAIL location: [{string.Join(", ", rule.SelectedLocations)}] vs '{ctx.CurrentLocationName}'");
                return false;
            }

            if (areasConfigured &&
                !rule.AreasSelectAll &&
                !rule.SelectedAreas.Any(a =>
                    (a.Equals("Outdoor", StringComparison.OrdinalIgnoreCase) && ctx.IsOutdoors) ||
                    (a.Equals("Indoor", StringComparison.OrdinalIgnoreCase) && !ctx.IsOutdoors)))
            {
                ScheduleDebug.Trace($"[Schedule]       FAIL area: [{string.Join(", ", rule.SelectedAreas)}] vs {(ctx.IsOutdoors ? "Outdoor" : "Indoor")}");
                return false;
            }

            if (rule.IsWeddingDay && (!ctx.IsWeddingDay || ctx.Trigger != EvaluationTrigger.DayStarted))
            {
                ScheduleDebug.Trace($"[Schedule]       FAIL wedding: day={ctx.IsWeddingDay}, trigger={ctx.Trigger}");
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

        private static string GetPriorityLabel(int priority)
        {
            if (priority >= 3) return "High";
            if (priority <= 1) return "Low";
            return "Medium";
        }

        internal static OutfitSet? PickOutfit(List<OutfitSet> pool, RotationState state, Random random)
        {
            if (pool.Count == 0)
                return null;

            if (pool.Count == 1)
            {
                ScheduleDebug.Debug($"[Schedule]   Single set → '{pool[0].Name}'");
                return pool[0];
            }

            return PickWithRotation(pool, state, random);
        }

        internal static OutfitSet? PickWithRotation(List<OutfitSet> pool, RotationState state, Random random)
        {
            var poolIds = new HashSet<string>(pool.Select(s => s.Id));

            int staleCount = state.Queue.RemoveAll(id => !poolIds.Contains(id));
            if (staleCount > 0)
                ScheduleDebug.Debug($"[Schedule]   Rotation: pruned {staleCount} stale");

            if (state.Queue.Count == 0)
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

            string chosenId = state.Queue[0];
            state.Queue.RemoveAt(0);
            state.LastUsedId = chosenId;

            var chosen = pool.FirstOrDefault(s => s.Id == chosenId);
            return chosen;
        }

        internal static bool ShouldBypassCache(ScheduleRule? winningRule, EvaluationTrigger trigger)
        {
            if (winningRule == null) return false;
            return winningRule.AdvanceOnWarp && trigger == EvaluationTrigger.Warped;
        }

        internal static void ShuffleList<T>(List<T> list, Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
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

                if (data.MapReplacements != null &&
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

            DebugLogger.Debug($"[Schedule] Loaded {cachedActiveFestivals.Count} active + {cachedPassiveFestivals.Count} passive festivals");
        }
    }
}
