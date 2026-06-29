#if UNITY_EDITOR
using DiskGolf.Gameplay;
using DiskGolf.UI.CourseEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.EditorTools.CourseEditor
{
    /// <summary>Rebuilds the Course Editor scene for runtime player editing (HUD shell + authoring systems).</summary>
    public static class PlayerCourseEditorSceneBuilder
    {
        const string ScenePath = ProjectArtPaths.Scenes.CourseEditor;

        [MenuItem("Disk Golf/Course/Rebuild Player Course Editor Scene")]
        public static void RebuildFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild Player Course Editor Scene",
                    "Replace the Course Editor scene with a runtime player editing rig. Continue?",
                    "Rebuild",
                    "Cancel"))
                return;

            Rebuild();
        }

        public static void Rebuild()
        {
            var ctx = PrototypeFlat3SceneBuilder.BuildCourseEditorSceneCore();
            ApplyPlayerRuntimeSetup(ctx);
            PrototypeFlat3SceneBuilder.FinishCourseEditorScene(
                ctx,
                ScenePath,
                $"[Disk Golf] Saved {ScenePath} — runtime player course editor.");
        }

        static void ApplyPlayerRuntimeSetup(PrototypeFlat3SceneBuilder.CourseEditorSceneContext ctx)
        {
            var editorHudCanvas = CreateEditorHudCanvas();

            var systems = new GameObject("CourseEditorSystems");
            systems.AddComponent<CourseEditorSceneLoader>();
            var input = systems.AddComponent<CourseEditorInputController>();
            var hud = systems.AddComponent<CourseEditorHudController>();
            var cameraController = systems.AddComponent<CourseEditorCameraController>();

            AssignSerialized(hud, "editorHudRoot", editorHudCanvas);
            if (ctx.HudCanvas != null)
                AssignSerialized(hud, "gameplayHudRoot", ctx.HudCanvas.gameObject);
            AssignSerialized(hud, "inputController", input);
            AssignSerialized(hud, "cameraController", cameraController);

            DisableGameplayUntilPlaytest(ctx);
        }

        static GameObject CreateEditorHudCanvas()
        {
            var holder = new GameObject("EditorHudCanvas");
            var canvas = holder.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = holder.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            holder.AddComponent<GraphicRaycaster>();

            var rt = holder.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            return holder;
        }

        static void AssignSerialized(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
                return;

            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void DisableGameplayUntilPlaytest(PrototypeFlat3SceneBuilder.CourseEditorSceneContext ctx)
        {
            if (ctx.HudCanvas != null)
                ctx.HudCanvas.gameObject.SetActive(false);

            if (ctx.ThrowInput != null)
                ctx.ThrowInput.enabled = false;

            if (ctx.HudController != null)
                ctx.HudController.enabled = false;

            if (ctx.ThrowController != null)
                ctx.ThrowController.enabled = false;
        }
    }
}
#endif
