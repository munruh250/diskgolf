using DiskGolf.Flight;
using DiskGolf.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>Wind direction arrow and speed readout.</summary>
    public sealed class WindWidget : MonoBehaviour
    {
        const string RootName = "WindWidget";

        static readonly Color FrameColor = new(0.12f, 0.12f, 0.12f, 1f);

        static readonly Color ArrowPanelColor = new(0.18f, 0.72f, 0.28f, 1f);

        static readonly Color SpeedPanelColor = new(0.72f, 0.72f, 0.72f, 1f);

        static readonly Color SpeedTextColor = new(0.08f, 0.08f, 0.08f, 1f);

        [SerializeField] RectTransform arrowRoot;

        [SerializeField] Image windIcon;

        [SerializeField] TextMeshProUGUI speedValueText;

        void Awake()
        {
            BindSceneReferences();
        }

        public static WindWidget Ensure(RectTransform hudRoot)
        {
            if (hudRoot == null)
                return null;

            var existing = hudRoot.Find(RootName)?.GetComponent<WindWidget>()
                ?? hudRoot.Find("NtmWindWidget")?.GetComponent<WindWidget>();
            if (existing != null)
            {
                if (existing.name != RootName)
                    existing.name = RootName;
                existing.BindSceneReferences();
                existing.ApplyTypography();
                return existing;
            }

            var go = new GameObject(RootName, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(hudRoot, false);

            var widget = go.AddComponent<WindWidget>();
            widget.Build();
            return widget;
        }

        public void BindSceneReferences()
        {
            var frame = transform.Find("Frame");
            if (frame != null)
            {
                var arrowPanel = frame.Find("ArrowPanel");
                arrowRoot ??= arrowPanel?.Find("ArrowRoot") as RectTransform;
                speedValueText ??= frame.Find("SpeedPanel/WindSpeed")?.GetComponent<TextMeshProUGUI>();
            }

            EnsureWindIcon();
        }

        public void SetWind(WindSettings wind)
        {
            EnsureWindIcon();

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

            EnsureWindIcon();

            var speedPanel = CreatePanel("SpeedPanel", SpeedPanelColor, new Vector2(64f, 4f), new Vector2(100f, 48f));
            speedPanel.SetParent(frame, false);

            var windLabel = CreateText("WindLabel", "WIND", new Vector2(8f, 28f),
                new Vector2(84f, 16f), SpeedTextColor, TextAlignmentOptions.TopLeft);
            windLabel.transform.SetParent(speedPanel.transform, false);

            speedValueText = CreateText("WindSpeed", "0m", new Vector2(8f, 4f),
                new Vector2(84f, 22f), SpeedTextColor, TextAlignmentOptions.BottomLeft);
            speedValueText.transform.SetParent(speedPanel.transform, false);
            ApplyTypography();
        }

        void EnsureWindIcon()
        {
            if (arrowRoot == null)
                return;

            windIcon ??= arrowRoot.Find("WindIcon")?.GetComponent<Image>();
            if (windIcon == null)
            {
                HideLegacyArrowArt();
                windIcon = CreateWindIcon(arrowRoot);
            }

            var sprite = RuntimeArt.LoadWindIconSprite();
            if (sprite != null)
            {
                windIcon.sprite = sprite;
                windIcon.enabled = true;
            }

            windIcon.preserveAspect = true;
            windIcon.color = Color.white;
            windIcon.raycastTarget = false;
        }

        static Image CreateWindIcon(RectTransform parent)
        {
            var go = new GameObject("WindIcon", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(4f, 4f);
            rt.offsetMax = new Vector2(-4f, -4f);
            return go.GetComponent<Image>();
        }

        void HideLegacyArrowArt()
        {
            if (arrowRoot == null)
                return;

            foreach (var childName in new[] { "Shaft", "Head" })
            {
                var legacy = arrowRoot.Find(childName);
                if (legacy != null)
                    legacy.gameObject.SetActive(false);
            }
        }

        public void ApplyTypography()
        {
            foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                var color = tmp.color;
                var align = tmp.alignment;

                if (tmp.name == "WindLabel")
                {
                    HudTypography.BindFont(tmp);
                    tmp.fontSize = 14f;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.alignment = align;
                }
                else
                {
                    HudTypography.Apply(tmp, align);
                }

                tmp.color = color;
            }
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

        static TextMeshProUGUI CreateText(string name, string text, Vector2 pos,
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
            HudTypography.Apply(tmp, align);
            return tmp;
        }

        public void ApplyLayout()
        {
            var rt = transform as RectTransform;
            float minimapTop = HudLayout.TopInset + HudLayout.HoleInfoHeight + HudLayout.StackGap;
            float windTop = minimapTop + HudLayout.MinimapHeight + HudLayout.StackGap;

            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-HudLayout.RightInset, -windTop);
            rt.sizeDelta = new Vector2(168f, 56f);
        }
    }
}
