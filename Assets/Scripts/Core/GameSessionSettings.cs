using DiskGolf.Disc;
using UnityEngine;

namespace DiskGolf.Core
{
    /// <summary>Persists menu choices (difficulty) for the active session and future launches.</summary>
    public static class GameSessionSettings
    {
        const string DifficultyKey = "diskg.difficulty";

        const string CharacterIndexKey = "diskg.characterIndex";

        static GameDifficulty _difficulty = GameDifficulty.Beginner;

        static int _characterIndex;

        static PlayerCharacterRoster _roster;

        static bool _loaded;

        public static PlayerCharacterRoster Roster
        {
            get
            {
                if (_roster != null)
                    return _roster;

                _roster = Resources.Load<PlayerCharacterRoster>("PlayerCharacterRoster");
                return _roster;
            }
            set => _roster = value;
        }

        public static PlayerCharacterProfile ActiveCharacter
        {
            get
            {
                var roster = Roster;
                return roster != null ? roster.Get(SelectedCharacterIndex) : null;
            }
        }

        public static int SelectedCharacterIndex
        {
            get
            {
                EnsureLoaded();
                return _characterIndex;
            }
            set
            {
                EnsureLoaded();
                int max = Roster != null ? Mathf.Max(0, Roster.Count - 1) : 0;
                _characterIndex = Mathf.Clamp(value, 0, max);
                PlayerPrefs.SetInt(CharacterIndexKey, _characterIndex);
                PlayerPrefs.Save();
            }
        }

        public static GameDifficulty Difficulty
        {
            get
            {
                EnsureLoaded();
                return _difficulty;
            }
            set
            {
                EnsureLoaded();
                _difficulty = value;
                PlayerPrefs.SetInt(DifficultyKey, (int)value);
                PlayerPrefs.Save();
            }
        }

        public static bool ShowMeterSweetSpots => Difficulty == GameDifficulty.Beginner;

        static void EnsureLoaded()
        {
            if (_loaded)
                return;

            _difficulty = (GameDifficulty)PlayerPrefs.GetInt(DifficultyKey, (int)GameDifficulty.Beginner);
            _characterIndex = PlayerPrefs.GetInt(CharacterIndexKey, 0);
            _loaded = true;
        }
    }
}
