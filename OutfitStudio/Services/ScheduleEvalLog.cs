using System.Collections.Generic;
using OutfitStudio.Models;

namespace OutfitStudio.Services
{
    internal class ScheduleEvalLog
    {
        private const int MaxEntries = 50;
        private readonly List<ScheduleEvalEntry> entries = new();

        public IReadOnlyList<ScheduleEvalEntry> Entries => entries;

        public ScheduleEvalEntry CreateEntry()
        {
            var entry = new ScheduleEvalEntry { Timestamp = System.DateTime.Now };
            entries.Insert(0, entry);
            if (entries.Count > MaxEntries)
                entries.RemoveAt(entries.Count - 1);
            return entry;
        }

        public void Clear() => entries.Clear();
    }
}
