#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DiskGolf.EditorTools
{
    /// <summary>Exports foliage atlas regions into individual sprite PNGs.</summary>
    static class FoliageSpriteImporter
    {
        const string SheetPath = "Assets/Resources/Foliage/FoliageSheet.png";

        const string OutputFolder = "Assets/Resources/Foliage";

        const float TreePixelsPerUnit = 64f;

        const float GrassPixelsPerUnit = 100f;

        static readonly SliceSpec[] Slices =
        {
            new("GrassLight_A", 0f, 768f, 341f, 256f, GrassPixelsPerUnit, new Vector2(0.5f, 0.5f)),
            new("GrassDark_A", 341f, 768f, 341f, 256f, GrassPixelsPerUnit, new Vector2(0.5f, 0.5f)),
            new("GrassLight_B", 0f, 512f, 341f, 256f, GrassPixelsPerUnit, new Vector2(0.5f, 0.5f)),
            new("GrassDark_B", 341f, 512f, 341f, 256f, GrassPixelsPerUnit, new Vector2(0.5f, 0.5f)),
            new("TreeConical", 0f, 0f, 341f, 512f, TreePixelsPerUnit, new Vector2(0.5f, 0.08f)),
            new("TreeRound", 341f, 0f, 341f, 512f, TreePixelsPerUnit, new Vector2(0.5f, 0.08f)),
        };

        [InitializeOnLoadMethod]
        static void ScheduleImport() => EditorApplication.delayCall += OnDelayedImport;

        static void OnDelayedImport() => EnsureImported(force: false);

        [MenuItem("Disk Golf/Reimport Foliage Sheet")]
        public static void ReimportFromMenu() => EnsureImported(force: true);

        static void EnsureImported(bool force)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!File.Exists(SheetPath))
                return;

            bool exported = force || !AllSlicesExist();
            if (exported)
                ExportSlicesFromSheet();

            foreach (var slice in Slices)
                ConfigureSingleSpriteImporter(slice);
        }

        static bool AllSlicesExist()
        {
            foreach (var slice in Slices)
            {
                if (!File.Exists(GetSlicePath(slice.Name)))
                    return false;
            }

            return true;
        }

        static void ExportSlicesFromSheet()
        {
            var importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
            if (importer == null)
                return;

            bool restoreReadable = !importer.isReadable;
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            var source = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath);
            if (source == null)
                return;

            Directory.CreateDirectory(OutputFolder);

            foreach (var slice in Slices)
            {
                var rect = slice.Rect;
                var pixels = source.GetPixels(
                    Mathf.RoundToInt(rect.x),
                    Mathf.RoundToInt(rect.y),
                    Mathf.RoundToInt(rect.width),
                    Mathf.RoundToInt(rect.height));

                KeyBlackToTransparent(pixels);

                var output = new Texture2D(
                    Mathf.RoundToInt(rect.width),
                    Mathf.RoundToInt(rect.height),
                    TextureFormat.RGBA32,
                    false);

                output.SetPixels(pixels);
                output.Apply();
                File.WriteAllBytes(GetSlicePath(slice.Name), output.EncodeToPNG());
                Object.DestroyImmediate(output);
            }

            AssetDatabase.Refresh();

            if (restoreReadable)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        }

        static void ConfigureSingleSpriteImporter(SliceSpec slice)
        {
            var path = GetSlicePath(slice.Name);
            if (!File.Exists(path))
                return;

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

            if (Mathf.Abs(importer.spritePixelsPerUnit - slice.PixelsPerUnit) > 0.1f)
            {
                importer.spritePixelsPerUnit = slice.PixelsPerUnit;
                dirty = true;
            }

            if (importer.spritePivot != slice.Pivot)
            {
                importer.spritePivot = slice.Pivot;
                dirty = true;
            }

            if (dirty)
                importer.SaveAndReimport();
        }

        static string GetSlicePath(string name) => $"{OutputFolder}/{name}.png";

        static void KeyBlackToTransparent(Color[] pixels)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                ref Color c = ref pixels[i];
                if (c.r > 0.09f || c.g > 0.09f || c.b > 0.09f)
                    continue;

                c.a = 0f;
            }
        }

        readonly struct SliceSpec
        {
            public readonly string Name;
            public readonly Rect Rect;
            public readonly float PixelsPerUnit;
            public readonly Vector2 Pivot;

            public SliceSpec(string name, float x, float y, float w, float h, float ppu, Vector2 pivot)
            {
                Name = name;
                Rect = new Rect(x, y, w, h);
                PixelsPerUnit = ppu;
                Pivot = pivot;
            }
        }
    }
}
#endif
