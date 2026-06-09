using UnityEngine;

namespace DiskGolf.CourseEditor
{
    /// <summary>
    /// ScriptableObject wrapper for hole data so editor tools have a stable Undo target.
    /// </summary>
    public sealed class HoleDataAsset : ScriptableObject
    {
        public HoleData Data = new();
    }
}
