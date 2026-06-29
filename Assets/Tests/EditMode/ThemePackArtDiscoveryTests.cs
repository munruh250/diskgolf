using DiskGolf.CourseEditor;
using NUnit.Framework;

namespace DiskGolf.Tests.EditMode
{
    public sealed class ThemePackArtDiscoveryTests
    {
        [Test]
        public void ArchetypeIdFromSpriteName_UsesLegacyTreeAlias()
        {
            Assert.AreEqual("tree_round", ThemePackArtDiscovery.ArchetypeIdFromSpriteName("Tree"));
        }

        [Test]
        public void ArchetypeIdFromSpriteName_MapsNewSprites()
        {
            Assert.AreEqual("tree_round_light", ThemePackArtDiscovery.ArchetypeIdFromSpriteName("TreeRound"));
            Assert.AreEqual("tree_round_dark", ThemePackArtDiscovery.ArchetypeIdFromSpriteName("TreeRoundDark"));
        }

        [Test]
        public void SkyboxIdFromMaterialName_PreservesExistingIds()
        {
            Assert.AreEqual("sky_clear", ThemePackArtDiscovery.SkyboxIdFromMaterialName("MAT_PrototypeSkybox"));
            Assert.AreEqual("sky_overcast", ThemePackArtDiscovery.SkyboxIdFromMaterialName("MAT_Sky_Overcast"));
        }

        [Test]
        public void PascalCaseToSnakeCase_SplitsWords()
        {
            Assert.AreEqual("tree_round_dark", ThemePackArtDiscovery.PascalCaseToSnakeCase("TreeRoundDark"));
        }
    }
}
