#if UNITY_EDITOR
using DiskGolf.Gameplay;
using UnityEditor;
using UnityEngine;

namespace DiskGolf.EditorTools
{
    /// <summary>Configures foliage sprite import settings (bottom pivot, PPU) for editor-built courses.</summary>
    static class FoliageSpriteImporter
    {
        const float FoliagePixelsPerUnit = 64f;

        static readonly Vector2 BottomCenterPivot = new(0.5f, 0f);

        [InitializeOnLoadMethod]
        static void ScheduleImport() => EditorApplication.delayCall += OnDelayedImport;

        [MenuItem("Disk Golf/Refresh Foliage Sprite Import")]
        public static void RefreshFromMenu() => ConfigureFoliageSprites(force: true);

        static void OnDelayedImport() => ConfigureFoliageSprites(force: false);

        static void ConfigureFoliageSprites(bool force)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var spritesRoot = ProjectArtPaths.Environment.Foliage.SpritesRoot;
            if (!AssetDatabase.IsValidFolder(spritesRoot))
                return;

            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { spritesRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                ConfigureSpriteImporter(path, force);
            }

            GameplayArtCatalogBuilder.EnsureCatalog(force: true);
        }

        static void ConfigureSpriteImporter(string path, bool force)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            bool dirty = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                dirty = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                dirty = true;
            }

            if (importer.alphaSource != TextureImporterAlphaSource.FromInput)
            {
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                dirty = true;
            }

            if (Mathf.Abs(importer.spritePixelsPerUnit - FoliagePixelsPerUnit) > 0.1f)
            {
                importer.spritePixelsPerUnit = FoliagePixelsPerUnit;
                dirty = true;
            }

            if (importer.spritePivot != BottomCenterPivot)
            {
                importer.spritePivot = BottomCenterPivot;
                dirty = true;
            }

            if (dirty || force)
                importer.SaveAndReimport();
        }
    }
}
#endif
