using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>Builds visible uGUI arc rings from rotated Image slices.</summary>
    public static class NtmArcRingBuilder
    {
        static Sprite _whiteSprite;

        public static RectTransform CreateRing(
            RectTransform parent,
            string name,
            float startDeg,
            float endDeg,
            float radius,
            float thickness,
            Color color,
            int segmentCount = 32)
        {
            var container = CreateContainer(parent, name);
            PopulateRing(container, startDeg, endDeg, radius, thickness, color, segmentCount);
            return container;
        }

        public static RectTransform CreateGradientRing(
            RectTransform parent,
            string name,
            float startDeg,
            float endDeg,
            float radius,
            float thickness,
            Color colorAtStart,
            Color colorAtMid,
            Color colorAtEnd,
            float midT = 0.5f,
            int segmentCount = 36)
        {
            var container = CreateContainer(parent, name);
            PopulateGradientRing(
                container, startDeg, endDeg, radius, thickness,
                colorAtStart, colorAtMid, colorAtEnd, midT, segmentCount);
            return container;
        }

        static RectTransform CreateContainer(RectTransform parent, string name)
        {
            var containerGo = new GameObject(name, typeof(RectTransform));
            var container = containerGo.GetComponent<RectTransform>();
            container.SetParent(parent, false);
            container.anchorMin = container.anchorMax = new Vector2(0.5f, 0f);
            container.pivot = new Vector2(0.5f, 0f);
            container.anchoredPosition = Vector2.zero;
            container.sizeDelta = Vector2.zero;
            return container;
        }

        public static void PopulateRing(
            RectTransform container,
            float startDeg,
            float endDeg,
            float radius,
            float thickness,
            Color color,
            int segmentCount = 32)
        {
            ClearChildren(container);

            int steps = Mathf.Max(3, segmentCount);
            var sprite = GetWhiteSprite();

            for (int i = 0; i < steps; i++)
            {
                float t0 = i / (float)steps;
                float t1 = (i + 1) / (float)steps;
                AddSegment(container, sprite, startDeg, endDeg, radius, thickness, color, t0, t1);
            }
        }

        public static void PopulateGradientRing(
            RectTransform container,
            float startDeg,
            float endDeg,
            float radius,
            float thickness,
            Color colorAtStart,
            Color colorAtMid,
            Color colorAtEnd,
            float midT = 0.5f,
            int segmentCount = 36)
        {
            ClearChildren(container);

            int steps = Mathf.Max(6, segmentCount);
            var sprite = GetWhiteSprite();

            for (int i = 0; i < steps; i++)
            {
                float t0 = i / (float)steps;
                float t1 = (i + 1) / (float)steps;
                float tMid = (t0 + t1) * 0.5f;
                Color color = SampleGradient(colorAtStart, colorAtMid, colorAtEnd, midT, tMid);
                AddSegment(container, sprite, startDeg, endDeg, radius, thickness, color, t0, t1);
            }
        }

        static Color SampleGradient(Color start, Color mid, Color end, float midT, float t)
        {
            if (t <= midT)
                return Color.Lerp(start, mid, t / Mathf.Max(midT, 1e-4f));

            return Color.Lerp(mid, end, (t - midT) / Mathf.Max(1f - midT, 1e-4f));
        }

        static void AddSegment(
            RectTransform container,
            Sprite sprite,
            float startDeg,
            float endDeg,
            float radius,
            float thickness,
            Color color,
            float t0,
            float t1)
        {
            float midDeg = Mathf.Lerp(startDeg, endDeg, (t0 + t1) * 0.5f);
            float midRad = midDeg * Mathf.Deg2Rad;
            float stepT = Mathf.Max(1e-4f, t1 - t0);
            float segWidth = Mathf.Max(6f, radius * Mathf.Abs(endDeg - startDeg) * Mathf.Deg2Rad * stepT * 1.35f);

            var segGo = new GameObject("Seg", typeof(RectTransform), typeof(Image));
            var seg = segGo.GetComponent<RectTransform>();
            seg.SetParent(container, false);
            seg.anchorMin = seg.anchorMax = new Vector2(0.5f, 0f);
            seg.pivot = new Vector2(0.5f, 0f);
            seg.sizeDelta = new Vector2(segWidth, thickness);
            seg.anchoredPosition = new Vector2(Mathf.Cos(midRad) * radius, Mathf.Sin(midRad) * radius);
            seg.localRotation = Quaternion.Euler(0f, 0f, midDeg - 90f);

            var img = segGo.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;
        }

        public static Vector2 PointOnArc(float degrees, float radius) =>
            new(Mathf.Cos(degrees * Mathf.Deg2Rad) * radius, Mathf.Sin(degrees * Mathf.Deg2Rad) * radius);

        public static void ClearChildren(RectTransform parent)
        {
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        public static Sprite WhiteSprite => GetWhiteSprite();

        static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null)
                return _whiteSprite;

            var tex = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            return _whiteSprite;
        }
    }
}
