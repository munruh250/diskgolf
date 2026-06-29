using System;
using System.Collections.Generic;
using DiskGolf.CourseEditor;
using DiskGolf.CourseEditor.Authoring;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI.CourseEditor
{
    public enum ValidationDrawerMode
    {
        BlockingErrors,
        WarningsConfirm
    }

    public sealed class ValidationDrawerController : MonoBehaviour
    {
        static readonly Color BackdropColor = new(0f, 0f, 0f, 0.72f);
        static readonly Color PanelColor = new(0.1f, 0.15f, 0.12f, 0.98f);
        static readonly Color RowColor = new(0.16f, 0.22f, 0.18f, 0.98f);
        static readonly Color ErrorColor = new(0.88f, 0.32f, 0.32f, 1f);
        static readonly Color WarningColor = new(0.95f, 0.73f, 0.28f, 1f);
        static readonly Color SectionColor = new(0.82f, 0.9f, 0.86f, 1f);
        static readonly Color BodyColor = new(0.92f, 0.95f, 0.93f, 1f);
        static readonly Color MutedColor = new(0.72f, 0.78f, 0.74f, 1f);

        CourseEditorSession session;
        CourseEditorCameraController cameraController;
        Action onPlayAnyway;

        GameObject root;
        Transform messagesRoot;
        TextMeshProUGUI subtitleLabel;
        TextMeshProUGUI closeButtonLabel;
        GameObject closeButtonRoot;
        GameObject playAnywayButtonRoot;
        readonly List<GameObject> spawnedRows = new();

        public bool IsOpen => root != null && root.activeSelf;

        public void Initialize(Transform parent, CourseEditorSession sessionRef, CourseEditorCameraController cameraControllerRef)
        {
            session = sessionRef;
            cameraController = cameraControllerRef;
            BuildUi(parent);
            Hide();
        }

        public void SetPlayAnywayHandler(Action handler) => onPlayAnyway = handler;

        public void Show(ValidationResult result, ValidationDrawerMode mode, bool focusFirstError = false)
        {
            if (root == null)
                return;

            root.SetActive(true);
            root.transform.SetAsLastSibling();

            if (subtitleLabel != null)
            {
                subtitleLabel.text = mode == ValidationDrawerMode.BlockingErrors
                    ? "Fix the items below before playtesting."
                    : "You can still playtest, but these should be addressed.";
            }

            if (closeButtonLabel != null)
                closeButtonLabel.text = mode == ValidationDrawerMode.WarningsConfirm ? "Cancel" : "Close";

            if (closeButtonRoot != null)
                closeButtonRoot.SetActive(true);

            if (playAnywayButtonRoot != null)
                playAnywayButtonRoot.SetActive(mode == ValidationDrawerMode.WarningsConfirm);

            RebuildRows(result);

            if (!focusFirstError || result == null)
                return;

            foreach (var message in result.Messages)
            {
                if (message.Severity != ValidationSeverity.Error)
                    continue;

                ApplyFocusHint(message.Code);
                break;
            }
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }

        void BuildUi(Transform parent)
        {
            if (root != null)
                return;

            root = CreateUiObject("ValidationDrawerRoot", parent);
            StretchFull(root.GetComponent<RectTransform>());
            var backdrop = root.AddComponent<Image>();
            backdrop.color = BackdropColor;
            backdrop.raycastTarget = true;

            var panel = CreateUiObject("Panel", root.transform);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(600f, 500f);
            panel.AddComponent<Image>().color = PanelColor;

            var column = panel.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(24, 24, 24, 24);
            column.spacing = 16f;
            column.childAlignment = TextAnchor.UpperLeft;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            var title = CreateSectionLabel(panel.transform, "Validation");
            title.fontSize = 30f;
            var titleLayout = title.GetComponent<LayoutElement>();
            titleLayout.preferredHeight = 40f;
            titleLayout.minHeight = 40f;

            subtitleLabel = CreateBodyLabel(panel.transform, "Subtitle", "Fix the items below before playtesting.");
            subtitleLabel.color = MutedColor;
            subtitleLabel.fontSize = 20f;
            var subtitleLayout = subtitleLabel.GetComponent<LayoutElement>();
            subtitleLayout.preferredHeight = 28f;
            subtitleLayout.minHeight = 28f;

            var scrollRoot = CreateUiObject("MessageScroll", panel.transform);
            scrollRoot.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.22f);
            var scrollRect = scrollRoot.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var scrollElement = scrollRoot.AddComponent<LayoutElement>();
            scrollElement.flexibleHeight = 1f;
            scrollElement.minHeight = 280f;
            scrollElement.preferredHeight = 320f;

            var viewport = CreateUiObject("Viewport", scrollRoot.transform);
            var viewportRt = viewport.GetComponent<RectTransform>();
            StretchFull(viewportRt);
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = CreateUiObject("Content", viewport.transform);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);

            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 10f;
            contentLayout.padding = new RectOffset(10, 10, 10, 10);
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;
            messagesRoot = content.transform;

            var buttonRow = CreateUiObject("Buttons", panel.transform);
            var buttonRowLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            buttonRowLayout.spacing = 12f;
            buttonRowLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonRowLayout.childControlWidth = false;
            buttonRowLayout.childControlHeight = true;
            buttonRowLayout.childForceExpandWidth = false;
            var buttonRowElement = buttonRow.AddComponent<LayoutElement>();
            buttonRowElement.preferredHeight = 48f;
            buttonRowElement.minHeight = 48f;

            closeButtonRoot = CreateNavButton(buttonRow.transform, "Close", 160f, Hide, out closeButtonLabel);
            playAnywayButtonRoot = CreateNavButton(buttonRow.transform, "Play Anyway", 200f, OnPlayAnywayClicked, out _);
            playAnywayButtonRoot.SetActive(false);
        }

        void OnPlayAnywayClicked()
        {
            Hide();
            onPlayAnyway?.Invoke();
        }

        void RebuildRows(ValidationResult result)
        {
            foreach (var row in spawnedRows)
            {
                if (row != null)
                    Destroy(row);
            }

            spawnedRows.Clear();

            if (messagesRoot == null)
                return;

            var errors = new List<ValidationMessage>();
            var warnings = new List<ValidationMessage>();
            if (result != null)
            {
                foreach (var message in result.Messages)
                {
                    if (message.Severity == ValidationSeverity.Error)
                        errors.Add(message);
                    else
                        warnings.Add(message);
                }
            }

            if (errors.Count == 0 && warnings.Count == 0)
            {
                var none = CreateBodyLabel(messagesRoot, "NoIssues", "No validation issues found.");
                none.color = MutedColor;
                spawnedRows.Add(none.gameObject);
                return;
            }

            if (errors.Count > 0)
                AddSection("Must fix", errors, ErrorColor);

            if (warnings.Count > 0)
                AddSection("Suggestions", warnings, WarningColor);

            LayoutRebuilder.ForceRebuildLayoutImmediate(messagesRoot as RectTransform);
        }

        void AddSection(string title, List<ValidationMessage> messages, Color markerColor)
        {
            var sectionLabel = CreateSectionLabel(messagesRoot, title);
            spawnedRows.Add(sectionLabel.gameObject);

            foreach (var message in messages)
            {
                string copy = ValidationMessageCopy.ForCode(message.Code);
                var row = CreateMessageRow(message.Code, copy, markerColor, () => ApplyFocusHint(message.Code));
                spawnedRows.Add(row);
            }
        }

        GameObject CreateMessageRow(string code, string copy, Color markerColor, UnityEngine.Events.UnityAction onClick)
        {
            var rowGo = CreateUiObject("Message_" + code, messagesRoot);
            var rowLayout = rowGo.AddComponent<LayoutElement>();
            rowLayout.minHeight = 72f;
            rowLayout.preferredHeight = 72f;
            rowLayout.flexibleWidth = 1f;

            var rowImage = rowGo.AddComponent<Image>();
            rowImage.color = RowColor;

            var button = rowGo.AddComponent<Button>();
            button.targetGraphic = rowImage;
            if (onClick != null)
                button.onClick.AddListener(onClick);

            var markerGo = CreateUiObject("Marker", rowGo.transform);
            var markerRt = markerGo.GetComponent<RectTransform>();
            markerRt.anchorMin = new Vector2(0f, 0f);
            markerRt.anchorMax = new Vector2(0f, 1f);
            markerRt.pivot = new Vector2(0f, 0.5f);
            markerRt.sizeDelta = new Vector2(8f, -16f);
            markerRt.anchoredPosition = new Vector2(8f, 0f);
            markerGo.AddComponent<Image>().color = markerColor;

            var labelGo = CreateUiObject("Label", rowGo.transform);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(24f, 8f);
            labelRt.offsetMax = new Vector2(-12f, -8f);

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = $"[{code.ToUpperInvariant()}] {copy}";
            tmp.fontSize = 18f;
            tmp.color = BodyColor;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            HudTypography.BindFont(tmp);

            return rowGo;
        }

        void ApplyFocusHint(string code)
        {
            var hint = ValidationMessageCopy.GetFocusHint(code);
            if (session != null)
            {
                if (hint.SelectTool.HasValue)
                    session.Authoring.ActiveTool = hint.SelectTool.Value;
                if (hint.SelectBrush.HasValue)
                    session.Authoring.BrushType = hint.SelectBrush.Value;
            }

            switch (hint.CameraPreset)
            {
                case CourseEditorCameraPreset.Overview:
                    cameraController?.SetOverviewPreset();
                    break;
                case CourseEditorCameraPreset.Tee:
                    cameraController?.SetTeePreset();
                    break;
                case CourseEditorCameraPreset.Basket:
                    cameraController?.SetBasketPreset();
                    break;
                case CourseEditorCameraPreset.TopDown:
                    cameraController?.SetTopDownPreset();
                    break;
            }
        }

        static TextMeshProUGUI CreateSectionLabel(Transform parent, string text)
        {
            var label = CreateBodyLabel(parent, text + "Header", text);
            label.fontSize = 22f;
            label.fontStyle = FontStyles.Bold;
            label.color = SectionColor;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.enableWordWrapping = false;

            var layout = label.GetComponent<LayoutElement>();
            layout.preferredHeight = 28f;
            layout.minHeight = 28f;
            return label;
        }

        static TextMeshProUGUI CreateBodyLabel(Transform parent, string name, string text)
        {
            var go = CreateUiObject(name, parent);
            var layout = go.AddComponent<LayoutElement>();
            layout.minHeight = 24f;
            layout.flexibleWidth = 1f;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 20f;
            tmp.color = BodyColor;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            HudTypography.BindFont(tmp);
            return tmp;
        }

        static GameObject CreateNavButton(Transform parent, string label, float width,
            UnityEngine.Events.UnityAction onClick, out TextMeshProUGUI labelTmp)
        {
            var go = CreateUiObject(label.Replace(" ", string.Empty) + "Button", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, 0f);

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.minWidth = width;
            layout.preferredHeight = 44f;
            layout.minHeight = 44f;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.22f, 0.28f, 0.24f, 1f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null)
                button.onClick.AddListener(onClick);

            var labelGo = CreateUiObject("Label", go.transform);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(12f, 6f);
            labelRt.offsetMax = new Vector2(-12f, -6f);

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            HudTypography.BindFont(tmp);
            labelTmp = tmp;
            return go;
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
