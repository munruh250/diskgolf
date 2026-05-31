#if UNITY_EDITOR
using DiskGolf.Gameplay;
using UnityEditor;
using UnityEngine;

namespace DiskGolf.EditorTools
{
    public static class SceneHierarchyOrganizer
    {
        [MenuItem("Disk Golf/Organize Scene Hierarchy")]
        public static void OrganizeActiveScene()
        {
            SceneHierarchy.Organize();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Disk Golf] Scene hierarchy organized into DiscMechanics, GameplayHUD, CourseElements, Camera, PlayerThrower.");
        }
    }
}
#endif
