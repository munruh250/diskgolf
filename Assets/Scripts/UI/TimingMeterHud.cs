using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>Hosts power and height arc meters under the gameplay HUD canvas.</summary>
    [DefaultExecutionOrder(-150)]
    public sealed class TimingMeterHud : MonoBehaviour
    {
        const string HudRootName = "GameplayHUD";

        const string MetersRootName = "TimingMeters";

        static TimingMeterHud _instance;

        PowerMeterVisual _power;

        HeightMeterVisual _height;

        public static PowerMeterVisual Power
        {
            get
            {
                Ensure();
                return _instance != null ? _instance._power : null;
            }
        }

        public static HeightMeterVisual Height
        {
            get
            {
                Ensure();
                return _instance != null ? _instance._height : null;
            }
        }

        public static TimingMeterHud Ensure()
        {
            var hudGo = GameObject.Find(HudRootName);
            if (hudGo == null)
                return null;

            var canvas = hudGo.GetComponent<RectTransform>();
            if (canvas == null)
                return null;

            FixCanvasRect(canvas);

            if (_instance == null)
            {
                _instance = hudGo.GetComponent<TimingMeterHud>();
                if (_instance == null)
                    _instance = hudGo.AddComponent<TimingMeterHud>();
                _instance.RefreshMeters(canvas);
            }
            else if (_instance._power == null || !_instance._power
                     || _instance._height == null || !_instance._height)
            {
                _instance.RefreshMeters(canvas);
            }

            return _instance;
        }

        void RefreshMeters(RectTransform canvas)
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

            UpgradeStaleMeter(root, PowerMeterVisual.VisualNameForFind, PowerMeterVisual.IsCurrentLayout);
            UpgradeStaleMeter(root, HeightMeterVisual.VisualNameForFind, HeightMeterVisual.IsCurrentLayout);

            _power = PowerMeterVisual.Ensure(root);
            _height = HeightMeterVisual.Ensure(root);

            _power?.EnsureBuilt();
            _height?.EnsureBuilt();

            _power?.SetChromeVisible(true);
            _height?.SetChromeVisible(true);

            NtmBottomBar.Ensure(canvas);
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        void Build(RectTransform canvas) => RefreshMeters(canvas);

        static void UpgradeStaleMeter(RectTransform root, string meterName, System.Func<Transform, bool> isCurrent)
        {
            var existing = root.Find(meterName);
            if (existing == null || isCurrent(existing))
                return;

            if (Application.isPlaying)
            {
                existing.GetComponent<PowerMeterVisual>()?.EnsureBuilt();
                existing.GetComponent<HeightMeterVisual>()?.EnsureBuilt();
                return;
            }

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
