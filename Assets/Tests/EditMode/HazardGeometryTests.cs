using System.Collections.Generic;
using DiskGolf.CourseEditor;
using NUnit.Framework;
using UnityEngine;

namespace DiskGolf.Tests.EditMode
{
    public sealed class HazardGeometryTests
    {
        readonly List<Vector2Int> _scratch = new();

        [Test]
        public void CollectTilesInside_FindsSlopedWaterFootprint()
        {
            var data = BuildSlopedHole();
            var hazard = new HazardPolygon("water_green", HazardType.Water, new[]
            {
                new Vector2Int(7, 116),
                new Vector2Int(11, 116),
                new Vector2Int(11, 117),
                new Vector2Int(7, 117),
            });

            HazardGeometry.CollectTilesInside(data, hazard, _scratch);

            Assert.That(_scratch, Has.Count.EqualTo(4));
            Assert.That(_scratch, Does.Contain(new Vector2Int(7, 116)));
            Assert.That(_scratch, Does.Contain(new Vector2Int(10, 116)));
        }

        [Test]
        public void IsTileInsideHazard_WaterTile_ReturnsTrue()
        {
            var data = BuildSlopedHole();
            data.SetTile(9, 116, SurfaceTileType.Fairway);

            Assert.IsTrue(HazardGeometry.IsTileInsideHazard(data, 9, 116, HazardType.Water));
            Assert.IsFalse(HazardGeometry.IsTileInsideHazard(data, 9, 115, HazardType.Water));
        }

        static HoleData BuildSlopedHole()
        {
            var data = new HoleData
            {
                TileSize = 2f,
                Origin = Vector2.zero,
                Elevation = new ElevationGrid(4, 4),
            };

            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    data.Elevation.Set(x, y, y);
                }
            }

            data.Hazards.Add(new HazardPolygon("water_green", HazardType.Water, new[]
            {
                new Vector2Int(7, 116),
                new Vector2Int(11, 116),
                new Vector2Int(11, 117),
                new Vector2Int(7, 117),
            }));

            return data;
        }
    }
}
