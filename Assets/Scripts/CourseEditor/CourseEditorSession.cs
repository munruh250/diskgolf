using DiskGolf.Core;
using DiskGolf.CourseEditor.Authoring;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    public sealed class CourseEditorSession
    {
        public static CourseEditorSession Instance { get; } = new();

        public HoleData Hole { get; private set; }
        public ThemePack Theme { get; private set; }
        public CourseEditorSessionMode Mode { get; set; } = CourseEditorSessionMode.Editing;
        public bool IsDirty { get; set; }
        public string ActiveHoleId { get; private set; }
        public CourseAuthoringState Authoring { get; } = new();
        public CourseAuthoringCommandStack Commands { get; } = new();
        public BuiltCourseHost CurrentBuiltCourse { get; private set; }

        CourseEditorSession() { }

        public void Load(HoleData hole, ThemePack theme, string holeId = null)
        {
            Hole = hole;
            Theme = theme;
            ActiveHoleId = holeId ?? hole?.Id;
            Mode = CourseEditorSessionMode.Editing;
            IsDirty = false;
            Authoring.IsDirty = false;
        }

        public void Rebake()
        {
            DestroyCurrentBuiltCourse();

            if (Hole == null || Theme == null)
            {
                CurrentBuiltCourse = null;
                return;
            }

            CurrentBuiltCourse = CourseBuilder.Build(Hole, Theme);
            if (CurrentBuiltCourse == null)
                return;

            var holeSetup = Object.FindFirstObjectByType<HoleSetup>();
            if (holeSetup == null)
                return;

            holeSetup.BindBuiltCourse(
                CurrentBuiltCourse.TeePad,
                CurrentBuiltCourse.Basket,
                Hole.Hole.Par,
                Hole.Hole.CircleRadiusFt,
                Hole.HoleLengthYards() * 3f);

            if (Mode == CourseEditorSessionMode.Playtesting)
            {
                holeSetup.PositionThrowerAtTee();
                holeSetup.RefreshCameraAimPoint();
            }

            SceneEnvironmentApplier.Apply(Hole, Theme);
            DiskGolf.UI.MinimapUI.NotifyCourseRebuilt();
        }

        void DestroyCurrentBuiltCourse()
        {
            if (CurrentBuiltCourse != null)
            {
                DestroyBuiltCourseObject(CurrentBuiltCourse.gameObject);
                CurrentBuiltCourse = null;
                return;
            }

            var existing = GameObject.Find(BuiltCourseHost.RootName);
            if (existing != null)
                DestroyBuiltCourseObject(existing);
        }

        static void DestroyBuiltCourseObject(GameObject target)
        {
            if (target == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(target);
                return;
            }
#endif
            Object.Destroy(target);
        }
    }
}
