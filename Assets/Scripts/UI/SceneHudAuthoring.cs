namespace DiskGolf.UI
{
    /// <summary>
    /// When enabled, HUD widgets under GameplayHUD are authored in the scene and must not be
    /// repositioned or rebuilt at runtime.
    /// </summary>
    public static class SceneHudAuthoring
    {
        public static bool IsActive => HudLayoutSettings.ShouldPreserveLayout();
    }
}
