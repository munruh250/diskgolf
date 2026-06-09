using System.Collections.Generic;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    [CreateAssetMenu(fileName = "ThemePack", menuName = "DiskGolf/Course/Theme Pack")]
    public sealed class ThemePack : ScriptableObject
    {
        public string themeId = "temperate";
        public string displayName = "Temperate";
        public Material fairwayMaterial;
        public Material roughMaterial;
        public Material greenMaterial;
        public Material teeMaterial;
        public List<FoliageArchetypeEntry> foliage = new();

        public Material GetMaterial(SurfaceTileType type) => type switch
        {
            SurfaceTileType.Tee => teeMaterial != null ? teeMaterial : fairwayMaterial,
            SurfaceTileType.Fairway => fairwayMaterial,
            SurfaceTileType.Rough => roughMaterial,
            SurfaceTileType.Green => greenMaterial,
            _ => fairwayMaterial
        };

        public Sprite ResolveFoliage(string archetypeId)
        {
            foreach (var entry in foliage)
            {
                if (entry.archetypeId == archetypeId)
                    return entry.sprite;
            }

            return null;
        }
    }
}
