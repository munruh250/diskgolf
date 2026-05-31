using TMPro;
using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>NTM-style distance-to-basket and throw distance rows (top-left).</summary>
    public sealed class NtmRestDriveReadout : MonoBehaviour
    {
        const string RootName = "NtmRestDrive";

        const string BasketLabel = "DISTANCE TO BASKET";

        const string ThrowLabel = "THROW";

        const float LabelWidth = 240f;

        const float ValueOffset = 248f;

        const float ValueWidth = 120f;

        static readonly Color BasketLabelColor = new(1f, 0.92f, 0.18f, 1f);

        static readonly Color ValueColor = new(0.98f, 0.98f, 0.98f, 1f);

        [SerializeField] TextMeshProUGUI basketLabel;

        [SerializeField] TextMeshProUGUI basketValue;

        [SerializeField] TextMeshProUGUI throwLabel;

        [SerializeField] TextMeshProUGUI throwValue;

        public static NtmRestDriveReadout Ensure(RectTransform hudRoot)
        {
            if (hudRoot == null)
                return null;

            var existing = hudRoot.Find(RootName)?.GetComponent<NtmRestDriveReadout>();
            if (existing != null)
            {
                existing.RebindFields();
                existing.RefreshLabels();
                return existing;
            }

            var go = new GameObject(RootName, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(hudRoot, false);

            var readout = go.AddComponent<NtmRestDriveReadout>();
            readout.Build();
            return readout;
        }

        public void SetValues(int basketYards, int throwYards)
        {
            if (basketValue != null)
                basketValue.text = $"{basketYards}y";

            if (throwValue != null)
                throwValue.text = $"{throwYards}y";
        }

        void Build()
        {
            basketLabel = CreateCell("BasketLabel", BasketLabel, BasketLabelColor, FontStyles.Bold, 26f, 0, true);
            basketValue = CreateCell("BasketValue", "0y", ValueColor, FontStyles.Bold, 40f, 1, false);
            throwLabel = CreateCell("ThrowLabel", ThrowLabel, ValueColor, FontStyles.Bold, 34f, 2, true);
            throwValue = CreateCell("ThrowValue", "0y", ValueColor, FontStyles.Bold, 40f, 3, false);
        }

        void RebindFields()
        {
            basketLabel ??= FindText("BasketLabel", "RestLabel");
            basketValue ??= FindText("BasketValue", "RestValue");
            throwLabel ??= FindText("ThrowLabel", "DriveLabel");
            throwValue ??= FindText("ThrowValue", "DriveValue");
        }

        TextMeshProUGUI FindText(string primary, string legacy)
        {
            var primaryTf = transform.Find(primary);
            if (primaryTf != null)
                return primaryTf.GetComponent<TextMeshProUGUI>();

            var legacyTf = transform.Find(legacy);
            return legacyTf != null ? legacyTf.GetComponent<TextMeshProUGUI>() : null;
        }

        void RefreshLabels()
        {
            RebindFields();

            if (basketLabel != null)
            {
                basketLabel.text = BasketLabel;
                basketLabel.fontSize = 26f;
                basketLabel.color = BasketLabelColor;
                ResizeCell(basketLabel.rectTransform, true, 0);
            }

            if (basketValue != null)
                ResizeCell(basketValue.rectTransform, false, 1);

            if (throwLabel != null)
            {
                throwLabel.text = ThrowLabel;
                ResizeCell(throwLabel.rectTransform, true, 2);
            }

            if (throwValue != null)
                ResizeCell(throwValue.rectTransform, false, 3);

            ApplyLayout();
        }

        static void ResizeCell(RectTransform rt, bool isLabel, int row)
        {
            if (rt == null)
                return;

            rt.anchoredPosition = new Vector2(isLabel ? 0f : ValueOffset, -(row / 2) * 52f);
            rt.sizeDelta = new Vector2(isLabel ? LabelWidth : ValueWidth, 48f);
        }

        TextMeshProUGUI CreateCell(string name, string text, Color color, FontStyles style, float fontSize, int row,
            bool isLabel)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(isLabel ? 0f : ValueOffset, -(row / 2) * 52f);
            rt.sizeDelta = new Vector2(isLabel ? LabelWidth : ValueWidth, 48f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.fontSize = fontSize;
            tmp.alignment = isLabel ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.TopRight;
            tmp.raycastTarget = false;
            return tmp;
        }

        public void ApplyLayout()
        {
            var rt = transform as RectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(36f, -36f);
            rt.sizeDelta = new Vector2(380f, 112f);
        }
    }
}
