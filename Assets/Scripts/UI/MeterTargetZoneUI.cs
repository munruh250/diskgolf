using DiskGolf.Flight;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>Green target band overlaid on a timing meter slider.</summary>
    [RequireComponent(typeof(Slider))]
    public sealed class MeterTargetZoneUI : MonoBehaviour
    {
        static readonly Color ZoneColor = new(0.12f, 0.82f, 0.28f, 0.55f);

        [SerializeField] RectTransform zoneRect;

        [SerializeField] Image zoneImage;

        Slider _slider;

        void Awake()
        {
            _slider = GetComponent<Slider>();
            EnsureZoneVisual();
            Hide();
        }

        public void SetZone(float center01, float width01)
        {
            EnsureZoneVisual();

            if (zoneRect == null || _slider == null)
                return;

            center01 = Mathf.Clamp01(center01);
            width01 = Mathf.Clamp(width01, 0.04f, 0.45f);

            float left = Mathf.Clamp01(center01 - width01 * 0.5f);
            float right = Mathf.Clamp01(center01 + width01 * 0.5f);

            zoneRect.anchorMin = new Vector2(left, 0f);
            zoneRect.anchorMax = new Vector2(right, 1f);
            zoneRect.offsetMin = Vector2.zero;
            zoneRect.offsetMax = Vector2.zero;
            zoneRect.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (zoneRect != null)
                zoneRect.gameObject.SetActive(false);
        }

        void EnsureZoneVisual()
        {
            if (zoneRect != null)
                return;

            var go = new GameObject("TargetZone", typeof(RectTransform), typeof(Image));
            zoneRect = go.GetComponent<RectTransform>();
            zoneRect.SetParent(transform, false);
            zoneRect.SetAsFirstSibling();

            zoneImage = go.GetComponent<Image>();
            zoneImage.color = ZoneColor;
            zoneImage.raycastTarget = false;
        }
    }
}
