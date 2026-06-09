#if UNITY_EDITOR
using DiskGolf.CourseEditor;
using UnityEditor;
using UnityEngine;

namespace DiskGolf.EditorTools.CourseEditor
{
    [InitializeOnLoad]
    public static class CourseEditorOverlay
    {
        const float OverlayY = 0.02f;
        static readonly Color GridColor = new Color(1f, 1f, 1f, 0.15f);
        static readonly Color FillTint = new Color(1f, 1f, 1f, 0.35f);

        static CourseEditorOverlay()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        static void OnSceneGUI(SceneView view)
        {
            var data = CourseEditorState.Data;
            if (data == null)
            {
                return;
            }

            DrawGridAndTiles(data);
            HandleInput(data);
            DrawMarkers(data);
        }

        static void DrawGridAndTiles(HoleData data)
        {
            float tileSize = data.TileSize > 0f ? data.TileSize : HoleData.DefaultTileSize;
            Vector2 origin = data.Origin;
            Vector2Int bounds = data.ComputeBoundsMax();

            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            Handles.color = GridColor;

            for (int x = 0; x <= bounds.x; x++)
            {
                float wx = origin.x + (x * tileSize);
                Handles.DrawLine(
                    new Vector3(wx, 0f, origin.y),
                    new Vector3(wx, 0f, origin.y + (bounds.y * tileSize)));
            }

            for (int y = 0; y <= bounds.y; y++)
            {
                float wz = origin.y + (y * tileSize);
                Handles.DrawLine(
                    new Vector3(origin.x, 0f, wz),
                    new Vector3(origin.x + (bounds.x * tileSize), 0f, wz));
            }

            foreach (var tile in data.SurfaceTiles)
            {
                Vector3 center = TileCenter(data, tile.X, tile.Y);
                float half = tileSize * 0.5f;
                var verts = new[]
                {
                    center + new Vector3(-half, 0f, -half),
                    center + new Vector3(-half, 0f, half),
                    center + new Vector3(half, 0f, half),
                    center + new Vector3(half, 0f, -half)
                };

                Color tint = ColorFor(tile.Type);
                Handles.DrawSolidRectangleWithOutline(verts, tint * FillTint, Color.clear);
            }
        }

        static void HandleInput(HoleData data)
        {
            Event evt = Event.current;
            bool isPaintEvent = evt.type == EventType.MouseDown || evt.type == EventType.MouseDrag;
            if (!isPaintEvent)
            {
                return;
            }

            if (evt.button != 0 || evt.alt)
            {
                return;
            }

            Ray ray = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
            if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance))
            {
                return;
            }

            Vector3 world = ray.GetPoint(distance);
            if (!CourseEditorState.TryWorldToTile(world, out Vector2Int tile))
            {
                return;
            }

            bool changed = false;
            switch (CourseEditorState.ActiveTool)
            {
                case CourseEditorTool.Paint:
                    changed = PaintTile(data, tile.x, tile.y);
                    break;
                case CourseEditorTool.Erase:
                    changed = EraseTile(data, tile.x, tile.y);
                    break;
                case CourseEditorTool.HoleTee:
                    changed = PlaceMarker(data, tile.x, tile.y, true);
                    break;
                case CourseEditorTool.HoleBasket:
                    changed = PlaceMarker(data, tile.x, tile.y, false);
                    break;
            }

            if (!changed)
            {
                return;
            }

            CourseEditorState.IsDirty = true;
            evt.Use();
            SceneView.RepaintAll();
        }

        static bool PaintTile(HoleData data, int x, int y)
        {
            data.TryGetTile(x, y, out SurfaceTileType existing);
            if (existing == CourseEditorState.BrushType)
            {
                return false;
            }

            RecordUndo("Paint Tile");
            data.SetTile(x, y, CourseEditorState.BrushType);
            SyncDataAsset(data);
            return true;
        }

        static bool EraseTile(HoleData data, int x, int y)
        {
            if (!data.TryGetTile(x, y, out _))
            {
                return false;
            }

            RecordUndo("Erase Tile");
            data.ClearTile(x, y);
            SyncDataAsset(data);
            return true;
        }

        static bool PlaceMarker(HoleData data, int x, int y, bool tee)
        {
            Vector2 marker = TileCenter2D(data, x, y);
            if (tee)
            {
                if (data.Hole.Tee == marker)
                {
                    return false;
                }

                RecordUndo("Place Tee");
                data.Hole.Tee = marker;
            }
            else
            {
                if (data.Hole.Basket == marker)
                {
                    return false;
                }

                RecordUndo("Place Basket");
                data.Hole.Basket = marker;
            }

            SyncDataAsset(data);
            return true;
        }

        static void DrawMarkers(HoleData data)
        {
            var hole = data.Hole;

            if (hole.Tee != Vector2.zero)
            {
                Handles.color = new Color(0.35f, 0.6f, 1f, 0.95f);
                Vector3 teePos = new Vector3(hole.Tee.x, 0.1f, hole.Tee.y);
                Handles.SphereHandleCap(0, teePos, Quaternion.identity, 0.3f, EventType.Repaint);
                Handles.Label(teePos + new Vector3(0f, 0.3f, 0f), "TEE");
            }

            if (hole.Basket != Vector2.zero)
            {
                Handles.color = new Color(1f, 0.45f, 0.2f, 0.95f);
                Vector3 basketPos = new Vector3(hole.Basket.x, 0.1f, hole.Basket.y);
                Handles.CubeHandleCap(0, basketPos, Quaternion.identity, 0.3f, EventType.Repaint);
                Handles.Label(basketPos + new Vector3(0f, 0.3f, 0f), "BASKET");
            }
        }

        static Vector3 TileCenter(HoleData data, int x, int y)
        {
            Vector2 center2D = TileCenter2D(data, x, y);
            return new Vector3(center2D.x, OverlayY, center2D.y);
        }

        static Vector2 TileCenter2D(HoleData data, int x, int y)
        {
            float half = data.TileSize * 0.5f;
            return new Vector2(
                data.Origin.x + (x * data.TileSize) + half,
                data.Origin.y + (y * data.TileSize) + half);
        }

        static void RecordUndo(string actionName)
        {
            Object undoTarget = CourseEditorState.DataAsset;
            if (undoTarget != null)
            {
                Undo.RecordObject(undoTarget, actionName);
                return;
            }

            // TODO(Task 8): Replace with CourseEditorWindow scratch asset target if this fallback is hit.
        }

        static void SyncDataAsset(HoleData data)
        {
            if (CourseEditorState.DataAsset == null)
            {
                return;
            }

            CourseEditorState.DataAsset.Data = data;
            EditorUtility.SetDirty(CourseEditorState.DataAsset);
        }

        static Color ColorFor(SurfaceTileType type) => type switch
        {
            SurfaceTileType.Fairway => new Color(0.2f, 0.7f, 0.3f, 1f),
            SurfaceTileType.Rough => new Color(0.55f, 0.4f, 0.2f, 1f),
            SurfaceTileType.Green => new Color(0.1f, 0.85f, 0.35f, 1f),
            SurfaceTileType.Tee => new Color(0.3f, 0.5f, 0.9f, 1f),
            _ => Color.gray
        };
    }
}
#endif
