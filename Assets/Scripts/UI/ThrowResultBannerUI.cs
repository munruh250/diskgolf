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

        static void ConfigureBannerRect(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(900f, 140f);
        }

        public static void ApplyStyle(TextMeshProUGUI tmp)
        {
            tmp.text = "200 FEET";
            tmp.fontSize = 64f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.42f, 0.96f, 0.52f);
            tmp.outlineWidth = 0.32f;
            tmp.outlineColor = Color.black;
            tmp.raycastTarget = false;

            var circleBanner = GameObject.Find(HudCanvasName)?.transform.Find("InTheCircleBanner")
                ?? GameObject.Find(HudCanvasName)?.transform.Find("TMPRow");
            var circleTmp = circleBanner != null ? circleBanner.GetComponent<TextMeshProUGUI>() : null;
            if (circleTmp != null && circleTmp.font != null)
                tmp.font = circleTmp.font;
            else
            {
                var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (font != null)
                    tmp.font = font;
            }
        }

        public void ShowThrowDistance(float distanceFt)
        {
            EnsureBuilt();

            if (label == null)
                return;

            int feet = Mathf.Max(0, Mathf.RoundToInt(distanceFt));
            label.text = $"{feet} FEET";
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
