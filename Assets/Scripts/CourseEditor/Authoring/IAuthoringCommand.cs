namespace DiskGolf.CourseEditor.Authoring
{
    public interface IAuthoringCommand
    {
        void Execute();
        void Undo();
    }
}
