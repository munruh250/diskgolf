#if UNITY_EDITOR
using DiskGolf.Gameplay;
using DiskGolf.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiskGolf.EditorTools
{
    /// <summary>Bulk-apply the project HUD font to existing TextMeshPro components.</summary>
    public static class HudFontTools
    {
        const string MenuRoot = "Disk Golf/Typography/";

        [MenuItem(MenuRoot + "Apply Pixel Emulator Font (Open Scene)")]
        public static void ApplyToOpenScene()
        {
            var font = LoadPixelFont();
            if (font == null)
                return;

            int count = ApplyToHierarchy(null, font, includeInactive: true);
            if (count > 0)
            {
                var scene = SceneManager.GetActiveScene();
                if (scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(scene);
            }

            Debug.Log($"[Disk Golf] Applied Pixel Emulator SDF to {count} TextMeshPro components in the open scene.");
        }

        [MenuItem(MenuRoot + "Apply Pixel Emulator Font (All Prefabs)")]
        public static void ApplyToAllPrefabs()
        {
            var font = LoadPixelFont();
            if (font == null)
                return;

            int prefabCount = 0;
            int componentCount = 0;

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                    continue;

                var root = PrefabUtility.LoadPrefabContents(path);
                int changed = ApplyToHierarchy(root.transform, font, includeInactive: true);
                if (changed > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    prefabCount++;
                    componentCount += changed;
                }

                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[Disk Golf] Applied Pixel Emulator SDF to {componentCount} TextMeshPro components across {prefabCount} prefabs.");
        }

        [MenuItem(MenuRoot + "Apply Pixel Emulator Font (All Scenes)")]
        public static void ApplyToAllScenes()
        {
            var font = LoadPixelFont();
            if (font == null)
                return;

            var activeScenePath = SceneManager.GetActiveScene().path;
            int sceneCount = 0;
            int componentCount = 0;

            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                    continue;

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int changed = ApplyToHierarchy(null, font, includeInactive: true);
                if (changed > 0)
                {
                    EditorSceneManager.SaveScene(scene);
                    sceneCount++;
                    componentCount += changed;
                }
            }

            if (!string.IsNullOrEmpty(activeScenePath))
                EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);

            Debug.Log(
                $"[Disk Golf] Applied Pixel Emulator SDF to {componentCount} TextMeshPro components across {sceneCount} scenes.");
        }

        [MenuItem(MenuRoot + "Set TMP Settings Default Font")]
        public static void SetTmpSettingsDefaultFont()
        {
            var font = LoadPixelFont();
            if (font == null)
                return;

            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(ProjectArtPaths.ThirdParty.TmpSettings);
            if (settings == null)
            {
                Debug.LogError("[Disk Golf] TMP Settings asset not found.");
                return;
            }

            var so = new SerializedObject(settings);
            so.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[Disk Golf] TMP Settings default font set to Pixel Emulator SDF.");
        }

        static TMP_FontAsset LoadPixelFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ProjectArtPaths.ThirdParty.PixelEmulatorSdf);
            if (font == null)
                Debug.LogError($"[Disk Golf] Font not found at {ProjectArtPaths.ThirdParty.PixelEmulatorSdf}");

            return font;
        }

        static int ApplyToHierarchy(Transform root, TMP_FontAsset font, bool includeInactive)
        {
            int count = 0;

            foreach (var tmp in FindTmpComponents(root, includeInactive))
            {
                if (tmp.font == font)
                    continue;

                Undo.RecordObject(tmp, "Apply Pixel Emulator font");
                tmp.font = font;
                EditorUtility.SetDirty(tmp);
                count++;
            }

            return count;
        }

        static System.Collections.Generic.IEnumerable<TextMeshProUGUI> FindTmpComponents(
            Transform root,
            bool includeInactive)
        {
            if (root != null)
            {
                foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(includeInactive))
                    yield return tmp;

                foreach (var tmp in root.GetComponentsInChildren<TextMeshPro>(includeInactive))
                {
                    if (tmp.font == null)
                        continue;

                    // TextMeshPro is 3D; skip unless we add support later.
                }

                yield break;
            }

            foreach (var tmp in Object.FindObjectsOfType<TextMeshProUGUI>(includeInactive))
            {
                if (EditorUtility.IsPersistent(tmp))
                    continue;

                if (!tmp.gameObject.scene.IsValid())
                    continue;

                yield return tmp;
            }
        }
    }
}
#endif
