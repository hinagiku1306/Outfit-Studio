using System;
using System.Collections.Generic;
using System.Linq;
using OutfitStudio.Models;
using StardewModdingAPI;

namespace OutfitStudio.Services
{
    public class ScheduleStore
    {
        private const string SaveDataKey = "OutfitStudio.Schedules";

        private readonly IModHelper helper;
        private readonly OutfitSetStore outfitSetStore;

        private ScheduleData data = new();

        public ScheduleStore(IModHelper helper, OutfitSetStore outfitSetStore)
        {
            this.helper = helper;
            this.outfitSetStore = outfitSetStore;
        }

        public Action<string>? OnRulesChanged { get; set; }

        public bool IsEnabled
        {
            get => data.Enabled;
            set => data.Enabled = value;
        }

        public void LoadLocalData()
        {
            data = helper.Data.ReadSaveData<ScheduleData>(SaveDataKey) ?? new ScheduleData();

            PruneOrphanedRotationStates();
            PruneStaleSelectedSetIds();

            DebugLogger.Log($"Loaded {data.Rules.Count} schedule rules.", LogLevel.Trace);
        }

        public void SaveLocalData()
        {
            helper.Data.WriteSaveData(SaveDataKey, data);
        }

        public void ClearLocalData()
        {
            data = new ScheduleData();
        }

        public List<ScheduleRule> GetRules()
        {
            return data.Rules;
        }

        public ScheduleRule? GetRuleById(string id)
        {
            return data.Rules.FirstOrDefault(r => r.Id == id);
        }

        public void AddRule(ScheduleRule rule)
        {
            data.Rules.Add(rule);
            SaveLocalData();
            OnRulesChanged?.Invoke(rule.Id);
        }

        public void UpdateRule(ScheduleRule rule)
        {
            int index = data.Rules.FindIndex(r => r.Id == rule.Id);
            if (index < 0)
                return;

            data.Rules[index] = rule;
            SaveLocalData();
            OnRulesChanged?.Invoke(rule.Id);
        }

        public void UpdateRule(ScheduleRule rule, List<string> previousSetIds)
        {
            int index = data.Rules.FindIndex(r => r.Id == rule.Id);
            if (index < 0)
                return;

            data.Rules[index] = rule;

            if (data.RotationStates.TryGetValue(rule.Id, out var rotState))
                SyncQueueWithSetIds(rotState, previousSetIds, rule.SelectedSetIds, new Random());

            SaveLocalData();
            OnRulesChanged?.Invoke(rule.Id);
        }

        public void DeleteRule(string ruleId)
        {
            data.Rules.RemoveAll(r => r.Id == ruleId);
            data.RotationStates.Remove(ruleId);
            SaveLocalData();
            OnRulesChanged?.Invoke(ruleId);
        }

        public RotationState? GetRotationState(string ruleId)
        {
            return data.RotationStates.TryGetValue(ruleId, out var state) ? state : null;
        }

        public void SetRotationState(string ruleId, RotationState state)
        {
            data.RotationStates[ruleId] = state;
        }

        public void ClearRotationState(string ruleId)
        {
            data.RotationStates.Remove(ruleId);
        }

        private void PruneOrphanedRotationStates()
        {
            var ruleIds = new HashSet<string>(data.Rules.Select(r => r.Id));
            var orphanedKeys = data.RotationStates.Keys.Where(k => !ruleIds.Contains(k)).ToList();

            foreach (var key in orphanedKeys)
            {
                data.RotationStates.Remove(key);
            }

            if (orphanedKeys.Count > 0)
                DebugLogger.Log($"Pruned {orphanedKeys.Count} orphaned rotation states.", LogLevel.Trace);
        }

        internal static void SyncQueueWithSetIds(RotationState rotState, List<string> oldSetIds, List<string> newSetIds, Random random)
        {
            var oldSet = new HashSet<string>(oldSetIds);
            var queueSet = new HashSet<string>(rotState.Queue);
            var newIds = newSetIds.Where(id => !oldSet.Contains(id) && !queueSet.Contains(id)).ToList();
            foreach (string newId in newIds)
            {
                int insertIndex = rotState.Queue.Count == 0 ? 0 : random.Next(rotState.Queue.Count + 1);
                rotState.Queue.Insert(insertIndex, newId);
            }

            var newSet = new HashSet<string>(newSetIds);
            rotState.Queue.RemoveAll(id => !newSet.Contains(id));
        }

        private void PruneStaleSelectedSetIds()
        {
            foreach (var rule in data.Rules)
            {
                int before = rule.SelectedSetIds.Count;
                rule.SelectedSetIds.RemoveAll(id => outfitSetStore.GetById(id) == null);

                int removed = before - rule.SelectedSetIds.Count;
                if (removed > 0)
                {
                    DebugLogger.Log($"Pruned {removed} stale selected set IDs from rule '{rule.Name}'.", LogLevel.Trace);

                    if (data.RotationStates.TryGetValue(rule.Id, out var rotState))
                    {
                        var validIds = new HashSet<string>(rule.SelectedSetIds);
                        rotState.Queue.RemoveAll(id => !validIds.Contains(id));
                    }
                }
            }
        }
    }
}
