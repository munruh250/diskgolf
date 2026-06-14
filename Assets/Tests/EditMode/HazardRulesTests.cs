using DiskGolf.CourseEditor;
using DiskGolf.Flight;
using NUnit.Framework;
using UnityEngine;

namespace DiskGolf.Tests.EditMode
{
    public sealed class HazardRulesTests
    {
        [Test]
        public void ResolveDrop_Water_ReturnsNearestPlayableTile()
        {
            var data = BuildHoleWithWaterHazard();
            var discPos = new Vector3(14f, 0f, 10f);

            var drop = HazardRules.ResolveDrop(data, LieType.Water, discPos);

            Assert.AreEqual(11f, drop.x, 0.001f);
            Assert.AreEqual(11f, drop.z, 0.001f);
        }

        [Test]
        public void ResolveDrop_OB_ReturnsTee()
        {
            var data = BuildHoleWithWaterHazard();
            data.Hole.Tee = new Vector2(4f, 4f);

            var drop = HazardRules.ResolveDrop(data, LieType.OB, new Vector3(14f, 0f, 10f));

            Assert.AreEqual(4f, drop.x, 0.001f);
            Assert.AreEqual(4f, drop.z, 0.001f);
        }

        [Test]
        public void TryClassifyHazard_DetectsWaterInsidePolygon()
        {
            var data = BuildHoleWithWaterHazard();

            Assert.IsTrue(HazardRules.TryClassifyHazard(data, new Vector3(14f, 0f, 10f), out var lie));
            Assert.AreEqual(LieType.Water, lie);
        }

        [Test]
        public void TryClassifyHazard_OutsidePolygon_ReturnsFalse()
        {
            var data = BuildHoleWithWaterHazard();

            Assert.IsFalse(HazardRules.TryClassifyHazard(data, new Vector3(2f, 0f, 2f), out _));
        }

        static HoleData BuildHoleWithWaterHazard()
        {
            var data = new HoleData { TileSize = 2f, Origin = Vector2.zero };
            data.SetTile(5, 5, SurfaceTileType.Fairway);
            data.Hole.Tee = new Vector2(10f, 10f);
            data.Hazards.Add(new HazardPolygon("water_1", HazardType.Water, new[]
            {
                new Vector2Int(6, 4),
                new Vector2Int(8, 4),
                new Vector2Int(8, 6),
                new Vector2Int(6, 6)
            }));
            return data;
        }
    }
}
