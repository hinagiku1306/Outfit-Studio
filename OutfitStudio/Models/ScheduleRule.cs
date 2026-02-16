using System;
using System.Collections.Generic;

namespace OutfitStudio.Models
{
    public class ScheduleRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public bool IsEnabled { get; set; }

        public bool SeasonsSelectAll { get; set; }
        public List<string> SelectedSeasons { get; set; } = new();

        public bool FestivalsSelectAll { get; set; }
        public List<string> SelectedFestivals { get; set; } = new();

        public bool WeatherSelectAll { get; set; }
        public List<string> SelectedWeather { get; set; } = new();

        public bool LocationsSelectAll { get; set; }
        public List<string> SelectedLocations { get; set; } = new();

        public bool AreasSelectAll { get; set; }
        public List<string> SelectedAreas { get; set; } = new();

        public List<string> SelectedSetIds { get; set; } = new();

        public const int PrioritySpecial = 4;

        public int Priority { get; set; } = 2;
        public bool IsWeddingDay { get; set; }
        public bool AdvanceOnWarp { get; set; }

        public bool IsSpecialEventRule => IsWeddingDay || FestivalsSelectAll || SelectedFestivals.Count > 0;
        public int EffectivePriority => IsSpecialEventRule ? PrioritySpecial : Priority;
    }

    public class RotationState
    {
        public string RuleId { get; set; } = "";
        public List<string> Queue { get; set; } = new();
        public string? LastUsedId { get; set; }
    }
}
