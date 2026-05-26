using DiskGolf.Disc;
using NUnit.Framework;

namespace DiskGolf.Tests
{
    public class DiscSelectionTests
    {
        [TestCase(30f, DiscCategory.Putter)]
        [TestCase(90f, DiscCategory.Putter)]
        [TestCase(120f, DiscCategory.Mid)]
        [TestCase(250f, DiscCategory.Mid)]
        [TestCase(280f, DiscCategory.Fairway)]
        [TestCase(370f, DiscCategory.Fairway)]
        [TestCase(400f, DiscCategory.Distance)]
        public void RecommendCategory_MatchesDistance(float distanceFt, DiscCategory expected) =>
            Assert.That(DiscSelection.RecommendCategory(distanceFt), Is.EqualTo(expected));
    }
}
