#if UNITY_EDITOR
using DiskGolf.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiskGolf.EditorTools
{
    public static class SceneHierarchyOrganizer
    {
        [MenuItem("Disk Golf/Organize Scene Hierarchy")]
        public static void OrganizeActiveScene()
        {
            SceneHierarchy.Organize();
            MarkDirty();
            Debug.Log("[Disk Golf] Scene hierarchy organized.");
        }

        [MenuItem("Disk Golf/Clean Up Scene Content")]
        public static void CleanUpActiveScene()
        {
            SceneContentCleanup.Apply();
            MarkDirty();
            Debug.Log("[Disk Golf] Removed unused cameras, legacy HUD, flattened HudRoot, merged CameraDirector onto GameManager.");
        }

        [MenuItem("Disk Golf/Organize And Clean Up Scene")]
        public static void OrganizeAndCleanUpActiveScene()
        {
            SceneHierarchy.Organize();
            SceneContentCleanup.Apply();
            MarkDirty();
            Debug.Log("[Disk Golf] Scene organized and cleaned up.");
        }

        static void MarkDirty() =>
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
}
#endif
