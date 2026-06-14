namespace DiskGolf.CourseEditor
{
    public static class HazardTags
    {
        public static string ToUnityTag(HazardType type) => type switch
        {
            HazardType.Water => "Water",
            HazardType.OB => "OB",
            _ => "Water"
        };
    }
}
