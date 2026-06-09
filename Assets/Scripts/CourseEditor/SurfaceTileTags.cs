namespace DiskGolf.CourseEditor
{
    public static class SurfaceTileTags
    {
        public static string ToUnityTag(SurfaceTileType type) => type switch
        {
            SurfaceTileType.Tee => "Tee",
            SurfaceTileType.Fairway => "Fairway",
            SurfaceTileType.Rough => "Rough",
            SurfaceTileType.Green => "Green",
            _ => "Fairway"
        };
    }
}
