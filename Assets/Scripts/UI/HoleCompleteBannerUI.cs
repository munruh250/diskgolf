using DiskGolf.Core;
using TMPro;
using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>Centered overlay when the disc finishes in the basket.</summary>
    public sealed class HoleCompleteBannerUI : MonoBehaviour
    {
        const string HudCanvasName = "GameplayHUD";
        const string BannerName = "HoleCompleteBanner";

        [SerializeField] TextMeshProUGUI titleLabel;

        [SerializeField] TextMeshProUGUI scoreLabel;

        [SerializeField] float displaySeconds = 3.5f;

        public float DisplaySeconds => displaySeconds;

        public static HoleCompleteBannerUI Ensure()
        {
            var canvas = FindHudCanvas();
            if (canvas == null)
                return null;

            var existing = canvas.Find(BannerName)?.GetComponent<HoleCompleteBannerUI>();
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
            if (titleLabel != null && scoreLabel != null)
                return;

            var canvas = transform.parent as RectTransform ?? FindHudCanvas();
            if (canvas == null)
                return;

            if (transform.parent != canvas)
            {
                transform.SetParent(canvas, false);
                gameObject.name = BannerName;
            }

            var root = transform as RectTransform;
            ConfigureRootRect(root);

            titleLabel ??= CreateLine(root, "Title", new Vector2(0f, 28f), 56f);
            scoreLabel ??= CreateLine(root, "Score", new Vector2(0f, -36f), 40f);

            ApplyTitleStyle(titleLabel);
            ApplyScoreStyle(scoreLabel);
            gameObject.SetActive(false);
        }

        static HoleCompleteBannerUI Build(RectTransform canvas)
        {
            var bannerGo = new GameObject(BannerName, typeof(RectTransform));
            var rt = bannerGo.GetComponent<RectTransform>();
            rt.SetParent(canvas, false);
            ConfigureRootRect(rt);

            var title = CreateLine(rt, "Title", new Vector2(0f, 28f), 56f);
            var score = CreateLine(rt, "Score", new Vector2(0f, -36f), 40f);
            ApplyTitleStyle(title);
            ApplyScoreStyle(score);

            var banner = bannerGo.AddComponent<HoleCompleteBannerUI>();
            banner.titleLabel = title;
            banner.scoreLabel = score;
            bannerGo.SetActive(false);
            return banner;
        }

        static TextMeshProUGUI CreateLine(RectTransform parent, string name, Vector2 offset, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(900f, 80f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        static void ConfigureRootRect(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(900f, 180f);
        }

        static void ApplyTitleStyle(TextMeshProUGUI tmp)
        {
            tmp.text = "HOLE COMPLETE";
            tmp.color = new Color(1f, 0.92f, 0.35f);
            tmp.outlineWidth = 0.32f;
            tmp.outlineColor = Color.black;
            CopyFont(tmp);
        }

        static void ApplyScoreStyle(TextMeshProUGUI tmp)
        {
            tmp.text = "3 — PAR";
            tmp.color = Color.white;
            tmp.outlineWidth = 0.28f;
            tmp.outlineColor = Color.black;
            CopyFont(tmp);
        }

        static void CopyFont(TextMeshProUGUI tmp)
        {
            var throwBanner = GameObject.Find(HudCanvasName)?.transform.Find("ThrowResultBanner")
                ?.GetComponent<TextMeshProUGUI>();

            if (throwBanner != null && throwBanner.font != null)
            {
                tmp.font = throwBanner.font;
                return;
            }

            HudTypography.BindFont(tmp);
        }

        static RectTransform FindHudCanvas()
        {
            var hud = GameObject.Find(HudCanvasName);
            return hud != null ? hud.GetComponent<RectTransform>() : null;
        }

        public void Show(int strokes, int par)
        {
            EnsureBuilt();

            if (titleLabel == null || scoreLabel == null)
                return;

            titleLabel.text = "HOLE COMPLETE";
            scoreLabel.text = HoleScore.CompletedLine(strokes, par);
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
