using DiskGolf.CourseEditor;
using NUnit.Framework;

namespace DiskGolf.Tests.EditMode
{
    public sealed class HoleDataTemplatesTests
    {
        [Test]
        public void StraightPar3_IsAbout250Yards()
        {
            var data = HoleDataTemplates.CreateStraightPar3("Test");
            float yards = data.HoleLengthYards();
            Assert.Greater(yards, 230f);
            Assert.Less(yards, 270f);
            Assert.AreEqual(3, data.Hole.Par);
        }
    }
}
