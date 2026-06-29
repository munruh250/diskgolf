using DiskGolf.CourseEditor;
using DiskGolf.CourseEditor.Authoring;
using NUnit.Framework;

namespace DiskGolf.Tests.EditMode
{
    public sealed class CourseAuthoringCommandStackTests
    {
        [Test]
        public void Undo_RestoresPreviousTile()
        {
            var data = new HoleData();
            var stack = new CourseAuthoringCommandStack();
            stack.Execute(new PaintTileCommand(data, 1, 1, SurfaceTileType.Fairway));
            Assert.IsTrue(data.TryGetTile(1, 1, out var t) && t == SurfaceTileType.Fairway);
            stack.Undo();
            Assert.IsFalse(data.TryGetTile(1, 1, out _));
        }

        [Test]
        public void Redo_ReappliesAfterUndo()
        {
            var data = new HoleData();
            var stack = new CourseAuthoringCommandStack();
            stack.Execute(new PaintTileCommand(data, 2, 3, SurfaceTileType.Fairway));
            stack.Undo();
            Assert.IsFalse(data.TryGetTile(2, 3, out _));
            stack.Redo();
            Assert.IsTrue(data.TryGetTile(2, 3, out var t) && t == SurfaceTileType.Fairway);
        }

        [Test]
        public void StackLimit_EvictsOldest()
        {
            var data = new HoleData();
            var stack = new CourseAuthoringCommandStack();

            stack.Execute(new PaintTileCommand(data, 0, 0, SurfaceTileType.Fairway));

            for (int i = 1; i < CourseAuthoringCommandStack.MaxCommands; i++)
            {
                stack.Execute(new PaintTileCommand(data, i, 0, SurfaceTileType.Rough));
            }

            stack.Execute(new PaintTileCommand(data, CourseAuthoringCommandStack.MaxCommands, 0, SurfaceTileType.Green));

            for (int i = 0; i < CourseAuthoringCommandStack.MaxCommands; i++)
            {
                stack.Undo();
            }

            Assert.IsTrue(data.TryGetTile(0, 0, out var t) && t == SurfaceTileType.Fairway);
            Assert.IsFalse(stack.CanUndo);
        }
    }
}
