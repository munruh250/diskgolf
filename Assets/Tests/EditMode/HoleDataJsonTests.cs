using DiskGolf.CourseEditor;
using NUnit.Framework;
using UnityEngine;

namespace DiskGolf.Tests.EditMode
{
    public sealed class HoleDataJsonTests
    {
        [Test]
        public void RoundTrip_PreservesTilesAndHoleMeta()
        {
            var original = new HoleData
            {
                Id = "test_hole",
                Name = "Test",
                TileSize = 2f,
                Origin = new Vector2(0f, 0f),
                ThemeId = "temperate"
            };
            original.SetTile(5, 10, SurfaceTileType.Fairway);
            original.SetTile(6, 10, SurfaceTileType.Green);
            original.Hole.Tee = new Vector2(10f, 20f);
            original.Hole.Basket = new Vector2(70f, 200f);
            original.Hole.Par = 3;

            string json = HoleDataJson.ToJson(original);
            var restored = HoleDataJson.FromJson(json);

            Assert.AreEqual(original.Id, restored.Id);
            Assert.AreEqual(2, restored.SurfaceTiles.Count);
            Assert.IsTrue(restored.TryGetTile(5, 10, out var fairway));
            Assert.AreEqual(SurfaceTileType.Fairway, fairway);
            Assert.AreEqual(original.Hole.Basket, restored.Hole.Basket);
            Assert.AreEqual(3, restored.Hole.Par);
        }
    }
}
