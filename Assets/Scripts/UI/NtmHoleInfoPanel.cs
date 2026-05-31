using TMPro;
using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>Hole number, total yardage, and par stacked above the minimap.</summary>
    public sealed class NtmHoleInfoPanel : MonoBehaviour
    {
        const string RootName = "NtmHoleInfo";

        [SerializeField] TextMeshProUGUI holeNumberText;

        [SerializeField] TextMeshProUGUI yardageText;

        [SerializeField] TextMeshProUGUI parText;

        public static NtmHoleInfoPanel Ensure(RectTransform hudRoot)
        {
            if (hudRoot == null)
                return null;

            var existing = hudRoot.Find(RootName)?.GetComponent<NtmHoleInfoPanel>();
            if (existing != null)
            {
                existing.RefreshLayout();
                return existing;
            }

            var go = new GameObject(RootName, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(hudRoot, false);

            var panel = go.AddComponent<NtmHoleInfoPanel>();
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
            NtmHudTypography.Apply(tmp, TextAlignmentOptions.MidlineLeft);
            ApplyLineLayout(tmp, row);
            return tmp;
        }

        static void ApplyLineLayout(TextMeshProUGUI tmp, int row)
        {
            if (tmp == null)
                return;

            NtmHudTypography.Apply(tmp, TextAlignmentOptions.MidlineLeft);

            var rt = tmp.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(0f, -row * NtmHudTypography.RowHeight);
            rt.sizeDelta = new Vector2(NtmHudLayout.MinimapWidth + 120f, NtmHudTypography.RowHeight);
        }

        public void ApplyLayout()
        {
            var rt = transform as RectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-NtmHudLayout.RightInset, -NtmHudTypography.TopInset);
            rt.sizeDelta = new Vector2(NtmHudLayout.MinimapWidth + 120f, NtmHudLayout.HoleInfoHeight);
        }
    }
}
