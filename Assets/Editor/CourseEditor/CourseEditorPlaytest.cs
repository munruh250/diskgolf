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

            if (Object.FindFirstObjectByType<ThrowController>() == null)
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "Course Editor",
                    "No ThrowController found in this scene. Open PrototypeFlat3 (or a scene with a throw rig) for a playable playtest. Continue anyway?",
                    "Continue",
                    "Cancel");
                if (!proceed)
                    return;
            }

            ApplyHoleSetup(holeSetup, host, data);
            EditorApplication.isPlaying = true;
        }

        static void ApplyHoleSetup(HoleSetup setup, BuiltCourseHost host, HoleData data)
        {
            var serializedSetup = new SerializedObject(setup);
            SetProperty(serializedSetup, "teePad", p => p.objectReferenceValue = host.TeePad);
            SetProperty(serializedSetup, "basket", p => p.objectReferenceValue = host.Basket);
            SetProperty(serializedSetup, "par", p => p.intValue = data.Hole.Par);
            SetProperty(serializedSetup, "circleRadiusFt", p => p.floatValue = data.Hole.CircleRadiusFt);
            SetProperty(serializedSetup, "holeLengthFt", p => p.floatValue = data.HoleLengthYards() * 3f);
            serializedSetup.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(setup);
        }

        static void SetProperty(SerializedObject so, string name, System.Action<SerializedProperty> assign)
        {
            var prop = so.FindProperty(name);
            if (prop == null)
            {
                Debug.LogWarning($"[CourseEditorPlaytest] HoleSetup missing serialized field: {name}");
                return;
            }

            assign(prop);
        }
    }
}
#endif
