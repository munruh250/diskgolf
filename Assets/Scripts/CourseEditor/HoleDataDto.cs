using System;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    [Serializable]
    public sealed class HoleTileDto
    {
        public int x;
        public int y;
        public string type;
    }

    [Serializable]
    public sealed class HoleMetaDto
    {
        public float[] tee = { 0f, 0f };
        public float[] basket = { 0f, 0f };
        public int par = 3;
        public float circleRadiusFt = 33f;
    }

    [Serializable]
    public sealed class HoleDataDto
    {
        public int schemaVersion = HoleData.SchemaVersion;
        public string id = "hole_01";
        public string name = "New Hole";
        public string units = "meters";
        public float tileSize = HoleData.DefaultTileSize;
        public float[] origin = { 0f, 0f };
        public string themeId = "temperate";
        public HoleTileDto[] surfaceTiles = Array.Empty<HoleTileDto>();
        public HoleMetaDto hole = new();

        public static HoleDataDto FromDomain(HoleData data)
        {
            var tiles = new HoleTileDto[data.SurfaceTiles.Count];
            for (int i = 0; i < data.SurfaceTiles.Count; i++)
            {
                var t = data.SurfaceTiles[i];
                tiles[i] = new HoleTileDto
                {
                    x = t.X,
                    y = t.Y,
                    type = t.Type.ToString().ToLowerInvariant()
                };
            }

            return new HoleDataDto
            {
                schemaVersion = data.SchemaVersionField,
                id = data.Id,
                name = data.Name,
                tileSize = data.TileSize,
                origin = new[] { data.Origin.x, data.Origin.y },
                themeId = data.ThemeId,
                surfaceTiles = tiles,
                hole = new HoleMetaDto
                {
                    tee = new[] { data.Hole.Tee.x, data.Hole.Tee.y },
                    basket = new[] { data.Hole.Basket.x, data.Hole.Basket.y },
                    par = data.Hole.Par,
                    circleRadiusFt = data.Hole.CircleRadiusFt
                }
            };
        }

        public HoleData ToDomain()
        {
            var data = new HoleData
            {
                SchemaVersionField = schemaVersion,
                Id = id,
                Name = name,
                TileSize = tileSize,
                Origin = new Vector2(origin[0], origin[1]),
                ThemeId = themeId
            };

            if (surfaceTiles != null)
            {
                foreach (var tile in surfaceTiles)
                {
                    if (!Enum.TryParse<SurfaceTileType>(tile.type, true, out var type))
                        continue;

                    data.SetTile(tile.x, tile.y, type);
                }
            }

            if (hole != null)
            {
                data.Hole.Tee = new Vector2(hole.tee[0], hole.tee[1]);
                data.Hole.Basket = new Vector2(hole.basket[0], hole.basket[1]);
                data.Hole.Par = hole.par;
                data.Hole.CircleRadiusFt = hole.circleRadiusFt;
            }

            return data;
        }
    }
}
