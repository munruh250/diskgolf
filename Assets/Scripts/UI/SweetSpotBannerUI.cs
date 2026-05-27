using TMPro;
using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>Centered overlay when both timing meters hit the sweet spot.</summary>
    public sealed class SweetSpotBannerUI : MonoBehaviour
    {
        const string HudCanvasName = "GameplayHUD";
        const string BannerName = "SweetSpotBanner";

        [SerializeField] TextMeshProUGUI label;

        [SerializeField] float displaySeconds = 2f;

        public float DisplaySeconds => displaySeconds;

        public static SweetSpotBannerUI Ensure()
        {
            var canvas = FindHudCanvas();
            if (canvas == null)
                return null;

            var existing = canvas.Find(BannerName)?.GetComponent<SweetSpotBannerUI>();
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

        static SweetSpotBannerUI Build(RectTransform canvas)
        {
            var bannerGo = new GameObject(BannerName, typeof(RectTransform));
            var rt = bannerGo.GetComponent<RectTransform>();
            rt.SetParent(canvas, false);
            ConfigureBannerRect(rt);

            var tmp = bannerGo.AddComponent<TextMeshProUGUI>();
            ApplyStyle(tmp);

            var banner = bannerGo.AddComponent<SweetSpotBannerUI>();
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
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.62f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(640f, 120f);
        }

        static void ApplyStyle(TextMeshProUGUI tmp)
        {
            tmp.text = "SWEET!";
            tmp.fontSize = 72f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.82f, 0.28f, 1f);
            tmp.outlineWidth = 0.35f;
            tmp.outlineColor = Color.black;
            tmp.raycastTarget = false;

            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font != null)
                tmp.font = font;
        }

        public void Show()
        {
            EnsureBuilt();

            if (label == null)
                return;

            label.text = "SWEET!";
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
