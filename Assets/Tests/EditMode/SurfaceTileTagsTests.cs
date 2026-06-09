using DiskGolf.CourseEditor;
using NUnit.Framework;

namespace DiskGolf.Tests.EditMode
{
    public sealed class SurfaceTileTagsTests
    {
        [TestCase(SurfaceTileType.Fairway, "Fairway")]
        [TestCase(SurfaceTileType.Rough, "Rough")]
        [TestCase(SurfaceTileType.Green, "Green")]
        [TestCase(SurfaceTileType.Tee, "Tee")]
        public void ToUnityTag_ReturnsExpectedTag(SurfaceTileType type, string expected)
        {
            Assert.AreEqual(expected, SurfaceTileTags.ToUnityTag(type));
        }
    }
}
