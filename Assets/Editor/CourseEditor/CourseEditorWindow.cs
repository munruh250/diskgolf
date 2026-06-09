#if UNITY_EDITOR
using System.IO;
using DiskGolf.CourseEditor;
using DiskGolf.Gameplay;
using UnityEditor;
using UnityEngine;

namespace DiskGolf.EditorTools.CourseEditor
{
    public sealed class CourseEditorWindow : EditorWindow
    {
        const string WindowTitle = "Course Editor";
        const string ScratchAssetPath = ProjectArtPaths.Data.CoursesRoot + "/_EditorScratch.asset";

        static HoleDataAsset s_ScratchAsset;
        static string s_CurrentJsonPath;

        [MenuItem("Disk Golf/Course Editor")]
        public static void Open() => GetWindow<CourseEditorWindow>(WindowTitle);

        public static Object GetSerializedHoleTarget()
        {
            EnsureScratchAsset();
            return s_ScratchAsset;
        }

        void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle);
            EnsureEditorState();
        }

        void OnGUI()
        {
            EnsureEditorState();
            DrawToolbar();

            HoleData data = CourseEditorState.Data;
            if (data == null)
            {
                EditorGUILayout.HelpBox("No hole data is loaded.", MessageType.Warning);
                return;
            }

            DrawTools(data);
            DrawValidation(data);
        }

        static void EnsureEditorState()
        {
            EnsureScratchAsset();
            CourseEditorState.SetDataAsset(s_ScratchAsset);

            if (CourseEditorState.Theme == null)
            {
                CourseEditorState.Theme = FindDefaultTheme();
            }
        }

        static void EnsureScratchAsset()
        {
            if (s_ScratchAsset != null)
            {
                return;
            }

            s_ScratchAsset = AssetDatabase.LoadAssetAtPath<HoleDataAsset>(ScratchAssetPath);
            if (s_ScratchAsset == null)
            {
                string coursesAbsolutePath = AssetPathToAbsolute(ProjectArtPaths.Data.CoursesRoot);
                if (!Directory.Exists(coursesAbsolutePath))
                {
                    Directory.CreateDirectory(coursesAbsolutePath);
                }

                s_ScratchAsset = CreateInstance<HoleDataAsset>();
                s_ScratchAsset.name = "_EditorScratch";
                AssetDatabase.CreateAsset(s_ScratchAsset, ScratchAssetPath);
                AssetDatabase.SaveAssets();
            }

            if (s_ScratchAsset.Data == null)
            {
                s_ScratchAsset.Data = new HoleData();
                EditorUtility.SetDirty(s_ScratchAsset);
                AssetDatabase.SaveAssets();
            }
        }

        static ThemePack FindDefaultTheme()
        {
            string[] guids = AssetDatabase.FindAssets("t:ThemePack");
            if (guids.Length == 0)
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<ThemePack>(path);
        }

        static string AssetPathToAbsolute(string assetPath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("New", EditorStyles.toolbarButton))
            {
                OnNew();
            }

            if (GUILayout.Button("Open", EditorStyles.toolbarButton))
            {
                OnOpen();
            }

            if (GUILayout.Button("Save", EditorStyles.toolbarButton))
            {
                OnSave();
            }

            if (GUILayout.Button("Export JSON", EditorStyles.toolbarButton))
            {
                OnExport();
            }

            if (GUILayout.Button("Import JSON", EditorStyles.toolbarButton))
            {
                OnImport();
            }

            if (GUILayout.Button("Bake", EditorStyles.toolbarButton))
            {
                OnBake();
            }

            if (GUILayout.Button("Playtest", EditorStyles.toolbarButton))
            {
                OnPlaytest();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        void DrawTools(HoleData data)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

            string[] toolLabels = { "Paint", "Erase", "HoleTee", "HoleBasket" };
            int selectedTool = (int)CourseEditorState.ActiveTool;
            int nextTool = GUILayout.Toolbar(selectedTool, toolLabels);
            if (nextTool != selectedTool)
            {
                CourseEditorState.ActiveTool = (CourseEditorTool)nextTool;
            }

            SurfaceTileType nextBrushType =
                (SurfaceTileType)EditorGUILayout.EnumPopup("Brush Type", CourseEditorState.BrushType);
            if (nextBrushType != CourseEditorState.BrushType)
            {
                CourseEditorState.BrushType = nextBrushType;
            }

            ThemePack nextTheme =
                (ThemePack)EditorGUILayout.ObjectField("Theme", CourseEditorState.Theme, typeof(ThemePack), false);
            if (nextTheme != CourseEditorState.Theme)
            {
                CourseEditorState.Theme = nextTheme;
                if (nextTheme != null)
                {
                    data.ThemeId = nextTheme.themeId;
                }

                CourseEditorState.IsDirty = true;
                EditorUtility.SetDirty(s_ScratchAsset);
            }

            int nextPar = EditorGUILayout.IntField("Par", data.Hole.Par);
            nextPar = Mathf.Max(1, nextPar);
            if (nextPar != data.Hole.Par)
            {
                Undo.RecordObject(s_ScratchAsset, "Change Par");
                data.Hole.Par = nextPar;
                MarkDataDirty();
            }

            EditorGUILayout.LabelField("Yardage", $"{data.HoleLengthYards():F0} yd");
        }

        void DrawValidation(HoleData data)
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            ValidationResult validation = CourseValidator.Validate(data);
            if (validation.Messages.Count == 0)
            {
                EditorGUILayout.HelpBox("No validation messages.", MessageType.Info);
                return;
            }

            foreach (ValidationMessage message in validation.Messages)
            {
                MessageType type = message.Severity == ValidationSeverity.Error
                    ? MessageType.Error
                    : MessageType.Warning;
                EditorGUILayout.HelpBox($"{message.Code}: {message.Text}", type);
            }
        }

        void OnNew()
        {
            Undo.RecordObject(s_ScratchAsset, "New Hole");
            s_ScratchAsset.Data = new HoleData();
            s_CurrentJsonPath = null;
            CourseEditorState.SetDataAsset(s_ScratchAsset);
            MarkDataDirty();
        }

        void OnOpen()
        {
            string path = EditorUtility.OpenFilePanel(
                "Open hole JSON",
                AssetPathToAbsolute(ProjectArtPaths.Data.CoursesRoot),
                "json");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            HoleData loaded = HoleDataJson.LoadFromFile(path);
            ReplaceData(loaded);
            s_CurrentJsonPath = path;
            CourseEditorState.IsDirty = false;
        }

        void OnSave()
        {
            string path = s_CurrentJsonPath;
            if (string.IsNullOrEmpty(path))
            {
                string defaultName = $"{CourseEditorState.Data.Id}.json";
                path = EditorUtility.SaveFilePanel(
                    "Save hole JSON",
                    AssetPathToAbsolute(ProjectArtPaths.Data.CoursesRoot),
                    defaultName,
                    "json");
            }

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            ApplyThemeId();
            HoleDataJson.SaveToFile(CourseEditorState.Data, path);
            s_CurrentJsonPath = path;
            CourseEditorState.IsDirty = false;
            AssetDatabase.Refresh();
        }

        void OnExport()
        {
            string defaultName = $"{CourseEditorState.Data.Id}.json";
            string path = EditorUtility.SaveFilePanel(
                "Export hole JSON",
                AssetPathToAbsolute(ProjectArtPaths.Data.CoursesRoot),
                defaultName,
                "json");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            ApplyThemeId();
            HoleDataJson.SaveToFile(CourseEditorState.Data, path);
            AssetDatabase.Refresh();
        }

        void OnImport()
        {
            string path = EditorUtility.OpenFilePanel(
                "Import hole JSON",
                AssetPathToAbsolute(ProjectArtPaths.Data.CoursesRoot),
                "json");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            HoleData loaded = HoleDataJson.LoadFromFile(path);
            ReplaceData(loaded);
            CourseEditorState.IsDirty = true;
        }

        void OnBake()
        {
            ApplyThemeId();
            CourseBuilder.Build(CourseEditorState.Data, CourseEditorState.Theme);
            CourseEditorState.IsDirty = false;
        }

        void OnPlaytest()
        {
            ApplyThemeId();
            CourseEditorPlaytest.Run(CourseEditorState.Data, CourseEditorState.Theme);
        }

        void ReplaceData(HoleData data)
        {
            Undo.RecordObject(s_ScratchAsset, "Load Hole Data");
            s_ScratchAsset.Data = data ?? new HoleData();
            CourseEditorState.SetDataAsset(s_ScratchAsset);
            MarkDataDirty();
        }

        void ApplyThemeId()
        {
            if (CourseEditorState.Theme != null && CourseEditorState.Data != null)
            {
                CourseEditorState.Data.ThemeId = CourseEditorState.Theme.themeId;
            }
        }

        void MarkDataDirty()
        {
            CourseEditorState.IsDirty = true;
            EditorUtility.SetDirty(s_ScratchAsset);
            SceneView.RepaintAll();
            Repaint();
        }
    }
}
#endif
