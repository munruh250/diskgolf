using UnityEngine;

namespace DiskGolf.Core
{
    /// <summary>Persists menu choices (difficulty) for the active session and future launches.</summary>
    public static class GameSessionSettings
    {
        const string DifficultyKey = "diskg.difficulty";

        static GameDifficulty _difficulty = GameDifficulty.Beginner;

        static bool _loaded;

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
            _loaded = true;
        }
    }
}
