using TMPro;
using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>Shared HUD text sizing and styling.</summary>
    public static class NtmHudTypography
    {
        public const float FontSize = 28f;

        public const float RowHeight = 36f;

        public const float LeftInset = 72f;

        public const float TopInset = 40f;

        public const float LabelWidth = 200f;

        public const float ValueOffset = 210f;

        public const float ValueWidth = 100f;

        public static readonly Color TextColor = Color.white;

        public static void Apply(TextMeshProUGUI tmp, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            if (tmp == null)
                return;

            tmp.fontSize = FontSize;
            tmp.color = TextColor;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = align;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
        }
    }
}
