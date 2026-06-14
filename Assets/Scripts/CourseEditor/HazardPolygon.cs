using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    [Serializable]
    public sealed class HazardPolygon
    {
        public string Id = "hazard_01";
        public HazardType Type = HazardType.Water;
        public List<Vector2Int> Vertices = new();

        public HazardPolygon() { }

        public HazardPolygon(string id, HazardType type, IEnumerable<Vector2Int> vertices)
        {
            Id = id;
            Type = type;
            Vertices = new List<Vector2Int>(vertices);
        }
    }
}
