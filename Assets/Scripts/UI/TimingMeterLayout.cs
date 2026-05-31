using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>Shared placement for timing meters.</summary>
    public static class TimingMeterLayout
    {
        /// <summary>UI scale multiplier for timing meters (2 = testing size).</summary>
        public const float UiScale = 2f;

        public const int LayoutVersion = 4;

        public const float BottomInset = 68f;

        /// <summary>Extra inset keeps height labels on screen.</summary>
        public const float RightInset = 48f;

        public const float ClusterGap = 8f * UiScale;

        public static readonly Color SweetSpotColor = new(0.78f, 0.18f, 1f, 1f);

        public static float ArcRadius => 72f * UiScale;

        /// <summary>Lift pivot so the arc's 6pm point sits on the widget bottom edge.</summary>
        public static float PowerPivotYOffset => ArcRadius;

        public static float HeightTrackWidth => 34f * UiScale;

        public static float HeightLabelWidth => 52f * UiScale;

        public static float HeightWidth => HeightTrackWidth + HeightLabelWidth + 8f * UiScale;

        public static float HeightTotal => 132f * UiScale;

        /// <summary>Hub on the right; arc opens left with room for 50% label.</summary>
        public static float PowerWidth => ArcRadius + 96f * UiScale;

        public static float PowerTotal => ArcRadius * 2f + 40f * UiScale;

        public static Vector2 HeightAnchorPos =>
            new(-RightInset, BottomInset);

        /// <summary>Immediately left of the height meter, both sharing the same bottom inset.</summary>
        public static Vector2 PowerAnchorPos =>
            new(-(RightInset + HeightWidth + ClusterGap), BottomInset);
    }
}
