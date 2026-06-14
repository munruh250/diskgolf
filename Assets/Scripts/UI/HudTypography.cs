using DiskGolf.Gameplay;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DiskGolf.UI
{
    /// <summary>Shared HUD text sizing — reads from HudLayoutSettings when present.</summary>
    public static class HudTypography
    {
        static TMP_FontAsset _defaultFont;

        public static TMP_FontAsset DefaultFont
        {
            get
            {
                if (_defaultFont != null)
                    return _defaultFont;

                _defaultFont = Resources.Load<TMP_FontAsset>(ProjectArtPaths.ThirdParty.PixelEmulatorSdfResource);

#if UNITY_EDITOR
                _defaultFont ??= AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    ProjectArtPaths.ThirdParty.PixelEmulatorSdf);
#endif

                return _defaultFont;
            }
        }

        public static void BindFont(TextMeshProUGUI tmp)
        {
            if (tmp == null)
                return;

            var font = DefaultFont;
            if (font != null)
                tmp.font = font;
        }

        public static void BindFont(TextMeshPro tmp)
        {
            if (tmp == null)
                return;

            var font = DefaultFont;
            if (font != null)
                tmp.font = font;
        }

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
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            BindFont(tmp);
        }

        /// <summary>Sets Pixel Emulator on all TMP under <paramref name="root"/> without changing size or color.</summary>
        public static void BindFontsPreservingStyle(Transform root)
        {
            if (root == null)
                return;

            foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                BindFont(tmp);
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

            var bar = canvas.Find("NtmBottomBar");
            if (bar != null)
                BindFontsPreservingStyle(bar);

            var meters = canvas.Find("TimingMeters");
            if (meters != null)
                BindFontsPreservingStyle(meters);

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
                28f, 36f, 72f, 40f, 200f, 268f, 100f, Color.white);

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
