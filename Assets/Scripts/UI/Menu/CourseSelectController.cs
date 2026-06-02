using DiskGolf.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI.Menu
{
    public sealed class CourseSelectController : MonoBehaviour
    {
        [SerializeField] Button backButton;

        [SerializeField] Button continueButton;

        void Awake()
        {
            if (backButton != null)
                backButton.onClick.AddListener(() => SceneLoader.Load(SceneFlow.MainMenu));

            if (continueButton != null)
                continueButton.onClick.AddListener(() => SceneLoader.Load(SceneFlow.Gameplay));
        }
    }
}
