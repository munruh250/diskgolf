using DiskGolf.Flight;
using DiskGolf.UI.Callouts;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>Facade — delegates to SpriteCalloutBanner on same GameObject.</summary>
    [RequireComponent(typeof(SpriteCalloutBanner))]
    public sealed class LieLandingBannerUI : MonoBehaviour
    {
        const string BannerName = "LieLandingBanner";

        SpriteCalloutBanner _banner;

        public float DisplaySeconds => Banner.DisplaySeconds;

        SpriteCalloutBanner Banner => _banner ??= GetComponent<SpriteCalloutBanner>();

        public static LieLandingBannerUI Ensure()
        {
            var canvas = HudCanvasUtility.FindHudCanvas();
            if (canvas == null)
                return null;

            var bannerTransform = canvas.Find(BannerName);
            if (bannerTransform != null)
                return EnsureOn(bannerTransform.gameObject);

            if (SceneHudAuthoring.IsActive)
            {
                Debug.LogWarning("[Disk Golf] LieLandingBanner missing. Bake GameplayCallouts prefab.");
                return null;
            }

            return Build(canvas);
        }

        public void Show(LieType lie) => Banner.Show(ResolveSprite(lie));

        public void ShowBriefly(LieType lie) => Banner.ShowBriefly(ResolveSprite(lie));

        public void Hide() => Banner.Hide();

        static LieLandingBannerUI EnsureOn(GameObject go)
        {
            EnsureSpritePrimitive(go);
            return go.GetComponent<LieLandingBannerUI>() ?? go.AddComponent<LieLandingBannerUI>();
        }

        static void EnsureSpritePrimitive(GameObject go)
        {
            var image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            image.raycastTarget = false;

            if (go.GetComponent<SpriteCalloutBanner>() == null)
                go.AddComponent<SpriteCalloutBanner>();
        }

        static LieLandingBannerUI Build(RectTransform canvas)
        {
            var bannerGo = new GameObject(BannerName, typeof(RectTransform), typeof(Image));
            var rt = bannerGo.GetComponent<RectTransform>();
            rt.SetParent(canvas, false);
            PostThrowCalloutLayout.ApplyScoreBannerRect(rt);

            var image = bannerGo.GetComponent<Image>();
            image.raycastTarget = false;

            bannerGo.AddComponent<SpriteCalloutBanner>();
            var facade = bannerGo.AddComponent<LieLandingBannerUI>();
            bannerGo.SetActive(false);
            return facade;
        }

        static Sprite ResolveSprite(LieType lie) => lie switch
        {
            LieType.Fairway => ScoreBannerSprites.Fairway,
            LieType.Rough => ScoreBannerSprites.Rough,
            _ => null,
        };
    }
}
