#if UNITY_EDITOR
using DiskGolf.CourseEditor;
using DiskGolf.Gameplay;
using UnityEditor;
using UnityEngine;

namespace DiskGolf.EditorTools.CourseEditor
{
    /// <summary>Creates or repairs the default temperate theme pack for the course editor.</summary>
    public static class ThemePackBootstrap
    {
        public const string TemperateAssetPath = ProjectArtPaths.Data.ThemesRoot + "/ThemePack_Temperate.asset";

        [MenuItem("Disk Golf/Course/Create Default Theme Pack")]
        public static void CreateDefaultThemePackMenu()
        {
            var theme = EnsureTemperateThemePack(forceRecreate: true);
            if (theme == null)
            {
                EditorUtility.DisplayDialog(
                    "Course Editor",
                    "Could not create ThemePack_Temperate. Check the Console for missing material paths.",
                    "OK");
                return;
            }

            Selection.activeObject = theme;
            EditorGUIUtility.PingObject(theme);
            EditorUtility.DisplayDialog(
                "Course Editor",
                $"Created {TemperateAssetPath}.\nAssign it in Disk Golf → Course Editor, or reopen the window.",
                "OK");
        }

        public static ThemePack EnsureTemperateThemePack(bool forceRecreate = false)
        {
            EnsureThemesFolder();

            if (!forceRecreate)
            {
                var existing = AssetDatabase.LoadAssetAtPath<ThemePack>(TemperateAssetPath);
                if (existing != null)
                    return existing;

                if (AssetDatabase.LoadMainAssetAtPath(TemperateAssetPath) != null)
                {
                    Debug.LogWarning(
                        $"[Disk Golf] {TemperateAssetPath} has a broken script reference; recreating theme pack.");
                    AssetDatabase.DeleteAsset(TemperateAssetPath);
                }
            }
            else if (AssetDatabase.LoadMainAssetAtPath(TemperateAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(TemperateAssetPath);
            }

            var theme = ScriptableObject.CreateInstance<ThemePack>();
            if (!TryConfigure(theme))
            {
                Object.DestroyImmediate(theme);
                return null;
            }

            AssetDatabase.CreateAsset(theme, TemperateAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Disk Golf] Created theme pack at {TemperateAssetPath}");
            return AssetDatabase.LoadAssetAtPath<ThemePack>(TemperateAssetPath);
        }

        static void EnsureThemesFolder()
        {
            if (!AssetDatabase.IsValidFolder(ProjectArtPaths.Data.Root))
                AssetDatabase.CreateFolder("Assets", "Data");

            if (!AssetDatabase.IsValidFolder(ProjectArtPaths.Data.ThemesRoot))
                AssetDatabase.CreateFolder(ProjectArtPaths.Data.Root, "Themes");
        }

        static bool TryConfigure(ThemePack theme)
        {
            theme.themeId = "temperate";
            theme.displayName = "Temperate";
            theme.fairwayMaterial = LoadMaterial(ProjectArtPaths.Environment.Fairway.Material, "fairway");
            theme.roughMaterial = LoadMaterial(ProjectArtPaths.Environment.Rough.Material, "rough");
            theme.greenMaterial = LoadMaterial(ProjectArtPaths.Environment.Green.Material, "green");
            theme.teeMaterial = theme.fairwayMaterial;

            var treeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ProjectArtPaths.Environment.Foliage.Tree);
            if (treeSprite == null)
            {
                var catalog = AssetDatabase.LoadAssetAtPath<GameplayArtCatalog>(
                    ProjectArtPaths.Runtime.GameplayArtCatalog);
                treeSprite = catalog != null ? catalog.treeRound : null;
            }

            theme.foliage.Clear();
            if (treeSprite != null)
            {
                theme.foliage.Add(new FoliageArchetypeEntry
                {
                    archetypeId = "tree_round",
                    sprite = treeSprite,
                    sortingOrder = 10
                });
            }

            if (theme.fairwayMaterial == null || theme.roughMaterial == null || theme.greenMaterial == null)
            {
                Debug.LogError(
                    "[Disk Golf] ThemePack bootstrap failed: fairway, rough, or green material missing. "
                    + "Expected MAT_Fairway.mat, MAT_Rough.mat, MAT_Green.mat under Assets/Art/Environment/Course/.");
                return false;
            }

            return true;
        }

        static Material LoadMaterial(string path, string label)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
                Debug.LogWarning($"[Disk Golf] Theme pack: missing {label} material at {path}");

            return material;
        }
    }
}
#endif
