#if UNITY_EDITOR
using DiskGolf.Disc;
using DiskGolf.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DiskGolf.EditorTools.CourseEditor
{
    /// <summary>Thin entry point for rebuilding the Course Editor playtest scene.</summary>
    public static class CourseEditorSceneBuilder
    {
        [MenuItem("Disk Golf/Course/Fix Course Editor HUD")]
        public static void FixHud()
        {
            var hud = GameObject.Find("GameplayHUD")?.GetComponent<RectTransform>();
            if (hud == null)
            {
                Debug.LogError("[Disk Golf] GameplayHUD not found. Run Rebuild Course Editor Scene first.");
                return;
            }

            hud.localScale = Vector3.one;
            EditorTools.HudSceneAuthoring.BakeMissingSceneWidgets();
            HudLayout.ForceApplyCanonicalLayout();

            var bag = Object.FindFirstObjectByType<DiscBag>();
            if (bag != null)
                DiscBagBootstrap.EnsurePopulated(bag);

            hud.localScale = Vector3.one;
            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
            Debug.Log("[Disk Golf] Course Editor HUD upgraded with canonical layout. Save the scene.");
        }

        public static void Rebuild() => EditorTools.PrototypeFlat3SceneBuilder.BuildCourseEditorScene();
    }
}
#endif
