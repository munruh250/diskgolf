using System.Collections.Generic;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    public static class HazardGeometry
    {
        public static Vector3 TileCornerToWorld(HoleData data, Vector2Int tileCorner)
        {
            float worldX = data.Origin.x + tileCorner.x * data.TileSize;
            float worldZ = data.Origin.y + tileCorner.y * data.TileSize;
            return new Vector3(worldX, HeightGridSampler.SampleWorldY(data, worldX, worldZ), worldZ);
        }

        public static bool ContainsWorldPoint(HoleData data, HazardPolygon hazard, float worldX, float worldZ)
        {
            if (data == null || hazard?.Vertices == null || hazard.Vertices.Count < 3)
            {
                return false;
            }

            var polygon = new List<Vector2>(hazard.Vertices.Count);
            foreach (var vertex in hazard.Vertices)
            {
                polygon.Add(new Vector2(
                    data.Origin.x + vertex.x * data.TileSize,
                    data.Origin.y + vertex.y * data.TileSize));
            }

            return PointInPolygon(new Vector2(worldX, worldZ), polygon);
        }

        public static void ComputeWorldBounds(HoleData data, HazardPolygon hazard, out Bounds bounds)
        {
            bounds = default;
            if (data == null || hazard?.Vertices == null || hazard.Vertices.Count == 0)
            {
                return;
            }

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minZ = float.PositiveInfinity;
            float maxZ = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;

            foreach (var vertex in hazard.Vertices)
            {
                float worldX = data.Origin.x + vertex.x * data.TileSize;
                float worldZ = data.Origin.y + vertex.y * data.TileSize;
                float worldY = HeightGridSampler.SampleWorldY(data, worldX, worldZ);

                minX = Mathf.Min(minX, worldX);
                maxX = Mathf.Max(maxX, worldX);
                minZ = Mathf.Min(minZ, worldZ);
                maxZ = Mathf.Max(maxZ, worldZ);
                minY = Mathf.Min(minY, worldY);
                maxY = Mathf.Max(maxY, worldY);
            }

            const float verticalPadding = 2f;
            var center = new Vector3(
                (minX + maxX) * 0.5f,
                (minY + maxY) * 0.5f + verticalPadding * 0.5f,
                (minZ + maxZ) * 0.5f);
            var size = new Vector3(
                Mathf.Max(0.5f, maxX - minX),
                Mathf.Max(1f, maxY - minY + verticalPadding),
                Mathf.Max(0.5f, maxZ - minZ));
            bounds = new Bounds(center, size);
        }

        static bool PointInPolygon(Vector2 point, IReadOnlyList<Vector2> polygon)
        {
            bool inside = false;
            int count = polygon.Count;

            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];

                if (((a.y > point.y) != (b.y > point.y))
                    && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y + 1e-6f) + a.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
