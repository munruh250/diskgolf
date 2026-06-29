using DiskGolf.Gameplay;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    /// <summary>Applies hole theme lighting and skybox to the active scene.</summary>
    public static class SceneEnvironmentApplier
    {
        public static void Apply(HoleData hole, ThemePack theme)
        {
            SceneLightingBootstrap.Apply();

            Material skybox = null;
            if (theme != null && hole?.Hole != null)
                skybox = theme.ResolveSkybox(hole.Hole.SkyboxId);

            skybox ??= RuntimeArt.LoadPrototypeSkyboxMaterial();
            if (skybox != null)
                RenderSettings.skybox = skybox;
        }
    }
}
