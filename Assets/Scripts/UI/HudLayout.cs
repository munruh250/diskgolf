using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>Repositions gameplay HUD widgets to the default layout.</summary>
    public static class HudLayout
    {
        const string HudRootName = "GameplayHUD";

        public static float MinimapWidth => Read(s => s.minimapWidth, 248f);

        public static float MinimapHeight => Read(s => s.minimapHeight, 392f);

        public static float RightInset => Read(s => s.rightInset, 20f);

        public static float TopInset => HudTypography.TopInset;

        public static float StackGap => Read(s => s.stackGap, 8f);

        public static float HoleInfoHeight =>
            HudLayoutSettings.Active != null
                ? HudLayoutSettings.Active.HoleInfoHeight
                : HudTypography.RowHeight * 3f;

        public static void Apply()
        {
            if (HudLayoutSettings.ShouldPreserveLayout() && !HudLayoutSettings.ShouldApplyLayoutOnPlay())
            {
                ApplyCleanupOnly();
                return;
            }

            ApplyPositions();
        }

        /// <summary>Hides legacy widgets and ensures HUD children exist without moving them.</summary>
        public static void ApplyCleanupOnly()
        {
            var hud = GameObject.Find(HudRootName);
            if (hud == null)
                return;

            var canvas = hud.GetComponent<RectTransform>();
            if (canvas == null)
                return;

            HudLayoutSettings.EnsureOnHudRoot();
            StyleCanvasScaler(hud);
            HideLegacyDiscHeightLabel(canvas);
            HideLegacyLabels(canvas);
            HideLegacySliders(canvas);

            RestDriveReadout.Ensure(canvas);
            HoleInfoPanel.Ensure(canvas);
            WindWidget.Ensure(canvas);
            TimingMeterHud.Ensure();

            HideLegacyPowerHeightLabels(canvas);
        }

        /// <summary>Re-applies the coded default HUD layout (Scene-view positions will be overwritten).</summary>
        public static void ApplyPositions()
        {
            var hud = GameObject.Find(HudRootName);
            if (hud == null)
                return;

            var canvas = hud.GetComponent<RectTransform>();
            if (canvas == null)
                return;

            var settings = HudLayoutSettings.EnsureOnHudRoot();
            StyleCanvasScaler(hud);
            HideLegacyDiscHeightLabel(canvas);
            HideLegacyLabels(canvas);
            HideLegacySliders(canvas);
            TimingMeterHud.Ensure();

            var flatOffset = settings != null ? settings.flatLabelOffset : new Vector2(36f, 88f);
            var discOffset = settings != null ? settings.discLabelOffset : new Vector2(36f, 48f);
            PinBottomLeft(FindTmp(canvas, "FLAT"), flatOffset, HudTypography.FontSize);
            PinBottomLeft(FindTmp(canvas, "Disc"), discOffset, HudTypography.FontSize - 2f);

            HideLegacyPowerHeightLabels(canvas);
            ApplyRightStack(canvas);
        }

        static void HideLegacyPowerHeightLabels(RectTransform canvas)
        {
            var powerLabel = FindTmpContains(canvas, "POWER");
            if (powerLabel != null)
            {
                PinBottomRight(powerLabel, new Vector2(-170f, 168f), 22f);
                powerLabel.gameObject.SetActive(false);
            }

            var heightLabel = FindTmpContains(canvas, "HEIGHT");
            if (heightLabel != null)
            {
                PinBottomRight(heightLabel, new Vector2(-28f, 188f), 22f);
                heightLabel.gameObject.SetActive(false);
            }

            var nice = FindUnityText(canvas, "NICE");
            if (nice != null)
                nice.gameObject.SetActive(false);
        }

        static void ApplyRightStack(RectTransform canvas)
        {
            var hudRoot = canvas;
            RestDriveReadout.Ensure(hudRoot)?.ApplyLayout();

            float minimapTop = TopInset + HoleInfoHeight + StackGap;
            var holeInfo = HoleInfoPanel.Ensure(hudRoot);
            holeInfo?.ApplyLayout();

            var host = GameObject.Find("MinimapHost")?.GetComponent<RectTransform>();
            if (host != null)
            {
                host.anchorMin = host.anchorMax = new Vector2(1f, 1f);
                host.pivot = new Vector2(1f, 1f);
                host.anchoredPosition = new Vector2(-RightInset, -minimapTop);
                host.sizeDelta = new Vector2(MinimapWidth, MinimapHeight);
            }

            var wind = WindWidget.Ensure(hudRoot);
            if (wind != null)
            {
                wind.ApplyLayout();
                wind.transform.SetAsLastSibling();
            }

            if (holeInfo != null && host != null)
                holeInfo.transform.SetSiblingIndex(host.GetSiblingIndex());
        }

        static float Read(System.Func<HudLayoutSettings, float> pick, float fallback) =>
            HudLayoutSettings.Active != null ? pick(HudLayoutSettings.Active) : fallback;

        static void HideLegacyDiscHeightLabel(RectTransform canvas)
        {
            var label = FindTmp(canvas, "DISC HEIGHT") ?? FindTmp(canvas, "DiscHeight");
            if (label != null)
                label.gameObject.SetActive(false);
        }

        static void HideLegacyLabels(RectTransform canvas)
        {
            var bucket = FindBucketDistanceLabel(canvas);
            if (bucket != null)
                bucket.gameObject.SetActive(false);

            var score = FindTmp(canvas, "PAR");
            if (score != null)
                score.gameObject.SetActive(false);

            var wind = FindTmp(canvas, "WIND");
            if (wind != null)
                wind.gameObject.SetActive(false);
        }

        static void HideLegacySliders(RectTransform canvas)
        {
            foreach (var slider in canvas.GetComponentsInChildren<Slider>(true))
                slider.gameObject.SetActive(false);
        }

        static void StyleCanvasScaler(GameObject hud)
        {
            var scaler = hud.GetComponent<CanvasScaler>();
            if (scaler == null)
                return;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        public static TextMeshProUGUI EnsureDiscHeightLabel()
        {
            var hud = GameObject.Find(HudRootName)?.GetComponent<RectTransform>();
            return hud != null ? EnsureDiscHeightLabel(hud) : null;
        }

        public static TextMeshProUGUI EnsureScoreLabel()
        {
            var hud = GameObject.Find(HudRootName)?.GetComponent<RectTransform>();
            return hud != null ? EnsureScoreLabel(hud) : null;
        }

        static TextMeshProUGUI EnsureScoreLabel(RectTransform canvas)
        {
            var existing = FindTmp(canvas, "PAR");
            if (existing != null)
                return existing;

            var go = new GameObject("Score", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvas, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "PAR 3  ·  THROW 0";
            tmp.color = new Color(0.95f, 0.95f, 0.95f);
            tmp.raycastTarget = false;

            var rest = FindBucketDistanceLabel(canvas);
            if (rest != null)
                tmp.font = rest.font;

            return tmp;
        }

        static TextMeshProUGUI EnsureDiscHeightLabel(RectTransform canvas)
        {
            var existing = FindTmp(canvas, "DISC HEIGHT");
            if (existing != null)
                return existing;

            var go = new GameObject("DiscHeight", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvas, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "DISC HEIGHT --- ft";
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            var rest = FindBucketDistanceLabel(canvas);
            if (rest != null)
                tmp.font = rest.font;

            return tmp;
        }

        static TextMeshProUGUI FindBucketDistanceLabel(RectTransform canvas) =>
            FindTmp(canvas, "Bucket Distance") ?? FindTmp(canvas, "REST");

        static TextMeshProUGUI FindTmp(RectTransform root, string exact)
        {
            foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.text.StartsWith(exact) || t.name.Contains(exact))
                    return t;
            }

            return null;
        }

        static TextMeshProUGUI FindTmpContains(RectTransform root, string token)
        {
            foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (t.text.Contains(token))
                    return t;
            }

            return null;
        }

        static Text FindUnityText(RectTransform root, string exact)
        {
            foreach (var t in root.GetComponentsInChildren<Text>(true))
            {
                if (t.text == exact)
                    return t;
            }

            return null;
        }

        static void PinBottomLeft(TextMeshProUGUI tmp, Vector2 offset, float fontSize)
        {
            if (tmp == null)
                return;

            var rt = tmp.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(420f, 40f);
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.BottomLeft;
        }

        static void PinBottomRight(TextMeshProUGUI tmp, Vector2 offset, float fontSize)
        {
            if (tmp == null)
                return;

            var rt = tmp.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(320f, 36f);
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.BottomRight;
        }

        static void PinBottomRight(Text tmp, Vector2 offset, float fontSize)
        {
            if (tmp == null)
                return;

            var rt = tmp.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(320f, 32f);
            tmp.fontSize = (int)fontSize;
            tmp.fontStyle = FontStyle.Bold;
            tmp.alignment = TextAnchor.LowerRight;
        }
    }
}
