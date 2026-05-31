using System.Collections.Generic;
using DiskGolf.Gameplay;
using UnityEditor;
using UnityEngine;

namespace DiskGolf.EditorTools
{
    /// <summary>Imports basket sprite with edge-connected background transparency.</summary>
    sealed class BasketSpriteImporter : AssetPostprocessor
    {
        const string BasketFolder = ProjectArtPaths.Environment.Basket.Root + "/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(BasketFolder) || !assetPath.EndsWith(".png"))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePivot = new Vector2(0.5f, 0f);
            importer.spritePixelsToUnits = 220f;
            importer.isReadable = true;
        }

        void OnPostprocessTexture(Texture2D texture)
        {
            if (!assetPath.StartsWith(BasketFolder) || !assetPath.EndsWith(".png"))
                return;

            KeyBorderBackgroundToTransparent(texture, 0.07f);
        }

        /// <summary>
        /// Only removes black pixels connected to the image border so the black basket frame stays visible.
        /// </summary>
        static void KeyBorderBackgroundToTransparent(Texture2D texture, float threshold)
        {
            int width = texture.width;
            int height = texture.height;
            var pixels = texture.GetPixels();
            int total = pixels.Length;
            var remove = new bool[total];
            var queue = new Queue<int>();
            float thresholdSq = threshold * threshold;

            bool IsBackgroundColor(Color pixel) =>
                pixel.a > 0.01f
                && pixel.r * pixel.r + pixel.g * pixel.g + pixel.b * pixel.b <= thresholdSq;

            void TryEnqueue(int x, int y)
            {
                if (x < 0 || y < 0 || x >= width || y >= height)
                    return;

                int index = y * width + x;
                if (remove[index] || !IsBackgroundColor(pixels[index]))
                    return;

                remove[index] = true;
                queue.Enqueue(index);
            }

            for (int x = 0; x < width; x++)
                TryEnqueue(x, height - 1);

            int bottomSeedCutoff = Mathf.Max(1, Mathf.RoundToInt(height * 0.04f));
            for (int y = bottomSeedCutoff; y < height; y++)
            {
                TryEnqueue(0, y);
                TryEnqueue(width - 1, y);
            }

            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int x = index % width;
                int y = index / width;
                TryEnqueue(x - 1, y);
                TryEnqueue(x + 1, y);
                TryEnqueue(x, y - 1);
                TryEnqueue(x, y + 1);
            }

            for (int i = 0; i < total; i++)
            {
                if (!remove[i])
                    continue;

                var pixel = pixels[i];
                pixel.a = 0f;
                pixels[i] = pixel;
            }

            texture.SetPixels(pixels);
            texture.Apply();
        }

        [MenuItem("Disk Golf/Reimport Basket Sprite")]
        public static void ReimportBasketSprite()
        {
            AssetDatabase.ImportAsset(ProjectArtPaths.Environment.Basket.Sprite, ImportAssetOptions.ForceUpdate);
            Debug.Log("[Disk Golf] Reimported basket sprite.");
        }
    }
}
