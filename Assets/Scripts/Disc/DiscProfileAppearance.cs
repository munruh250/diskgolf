using DiskGolf.Gameplay;
using UnityEngine;

namespace DiskGolf.Disc
{
    /// <summary>Applies <see cref="DiscProfile.discMaterial"/> to a disc mesh renderer.</summary>
    public static class DiscProfileAppearance
    {
        public static void Apply(Renderer renderer, DiscProfile profile, Material fallback = null)
        {
            if (renderer == null)
                return;

            var material = ResolveMaterial(profile, fallback);
            if (material != null)
                renderer.sharedMaterial = material;
        }

        public static Material ResolveMaterial(DiscProfile profile, Material fallback = null)
        {
            if (profile != null && profile.discMaterial != null)
                return profile.discMaterial;

            if (fallback != null)
                return fallback;

            return RuntimeArt.LoadDiscMaterial();
        }
    }
}
