using DiskGolf.UI.Callouts;
using TMPro;
using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>Facade — delegates to TextCalloutBanner on same GameObject.</summary>
    [RequireComponent(typeof(TextCalloutBanner))]
    public sealed class ThrowResultBannerUI : MonoBehaviour
    {
        const string BannerName = "ThrowResultBanner";

        TextCalloutBanner _banner;

        public float DisplaySeconds => Banner.DisplaySeconds;

        TextCalloutBanner Banner => _banner ??= GetComponent<TextCalloutBanner>();

        public static ThrowResultBannerUI Ensure()
        {
            var canvas = HudCanvasUtility.FindHudCanvas();
            if (canvas == null)
                return null;

            var bannerTransform = canvas.Find(BannerName);
            if (bannerTransform != null)
                return EnsureOn(bannerTransform.gameObject);

            if (SceneHudAuthoring.IsActive)
            {
                Debug.LogWarning("[Disk Golf] ThrowResultBanner missing. Bake GameplayCallouts prefab.");
                return null;
            }

            return Build(canvas);
        }

        public static void ApplyStyle(TextMeshProUGUI tmp)
        {
            tmp.fontSize = 64f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Top;
            tmp.color = Color.black;
            tmp.outlineWidth = 0f;
            tmp.raycastTarget = false;

            var circleBanner = GameObject.Find(HudCanvasUtility.HudCanvasName)?.transform.Find("InTheCircleBanner")
                ?? GameObject.Find(HudCanvasUtility.HudCanvasName)?.transform.Find("TMPRow");
            var circleTmp = circleBanner != null ? circleBanner.GetComponent<TextMeshProUGUI>() : null;
            if (circleTmp != null && circleTmp.font != null)
                tmp.font = circleTmp.font;
            else
                HudTypography.BindFont(tmp);
        }

        public void ShowThrowDistance(float distanceFt) => Banner.ShowThrowDistance(distanceFt);

        public void Hide() => Banner.Hide();

        static ThrowResultBannerUI EnsureOn(GameObject go)
        {
            EnsureTextPrimitive(go);
            return go.GetComponent<ThrowResultBannerUI>() ?? go.AddComponent<ThrowResultBannerUI>();
        }

        static void EnsureTextPrimitive(GameObject go)
        {
            var label = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
            ApplyStyle(label);

            if (go.GetComponent<TextCalloutBanner>() == null)
            {
                var banner = go.AddComponent<TextCalloutBanner>();
                banner.Layout = TextCalloutLayout.FeetLabel;
            }
        }

        static ThrowResultBannerUI Build(RectTransform canvas)
        {
            var bannerGo = new GameObject(BannerName, typeof(RectTransform));
            var rt = bannerGo.GetComponent<RectTransform>();
            rt.SetParent(canvas, false);
            PostThrowCalloutLayout.ApplyFeetLabelRect(rt);

            var tmp = bannerGo.AddComponent<TextMeshProUGUI>();
            ApplyStyle(tmp);

            bannerGo.AddComponent<TextCalloutBanner>();
            var facade = bannerGo.AddComponent<ThrowResultBannerUI>();
            bannerGo.SetActive(false);
            return facade;
        }
    }
}
