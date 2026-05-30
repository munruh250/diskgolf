using UnityEngine;

namespace DiskGolf.Gameplay
{
    /// <summary>Loads sliced foliage sprites from Resources/Foliage.</summary>
    public static class FoliageSprites
    {
        const string Root = "Foliage/";

        public const string GrassLightA = "GrassLight_A";

        public const string GrassDarkA = "GrassDark_A";

        public const string GrassLightB = "GrassLight_B";

        public const string GrassDarkB = "GrassDark_B";

        public const string TreeConical = "TreeConical";

        public const string TreeRound = "TreeRound";

        public static Sprite Load(string spriteName) =>
            Resources.Load<Sprite>(Root + spriteName);

        public static Material CreateUnlitMaterial(string spriteName)
        {
            var sprite = Load(spriteName);
            if (sprite == null || sprite.texture == null)
                return null;

            var mat = new Material(Shader.Find("Unlit/Texture"));
            ApplySpriteRegion(mat, sprite);
            return mat;
        }

        public static void ApplySpriteRegion(Material mat, Sprite sprite)
        {
            if (mat == null || sprite == null || sprite.texture == null)
                return;

            mat.mainTexture = sprite.texture;

            var rect = sprite.textureRect;
            var tex = sprite.texture;
            mat.mainTextureScale = new Vector2(rect.width / tex.width, rect.height / tex.height);
            mat.mainTextureOffset = new Vector2(rect.x / tex.width, rect.y / tex.height);
        }
    }
}
