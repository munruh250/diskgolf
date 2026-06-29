using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>Vertical accuracy meter with red / yellow / green / yellow / red zones.</summary>
    public sealed class HeightMeterVisual : MonoBehaviour
    {
        const string VisualName = "HeightMeter";

        const string AccuracyVisualName = "AccuracyMeter";

        const string LegacyVisualName = "NtmHeightMeter";

        public static string VisualNameForFind => VisualName;

        public static Transform FindMeterRoot(Transform parent)
        {
            if (parent == null)
                return null;

            return parent.Find(VisualName)
                ?? parent.Find(AccuracyVisualName)
                ?? parent.Find(LegacyVisualName);
        }

        public static bool IsCurrentLayout(Transform meterRoot) =>
            IsAccuracyLayout(meterRoot);

        public static bool IsAccuracyLayout(Transform meterRoot)
        {
            if (meterRoot == null)
                return false;

            var rt = meterRoot as RectTransform;
            if (rt == null || meterRoot.Find("Track/Frame") == null)
                return false;

            var title = meterRoot.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if (title == null || title.text != "ACCURACY")
                return false;

            int zoneCount = 0;
            var track = meterRoot.Find("Track");
            if (track == null)
                return false;

            foreach (Transform child in track)
            {
                if (child.name == "Zone")
                    zoneCount++;
            }

            return zoneCount >= 5;
        }

        static readonly Color RedColor = new(0.92f, 0.22f, 0.14f, 0.95f);

        static readonly Color YellowColor = new(1f, 0.92f, 0.18f, 0.95f);

        static readonly Color GreenColor = new(0.18f, 0.78f, 0.28f, 0.95f);

        [SerializeField] RectTransform track;

        [SerializeField] RectTransform indicator;

        [SerializeField] RectTransform sweetSpot;

        public static HeightMeterVisual Ensure(Transform parent)
        {
            if (parent == null)
                return null;

            var existing = FindMeterRoot(parent)?.GetComponent<HeightMeterVisual>();
            if (existing != null)
            {
                if (existing.name != VisualName)
                    existing.name = VisualName;

                if (SceneHudAuthoring.IsActive)
                    existing.BindSceneReferences();
                else
                    existing.EnsureBuilt();
                return existing;
            }

            if (SceneHudAuthoring.IsActive)
            {
                Debug.LogWarning("[Disk Golf] HeightMeter not found under TimingMeters. Author it in the scene instead of creating at runtime.");
                return null;
            }

            return Build(parent);
        }

        public void BindSceneReferences()
        {
            track ??= transform.Find("Track") as RectTransform;
            if (track == null)
                return;

            indicator ??= track.Find("Indicator") as RectTransform;
            sweetSpot ??= track.Find("SweetSpot") as RectTransform;
            RefreshAccuracyTitle();
        }

        void RefreshAccuracyTitle()
        {
            var title = transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            ApplyAccuracyTitleStyle(title);
        }

        static void ApplyAccuracyTitleStyle(TextMeshProUGUI title)
        {
            if (title == null)
                return;

            title.color = TimingMeterLayout.MeterTitleColor;
            title.fontStyle = FontStyles.Bold;
        }

        /// <summary>Editor bake: rebuild track as 5-zone accuracy meter if layout is outdated.</summary>
        public void BakeAccuracyLayoutEditor()
        {
            if (IsAccuracyLayout(transform))
            {
                BindSceneReferences();
                return;
            }

            RebuildHeight();
        }

        public void EnsureBuilt()
        {
            if (SceneHudAuthoring.IsActive)
            {
                BindSceneReferences();
                return;
            }

            ApplyRootLayout();

            var root = transform as RectTransform;
            if (track != null
                && root.sizeDelta.y >= TimingMeterLayout.HeightTotal - 1f
                && root.sizeDelta.x >= TimingMeterLayout.HeightWidth - 1f
                && track.Find("Frame") != null)
                return;

            if (track != null)
            {
                RebuildHeight();
                return;
            }

            BuildFromExistingRoot(transform);
        }

        public void ApplyCanonicalLayout()
        {
            ApplyRootLayout();
            RefreshAccuracyTitle();

            if (IsAccuracyLayout(transform)
                && track != null
                && (transform as RectTransform).sizeDelta.y >= TimingMeterLayout.HeightTotal - 1f)
            {
                BindSceneReferences();
                return;
            }

            if (track != null)
            {
                RebuildHeight();
                return;
            }

            BuildFromExistingRoot(transform);
        }

        void RebuildHeight()
        {
            var root = transform as RectTransform;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                    Destroy(root.GetChild(i).gameObject);
                else
                    DestroyImmediate(root.GetChild(i).gameObject);
            }

            track = null;
            indicator = null;
            sweetSpot = null;
            BuildFromExistingRoot(transform);
        }

        void ApplyRootLayout()
        {
            var root = transform as RectTransform;
            root.anchorMin = root.anchorMax = new Vector2(1f, 0f);
            root.pivot = new Vector2(1f, 0f);
            root.anchoredPosition = TimingMeterLayout.HeightAnchorPos;
            root.sizeDelta = new Vector2(TimingMeterLayout.HeightWidth, TimingMeterLayout.HeightTotal);
        }

        public void SetChromeVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        public void SetNeedleVisible(bool visible)
        {
            if (indicator == null)
                return;

            indicator.gameObject.SetActive(visible);
            if (visible)
                EnsureIndicatorDrawOrder();
        }

        void EnsureIndicatorDrawOrder()
        {
            if (indicator == null)
                return;

            indicator.SetAsLastSibling();
            var img = indicator.GetComponent<Image>();
            if (img != null)
                img.color = Color.white;
        }

        static HeightMeterVisual Build(Transform parent)
        {
            var rootGo = new GameObject(VisualName, typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            var visual = rootGo.AddComponent<HeightMeterVisual>();
            visual.BuildFromExistingRoot(rootGo.transform);
            return visual;
        }

        void BuildFromExistingRoot(Transform rootTransform)
        {
            float s = TimingMeterLayout.UiScale;
            var root = rootTransform as RectTransform;
            root.anchorMin = root.anchorMax = new Vector2(1f, 0f);
            root.pivot = new Vector2(1f, 0f);
            root.anchoredPosition = TimingMeterLayout.HeightAnchorPos;
            root.sizeDelta = new Vector2(TimingMeterLayout.HeightWidth, TimingMeterLayout.HeightTotal);

            var trackGo = new GameObject("Track", typeof(RectTransform));
            var trackRt = trackGo.GetComponent<RectTransform>();
            trackRt.SetParent(root, false);
            trackRt.anchorMin = new Vector2(0f, 0f);
            trackRt.anchorMax = new Vector2(0f, 1f);
            trackRt.pivot = new Vector2(0f, 0.5f);
            trackRt.anchoredPosition = Vector2.zero;
            trackRt.sizeDelta = new Vector2(TimingMeterLayout.HeightTrackWidth, -4f * s);
            track = trackRt;

            var frameGo = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            var frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.SetParent(trackRt, false);
            frameRt.anchorMin = Vector2.zero;
            frameRt.anchorMax = Vector2.one;
            frameRt.offsetMin = new Vector2(-3f * s, -3f * s);
            frameRt.offsetMax = new Vector2(3f * s, 3f * s);
            frameGo.transform.SetAsFirstSibling();
            var frameImg = frameGo.GetComponent<Image>();
            frameImg.sprite = ArcRingBuilder.WhiteSprite;
            frameImg.color = new Color(0.06f, 0.06f, 0.06f, 0.94f);
            frameImg.raycastTarget = false;

            AddZone(trackRt, 0f, 0.2f, RedColor);
            AddZone(trackRt, 0.2f, 0.4f, YellowColor);
            AddZone(trackRt, 0.4f, 0.6f, GreenColor);
            AddZone(trackRt, 0.6f, 0.8f, YellowColor);
            AddZone(trackRt, 0.8f, 1f, RedColor);

            var sweetGo = new GameObject("SweetSpot", typeof(RectTransform), typeof(Image));
            var sweetRt = sweetGo.GetComponent<RectTransform>();
            sweetRt.SetParent(trackRt, false);
            sweetRt.anchorMin = sweetRt.anchorMax = new Vector2(0.5f, 0.5f);
            sweetRt.pivot = new Vector2(0.5f, 0.5f);
            sweetRt.sizeDelta = new Vector2(TimingMeterLayout.HeightTrackWidth + 4f * s, 12f * s);
            sweetGo.GetComponent<Image>().color = TimingMeterLayout.SweetSpotColor;
            sweetGo.GetComponent<Image>().sprite = ArcRingBuilder.WhiteSprite;
            sweetGo.GetComponent<Image>().raycastTarget = false;
            sweetSpot = sweetRt;
            sweetGo.SetActive(false);

            var indicatorGo = new GameObject("Indicator", typeof(RectTransform), typeof(Image));
            var indicatorRt = indicatorGo.GetComponent<RectTransform>();
            indicatorRt.SetParent(trackRt, false);
            indicatorRt.anchorMin = indicatorRt.anchorMax = new Vector2(0f, 0.5f);
            indicatorRt.pivot = new Vector2(1f, 0.5f);
            indicatorRt.anchoredPosition = new Vector2(-4f * s, 0f);
            indicatorRt.sizeDelta = new Vector2(14f * s, 5f * s);
            indicatorGo.GetComponent<Image>().color = Color.white;
            indicatorGo.GetComponent<Image>().sprite = ArcRingBuilder.WhiteSprite;
            indicatorGo.GetComponent<Image>().raycastTarget = false;
            indicator = indicatorRt;

            var titleGo = new GameObject("Title", typeof(RectTransform));
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.SetParent(root, false);
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0f, 1f);
            titleRt.pivot = new Vector2(0f, 0f);
            titleRt.anchoredPosition = new Vector2(0f, 6f * s);
            titleRt.sizeDelta = new Vector2(TimingMeterLayout.HeightWidth, 20f * s);
            var title = titleGo.AddComponent<TextMeshProUGUI>();
            title.text = "ACCURACY";
            HudTypography.Apply(title, TextAlignmentOptions.MidlineLeft);
            ApplyAccuracyTitleStyle(title);

            SetNeedleVisible(false);
        }

        static void AddZone(RectTransform track, float minY, float maxY, Color color)
        {
            var go = new GameObject("Zone", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(track, false);
            rt.anchorMin = new Vector2(0f, minY);
            rt.anchorMax = new Vector2(1f, maxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            go.GetComponent<Image>().sprite = ArcRingBuilder.WhiteSprite;
            go.GetComponent<Image>().raycastTarget = false;
        }

        static void AddZoneLabel(RectTransform track, float anchorY, string text, float scale)
        {
            var go = new GameObject(text, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(track, false);
            rt.anchorMin = rt.anchorMax = new Vector2(1f, anchorY);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(6f * scale, 0f);
            rt.sizeDelta = new Vector2(TimingMeterLayout.HeightLabelWidth, 18f * scale);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            HudTypography.Apply(tmp, TextAlignmentOptions.MidlineLeft);
            tmp.raycastTarget = false;
        }

        public void SetIndicator(float normalized01)
        {
            if (indicator == null)
                return;

            normalized01 = Mathf.Clamp01(normalized01);
            float s = TimingMeterLayout.UiScale;
            indicator.anchorMin = indicator.anchorMax = new Vector2(0f, normalized01);
            indicator.anchoredPosition = new Vector2(-4f * s, 0f);
            EnsureIndicatorDrawOrder();
        }

        public void SetSweetSpot(float center01, float width01)
        {
            if (sweetSpot == null)
                return;

            center01 = Mathf.Clamp01(center01);
            width01 = Mathf.Clamp(width01, 0.04f, 0.22f);

            float trackHeight = track != null && track.rect.height > 1f ? track.rect.height : 120f * TimingMeterLayout.UiScale;
            sweetSpot.anchorMin = sweetSpot.anchorMax = new Vector2(0.5f, center01);
            sweetSpot.sizeDelta = new Vector2(
                TimingMeterLayout.HeightTrackWidth + 4f * TimingMeterLayout.UiScale,
                Mathf.Max(12f * TimingMeterLayout.UiScale, width01 * trackHeight));
            sweetSpot.gameObject.SetActive(true);
            sweetSpot.SetAsLastSibling();
            EnsureIndicatorDrawOrder();
        }

        public void HideSweetSpot()
        {
            if (sweetSpot != null)
                sweetSpot.gameObject.SetActive(false);
        }
    }
}
