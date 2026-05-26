using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>Repositions gameplay HUD to match Neo Turf Masters layout.</summary>
    public static class NtmHudLayout
    {
        const string HudRootName = "GameplayHUD";

        public static void Apply()
        {
            var hud = GameObject.Find(HudRootName);
            if (hud == null)
                return;

            var canvas = hud.GetComponent<RectTransform>();
            if (canvas == null)
                return;

            ApplyMinimap();
            StyleCanvasScaler(hud);

            PinTopLeft(FindTmp(canvas, "REST"), new Vector2(36f, -36f), 44f);
            PinTopLeft(EnsureDiscHeightLabel(canvas), new Vector2(36f, -96f), 32f);
            PinTopRight(FindTmp(canvas, "WIND"), new Vector2(-36f, -132f), 30f);

            PinBottomLeft(FindTmp(canvas, "FLAT"), new Vector2(36f, 88f), 28f);
            PinBottomLeft(FindTmp(canvas, "Disc"), new Vector2(36f, 48f), 26f);

            var powerSlider = FindSlider(canvas, "Slider", 0);
            var heightSlider = FindSlider(canvas, "Slider", 1);

            PinBottomRight(powerSlider, new Vector2(-348f, 52f), new Vector2(300f, 48f));
            PinBottomRight(heightSlider, new Vector2(-36f, 52f), new Vector2(300f, 48f));

            var powerLabel = FindTmpContains(canvas, "POWER");
            if (powerLabel != null)
                PinBottomRight(powerLabel, new Vector2(-348f, 112f), 24f);

            var heightLabel = FindTmpContains(canvas, "HEIGHT");
            if (heightLabel != null)
                PinBottomRight(heightLabel, new Vector2(-36f, 112f), 24f);

            var nice = FindUnityText(canvas, "NICE");
            if (nice != null)
                PinBottomRight(nice, new Vector2(-36f, 16f), 22f);

            StyleSlider(powerSlider, new Color(0.08f, 0.72f, 0.68f));
            StyleSlider(heightSlider, new Color(0.95f, 0.72f, 0.18f));
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

            var rest = FindTmp(canvas, "REST");
            if (rest != null)
                tmp.font = rest.font;

            return tmp;
        }

        static void ApplyMinimap()
        {
            var host = GameObject.Find("MinimapHost")?.GetComponent<RectTransform>();
            if (host == null)
                return;

            host.anchorMin = host.anchorMax = new Vector2(1f, 1f);
            host.pivot = new Vector2(1f, 1f);
            host.anchoredPosition = new Vector2(-20f, -20f);
            host.sizeDelta = new Vector2(248f, 392f);
        }

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

        static Slider FindSlider(RectTransform root, string name, int index = 0)
        {
            int i = 0;
            foreach (var s in root.GetComponentsInChildren<Slider>(true))
            {
                if (s.name != name && name != null)
                    continue;

                if (i++ == index)
                    return s;
            }

            return null;
        }

        static void PinTopLeft(TextMeshProUGUI tmp, Vector2 offset, float fontSize)
        {
            if (tmp == null)
                return;

            var rt = tmp.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(420f, 64f);
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.TopLeft;
        }

        static void PinTopRight(TextMeshProUGUI tmp, Vector2 offset, float fontSize)
        {
            if (tmp == null)
                return;

            var rt = tmp.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(260f, 48f);
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.TopRight;
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

        static void PinBottomRight(Slider slider, Vector2 offset, Vector2 size)
        {
            if (slider == null)
                return;

            var rt = slider.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
        }

        static void StyleSlider(Slider slider, Color fillColor)
        {
            if (slider == null)
                return;

            var fill = slider.fillRect?.GetComponent<Image>();
            if (fill != null)
                fill.color = fillColor;

            var bg = slider.transform.Find("Background")?.GetComponent<Image>();
            if (bg != null)
                bg.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);
        }
    }
}
