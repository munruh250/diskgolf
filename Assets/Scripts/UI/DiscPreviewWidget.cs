using DiskGolf.Disc;
using DiskGolf.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>NTM-style disc icon in the power-meter hub; swaps with the active bag disc.</summary>
    public sealed class DiscPreviewWidget : MonoBehaviour
    {
        static readonly Color TeeBackdrop = new(0.1f, 0.34f, 0.12f, 1f);

        static float S => TimingMeterLayout.UiScale;

        [SerializeField] Image display;

        [SerializeField] Sprite defaultSprite;

        [SerializeField] DiscBag bag;

        DiscProfile _lastApplied;

        void OnEnable()
        {
            BindReferences();
            if (bag != null)
                bag.SelectionChanged += OnDiscChanged;
            Refresh();
        }

        void OnDisable()
        {
            if (bag != null)
                bag.SelectionChanged -= OnDiscChanged;
        }

        void OnDiscChanged(DiscProfile _) => Refresh();

        public void BindReferences()
        {
            display ??= GetComponent<Image>();
            bag ??= FindObjectOfType<DiscBag>();
            defaultSprite ??= RuntimeArt.LoadDiscPreviewSprite();
            Refresh();
        }

        public void Refresh()
        {
            if (display == null)
                return;

            var profile = bag != null ? bag.Active : null;
            display.sprite = ResolveSprite(profile);
            display.color = ResolveTint(profile);
            display.enabled = display.sprite != null;
        }

        Sprite ResolveSprite(DiscProfile profile)
        {
            if (profile != null && profile.previewSprite != null)
                return profile.previewSprite;

            return defaultSprite ?? RuntimeArt.LoadDiscPreviewSprite();
        }

        static Color ResolveTint(DiscProfile profile)
        {
            if (profile != null && profile.discMaterial != null)
                return profile.discMaterial.color;

            return Color.white;
        }

        /// <summary>Creates the NTM hub + disc preview under an existing ArcHub (editor bake only).</summary>
        public static DiscPreviewWidget BakeIntoArcHub(RectTransform arcHub, Sprite fallbackSprite)
        {
            if (arcHub == null)
                return null;

            var hub = EnsureHub(arcHub);
            var widget = hub.GetComponentInChildren<DiscPreviewWidget>(true);
            if (widget == null)
                widget = CreatePreviewImage(hub);

            widget.defaultSprite = fallbackSprite ?? RuntimeArt.LoadDiscPreviewSprite();
            widget.BindReferences();

            var needle = arcHub.Find("Needle");
            if (needle != null)
                needle.SetAsLastSibling();

            return widget;
        }

        static RectTransform EnsureHub(RectTransform arcHub)
        {
            var existing = arcHub.Find("Hub") as RectTransform;
            if (existing != null)
                return existing;

            var hubGo = new GameObject("Hub", typeof(RectTransform));
            var hubRt = hubGo.GetComponent<RectTransform>();
            hubRt.SetParent(arcHub, false);
            hubRt.anchorMin = hubRt.anchorMax = new Vector2(0.5f, 0f);
            hubRt.pivot = new Vector2(0.5f, 0.5f);
            hubRt.anchoredPosition = Vector2.zero;
            hubRt.sizeDelta = new Vector2(38f * S, 38f * S);

            var backdropGo = new GameObject("HubBackdrop", typeof(RectTransform), typeof(Image));
            var backdropRt = backdropGo.GetComponent<RectTransform>();
            backdropRt.SetParent(hubRt, false);
            backdropRt.anchorMin = Vector2.zero;
            backdropRt.anchorMax = Vector2.one;
            backdropRt.offsetMin = new Vector2(2f * S, 2f * S);
            backdropRt.offsetMax = new Vector2(-2f * S, -2f * S);
            var backdropImg = backdropGo.GetComponent<Image>();
            backdropImg.sprite = ArcRingBuilder.WhiteSprite;
            backdropImg.color = TeeBackdrop;
            backdropImg.raycastTarget = false;

            var ringGo = new GameObject("HubRing", typeof(RectTransform), typeof(Image));
            var ringRt = ringGo.GetComponent<RectTransform>();
            ringRt.SetParent(hubRt, false);
            ringRt.anchorMin = Vector2.zero;
            ringRt.anchorMax = Vector2.one;
            ringRt.offsetMin = ringRt.offsetMax = Vector2.zero;
            var ringImg = ringGo.GetComponent<Image>();
            ringImg.sprite = ArcRingBuilder.WhiteSprite;
            ringImg.color = new Color(0.06f, 0.06f, 0.06f, 0.98f);
            ringImg.raycastTarget = false;

            hubRt.SetSiblingIndex(2);
            return hubRt;
        }

        static DiscPreviewWidget CreatePreviewImage(RectTransform hub)
        {
            var go = new GameObject("DiscPreview", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(hub, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = hub.sizeDelta * 0.82f;

            var img = go.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;

            var widget = go.AddComponent<DiscPreviewWidget>();
            widget.display = img;
            return widget;
        }
    }
}
