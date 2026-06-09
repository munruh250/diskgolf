#if UNITY_EDITOR
using DiskGolf.Core;
using DiskGolf.CourseEditor;
using UnityEditor;
using UnityEngine;

namespace DiskGolf.EditorTools.CourseEditor
{
    public static class CourseEditorPlaytest
    {
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

            BuiltCourseHost host = CourseBuilder.Build(data, theme);
            if (host == null)
                return;

            HoleSetup holeSetup = Object.FindFirstObjectByType<HoleSetup>();
            if (holeSetup == null)
            {
                var go = new GameObject("HoleSetup");
                holeSetup = go.AddComponent<HoleSetup>();
            }

            ApplyHoleSetup(holeSetup, host, data);
            EditorApplication.isPlaying = true;
        }

        static void ApplyHoleSetup(HoleSetup setup, BuiltCourseHost host, HoleData data)
        {
            var serializedSetup = new SerializedObject(setup);
            serializedSetup.FindProperty("teePad").objectReferenceValue = host.TeePad;
            serializedSetup.FindProperty("basket").objectReferenceValue = host.Basket;
            serializedSetup.FindProperty("par").intValue = data.Hole.Par;
            serializedSetup.FindProperty("circleRadiusFt").floatValue = data.Hole.CircleRadiusFt;
            serializedSetup.FindProperty("holeLengthFt").floatValue = data.HoleLengthYards() * 3f;
            serializedSetup.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(setup);
        }
    }
}
#endif
