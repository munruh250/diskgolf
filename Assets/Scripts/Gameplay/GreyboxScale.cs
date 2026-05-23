namespace DiskGolf.Gameplay
{
    /// <summary>Increment when greybox auto-setup logic changes (triggers editor re-migration).</summary>
    public static class GreyboxScale
    {
        public const int SetupVersion = 10;

        /// <summary>Local Y offset for thrower sprite child (feet alignment on tee).</summary>
        public const float ThrowerSpriteLocalY = 0.730f;

        public const float DiscDiameterM = 0.21f;
        public const float DiscThicknessM = 0.05f;
        public const float BasketCatchDiameterM = DiscDiameterM * 10f;
        public const float BasketCatchHeightM = 1.35f;
        public const float PoleDiameterM = 0.05f;

        public static readonly UnityEngine.Color DiscColor = new(0.92f, 0.42f, 0.06f);
        public static readonly UnityEngine.Color BasketColor = new(0.46f, 0.49f, 0.53f);
        public static readonly UnityEngine.Color ThrowerColor = new(0.22f, 0.45f, 0.78f);
    }
}
