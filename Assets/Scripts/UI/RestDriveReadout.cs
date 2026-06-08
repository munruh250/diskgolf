using TMPro;
using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>Distance, throw, and disc height rows (top-left).</summary>
    public sealed class RestDriveReadout : MonoBehaviour
    {
        const string RootName = "RestDrive";

        const string DistanceLabel = "DISTANCE";

        const string ThrowLabel = "THROW";

        const string HeightLabel = "DISC HEIGHT";

        [SerializeField] TextMeshProUGUI distanceLabel;

        [SerializeField] TextMeshProUGUI distanceValue;

        [SerializeField] TextMeshProUGUI throwLabel;

        [SerializeField] TextMeshProUGUI throwValue;

        [SerializeField] TextMeshProUGUI heightLabel;

        [SerializeField] TextMeshProUGUI heightValue;

        public static RestDriveReadout Ensure(RectTransform hudRoot)
        {
            if (hudRoot == null)
                return null;

            var existing = hudRoot.Find(RootName)?.GetComponent<RestDriveReadout>()
                ?? hudRoot.Find("NtmRestDrive")?.GetComponent<RestDriveReadout>();
            if (existing != null)
            {
                if (existing.name != RootName)
                    existing.name = RootName;

                existing.RepairRowLayout();
                if (!HudLayoutSettings.ShouldPreserveLayout())
                    existing.ApplyLayout();
                return existing;
            }

            var go = new GameObject(RootName, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(hudRoot, false);

            var readout = go.AddComponent<RestDriveReadout>();
            readout.Build();
            readout.RepairRowLayout();
            readout.ApplyLayout();
            return readout;
        }

        public void SetValues(int basketYards, int throwYards, int heightFt)
        {
            if (distanceValue != null)
                distanceValue.text = $"{basketYards}y";

            if (throwValue != null)
                throwValue.text = $"{throwYards}y";

            if (heightValue != null)
                heightValue.text = $"{heightFt}ft";
        }

        void Build()
        {
            distanceLabel = CreateLabel("DistanceLabel", DistanceLabel, 0);
            distanceValue = CreateValue("DistanceValue", "0y", 0);
            throwLabel = CreateLabel("ThrowLabel", ThrowLabel, 1);
            throwValue = CreateValue("ThrowValue", "0y", 1);
            heightLabel = CreateLabel("HeightLabel", HeightLabel, 2);
            heightValue = CreateValue("HeightValue", "0ft", 2);
        }

        public void RepairRowLayout()
        {
            RebindFields();
            EnsureHeightRow();
            ApplyRowLayout(distanceLabel, distanceValue, 0);
            ApplyRowLayout(throwLabel, throwValue, 1);
            ApplyRowLayout(heightLabel, heightValue, 2);
        }

        void RebindFields()
        {
            distanceLabel ??= FindText("DistanceLabel", "BasketLabel", "RestLabel");
            distanceValue ??= FindText("DistanceValue", "BasketValue", "RestValue");
            throwLabel ??= FindText("ThrowLabel", "DriveLabel");
            throwValue ??= FindText("ThrowValue", "DriveValue");
            heightLabel ??= FindText("HeightLabel");
            heightValue ??= FindText("HeightValue");
        }

        TextMeshProUGUI FindText(params string[] names)
        {
            foreach (var name in names)
            {
                var tf = transform.Find(name);
                if (tf != null)
                    return tf.GetComponent<TextMeshProUGUI>();
            }

            return null;
        }

        void RefreshLayout()
        {
            RepairRowLayout();
            ApplyLayout();
        }

        void EnsureHeightRow()
        {
            if (heightLabel != null && heightValue != null)
                return;

            heightLabel = CreateLabel("HeightLabel", HeightLabel, 2);
            heightValue = CreateValue("HeightValue", "0ft", 2);
        }

        static void ApplyRowLayout(TextMeshProUGUI label, TextMeshProUGUI value, int row)
        {
            if (label != null)
            {
                label.text = row switch
                {
                    0 => DistanceLabel,
                    1 => ThrowLabel,
                    _ => HeightLabel,
                };
                HudTypography.Apply(label, TextAlignmentOptions.MidlineLeft);
                LayoutCell(label.rectTransform, true, row);
                label.gameObject.SetActive(true);
            }

            if (value != null)
            {
                HudTypography.Apply(value, TextAlignmentOptions.MidlineRight);
                LayoutCell(value.rectTransform, false, row);
                value.gameObject.SetActive(true);
            }
        }

        static void LayoutCell(RectTransform rt, bool isLabel, int row)
        {
            if (rt == null)
                return;

            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(isLabel ? 0f : HudTypography.ValueOffset, -row * HudTypography.RowHeight);
            rt.sizeDelta = new Vector2(isLabel ? HudTypography.LabelWidth : HudTypography.ValueWidth,
                HudTypography.RowHeight);
        }

        TextMeshProUGUI CreateLabel(string name, string text, int row)
        {
            var tmp = CreateCell(name, text, row, true);
            HudTypography.Apply(tmp, TextAlignmentOptions.MidlineLeft);
            return tmp;
        }

        TextMeshProUGUI CreateValue(string name, string text, int row)
        {
            var tmp = CreateCell(name, text, row, false);
            HudTypography.Apply(tmp, TextAlignmentOptions.MidlineRight);
            return tmp;
        }

        TextMeshProUGUI CreateCell(string name, string text, int row, bool isLabel)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            LayoutCell(rt, isLabel, row);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.gameObject.SetActive(true);
            return tmp;
        }

        public void ApplyLayout()
        {
            var rt = transform as RectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(HudTypography.LeftInset, -HudTypography.TopInset);
            rt.sizeDelta = new Vector2(HudTypography.ValueOffset + HudTypography.ValueWidth,
                HudTypography.RowHeight * 3f);
        }
    }
}
