using UnityEngine;

namespace DiskGolf.Disc
{
    [CreateAssetMenu(fileName = "PlayerCharacterRoster", menuName = "Disk Golf/Player Character Roster")]
    public sealed class PlayerCharacterRoster : ScriptableObject
    {
        public PlayerCharacterProfile[] characters;

        public int Count => characters != null ? characters.Length : 0;

        public PlayerCharacterProfile Get(int index)
        {
            if (characters == null || characters.Length == 0)
                return null;

            index = Mathf.Clamp(index, 0, characters.Length - 1);
            return characters[index];
        }
    }
}
