using DiskGolf.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI.Menu
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] Button characterSelectButton;

        [SerializeField] Button courseSelectButton;

        [SerializeField] Button settingsButton;

        void Awake()
        {
            if (characterSelectButton != null)
                characterSelectButton.onClick.AddListener(() => SceneLoader.Load(SceneFlow.CharacterSelect));

            if (courseSelectButton != null)
                courseSelectButton.onClick.AddListener(() => SceneLoader.Load(SceneFlow.CourseSelect));

            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => SceneLoader.Load(SceneFlow.Settings));
        }
    }
}
