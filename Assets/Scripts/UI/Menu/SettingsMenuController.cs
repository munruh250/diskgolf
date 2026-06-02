using DiskGolf.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI.Menu
{
    public sealed class SettingsMenuController : MonoBehaviour
    {
        [SerializeField] Toggle beginnerToggle;

        [SerializeField] Toggle advancedToggle;

        [SerializeField] Button backButton;

        void Awake()
        {
            if (backButton != null)
                backButton.onClick.AddListener(() => SceneLoader.Load(SceneFlow.MainMenu));

            if (beginnerToggle != null)
                beginnerToggle.onValueChanged.AddListener(OnBeginnerChanged);

            if (advancedToggle != null)
                advancedToggle.onValueChanged.AddListener(OnAdvancedChanged);

            RefreshToggles();
        }

        void RefreshToggles()
        {
            bool beginner = GameSessionSettings.Difficulty == GameDifficulty.Beginner;

            if (beginnerToggle != null)
                beginnerToggle.SetIsOnWithoutNotify(beginner);

            if (advancedToggle != null)
                advancedToggle.SetIsOnWithoutNotify(!beginner);
        }

        void OnBeginnerChanged(bool on)
        {
            if (!on)
                return;

            GameSessionSettings.Difficulty = GameDifficulty.Beginner;
            RefreshToggles();
        }

        void OnAdvancedChanged(bool on)
        {
            if (!on)
                return;

            GameSessionSettings.Difficulty = GameDifficulty.Advanced;
            RefreshToggles();
        }
    }
}
