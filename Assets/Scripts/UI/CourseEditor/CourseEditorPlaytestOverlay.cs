using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI.CourseEditor
{
    public sealed class CourseEditorPlaytestOverlay : MonoBehaviour
    {
        static readonly Color PillColor = new(0f, 0f, 0f, 0.62f);
        const float HorizontalPadding = 32f;
        const float PillHeight = 48f;
        const float MinPillWidth = 900f;

        GameObject root;
        RectTransform rootRt;
        TextMeshProUGUI label;

        public void Initialize(Transform parent)
        {
            if (root != null)
                return;

            root = CreateUiObject("PlaytestOverlay", parent);
            rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 1f);
            rootRt.anchorMax = new Vector2(0.5f, 1f);
            rootRt.pivot = new Vector2(0.5f, 1f);
            rootRt.anchoredPosition = new Vector2(0f, -12f);

            var image = root.AddComponent<Image>();
            image.color = PillColor;
            image.raycastTarget = false;

            var textGo = CreateUiObject("Label", root.transform);
            var textRt = textGo.GetComponent<RectTransform>();
            StretchFull(textRt);

            label = textGo.AddComponent<TextMeshProUGUI>();
            label.text = "Playtesting — Press Esc to return to editing";
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            HudTypography.BindFont(label);

            ResizeToFitLabel();
            Hide();
        }

        public void Show()
        {
            if (root == null)
                return;

            ResizeToFitLabel();
            root.SetActive(true);
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }

        void ResizeToFitLabel()
        {
            if (rootRt == null || label == null)
                return;

            label.ForceMeshUpdate();
            float textWidth = label.preferredWidth;
            if (textWidth < 1f)
                textWidth = label.GetPreferredValues(label.text, float.PositiveInfinity, PillHeight).x;

            float width = Mathf.Max(MinPillWidth, textWidth + HorizontalPadding * 2f);
            rootRt.sizeDelta = new Vector2(width, PillHeight);
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
            rt.offsetMin = new Vector2(HorizontalPadding, 6f);
            rt.offsetMax = new Vector2(-HorizontalPadding, -6f);
            rt.localScale = Vector3.one;
        }
    }
}
