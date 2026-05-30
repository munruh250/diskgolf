using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>Hosts NTM arc meters under the gameplay HUD canvas.</summary>
    [DefaultExecutionOrder(-150)]
    public sealed class TimingMeterHud : MonoBehaviour
    {
        const string HudRootName = "GameplayHUD";

        const string MetersRootName = "TimingMeters";

        static TimingMeterHud _instance;

        NtmPowerMeterVisual _power;

        NtmHeightMeterVisual _height;

        public static NtmPowerMeterVisual Power
        {
            get
            {
                Ensure();
                return _instance != null ? _instance._power : null;
            }
        }

        public static NtmHeightMeterVisual Height
        {
            get
            {
                Ensure();
                return _instance != null ? _instance._height : null;
            }
        }

        public static TimingMeterHud Ensure()
        {
            if (_instance != null)
                return _instance;

            var hudGo = GameObject.Find(HudRootName);
            if (hudGo == null)
                return null;

            var canvas = hudGo.GetComponent<RectTransform>();
            if (canvas == null)
                return null;

            FixCanvasRect(canvas);

            _instance = hudGo.GetComponent<TimingMeterHud>();
            if (_instance == null)
                _instance = hudGo.AddComponent<TimingMeterHud>();

            _instance.Build(canvas);
            return _instance;
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        void Build(RectTransform canvas)
        {
            var root = canvas.Find(MetersRootName) as RectTransform;
            if (root == null)
            {
                var go = new GameObject(MetersRootName, typeof(RectTransform));
                root = go.GetComponent<RectTransform>();
                root.SetParent(canvas, false);
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
                root.offsetMin = Vector2.zero;
                root.offsetMax = Vector2.zero;
            }

            root.SetAsLastSibling();

            DestroyStaleMeter(root, NtmPowerMeterVisual.VisualNameForFind,
                t =>
                {
                    if (t.Find("Pivot/ArcHub/TrackColorV4") == null)
                        return false;

                    var rt = t as RectTransform;
                    return rt != null
                           && rt.sizeDelta.x >= NtmTimingMeterLayout.PowerWidth - 1f
                           && Vector2.Distance(rt.anchoredPosition, NtmTimingMeterLayout.PowerAnchorPos) < 1f;
                });

            DestroyStaleMeter(root, NtmHeightMeterVisual.VisualNameForFind,
                t =>
                {
                    var rt = t as RectTransform;
                    return rt != null
                           && rt.sizeDelta.y >= NtmTimingMeterLayout.HeightTotal - 1f
                           && rt.sizeDelta.x >= NtmTimingMeterLayout.HeightWidth - 1f;
                });

            _power = NtmPowerMeterVisual.Ensure(root);
            _height = NtmHeightMeterVisual.Ensure(root);

            _power?.EnsureBuilt();
            _height?.EnsureBuilt();

            _power.SetChromeVisible(true);
            _height.SetChromeVisible(true);
        }

        static void DestroyStaleMeter(RectTransform root, string meterName, System.Func<Transform, bool> isCurrent)
        {
            var existing = root.Find(meterName);
            if (existing == null || isCurrent(existing))
                return;

            if (Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyImmediate(existing.gameObject);
        }

        static void FixCanvasRect(RectTransform canvas)
        {
            if (canvas.localScale.sqrMagnitude < 0.01f)
                canvas.localScale = Vector3.one;

            canvas.anchorMin = Vector2.zero;
            canvas.anchorMax = Vector2.one;
            canvas.pivot = new Vector2(0.5f, 0.5f);
            canvas.anchoredPosition = Vector2.zero;
            canvas.sizeDelta = Vector2.zero;
        }
    }
}
