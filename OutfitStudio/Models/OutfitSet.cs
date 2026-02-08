using System;
using System.Collections.Generic;

namespace OutfitStudio.Models
{
    public class OutfitSet
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string? ShirtId { get; set; }
        public string? PantsId { get; set; }
        public string? HatId { get; set; }
        public string? ShirtColor { get; set; }
        public string? PantsColor { get; set; }
        public List<string> Tags { get; set; } = new();
        public bool IsFavorite { get; set; }
        public bool IsGlobal { get; set; } = true;

        // Runtime-only — recalculated on load, persisted value is ignored
        public bool IsValid { get; set; } = true;
    }

    public class OutfitSetGlobalData
    {
        public int Version { get; set; } = 1;
        public List<string> Tags { get; set; } = new();
        public List<OutfitSet> Sets { get; set; } = new();
    }

    public class OutfitSetLocalData
    {
        public List<OutfitSet> Sets { get; set; } = new();
    }
}
