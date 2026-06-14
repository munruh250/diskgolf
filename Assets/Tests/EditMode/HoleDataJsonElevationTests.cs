using DiskGolf.CourseEditor;
using NUnit.Framework;
using UnityEngine;

namespace DiskGolf.Tests.EditMode
{
    public sealed class HoleDataJsonElevationTests
    {
        [Test]
        public void RoundTrip_PreservesElevationAndHazards()
        {
            var original = new HoleData { Id = "elev_hole", Name = "Elevated" };
            original.SetTile(0, 0, SurfaceTileType.Fairway);

            original.Elevation = new ElevationGrid(3, 3);
            original.Elevation.Set(0, 0, 0f);
            original.Elevation.Set(1, 0, 1f);
            original.Elevation.Set(2, 2, 2.5f);

            original.Hazards.Add(new HazardPolygon("water_1", HazardType.Water, new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(4, 0),
                new Vector2Int(4, 2)
            }));

            string json = HoleDataJson.ToJson(original);
            var restored = HoleDataJson.FromJson(json);

            Assert.IsNotNull(restored.Elevation);
            Assert.AreEqual(3, restored.Elevation.Width);
            Assert.AreEqual(3, restored.Elevation.Height);
            Assert.AreEqual(1f, restored.Elevation.Get(1, 0), 0.001f);
            Assert.AreEqual(2.5f, restored.Elevation.Get(2, 2), 0.001f);

            Assert.AreEqual(1, restored.Hazards.Count);
            Assert.AreEqual("water_1", restored.Hazards[0].Id);
            Assert.AreEqual(HazardType.Water, restored.Hazards[0].Type);
            Assert.AreEqual(3, restored.Hazards[0].Vertices.Count);
            Assert.AreEqual(new Vector2Int(4, 2), restored.Hazards[0].Vertices[2]);
        }

        [Test]
        public void FromJson_P0HoleWithoutElevationOrHazards_LoadsFlat()
        {
            const string json = @"{
                ""schemaVersion"": 1,
                ""id"": ""legacy_hole"",
                ""name"": ""Legacy"",
                ""tileSize"": 2.0,
                ""origin"": [0.0, 0.0],
                ""themeId"": ""temperate"",
                ""surfaceTiles"": [{ ""x"": 0, ""y"": 0, ""type"": ""fairway"" }],
                ""hole"": { ""tee"": [0.0, 0.0], ""basket"": [10.0, 10.0], ""par"": 3 }
            }";

            var data = HoleDataJson.FromJson(json);

            Assert.AreEqual("legacy_hole", data.Id);
            Assert.IsNull(data.Elevation);
            Assert.IsNotNull(data.Hazards);
            Assert.AreEqual(0, data.Hazards.Count);
            Assert.IsTrue(data.TryGetTile(0, 0, out var type));
            Assert.AreEqual(SurfaceTileType.Fairway, type);
        }

        [Test]
        public void RoundTrip_ObHazardType()
        {
            var original = new HoleData();
            original.Hazards.Add(new HazardPolygon("ob_left", HazardType.OB, new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(0, 10)
            }));

            var restored = HoleDataJson.FromJson(HoleDataJson.ToJson(original));

            Assert.AreEqual(HazardType.OB, restored.Hazards[0].Type);
        }
    }
}
