using UnityEngine;

namespace DiskGolf.Disc
{
    [CreateAssetMenu(fileName = "DiscProfile", menuName = "Disk Golf/Disc Profile")]
    public class DiscProfile : ScriptableObject
    {
        public string displayName = "Buzzz";
        public DiscCategory category = DiscCategory.Mid;
        [Range(1, 14)] public int speed = 5;
        [Range(1, 7)] public int glide = 4;
        [Range(-5, 1)] public int turn = -1;
        [Range(0, 5)] public int fade = 1;
        public float maxDistanceFt = 250f;
        public float meterSpeedMod = 1f;
    }
}
