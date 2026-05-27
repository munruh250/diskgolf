using DiskGolf.Core;
using NUnit.Framework;

namespace DiskGolf.Tests.EditMode
{
    public sealed class HoleScoreTests
    {
        [TestCase(1, 3, "HOLE IN ONE")]
        [TestCase(2, 3, "BIRDIE")]
        [TestCase(3, 3, "PAR")]
        [TestCase(4, 3, "BOGEY")]
        [TestCase(5, 3, "DOUBLE BOGEY")]
        public void ResultName_UsesRegularGolfTerms(int strokes, int par, string expected)
        {
            Assert.AreEqual(expected, HoleScore.ResultName(strokes, par));
        }

        [Test]
        public void InProgressLine_ShowsParAndThrowCount()
        {
            Assert.AreEqual("PAR 3  ·  THROW 2", HoleScore.InProgressLine(2, 3));
        }

        [TestCase(2, 3, "-1")]
        [TestCase(3, 3, "E")]
        [TestCase(4, 3, "+1")]
        public void VsParToken_MatchesGolfNotation(int strokes, int par, string expected)
        {
            Assert.AreEqual(expected, HoleScore.VsParToken(strokes, par));
        }
    }
}
