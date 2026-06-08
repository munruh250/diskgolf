#if UNITY_EDITOR
using DiskGolf.Gameplay;
using UnityEditor;
using UnityEngine;

namespace DiskGolf.EditorTools
{
    /// <summary>Configures the standalone tree sprite import settings without overwriting authored PNGs.</summary>
    static class FoliageSpriteImporter
    {
        const string TreeSpritePath = ProjectArtPaths.Environment.Foliage.Tree;

        const float TreePixelsPerUnit = 64f;

        static readonly Vector2 TreePivot = new(0.5f, 0.18f);

        [InitializeOnLoadMethod]
        static void ScheduleImport() => EditorApplication.delayCall += OnDelayedImport;

        [MenuItem("Disk Golf/Refresh Tree Sprite Import")]
        public static void RefreshFromMenu() => ConfigureTreeSpriteImporter(force: true);

        static void OnDelayedImport() => ConfigureTreeSpriteImporter(force: false);

        static void ConfigureTreeSpriteImporter(bool force)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!System.IO.File.Exists(TreeSpritePath))
                return;

            var importer = AssetImporter.GetAtPath(TreeSpritePath) as TextureImporter;
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

            if (Mathf.Abs(importer.spritePixelsPerUnit - TreePixelsPerUnit) > 0.1f)
            {
                importer.spritePixelsPerUnit = TreePixelsPerUnit;
                dirty = true;
            }

            if (importer.spritePivot != TreePivot)
            {
                importer.spritePivot = TreePivot;
                dirty = true;
            }

            if (dirty || force)
                importer.SaveAndReimport();

            GameplayArtCatalogBuilder.EnsureCatalog(force: true);
        }
    }
}
#endif
