using System;
using UnityEngine;

namespace DiskGolf.Disc
{
    public class DiscBag : MonoBehaviour
    {
        [SerializeField] DiscProfile[] discs = new DiscProfile[4];
        int _index;

        public event Action<DiscProfile> SelectionChanged;

        public DiscProfile Active => discs[_index];
        public DiscProfile[] All => discs;

        public void SelectIndex(int index)
        {
            int next = Mathf.Clamp(index, 0, discs.Length - 1);
            if (next == _index)
                return;

            _index = next;
            SelectionChanged?.Invoke(Active);
        }

        public void SelectCategory(DiscCategory category)
        {
            for (int i = 0; i < discs.Length; i++)
            {
                if (discs[i] != null && discs[i].category == category)
                {
                    SelectIndex(i);
                    return;
                }
            }
        }

        public void SelectForDistance(float distanceFt) =>
            SelectCategory(DiscSelection.RecommendCategory(distanceFt));

        public void CycleNext() => SelectIndex((_index + 1) % discs.Length);

        public void CyclePrev() => SelectIndex((_index - 1 + discs.Length) % discs.Length);

        public void CycleCategoryNext()
        {
            var current = Active != null ? Active.category : DiscCategory.Distance;
            var next = current switch
            {
                DiscCategory.Putter => DiscCategory.Mid,
                DiscCategory.Mid => DiscCategory.Fairway,
                DiscCategory.Fairway => DiscCategory.Distance,
                _ => DiscCategory.Putter,
            };

            SelectCategory(next);
        }
    }
}
