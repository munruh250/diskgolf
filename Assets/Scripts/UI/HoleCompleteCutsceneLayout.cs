using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>Layout for the full-screen post-hole summary overlay.</summary>
    public static class HoleCompleteCutsceneLayout
    {
        public static readonly Vector2 ScoreBannerAnchor = new(0.26f, 0.52f);

        public static readonly Vector2 ScoreBannerSize = new(1740f, 720f);

        public static readonly Vector2 CharacterAnchor = new(0.7f, 0.38f);

        public static readonly Vector2 CharacterSize = new(1600f, 1840f);

        public static void ApplyBackgroundRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        public static void ApplyScoreBannerRect(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = ScoreBannerAnchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = ScoreBannerSize;
        }

        public static void ApplyCharacterRect(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = CharacterAnchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = CharacterSize;
        }
    }
}
