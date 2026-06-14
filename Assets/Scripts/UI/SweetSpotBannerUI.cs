using DiskGolf.UI.Callouts;
using TMPro;
using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>Facade — delegates to TextCalloutBanner on same GameObject.</summary>
    [RequireComponent(typeof(TextCalloutBanner))]
    public sealed class SweetSpotBannerUI : MonoBehaviour
    {
        const string BannerName = "SweetSpotBanner";

        TextCalloutBanner _banner;

        public float DisplaySeconds => Banner.DisplaySeconds;

        TextCalloutBanner Banner => _banner ??= GetComponent<TextCalloutBanner>();

        public static SweetSpotBannerUI Ensure()
        {
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

        public void Show()
        {
            Banner.ApplySweetSpotStyle();
            Banner.Show("SWEET!");
        }

        public void ShowBriefly()
        {
            Banner.ApplySweetSpotStyle();
            Banner.ShowBriefly("SWEET!");
        }

        public void Hide() => Banner.Hide();

        static SweetSpotBannerUI EnsureOn(GameObject go)
        {
            EnsureTextPrimitive(go);
            return go.GetComponent<SweetSpotBannerUI>() ?? go.AddComponent<SweetSpotBannerUI>();
        }

        static void EnsureTextPrimitive(GameObject go)
        {
            var label = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
            label.raycastTarget = false;

            var banner = go.GetComponent<TextCalloutBanner>() ?? go.AddComponent<TextCalloutBanner>();
            SetLayout(banner, TextCalloutLayout.CenterPopup);
        }

        static SweetSpotBannerUI Build(RectTransform canvas)
        {
            var bannerGo = new GameObject(BannerName, typeof(RectTransform));
            var rt = bannerGo.GetComponent<RectTransform>();
            rt.SetParent(canvas, false);

            bannerGo.AddComponent<TextMeshProUGUI>();
            var textBanner = bannerGo.AddComponent<TextCalloutBanner>();
            SetLayout(textBanner, TextCalloutLayout.CenterPopup);

            var facade = bannerGo.AddComponent<SweetSpotBannerUI>();
            bannerGo.SetActive(false);
            return facade;
        }

        static void SetLayout(TextCalloutBanner banner, TextCalloutLayout layout) =>
            banner.Layout = layout;
    }
}
