using DiskGolf.Flight;
using DiskGolf.Gameplay;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    public static class HazardRules
    {
        public static bool IsPlayableSurface(SurfaceTileType type) =>
            type is SurfaceTileType.Tee or SurfaceTileType.Fairway or SurfaceTileType.Green;

        public static bool TryClassifyHazard(HoleData data, Vector3 worldPos, out LieType lie)
        {
            lie = default;
            if (data?.Hazards == null)
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

            if (hazard == LieType.OB)
            {
                float teeY = HeightGridSampler.SampleWorldY(data, data.Hole.Tee.x, data.Hole.Tee.y);
                return DiscLieGround.SnapLie(new Vector3(data.Hole.Tee.x, teeY, data.Hole.Tee.y), teeY);
            }

            Vector3 playable = FindNearestPlayableWorld(data, discPos);
            float groundY = HeightGridSampler.SampleWorldY(data, playable.x, playable.z);
            return DiscLieGround.SnapLie(new Vector3(playable.x, groundY, playable.z), groundY);
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
    }
}
