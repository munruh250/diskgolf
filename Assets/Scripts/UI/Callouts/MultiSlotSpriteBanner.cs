using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI.Callouts
{
    public sealed class MultiSlotSpriteBanner : MonoBehaviour
    {
        readonly Dictionary<HoleCompleteScoreKind, Image> _slots = new();

        [SerializeField] float displaySeconds = 3.5f;

        public float DisplaySeconds => displaySeconds;

        public void BindSceneReferences()
        {
            _slots.Clear();
            RegisterSlot(HoleCompleteScoreKind.HoleInOne, "HoleInOne");
            RegisterSlot(HoleCompleteScoreKind.Eagle, "Eagle");
            RegisterSlot(HoleCompleteScoreKind.Birdie, "Birdie");
            RegisterSlot(HoleCompleteScoreKind.Par, "Par");
            RegisterSlot(HoleCompleteScoreKind.Bogey, "Bogey");
            RegisterSlot(HoleCompleteScoreKind.DoubleBogey, "DoubleBogey");
            RegisterSlot(HoleCompleteScoreKind.TripleBogey, "TripleBogey");
            RegisterSlot(HoleCompleteScoreKind.Awful, "Awful");
            HideAll();
        }

        void Awake()
        {
            BindSceneReferences();
            if (Application.isPlaying)
                Hide();
        }

        void RegisterSlot(HoleCompleteScoreKind kind, string childName)
        {
            var image = transform.Find(childName)?.GetComponent<Image>();
            if (image == null)
                return;

            ApplySlotSprite(image, kind);
            image.raycastTarget = false;
            _slots[kind] = image;
            image.gameObject.SetActive(false);
        }

        public void Show(int strokes, int par) =>
            ShowKind(ScoreBannerSprites.ResolveKind(strokes, par));

        public void ShowKind(HoleCompleteScoreKind kind)
        {
            HideAll();
            if (!_slots.TryGetValue(kind, out var slot) || slot == null)
                return;

            ApplySlotSprite(slot, kind);
            slot.gameObject.SetActive(true);
            CalloutLifecycle.BringToFront(transform);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            HideAll();
            gameObject.SetActive(false);
        }

        void HideAll()
        {
            foreach (var slot in _slots.Values)
            {
                if (slot != null)
                    slot.gameObject.SetActive(false);
            }
        }

        static void ApplySlotSprite(Image image, HoleCompleteScoreKind kind)
        {
            if (image.sprite == null)
                image.sprite = ScoreBannerSprites.LoadKind(kind);

            image.preserveAspect = true;
            image.color = Color.white;
            image.enabled = image.sprite != null;
        }

#if UNITY_INCLUDE_TESTS
        public void ConfigureSlotsForTests((HoleCompleteScoreKind kind, Image image)[] slots)
        {
            _slots.Clear();
            foreach (var entry in slots)
                _slots[entry.kind] = entry.image;
        }
#endif
    }
}
