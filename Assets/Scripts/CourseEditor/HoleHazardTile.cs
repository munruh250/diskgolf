using System;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    [Serializable]
    public sealed class HoleHazardTile
    {
        public int X;
        public int Y;
        public HazardType Type;

        public HoleHazardTile() { }

        public HoleHazardTile(int x, int y, HazardType type)
        {
            X = x;
            Y = y;
            Type = type;
        }
    }
}
