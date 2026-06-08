using TMPro;
using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>Centered popup when the disc stops (e.g. "200 FEET").</summary>
    public sealed class ThrowResultBannerUI : MonoBehaviour
    {
        const string HudCanvasName = "GameplayHUD";
        const string BannerName = "ThrowResultBanner";

        [SerializeField] TextMeshProUGUI label;

        [SerializeField] float displaySeconds = 2.75f;

        public float DisplaySeconds => displaySeconds;

        public static ThrowResultBannerUI Ensure()
        {
            var canvas = FindHudCanvas();
            if (canvas == null)
                return null;

            var existing = canvas.Find(BannerName)?.GetComponent<ThrowResultBannerUI>();
            if (existing != null)
            {
                existing.EnsureBuilt();
                return existing;
            }

            return Build(canvas);
        }

        void Awake() => EnsureBuilt();

        public void EnsureBuilt()
        {
            if (label != null)
                return;

            var canvas = transform.parent as RectTransform ?? FindHudCanvas();
            if (canvas == null)
                return;

            if (transform.parent != canvas)
            {
                transform.SetParent(canvas, false);
                gameObject.name = BannerName;
            }

            ConfigureBannerRect(transform as RectTransform);
            label = GetComponent<TextMeshProUGUI>() ?? gameObject.AddComponent<TextMeshProUGUI>();
            ApplyStyle(label);
            gameObject.SetActive(false);
        }

        static ThrowResultBannerUI Build(RectTransform canvas)
        {
            var bannerGo = new GameObject(BannerName, typeof(RectTransform));
            var rt = bannerGo.GetComponent<RectTransform>();
            rt.SetParent(canvas, false);
            ConfigureBannerRect(rt);

            var tmp = bannerGo.AddComponent<TextMeshProUGUI>();
            ApplyStyle(tmp);

            var banner = bannerGo.AddComponent<ThrowResultBannerUI>();
            banner.label = tmp;
            bannerGo.SetActive(false);
            return banner;
        }

        static RectTransform FindHudCanvas()
        {
            var hud = GameObject.Find(HudCanvasName);
            return hud != null ? hud.GetComponent<RectTransform>() : null;
        }

        static void ConfigureBannerRect(RectTransform rt) =>
            PostThrowCalloutLayout.ApplyFeetLabelRect(rt);

        public static void ApplyStyle(TextMeshProUGUI tmp)
        {
            tmp.fontSize = 64f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Top;
            tmp.color = Color.black;
            tmp.outlineWidth = 0f;
            tmp.raycastTarget = false;

            var circleBanner = GameObject.Find(HudCanvasName)?.transform.Find("InTheCircleBanner")
                ?? GameObject.Find(HudCanvasName)?.transform.Find("TMPRow");
            var circleTmp = circleBanner != null ? circleBanner.GetComponent<TextMeshProUGUI>() : null;
            if (circleTmp != null && circleTmp.font != null)
                tmp.font = circleTmp.font;
            else
                HudTypography.BindFont(tmp);
        }

        public void ShowThrowDistance(float distanceFt)
        {
            EnsureBuilt();

            if (label == null)
                return;

            ApplyStyle(label);
            int feet = Mathf.Max(0, Mathf.RoundToInt(distanceFt));
            label.text = $"{feet} FEET";
            label.ForceMeshUpdate();
            ConfigureBannerRect(transform as RectTransform);
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
