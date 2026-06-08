using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>Shared placement for post-landing score sprites with feet readout underneath.</summary>
    public static class PostThrowCalloutLayout
    {
        public const float BannerCenterY = 0.55f;

        public static readonly Vector2 ScoreBannerSize = new(1280f, 320f);

        public static readonly Vector2 FeetLabelSize = new(900f, 64f);

        const float FeetGap = 0f;

        public static void ApplyScoreBannerRect(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, BannerCenterY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = ScoreBannerSize;
        }

        public static void ApplyFeetLabelRect(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, BannerCenterY);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -ScoreBannerSize.y * 0.5f - FeetGap);
            rt.sizeDelta = FeetLabelSize;
        }
    }
}
