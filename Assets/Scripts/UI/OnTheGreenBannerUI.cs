using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>Brief centered score banner when the disc lands in the circle.</summary>
    public sealed class OnTheGreenBannerUI : MonoBehaviour
    {
        const string HudCanvasName = "GameplayHUD";
        const string BannerName = "OnTheGreenBanner";
        const string LegacyBannerName = "InTheCircleBanner";

        [SerializeField] Image bannerImage;

        [SerializeField] float displaySeconds = 2.25f;

        Coroutine _hideRoutine;

        public float DisplaySeconds => displaySeconds;

        public static OnTheGreenBannerUI Ensure()
        {
            var canvas = FindHudCanvas();
            if (canvas == null)
                return null;

            var existing = canvas.Find(BannerName)?.GetComponent<OnTheGreenBannerUI>();
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
            HideLegacyTextBanner();

            if (bannerImage != null)
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
            bannerImage = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            ApplySprite(bannerImage);
            bannerImage.raycastTarget = false;
            gameObject.SetActive(false);
        }

        static OnTheGreenBannerUI Build(RectTransform canvas)
        {
            HideLegacyTextBanner(canvas);

            var bannerGo = new GameObject(BannerName, typeof(RectTransform), typeof(Image));
            var rt = bannerGo.GetComponent<RectTransform>();
            rt.SetParent(canvas, false);
            ConfigureBannerRect(rt);

            var image = bannerGo.GetComponent<Image>();
            ApplySprite(image);
            image.raycastTarget = false;

            var banner = bannerGo.AddComponent<OnTheGreenBannerUI>();
            banner.bannerImage = image;
            bannerGo.SetActive(false);
            return banner;
        }

        static void HideLegacyTextBanner(RectTransform canvas = null)
        {
            canvas ??= FindHudCanvas();
            var legacy = canvas != null ? canvas.Find(LegacyBannerName) : null;
            if (legacy != null)
                legacy.gameObject.SetActive(false);
        }

        static void ApplySprite(Image image)
        {
            var sprite = ScoreBannerSprites.OnTheGreen;
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = Color.white;
            image.enabled = sprite != null;
        }

        static void ConfigureBannerRect(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.55f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(640f, 160f);
        }

        static RectTransform FindHudCanvas()
        {
            var hud = GameObject.Find(HudCanvasName);
            return hud != null ? hud.GetComponent<RectTransform>() : null;
        }

        public void ShowBriefly()
        {
            EnsureBuilt();

            if (bannerImage == null)
                return;

            ApplySprite(bannerImage);
            transform.SetAsLastSibling();
            gameObject.SetActive(true);

            if (_hideRoutine != null)
                StopCoroutine(_hideRoutine);

            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(displaySeconds);
            Hide();
            _hideRoutine = null;
        }

        public void Hide()
        {
            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }

            gameObject.SetActive(false);
        }
    }
}
