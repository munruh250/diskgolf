using DiskGolf.UI.Callouts;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>Facade — delegates to SpriteCalloutBanner on same GameObject.</summary>
    [RequireComponent(typeof(SpriteCalloutBanner))]
    public sealed class OnTheGreenBannerUI : MonoBehaviour
    {
        const string BannerName = "OnTheGreenBanner";
        const string LegacyBannerName = "InTheCircleBanner";

        SpriteCalloutBanner _banner;

        public float DisplaySeconds => Banner.DisplaySeconds;

        SpriteCalloutBanner Banner => _banner ??= GetComponent<SpriteCalloutBanner>();

        public static OnTheGreenBannerUI Ensure()
        {
            var canvas = HudCanvasUtility.FindHudCanvas();
            if (canvas == null)
                return null;

            var bannerTransform = canvas.Find(BannerName);
            if (bannerTransform != null)
                return EnsureOn(bannerTransform.gameObject);

            if (SceneHudAuthoring.IsActive)
            {
                Debug.LogWarning("[Disk Golf] OnTheGreenBanner missing. Bake GameplayCallouts prefab.");
                return null;
            }

            return Build(canvas);
        }

        void Awake()
        {
            var canvas = transform.parent;
            canvas?.Find(LegacyBannerName)?.gameObject.SetActive(false);
        }

        public void Show() => Banner.Show(ScoreBannerSprites.OnTheGreen);

        public void ShowBriefly() => Banner.ShowBriefly(ScoreBannerSprites.OnTheGreen);

        public void Hide() => Banner.Hide();

        static OnTheGreenBannerUI EnsureOn(GameObject go)
        {
            EnsureSpritePrimitive(go);
            return go.GetComponent<OnTheGreenBannerUI>() ?? go.AddComponent<OnTheGreenBannerUI>();
        }

        static void EnsureSpritePrimitive(GameObject go)
        {
            var image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            image.raycastTarget = false;

            if (go.GetComponent<SpriteCalloutBanner>() == null)
                go.AddComponent<SpriteCalloutBanner>();
        }

        static OnTheGreenBannerUI Build(RectTransform canvas)
        {
            var legacy = canvas.Find(LegacyBannerName);
            if (legacy != null)
                legacy.gameObject.SetActive(false);

            var bannerGo = new GameObject(BannerName, typeof(RectTransform), typeof(Image));
            var rt = bannerGo.GetComponent<RectTransform>();
            rt.SetParent(canvas, false);
            PostThrowCalloutLayout.ApplyScoreBannerRect(rt);

            var image = bannerGo.GetComponent<Image>();
            image.raycastTarget = false;

            bannerGo.AddComponent<SpriteCalloutBanner>();
            var facade = bannerGo.AddComponent<OnTheGreenBannerUI>();
            bannerGo.SetActive(false);
            return facade;
        }
    }
}
