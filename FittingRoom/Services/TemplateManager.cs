using System;
using System.Collections.Generic;
using FittingRoom.Models;
using StardewModdingAPI;
using StardewValley;

namespace FittingRoom.Services
{
    public class TemplateManager
    {
        private const string SaveDataKey = "FittingRoom.Templates";

        private readonly IModHelper helper;
        private OutfitTemplateData? cachedData;

        public TemplateManager(IModHelper helper)
        {
            this.helper = helper;
        }

        public List<OutfitTemplate> GetAllTemplates()
        {
            LoadDataIfNeeded();
            return cachedData!.Templates;
        }

        public OutfitTemplate? GetTemplateById(string id)
        {
            LoadDataIfNeeded();
            return cachedData!.Templates.Find(t => t.Id == id);
        }

        public void SaveTemplate(OutfitTemplate template)
        {
            LoadDataIfNeeded();
            cachedData!.Templates.Add(template);
            PersistData();
        }

        public void UpdateTemplate(OutfitTemplate template)
        {
            LoadDataIfNeeded();
            int index = cachedData!.Templates.FindIndex(t => t.Id == template.Id);
            if (index >= 0)
            {
                cachedData.Templates[index] = template;
                PersistData();
            }
        }

        public void DeleteTemplate(string id)
        {
            LoadDataIfNeeded();
            cachedData!.Templates.RemoveAll(t => t.Id == id);
            PersistData();
        }

        public OutfitTemplate CreateFromCurrentOutfit(
            string name,
            string? tag,
            bool isFavorite,
            bool includeShirt,
            bool includePants,
            bool includeHat)
        {
            var template = new OutfitTemplate
            {
                Name = name,
                Tag = tag,
                IsFavorite = isFavorite,
                ShirtId = includeShirt ? OutfitState.GetClothingId(Game1.player.shirtItem.Value) : null,
                PantsId = includePants ? OutfitState.GetClothingId(Game1.player.pantsItem.Value) : null,
                HatId = includeHat ? OutfitState.GetHatIdFromItem(Game1.player.hat.Value) : null,
                CreatedAt = DateTime.UtcNow
            };

            SaveTemplate(template);
            return template;
        }

        private void LoadDataIfNeeded()
        {
            if (cachedData != null)
                return;

            cachedData = helper.Data.ReadSaveData<OutfitTemplateData>(SaveDataKey);
            cachedData ??= new OutfitTemplateData();
        }

        private void PersistData()
        {
            if (cachedData != null)
            {
                helper.Data.WriteSaveData(SaveDataKey, cachedData);
            }
        }

        public void InvalidateCache()
        {
            cachedData = null;
        }
    }
}
