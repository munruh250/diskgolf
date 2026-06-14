using DiskGolf.UI.Callouts;
using UnityEngine;

namespace DiskGolf.UI
{
    [RequireComponent(typeof(MultiSlotSpriteBanner))]
    public sealed class HoleCompleteBannerUI : MonoBehaviour
    {
        const string BannerName = "HoleCompleteBanner";

        MultiSlotSpriteBanner _banner;

        MultiSlotSpriteBanner Banner => _banner ??= GetComponent<MultiSlotSpriteBanner>();

        public float DisplaySeconds => Banner.DisplaySeconds;

        public static HoleCompleteBannerUI Ensure()
        {
            var host = GameplayCalloutHost.Ensure();
            if (host == null)
                return null;

            return host.GetComponentInChildren<HoleCompleteBannerUI>(true);
        }

        public static HoleCompleteBannerUI CreateForScene(RectTransform hud)
        {
            if (hud == null)
                return null;

            var existing = hud.Find(BannerName)?.GetComponent<HoleCompleteBannerUI>();
            if (existing != null)
            {
                existing.BindSceneReferences();
                return existing;
            }

            var root = new GameObject(BannerName, typeof(RectTransform), typeof(MultiSlotSpriteBanner), typeof(HoleCompleteBannerUI));
            var rt = root.GetComponent<RectTransform>();
            rt.SetParent(hud, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(640f, 160f);

            var facade = root.GetComponent<HoleCompleteBannerUI>();
            facade.BindSceneReferences();
            return facade;
        }

        public void Show(int strokes, int par) => Banner.Show(strokes, par);

        public void Hide() => Banner.Hide();

        public void BindSceneReferences() => Banner.BindSceneReferences();

        public void EnsureSceneLayout() => Banner.BindSceneReferences();

        public void ApplyEditorPreview() => Banner.ShowKind(HoleCompleteScoreKind.Par);
    }
}
