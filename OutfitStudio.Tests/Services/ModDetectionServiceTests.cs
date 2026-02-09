using Xunit;

namespace OutfitStudio.Tests.Services
{
    public class ModDetectionServiceTests
    {
        // --- IsVanillaId ---

        [Fact]
        // Expected: IsVanillaId returns true for a purely numeric ID
        public void IsVanillaId_NumericId_True()
        {
            Assert.True(ModDetectionService.IsVanillaId("123"));
        }

        [Fact]
        // Expected: IsVanillaId returns true for a simple string ID with no dots or underscores
        public void IsVanillaId_SimpleStringId_True()
        {
            Assert.True(ModDetectionService.IsVanillaId("SomeId"));
        }

        [Fact]
        // Expected: IsVanillaId returns false for a dot-separated ID (mod content)
        public void IsVanillaId_DottedId_False()
        {
            Assert.False(ModDetectionService.IsVanillaId("Mod.Item"));
        }

        [Fact]
        // Expected: IsVanillaId returns false for an underscore-separated ID (mod content)
        public void IsVanillaId_UnderscoreId_False()
        {
            Assert.False(ModDetectionService.IsVanillaId("Mod_Item"));
        }

        // --- StripVersionSuffix ---

        [Fact]
        // Expected: StripVersionSuffix removes ".v2" suffix
        public void StripVersionSuffix_RemovesV2()
        {
            Assert.Equal("ModName", ModDetectionService.StripVersionSuffix("ModName.v2"));
        }

        [Fact]
        // Expected: StripVersionSuffix removes ".ver2" suffix
        public void StripVersionSuffix_RemovesVer2()
        {
            Assert.Equal("ModName", ModDetectionService.StripVersionSuffix("ModName.ver2"));
        }

        [Fact]
        // Expected: StripVersionSuffix removes complex version suffix like "_v2.1"
        public void StripVersionSuffix_RemovesComplex()
        {
            Assert.Equal("ModName", ModDetectionService.StripVersionSuffix("ModName_v2.1"));
        }

        [Fact]
        // Expected: StripVersionSuffix returns input unchanged when no version suffix exists
        public void StripVersionSuffix_NoVersion_Unchanged()
        {
            Assert.Equal("ModName", ModDetectionService.StripVersionSuffix("ModName"));
        }

        // --- HumanizeName ---

        [Fact]
        // Expected: HumanizeName capitalizes first letter and lowercases the rest for long names
        public void HumanizeName_LongName()
        {
            Assert.Equal("Modname", ModDetectionService.HumanizeName("modname"));
        }

        [Fact]
        // Expected: HumanizeName returns short names (length <= 2) unchanged
        public void HumanizeName_ShortName_Unchanged()
        {
            Assert.Equal("AB", ModDetectionService.HumanizeName("AB"));
        }

        [Fact]
        // Expected: HumanizeName returns empty string unchanged
        public void HumanizeName_Empty_Unchanged()
        {
            Assert.Equal("", ModDetectionService.HumanizeName(""));
        }

        // --- NormalizeName ---

        [Fact]
        // Expected: NormalizeName strips version suffix then humanizes the result
        public void NormalizeName_CombinesStripAndHumanize()
        {
            Assert.Equal("Modname", ModDetectionService.NormalizeName("modname.v2"));
        }

        // --- NormalizeForComparison ---

        [Fact]
        // Expected: NormalizeForComparison removes underscores and uppercases for comparison
        public void NormalizeForComparison_RemovesUnderscores()
        {
            Assert.Equal("MODNAME", ModDetectionService.NormalizeForComparison("mod_name"));
        }

        [Fact]
        // Expected: NormalizeForComparison strips version suffix before normalizing
        public void NormalizeForComparison_StripsVersionFirst()
        {
            Assert.Equal("MOD", ModDetectionService.NormalizeForComparison("mod.v2"));
        }

        [Fact]
        // Expected: NormalizeForComparison returns empty string for empty input
        public void NormalizeForComparison_Empty_ReturnsEmpty()
        {
            Assert.Equal("", ModDetectionService.NormalizeForComparison(""));
        }

        // --- CleanModName ---

        [Fact]
        // Expected: CleanModName removes "[CP] " prefix
        public void CleanModName_RemovesCPPrefix()
        {
            Assert.Equal("My Mod", ModDetectionService.CleanModName("[CP] My Mod"));
        }

        [Fact]
        // Expected: CleanModName removes "(JA) " prefix
        public void CleanModName_RemovesJAPrefix()
        {
            Assert.Equal("Another Mod", ModDetectionService.CleanModName("(JA) Another Mod"));
        }

        [Fact]
        // Expected: CleanModName returns input unchanged when no known prefix exists
        public void CleanModName_NoPrefix_Unchanged()
        {
            Assert.Equal("Normal Name", ModDetectionService.CleanModName("Normal Name"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        // Expected: CleanModName returns null/empty/whitespace input as-is
        public void CleanModName_NullOrWhitespace_ReturnsInput(string? input)
        {
            Assert.Equal(input, ModDetectionService.CleanModName(input!));
        }

        [Fact]
        // Expected: CleanModName removes lowercase prefixes like "[cp]" (case-insensitive)
        public void CleanModName_LowercasePrefix_Removed()
        {
            Assert.Equal("My Mod", ModDetectionService.CleanModName("[cp] My Mod"));
        }

        // --- Additional StripVersionSuffix ---

        [Fact]
        // Expected: StripVersionSuffix removes "_ver2" suffix
        public void StripVersionSuffix_UnderscoreVer()
        {
            Assert.Equal("ModName", ModDetectionService.StripVersionSuffix("ModName_ver2"));
        }

        [Fact]
        // Expected: StripVersionSuffix removes multi-part version like ".v2.1.3"
        public void StripVersionSuffix_DotV_MultiPart()
        {
            Assert.Equal("ModName", ModDetectionService.StripVersionSuffix("ModName.v2.1.3"));
        }

        // --- Additional HumanizeName ---

        [Fact]
        // Expected: HumanizeName uppercases first char and lowercases the rest of an all-caps input
        public void HumanizeName_UppercaseInput_LowersRest()
        {
            Assert.Equal("Modname", ModDetectionService.HumanizeName("MODNAME"));
        }

        [Fact]
        // Expected: HumanizeName returns single-character input unchanged (length <= 2)
        public void HumanizeName_SingleChar_Unchanged()
        {
            Assert.Equal("A", ModDetectionService.HumanizeName("A"));
        }

        // --- Additional IsVanillaId ---

        [Fact]
        // Expected: IsVanillaId returns true for empty string (no dots or underscores)
        public void IsVanillaId_EmptyString_True()
        {
            Assert.True(ModDetectionService.IsVanillaId(""));
        }

        // --- Additional NormalizeName ---

        [Fact]
        // Expected: NormalizeName strips version then humanizes an all-uppercase input
        public void NormalizeName_UppercaseWithVersion()
        {
            Assert.Equal("Bigmod", ModDetectionService.NormalizeName("BIGMOD.v3"));
        }
    }
}
