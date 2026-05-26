using UnityEngine;

namespace DiskGolf.Disc
{
    public class DiscBag : MonoBehaviour
    {
        [SerializeField] DiscProfile[] discs = new DiscProfile[4];
        int _index;

        public DiscProfile Active => discs[_index];
        public DiscProfile[] All => discs;

        public void SelectIndex(int index)
        {
            _index = Mathf.Clamp(index, 0, discs.Length - 1);
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
    }
}
