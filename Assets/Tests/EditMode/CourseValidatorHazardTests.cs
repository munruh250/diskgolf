using DiskGolf.CourseEditor;
using NUnit.Framework;
using UnityEngine;

namespace DiskGolf.Tests.EditMode
{
    public sealed class CourseValidatorHazardTests
    {
        [Test]
        public void Validate_SelfIntersectingHazard_HasW003()
        {
            var data = BuildSimplePar3();
            data.Hazards.Add(new HazardPolygon("bad_water", HazardType.Water, new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(4, 4),
                new Vector2Int(4, 0),
                new Vector2Int(0, 4)
            }));

            var result = CourseValidator.Validate(data);

            Assert.IsTrue(result.HasCode("W003"));
        }

        [Test]
        public void Validate_ConvexHazard_NoW003()
        {
            var data = BuildSimplePar3();
            data.Hazards.Add(new HazardPolygon("water_1", HazardType.Water, new[]
            {
                new Vector2Int(4, 4),
                new Vector2Int(6, 4),
                new Vector2Int(6, 6),
                new Vector2Int(4, 6)
            }));

            var result = CourseValidator.Validate(data);

            Assert.IsFalse(result.HasCode("W003"));
        }

        static HoleData BuildSimplePar3()
        {
            var data = new HoleData();
            for (int y = 0; y < 30; y++)
                data.SetTile(5, y, SurfaceTileType.Fairway);
            data.SetTile(5, 29, SurfaceTileType.Green);
            data.Hole.Tee = new Vector2(10f, 0f);
            data.Hole.Basket = new Vector2(10f, 58f);
            data.Hole.Par = 3;
            return data;
        }
    }
}
