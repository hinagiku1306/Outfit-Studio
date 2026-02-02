using System.Collections.Generic;

namespace FittingRoom
{
    /// <summary>
    /// Provides filtered item lists for each category in the outfit menu.
    /// </summary>
    public class OutfitItemListProvider
    {
        private readonly OutfitFilterManager filterManager;
        private readonly OutfitCategoryManager categoryManager;
        private readonly OutfitState state;

        public OutfitItemListProvider(
            OutfitFilterManager filterManager,
            OutfitCategoryManager categoryManager,
            OutfitState state)
        {
            this.filterManager = filterManager;
            this.categoryManager = categoryManager;
            this.state = state;
        }

        public int GetCurrentListCount()
        {
            var category = categoryManager.CurrentCategory;
            return category switch
            {
                OutfitCategoryManager.Category.All => GetCurrentAllItems().Count,
                OutfitCategoryManager.Category.Shirts => GetCurrentShirtIds().Count,
                OutfitCategoryManager.Category.Pants => GetCurrentPantsIds().Count,
                OutfitCategoryManager.Category.Hats => GetCurrentHatIds().Count,
                _ => 0
            };
        }

        public List<string> GetCurrentShirtIds() => filterManager.GetFilteredAndSearchedShirtIds(
            categoryManager.ShirtIds,
            state.GetModFilter(OutfitCategoryManager.Category.Shirts),
            state.GetSearchText(OutfitCategoryManager.Category.Shirts));

        public List<string> GetCurrentPantsIds() => filterManager.GetFilteredAndSearchedPantsIds(
            categoryManager.PantsIds,
            state.GetModFilter(OutfitCategoryManager.Category.Pants),
            state.GetSearchText(OutfitCategoryManager.Category.Pants));

        public List<string> GetCurrentHatIds() => filterManager.GetFilteredAndSearchedHatIds(
            categoryManager.HatIds,
            state.GetModFilter(OutfitCategoryManager.Category.Hats),
            state.GetSearchText(OutfitCategoryManager.Category.Hats));

        public List<(OutfitCategoryManager.Category ItemCategory, string ItemId)> GetCurrentAllItems()
        {
            var result = new List<(OutfitCategoryManager.Category, string)>();
            string? modFilter = state.GetModFilter(OutfitCategoryManager.Category.All);
            string? searchText = state.GetSearchText(OutfitCategoryManager.Category.All);

            var shirts = filterManager.GetFilteredAndSearchedShirtIds(categoryManager.ShirtIds, modFilter, searchText);
            var pants = filterManager.GetFilteredAndSearchedPantsIds(categoryManager.PantsIds, modFilter, searchText);
            var hats = filterManager.GetFilteredAndSearchedHatIds(categoryManager.HatIds, modFilter, searchText);

            foreach (var id in shirts)
                result.Add((OutfitCategoryManager.Category.Shirts, id));
            foreach (var id in pants)
                result.Add((OutfitCategoryManager.Category.Pants, id));
            foreach (var id in hats)
                result.Add((OutfitCategoryManager.Category.Hats, id));

            return result;
        }
    }
}
