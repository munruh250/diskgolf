using System;

using System.Collections.Generic;

using DiskGolf.Core;

using DiskGolf.CourseEditor;

using DiskGolf.CourseEditor.Platform;

using DiskGolf.UI;

using TMPro;

using UnityEngine;

using UnityEngine.UI;



namespace DiskGolf.UI.CourseEditor

{

    public sealed class CourseEditorHubController : MonoBehaviour

    {

        static readonly Color PanelColor = new(0.12f, 0.18f, 0.14f, 0.96f);

        static readonly Color AccentColor = new(0.28f, 0.78f, 0.36f, 1f);

        static readonly Color ModalBackdropColor = new(0f, 0f, 0f, 0.72f);

        static readonly Color DeleteAccentColor = new(0.82f, 0.28f, 0.24f, 1f);



        const float MinListWidth = 1240f;

        const float ScrollSensitivity = 50f;

        const float CardButtonWidth = 92f;

        const float CardExportButtonWidth = 100f;

        const float CardDeleteButtonWidth = 100f;



        [SerializeField] Button backButton;

        [SerializeField] Button newHoleButton;

        [SerializeField] Button importButton;

        [SerializeField] RectTransform holeListContent;

        [SerializeField] NewHoleWizardController wizard;



        readonly Dictionary<string, HoleData> loadedHoles = new();



        GameObject deleteModalRoot;

        TextMeshProUGUI deleteModalBody;

        string pendingDeleteId;



        void Awake()

        {

            if (backButton != null)

            {

                backButton.onClick.AddListener(() => SceneLoader.Load(SceneFlow.MainMenu));

                var backLabel = backButton.GetComponentInChildren<TextMeshProUGUI>();

                if (backLabel != null)

                    backLabel.text = "Back";

            }



            if (newHoleButton != null)

                newHoleButton.onClick.AddListener(OnNewHole);



            if (importButton != null)

                importButton.onClick.AddListener(OnImport);



            ConfigureHoleListScroll();

            EnsureDeleteModal();

        }



        void OnEnable()

        {

            RefreshHoleList();

        }



        void ConfigureHoleListScroll()

        {

            if (holeListContent == null)

                return;



            var scroll = holeListContent.GetComponentInParent<ScrollRect>();

            if (scroll == null)

                return;



            scroll.scrollSensitivity = ScrollSensitivity;

            scroll.inertia = true;

            scroll.decelerationRate = 0.135f;



            var scrollRt = scroll.transform as RectTransform;

            if (scrollRt != null && scrollRt.sizeDelta.x < MinListWidth)

                scrollRt.sizeDelta = new Vector2(MinListWidth, Mathf.Max(scrollRt.sizeDelta.y, 320f));



            if (scroll.viewport != null)

            {

                var viewport = scroll.viewport;

                viewport.offsetMin = new Vector2(8f, 8f);

                viewport.offsetMax = new Vector2(scroll.verticalScrollbar != null ? -26f : -8f, -8f);

            }



            if (scroll.verticalScrollbar == null)

                scroll.verticalScrollbar = EnsureVerticalScrollbar(scrollRt, scroll);



            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;



            var contentLayout = holeListContent.GetComponent<LayoutElement>();

            if (contentLayout == null)

                contentLayout = holeListContent.gameObject.AddComponent<LayoutElement>();

            contentLayout.minWidth = MinListWidth - 48f;

        }



        static Scrollbar EnsureVerticalScrollbar(RectTransform scrollRt, ScrollRect scroll)

        {

            if (scrollRt == null)

                return null;



            var existing = scrollRt.Find("VerticalScrollbar")?.GetComponent<Scrollbar>();

            if (existing != null)

                return existing;



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



            scroll.verticalScrollbar = scrollbarGo.GetComponent<Scrollbar>();

            return scroll.verticalScrollbar;

        }



        void EnsureDeleteModal()

        {

            if (deleteModalRoot != null)

                return;



            var canvas = FindUiCanvas();

            if (canvas == null)

                return;



            var backdrop = new GameObject("DeleteCourseModal", typeof(RectTransform), typeof(Image));

            var backdropRt = backdrop.GetComponent<RectTransform>();

            backdropRt.SetParent(canvas.transform, false);

            backdropRt.anchorMin = Vector2.zero;

            backdropRt.anchorMax = Vector2.one;

            backdropRt.offsetMin = Vector2.zero;

            backdropRt.offsetMax = Vector2.zero;

            backdrop.GetComponent<Image>().color = ModalBackdropColor;

            backdrop.GetComponent<Image>().raycastTarget = true;



            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));

