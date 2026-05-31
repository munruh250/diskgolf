using DiskGolf.Flight;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>NTM-style wind box: green direction arrow + speed readout.</summary>
    public sealed class NtmWindWidget : MonoBehaviour
    {
        const string RootName = "NtmWindWidget";

        static readonly Color FrameColor = new(0.12f, 0.12f, 0.12f, 1f);

        static readonly Color ArrowPanelColor = new(0.18f, 0.72f, 0.28f, 1f);

        static readonly Color SpeedPanelColor = new(0.72f, 0.72f, 0.72f, 1f);

        static readonly Color SpeedTextColor = new(0.08f, 0.08f, 0.08f, 1f);

        [SerializeField] RectTransform arrowRoot;

        [SerializeField] TextMeshProUGUI speedValueText;

        public static NtmWindWidget Ensure(RectTransform hudRoot)
        {
            if (hudRoot == null)
                return null;

            var existing = hudRoot.Find(RootName)?.GetComponent<NtmWindWidget>();
            if (existing != null)
                return existing;

            var go = new GameObject(RootName, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(hudRoot, false);

            var widget = go.AddComponent<NtmWindWidget>();
            widget.Build();
            return widget;
        }

        public void SetWind(WindSettings wind)
        {
            if (arrowRoot != null)
            {
                float angle = Mathf.Atan2(wind.direction.x, wind.direction.y) * Mathf.Rad2Deg;
                arrowRoot.localRotation = Quaternion.Euler(0f, 0f, -angle);
            }

            if (speedValueText != null)
                speedValueText.text = $"{Mathf.Max(0, Mathf.RoundToInt(wind.speedMph * 0.2f))}m";
        }

        void Build()
        {
            var frame = CreatePanel("Frame", FrameColor, Vector2.zero, new Vector2(168f, 56f));
            frame.SetParent(transform, false);

            var arrowPanel = CreatePanel("ArrowPanel", ArrowPanelColor, new Vector2(4f, 4f), new Vector2(56f, 48f));
            arrowPanel.SetParent(frame, false);

            var arrowGo = new GameObject("ArrowRoot", typeof(RectTransform));
            arrowRoot = arrowGo.GetComponent<RectTransform>();
            arrowRoot.SetParent(arrowPanel, false);
            arrowRoot.anchorMin = arrowRoot.anchorMax = new Vector2(0.5f, 0.5f);
            arrowRoot.pivot = new Vector2(0.5f, 0.5f);
            arrowRoot.anchoredPosition = Vector2.zero;
            arrowRoot.sizeDelta = new Vector2(32f, 32f);

            CreateArrowHead(arrowRoot);

            var speedPanel = CreatePanel("SpeedPanel", SpeedPanelColor, new Vector2(64f, 4f), new Vector2(100f, 48f));
            speedPanel.SetParent(frame, false);

            var windLabel = CreateText("WindLabel", "WIND", 14f, FontStyles.Bold, new Vector2(8f, 24f),
                new Vector2(84f, 20f), SpeedTextColor, TextAlignmentOptions.TopLeft);
            windLabel.transform.SetParent(speedPanel.transform, false);

            speedValueText = CreateText("WindSpeed", "0m", 24f, FontStyles.Bold, new Vector2(8f, 0f),
                new Vector2(84f, 28f), SpeedTextColor, TextAlignmentOptions.BottomLeft);
            speedValueText.transform.SetParent(speedPanel.transform, false);
        }

        static RectTransform CreatePanel(string name, Color color, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rt;
        }

        static void CreateArrowHead(RectTransform parent)
        {
            var shaft = new GameObject("Shaft", typeof(RectTransform), typeof(Image));
            var shaftRt = shaft.GetComponent<RectTransform>();
            shaftRt.SetParent(parent, false);
            shaftRt.anchorMin = shaftRt.anchorMax = new Vector2(0.5f, 0.5f);
            shaftRt.pivot = new Vector2(0.5f, 0.5f);
            shaftRt.anchoredPosition = new Vector2(0f, -2f);
            shaftRt.sizeDelta = new Vector2(6f, 22f);
            shaft.GetComponent<Image>().color = Color.white;

            var head = new GameObject("Head", typeof(RectTransform), typeof(Image));
            var headRt = head.GetComponent<RectTransform>();
            headRt.SetParent(parent, false);
            headRt.anchorMin = headRt.anchorMax = new Vector2(0.5f, 0.5f);
            headRt.pivot = new Vector2(0.5f, 0f);
            headRt.anchoredPosition = new Vector2(0f, 10f);
            headRt.sizeDelta = new Vector2(16f, 14f);
            head.GetComponent<Image>().color = Color.white;
        }

        static TextMeshProUGUI CreateText(string name, string text, float fontSize, FontStyles style, Vector2 pos,
            Vector2 size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        public void ApplyLayout()
        {
            var rt = transform as RectTransform;
            float minimapTop = NtmHudLayout.TopInset + NtmHudLayout.HoleInfoHeight + NtmHudLayout.StackGap;
            float windTop = minimapTop + NtmHudLayout.MinimapHeight + NtmHudLayout.StackGap;

            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-NtmHudLayout.RightInset, -windTop);
            rt.sizeDelta = new Vector2(168f, 56f);
        }
    }
}
