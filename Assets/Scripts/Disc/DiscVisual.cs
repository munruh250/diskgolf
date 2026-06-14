using DiskGolf.Gameplay;
using UnityEngine;

namespace DiskGolf.Disc
{
    /// <summary>Swaps the world disc material when the active bag disc changes.</summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class DiscVisual : MonoBehaviour
    {
        [SerializeField] DiscBag bag;

        [SerializeField] Renderer targetRenderer;

        [SerializeField] Material fallbackMaterial;

        DiscProfile _lastApplied;

        void Awake()
        {
            targetRenderer ??= GetComponent<Renderer>();
            bag ??= FindFirstObjectByType<DiscBag>();
            fallbackMaterial ??= RuntimeArt.LoadDiscMaterial();
        }

        void OnEnable()
        {
            if (bag != null)
                bag.SelectionChanged += OnSelectionChanged;

            Refresh();
        }

        void OnDisable()
        {
            if (bag != null)
                bag.SelectionChanged -= OnSelectionChanged;
        }

        void OnSelectionChanged(DiscProfile _) => Refresh();

        public void Refresh() => Apply(bag != null ? bag.Active : null);

        public void Apply(DiscProfile profile)
        {
            _lastApplied = profile;
            DiscProfileAppearance.Apply(targetRenderer, profile, fallbackMaterial);
        }
    }
}
