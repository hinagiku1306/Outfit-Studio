using System;
using System.Collections.Generic;

namespace FittingRoom.Models
{
    public class OutfitTemplate
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string? ShirtId { get; set; }
        public string? PantsId { get; set; }
        public string? HatId { get; set; }
        public string? Tag { get; set; }
        public bool IsFavorite { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class OutfitTemplateData
    {
        public List<OutfitTemplate> Templates { get; set; } = new();
    }
}
