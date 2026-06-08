#if UNITY_EDITOR
using System.IO;
using System.Linq;
using DiskGolf.Core;
using DiskGolf.Gameplay;
using DiskGolf.UI;
using DiskGolf.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiskGolf.EditorTools
{
    public static class MenuSceneBuilder
    {
        static readonly Color BgColor = new(0.08f, 0.14f, 0.11f, 1f);

        static readonly Color PanelColor = new(0.12f, 0.18f, 0.14f, 0.96f);

        static readonly Color AccentColor = new(0.28f, 0.78f, 0.36f, 1f);

        [MenuItem("Disk Golf/Build Menu Scenes")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(ProjectArtPaths.Scenes.MenuRoot);
            EnsureTmpEssentials();
            PlayerCharacterRosterBuilder.CreateDefaultRoster();

            BuildIntroScene();
            BuildMainMenuScene();
            BuildCharacterSelectScene();
            BuildCourseSelectScene();
            BuildSettingsScene();
            UpdateBuildSettings();

            Debug.Log("[Disk Golf] Menu scenes built and added to Build Settings (Intro first).");
        }

        static void BuildIntroScene()
        {
            NewMenuScene();
            SetupCamera();

            var canvas = MenuCanvas();
            CreateTitle(canvas, "Disk Golf Masters", 92f, new Vector2(0f, 80f));
            CreateBody(canvas, "Press any key or click to continue", 30f, new Vector2(0f, -40f),
                TextAlignmentOptions.Center);

            new GameObject("IntroController").AddComponent<IntroSceneController>();
            SaveScene(ProjectArtPaths.Scenes.Intro);
        }

        static void BuildMainMenuScene()
        {
            NewMenuScene();
            SetupCamera();

            var canvas = MenuCanvas();
            CreateTitle(canvas, "Main Menu", 64f, new Vector2(0f, 280f));

            var root = new GameObject("MainMenuController");
            var menu = root.AddComponent<MainMenuController>();

            var character = CreateMenuButton(canvas, "Character Select", new Vector2(0f, 60f));
            var course = CreateMenuButton(canvas, "Course Select", new Vector2(0f, -40f));
            var settings = CreateMenuButton(canvas, "Settings", new Vector2(0f, -140f));

            AssignSerialized(menu, "characterSelectButton", character);
            AssignSerialized(menu, "courseSelectButton", course);
            AssignSerialized(menu, "settingsButton", settings);

            SaveScene(ProjectArtPaths.Scenes.MainMenu);
        }

        static void BuildCharacterSelectScene()
        {
            BuildCharacterSelectSceneOnly();
        }

        public static void BuildCharacterSelectSceneOnly()
        {
            NewMenuScene();
            SetupCamera();

            var canvas = MenuCanvas();
            var bg = canvas.gameObject.GetComponent<Image>() ?? canvas.gameObject.AddComponent<Image>();
            bg.color = new Color(0.22f, 0.28f, 0.2f, 1f);
            bg.raycastTarget = false;

            var screenGo = new GameObject("CharacterSelectScreen", typeof(RectTransform));
            var screenRt = screenGo.GetComponent<RectTransform>();
            screenRt.SetParent(canvas, false);
            screenRt.anchorMin = Vector2.zero;
            screenRt.anchorMax = Vector2.one;
            screenRt.offsetMin = Vector2.zero;
            screenRt.offsetMax = Vector2.zero;
            var screen = screenGo.AddComponent<CharacterSelectScreen>();

            CharacterSelectSceneBuilder.BuildUi(screenRt, screen);

            SaveScene(ProjectArtPaths.Scenes.CharacterSelect);
        }

        static void BuildCourseSelectScene()
        {
            NewMenuScene();
            SetupCamera();

            var canvas = MenuCanvas();
            CreateTitle(canvas, "Course Select", 58f, new Vector2(0f, 280f));
            CreateBody(canvas, "Prototype Flat 3 — 250 ft par 3", 28f, new Vector2(0f, 40f),
                TextAlignmentOptions.Center);

            var root = new GameObject("CourseSelectController");
            var controller = root.AddComponent<CourseSelectController>();
            WireNavButtons(canvas, controller, continueLabel: "Continue To Play");

            SaveScene(ProjectArtPaths.Scenes.CourseSelect);
        }

        static void BuildSettingsScene()
        {
            NewMenuScene();
            SetupCamera();

            var canvas = MenuCanvas();
            CreateTitle(canvas, "Settings", 58f, new Vector2(0f, 280f));
            CreateBody(canvas, "Difficulty", 34f, new Vector2(0f, 120f), TextAlignmentOptions.Center);

            var root = new GameObject("SettingsController");
            var settings = root.AddComponent<SettingsMenuController>();

            var beginner = CreateDifficultyToggle(canvas, "BeginnerToggle", "Beginner", new Vector2(-180f, 20f));
            var advanced = CreateDifficultyToggle(canvas, "AdvancedToggle", "Advanced", new Vector2(180f, 20f));

            var back = CreateMenuButton(canvas, "Back To Main Menu", new Vector2(0f, -320f), new Vector2(360f, 56f));

            AssignSerialized(settings, "beginnerToggle", beginner);
            AssignSerialized(settings, "advancedToggle", advanced);
            AssignSerialized(settings, "backButton", back);

            SaveScene(ProjectArtPaths.Scenes.Settings);
        }

        static void WireNavButtons(RectTransform canvas, MonoBehaviour controller, string continueLabel)
        {
            var back = CreateMenuButton(canvas, "Back To Main Menu", new Vector2(-220f, -320f), new Vector2(360f, 56f));
            var cont = CreateMenuButton(canvas, continueLabel, new Vector2(220f, -320f), new Vector2(360f, 56f));

            AssignSerialized(controller, "backButton", back);
            AssignSerialized(controller, "continueButton", cont);
        }

        static void NewMenuScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        static void SetupCamera()
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
            cam.backgroundColor = BgColor;
            cam.orthographic = false;
            camGo.AddComponent<AudioListener>();
        }

        static void EventSystemBootstrap()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        static RectTransform MenuCanvas()
        {
            EventSystemBootstrap();

            var holder = new GameObject("MenuCanvas");
            var canvas = holder.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = holder.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            holder.AddComponent<GraphicRaycaster>();

            var rt = holder.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        static TextMeshProUGUI CreateTitle(RectTransform parent, string text, float size, Vector2 pos)
        {
            return CreateLabel(parent, "Title", text, size, pos, new Vector2(1200f, 140f),
                TextAlignmentOptions.Center, AccentColor);
        }

        static TextMeshProUGUI CreateBody(RectTransform parent, string text, float size, Vector2 pos,
            TextAlignmentOptions align)
        {
            return CreateLabel(parent, "Body", text, size, pos, new Vector2(1100f, 100f), align, Color.white);
        }

        static TextMeshProUGUI CreateLabel(
            RectTransform parent,
            string name,
            string text,
            float size,
            Vector2 pos,
            Vector2 dimensions,
            TextAlignmentOptions align,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = dimensions;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            HudTypography.BindFont(tmp);
            return tmp;
        }

        static Button CreateMenuButton(RectTransform parent, string label, Vector2 pos, Vector2? size = null)
        {
            var dimensions = size ?? new Vector2(420f, 64f);
            var go = new GameObject(label.Replace(" ", "") + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = dimensions;

            var image = go.GetComponent<Image>();
            image.color = PanelColor;

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = AccentColor * 1.1f;
            colors.pressedColor = AccentColor * 0.85f;
            colors.selectedColor = AccentColor;
            button.colors = colors;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.SetParent(rt, false);
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(12f, 6f);
            labelRt.offsetMax = new Vector2(-12f, -6f);

            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 28f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            HudTypography.BindFont(tmp);

            return button;
        }

        static Toggle CreateDifficultyToggle(RectTransform parent, string name, string label, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(280f, 48f);

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.SetParent(rt, false);
            bgRt.anchorMin = new Vector2(0f, 0.5f);
            bgRt.anchorMax = new Vector2(0f, 0.5f);
            bgRt.pivot = new Vector2(0f, 0.5f);
            bgRt.anchoredPosition = Vector2.zero;
            bgRt.sizeDelta = new Vector2(34f, 34f);
            bg.GetComponent<Image>().color = PanelColor;

            var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            var checkRt = check.GetComponent<RectTransform>();
            checkRt.SetParent(bgRt, false);
            checkRt.anchorMin = Vector2.zero;
            checkRt.anchorMax = Vector2.one;
            checkRt.offsetMin = new Vector2(6f, 6f);
            checkRt.offsetMax = new Vector2(-6f, -6f);
            check.GetComponent<Image>().color = AccentColor;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.SetParent(rt, false);
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.offsetMin = new Vector2(44f, 0f);
            labelRt.offsetMax = Vector2.zero;

            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 30f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;
            HudTypography.BindFont(tmp);

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            toggle.isOn = label == "Beginner";
            return toggle;
        }

        static void SaveScene(string path)
        {
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene, path);
        }

        static void UpdateBuildSettings()
        {
            var paths = new[]
            {
                ProjectArtPaths.Scenes.Intro,
                ProjectArtPaths.Scenes.MainMenu,
                ProjectArtPaths.Scenes.CharacterSelect,
                ProjectArtPaths.Scenes.CourseSelect,
                ProjectArtPaths.Scenes.Settings,
                ProjectArtPaths.Scenes.PrototypeFlat3,
            };

            EditorBuildSettings.scenes = paths
                .Select(p => new EditorBuildSettingsScene(p, true))
                .ToArray();
        }

        static void EnsureTmpEssentials()
        {
            if (!AssetDatabase.IsValidFolder("Assets/ThirdParty/TextMesh Pro/Resources"))
                return;

            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(ProjectArtPaths.ThirdParty.TmpSettings);
            if (settings == null)
                Debug.LogWarning("[Disk Golf] TMP Settings missing — run TMP Essentials import if menu fonts fail.");
        }

        static void AssignSerialized(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif
