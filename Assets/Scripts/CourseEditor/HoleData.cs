using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    [Serializable]
    public sealed class HoleTile
    {
        public int X;
        public int Y;
        public SurfaceTileType Type;

        public HoleTile() { }

        public HoleTile(int x, int y, SurfaceTileType type)
        {
            X = x;
            Y = y;
            Type = type;
        }
    }

    [Serializable]
    public sealed class HoleMeta
    {
        public Vector2 Tee = Vector2.zero;
        public Vector2 Basket = Vector2.zero;
        public int Par = 3;
        public float CircleRadiusFt = 33f;
    }

    [Serializable]
    public sealed class HoleData
    {
        public const int SchemaVersion = 1;
        public const float DefaultTileSize = 2f;

        public int SchemaVersionField = SchemaVersion;
        public string Id = "hole_01";
        public string Name = "New Hole";
        public float TileSize = DefaultTileSize;
        public Vector2 Origin = Vector2.zero;
        public string ThemeId = "temperate";
        public List<HoleTile> SurfaceTiles = new();
        public HoleMeta Hole = new();
        public ElevationGrid Elevation;
        public List<HazardPolygon> Hazards = new();

        public void SetTile(int x, int y, SurfaceTileType type)
        {
            for (int i = 0; i < SurfaceTiles.Count; i++)
            {
                if (SurfaceTiles[i].X == x && SurfaceTiles[i].Y == y)
                {
                    SurfaceTiles[i] = new HoleTile(x, y, type);
                    return;
                }
            }

            SurfaceTiles.Add(new HoleTile(x, y, type));
        }

        public bool TryGetTile(int x, int y, out SurfaceTileType type)
        {
            foreach (var tile in SurfaceTiles)
            {
                if (tile.X == x && tile.Y == y)
                {
                    type = tile.Type;
                    return true;
                }
            }

            type = default;
            return false;
        }

        public void ClearTile(int x, int y)
        {
            SurfaceTiles.RemoveAll(t => t.X == x && t.Y == y);
        }

        public Vector2Int ComputeBoundsMax()
        {
            int maxX = 0;
            int maxY = 0;

            foreach (var tile in SurfaceTiles)
            {
                maxX = Mathf.Max(maxX, tile.X);
                maxY = Mathf.Max(maxY, tile.Y);
            }

            return new Vector2Int(maxX + 1, maxY + 1);
        }

        /// <summary>Grid extent for editor overlay — at least <paramref name="minTilesX"/> × <paramref name="minTilesY"/>.</summary>
        public Vector2Int ComputeEditorGridBounds(int minTilesX = 20, int minTilesY = 120)
        {
            var painted = ComputeBoundsMax();
            return new Vector2Int(
                Mathf.Max(painted.x, minTilesX),
                Mathf.Max(painted.y, minTilesY));
        }

        public Bounds ComputeEditorWorldBounds(int minTilesX = 20, int minTilesY = 120)
        {
            var grid = ComputeEditorGridBounds(minTilesX, minTilesY);
            float width = grid.x * TileSize;
            float depth = grid.y * TileSize;
            var center = new Vector3(Origin.x + width * 0.5f, 0f, Origin.y + depth * 0.5f);
            return new Bounds(center, new Vector3(width, 1f, depth));
        }

        public float HoleLengthYards()
        {
            var delta = Hole.Basket - Hole.Tee;
            float meters = delta.magnitude;
            return meters / 0.9144f;
        }
    }
}
