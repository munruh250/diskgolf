using UnityEngine;

namespace DiskGolf.CourseEditor
{
    /// <summary>Helpers for editor picker thumbnails (skybox horizon band, etc.).</summary>
    public static class ThemeArtPreview
    {
        public static readonly Rect HorizonBandUvRect = new(0f, 0.35f, 1f, 0.3f);

        public static Texture GetMainTexture(Material material)
        {
            if (material == null)
                return null;

            if (material.HasProperty("_MainTex"))
            {
                var mainTex = material.GetTexture("_MainTex");
                if (mainTex != null)
                    return mainTex;
            }

            return material.mainTexture;
        }
    }
}
