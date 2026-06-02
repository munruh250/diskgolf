using DiskGolf.Core;
using UnityEngine;

namespace DiskGolf.UI.Menu
{
    public sealed class IntroSceneController : MonoBehaviour
    {
        void Update()
        {
            if (AnyContinueInput())
                SceneLoader.Load(SceneFlow.MainMenu);
        }

        static bool AnyContinueInput()
        {
            if (UnityEngine.Input.anyKeyDown)
                return true;

            return UnityEngine.Input.GetMouseButtonDown(0)
                || UnityEngine.Input.GetMouseButtonDown(1)
                || UnityEngine.Input.GetMouseButtonDown(2);
        }
    }
}
