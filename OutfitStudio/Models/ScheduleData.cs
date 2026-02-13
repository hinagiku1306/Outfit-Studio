using System.Collections.Generic;

namespace OutfitStudio.Models
{
    public class ScheduleData
    {
        public bool Enabled { get; set; } = true;
        public List<ScheduleRule> Rules { get; set; } = new();
        public Dictionary<string, RotationState> RotationStates { get; set; } = new();
    }
}
