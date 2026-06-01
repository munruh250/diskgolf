using UnityEngine;

namespace DiskGolf.Disc
{
    [CreateAssetMenu(fileName = "DiscProfile", menuName = "Disk Golf/Disc Profile")]
    public class DiscProfile : ScriptableObject
    {
        public string displayName = "Midrange";
        public DiscCategory category = DiscCategory.Mid;
        [Tooltip("Optional HUD preview icon for the power-meter hub. Falls back to the default disc sprite.")]
        public Sprite previewSprite;
        [Tooltip("Material applied to the world disc mesh when this profile is selected.")]
        public Material discMaterial;
        [Range(1, 14)] public int speed = 5;
        [Range(1, 7)] public int glide = 4;
        [Range(-5, 1)] public int turn = -1;
        [Range(0, 5)] public int fade = 1;
        public float maxDistanceFt = 320f;
        public float meterSpeedMod = 1f;
    }
}
