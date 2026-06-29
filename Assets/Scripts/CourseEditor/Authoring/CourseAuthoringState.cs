using System.Collections.Generic;
using UnityEngine;

namespace DiskGolf.CourseEditor.Authoring
{
    public sealed class CourseAuthoringState
    {
        public CourseAuthoringTool ActiveTool { get; set; } = CourseAuthoringTool.Paint;
        public SurfaceTileType BrushType { get; set; } = SurfaceTileType.Fairway;
        public int ElevateRadius { get; set; } = 2;
        public float ElevateStrength { get; set; } = 0.25f;
        public bool ElevateSmooth { get; set; }
        public HazardType HazardBrushType { get; set; } = HazardType.Water;
        public string FoliageArchetype { get; set; } = "tree_round";
        public List<Vector2Int> HazardDraftVertices { get; set; } = new();
        public bool IsDirty { get; set; }
    }
}
