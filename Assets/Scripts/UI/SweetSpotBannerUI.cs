using DiskGolf.UI.Callouts;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>Facade — delegates to SpriteCalloutBanner on same GameObject.</summary>
    [RequireComponent(typeof(SpriteCalloutBanner))]
    public sealed class SweetSpotBannerUI : MonoBehaviour
    {
        const string BannerName = "SweetSpotBanner";

        SpriteCalloutBanner _banner;

        public float DisplaySeconds => Banner.DisplaySeconds;

        SpriteCalloutBanner Banner => _banner ??= GetComponent<SpriteCalloutBanner>();

        public static SweetSpotBannerUI Ensure()
        {
            var host = GameplayCalloutHost.Ensure();
            var nested = host != null ? host.GetComponentInChildren<SweetSpotBannerUI>(true) : null;
            if (nested != null)
                return nested;

            var canvas = HudCanvasUtility.FindHudCanvas();
            if (canvas == null)
                return null;

            var bannerTransform = canvas.Find(BannerName);
            if (bannerTransform != null)
                return EnsureOn(bannerTransform.gameObject);

            if (SceneHudAuthoring.IsActive)
            {
                Debug.LogWarning("[Disk Golf] SweetSpotBanner missing. Bake GameplayCallouts prefab.");
                return null;
            }

            return Build(canvas);
        }

        public void Show() => Banner.Show(ScoreBannerSprites.SweetSpot);

        public void ShowBriefly() => Banner.ShowBriefly(ScoreBannerSprites.SweetSpot);

        public void Hide() => Banner.Hide();

        static SweetSpotBannerUI EnsureOn(GameObject go)
        {
            EnsureSpritePrimitive(go);
            return go.GetComponent<SweetSpotBannerUI>() ?? go.AddComponent<SweetSpotBannerUI>();
        }

        static void EnsureSpritePrimitive(GameObject go)
        {
            var image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            image.raycastTarget = false;

            if (go.GetComponent<SpriteCalloutBanner>() == null)
                go.AddComponent<SpriteCalloutBanner>();
        }

        static SweetSpotBannerUI Build(RectTransform canvas)
        {
            var bannerGo = new GameObject(BannerName, typeof(RectTransform), typeof(Image));
            var rt = bannerGo.GetComponent<RectTransform>();
            rt.SetParent(canvas, false);
            PostThrowCalloutLayout.ApplyScoreBannerRect(rt);

            var image = bannerGo.GetComponent<Image>();
            image.raycastTarget = false;

            bannerGo.AddComponent<SpriteCalloutBanner>();
            var facade = bannerGo.AddComponent<SweetSpotBannerUI>();
            bannerGo.SetActive(false);
            return facade;
        }
    }
}
