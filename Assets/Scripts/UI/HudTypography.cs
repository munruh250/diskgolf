using TMPro;
using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>Shared HUD text sizing — reads from HudLayoutSettings when present.</summary>
    public static class HudTypography
    {
        public static float FontSize => Settings.fontSize;

        public static float RowHeight => Settings.rowHeight;

        public static float LeftInset => Settings.leftInset;

        public static float TopInset => Settings.topInset;

        public static float LabelWidth => Settings.labelWidth;

        public static float ValueOffset => Settings.valueOffset;

        public static float ValueWidth => Settings.valueWidth;

        public static Color TextColor => Settings.textColor;

        static LayoutSnapshot Settings
        {
            get
            {
                var active = HudLayoutSettings.Active;
                return active != null ? LayoutSnapshot.From(active) : LayoutSnapshot.Defaults;
            }
        }

        public static void Apply(TextMeshProUGUI tmp, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            if (tmp == null)
                return;

            var s = Settings;
            tmp.fontSize = s.fontSize;
            tmp.color = s.textColor;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = align;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
        }

        /// <summary>Applies the standard HUD font size to persistent readout labels.</summary>
        public static void ApplyToGameplayHud(RectTransform canvas)
        {
            if (canvas == null)
                return;

            canvas.GetComponentInChildren<RestDriveReadout>()?.RepairRowLayout();
            canvas.GetComponentInChildren<HoleInfoPanel>()?.RepairLineLayout();

            ApplyBottomLabel(canvas, "Disc");
            ApplyBottomLabel(canvas, "StanceLabel");
            ApplyBottomLabel(canvas, "TypeThrow");
            ApplyBottomLabel(canvas, "FLAT");

            var wind = canvas.Find("WindWidget");
            if (wind != null)
            {
                foreach (var tmp in wind.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    var color = tmp.color;
                    Apply(tmp, tmp.alignment);
                    tmp.color = color;
                }
            }

            var meters = canvas.Find("TimingMeters");
            if (meters != null)
            {
                foreach (var tmp in meters.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    var color = tmp.color;
                    Apply(tmp, tmp.alignment);
                    tmp.color = color;
                }
            }

            canvas.GetComponentInChildren<DiscSelectUI>()?.ApplyTypography();
        }

        static void ApplyBottomLabel(RectTransform canvas, string name)
        {
            var tmp = canvas.Find(name)?.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
                Apply(tmp, TextAlignmentOptions.BottomLeft);
        }

        readonly struct LayoutSnapshot
        {
            public readonly float fontSize;
            public readonly float rowHeight;
            public readonly float leftInset;
            public readonly float topInset;
            public readonly float labelWidth;
            public readonly float valueOffset;
            public readonly float valueWidth;
            public readonly Color textColor;

            LayoutSnapshot(
                float fontSize,
                float rowHeight,
                float leftInset,
                float topInset,
                float labelWidth,
                float valueOffset,
                float valueWidth,
                Color textColor)
            {
                this.fontSize = fontSize;
                this.rowHeight = rowHeight;
                this.leftInset = leftInset;
                this.topInset = topInset;
                this.labelWidth = labelWidth;
                this.valueOffset = valueOffset;
                this.valueWidth = valueWidth;
                this.textColor = textColor;
            }

            public static LayoutSnapshot Defaults => new(
                28f, 36f, 72f, 40f, 200f, 210f, 100f, Color.white);

            public static LayoutSnapshot From(HudLayoutSettings s) => new(
                s.fontSize,
                s.rowHeight,
                s.leftInset,
                s.topInset,
                s.labelWidth,
                s.valueOffset,
                s.valueWidth,
                s.textColor);
        }
    }
}
