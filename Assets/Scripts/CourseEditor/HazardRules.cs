using DiskGolf.CourseEditor;
using DiskGolf.CourseEditor.Authoring;
using DiskGolf.Flight;
using DiskGolf.Gameplay;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    public static class HazardRules
    {
        public static bool IsPlayableSurface(SurfaceTileType type) =>
            type is SurfaceTileType.Tee or SurfaceTileType.Fairway or SurfaceTileType.Green;

        public static bool IsRoughSurface(SurfaceTileType type) => type == SurfaceTileType.Rough;

        public static bool TryClassifyHazard(HoleData data, Vector3 worldPos, out LieType lie)
        {
            lie = default;
            if (data == null)
            {
                return false;
            }

            if (CourseAuthoringGrid.TryWorldToTile(data, worldPos, out Vector2Int tile)
                && data.TryGetHazardTile(tile.x, tile.y, out var painted))
            {
                lie = painted == HazardType.OB ? LieType.OB : LieType.Water;
                return true;
            }

            if (data.Hazards == null)
            {
                return false;
            }

            foreach (var hazard in data.Hazards)
            {
                if (!HazardGeometry.ContainsWorldPoint(data, hazard, worldPos.x, worldPos.z))
                {
                    continue;
                }

                lie = hazard.Type == HazardType.OB ? LieType.OB : LieType.Water;
                return true;
            }

            return false;
        }

        public static Vector3 ResolveDrop(HoleData data, LieType hazard, Vector3 discPos)
        {
            if (data == null)
            {
                return discPos;
            }

            Vector3 dropPoint = hazard == LieType.OB
                ? FindNearestRoughWorld(data, discPos)
                : FindNearestPlayableWorld(data, discPos);

            float groundY = HeightGridSampler.SampleWorldY(data, dropPoint.x, dropPoint.z);
            return DiscLieGround.SnapLie(new Vector3(dropPoint.x, groundY, dropPoint.z), groundY);
        }

        static Vector3 FindNearestPlayableWorld(HoleData data, Vector3 from)
        {
            float bestDistSq = float.PositiveInfinity;
            Vector3 best = new Vector3(data.Hole.Tee.x, 0f, data.Hole.Tee.y);

            foreach (var tile in data.SurfaceTiles)
            {
                if (!IsPlayableSurface(tile.Type))
                {
                    continue;
                }

                float centerX = data.Origin.x + tile.X * data.TileSize + data.TileSize * 0.5f;
                float centerZ = data.Origin.y + tile.Y * data.TileSize + data.TileSize * 0.5f;
                float dx = centerX - from.x;
                float dz = centerZ - from.z;
                float distSq = dx * dx + dz * dz;
                if (distSq >= bestDistSq)
                {
                    continue;
                }

                bestDistSq = distSq;
                best = new Vector3(centerX, 0f, centerZ);
            }

            return best;
        }

        static Vector3 FindNearestRoughWorld(HoleData data, Vector3 from)
        {
            float bestDistSq = float.PositiveInfinity;
            Vector3 best = new Vector3(from.x, 0f, from.z);
            bool foundRough = false;

            foreach (var tile in data.SurfaceTiles)
            {
                if (!IsRoughSurface(tile.Type))
                {
                    continue;
                }

                float centerX = data.Origin.x + tile.X * data.TileSize + data.TileSize * 0.5f;
                float centerZ = data.Origin.y + tile.Y * data.TileSize + data.TileSize * 0.5f;
                float dx = centerX - from.x;
                float dz = centerZ - from.z;
                float distSq = dx * dx + dz * dz;
                if (distSq >= bestDistSq)
                {
                    continue;
                }

                bestDistSq = distSq;
                best = new Vector3(centerX, 0f, centerZ);
                foundRough = true;
            }

            return foundRough ? best : FindNearestPlayableWorld(data, from);
        }
    }
}
