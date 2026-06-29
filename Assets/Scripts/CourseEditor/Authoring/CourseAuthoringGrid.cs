using UnityEngine;

namespace DiskGolf.CourseEditor.Authoring
{
    public static class CourseAuthoringGrid
    {
        public static bool TryWorldToTile(HoleData data, Vector3 world, out Vector2Int tile)
        {
            tile = default;
            if (data == null || data.TileSize <= 0f)
            {
                return false;
            }

            int x = Mathf.FloorToInt((world.x - data.Origin.x) / data.TileSize);
            int y = Mathf.FloorToInt((world.z - data.Origin.y) / data.TileSize);
            if (x < 0 || y < 0)
            {
                return false;
            }

            tile = new Vector2Int(x, y);
            return true;
        }

        public static Vector3 TileCenterWorld(HoleData data, int x, int y)
        {
            float half = data.TileSize * 0.5f;
            float centerX = data.Origin.x + (x * data.TileSize) + half;
            float centerZ = data.Origin.y + (y * data.TileSize) + half;
            return new Vector3(
                centerX,
                HeightGridSampler.SampleTileSurfaceWorldY(data, centerX, centerZ),
                centerZ);
        }

        public static Vector3 TileCornerWorld(HoleData data, int tileX, int tileY)
        {
            float cornerX = data.Origin.x + tileX * data.TileSize;
            float cornerZ = data.Origin.y + tileY * data.TileSize;
            return new Vector3(
                cornerX,
                HeightGridSampler.SampleWorldY(data, cornerX, cornerZ),
                cornerZ);
        }
    }
}
