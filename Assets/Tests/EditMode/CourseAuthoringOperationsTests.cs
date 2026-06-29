using DiskGolf.CourseEditor;
using DiskGolf.CourseEditor.Authoring;
using NUnit.Framework;
using UnityEngine;

namespace DiskGolf.Tests.EditMode
{
    public sealed class CourseAuthoringOperationsTests
    {
        [Test]
        public void PaintTile_SetsSurfaceType()
        {
            var data = new HoleData();
            var state = new CourseAuthoringState { BrushType = SurfaceTileType.Fairway };

            Assert.IsTrue(CourseAuthoringOperations.TryPaintTile(data, state, 3, 4));
            Assert.IsTrue(data.TryGetTile(3, 4, out var t) && t == SurfaceTileType.Fairway);
        }

        [Test]
        public void EraseTile_RemovesSurfaceTile()
        {
            var data = new HoleData();
            data.SetTile(2, 3, SurfaceTileType.Rough);
            var state = new CourseAuthoringState();

            Assert.IsTrue(CourseAuthoringOperations.TryEraseTile(data, state, 2, 3));
            Assert.IsFalse(data.TryGetTile(2, 3, out _));
        }

        [Test]
        public void PlaceTee_SetsHoleTeeAtTileCenter()
        {
            var data = new HoleData { TileSize = 2f, Origin = Vector2.zero };
            var state = new CourseAuthoringState();

            Assert.IsTrue(CourseAuthoringOperations.TryPlaceTee(data, state, 1, 2));
            Assert.AreEqual(new Vector2(3f, 5f), data.Hole.Tee);
        }

        [Test]
        public void PaintHazardTile_SetsHazardType()
        {
            var data = new HoleData();
            var state = new CourseAuthoringState { HazardBrushType = HazardType.Water };

            Assert.IsTrue(CourseAuthoringOperations.TryPaintHazardTile(data, state, 2, 3));
            Assert.IsTrue(data.TryGetHazardTile(2, 3, out var type) && type == HazardType.Water);
        }

        [Test]
        public void AppendHazardVertex_AddsCornerToDraft()
        {
            var data = new HoleData();
            var state = new CourseAuthoringState();

            Assert.IsTrue(CourseAuthoringOperations.TryAppendHazardVertex(data, state, 4, 5));
            Assert.AreEqual(1, state.HazardDraftVertices.Count);
            Assert.AreEqual(new Vector2Int(4, 5), state.HazardDraftVertices[0]);
        }
    }
}
