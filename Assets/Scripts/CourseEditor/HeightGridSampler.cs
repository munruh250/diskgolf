using UnityEngine;

namespace DiskGolf.CourseEditor
{
    public static class HeightGridSampler
    {
        const int DefaultPaddingTiles = 1;

        public static Vector2Int GridSizeForHole(HoleData data)
        {
            if (data == null || data.SurfaceTiles == null || data.SurfaceTiles.Count == 0)
            {
                return new Vector2Int(1, 1);
            }

            var bounds = data.ComputeBoundsMax();
            return new Vector2Int(
                Mathf.Max(1, bounds.x + DefaultPaddingTiles),
                Mathf.Max(1, bounds.y + DefaultPaddingTiles));
        }

        public static float SampleWorldY(HoleData data, float worldX, float worldZ)
        {
            if (data == null || data.Elevation == null)
            {
                return 0f;
            }

            var grid = data.Elevation;
            if (grid.Width <= 0 || grid.Height <= 0)
            {
                return 0f;
            }

            float gridX = (worldX - data.Origin.x) / data.TileSize;
            float gridZ = (worldZ - data.Origin.y) / data.TileSize;

            if (grid.Width == 1 && grid.Height == 1)
            {
                return grid.Get(0, 0);
            }

            float u = grid.Width > 1 ? Mathf.Clamp01(gridX / (grid.Width - 1)) : 0f;
            float v = grid.Height > 1 ? Mathf.Clamp01(gridZ / (grid.Height - 1)) : 0f;
            return grid.SampleBilinear(u, v);
        }

        /// <summary>Corner heights for a tile in CourseBuilder vertex order: SW, NW, NE, SE.</summary>
        public static float[] TileCornerHeights(HoleData data, int tileX, int tileY)
        {
            return new[]
            {
                SampleGridCorner(data, tileX, tileY),
                SampleGridCorner(data, tileX, tileY + 1),
                SampleGridCorner(data, tileX + 1, tileY + 1),
                SampleGridCorner(data, tileX + 1, tileY)
            };
        }

        /// <summary>Matches painted tile mesh height at a world XZ point.</summary>
        public static float SampleTileSurfaceWorldY(HoleData data, float worldX, float worldZ)
        {
            if (data == null || data.Elevation == null)
                return 0f;

            float tileXf = (worldX - data.Origin.x) / data.TileSize;
            float tileZf = (worldZ - data.Origin.y) / data.TileSize;
            int tileX = Mathf.FloorToInt(tileXf);
            int tileY = Mathf.FloorToInt(tileZf);
            float fx = Mathf.Clamp01(tileXf - tileX);
            float fz = Mathf.Clamp01(tileZf - tileY);

            var corners = TileCornerHeights(data, tileX, tileY);
            float south = Mathf.Lerp(corners[0], corners[3], fx);
            float north = Mathf.Lerp(corners[1], corners[2], fx);
            return Mathf.Lerp(south, north, fz);
        }

        static float SampleGridCorner(HoleData data, int gridX, int gridY)
        {
            if (data?.Elevation == null)
            {
                return 0f;
            }

            return data.Elevation.Get(gridX, gridY);
        }
    }
}
