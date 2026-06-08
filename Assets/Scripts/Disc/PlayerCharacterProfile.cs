using UnityEngine;

namespace DiskGolf.Disc
{
    [CreateAssetMenu(fileName = "PlayerCharacter", menuName = "Disk Golf/Player Character")]
    public sealed class PlayerCharacterProfile : ScriptableObject
    {
        public string firstName = "Jack";

        public string nickname = "The Young Hero";

        public string lastName = "Unruh";

        [Tooltip("Short label used elsewhere in HUD (defaults to nickname).")]
        public string displayName = "The Young Hero";

        [Tooltip("Short region label shown under the portrait (e.g. USA).")]
        public string regionLabel = "USA";

        [Range(60, 100)] public int power = 80;

        [Range(60, 100)] public int accuracy = 80;

        [Range(60, 100)] public int clutch = 80;

        [Tooltip("Placeholder portrait tint until sprite art is added.")]
        public Color portraitColor = new(0.35f, 0.55f, 0.85f, 1f);

        [Tooltip("Character select portrait and other UI previews. Falls back to portraitColor when unset.")]
        public Sprite previewSprite;

        public string NicknameLine => string.IsNullOrWhiteSpace(nickname) ? string.Empty : $"'{nickname}'";
    }
}
