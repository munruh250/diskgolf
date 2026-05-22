using DiskGolf.Core;
using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>Minimal 2D map: interpolate disc marker between tee and basket on the HUD.</summary>
    public class MinimapUI : MonoBehaviour
    {
        [SerializeField] HoleSetup hole;

        [SerializeField] Transform discTransform;

        [SerializeField] RectTransform discDot;

        [SerializeField] Vector2 teeMapAnchored = new(-72f, -72f);

        [SerializeField] Vector2 basketMapAnchored = new(72f, 72f);

        void LateUpdate()
        {
            if (hole == null || discTransform == null || discDot == null)
                return;

            Vector3 tee = hole.TeePosition;
            Vector3 basket = hole.BasketPosition;
            Vector3 disc = discTransform.position;

            var ba =
                new Vector2(basket.x - tee.x, basket.z - tee.z);

            float denom = ba.sqrMagnitude;

            Vector2 vd = new(disc.x - tee.x, disc.z - tee.z);

            float t = denom > 1e-6f
                ? Mathf.Clamp01(Vector2.Dot(vd, ba) / denom)
                : 0f;

            discDot.anchoredPosition = Vector2.Lerp(teeMapAnchored, basketMapAnchored, t);
        }
    }
}
