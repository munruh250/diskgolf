#if UNITY_EDITOR
using DiskGolf.CourseEditor;
using DiskGolf.CourseEditor.Authoring;
using UnityEngine;

namespace DiskGolf.EditorTools.CourseEditor
{
    public enum CourseEditorTool
    {
        Paint,
        Erase,
        HoleTee,
        HoleBasket,
        Elevate,
        Hazard
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

        public static int ElevateRadius { get; set; } = 2;
        public static float ElevateStrength { get; set; } = 0.25f;
        public static bool ElevateSmooth { get; set; }

        public static HazardType HazardBrushType { get; set; } = HazardType.Water;
        public static System.Collections.Generic.List<Vector2Int> HazardDraftVertices { get; } = new();

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

        public static void ClearHazardDraft() => HazardDraftVertices.Clear();

        public static bool TryWorldToTile(Vector3 world, out Vector2Int tile)
            => CourseAuthoringGrid.TryWorldToTile(Data, world, out tile);

        public static Vector3 TileCornerWorld(HoleData data, int tileX, int tileY)
            => CourseAuthoringGrid.TileCornerWorld(data, tileX, tileY);
    }
}
#endif
