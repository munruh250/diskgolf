#if UNITY_EDITOR
using DiskGolf.CourseEditor;
using UnityEngine;

namespace DiskGolf.EditorTools.CourseEditor
{
    public enum CourseEditorTool
    {
        Paint,
        Erase,
        HoleTee,
        HoleBasket
    }

    /// <summary>
    /// Shared editor state for the course editor window and scene overlay.
    /// </summary>
    public static class CourseEditorState
    {
        public static HoleDataAsset DataAsset { get; set; }
        public static HoleData Data { get; private set; } = new();
        public static ThemePack Theme { get; set; }
        public static CourseEditorTool ActiveTool { get; set; } = CourseEditorTool.Paint;
        public static SurfaceTileType BrushType { get; set; } = SurfaceTileType.Fairway;
        public static bool IsDirty { get; set; }

        public static void SetData(HoleData data)
        {
            Data = data ?? new HoleData();

            if (DataAsset != null)
            {
                DataAsset.Data = Data;
            }
        }

        public static void SetDataAsset(HoleDataAsset asset)
        {
            DataAsset = asset;
            SetData(asset != null ? asset.Data : null);
        }

        public static bool TryWorldToTile(Vector3 world, out Vector2Int tile)
        {
            tile = default;
            if (Data == null || Data.TileSize <= 0f)
            {
                return false;
            }

            int x = Mathf.FloorToInt((world.x - Data.Origin.x) / Data.TileSize);
            int y = Mathf.FloorToInt((world.z - Data.Origin.y) / Data.TileSize);
            if (x < 0 || y < 0)
            {
                return false;
            }

            tile = new Vector2Int(x, y);
            return true;
        }
    }
}
#endif
