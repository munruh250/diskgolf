using System.Collections;
using TMPro;
using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>NTM-style centered popup when the disc stops (e.g. "200 FEET").</summary>
    public sealed class ThrowResultBannerUI : MonoBehaviour
    {
        const string HudRootName = "GameplayHUD";

        [SerializeField] TextMeshProUGUI label;

        [SerializeField] float displaySeconds = 2.75f;

        Coroutine _hideRoutine;

        public float DisplaySeconds => displaySeconds;

        public static ThrowResultBannerUI Ensure()
        {
            var hudRoot = FindHudRoot();
            if (hudRoot == null)
                return null;

            var existing = hudRoot.GetComponentInChildren<ThrowResultBannerUI>(true);
            if (existing != null)
            {
                existing.EnsureBuilt();
                return existing;
            }

            return Build(hudRoot);
        }

        void Awake() => EnsureBuilt();

        public void EnsureBuilt()
        {
            if (label != null)
                return;

            var hudRoot = transform.parent as RectTransform ?? FindHudRoot();
            if (hudRoot == null)
                return;

            if (transform.parent != hudRoot)
                transform.SetParent(hudRoot, false);

            label = CreateLabel(transform as RectTransform ?? gameObject.AddComponent<RectTransform>());
            ApplyStyle(label);
            gameObject.SetActive(false);
        }

        static ThrowResultBannerUI Build(RectTransform hudRoot)
        {
            var bannerGo = new GameObject("ThrowResultBanner", typeof(RectTransform));
            var rt = bannerGo.GetComponent<RectTransform>();
            rt.SetParent(hudRoot, false);
            ConfigureBannerRect(rt);

            var banner = bannerGo.AddComponent<ThrowResultBannerUI>();
            banner.label = CreateLabel(rt);
            ApplyStyle(banner.label);
            bannerGo.SetActive(false);
            return banner;
        }

        static RectTransform FindHudRoot()
        {
            var hud = GameObject.Find(HudRootName);
            if (hud == null)
                return null;

            var hudRoot = hud.transform.Find("HudRoot") as RectTransform;
            return hudRoot != null ? hudRoot : hud.GetComponent<RectTransform>();
        }

        static void ConfigureBannerRect(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(900f, 140f);
        }

        static TextMeshProUGUI CreateLabel(RectTransform parent)
        {
            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.SetParent(parent, false);
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            return labelGo.AddComponent<TextMeshProUGUI>();
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

            var circleBanner = GameObject.Find("GameplayHUD")?.transform.Find("HudRoot/TMPRow");
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

            if (_hideRoutine != null)
                StopCoroutine(_hideRoutine);

            _hideRoutine = StartCoroutine(HideAfterDelay());
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

        IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(displaySeconds);
            _hideRoutine = null;
            gameObject.SetActive(false);
        }
    }
}
