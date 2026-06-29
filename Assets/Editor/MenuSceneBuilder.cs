#if UNITY_EDITOR
using System.IO;
using System.Linq;
using DiskGolf.Core;
using DiskGolf.Gameplay;
using DiskGolf.UI;
using DiskGolf.UI.CourseEditor;
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
            BuildCourseEditorHubScene();
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
            var courseEditor = CreateMenuButton(canvas, "Course Editor", new Vector2(0f, -240f));

            AssignSerialized(menu, "characterSelectButton", character);
            AssignSerialized(menu, "courseSelectButton", course);
            AssignSerialized(menu, "settingsButton", settings);
            AssignSerialized(menu, "courseEditorButton", courseEditor);

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

        static void BuildCourseEditorHubScene()
        {
            NewMenuScene();
            SetupCamera();

            var canvas = MenuCanvas();
            CreateTitle(canvas, "Course Editor", 58f, new Vector2(0f, 280f));

            var root = new GameObject("CourseEditorHubController");
            root.transform.SetParent(canvas, false);
            var controller = root.AddComponent<CourseEditorHubController>();

            var holeListContent = CreateHoleListScrollView(canvas);
            var newHole = CreateMenuButton(canvas, "New Hole", new Vector2(0f, -180f));
            var import = CreateMenuButton(canvas, "Import", new Vector2(0f, -250f), new Vector2(280f, 52f));
            var back = CreateMenuButton(canvas, "Back", new Vector2(0f, -320f), new Vector2(220f, 56f));

            var wizard = CreateNewHoleWizardOverlay(canvas);

            AssignSerialized(controller, "holeListContent", holeListContent);
            AssignSerialized(controller, "newHoleButton", newHole);
            AssignSerialized(controller, "importButton", import);
            AssignSerialized(controller, "backButton", back);
            AssignSerialized(controller, "wizard", wizard);

            SaveScene(ProjectArtPaths.Scenes.CourseEditorHub);
        }

        static RectTransform CreateHoleListScrollView(RectTransform parent)
        {
            var scrollGo = new GameObject("HoleListScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.SetParent(parent, false);
            scrollRt.anchorMin = scrollRt.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRt.pivot = new Vector2(0.5f, 0.5f);
            scrollRt.anchoredPosition = new Vector2(0f, 30f);
            scrollRt.sizeDelta = new Vector2(1240f, 320f);
            scrollGo.GetComponent<Image>().color = PanelColor * 0.85f;

            var scrollbar = CreateHubVerticalScrollbar(scrollRt);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.SetParent(scrollRt, false);
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(8f, 8f);
            viewportRt.offsetMax = new Vector2(-26f, -8f);
            var viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = Color.white;
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.SetParent(viewportRt, false);
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);

            var contentLayout = contentGo.GetComponent<LayoutElement>();
            contentLayout.minWidth = 1180f;

            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewportRt;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 50f;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            return contentRt;
        }

        static Scrollbar CreateHubVerticalScrollbar(RectTransform scrollRt)
        {
            var resources = new DefaultControls.Resources();
            var scrollbarGo = DefaultControls.CreateScrollbar(resources);
            scrollbarGo.name = "VerticalScrollbar";
            var scrollbarRt = scrollbarGo.GetComponent<RectTransform>();
            scrollbarRt.SetParent(scrollRt, false);
            scrollbarRt.anchorMin = new Vector2(1f, 0f);
            scrollbarRt.anchorMax = new Vector2(1f, 1f);
            scrollbarRt.pivot = new Vector2(1f, 1f);
            scrollbarRt.anchoredPosition = Vector2.zero;
            scrollbarRt.sizeDelta = new Vector2(20f, -16f);

            var trackImage = scrollbarGo.GetComponent<Image>();
            if (trackImage != null)
                trackImage.color = new Color(0.08f, 0.1f, 0.09f, 0.95f);

            var handle = scrollbarGo.transform.Find("Sliding Area/Handle")?.GetComponent<Image>();
            if (handle != null)
                handle.color = AccentColor * 0.85f;

            return scrollbarGo.GetComponent<Scrollbar>();
        }

        static NewHoleWizardController CreateNewHoleWizardOverlay(RectTransform parent)
        {
            var overlayGo = new GameObject("NewHoleWizardOverlay", typeof(RectTransform), typeof(Image));
            var overlayRt = overlayGo.GetComponent<RectTransform>();
            overlayRt.SetParent(parent, false);
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            overlayGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

            var panelGo = new GameObject("WizardPanel", typeof(RectTransform), typeof(Image));
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.SetParent(overlayRt, false);
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(820f, 540f);
            panelGo.GetComponent<Image>().color = PanelColor;

            var wizard = overlayGo.AddComponent<NewHoleWizardController>();

            var step1 = CreateWizardStepPanel(panelRt, "Step1_Name");
            CreateWizardTitle(step1, "Name your hole");
            var nameInput = CreateWizardCenteredInput(step1, "My Par 3");

            var step2 = CreateWizardStepPanel(panelRt, "Step2_Template");
            CreateWizardTitle(step2, "Choose a template");
            var templateGroupRt = CreateWizardToggleList(step2, out var templateGroup);
            var blankToggle = CreateWizardTemplateToggle(templateGroupRt, "BlankPar3Toggle", "Blank Par 3");
            var straightToggle = CreateWizardTemplateToggle(templateGroupRt, "StraightPar3Toggle", "Straight Par 3");
            var ridgelineToggle = CreateWizardTemplateToggle(templateGroupRt, "RidgelineToggle", "Ridgeline");
            blankToggle.group = templateGroup;
            straightToggle.group = templateGroup;
            ridgelineToggle.group = templateGroup;
            straightToggle.isOn = true;

            var step3 = CreateWizardStepPanel(panelRt, "Step3_Theme");
            CreateWizardTitle(step3, "Theme");
            var themeGroupRt = CreateWizardToggleList(step3, out var themeGroup);
            var temperateToggle = CreateWizardTemplateToggle(themeGroupRt, "TemperateThemeToggle", "Temperate");
            temperateToggle.group = themeGroup;
            temperateToggle.isOn = true;

            var step4 = CreateWizardStepPanel(panelRt, "Step4_Start");
            CreateWizardTitle(step4, "Ready to build");
            var readySubtitle = CreateWizardSubtitle(
                step4,
                "Your new hole will open in the course editor.");

            var (cancel, back, next, start) = CreateWizardNavBar(panelRt);

            AssignSerialized(wizard, "overlayRoot", overlayGo);
            AssignSerializedArray(wizard, "stepPanels", step1.gameObject, step2.gameObject, step3.gameObject, step4.gameObject);
            AssignSerialized(wizard, "nameInput", nameInput);
            AssignSerialized(wizard, "blankTemplateToggle", blankToggle);
            AssignSerialized(wizard, "straightTemplateToggle", straightToggle);
            AssignSerialized(wizard, "ridgelineTemplateToggle", ridgelineToggle);
            AssignSerialized(wizard, "temperateThemeToggle", temperateToggle);
            AssignSerialized(wizard, "cancelButton", cancel);
            AssignSerialized(wizard, "backButton", back);
            AssignSerialized(wizard, "nextButton", next);
            AssignSerialized(wizard, "startBuildingButton", start);

            step2.gameObject.SetActive(false);
            step3.gameObject.SetActive(false);
            step4.gameObject.SetActive(false);
            back.gameObject.SetActive(false);
            start.gameObject.SetActive(false);

            overlayGo.SetActive(false);
            return wizard;
        }

        static void CreateWizardTitle(RectTransform step, string title)
        {
            var titleLabel = CreateLabel(
                step,
                "Title",
                title,
                34f,
                Vector2.zero,
                new Vector2(760f, 48f),
                TextAlignmentOptions.Center,
                Color.white);
            var rt = titleLabel.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -8f);
            rt.sizeDelta = new Vector2(760f, 48f);
        }

        static TextMeshProUGUI CreateWizardSubtitle(RectTransform step, string text)
        {
            var subtitle = CreateLabel(
                step,
                "Subtitle",
                text,
                24f,
                Vector2.zero,
                new Vector2(640f, 80f),
                TextAlignmentOptions.Center,
                Color.white);
            subtitle.enableWordWrapping = true;
            var rt = subtitle.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -20f);
            rt.sizeDelta = new Vector2(640f, 80f);
            return subtitle;
        }

        static TMP_InputField CreateWizardCenteredInput(RectTransform step, string defaultText)
        {
            var input = CreateTmpInputField(step, defaultText, Vector2.zero, new Vector2(520f, 56f));
            if (input.textComponent != null)
                input.textComponent.alignment = TextAlignmentOptions.Center;
            if (input.placeholder is TextMeshProUGUI placeholder)
                placeholder.alignment = TextAlignmentOptions.Center;
            return input;
        }

        static RectTransform CreateWizardToggleList(RectTransform step, out ToggleGroup toggleGroup)
        {
            var groupGo = new GameObject("ToggleList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ToggleGroup));
            var groupRt = groupGo.GetComponent<RectTransform>();
            groupRt.SetParent(step, false);
            groupRt.anchorMin = new Vector2(0f, 0.22f);
            groupRt.anchorMax = new Vector2(1f, 0.78f);
            groupRt.offsetMin = Vector2.zero;
            groupRt.offsetMax = Vector2.zero;

            var layout = groupGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            toggleGroup = groupGo.GetComponent<ToggleGroup>();
            toggleGroup.allowSwitchOff = false;
            return groupRt;
        }

        static (Button cancel, Button back, Button next, Button start) CreateWizardNavBar(RectTransform panelRt)
        {
            var navGo = new GameObject("NavBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var navRt = navGo.GetComponent<RectTransform>();
            navRt.SetParent(panelRt, false);
            navRt.anchorMin = new Vector2(0f, 0f);
            navRt.anchorMax = new Vector2(1f, 0f);
            navRt.pivot = new Vector2(0.5f, 0f);
            navRt.anchoredPosition = Vector2.zero;
            navRt.sizeDelta = new Vector2(0f, 72f);

            var nav = navGo.GetComponent<HorizontalLayoutGroup>();
            nav.padding = new RectOffset(24, 24, 12, 12);
            nav.spacing = 16f;
            nav.childAlignment = TextAnchor.MiddleCenter;
            nav.childControlWidth = false;
            nav.childControlHeight = true;
            nav.childForceExpandWidth = false;
            nav.childForceExpandHeight = false;

            var cancel = CreateWizardNavButton(navRt, "Cancel", 168f);
            var back = CreateWizardNavButton(navRt, "Back", 140f);
            var next = CreateWizardNavButton(navRt, "Next", 140f);
            var start = CreateWizardNavButton(navRt, "Start Building", 260f);
            return (cancel, back, next, start);
        }

        static Button CreateWizardNavButton(RectTransform parent, string label, float width)
        {
            var go = new GameObject(label.Replace(" ", "") + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);

            var layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.minWidth = width;
            layout.preferredHeight = 48f;
            layout.minHeight = 48f;

            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, 0f);

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
            labelRt.offsetMin = new Vector2(8f, 4f);
            labelRt.offsetMax = new Vector2(-8f, -4f);

            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 26f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            HudTypography.BindFont(tmp);

            return button;
        }

        static RectTransform CreateWizardStepPanel(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(24f, 80f);
            rt.offsetMax = new Vector2(-24f, -80f);
            return rt;
        }

        static TMP_InputField CreateTmpInputField(RectTransform parent, string defaultText, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("NameInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = BgColor;

            var textAreaGo = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            var textAreaRt = textAreaGo.GetComponent<RectTransform>();
            textAreaRt.SetParent(rt, false);
            textAreaRt.anchorMin = Vector2.zero;
            textAreaRt.anchorMax = Vector2.one;
            textAreaRt.offsetMin = new Vector2(12f, 8f);
            textAreaRt.offsetMax = new Vector2(-12f, -8f);

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            var placeholderRt = placeholderGo.GetComponent<RectTransform>();
            placeholderRt.SetParent(textAreaRt, false);
            placeholderRt.anchorMin = Vector2.zero;
            placeholderRt.anchorMax = Vector2.one;
            placeholderRt.offsetMin = Vector2.zero;
            placeholderRt.offsetMax = Vector2.zero;
            var placeholder = placeholderGo.GetComponent<TextMeshProUGUI>();
            placeholder.text = defaultText;
            placeholder.fontSize = 26f;
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.color = new Color(1f, 1f, 1f, 0.45f);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            HudTypography.BindFont(placeholder);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.SetParent(textAreaRt, false);
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.text = defaultText;
            text.fontSize = 26f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            HudTypography.BindFont(text);

            var input = go.GetComponent<TMP_InputField>();
            input.textViewport = textAreaRt;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.text = defaultText;
            return input;
        }

        static void AssignSerializedArray(Object target, string fieldName, params Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null || !prop.isArray)
                return;

            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            so.ApplyModifiedPropertiesWithoutUndo();
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

        static readonly Color WizardRowIdleColor = new(0.34f, 0.36f, 0.4f, 0.96f);

        static Toggle CreateWizardTemplateToggle(RectTransform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Toggle), typeof(LayoutElement));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);

            var layout = go.GetComponent<LayoutElement>();
            layout.preferredHeight = 52f;
            layout.minHeight = 52f;

            var rowImage = go.GetComponent<Image>();
            rowImage.color = WizardRowIdleColor;

            var checkBg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            var checkBgRt = checkBg.GetComponent<RectTransform>();
            checkBgRt.SetParent(rt, false);
            checkBgRt.anchorMin = new Vector2(0f, 0.5f);
            checkBgRt.anchorMax = new Vector2(0f, 0.5f);
            checkBgRt.pivot = new Vector2(0f, 0.5f);
            checkBgRt.anchoredPosition = new Vector2(14f, 0f);
            checkBgRt.sizeDelta = new Vector2(28f, 28f);
            checkBg.GetComponent<Image>().color = new Color(0.22f, 0.24f, 0.27f, 1f);

            var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            var checkRt = check.GetComponent<RectTransform>();
            checkRt.SetParent(checkBgRt, false);
            checkRt.anchorMin = Vector2.zero;
            checkRt.anchorMax = Vector2.one;
            checkRt.offsetMin = new Vector2(5f, 5f);
            checkRt.offsetMax = new Vector2(-5f, -5f);
            check.GetComponent<Image>().color = AccentColor;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.SetParent(rt, false);
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(52f, 0f);
            labelRt.offsetMax = new Vector2(-16f, 0f);

            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 28f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            HudTypography.BindFont(tmp);

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = rowImage;
            toggle.graphic = check.GetComponent<Image>();
            var colors = toggle.colors;
            colors.normalColor = WizardRowIdleColor;
            colors.highlightedColor = new Color(0.42f, 0.44f, 0.48f, 0.98f);
            colors.pressedColor = AccentColor * 0.92f;
            colors.selectedColor = AccentColor;
            toggle.colors = colors;
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
                ProjectArtPaths.Scenes.CourseEditorHub,
                ProjectArtPaths.Scenes.CourseEditor,
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
