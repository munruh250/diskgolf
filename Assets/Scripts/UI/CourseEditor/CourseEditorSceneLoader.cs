using DiskGolf.CourseEditor;
using UnityEngine;

namespace DiskGolf.UI.CourseEditor
{
    /// <summary>Loads the active hole into <see cref="CourseEditorSession"/> when the editor scene starts.</summary>
    [DefaultExecutionOrder(-300)]
    public sealed class CourseEditorSceneLoader : MonoBehaviour
    {
        void Awake()
        {
            var session = CourseEditorSession.Instance;
            if (session.Hole != null)
                return;

            string id = PlayerPrefs.GetString(CourseEditorNavigation.ActiveHoleIdKey, null);
            if (string.IsNullOrEmpty(id))
                return;

            var hole = HoleDataCatalog.Player.Load(id);
            var theme = ThemePackLoader.Load(hole.ThemeId);
            session.Load(hole, theme, id);
        }
    }
}
