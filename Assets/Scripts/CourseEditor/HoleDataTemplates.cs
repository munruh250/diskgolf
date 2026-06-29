using System;
using System.IO;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    public static class HoleDataTemplates
    {
        public const string StraightPar3Id = "template_straight_par3";

        const int StraightPar3CenterX = 6;
        const int StraightPar3BasketTileY = 114;

        public static HoleData CreateBlankPar3(string displayName)
        {
            return CreateStarterPar3(displayName, "blank_par3");
        }

        public static HoleData CreateStraightPar3(string displayName)
        {
            return CreateStarterPar3(displayName, StraightPar3Id);
        }

        static HoleData CreateStarterPar3(string displayName, string idSuffix)
        {
            var data = NewBase(displayName, idSuffix);
            int centerX = StraightPar3CenterX;
            int basketY = StraightPar3BasketTileY;
            int width = 12;
            int length = basketY + 6;

            for (int x = 0; x < width; x++)
            for (int y = 0; y < length; y++)
                data.SetTile(x, y, SurfaceTileType.Rough);

            for (int y = 0; y <= basketY; y++)
                data.SetTile(centerX, y, SurfaceTileType.Fairway);

            for (int y = basketY - 4; y <= basketY; y++)
                data.SetTile(centerX, y, SurfaceTileType.Green);

            data.SetTile(centerX, 0, SurfaceTileType.Tee);
            data.Hole.Tee = TileCenter(data, centerX, 0);
            data.Hole.Basket = TileCenter(data, centerX, basketY);
            data.Hole.Par = 3;
            return data;
        }

        public static HoleData CreateFromTemplateId(string templateId, string displayName)
        {
            return templateId switch
            {
                StraightPar3Id => CreateStraightPar3(displayName),
                "blank_par3" => CreateBlankPar3(displayName),
                "ridgeline" => HoleDataJson.LoadFromFile(
                    Path.Combine(Application.dataPath, "Data/Courses/Example/hole_01.json")),
                _ => CreateBlankPar3(displayName)
            };
        }

        static HoleData NewBase(string displayName, string idSuffix)
        {
            return new HoleData
            {
                Id = $"{idSuffix}_{Guid.NewGuid():N}".Substring(0, 24),
                Name = displayName,
                ThemeId = "temperate",
                TileSize = HoleData.DefaultTileSize
            };
        }

        static Vector2 TileCenter(HoleData data, int x, int y) =>
            new(data.Origin.x + (x + 0.5f) * data.TileSize, data.Origin.y + (y + 0.5f) * data.TileSize);
    }
}
