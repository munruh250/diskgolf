using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>Semicircle shot-power meter.</summary>
    public sealed class PowerMeterVisual : MonoBehaviour
    {
        // Clock face: 0% at 6pm (bottom), 50% at 9pm (left), 100% at 12pm (top).
        public const float ArcStartDeg = 270f;

        public const float ArcEndDeg = 90f;

        static float S => TimingMeterLayout.UiScale;

        static float TrackThickness => 14f * S;

        static float BackdropThickness => 20f * S;

        const string VisualName = "PowerMeter";

        const string LayoutMarker = "TrackColorV4";

        public static string VisualNameForFind => VisualName;

        static float ArcRadius => TimingMeterLayout.ArcRadius;

        static float PivotYOffset => TimingMeterLayout.PowerPivotYOffset;

        static readonly Color GreenLow = new(0.28f, 0.82f, 0.34f, 1f);

        static readonly Color YellowMid = new(1f, 0.92f, 0.18f, 1f);

        static readonly Color RedHigh = new(0.95f, 0.22f, 0.14f, 1f);

        [SerializeField] RectTransform pivot;

        [SerializeField] RectTransform needle;

        [SerializeField] RectTransform sweetSpotRoot;

        [SerializeField] TextMeshProUGUI label50;

        [SerializeField] TextMeshProUGUI label100;

        public static PowerMeterVisual Ensure(Transform parent)
        {
            if (parent == null)
                return null;

            var existing = parent.Find(VisualName)?.GetComponent<PowerMeterVisual>()
                ?? parent.Find("NtmPowerMeter")?.GetComponent<PowerMeterVisual>();
            if (existing != null)
            {
                if (existing.name != VisualName)
                    existing.name = VisualName;
                existing.EnsureBuilt();
                return existing;
            }

            return Build(parent);
        }

        public void EnsureBuilt()
        {
            ApplyRootLayout();

            if (pivot != null && pivot.Find("ArcHub/" + LayoutMarker) != null)
            {
                var root = transform as RectTransform;
                if (root.sizeDelta.x >= TimingMeterLayout.PowerWidth - 1f
                    && Vector2.Distance(root.anchoredPosition, TimingMeterLayout.PowerAnchorPos) < 1f)
                    return;
            }

            if (pivot != null)
            {
                RebuildMeterArt();
                return;
            }

            BuildFromExistingRoot(transform);
        }

        void ApplyRootLayout()
        {
            var root = transform as RectTransform;
            root.anchorMin = root.anchorMax = new Vector2(1f, 0f);
            root.pivot = new Vector2(1f, 0f);
            root.anchoredPosition = TimingMeterLayout.PowerAnchorPos;
            root.sizeDelta = new Vector2(TimingMeterLayout.PowerWidth, TimingMeterLayout.PowerTotal);
        }

        void RebuildMeterArt()
        {
            ArcRingBuilder.ClearChildren(pivot);

            var root = transform as RectTransform;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child != pivot)
                {
                    if (Application.isPlaying)
                        Destroy(child.gameObject);
                    else
                        DestroyImmediate(child.gameObject);
                }
            }

            var arcHub = CreateArcHub(pivot);
            BuildArcArt(arcHub);
            CreateNeedleAndSweetSpot(arcHub);
            BuildArcLabels(arcHub);

            SetNeedleVisible(false);
        }

        public void SetChromeVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        public void SetNeedleVisible(bool visible)
        {
            if (needle != null)
                needle.gameObject.SetActive(visible);
        }

        static PowerMeterVisual Build(Transform parent)
        {
            var rootGo = new GameObject(VisualName, typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            var visual = rootGo.AddComponent<PowerMeterVisual>();
            visual.BuildFromExistingRoot(rootGo.transform);
            return visual;
        }

        void BuildFromExistingRoot(Transform rootTransform)
        {
            ApplyRootLayout();

            var root = rootTransform as RectTransform;

            var pivotGo = new GameObject("Pivot", typeof(RectTransform));
            var pivotRt = pivotGo.GetComponent<RectTransform>();
            pivotRt.SetParent(root, false);
            pivotRt.anchorMin = pivotRt.anchorMax = new Vector2(1f, 0f);
            pivotRt.pivot = new Vector2(1f, 0f);
            pivotRt.anchoredPosition = new Vector2(-6f * S, PivotYOffset);
            pivotRt.sizeDelta = new Vector2(ArcRadius + 40f * S, ArcRadius + 32f * S);
            pivot = pivotRt;

            var arcHub = CreateArcHub(pivotRt);
            BuildArcArt(arcHub);
            CreateNeedleAndSweetSpot(arcHub);
            BuildArcLabels(arcHub);

            SetNeedleVisible(false);
        }

        static RectTransform CreateArcHub(RectTransform parent)
        {
            var hubGo = new GameObject("ArcHub", typeof(RectTransform));
            var hubRt = hubGo.GetComponent<RectTransform>();
            hubRt.SetParent(parent, false);
            hubRt.anchorMin = hubRt.anchorMax = new Vector2(1f, 0f);
            hubRt.pivot = new Vector2(1f, 0f);
            hubRt.anchoredPosition = Vector2.zero;
            hubRt.sizeDelta = Vector2.zero;
            return hubRt;
        }

        static void BuildArcArt(RectTransform pivotRt)
        {
            ArcRingBuilder.CreateRing(
                pivotRt,
                "TrackBackdrop",
                ArcStartDeg,
                ArcEndDeg,
                ArcRadius,
                BackdropThickness,
                new Color(0.06f, 0.06f, 0.06f, 0.94f),
                38);

            ArcRingBuilder.CreateGradientRing(
                pivotRt,
                LayoutMarker,
                ArcStartDeg,
                ArcEndDeg,
                ArcRadius,
                TrackThickness,
                GreenLow,
                YellowMid,
                RedHigh,
                0.5f,
                34);

            var hubGo = new GameObject("Hub", typeof(RectTransform), typeof(Image));
            var hubRt = hubGo.GetComponent<RectTransform>();
            hubRt.SetParent(pivotRt, false);
            hubRt.anchorMin = hubRt.anchorMax = new Vector2(0.5f, 0f);
            hubRt.pivot = new Vector2(0.5f, 0.5f);
            hubRt.anchoredPosition = Vector2.zero;
            hubRt.sizeDelta = new Vector2(34f * S, 34f * S);
            var hubImg = hubGo.GetComponent<Image>();
            hubImg.sprite = ArcRingBuilder.WhiteSprite;
            hubImg.color = new Color(0.12f, 0.38f, 0.14f, 0.95f);
            hubImg.raycastTarget = false;
        }

        void BuildArcLabels(RectTransform pivotRt)
        {
            float angle50 = Mathf.Lerp(ArcStartDeg, ArcEndDeg, 0.5f);
            float labelRadius = ArcRadius + 24f * S;

            label50 = AddArcLabel(pivotRt, "50%", angle50, labelRadius, new Vector2(-14f * S, 0f));
            label100 = AddArcLabel(pivotRt, "100%", ArcEndDeg, labelRadius + 6f * S, new Vector2(0f, 10f * S));
        }

        static TextMeshProUGUI AddArcLabel(
            RectTransform pivotRt,
            string text,
            float degrees,
            float radius,
            Vector2 offset)
        {
            var go = new GameObject(text, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(pivotRt, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = ArcRingBuilder.PointOnArc(degrees, radius) + offset;
            rt.sizeDelta = new Vector2(52f * S, 20f * S);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            var labelColor = new Color(1f, 0.92f, 0.2f);
            HudTypography.Apply(tmp, TextAlignmentOptions.Center);
            tmp.color = labelColor;
            BindFont(tmp);
            return tmp;
        }

        void CreateNeedleAndSweetSpot(RectTransform pivotRt)
        {
            var needleGo = new GameObject("Needle", typeof(RectTransform), typeof(Image));
            var needleRt = needleGo.GetComponent<RectTransform>();
            needleRt.SetParent(pivotRt, false);
            needleRt.anchorMin = needleRt.anchorMax = new Vector2(0.5f, 0f);
            needleRt.pivot = new Vector2(0.5f, 0f);
            needleRt.anchoredPosition = Vector2.zero;
            needleRt.sizeDelta = new Vector2(5f * S, ArcRadius + 8f * S);
            var needleImg = needleGo.GetComponent<Image>();
            needleImg.sprite = ArcRingBuilder.WhiteSprite;
            needleImg.color = Color.white;
            needleImg.raycastTarget = false;
            needle = needleRt;

            var sweetGo = new GameObject("SweetSpot", typeof(RectTransform));
            sweetSpotRoot = sweetGo.GetComponent<RectTransform>();
            sweetSpotRoot.SetParent(pivotRt, false);
            sweetSpotRoot.anchorMin = sweetSpotRoot.anchorMax = new Vector2(0.5f, 0f);
            sweetSpotRoot.pivot = new Vector2(0.5f, 0f);
            sweetSpotRoot.anchoredPosition = Vector2.zero;
            sweetSpotRoot.sizeDelta = Vector2.zero;
            sweetGo.SetActive(false);
        }

        static void BindFont(TextMeshProUGUI tmp)
        {
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font != null)
                tmp.font = font;
        }

        public void SetIndicator(float normalized01)
        {
            if (needle == null)
                return;

            float angle = Mathf.Lerp(ArcStartDeg, ArcEndDeg, Mathf.Clamp01(normalized01));
            needle.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        public void SetSweetSpot(float center01, float width01)
        {
            if (sweetSpotRoot == null)
                return;

            center01 = Mathf.Clamp01(center01);
            width01 = Mathf.Clamp(width01, 0.03f, 0.2f);

            float halfAngle = width01 * 0.5f * Mathf.Abs(ArcEndDeg - ArcStartDeg);
            float centerAngle = Mathf.Lerp(ArcStartDeg, ArcEndDeg, center01);

            ArcRingBuilder.PopulateRing(
                sweetSpotRoot,
                centerAngle - halfAngle,
                centerAngle + halfAngle,
                ArcRadius + 1f,
                TrackThickness + 8f,
                TimingMeterLayout.SweetSpotColor,
                8);

            sweetSpotRoot.gameObject.SetActive(true);
            sweetSpotRoot.SetAsLastSibling();
            if (needle != null)
                needle.SetAsLastSibling();
        }

        public void HideSweetSpot()
        {
            if (sweetSpotRoot != null)
                sweetSpotRoot.gameObject.SetActive(false);
        }
    }
}
