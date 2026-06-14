using TMPro;
using UnityEngine;

namespace DiskGolf.UI
{
    static class ThrowSummaryLayout
    {
        public const float AnchorY = 0.56f;

        public static readonly Vector2 RootSize = new(1000f, 264f);

        public static void ApplyRootRect(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, AnchorY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = RootSize;
        }

        public static void ApplyPanelRect(RectTransform rt, bool shadow)
        {
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = shadow ? new Vector2(188f, -12f) : new Vector2(176f, 0f);
            rt.sizeDelta = new Vector2(-176f, 192f);
        }

        public static void ApplyPortraitRect(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.38f);
            rt.anchoredPosition = new Vector2(104f, 12f);
            rt.sizeDelta = new Vector2(224f, 256f);
        }

        public static void ApplyTextBlockRect(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(216f, 0f);
            rt.sizeDelta = new Vector2(760f, 192f);
        }

        public static void ApplyNameLabelRect(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 56f);
            rt.sizeDelta = new Vector2(760f, 112f);
        }

        public static void ApplyThrowLabelRect(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -48f);
            rt.sizeDelta = new Vector2(760f, 80f);
        }

        public static void ApplyNameStyle(TextMeshProUGUI tmp, string text)
        {
            ApplyLabelStyle(tmp, text, 92f);
        }

        public static void ApplyThrowStyle(TextMeshProUGUI tmp, string text)
        {
            ApplyLabelStyle(tmp, text, 52f);
        }

        static void ApplyLabelStyle(TextMeshProUGUI tmp, string text, float fontSize)
        {
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = new Color(1f, 0.82f, 0.15f, 1f);
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
        }
    }
}
