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
        public List<string> Tags { get; set; } = new();
        public bool IsFavorite { get; set; }
        public bool IsGlobal { get; set; } = true;

        /// <summary>
        /// Runtime-only flag indicating whether all items in the set exist in the game.
        /// Recalculated on load - any persisted value is ignored.
        /// </summary>
        public bool IsValid { get; set; } = true;
    }

    /// <summary>
    /// Container for global outfit sets stored in mod folder JSON file.
    /// </summary>
    public class OutfitSetGlobalData
    {
        public int Version { get; set; } = 1;
        public List<string> Tags { get; set; } = new();
        public List<OutfitSet> Sets { get; set; } = new();
    }

    /// <summary>
    /// Container for local outfit sets stored in SMAPI save data.
    /// </summary>
    public class OutfitSetLocalData
    {
        public List<OutfitSet> Sets { get; set; } = new();
    }
}
