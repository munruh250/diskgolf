using UnityEngine;

namespace DiskGolf.Gameplay
{
    static class BasketSpriteUtil
    {
        public const float PixelsPerUnit = 220f;

        public static Sprite CreateGroundAlignedSprite(Texture2D texture)
        {
            if (texture == null)
                return null;

            if (!texture.isReadable)
            {
                return Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0f),
                    PixelsPerUnit);
            }

            if (!TryGetOpaqueRect(texture, out var rect))
            {
                return Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0f),
                    PixelsPerUnit);
            }

            return Sprite.Create(
                texture,
                new Rect(rect.x, rect.y, rect.width, rect.height),
                new Vector2(0.5f, 0f),
                PixelsPerUnit);
        }

        public static float ComputeGroundOffset(Sprite sprite, float uniformScale)
        {
            if (sprite == null)
                return GreyboxScale.BasketSpriteGroundInset;

            float footLift = -sprite.bounds.min.y * uniformScale;
            return footLift + GreyboxScale.BasketSpriteGroundInset;
        }

        public static bool TryGetOpaqueRect(Texture2D texture, out RectInt rect)
        {
            rect = default;
            if (texture == null || !texture.isReadable)
                return false;

            int width = texture.width;
            int height = texture.height;
            var pixels = texture.GetPixels32();

            int minX = width;
            int minY = height;
            int maxX = 0;
            int maxY = 0;
            bool found = false;

            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    if (pixels[row + x].a <= 10)
                        continue;

                    found = true;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            if (!found)
                return false;

            rect = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
            return rect.width > 0 && rect.height > 0;
        }
    }
}
