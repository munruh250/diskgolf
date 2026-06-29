using System;
using System.Collections;
using System.Collections.Generic;
using DiskGolf.Core;
using DiskGolf.CourseEditor;
using DiskGolf.CourseEditor.Authoring;
using DiskGolf.CourseEditor.Platform;
using DiskGolf.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI.CourseEditor
{
    public sealed class CourseEditorHudController : MonoBehaviour
    {
        static readonly Color BgColor = new(0.08f, 0.14f, 0.11f, 1f);
        static readonly Color TopBarColor = new(0.96f, 0.97f, 0.98f, 0.98f);
        static readonly Color PanelColor = new(0.96f, 0.97f, 0.98f, 0.98f);
        static readonly Color AccentColor = new(0.278f, 0.78f, 0.36f, 1f);
        static readonly Color DimButtonColor = new(0.82f, 0.85f, 0.88f, 1f);
        static readonly Color LabelColor = new(0.18f, 0.38f, 0.28f, 1f);
        static readonly Color TopBarTextColor = new(0.12f, 0.16f, 0.14f, 1f);
        const float AutosaveIntervalSeconds = 30f;
        const string AutosaveEnabledPrefKey = "CourseEditor.AutosaveEnabled";
        const float StatusToastSeconds = 2.5f;

        [SerializeField] GameObject editorHudRoot;
        [SerializeField] GameObject gameplayHudRoot;

        [SerializeField] CourseEditorInputController inputController;
        [SerializeField] CourseEditorCameraController cameraController;

        CourseEditorSession session;
        ValidationDrawerController validationDrawer;
        CourseEditorPlaytestOverlay playtestOverlay;
        CourseEditorPlaytestController playtestController;
        DiskGolf.Input.ThrowInputHandler throwInputHandler;
        ThrowController throwController;
        HUDController gameplayHudController;
        HoleSetup holeSetup;
        bool confirmDiscardArmed;

        TextMeshProUGUI holeNameText;
        TextMeshProUGUI yardageText;
        TextMeshProUGUI parValueText;
        Button parDecreaseButton;
        Button parIncreaseButton;
        TextMeshProUGUI statusText;
        GameObject typePanelRoot;
        GameObject paintTypeContextRoot;
        GameObject objectTypeContextRoot;
        GameObject foliageTypeContextRoot;
        GameObject skyboxTypeContextRoot;
        readonly Dictionary<EditorActionMode, Button> actionButtons = new();
        readonly Dictionary<PaintSurfaceKind, Button> paintTypeButtons = new();
        readonly Dictionary<CourseObjectKind, Button> objectTypeButtons = new();
        readonly Dictionary<string, Button> foliageTypeButtons = new();
        readonly Dictionary<string, Button> skyboxTypeButtons = new();
        EditorActionMode currentActionMode = EditorActionMode.Paint;
        PaintSurfaceKind currentPaintSurface = PaintSurfaceKind.Fairway;
        CourseObjectKind currentObjectKind = CourseObjectKind.Tee;
        Button undoButton;
        Button redoButton;
        Toggle autosaveToggle;
        bool autosaveEnabled = true;

        int cachedPar = -1;
        int cachedYardage = -1;
        string cachedHoleName;
        Coroutine autosaveCoroutine;
        Coroutine statusToastCoroutine;

        CourseAuthoringTool cachedTool = (CourseAuthoringTool)(-1);

        enum EditorActionMode
        {
            Paint,
            CourseObject,
            Erase
        }

        enum PaintSurfaceKind
        {
            Fairway,
            Rough,
            Green,
            Water,
            OB
        }

        enum CourseObjectKind
        {
            Tee,
            Basket,
            Foliage,
            Skybox
        }

        void Awake()
        {
            session = CourseEditorSession.Instance;
            if (session.Hole == null)
            {
                Debug.LogError("[CourseEditorHudController] Session hole missing; expected CourseEditorSceneLoader to initialize it.");
                enabled = false;
                return;
            }

            EnsureSessionTheme();

            inputController ??= FindFirstObjectByType<CourseEditorInputController>();
            cameraController ??= FindFirstObjectByType<CourseEditorCameraController>();

            EnsureHudRoots();
            if (editorHudRoot != null)
                editorHudRoot.SetActive(true);
            if (gameplayHudRoot != null)
                gameplayHudRoot.SetActive(false);
            session.Mode = CourseEditorSessionMode.Editing;

            throwInputHandler = FindFirstObjectByType<DiskGolf.Input.ThrowInputHandler>(FindObjectsInactive.Include);
            throwController = FindFirstObjectByType<ThrowController>(FindObjectsInactive.Include);
            gameplayHudController = FindFirstObjectByType<HUDController>(FindObjectsInactive.Include);
            holeSetup = FindFirstObjectByType<HoleSetup>(FindObjectsInactive.Include);

            BuildHud();
            BuildValidationAndPlaytestUi();
            BindControls();
            RefreshHud();
            session.Rebake();
            SetThrowerVisibleForMode(false);
            SelectAction(EditorActionMode.Paint);
            autosaveEnabled = PlayerPrefs.GetInt(AutosaveEnabledPrefKey, 1) == 1;
            if (autosaveToggle != null)
                autosaveToggle.SetIsOnWithoutNotify(autosaveEnabled);
            autosaveCoroutine = StartCoroutine(AutosaveLoop());

            if (CourseEditorNavigation.ConsumePlaytestLaunch())
                StartCoroutine(DeferredQuickPlay());
        }

        IEnumerator DeferredQuickPlay()
        {
            yield return null;
            OnPlaytestClicked();
        }

        void OnDestroy()
        {
            if (autosaveCoroutine != null)
                StopCoroutine(autosaveCoroutine);
            if (statusToastCoroutine != null)
                StopCoroutine(statusToastCoroutine);
        }

        void Update()
        {
            if (session?.Hole == null)
                return;

            RefreshHud();
        }

        void EnsureHudRoots()
        {
            if (editorHudRoot == null)
                editorHudRoot = GameObject.Find("EditorHudCanvas");
            if (editorHudRoot == null)
                editorHudRoot = gameObject;

            if (editorHudRoot == null)
                return;

            var rt = editorHudRoot.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        void ClearRuntimeHud()
        {
            if (editorHudRoot == null)
                return;

            var existing = editorHudRoot.transform.Find("RuntimeEditorHudRoot");
            if (existing != null)
                Destroy(existing.gameObject);
        }

        void BuildHud()
        {
            ClearRuntimeHud();

            var root = CreateUiObject("RuntimeEditorHudRoot", editorHudRoot.transform);
            var rootRt = root.GetComponent<RectTransform>();
            StretchFull(rootRt);
            var rootImg = root.AddComponent<Image>();
            rootImg.color = new Color(BgColor.r, BgColor.g, BgColor.b, 0.08f);
            rootImg.raycastTarget = false;

            BuildTopBar(root.transform);
            BuildRightEditorPanel(root.transform);
            BuildStatus(root.transform);
        }

        void BuildTopBar(Transform parent)
        {
            const float rightPanelWidth = 620f;
            const float rightPanelMargin = 12f;
            const float topBarHeight = 88f;
            const float topBarTopInset = 8f;

            var bar = CreateUiObject("TopBar", parent);
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -(topBarHeight + topBarTopInset));
            rt.offsetMax = new Vector2(0f, -topBarTopInset);

            var barImg = bar.AddComponent<Image>();
            barImg.color = TopBarColor;

            var leftCluster = CreateUiObject("TopBarLeft", bar.transform);
            var leftRt = leftCluster.GetComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0f, 0f);
            leftRt.anchorMax = new Vector2(1f, 1f);
            leftRt.offsetMin = new Vector2(16f, 0f);
            leftRt.offsetMax = new Vector2(-(rightPanelWidth + rightPanelMargin + 8f), 0f);

            var leftRow = leftCluster.AddComponent<HorizontalLayoutGroup>();
            leftRow.spacing = 12f;
            leftRow.padding = new RectOffset(0, 0, 12, 12);
            leftRow.childAlignment = TextAnchor.MiddleLeft;
            leftRow.childForceExpandWidth = false;
            leftRow.childControlWidth = true;
            leftRow.childControlHeight = true;

            holeNameText = CreateTextLabel(leftCluster.transform, "HoleName", "Hole", 28f, new Vector2(380f, 40f), TopBarTextColor);
            holeNameText.alignment = TextAlignmentOptions.MidlineLeft;
            holeNameText.enableWordWrapping = false;
            holeNameText.overflowMode = TextOverflowModes.Ellipsis;
            var holeNameLayout = holeNameText.GetComponent<LayoutElement>();
            holeNameLayout.flexibleWidth = 1f;
            holeNameLayout.minWidth = 280f;

            var parCluster = CreateUiObject("ParCluster", leftCluster.transform);
            var parClusterLayout = parCluster.AddComponent<HorizontalLayoutGroup>();
            parClusterLayout.spacing = 6f;
            parClusterLayout.childAlignment = TextAnchor.MiddleLeft;
            parClusterLayout.childControlWidth = false;
            parClusterLayout.childControlHeight = true;
            var parClusterElement = parCluster.AddComponent<LayoutElement>();
            parClusterElement.minHeight = 40f;

            var parLabel = CreateTextLabel(parCluster.transform, "ParLabel", "Par", 22f, new Vector2(40f, 40f), TopBarTextColor);
            parLabel.enableWordWrapping = false;
            parLabel.overflowMode = TextOverflowModes.Overflow;
            CreateParStepper(parCluster.transform);

            yardageText = CreateTextLabel(parCluster.transform, "Yardage", "0 yd", 22f, new Vector2(72f, 40f), TopBarTextColor);
            yardageText.enableWordWrapping = false;
            yardageText.overflowMode = TextOverflowModes.Overflow;

            var rightCluster = CreateUiObject("TopBarRight", bar.transform);
            var rightRt = rightCluster.GetComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(1f, 0f);
            rightRt.anchorMax = new Vector2(1f, 1f);
            rightRt.pivot = new Vector2(1f, 0.5f);
            rightRt.offsetMin = new Vector2(-(rightPanelWidth + rightPanelMargin), 0f);
            rightRt.offsetMax = new Vector2(-rightPanelMargin, 0f);

            var rightRow = rightCluster.AddComponent<HorizontalLayoutGroup>();
            rightRow.spacing = 8f;
            rightRow.padding = new RectOffset(0, 0, 12, 12);
            rightRow.childAlignment = TextAnchor.MiddleCenter;
            rightRow.childForceExpandWidth = false;
            rightRow.childControlWidth = false;
            rightRow.childControlHeight = true;

            CreateAutosaveToggle(rightCluster.transform);
            CreateButton(rightCluster.transform, "Exit", new Vector2(88f, 40f), OnBackToHubClicked, TopBarTextColor);
            CreateButton(rightCluster.transform, "Save", new Vector2(88f, 40f), OnSaveClicked, TopBarTextColor);
            undoButton = CreateButton(rightCluster.transform, "Undo", new Vector2(88f, 40f), OnUndoClicked, TopBarTextColor);
            redoButton = CreateButton(rightCluster.transform, "Redo", new Vector2(88f, 40f), OnRedoClicked, TopBarTextColor);
            CreateButton(rightCluster.transform, "Play", new Vector2(88f, 40f), OnPlaytestClicked, TopBarTextColor);
        }

        void BuildRightEditorPanel(Transform parent)
        {
            var panel = CreateUiObject("RightEditorPanel", parent);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-12f, -96f);
            rt.sizeDelta = new Vector2(620f, 0f);

            var panelImg = panel.AddComponent<Image>();
            panelImg.color = PanelColor;

            var fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var column = panel.AddComponent<VerticalLayoutGroup>();
            column.spacing = 8f;
            column.padding = new RectOffset(12, 12, 10, 10);
            column.childAlignment = TextAnchor.UpperLeft;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            CreateSectionLabel(panel.transform, "Camera");
            var cameraRow = CreateButtonRow(panel.transform);
            CreateButton(cameraRow.transform, "Overview", new Vector2(156f, 34f), () => cameraController?.SetOverviewPreset(), labelPaddingH: 18f);
            CreateButton(cameraRow.transform, "Tee", new Vector2(96f, 34f), () => cameraController?.SetTeePreset(), labelPaddingH: 14f);
            CreateButton(cameraRow.transform, "Basket", new Vector2(116f, 34f), () => cameraController?.SetBasketPreset(), labelPaddingH: 14f);
            CreateButton(cameraRow.transform, "Top Down", new Vector2(156f, 34f), () => cameraController?.SetTopDownPreset(), labelPaddingH: 18f);

            CreateSectionLabel(panel.transform, "Action");
            var actionRow = CreateButtonRow(panel.transform);
            actionButtons[EditorActionMode.Paint] = CreateButton(actionRow.transform, "Paint", new Vector2(96f, 34f),
                () => SelectAction(EditorActionMode.Paint));
            actionButtons[EditorActionMode.CourseObject] = CreateButton(actionRow.transform, "Object", new Vector2(96f, 34f),
                () => SelectAction(EditorActionMode.CourseObject));
            actionButtons[EditorActionMode.Erase] = CreateButton(actionRow.transform, "Erase", new Vector2(96f, 34f),
                () => SelectAction(EditorActionMode.Erase));

            CreateSectionLabel(panel.transform, "Type");
            typePanelRoot = CreateUiObject("TypePanel", panel.transform);
            var typePanelLayout = typePanelRoot.AddComponent<LayoutElement>();
            typePanelLayout.minHeight = 40f;
            var typeColumn = typePanelRoot.AddComponent<VerticalLayoutGroup>();
            typeColumn.spacing = 6f;
            typeColumn.childControlWidth = false;
            typeColumn.childControlHeight = true;
            typeColumn.childForceExpandWidth = false;
            typeColumn.childAlignment = TextAnchor.UpperLeft;

            paintTypeContextRoot = CreateUiObject("PaintTypeContext", typePanelRoot.transform);
            paintTypeContextRoot.AddComponent<HorizontalLayoutGroup>();
            ConfigureButtonRow(paintTypeContextRoot.GetComponent<HorizontalLayoutGroup>());
            paintTypeButtons[PaintSurfaceKind.Fairway] = CreateButton(paintTypeContextRoot.transform, "Fairway", new Vector2(112f, 34f),
                () => SelectPaintSurface(PaintSurfaceKind.Fairway));
            paintTypeButtons[PaintSurfaceKind.Rough] = CreateButton(paintTypeContextRoot.transform, "Rough", new Vector2(100f, 34f),
                () => SelectPaintSurface(PaintSurfaceKind.Rough));
            paintTypeButtons[PaintSurfaceKind.Green] = CreateButton(paintTypeContextRoot.transform, "Green", new Vector2(96f, 34f),
                () => SelectPaintSurface(PaintSurfaceKind.Green));
            paintTypeButtons[PaintSurfaceKind.Water] = CreateButton(paintTypeContextRoot.transform, "Water", new Vector2(96f, 34f),
                () => SelectPaintSurface(PaintSurfaceKind.Water));
            paintTypeButtons[PaintSurfaceKind.OB] = CreateButton(paintTypeContextRoot.transform, "OB", new Vector2(72f, 34f),
                () => SelectPaintSurface(PaintSurfaceKind.OB));

            objectTypeContextRoot = CreateUiObject("ObjectTypeContext", typePanelRoot.transform);
            objectTypeContextRoot.AddComponent<HorizontalLayoutGroup>();
            ConfigureButtonRow(objectTypeContextRoot.GetComponent<HorizontalLayoutGroup>());
            objectTypeButtons[CourseObjectKind.Tee] = CreateButton(objectTypeContextRoot.transform, "Tee", new Vector2(84f, 34f),
                () => SelectCourseObject(CourseObjectKind.Tee));
            objectTypeButtons[CourseObjectKind.Basket] = CreateButton(objectTypeContextRoot.transform, "Basket", new Vector2(100f, 34f),
                () => SelectCourseObject(CourseObjectKind.Basket));
            objectTypeButtons[CourseObjectKind.Foliage] = CreateButton(objectTypeContextRoot.transform, "Foliage", new Vector2(108f, 34f),
                () => SelectCourseObject(CourseObjectKind.Foliage));
            objectTypeButtons[CourseObjectKind.Skybox] = CreateButton(objectTypeContextRoot.transform, "Skybox", new Vector2(108f, 34f),
                () => SelectCourseObject(CourseObjectKind.Skybox));

            foliageTypeContextRoot = CreateUiObject("FoliageTypeContext", typePanelRoot.transform);
            var foliageRow = foliageTypeContextRoot.AddComponent<HorizontalLayoutGroup>();
            ConfigureButtonRow(foliageRow);
            BuildFoliageTypePicker(foliageTypeContextRoot.transform);

            skyboxTypeContextRoot = CreateUiObject("SkyboxTypeContext", typePanelRoot.transform);
            var skyboxRow = skyboxTypeContextRoot.AddComponent<HorizontalLayoutGroup>();
            ConfigureButtonRow(skyboxRow);
            BuildSkyboxTypePicker(skyboxTypeContextRoot.transform);
        }

        void BuildFoliageTypePicker(Transform parent)
        {
            foliageTypeButtons.Clear();
            if (session?.Theme?.foliage == null)
                return;

            foreach (var entry in session.Theme.foliage)
            {
                if (entry == null || string.IsNullOrEmpty(entry.archetypeId))
                    continue;

                string archetypeId = entry.archetypeId;
                var button = CreateFoliageIconButton(parent, entry);
                foliageTypeButtons[archetypeId] = button;
                button.onClick.AddListener(() => SelectFoliageArchetype(archetypeId));
            }

            EnsureFoliageArchetype();
        }

        void SelectFoliageArchetype(string archetypeId)
        {
            session.Authoring.FoliageArchetype = archetypeId;
            SelectCourseObject(CourseObjectKind.Foliage);
        }

        static Button CreateFoliageIconButton(Transform parent, FoliageArchetypeEntry entry)
        {
            var shell = CreateSelectableIconShell(parent, "Foliage_" + entry.archetypeId);
            var image = shell.content.gameObject.AddComponent<Image>();
            StretchRect(image.rectTransform);
            image.color = Color.white;
            image.preserveAspect = true;
            if (entry.sprite != null)
                image.sprite = entry.sprite;
            else
                image.color = DimButtonColor;

            return shell.button;
        }

        void TintFoliageButton(Button button, bool selected)
        {
            SetSelectableIconSelected(button, selected);
        }

        void BuildSkyboxTypePicker(Transform parent)
        {
            skyboxTypeButtons.Clear();
            if (session?.Theme?.skyboxes == null)
                return;

            foreach (var entry in session.Theme.skyboxes)
            {
                if (entry == null || string.IsNullOrEmpty(entry.skyboxId))
                    continue;

                string skyboxId = entry.skyboxId;
                var button = CreateSkyboxIconButton(parent, entry);
                skyboxTypeButtons[skyboxId] = button;
                button.onClick.AddListener(() => SelectSkybox(skyboxId));
            }

            EnsureSkyboxId();
        }

        void SelectSkybox(string skyboxId)
        {
            if (session?.Hole == null || string.IsNullOrEmpty(skyboxId))
                return;

            if (session.Hole.Hole.SkyboxId == skyboxId)
            {
                SelectCourseObject(CourseObjectKind.Skybox);
                return;
            }

            session.Commands.Execute(new SetSkyboxCommand(session.Hole, skyboxId));
            SceneEnvironmentApplier.Apply(session.Hole, session.Theme);
            session.IsDirty = true;
            session.Authoring.IsDirty = true;
            SelectCourseObject(CourseObjectKind.Skybox);
        }

        static Button CreateSkyboxIconButton(Transform parent, SkyboxArchetypeEntry entry)
        {
            var shell = CreateSelectableIconShell(parent, "Skybox_" + entry.skyboxId);
            if (entry.preview != null)
            {
                var image = shell.content.gameObject.AddComponent<Image>();
                StretchRect(image.rectTransform);
                image.color = Color.white;
                image.preserveAspect = true;
                image.sprite = entry.preview;
            }
            else
            {
                var previewTexture = ThemeArtPreview.GetMainTexture(entry.material);
                if (previewTexture != null)
                {
                    var rawImage = shell.content.gameObject.AddComponent<RawImage>();
                    StretchRect(rawImage.rectTransform);
                    rawImage.texture = previewTexture;
                    rawImage.uvRect = ThemeArtPreview.HorizonBandUvRect;
                    rawImage.color = Color.white;
                }
                else
                {
                    var image = shell.content.gameObject.AddComponent<Image>();
                    StretchRect(image.rectTransform);
                    image.color = DimButtonColor;
                }
            }

            return shell.button;
        }

        void TintSkyboxButton(Button button, bool selected)
        {
            SetSelectableIconSelected(button, selected);
        }

        sealed class SelectableIconShell
        {
            public Button button;
            public RectTransform content;
        }

        static SelectableIconShell CreateSelectableIconShell(Transform parent, string name)
        {
            const float size = 52f;
            var go = CreateUiObject(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, 0f);

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = size;
            layout.minWidth = size;
            layout.preferredHeight = size;
            layout.minHeight = size;

            var border = go.AddComponent<Image>();
            border.color = Color.clear;
            border.raycastTarget = true;

            var button = go.AddComponent<Button>();
            button.targetGraphic = border;

            var contentGo = CreateUiObject("Content", go.transform);
            var content = contentGo.GetComponent<RectTransform>();
            StretchRect(content);

            var outline = go.AddComponent<SelectableIconOutline>();
            outline.border = border;
            outline.content = content;

            return new SelectableIconShell { button = button, content = content };
        }

        void SetSelectableIconSelected(Button button, bool selected)
        {
            if (button == null)
                return;

            var outline = button.GetComponent<SelectableIconOutline>();
            if (outline != null)
            {
                outline.SetSelected(selected, AccentColor);
                return;
            }

            if (button.targetGraphic is Image image)
                image.color = selected ? AccentColor : Color.white;
        }

        static void StretchRect(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        static void ConfigureButtonRow(HorizontalLayoutGroup layout)
        {
            layout.spacing = 6f;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;
        }

        static GameObject CreateButtonRow(Transform parent)
        {
            var row = CreateUiObject("ButtonRow", parent);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            var element = row.AddComponent<LayoutElement>();
            element.minHeight = 38f;
            element.preferredWidth = 580f;
            return row;
        }

        static TextMeshProUGUI CreateSectionLabel(Transform parent, string text)
        {
            var label = CreateTextLabel(parent, text + "Label", text.ToUpperInvariant(), 15f, new Vector2(120f, 20f), LabelColor);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontStyle = FontStyles.Bold;
            var layout = label.GetComponent<LayoutElement>();
            if (layout != null)
                layout.minHeight = 20f;
            return label;
        }

        void BuildStatus(Transform parent)
        {
            var status = CreateUiObject("StatusText", parent);
            var rt = status.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 24f);
            rt.sizeDelta = new Vector2(920f, 28f);

            statusText = status.AddComponent<TextMeshProUGUI>();
            statusText.text = string.Empty;
            statusText.color = Color.white;
            statusText.fontSize = 22f;
            statusText.alignment = TextAlignmentOptions.Center;
            HudTypography.BindFont(statusText);
        }

        void BuildValidationAndPlaytestUi()
        {
            if (editorHudRoot == null)
                return;

            validationDrawer = gameObject.AddComponent<ValidationDrawerController>();
            validationDrawer.Initialize(editorHudRoot.transform, session, cameraController);
            validationDrawer.SetPlayAnywayHandler(EnterPlaytest);

            playtestOverlay = gameObject.AddComponent<CourseEditorPlaytestOverlay>();
            var overlayParent = gameplayHudRoot != null ? gameplayHudRoot.transform : editorHudRoot.transform;
            playtestOverlay.Initialize(overlayParent);

            playtestController = gameObject.AddComponent<CourseEditorPlaytestController>();
            playtestController.Configure(
                session,
                editorHudRoot,
                gameplayHudRoot,
                playtestOverlay,
                throwInputHandler,
                throwController,
                gameplayHudController,
                this);
        }

        void BindControls()
        {
            // Par stepper wired in CreateParStepper.
        }

        void RefreshHud()
        {
            var hole = session.Hole;
            if (hole == null)
                return;

            SyncActionModeFromActiveTool();

            int par = hole.Hole.Par;
            int yardage = Mathf.RoundToInt(hole.HoleLengthYards());
            string holeName = hole.Name;

            if (par != cachedPar)
            {
                cachedPar = par;
                if (parValueText != null)
                    parValueText.text = par.ToString();
                if (parDecreaseButton != null)
                    parDecreaseButton.interactable = par > 1;
                if (parIncreaseButton != null)
                    parIncreaseButton.interactable = par < 6;
            }

            if (yardage != cachedYardage && yardageText != null)
            {
                yardageText.text = $"{yardage} yd";
                cachedYardage = yardage;
            }

            if (holeName != cachedHoleName && holeNameText != null)
            {
                holeNameText.text = string.IsNullOrEmpty(holeName) ? "Untitled Hole" : holeName;
                cachedHoleName = holeName;
            }

            if (undoButton != null)
                undoButton.interactable = session.Commands.CanUndo;
            if (redoButton != null)
                redoButton.interactable = session.Commands.CanRedo;

            bool paintAction = currentActionMode == EditorActionMode.Paint;
            bool objectAction = currentActionMode == EditorActionMode.CourseObject;
            if (typePanelRoot != null)
                typePanelRoot.SetActive(currentActionMode != EditorActionMode.Erase);
            if (paintTypeContextRoot != null)
                paintTypeContextRoot.SetActive(paintAction);
            if (objectTypeContextRoot != null)
                objectTypeContextRoot.SetActive(objectAction);
            if (foliageTypeContextRoot != null)
                foliageTypeContextRoot.SetActive(objectAction && currentObjectKind == CourseObjectKind.Foliage);
            if (skyboxTypeContextRoot != null)
                skyboxTypeContextRoot.SetActive(objectAction && currentObjectKind == CourseObjectKind.Skybox);

            foreach (var entry in actionButtons)
                TintButton(entry.Value, currentActionMode == entry.Key);

            foreach (var entry in paintTypeButtons)
            {
                bool selected = paintAction && IsPaintSurfaceSelected(entry.Key);
                TintButton(entry.Value, selected);
            }

            foreach (var entry in objectTypeButtons)
            {
                bool selected = objectAction && currentObjectKind == entry.Key;
                TintButton(entry.Value, selected);
            }

            foreach (var entry in foliageTypeButtons)
            {
                bool selected = objectAction && currentObjectKind == CourseObjectKind.Foliage
                    && session.Authoring.FoliageArchetype == entry.Key;
                TintFoliageButton(entry.Value, selected);
            }

            foreach (var entry in skyboxTypeButtons)
            {
                bool selected = objectAction && currentObjectKind == CourseObjectKind.Skybox
                    && session.Hole.Hole.SkyboxId == entry.Key;
                TintSkyboxButton(entry.Value, selected);
            }

            if (cachedTool != session.Authoring.ActiveTool)
            {
                cachedTool = session.Authoring.ActiveTool;
                SetStatus(GetToolHint(session.Authoring.ActiveTool));
            }
        }

        void SyncActionModeFromActiveTool()
        {
            switch (session.Authoring.ActiveTool)
            {
                case CourseAuthoringTool.Erase:
                    currentActionMode = EditorActionMode.Erase;
                    break;
                case CourseAuthoringTool.HoleTee:
                    currentActionMode = EditorActionMode.CourseObject;
                    currentObjectKind = CourseObjectKind.Tee;
                    break;
                case CourseAuthoringTool.HoleBasket:
                    currentActionMode = EditorActionMode.CourseObject;
                    currentObjectKind = CourseObjectKind.Basket;
                    break;
                case CourseAuthoringTool.Foliage:
                    currentActionMode = EditorActionMode.CourseObject;
                    currentObjectKind = CourseObjectKind.Foliage;
                    break;
                case CourseAuthoringTool.Skybox:
                    currentActionMode = EditorActionMode.CourseObject;
                    currentObjectKind = CourseObjectKind.Skybox;
                    break;
                case CourseAuthoringTool.Hazard:
                    currentActionMode = EditorActionMode.Paint;
                    currentPaintSurface = session.Authoring.HazardBrushType == HazardType.OB
                        ? PaintSurfaceKind.OB
                        : PaintSurfaceKind.Water;
                    break;
                default:
                    currentActionMode = EditorActionMode.Paint;
                    currentPaintSurface = session.Authoring.BrushType switch
                    {
                        SurfaceTileType.Rough => PaintSurfaceKind.Rough,
                        SurfaceTileType.Green => PaintSurfaceKind.Green,
                        _ => PaintSurfaceKind.Fairway
                    };
                    break;
            }
        }

        bool IsPaintSurfaceSelected(PaintSurfaceKind surface) => surface switch
        {
            PaintSurfaceKind.Fairway => session.Authoring.ActiveTool == CourseAuthoringTool.Paint
                && session.Authoring.BrushType == SurfaceTileType.Fairway,
            PaintSurfaceKind.Rough => session.Authoring.ActiveTool == CourseAuthoringTool.Paint
                && session.Authoring.BrushType == SurfaceTileType.Rough,
            PaintSurfaceKind.Green => session.Authoring.ActiveTool == CourseAuthoringTool.Paint
                && session.Authoring.BrushType == SurfaceTileType.Green,
            PaintSurfaceKind.Water => session.Authoring.ActiveTool == CourseAuthoringTool.Hazard
                && session.Authoring.HazardBrushType == HazardType.Water,
            PaintSurfaceKind.OB => session.Authoring.ActiveTool == CourseAuthoringTool.Hazard
                && session.Authoring.HazardBrushType == HazardType.OB,
            _ => false
        };

        static string GetToolHint(CourseAuthoringTool tool) => tool switch
        {
            CourseAuthoringTool.Paint => "Paint fairway, rough, or green on the ground.",
            CourseAuthoringTool.Erase => "Erase tiles, hazards, foliage, tee, or basket under the cursor.",
            CourseAuthoringTool.HoleTee => "Click the ground to place the tee pad.",
            CourseAuthoringTool.HoleBasket => "Click the ground to place the basket.",
            CourseAuthoringTool.Foliage => "Click the ground to place foliage.",
            CourseAuthoringTool.Skybox => "Pick a skybox for this hole.",
            CourseAuthoringTool.Hazard => "Paint water or OB tiles on the ground.",
            _ => string.Empty
        };

        void SelectAction(EditorActionMode action)
        {
            currentActionMode = action;
            switch (action)
            {
                case EditorActionMode.Paint:
                    SelectPaintSurface(currentPaintSurface);
                    break;
                case EditorActionMode.CourseObject:
                    SelectCourseObject(currentObjectKind);
                    break;
                case EditorActionMode.Erase:
                    session.Authoring.ActiveTool = CourseAuthoringTool.Erase;
                    SetStatus(GetToolHint(CourseAuthoringTool.Erase));
                    break;
            }
        }

        void SelectPaintSurface(PaintSurfaceKind surface)
        {
            currentActionMode = EditorActionMode.Paint;
            currentPaintSurface = surface;

            if (surface == PaintSurfaceKind.Water || surface == PaintSurfaceKind.OB)
            {
                session.Authoring.ActiveTool = CourseAuthoringTool.Hazard;
                session.Authoring.HazardBrushType = surface == PaintSurfaceKind.OB
                    ? HazardType.OB
                    : HazardType.Water;
            }
            else
            {
                session.Authoring.ActiveTool = CourseAuthoringTool.Paint;
                session.Authoring.BrushType = surface switch
                {
                    PaintSurfaceKind.Rough => SurfaceTileType.Rough,
                    PaintSurfaceKind.Green => SurfaceTileType.Green,
                    _ => SurfaceTileType.Fairway
                };
            }

            SetStatus(GetToolHint(session.Authoring.ActiveTool));
        }

        void SelectCourseObject(CourseObjectKind kind)
        {
            currentActionMode = EditorActionMode.CourseObject;
            currentObjectKind = kind;
            if (kind == CourseObjectKind.Foliage)
                EnsureFoliageArchetype();
            if (kind == CourseObjectKind.Skybox)
                EnsureSkyboxId();

            session.Authoring.ActiveTool = kind switch
            {
                CourseObjectKind.Basket => CourseAuthoringTool.HoleBasket,
                CourseObjectKind.Foliage => CourseAuthoringTool.Foliage,
                CourseObjectKind.Skybox => CourseAuthoringTool.Skybox,
                _ => CourseAuthoringTool.HoleTee
            };

            SetStatus(GetToolHint(session.Authoring.ActiveTool));
        }

        void EnsureSkyboxId()
        {
            if (session.Theme?.skyboxes == null || session.Theme.skyboxes.Count == 0)
            {
                session.Hole.Hole.SkyboxId = "sky_clear";
                return;
            }

            foreach (var entry in session.Theme.skyboxes)
            {
                if (entry != null && entry.skyboxId == session.Hole.Hole.SkyboxId)
                    return;
            }

            session.Hole.Hole.SkyboxId = session.Theme.skyboxes[0].skyboxId;
        }

        void EnsureFoliageArchetype()
        {
            if (session.Theme?.foliage == null || session.Theme.foliage.Count == 0)
            {
                session.Authoring.FoliageArchetype = "tree_round";
                return;
            }

            foreach (var entry in session.Theme.foliage)
            {
                if (entry != null && entry.archetypeId == session.Authoring.FoliageArchetype)
                    return;
            }

            session.Authoring.FoliageArchetype = session.Theme.foliage[0].archetypeId;
        }

        public void OnExitPlaytest()
        {
            if (statusToastCoroutine != null)
            {
                StopCoroutine(statusToastCoroutine);
                statusToastCoroutine = null;
            }

            if (session?.Hole != null)
                SetStatus(GetToolHint(session.Authoring.ActiveTool));
            else if (statusText != null)
                statusText.text = string.Empty;
        }

        void SetThrowerVisibleForMode(bool playtesting)
        {
            holeSetup ??= FindFirstObjectByType<HoleSetup>(FindObjectsInactive.Include);
            if (holeSetup?.Thrower == null)
                return;

            holeSetup.Thrower.gameObject.SetActive(playtesting);
        }

        void EnsureSessionTheme()
        {
            if (session.Hole == null || session.Theme != null)
                return;

            var theme = ThemePackLoader.Load(session.Hole.ThemeId);
            if (theme == null)
            {
                Debug.LogError("[CourseEditorHud] Temperate theme failed to load. Run Disk Golf → Course → Fix Theme Pack Registry.");
                SetStatus("Theme missing — run Fix Theme Pack Registry in Unity menu.");
                return;
            }

            session.Load(session.Hole, theme, session.ActiveHoleId ?? session.Hole.Id);
        }

        void OnBackToHubClicked()
        {
            if (session.IsDirty && !confirmDiscardArmed)
            {
                confirmDiscardArmed = true;
                SetStatus("Unsaved changes. Press Exit again to discard.");
                return;
            }

            SceneLoader.Load(SceneFlow.CourseEditorHub);
        }

        void OnSaveClicked()
        {
            if (session.Hole == null)
                return;

            HoleDataCatalog.Player.Save(session.Hole, published: false);
            session.IsDirty = false;
            confirmDiscardArmed = false;
            SetStatus("Saved.");
        }

        void OnExportClicked()
        {
#if UNITY_STANDALONE_WIN
            if (session.Hole == null)
                return;

            string suggested = string.IsNullOrEmpty(session.Hole.Id) ? "hole" : session.Hole.Id;
            if (!WindowsFileDialog.TrySaveJsonFile(suggested, out string path))
                return;

            try
            {
                HoleDataJson.SaveToFile(session.Hole, path);
                SetStatus("Exported.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CourseEditorHud] Export failed: {ex.Message}");
                SetStatus("Export failed.");
            }
#else
            SetStatus("Export is PC Windows only.");
#endif
        }

        void OnUndoClicked()
        {
            session.Commands.Undo();
            AfterCommandMutation();
        }

        void OnRedoClicked()
        {
            session.Commands.Redo();
            AfterCommandMutation();
        }

        void OnPlaytestClicked()
        {
            var result = CourseValidator.Validate(session.Hole);
            validationDrawer?.Hide();
            CourseValidator.LogToConsole(result);

            if (!result.CanPlaytest)
            {
                validationDrawer?.Show(result, ValidationDrawerMode.BlockingErrors, focusFirstError: true);
                SetStatus("Playtest blocked. Fix must-fix items.");
                return;
            }

            bool hasWarnings = false;
            foreach (var message in result.Messages)
            {
                if (message.Severity == ValidationSeverity.Warning)
                {
                    hasWarnings = true;
                    break;
                }
            }

            if (hasWarnings)
            {
                validationDrawer?.Show(result, ValidationDrawerMode.WarningsConfirm);
                return;
            }

            EnterPlaytest();
        }

        void ChangePar(int delta)
        {
            if (session?.Hole == null)
                return;

            int newPar = Mathf.Clamp(session.Hole.Hole.Par + delta, 1, 6);
            if (newPar == session.Hole.Hole.Par)
                return;

            session.Hole.Hole.Par = newPar;
            cachedPar = -1;
            AfterCommandMutation();
        }

        void AfterCommandMutation()
        {
            confirmDiscardArmed = false;
            inputController?.NotifyMutation();
            if (inputController == null)
            {
                session.IsDirty = true;
                session.Authoring.IsDirty = true;
                session.Rebake();
            }
        }

        void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        IEnumerator AutosaveLoop()
        {
            var wait = new WaitForSeconds(AutosaveIntervalSeconds);
            while (true)
            {
                yield return wait;
                TryAutosave();
            }
        }

        void TryAutosave()
        {
            if (!autosaveEnabled)
                return;
            if (session?.Hole == null || !session.IsDirty)
                return;
            if (session.Mode == CourseEditorSessionMode.Playtesting)
                return;

            HoleDataCatalog.Player.Save(session.Hole, published: false);
            ShowStatusToast("Autosaved.");
        }

        void SaveBeforePlaytest()
        {
            if (session?.Hole == null)
                return;

            HoleDataCatalog.Player.Save(session.Hole, published: false);
        }

        void ShowStatusToast(string message)
        {
            if (statusToastCoroutine != null)
                StopCoroutine(statusToastCoroutine);
            statusToastCoroutine = StartCoroutine(StatusToastRoutine(message));
        }

        IEnumerator StatusToastRoutine(string message)
        {
            SetStatus(message);
            yield return new WaitForSeconds(StatusToastSeconds);
            if (statusText != null && statusText.text == message)
                statusText.text = string.Empty;
            statusToastCoroutine = null;
        }

        void EnterPlaytest()
        {
            validationDrawer?.Hide();
            SaveBeforePlaytest();

            session.Mode = CourseEditorSessionMode.Playtesting;
            cameraController?.SetEditorCameraActive(false);
            session.Rebake();
            holeSetup ??= FindFirstObjectByType<HoleSetup>(FindObjectsInactive.Include);
            holeSetup?.PositionThrowerAtTee();
            SetThrowerVisibleForMode(true);
            playtestController?.EnterPlaytest();
        }

        static void TintButton(Button button, bool selected)
        {
            if (button == null || button.targetGraphic == null)
                return;

            var image = button.targetGraphic as Image;
            if (image != null)
                image.color = selected ? AccentColor : DimButtonColor;

            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.color = selected ? Color.white : new Color(0.12f, 0.16f, 0.14f, 1f);
        }

        static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 size)
        {
            var panel = CreateUiObject(name, parent);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            var img = panel.AddComponent<Image>();
            img.color = PanelColor;
            return panel;
        }

        static TextMeshProUGUI CreateTextLabel(Transform parent, string name, string text, float fontSize, Vector2 size,
            Color? textColor = null)
        {
            var go = CreateUiObject(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size.x, 0f);

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.minWidth = size.x;
            layout.minHeight = size.y;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = textColor ?? Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            HudTypography.BindFont(tmp);
            return tmp;
        }

        void CreateParStepper(Transform parent)
        {
            var row = CreateUiObject("ParStepper", parent);
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0f, 0f);
            rowRt.anchorMax = new Vector2(0f, 1f);
            rowRt.pivot = new Vector2(0.5f, 0.5f);
            rowRt.sizeDelta = new Vector2(88f, 0f);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 0f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            var rowElement = row.AddComponent<LayoutElement>();
            rowElement.preferredWidth = 88f;
            rowElement.minWidth = 88f;
            rowElement.minHeight = 40f;

            parDecreaseButton = CreateButton(row.transform, "<", new Vector2(28f, 40f), () => ChangePar(-1), TopBarTextColor, 2f);
            parValueText = CreateTextLabel(row.transform, "ParValue", "3", 22f, new Vector2(28f, 40f), TopBarTextColor);
            parValueText.alignment = TextAlignmentOptions.Center;
            parIncreaseButton = CreateButton(row.transform, ">", new Vector2(28f, 40f), () => ChangePar(1), TopBarTextColor, 2f);
        }

        void CreateAutosaveToggle(Transform parent)
        {
            var row = CreateUiObject("AutosaveToggle", parent);
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(132f, 0f);

            var layout = row.AddComponent<LayoutElement>();
            layout.preferredWidth = 132f;
            layout.minWidth = 132f;
            layout.minHeight = 40f;

            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 6f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = true;
            rowLayout.padding = new RectOffset(0, 0, 0, 0);

            var label = CreateTextLabel(row.transform, "AutosaveLabel", "Autosave", 17f, new Vector2(72f, 40f), TopBarTextColor);
            label.alignment = TextAlignmentOptions.MidlineRight;

            var toggleGo = CreateUiObject("Checkbox", row.transform);
            var toggleRt = toggleGo.GetComponent<RectTransform>();
            toggleRt.sizeDelta = new Vector2(28f, 28f);
            var toggleLayout = toggleGo.AddComponent<LayoutElement>();
            toggleLayout.preferredWidth = 28f;
            toggleLayout.minWidth = 28f;
            toggleLayout.preferredHeight = 28f;
            toggleLayout.minHeight = 28f;

            var background = toggleGo.AddComponent<Image>();
            background.color = DimButtonColor;

            var checkmarkGo = CreateUiObject("Checkmark", toggleGo.transform);
            var checkmarkRt = checkmarkGo.GetComponent<RectTransform>();
            checkmarkRt.anchorMin = Vector2.zero;
            checkmarkRt.anchorMax = Vector2.one;
            checkmarkRt.offsetMin = new Vector2(5f, 5f);
            checkmarkRt.offsetMax = new Vector2(-5f, -5f);
            var checkmark = checkmarkGo.AddComponent<Image>();
            checkmark.color = AccentColor;

            autosaveToggle = toggleGo.AddComponent<Toggle>();
            autosaveToggle.targetGraphic = background;
            autosaveToggle.graphic = checkmark;
            autosaveToggle.isOn = autosaveEnabled;
            autosaveToggle.onValueChanged.AddListener(OnAutosaveToggleChanged);
        }

        void OnAutosaveToggleChanged(bool enabled)
        {
            autosaveEnabled = enabled;
            PlayerPrefs.SetInt(AutosaveEnabledPrefKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        static Button CreateButton(Transform parent, string label, Vector2 size, UnityEngine.Events.UnityAction onClick,
            Color? labelColor = null, float labelPaddingH = 8f)
        {
            var go = CreateUiObject(label.Replace(" ", string.Empty) + "Button", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size.x, 0f);

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.minWidth = size.x;
            layout.minHeight = size.y;

            var image = go.AddComponent<Image>();
            image.color = DimButtonColor;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            if (onClick != null)
                button.onClick.AddListener(onClick);

            var labelGo = CreateUiObject("Label", go.transform);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(labelPaddingH, 4f);
            labelRt.offsetMax = new Vector2(-labelPaddingH, -4f);

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 17f;
            tmp.color = labelColor ?? new Color(0.12f, 0.16f, 0.14f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            HudTypography.BindFont(tmp);

            return button;
        }

        static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }
}
