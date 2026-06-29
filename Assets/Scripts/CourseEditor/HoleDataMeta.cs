using System;

namespace DiskGolf.CourseEditor
{
    [Serializable]
    public sealed class HoleDataMeta
    {
        public string displayName;
        public bool published;
        public string lastEditedUtc;
        public string templateSource;
    }
}
