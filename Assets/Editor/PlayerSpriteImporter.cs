#if UNITY_EDITOR
using DiskGolf.Gameplay;
using UnityEditor;
using UnityEngine;

namespace DiskGolf.EditorTools
{
    static class PlayerSpriteImporter
    {
        const string AssetPath = ProjectArtPaths.Characters.ThrowerSprite;

        [InitializeOnLoadMethod]
        static void ScheduleImport() => EditorApplication.delayCall += EnsureImported;

        static void EnsureImported()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var importer = AssetImporter.GetAtPath(AssetPath) as TextureImporter;
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

            if (Mathf.Abs(importer.spritePixelsPerUnit - 420f) > 0.1f)
            {
                importer.spritePixelsPerUnit = 420f;
                dirty = true;
            }

            if (dirty)
                importer.SaveAndReimport();
        }
    }
}
#endif
