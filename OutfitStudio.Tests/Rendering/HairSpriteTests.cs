using Microsoft.Xna.Framework;
using OutfitStudio.Tests.Helpers;
using Xunit;

namespace OutfitStudio.Tests.Rendering
{
    public class HairSpriteTests
    {
        // ── Structural: hair sprite cache ──

        [Fact]
        // Expected: OutfitItemRenderer has a hairSpriteCache field for per-session caching
        public void Renderer_HasHairSpriteCache()
        {
            string source = SourceScanner.ReadSourceFile("Rendering/OutfitItemRenderer.cs");
            Assert.Contains("hairSpriteCache", source);
        }

        [Fact]
        // Expected: ClearCache clears both item cache and hair sprite cache
        public void ClearCache_ClearsHairSpriteCache()
        {
            string source = SourceScanner.ReadSourceFile("Rendering/OutfitItemRenderer.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void ClearCache");
            Assert.Contains("hairSpriteCache.Clear()", body);
        }

        [Fact]
        // Expected: Grid drawing uses cached hair sprite info (DrawHairSpriteCached), not static DrawHairSprite
        public void DrawItemSprite_UsesCache_ForHairCategory()
        {
            string source = SourceScanner.ReadSourceFile("Rendering/OutfitItemRenderer.cs");
            string body = SourceScanner.ExtractMethodBody(source, "void DrawItemSprite");
            Assert.Contains("DrawHairSpriteCached", body);
            Assert.DoesNotContain("DrawHairSprite(", body);
        }

        // ── Pure calculation tests ──

        [Fact]
        // Expected: Hair ID 0 maps to top-left of texture (0,0)
        public void VanillaSourceRect_Id0_TopLeft()
        {
            var rect = OutfitItemRenderer.CalculateVanillaHairSourceRect(0, 128);
            Assert.Equal(new Rectangle(0, 0, 16, 15), rect);
        }

        [Fact]
        // Expected: Hair ID 7 (last in first row of 128-wide texture) is at x=112
        public void VanillaSourceRect_LastInRow_CorrectX()
        {
            // 128 / 16 = 8 tiles per row; ID 7 = last tile in row 0
            var rect = OutfitItemRenderer.CalculateVanillaHairSourceRect(7, 128);
            Assert.Equal(new Rectangle(112, 0, 16, 15), rect);
        }

        [Fact]
        // Expected: Hair ID 8 wraps to second row (y=96) in a 128-wide texture
        public void VanillaSourceRect_WrapsToNextRow()
        {
            // ID 8 * 16 = 128, wraps: x = 128 % 128 = 0, y = 128 / 128 * 96 = 96
            var rect = OutfitItemRenderer.CalculateVanillaHairSourceRect(8, 128);
            Assert.Equal(new Rectangle(0, 96, 16, 15), rect);
        }

        [Fact]
        // Expected: Mid-row ID calculates correct x and y offsets
        public void VanillaSourceRect_MidRow_CorrectPosition()
        {
            // ID 10, width 128: pixel offset = 160, x = 160 % 128 = 32, y = 160 / 128 * 96 = 96
            var rect = OutfitItemRenderer.CalculateVanillaHairSourceRect(10, 128);
            Assert.Equal(new Rectangle(32, 96, 16, 15), rect);
        }

        [Fact]
        // Expected: Source rect always has size 16x15 regardless of input
        public void VanillaSourceRect_AlwaysCorrectSize()
        {
            var rect = OutfitItemRenderer.CalculateVanillaHairSourceRect(55, 128);
            Assert.Equal(16, rect.Width);
            Assert.Equal(15, rect.Height);
        }

        [Fact]
        // Expected: Metadata tileY=0 produces Y=0 (matches vanilla FarmerRenderer using tileY*16)
        public void MetadataSourceRect_TileY0_YIsZero()
        {
            var rect = OutfitItemRenderer.CalculateMetadataHairSourceRect(3, 0);
            Assert.Equal(new Rectangle(48, 0, 16, 15), rect);
        }

        [Fact]
        // Expected: Metadata tileY>0 uses tileY*16 (not *96) to match vanilla game rendering
        public void MetadataSourceRect_TileY1_YIs16()
        {
            var rect = OutfitItemRenderer.CalculateMetadataHairSourceRect(0, 1);
            Assert.Equal(new Rectangle(0, 16, 16, 15), rect);
        }

        [Fact]
        // Expected: Metadata source rect with larger tileY scales correctly at 16px per tile
        public void MetadataSourceRect_LargerTileY_CorrectPosition()
        {
            var rect = OutfitItemRenderer.CalculateMetadataHairSourceRect(2, 5);
            Assert.Equal(new Rectangle(32, 80, 16, 15), rect);
        }
    }
}
