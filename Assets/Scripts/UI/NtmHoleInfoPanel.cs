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
                existing.ApplyLayout();
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
                holeNumberText.text = holeNumber.ToString();

            if (yardageText != null)
                yardageText.text = $"{totalYards} Y";

            if (parText != null)
                parText.text = $"PAR {par}";
        }

        void Build()
        {
            holeNumberText = CreateText("HoleNumber", "1", 52f, FontStyles.Bold, new Vector2(0f, 0f),
                new Vector2(56f, 72f), TextAlignmentOptions.BottomLeft);

            yardageText = CreateText("Yardage", "250 Y", 28f, FontStyles.Bold, new Vector2(64f, 34f),
                new Vector2(140f, 36f), TextAlignmentOptions.BottomLeft);

            parText = CreateText("Par", "PAR 3", 28f, FontStyles.Bold, new Vector2(64f, 0f),
                new Vector2(140f, 36f), TextAlignmentOptions.BottomLeft);
        }

        TextMeshProUGUI CreateText(string name, string text, float fontSize, FontStyles style, Vector2 pos,
            Vector2 size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = Color.white;
            tmp.fontStyle = style;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        public void ApplyLayout()
        {
            var rt = transform as RectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20f, -432f);
            rt.sizeDelta = new Vector2(248f, 72f);
        }
    }
}
