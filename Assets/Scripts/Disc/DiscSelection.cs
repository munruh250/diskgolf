namespace DiskGolf.Disc
{
    /// <summary>Picks the sensible disc class for a given carry distance.</summary>
    public static class DiscSelection
    {
        public const float PutterMaxFt = 90f;

        public const float MidrangeMaxFt = 250f;

        public const float FairwayMaxFt = 370f;

        public static DiscCategory RecommendCategory(float distanceFt)
        {
            if (distanceFt <= PutterMaxFt)
                return DiscCategory.Putter;

            if (distanceFt <= MidrangeMaxFt)
                return DiscCategory.Mid;

            if (distanceFt <= FairwayMaxFt)
                return DiscCategory.Fairway;

            return DiscCategory.Distance;
        }
    }
}
