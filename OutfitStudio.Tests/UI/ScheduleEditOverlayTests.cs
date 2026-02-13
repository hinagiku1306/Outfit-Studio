using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace OutfitStudio.Tests.UI
{
    public class GenerateRuleNameTests
    {
        private static readonly string[] Empty = System.Array.Empty<string>();

        [Fact]
        // Expected: No triggers selected returns "Always"
        public void NoTriggers_ReturnsAlways()
        {
            var result = UIHelpers.GenerateRuleName(
                Empty, Empty, Empty, Empty, Empty, false, "Wedding Day");

            Assert.Equal("Always", result);
        }

        [Fact]
        // Expected: Single season selected returns just that season
        public void SingleSeason_ReturnsSeason()
        {
            var result = UIHelpers.GenerateRuleName(
                new[] { "Spring" }, Empty, Empty, Empty, Empty, false, "Wedding Day");

            Assert.Equal("Spring", result);
        }

        [Fact]
        // Expected: Multiple seasons returns comma-separated list
        public void MultipleSeasons_CommaSeparated()
        {
            var result = UIHelpers.GenerateRuleName(
                new[] { "Spring", "Summer" }, Empty, Empty, Empty, Empty, false, "Wedding Day");

            Assert.Equal("Spring, Summer", result);
        }

        [Fact]
        // Expected: Single weather returns weather display name
        public void SingleWeather_ReturnsDisplayName()
        {
            var result = UIHelpers.GenerateRuleName(
                Empty, new[] { "Sunny" }, Empty, Empty, Empty, false, "Wedding Day");

            Assert.Equal("Sunny", result);
        }

        [Fact]
        // Expected: All weather types selected omits weather from name
        public void AllWeather_OmittedFromName()
        {
            var result = UIHelpers.GenerateRuleName(
                Empty, new[] { "Sunny", "Rainy" }, Empty, Empty, Empty, false, "Wedding Day");

            Assert.Equal("Always", result);
        }

        [Fact]
        // Expected: All seasons selected with partial weather omits seasons
        public void AllSeasons_WithPartialWeather_OmitsSeasons()
        {
            var result = UIHelpers.GenerateRuleName(
                new[] { "Spring", "Summer", "Fall", "Winter" },
                new[] { "Rainy" }, Empty, Empty, Empty, false, "Wedding Day");

            Assert.Equal("Rainy", result);
        }

        [Fact]
        // Expected: Areas only returns area display names
        public void AreasOnly_ReturnsDisplayNames()
        {
            var result = UIHelpers.GenerateRuleName(
                Empty, Empty, new[] { "Outdoor" }, Empty, Empty, false, "Wedding Day");

            Assert.Equal("Outdoor", result);
        }

        [Fact]
        // Expected: Locations only returns location names
        public void LocationsOnly_ReturnsNames()
        {
            var result = UIHelpers.GenerateRuleName(
                Empty, Empty, Empty, new[] { "Farm", "Beach" }, Empty, false, "Wedding Day");

            Assert.Equal("Farm, Beach", result);
        }

        [Fact]
        // Expected: Festivals only returns festival display names
        public void FestivalsOnly_ReturnsDisplayNames()
        {
            var result = UIHelpers.GenerateRuleName(
                Empty, Empty, Empty, Empty, new[] { "Egg Festival" }, false, "Wedding Day");

            Assert.Equal("Egg Festival", result);
        }

        [Fact]
        // Expected: Wedding only returns wedding label
        public void WeddingOnly_ReturnsLabel()
        {
            var result = UIHelpers.GenerateRuleName(
                Empty, Empty, Empty, Empty, Empty, true, "Wedding Day");

            Assert.Equal("Wedding Day", result);
        }

        [Fact]
        // Expected: Multiple trigger categories separated by " | "
        public void MultipleCategories_PipeSeparated()
        {
            var result = UIHelpers.GenerateRuleName(
                new[] { "Spring" }, new[] { "Sunny" }, Empty, Empty, Empty, false, "Wedding Day");

            Assert.Equal("Spring | Sunny", result);
        }

        [Fact]
        // Expected: All categories present produces full pipe-separated format
        public void AllCategories_FullFormat()
        {
            var result = UIHelpers.GenerateRuleName(
                new[] { "Spring", "Summer" },
                new[] { "Rainy" },
                new[] { "Indoor" },
                new[] { "Farm" },
                new[] { "Egg Festival" },
                true,
                "Wedding Day");

            Assert.Equal("Spring, Summer | Rainy | Indoor | Farm | Egg Festival | Wedding Day", result);
        }

        [Fact]
        // Expected: Order is Seasons | Weather | Areas | Locations | Festivals | Wedding
        public void CategoryOrder_IsCorrect()
        {
            var result = UIHelpers.GenerateRuleName(
                new[] { "Winter" },
                new[] { "Sunny" },
                new[] { "Outdoor" },
                new[] { "Beach" },
                new[] { "Luau" },
                true,
                "Wedding Day");

            var parts = result.Split(" | ");
            Assert.Equal(6, parts.Length);
            Assert.Equal("Winter", parts[0]);
            Assert.Equal("Sunny", parts[1]);
            Assert.Equal("Outdoor", parts[2]);
            Assert.Equal("Beach", parts[3]);
            Assert.Equal("Luau", parts[4]);
            Assert.Equal("Wedding Day", parts[5]);
        }

        [Fact]
        // Expected: Seasons + Locations (skip middle categories) produces two-part pipe format
        public void SeasonsAndLocations_SkipsMiddle()
        {
            var result = UIHelpers.GenerateRuleName(
                new[] { "Fall" }, Empty, Empty, new[] { "Mine", "Town" }, Empty, false, "Wedding Day");

            Assert.Equal("Fall | Mine, Town", result);
        }

        [Fact]
        // Expected: Wedding label is configurable (uses whatever string is passed)
        public void WeddingLabel_UsesProvidedString()
        {
            var result = UIHelpers.GenerateRuleName(
                Empty, Empty, Empty, Empty, Empty, true, "Boda");

            Assert.Equal("Boda", result);
        }

        [Fact]
        // Expected: Single items in multiple categories still pipe-separate
        public void SingleItemPerCategory_PipeSeparated()
        {
            var result = UIHelpers.GenerateRuleName(
                new[] { "Spring" }, Empty, new[] { "Indoor" }, Empty, Empty, true, "Wedding Day");

            Assert.Equal("Spring | Indoor | Wedding Day", result);
        }
    }
}
