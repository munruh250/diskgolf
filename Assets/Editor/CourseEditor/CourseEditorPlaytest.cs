#if UNITY_EDITOR
using DiskGolf.Core;
using DiskGolf.CourseEditor;
using DiskGolf.Gameplay;
using UnityEditor;
using UnityEngine;

namespace DiskGolf.EditorTools.CourseEditor
{
    public static class CourseEditorPlaytest
    {
        const string ScratchAssetPath = ProjectArtPaths.Data.CoursesRoot + "/_EditorScratch.asset";

        public static void Run(HoleData data, ThemePack theme)
        {
            var validation = CourseValidator.Validate(data);
            if (!validation.CanPlaytest)
            {
                EditorUtility.DisplayDialog(
                    "Course Editor",
                    "Fix validation errors before playtest.",
                    "OK");
                return;
            }

            var throwController = Object.FindFirstObjectByType<ThrowController>();
            if (throwController == null)
            {
                bool rebuild = EditorUtility.DisplayDialog(
                    "Course Editor",
                    "This scene has no throw rig. Open Assets/Scenes/Prototype/CourseEditor.unity "
                    + "or run Disk Golf → Course → Rebuild Course Editor Scene.",
                    "OK",
                    "Rebuild Scene Now");

                if (rebuild)
                    CourseEditorSceneBuilder.Rebuild();

                return;
            }

            var holeSetup = Object.FindFirstObjectByType<HoleSetup>();
            if (holeSetup == null)
            {
                EditorUtility.DisplayDialog("Course Editor", "HoleSetup not found in this scene.", "OK");
                return;
            }

            var scratch = AssetDatabase.LoadAssetAtPath<HoleDataAsset>(ScratchAssetPath);
            if (scratch == null)
            {
                EditorUtility.DisplayDialog(
                    "Course Editor",
                    $"Missing scratch asset at {ScratchAssetPath}. Reopen the Course Editor window.",
                    "OK");
                return;
            }

            scratch.Data = data;
            EditorUtility.SetDirty(scratch);
            AssetDatabase.SaveAssets();

            var bootstrap = Object.FindFirstObjectByType<CourseEditorRuntimeBootstrap>();
            if (bootstrap == null)
                bootstrap = throwController.gameObject.AddComponent<CourseEditorRuntimeBootstrap>();

            bootstrap.Configure(scratch, theme, holeSetup);

            EditorUtility.SetDirty(bootstrap);
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
