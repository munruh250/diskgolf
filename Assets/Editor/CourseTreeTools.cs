#if UNITY_EDITOR
using DiskGolf.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiskGolf.EditorTools
{
    /// <summary>Editor helpers for placing foliage trees on a course layout.</summary>
    public static class CourseTreeTools
    {
        const string MenuRoot = "Disk Golf/Course/";

        [MenuItem(MenuRoot + "Place Test Tree")]
        public static void PlaceRoundTree()
        {
            PlaceTree(CourseTreeVariant.Round);
        }

        [MenuItem(MenuRoot + "Refit Tree Colliders In Scene")]
        public static void RefitTreeCollidersInScene()
        {
            int count = 0;

            foreach (var tree in Object.FindObjectsOfType<TreeObstacle>())
            {
                CourseTree.FitColliderToSprite(tree.gameObject);
                EditorUtility.SetDirty(tree.gameObject);
                count++;
            }

            if (count > 0)
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log($"[Disk Golf] Refit colliders on {count} tree(s) to match sprite bounds.");
        }

        static void PlaceTree(CourseTreeVariant variant)
        {
            var layout = CourseLayout.EnsureInScene();
            if (layout == null)
            {
                Debug.LogError("[Disk Golf] CourseElements not found. Open a gameplay scene first.");
                return;
            }

            layout.ResolveReferences();

            var treesRoot = layout.TreesRoot;
            if (treesRoot == null)
            {
                var rootGo = new GameObject(CourseLayout.TreesRootName);
                Undo.RegisterCreatedObjectUndo(rootGo, "Create trees root");
                rootGo.transform.SetParent(layout.transform, false);
                treesRoot = rootGo.transform;
            }

            var position = ResolvePlacementPosition(layout);
            var tree = CourseTree.Spawn(treesRoot, position, variant, Random.Range(-25f, 25f));
            if (tree == null)
            {
                Debug.LogError(
                    "[Disk Golf] TreeRound sprite not loaded. Run Disk Golf art import / catalog build so foliage sprites are available.");
                return;
            }

            Undo.RegisterCreatedObjectUndo(tree.gameObject, "Place test tree");
            Selection.activeTransform = tree;
            EditorSceneManager.MarkSceneDirty(tree.gameObject.scene);

            Debug.Log(
                $"[Disk Golf] Placed {variant} tree under {CourseLayout.RootName}/{CourseLayout.TreesRootName} at {position}. "
                + "Move it in Scene view, then save the scene.");
        }

        static Vector3 ResolvePlacementPosition(CourseLayout layout)
        {
            if (layout.TeePad != null && layout.Basket != null)
            {
                var forward = (layout.Basket.position - layout.TeePad.position).normalized;
                var right = Vector3.Cross(Vector3.up, forward);
                return layout.TeePad.position + forward * 70f + right * 10f;
            }

            if (SceneView.lastActiveSceneView != null)
            {
                var view = SceneView.lastActiveSceneView;
                var ray = new Ray(view.camera.transform.position, view.camera.transform.forward);
                return Physics.Raycast(ray, out var hit, 500f)
                    ? hit.point
                    : view.pivot;
            }

            return Vector3.zero;
        }
    }
}
#endif
