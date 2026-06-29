using System.Collections.Generic;

namespace DiskGolf.CourseEditor.Authoring
{
    public sealed class CourseAuthoringCommandStack
    {
        public const int MaxCommands = 50;

        readonly List<IAuthoringCommand> _undo = new();
        readonly List<IAuthoringCommand> _redo = new();

        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;

        public void Execute(IAuthoringCommand command)
        {
            command.Execute();
            _undo.Add(command);
            _redo.Clear();
            TrimUndoToMax();
        }

        public void Undo()
        {
            if (!CanUndo)
            {
                return;
            }

            var command = _undo[_undo.Count - 1];
            _undo.RemoveAt(_undo.Count - 1);
            command.Undo();
            _redo.Add(command);
        }

        public void Redo()
        {
            if (!CanRedo)
            {
                return;
            }

            var command = _redo[_redo.Count - 1];
            _redo.RemoveAt(_redo.Count - 1);
            command.Execute();
            _undo.Add(command);
            TrimUndoToMax();
        }

        void TrimUndoToMax()
        {
            while (_undo.Count > MaxCommands)
            {
                _undo.RemoveAt(0);
            }
        }
    }
}