            var panelRt = panel.GetComponent<RectTransform>();

            panelRt.SetParent(backdrop.transform, false);

            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);

            panelRt.pivot = new Vector2(0.5f, 0.5f);

            panelRt.sizeDelta = new Vector2(640f, 320f);

            panel.GetComponent<Image>().color = PanelColor;

            var column = panel.GetComponent<VerticalLayoutGroup>();

            column.spacing = 20f;

            column.padding = new RectOffset(28, 28, 28, 28);

            column.childAlignment = TextAnchor.UpperLeft;

            column.childControlWidth = true;

            column.childControlHeight = true;

            column.childForceExpandWidth = true;

            column.childForceExpandHeight = false;



            var title = CreateModalLabel(panelRt, "Delete course?", 32f, 40f);

            title.alignment = TextAlignmentOptions.MidlineLeft;



            deleteModalBody = CreateModalLabel(panelRt, string.Empty, 24f, 96f, flexibleHeight: true);

            deleteModalBody.alignment = TextAlignmentOptions.TopLeft;

            deleteModalBody.enableWordWrapping = true;



            var buttonRow = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));

            var buttonRowRt = buttonRow.GetComponent<RectTransform>();

            buttonRowRt.SetParent(panelRt, false);

            var buttonRowLayout = buttonRow.GetComponent<LayoutElement>();

            buttonRowLayout.preferredHeight = 52f;

            buttonRowLayout.minHeight = 52f;

            buttonRowLayout.flexibleHeight = 0f;

            var buttonLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();

            buttonLayout.spacing = 16f;

            buttonLayout.padding = new RectOffset(0, 0, 4, 0);

            buttonLayout.childAlignment = TextAnchor.MiddleCenter;

            buttonLayout.childControlWidth = false;

            buttonLayout.childControlHeight = true;

            buttonLayout.childForceExpandWidth = false;



            CreateModalButton(buttonRowRt, "Cancel", 184f, PanelColor * 1.2f, HideDeleteModal);

            CreateModalButton(buttonRowRt, "Delete", 184f, DeleteAccentColor, ConfirmDelete);



            deleteModalRoot = backdrop;

            deleteModalRoot.transform.SetAsLastSibling();

            deleteModalRoot.SetActive(false);

        }



        static Canvas FindUiCanvas()

        {

            var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();

            return canvas != null ? canvas : null;

        }



        void OnNewHole()

        {

            if (wizard != null)

                wizard.Open();

        }



        void OnImport()

        {

#if UNITY_STANDALONE_WIN

            if (!WindowsFileDialog.TryOpenJsonFile(out string path))

                return;



            try

            {

                var data = HoleDataJson.LoadFromFile(path);

                EnsureCatalogId(data);

                data.Name = HoleDataCatalog.Player.MakeUniqueDisplayName(data.Name);

                HoleDataCatalog.Player.Save(data, published: false);

                RefreshHoleList();

            }

            catch (Exception ex)

            {

                Debug.LogError($"[CourseEditorHub] Import failed: {ex.Message}");

            }

#else

            Debug.LogWarning("[CourseEditorHub] Import is available on PC Windows builds only.");

#endif

        }



        void OnExportHole(string id)

        {

#if UNITY_STANDALONE_WIN

            if (!TryGetHoleData(id, out var data))

                return;



            string suggested = string.IsNullOrEmpty(data.Id) ? "hole" : data.Id;

            if (!WindowsFileDialog.TrySaveJsonFile(suggested, out string path))

                return;



            try

            {

                HoleDataJson.SaveToFile(data, path);

            }

            catch (Exception ex)

            {

                Debug.LogError($"[CourseEditorHub] Export failed: {ex.Message}");

            }

#else

            Debug.LogWarning("[CourseEditorHub] Export is available on PC Windows builds only.");

#endif

        }



        static void EnsureCatalogId(HoleData data)

        {

            if (data == null)

                throw new ArgumentNullException(nameof(data));



            bool needsNewId = string.IsNullOrWhiteSpace(data.Id);

            if (!needsNewId)

            {

                foreach (var entry in HoleDataCatalog.Player.ListEntries())

                {

                    if (string.Equals(entry.Id, data.Id, StringComparison.OrdinalIgnoreCase))

                    {

                        needsNewId = true;

                        break;

                    }

                }

            }



            if (needsNewId)

                data.Id = $"import_{Guid.NewGuid():N}".Substring(0, 24);

        }



        void RefreshHoleList()

        {

            if (holeListContent == null)

                return;



            for (int i = holeListContent.childCount - 1; i >= 0; i--)

                Destroy(holeListContent.GetChild(i).gameObject);



            loadedHoles.Clear();



            foreach (var entry in HoleDataCatalog.Player.ListEntries())

                CreateHoleCard(entry);

        }



        void CreateHoleCard(HoleCatalogEntry entry)

        {

            var cardGo = new GameObject("HoleCard_" + entry.Id, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));

            var cardRt = cardGo.GetComponent<RectTransform>();

            cardRt.SetParent(holeListContent, false);



            var cardLayout = cardGo.GetComponent<LayoutElement>();

            cardLayout.minHeight = 76f;

            cardLayout.preferredHeight = 76f;



            var cardImage = cardGo.GetComponent<Image>();

            cardImage.color = PanelColor;



            var layout = cardGo.GetComponent<HorizontalLayoutGroup>();

            layout.padding = new RectOffset(16, 8, 10, 10);

            layout.spacing = 8f;

            layout.childAlignment = TextAnchor.MiddleLeft;

            layout.childControlWidth = false;

            layout.childControlHeight = true;

            layout.childForceExpandWidth = false;

            layout.childForceExpandHeight = true;



            var nameLabel = CreateCardLabel(cardRt, entry.DisplayName, 0f, 28f, TextAlignmentOptions.MidlineLeft);

            var nameLayout = nameLabel.GetComponent<LayoutElement>();

            nameLayout.preferredWidth = 220f;

            nameLayout.flexibleWidth = 1f;

            nameLayout.minWidth = 140f;



            var parLabel = CreateCardLabel(cardRt, FormatPar(entry), 72f, 24f, TextAlignmentOptions.MidlineLeft);

            parLabel.color = new Color(0.85f, 0.9f, 0.86f, 1f);



            var yardageLabel = CreateCardLabel(cardRt, FormatYardage(entry), 112f, 24f, TextAlignmentOptions.MidlineRight);

            yardageLabel.color = new Color(0.85f, 0.9f, 0.86f, 1f);

            yardageLabel.overflowMode = TextOverflowModes.Overflow;



            var playButton = CreateCardButton(cardRt, "Play", CardButtonWidth);

            playButton.onClick.AddListener(() => CourseEditorNavigation.OpenPlaytest(entry.Id));



            var editButton = CreateCardButton(cardRt, "Edit", CardButtonWidth);

            editButton.onClick.AddListener(() => CourseEditorNavigation.OpenEditor(entry.Id));



            var exportButton = CreateCardButton(cardRt, "Export", CardExportButtonWidth);

            exportButton.onClick.AddListener(() => OnExportHole(entry.Id));



            var deleteButton = CreateCardButton(cardRt, "Delete", CardDeleteButtonWidth);

            deleteButton.onClick.AddListener(() => ShowDeleteConfirm(entry));

        }



        void ShowDeleteConfirm(HoleCatalogEntry entry)

        {

            EnsureDeleteModal();

            if (deleteModalRoot == null || deleteModalBody == null)

                return;



            pendingDeleteId = entry.Id;

            deleteModalBody.text =

                $"Delete \"{entry.DisplayName}\"?\n\nThis removes the course from your library. You cannot undo this.";

            deleteModalRoot.SetActive(true);

            deleteModalRoot.transform.SetAsLastSibling();

        }



        void HideDeleteModal()

        {

            pendingDeleteId = null;

            if (deleteModalRoot != null)

                deleteModalRoot.SetActive(false);

        }



        void ConfirmDelete()

        {

            if (string.IsNullOrEmpty(pendingDeleteId))

            {

                HideDeleteModal();

                return;

            }



            OnDeleteHole(pendingDeleteId);

            HideDeleteModal();

        }



        void OnDeleteHole(string id)

        {

            if (string.IsNullOrEmpty(id))

                return;



            try

            {

                HoleDataCatalog.Player.Delete(id);

                loadedHoles.Remove(id);

                RefreshHoleList();

            }

            catch (Exception ex)

            {

                Debug.LogError($"[CourseEditorHub] Delete failed: {ex.Message}");

            }

        }



        string FormatPar(HoleCatalogEntry entry)

        {

            if (!TryGetHoleData(entry.Id, out var data))

                return "Par —";



            return $"Par {data.Hole.Par}";

        }



        string FormatYardage(HoleCatalogEntry entry)

        {

            if (!TryGetHoleData(entry.Id, out var data))

                return "— yd";



            return $"{data.HoleLengthYards():F0} yd";

        }



        bool TryGetHoleData(string id, out HoleData data)

        {

            if (loadedHoles.TryGetValue(id, out data))

                return true;



            try

            {

                data = HoleDataCatalog.Player.Load(id);

                loadedHoles[id] = data;

                return true;

            }

            catch

            {

                data = null;

                return false;

            }

        }



        static TextMeshProUGUI CreateCardLabel(

            RectTransform parent,

            string text,

            float width,

            float fontSize,

            TextAlignmentOptions align)

        {

            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));

            var rt = go.GetComponent<RectTransform>();

            rt.SetParent(parent, false);



            var layout = go.GetComponent<LayoutElement>();

            layout.preferredWidth = width;

            layout.flexibleWidth = 0f;



            var tmp = go.GetComponent<TextMeshProUGUI>();

            tmp.text = text;

            tmp.fontSize = fontSize;

            tmp.alignment = align;

            tmp.color = Color.white;

            tmp.fontStyle = FontStyles.Bold;

            tmp.enableWordWrapping = false;

            tmp.overflowMode = TextOverflowModes.Ellipsis;

            HudTypography.BindFont(tmp);

            return tmp;

        }



        static Button CreateCardButton(RectTransform parent, string label, float width)

        {

            var go = new GameObject(label.Replace(" ", "") + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));

            var rt = go.GetComponent<RectTransform>();

            rt.SetParent(parent, false);



            var layout = go.GetComponent<LayoutElement>();

            layout.preferredWidth = width;

            layout.minHeight = 48f;



            var image = go.GetComponent<Image>();

            image.color = PanelColor * 1.2f;



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

            labelRt.offsetMin = new Vector2(6f, 4f);

            labelRt.offsetMax = new Vector2(-6f, -4f);



            var tmp = labelGo.GetComponent<TextMeshProUGUI>();

            tmp.text = label;

            tmp.fontSize = 20f;

            tmp.alignment = TextAlignmentOptions.Center;

            tmp.color = Color.white;

            tmp.enableWordWrapping = false;

            tmp.overflowMode = TextOverflowModes.Overflow;

            HudTypography.BindFont(tmp);



            return button;

        }



        static TextMeshProUGUI CreateModalLabel(Transform parent, string text, float fontSize, float minHeight, bool flexibleHeight = false)

        {

            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));

            go.transform.SetParent(parent, false);



            var layout = go.GetComponent<LayoutElement>();

            layout.minHeight = minHeight;

            layout.preferredHeight = flexibleHeight ? -1f : minHeight;

            layout.flexibleHeight = 0f;



            var tmp = go.GetComponent<TextMeshProUGUI>();

            tmp.text = text;

            tmp.fontSize = fontSize;

            tmp.color = Color.white;

            tmp.fontStyle = FontStyles.Bold;

            tmp.enableWordWrapping = true;

            tmp.overflowMode = TextOverflowModes.Overflow;

            tmp.alignment = TextAlignmentOptions.TopLeft;

            HudTypography.BindFont(tmp);



            if (flexibleHeight)

            {

                var fitter = go.AddComponent<ContentSizeFitter>();

                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            }



            return tmp;

        }



        static void CreateModalButton(Transform parent, string label, float width, Color color, UnityEngine.Events.UnityAction onClick)

        {

            var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));

            go.transform.SetParent(parent, false);

            var layout = go.GetComponent<LayoutElement>();

            layout.preferredWidth = width;

            layout.minWidth = width;

            layout.minHeight = 48f;

            go.GetComponent<Image>().color = color;



            var button = go.GetComponent<Button>();

            button.onClick.AddListener(onClick);



            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));

            var labelRt = labelGo.GetComponent<RectTransform>();

            labelRt.SetParent(go.transform, false);

            labelRt.anchorMin = Vector2.zero;

            labelRt.anchorMax = Vector2.one;

            labelRt.offsetMin = new Vector2(10f, 6f);

            labelRt.offsetMax = new Vector2(-10f, -6f);



            var tmp = labelGo.GetComponent<TextMeshProUGUI>();

            tmp.text = label;

            tmp.fontSize = 20f;

            tmp.alignment = TextAlignmentOptions.Center;

            tmp.color = Color.white;

            tmp.enableWordWrapping = false;

            tmp.overflowMode = TextOverflowModes.Overflow;

            HudTypography.BindFont(tmp);

        }

    }

}


