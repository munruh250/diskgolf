using System.Collections;
using DiskGolf.Flight;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>Brief centered banner when the disc lands on fairway or rough.</summary>
    public sealed class LieLandingBannerUI : MonoBehaviour
    {
        const string HudCanvasName = "GameplayHUD";
        const string BannerName = "LieLandingBanner";

        [SerializeField] Image bannerImage;

        [SerializeField] float displaySeconds = 2.25f;

        Coroutine _hideRoutine;

        public float DisplaySeconds => displaySeconds;

        public static LieLandingBannerUI Ensure()
        {
            var canvas = FindHudCanvas();
            if (canvas == null)
                return null;

            var existing = canvas.Find(BannerName)?.GetComponent<LieLandingBannerUI>();
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
            bannerImage.raycastTarget = false;
            gameObject.SetActive(false);
        }

        static LieLandingBannerUI Build(RectTransform canvas)
        {
            var bannerGo = new GameObject(BannerName, typeof(RectTransform), typeof(Image));
            var rt = bannerGo.GetComponent<RectTransform>();
            rt.SetParent(canvas, false);
            ConfigureBannerRect(rt);

            var image = bannerGo.GetComponent<Image>();
            image.raycastTarget = false;

            var banner = bannerGo.AddComponent<LieLandingBannerUI>();
            banner.bannerImage = image;
            bannerGo.SetActive(false);
            return banner;
        }

        static void ConfigureBannerRect(RectTransform rt) =>
            PostThrowCalloutLayout.ApplyScoreBannerRect(rt);

        static RectTransform FindHudCanvas()
        {
            var hud = GameObject.Find(HudCanvasName);
            return hud != null ? hud.GetComponent<RectTransform>() : null;
        }

        public void Show(LieType lie)
        {
            EnsureBuilt();

            if (bannerImage == null)
                return;

            var sprite = ResolveSprite(lie);
            if (sprite == null)
                return;

            bannerImage.sprite = sprite;
            bannerImage.preserveAspect = true;
            bannerImage.color = Color.white;
            bannerImage.enabled = true;

            ConfigureBannerRect(transform as RectTransform);
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void ShowBriefly(LieType lie)
        {
            Show(lie);

            if (_hideRoutine != null)
                StopCoroutine(_hideRoutine);

            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        static Sprite ResolveSprite(LieType lie) => lie switch
        {
            LieType.Fairway => ScoreBannerSprites.Fairway,
            LieType.Rough => ScoreBannerSprites.Rough,
            _ => null,
        };

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
