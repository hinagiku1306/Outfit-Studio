using Xunit;

namespace OutfitStudio.Tests.Utilities
{
    public class TranslationCacheTests
    {
        [Fact]
        // Expected: ToTitleCase capitalizes the first letter of a single word
        public void ToTitleCase_SingleWord()
        {
            Assert.Equal("Hello", TranslationCache.ToTitleCase("hello"));
        }

        [Fact]
        // Expected: ToTitleCase capitalizes the first letter of each space-separated word
        public void ToTitleCase_MultipleWords()
        {
            Assert.Equal("Hello World", TranslationCache.ToTitleCase("hello world"));
        }

        [Fact]
        // Expected: ToTitleCase capitalizes after hyphens as well as spaces
        public void ToTitleCase_HyphenSeparated()
        {
            Assert.Equal("Foo-Bar", TranslationCache.ToTitleCase("foo-bar"));
        }

        [Fact]
        // Expected: ToTitleCase returns already-title-cased text unchanged
        public void ToTitleCase_AlreadyTitleCase()
        {
            Assert.Equal("Hello World", TranslationCache.ToTitleCase("Hello World"));
        }

        [Fact]
        // Expected: ToTitleCase only uppercases first char of each word, preserves rest (e.g. "hELLO" -> "HELLO")
        public void ToTitleCase_PreservesNonInitialCase()
        {
            Assert.Equal("HELLO", TranslationCache.ToTitleCase("hELLO"));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        // Expected: ToTitleCase returns null for null input and empty string for empty input
        public void ToTitleCase_NullOrEmpty(string? input, string? expected)
        {
            Assert.Equal(expected, TranslationCache.ToTitleCase(input!));
        }

        [Fact]
        // Expected: ToTitleCase capitalizes a single character
        public void ToTitleCase_SingleCharacter()
        {
            Assert.Equal("A", TranslationCache.ToTitleCase("a"));
        }

        [Fact]
        // Expected: GetTagDisplayName returns title-cased text for non-predefined tags (no translations loaded)
        public void GetTagDisplayName_NonPredefined_ReturnsTitleCase()
        {
            Assert.Equal("My Custom Tag", TranslationCache.GetTagDisplayName("my custom tag"));
        }
    }
}
