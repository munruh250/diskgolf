using System;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    [Serializable]
    public sealed class FoliagePlacement
    {
        public string Archetype = "tree_round";
        public float X;
        public float Z;
        public float Yaw;
        public float Scale = 1f;

        public FoliagePlacement() { }

        public FoliagePlacement(string archetype, float x, float z, float yaw = 0f, float scale = 1f)
        {
            Archetype = archetype;
            X = x;
            Z = z;
            Yaw = yaw;
            Scale = scale;
        }
    }
}
