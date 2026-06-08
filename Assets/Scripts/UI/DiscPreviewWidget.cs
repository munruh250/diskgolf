using DiskGolf.Disc;
using DiskGolf.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>Active disc icon preview (bottom bar and similar HUD slots).</summary>
    public sealed class DiscPreviewWidget : MonoBehaviour
    {
        [SerializeField] Image display;

        [SerializeField] Sprite defaultSprite;

        [SerializeField] DiscBag bag;

        void OnEnable()
        {
            BindReferences();
            if (bag != null)
                bag.SelectionChanged += OnDiscChanged;
            Refresh();
        }

        void OnDisable()
        {
            if (bag != null)
                bag.SelectionChanged -= OnDiscChanged;
        }

        void OnDiscChanged(DiscProfile _) => Refresh();

        public void BindReferences()
        {
            display ??= GetComponent<Image>();
            bag ??= FindObjectOfType<DiscBag>();
            defaultSprite ??= RuntimeArt.LoadDiscPreviewSprite();
            Refresh();
        }

        public void Refresh()
        {
            if (display == null)
                return;

            var profile = bag != null ? bag.Active : null;
            display.sprite = ResolveSprite(profile);
            display.color = ResolveTint(profile);
            display.enabled = display.sprite != null;
        }

        Sprite ResolveSprite(DiscProfile profile)
        {
            if (profile != null && profile.previewSprite != null)
                return profile.previewSprite;

            return defaultSprite ?? RuntimeArt.LoadDiscPreviewSprite();
        }

        static Color ResolveTint(DiscProfile profile)
        {
            if (profile != null && profile.discMaterial != null)
                return profile.discMaterial.color;

            return Color.white;
        }
    }
}
