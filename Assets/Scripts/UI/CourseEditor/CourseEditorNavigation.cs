using DiskGolf.Core;
using UnityEngine;

namespace DiskGolf.UI.CourseEditor
{
    public enum CourseEditorLaunchMode
    {
        Edit,
        Playtest
    }

    public static class CourseEditorNavigation
    {
        public const string ActiveHoleIdKey = "course_editor_active_id";
        public const string LaunchModeKey = "course_editor_launch_mode";

        public static void OpenEditor(string holeId)
        {
            PlayerPrefs.SetString(ActiveHoleIdKey, holeId);
            PlayerPrefs.SetString(LaunchModeKey, CourseEditorLaunchMode.Edit.ToString());
            SceneLoader.Load(SceneFlow.CourseEditor);
        }

        public static void OpenPlaytest(string holeId)
        {
            PlayerPrefs.SetString(ActiveHoleIdKey, holeId);
            PlayerPrefs.SetString(LaunchModeKey, CourseEditorLaunchMode.Playtest.ToString());
            SceneLoader.Load(SceneFlow.CourseEditor);
        }

        public static bool ConsumePlaytestLaunch()
        {
            bool playtest = PlayerPrefs.GetString(LaunchModeKey, CourseEditorLaunchMode.Edit.ToString())
                == CourseEditorLaunchMode.Playtest.ToString();
            PlayerPrefs.SetString(LaunchModeKey, CourseEditorLaunchMode.Edit.ToString());
            return playtest;
        }
    }
}
