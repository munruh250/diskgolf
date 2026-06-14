using DiskGolf.CourseEditor;
using NUnit.Framework;
using UnityEngine;

namespace DiskGolf.Tests.EditMode
{
    public sealed class ElevationGridTests
    {
        [Test]
        public void EnsureSize_CreatesZeroFilledGrid()
        {
            var grid = new ElevationGrid();
            grid.EnsureSize(3, 4);

            Assert.AreEqual(3, grid.Width);
            Assert.AreEqual(4, grid.Height);
            Assert.AreEqual(12, grid.Heights.Length);
            Assert.AreEqual(0f, grid.Get(2, 3));
        }

        [Test]
        public void EnsureSize_PreservesOverlappingHeightsWhenGrowing()
        {
            var grid = new ElevationGrid(2, 2);
            grid.Set(0, 0, 1f);
            grid.Set(1, 1, 2f);

            grid.EnsureSize(3, 3);

            Assert.AreEqual(1f, grid.Get(0, 0));
            Assert.AreEqual(2f, grid.Get(1, 1));
            Assert.AreEqual(0f, grid.Get(2, 2));
        }

        [Test]
        public void SampleBilinear_ReturnsCornerHeights()
        {
            var grid = new ElevationGrid(2, 2);
            grid.Set(0, 0, 0f);
            grid.Set(1, 0, 2f);
            grid.Set(0, 1, 0f);
            grid.Set(1, 1, 2f);

            Assert.AreEqual(0f, grid.SampleBilinear(0f, 0f), 0.001f);
            Assert.AreEqual(2f, grid.SampleBilinear(1f, 1f), 0.001f);
        }

        [Test]
        public void SampleBilinear_InterpolatesCenter()
        {
            var grid = new ElevationGrid(2, 2);
            grid.Set(0, 0, 0f);
            grid.Set(1, 0, 2f);
            grid.Set(0, 1, 0f);
            grid.Set(1, 1, 2f);

            Assert.AreEqual(1f, grid.SampleBilinear(0.5f, 0.5f), 0.001f);
        }

        [Test]
        public void HoleData_ElevationAndHazards_DefaultEmpty()
        {
            var data = new HoleData();

            Assert.IsNull(data.Elevation);
            Assert.IsNotNull(data.Hazards);
            Assert.AreEqual(0, data.Hazards.Count);
        }

        [Test]
        public void HazardPolygon_StoresVertices()
        {
            var hazard = new HazardPolygon("water_01", HazardType.Water, new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(4, 0),
                new Vector2Int(4, 2)
            });

            Assert.AreEqual(HazardType.Water, hazard.Type);
            Assert.AreEqual(3, hazard.Vertices.Count);
        }

        [Test]
        public void GridSizeForHole_AddsPaddingBeyondPaintedBounds()
        {
            var data = new HoleData();
            data.SetTile(0, 0, SurfaceTileType.Fairway);
            data.SetTile(5, 10, SurfaceTileType.Fairway);

            var size = HeightGridSampler.GridSizeForHole(data);

            Assert.AreEqual(7, size.x);
            Assert.AreEqual(12, size.y);
        }

        [Test]
        public void SampleWorldY_MapsOriginAndTileSizeToGridCorners()
        {
            var data = new HoleData
            {
                Origin = new Vector2(10f, 20f),
                TileSize = 2f
            };
            data.SetTile(0, 0, SurfaceTileType.Fairway);
            data.Elevation = new ElevationGrid(2, 2);
            data.Elevation.Set(0, 0, 1f);
            data.Elevation.Set(1, 0, 3f);
            data.Elevation.Set(0, 1, 0f);
            data.Elevation.Set(1, 1, 2f);

            Assert.AreEqual(1f, HeightGridSampler.SampleWorldY(data, 10f, 20f), 0.001f);
            Assert.AreEqual(3f, HeightGridSampler.SampleWorldY(data, 12f, 20f), 0.001f);
            Assert.AreEqual(2f, HeightGridSampler.SampleWorldY(data, 11f, 20f), 0.001f);
        }

        [Test]
        public void SampleWorldY_ReturnsZeroWhenElevationMissing()
        {
            var data = new HoleData { Origin = Vector2.zero, TileSize = 2f };
            data.SetTile(0, 0, SurfaceTileType.Fairway);

            Assert.AreEqual(0f, HeightGridSampler.SampleWorldY(data, 0f, 0f));
        }

        [Test]
        public void TileCornerHeights_ReturnsCornersInMeshOrder()
        {
            var data = new HoleData();
            data.SetTile(1, 2, SurfaceTileType.Fairway);
            data.Elevation = new ElevationGrid(4, 5);
            data.Elevation.Set(1, 2, 1f);
            data.Elevation.Set(2, 2, 2f);
            data.Elevation.Set(1, 3, 3f);
            data.Elevation.Set(2, 3, 4f);

            var corners = HeightGridSampler.TileCornerHeights(data, 1, 2);

            Assert.AreEqual(4, corners.Length);
            Assert.AreEqual(1f, corners[0], 0.001f);
            Assert.AreEqual(3f, corners[1], 0.001f);
            Assert.AreEqual(4f, corners[2], 0.001f);
            Assert.AreEqual(2f, corners[3], 0.001f);
        }
    }
}
