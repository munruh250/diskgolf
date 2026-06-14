using System.Collections;
using DiskGolf.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI.Callouts
{
    public sealed class SpriteCalloutBanner : MonoBehaviour
    {
        [SerializeField] Image bannerImage;

        [SerializeField] float displaySeconds = 2.25f;

        [SerializeField] bool useScoreBannerLayout = true;

        Coroutine _hideRoutine;

        public float DisplaySeconds => displaySeconds;

        void Awake() => ResolveImage();

        public void ResolveImage()
        {
            bannerImage ??= GetComponent<Image>();
            if (bannerImage != null)
                bannerImage.raycastTarget = false;
        }

        public void Show(Sprite sprite)
        {
            ResolveImage();
            if (bannerImage == null || sprite == null)
                return;

            ApplyLayout();
            bannerImage.sprite = sprite;
            bannerImage.preserveAspect = true;
            bannerImage.color = Color.white;
            bannerImage.enabled = true;
            CalloutLifecycle.BringToFront(transform);
            gameObject.SetActive(true);
        }

        public void ShowBriefly(Sprite sprite)
        {
            _hideRoutine = CalloutLifecycle.ShowBriefly(
                this,
                _hideRoutine,
                displaySeconds,
                () => Show(sprite),
                Hide);
        }

        public void Hide()
        {
            CalloutLifecycle.Cancel(ref _hideRoutine, this);
            gameObject.SetActive(false);
        }

        void ApplyLayout()
        {
            var rt = transform as RectTransform;
            if (rt == null)
                return;

            if (useScoreBannerLayout)
                PostThrowCalloutLayout.ApplyScoreBannerRect(rt);
        }
    }
}
