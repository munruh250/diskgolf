using System.IO;
using DiskGolf.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiskGolf.EditorTools
{
    /// <summary>
    /// Applies greybox migration when Unity recompiles — no batchmode rebuild required.
    /// </summary>
    [InitializeOnLoad]
    static class GreyboxEditorAutoSetup
    {
        const string VersionKey = "DiskGolf.GreyboxSetupVersion";

        const string PrototypeScenePath = ProjectArtPaths.Scenes.PrototypeFlat3;

        static GreyboxEditorAutoSetup() =>
            EditorApplication.delayCall += RunIfNeeded;

        static void RunIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (EditorPrefs.GetInt(VersionKey, 0) >= GreyboxScale.SetupVersion)
                return;

            MigratePrototypeScene();
            BasketSpriteImporter.ReimportBasketSprite();
            EditorPrefs.SetInt(VersionKey, GreyboxScale.SetupVersion);
            Debug.Log("[Disk Golf] Greybox auto setup migrated PrototypeFlat3 (version " +
                      GreyboxScale.SetupVersion + ").");
        }

        [MenuItem("Disk Golf/Apply Greybox Auto Setup Now")]
        public static void MigrateFromMenu()
        {
            MigratePrototypeScene();
            EditorPrefs.SetInt(VersionKey, GreyboxScale.SetupVersion);
            Debug.Log("[Disk Golf] Greybox auto setup applied.");
        }

        [MenuItem("Disk Golf/Clean Duplicate Vcams In Scene")]
        public static void CleanDuplicateVcamsInScene()
        {
            DiskGolf.Camera.CameraRig.RemoveDuplicateVcams();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Disk Golf] Removed duplicate virtual cameras from the active scene.");
        }

        [MenuItem("Disk Golf/Reset Greybox Migration Version")]
        public static void ResetMigrationVersion()
        {
            EditorPrefs.DeleteKey(VersionKey);
            Debug.Log("[Disk Golf] Greybox migration version reset — will re-run on next script reload.");
        }

        static void MigratePrototypeScene()
        {
            if (!File.Exists(PrototypeScenePath))
                return;

            var activePath = SceneManager.GetActiveScene().path;

            if (activePath == PrototypeScenePath)
            {
                ApplyToGameManager();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
                return;
            }

            var scene = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Additive);
            ApplyInScene(scene);
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.CloseScene(scene, true);
        }

        static void ApplyToGameManager()
        {
            var gm = GameObject.Find("GameManager");
            if (gm == null)
                return;

            EnsureAndApply(gm);
        }

        static void ApplyInScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "GameManager")
                    continue;

                EnsureAndApply(root);
                return;
            }
        }

        static void EnsureAndApply(GameObject gm)
        {
            var setup = gm.GetComponent<GreyboxAutoSetup>();
            if (setup == null)
                setup = gm.AddComponent<GreyboxAutoSetup>();

            setup.Apply();
        }
    }
}
