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
        public string skyboxId = "sky_clear";
    }

    [Serializable]
    public sealed class ElevationGridDto
    {
        public int width;
        public int height;
        public float[] heights = Array.Empty<float>();
    }

    [Serializable]
    public sealed class HazardVertexDto
    {
        public int x;
        public int y;
    }

    [Serializable]
    public sealed class HazardTileDto
    {
        public int x;
        public int y;
        public string type = "water";
    }

    [Serializable]
    public sealed class HazardPolygonDto
    {
        public string id = "hazard_01";
        public string type = "water";
        public HazardVertexDto[] vertices = Array.Empty<HazardVertexDto>();
    }

    [Serializable]
    public sealed class FoliagePlacementDto
    {
        public string archetype = "tree_round";
        public float x;
        public float z;
        public float yaw;
        public float scale = 1f;
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
        public ElevationGridDto elevation;
        public HoleTileDto[] surfaceTiles = Array.Empty<HoleTileDto>();
        public HazardTileDto[] hazardTiles = Array.Empty<HazardTileDto>();
        public HazardPolygonDto[] hazards = Array.Empty<HazardPolygonDto>();
        public FoliagePlacementDto[] placements = Array.Empty<FoliagePlacementDto>();
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
                elevation = ToElevationDto(data.Elevation),
                surfaceTiles = tiles,
                hazardTiles = ToHazardTileDtos(data.HazardTiles),
                hazards = ToHazardDtos(data.Hazards),
                placements = ToPlacementDtos(data.Placements),
                hole = new HoleMetaDto
                {
                    tee = new[] { data.Hole.Tee.x, data.Hole.Tee.y },
                    basket = new[] { data.Hole.Basket.x, data.Hole.Basket.y },
                    par = data.Hole.Par,
                    circleRadiusFt = data.Hole.CircleRadiusFt,
                    skyboxId = string.IsNullOrEmpty(data.Hole.SkyboxId) ? "sky_clear" : data.Hole.SkyboxId
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

            data.Elevation = ToElevationDomain(elevation);
            data.Hazards = ToHazardDomain(hazards);
            data.Placements = ToPlacementDomain(placements);

            if (hazardTiles != null)
            {
                foreach (var tile in hazardTiles)
                {
                    if (!TryParseHazardType(tile.type, out var type))
                        continue;

                    data.SetHazardTile(tile.x, tile.y, type);
                }
            }

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
                data.Hole.SkyboxId = string.IsNullOrEmpty(hole.skyboxId) ? "sky_clear" : hole.skyboxId;
            }

            return data;
        }

        static ElevationGridDto ToElevationDto(ElevationGrid grid)
        {
            if (grid == null || grid.Width <= 0 || grid.Height <= 0 || grid.Heights == null || grid.Heights.Length == 0)
            {
                return null;
            }

            var heights = new float[grid.Width * grid.Height];
            Array.Copy(grid.Heights, heights, Mathf.Min(grid.Heights.Length, heights.Length));

            return new ElevationGridDto
            {
                width = grid.Width,
                height = grid.Height,
                heights = heights
            };
        }

        static ElevationGrid ToElevationDomain(ElevationGridDto dto)
        {
            if (dto == null || dto.width <= 0 || dto.height <= 0 || dto.heights == null || dto.heights.Length == 0)
            {
                return null;
            }

            var grid = new ElevationGrid(dto.width, dto.height);
            int copyLength = Mathf.Min(dto.heights.Length, grid.Heights.Length);
            Array.Copy(dto.heights, grid.Heights, copyLength);
            return grid;
        }

        static HazardTileDto[] ToHazardTileDtos(System.Collections.Generic.List<HoleHazardTile> tiles)
        {
            if (tiles == null || tiles.Count == 0)
                return Array.Empty<HazardTileDto>();

            var dtos = new HazardTileDto[tiles.Count];
            for (int i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                dtos[i] = new HazardTileDto
                {
                    x = tile.X,
                    y = tile.Y,
                    type = tile.Type.ToString().ToLowerInvariant()
                };
            }

            return dtos;
        }

        static HazardPolygonDto[] ToHazardDtos(System.Collections.Generic.List<HazardPolygon> hazards)
        {
            if (hazards == null || hazards.Count == 0)
            {
                return Array.Empty<HazardPolygonDto>();
            }

            var dtos = new HazardPolygonDto[hazards.Count];
            for (int i = 0; i < hazards.Count; i++)
            {
                var hazard = hazards[i];
                var vertices = new HazardVertexDto[hazard.Vertices.Count];
                for (int v = 0; v < hazard.Vertices.Count; v++)
                {
                    var vertex = hazard.Vertices[v];
                    vertices[v] = new HazardVertexDto { x = vertex.x, y = vertex.y };
                }

                dtos[i] = new HazardPolygonDto
                {
                    id = hazard.Id,
                    type = hazard.Type.ToString().ToLowerInvariant(),
                    vertices = vertices
                };
            }

            return dtos;
        }

        static System.Collections.Generic.List<HazardPolygon> ToHazardDomain(HazardPolygonDto[] dtos)
        {
            var hazards = new System.Collections.Generic.List<HazardPolygon>();
            if (dtos == null)
            {
                return hazards;
            }

            foreach (var dto in dtos)
            {
                if (dto == null || dto.vertices == null || dto.vertices.Length == 0)
                {
                    continue;
                }

                if (!TryParseHazardType(dto.type, out var type))
                {
                    continue;
                }

                var vertices = new System.Collections.Generic.List<Vector2Int>(dto.vertices.Length);
                foreach (var vertex in dto.vertices)
                {
                    if (vertex == null)
                    {
                        continue;
                    }

                    vertices.Add(new Vector2Int(vertex.x, vertex.y));
                }

                if (vertices.Count == 0)
                {
                    continue;
                }

                hazards.Add(new HazardPolygon(
                    string.IsNullOrEmpty(dto.id) ? "hazard_01" : dto.id,
                    type,
                    vertices));
            }

            return hazards;
        }

        static bool TryParseHazardType(string value, out HazardType type)
        {
            if (string.IsNullOrEmpty(value))
            {
                type = default;
                return false;
            }

            if (value.Equals("ob", System.StringComparison.OrdinalIgnoreCase))
            {
                type = HazardType.OB;
                return true;
            }

            return Enum.TryParse(value, true, out type);
        }

        static FoliagePlacementDto[] ToPlacementDtos(System.Collections.Generic.List<FoliagePlacement> placements)
        {
            if (placements == null || placements.Count == 0)
            {
                return Array.Empty<FoliagePlacementDto>();
            }

            var dtos = new FoliagePlacementDto[placements.Count];
            for (int i = 0; i < placements.Count; i++)
            {
                var placement = placements[i];
                dtos[i] = new FoliagePlacementDto
                {
                    archetype = placement.Archetype,
                    x = placement.X,
                    z = placement.Z,
                    yaw = placement.Yaw,
                    scale = placement.Scale
                };
            }

            return dtos;
        }

        static System.Collections.Generic.List<FoliagePlacement> ToPlacementDomain(FoliagePlacementDto[] dtos)
        {
            var placements = new System.Collections.Generic.List<FoliagePlacement>();
            if (dtos == null)
            {
                return placements;
            }

            foreach (var dto in dtos)
            {
                if (dto == null || string.IsNullOrEmpty(dto.archetype))
                {
                    continue;
                }

                placements.Add(new FoliagePlacement(
                    dto.archetype,
                    dto.x,
                    dto.z,
                    dto.yaw,
                    dto.scale <= 0f ? 1f : dto.scale));
            }

            return placements;
        }
    }
}
