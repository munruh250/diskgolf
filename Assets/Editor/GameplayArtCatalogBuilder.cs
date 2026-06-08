#if UNITY_EDITOR
using DiskGolf.Gameplay;
using UnityEditor;
using UnityEngine;

namespace DiskGolf.EditorTools
{
    /// <summary>Creates and refreshes the single Resources bootstrap catalog.</summary>
    static class GameplayArtCatalogBuilder
    {
        [InitializeOnLoadMethod]
        static void ScheduleBuild() => EditorApplication.delayCall += () => EnsureCatalog(force: false);

        [MenuItem("Disk Golf/Refresh Gameplay Art Catalog")]
        public static void RefreshFromMenu() => EnsureCatalog(force: true);

        public static void EnsureCatalog(bool force)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var catalog = AssetDatabase.LoadAssetAtPath<GameplayArtCatalog>(
                ProjectArtPaths.Runtime.GameplayArtCatalog);

            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<GameplayArtCatalog>();
                System.IO.Directory.CreateDirectory("Assets/Resources");
                AssetDatabase.CreateAsset(catalog, ProjectArtPaths.Runtime.GameplayArtCatalog);
                force = true;
            }

            if (!force && CatalogLooksComplete(catalog))
                return;

            catalog.treeRound = LoadSprite(ProjectArtPaths.Environment.Foliage.Sprite(FoliageSprites.Tree));
            catalog.basket = LoadSprite(ProjectArtPaths.Environment.Basket.Sprite);
            catalog.thrower = LoadSprite(ProjectArtPaths.Characters.ThrowerSprite);
            catalog.prototypeSkybox = AssetDatabase.LoadAssetAtPath<Material>(
                ProjectArtPaths.Environment.Skybox.Material);
            catalog.discPreviewDefault = LoadSprite(ProjectArtPaths.Ui.DiscPreview.DefaultSprite);
            catalog.discDefault = AssetDatabase.LoadAssetAtPath<Material>(ProjectArtPaths.Gameplay.DiscMaterial);

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        static bool CatalogLooksComplete(GameplayArtCatalog catalog) =>
            catalog.treeRound != null
            && catalog.basket != null
            && catalog.thrower != null
            && catalog.prototypeSkybox != null;

        static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
