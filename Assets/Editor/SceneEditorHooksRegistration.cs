#if UNITY_EDITOR
using DiskGolf.Gameplay;
using UnityEditor;

namespace DiskGolf.EditorTools
{
    [InitializeOnLoad]
    static class SceneEditorHooksRegistration
    {
        static SceneEditorHooksRegistration() =>
            SceneEditorHooks.Instance = new Hooks();

        sealed class Hooks : ISceneEditorHooks
        {
            public void RunContentCleanup() => SceneContentCleanup.Apply();
        }
    }
}
#endif
