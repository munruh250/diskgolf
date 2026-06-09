using DiskGolf.CourseEditor;
using NUnit.Framework;
using UnityEngine;

namespace DiskGolf.Tests.EditMode
{
    public sealed class CourseValidatorTests
    {
        [Test]
        public void Validate_MissingBasket_IsError()
        {
            var data = new HoleData();
            data.SetTile(0, 0, SurfaceTileType.Fairway);
            data.Hole.Tee = new Vector2(0f, 0f);
            data.Hole.Basket = Vector2.zero;

            var result = CourseValidator.Validate(data);

            Assert.IsFalse(result.CanPlaytest);
            Assert.IsTrue(result.HasCode("E002") || result.HasCode("E001"));
        }

        [Test]
        public void ValidPar3_HasNoErrors()
        {
            var data = BuildSimplePar3();
            var result = CourseValidator.Validate(data);
            Assert.IsTrue(result.CanPlaytest);
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
