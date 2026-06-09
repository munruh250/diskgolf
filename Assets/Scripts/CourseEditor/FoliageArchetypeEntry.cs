using System;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    [Serializable]
    public sealed class FoliageArchetypeEntry
    {
        public string archetypeId = "tree_round";
        public Sprite sprite;
        public int sortingOrder = 10;
    }
}
