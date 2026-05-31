using TMPro;
using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>NTM-style REST / DRIVE distance rows (top-left).</summary>
    public sealed class NtmRestDriveReadout : MonoBehaviour
    {
        const string RootName = "NtmRestDrive";

        static readonly Color RestLabelColor = new(1f, 0.92f, 0.18f, 1f);

        static readonly Color ValueColor = new(0.98f, 0.98f, 0.98f, 1f);

        [SerializeField] TextMeshProUGUI restLabel;

        [SerializeField] TextMeshProUGUI restValue;

        [SerializeField] TextMeshProUGUI driveLabel;

        [SerializeField] TextMeshProUGUI driveValue;

        public static NtmRestDriveReadout Ensure(RectTransform hudRoot)
        {
            if (hudRoot == null)
                return null;

            var existing = hudRoot.Find(RootName)?.GetComponent<NtmRestDriveReadout>();
            if (existing != null)
            {
                existing.ApplyLayout();
                return existing;
            }

            var go = new GameObject(RootName, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(hudRoot, false);

            var readout = go.AddComponent<NtmRestDriveReadout>();
            readout.Build();
            readout.ApplyLayout();
            return readout;
        }

        public void SetValues(int restYards, int driveYards)
        {
            if (restValue != null)
                restValue.text = $"{restYards}y";

            if (driveValue != null)
                driveValue.text = $"{driveYards}y";
        }

        void Build()
        {
            restLabel = CreateCell("RestLabel", "REST", RestLabelColor, FontStyles.Bold, 34f, 0);
            restValue = CreateCell("RestValue", "0y", ValueColor, FontStyles.Bold, 40f, 1);
            driveLabel = CreateCell("DriveLabel", "DRIVE", ValueColor, FontStyles.Bold, 34f, 2);
            driveValue = CreateCell("DriveValue", "0y", ValueColor, FontStyles.Bold, 40f, 3);
        }

        TextMeshProUGUI CreateCell(string name, string text, Color color, FontStyles style, float fontSize, int row)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(row % 2 == 0 ? 0f : 132f, -(row / 2) * 52f);
            rt.sizeDelta = new Vector2(row % 2 == 0 ? 120f : 160f, 48f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.fontSize = fontSize;
            tmp.alignment = row % 2 == 0 ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.TopRight;
            tmp.raycastTarget = false;
            return tmp;
        }

        public void ApplyLayout()
        {
            var rt = transform as RectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(36f, -36f);
            rt.sizeDelta = new Vector2(300f, 112f);
        }
    }
}
