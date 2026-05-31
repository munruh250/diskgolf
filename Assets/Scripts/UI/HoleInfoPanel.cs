using TMPro;
using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>Hole number, total yardage, and par stacked above the minimap.</summary>
    public sealed class HoleInfoPanel : MonoBehaviour
    {
        const string RootName = "HoleInfo";

        [SerializeField] TextMeshProUGUI holeNumberText;

        [SerializeField] TextMeshProUGUI yardageText;

        [SerializeField] TextMeshProUGUI parText;

        public static HoleInfoPanel Ensure(RectTransform hudRoot)
        {
            if (hudRoot == null)
                return null;

            var existing = hudRoot.Find(RootName)?.GetComponent<HoleInfoPanel>()
                ?? hudRoot.Find("NtmHoleInfo")?.GetComponent<HoleInfoPanel>();
            if (existing != null)
            {
                if (existing.name != RootName)
                    existing.name = RootName;
                if (!HudLayoutSettings.ShouldPreserveLayout())
                    existing.RefreshLayout();
                return existing;
            }

            var go = new GameObject(RootName, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(hudRoot, false);

            var panel = go.AddComponent<HoleInfoPanel>();
            panel.Build();
            panel.ApplyLayout();
            return panel;
        }

        public void SetHoleInfo(int holeNumber, int totalYards, int par)
        {
            if (holeNumberText != null)
                holeNumberText.text = $"Hole: {holeNumber}";

            if (yardageText != null)
                yardageText.text = $"{totalYards} Yards";

            if (parText != null)
                parText.text = $"PAR {par}";
        }

        void Build()
        {
            holeNumberText = CreateLine("HoleNumber", "Hole: 1", 0);
            yardageText = CreateLine("Yardage", "250 Yards", 1);
            parText = CreateLine("Par", "PAR 3", 2);
        }

        void RefreshLayout()
        {
            ApplyLineLayout(holeNumberText, 0);
            ApplyLineLayout(yardageText, 1);
            ApplyLineLayout(parText, 2);
            ApplyLayout();
        }

        TextMeshProUGUI CreateLine(string name, string text, int row)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            HudTypography.Apply(tmp, TextAlignmentOptions.MidlineLeft);
            ApplyLineLayout(tmp, row);
            return tmp;
        }

        static void ApplyLineLayout(TextMeshProUGUI tmp, int row)
        {
            if (tmp == null)
                return;

            HudTypography.Apply(tmp, TextAlignmentOptions.MidlineLeft);

            var rt = tmp.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(0f, -row * HudTypography.RowHeight);
            rt.sizeDelta = new Vector2(HudLayout.MinimapWidth + 120f, HudTypography.RowHeight);
        }

        public void ApplyLayout()
        {
            var rt = transform as RectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-HudLayout.RightInset, -HudTypography.TopInset);
            rt.sizeDelta = new Vector2(HudLayout.MinimapWidth + 120f, HudLayout.HoleInfoHeight);
        }
    }
}
