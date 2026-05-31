namespace DiskGolf.Gameplay
{
    /// <summary>Editor-only hooks registered from the DiskGolf.Editor assembly.</summary>
    public interface ISceneEditorHooks
    {
        void RunContentCleanup();
    }

    public static class SceneEditorHooks
    {
        public static ISceneEditorHooks Instance { get; set; }
    }
}
