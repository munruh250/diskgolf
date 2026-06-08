using UnityEngine.SceneManagement;

namespace DiskGolf.Core
{
    public static class SceneLoader
    {
        public static void Load(string sceneName) => SceneManager.LoadScene(sceneName);
    }
}
