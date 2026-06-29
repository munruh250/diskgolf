#if UNITY_EDITOR
using System.Collections.Generic;
using DiskGolf.CourseEditor;
using DiskGolf.CourseEditor.Authoring;
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
        static readonly Color WaterPreview = new Color(0.2f, 0.45f, 1f, 0.45f);
        static readonly Color ObPreview = new Color(1f, 0.25f, 0.25f, 0.45f);

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

            DrawSceneInstructions(data);
            DrawGridAndTiles(data);
            DrawElevationContours(data);
            DrawHazardPolygons(data);
            HandleInput(data);
            DrawMarkers(data);
        }

        static void DrawSceneInstructions(HoleData data)
        {
            string toolHint = CourseEditorState.ActiveTool switch
            {
                CourseEditorTool.Paint => $"Paint: click or drag in Scene view ({CourseEditorState.BrushType})",
                CourseEditorTool.Erase => "Erase: click or drag to remove tiles",
                CourseEditorTool.HoleTee => "Hole Tee: click a tile to place the tee",
                CourseEditorTool.HoleBasket => "Hole Basket: click a tile to place the basket",
                CourseEditorTool.Elevate => $"Elevate: drag to raise/lower terrain (strength {CourseEditorState.ElevateStrength:0.##} m)",
                CourseEditorTool.Hazard => $"Hazard ({CourseEditorState.HazardBrushType}): click tile corners, Enter to finish, Esc to cancel",
                _ => string.Empty
            };

            Handles.BeginGUI();
            var rect = new Rect(10f, 10f, 460f, 44f);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, 36f),
                "Course Editor — paint here in Scene view (not in the editor window).\n" + toolHint);
            Handles.EndGUI();
        }

        public static void FocusSceneOnGrid()
        {
            var data = CourseEditorState.Data;
            if (data == null)
                return;

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
                return;

            sceneView.Frame(data.ComputeEditorWorldBounds(), false);
            sceneView.Repaint();
        }

        static void DrawGridAndTiles(HoleData data)
        {
            float tileSize = data.TileSize > 0f ? data.TileSize : HoleData.DefaultTileSize;
            Vector2 origin = data.Origin;
            Vector2Int bounds = data.ComputeEditorGridBounds();

            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            Handles.color = GridColor;

            for (int x = 0; x <= bounds.x; x++)
            {
                float wx = origin.x + (x * tileSize);
                Handles.DrawLine(
                    new Vector3(wx, OverlayY, origin.y),
                    new Vector3(wx, OverlayY, origin.y + (bounds.y * tileSize)));
            }

            for (int y = 0; y <= bounds.y; y++)
            {
                float wz = origin.y + (y * tileSize);
                Handles.DrawLine(
                    new Vector3(origin.x, OverlayY, wz),
                    new Vector3(origin.x + (bounds.x * tileSize), OverlayY, wz));
            }

            foreach (var tile in data.SurfaceTiles)
            {
                Vector3 center = TileCenter(data, tile.X, tile.Y);
                float half = tileSize * 0.5f;
                float y = HeightGridSampler.SampleWorldY(data, center.x, center.z) + OverlayY;
                center.y = y;
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

        static void DrawElevationContours(HoleData data)
        {
            if (CourseEditorState.ActiveTool != CourseEditorTool.Elevate)
            {
                return;
            }

            if (data.Elevation == null || data.Elevation.Width <= 1 || data.Elevation.Height <= 1)
            {
                return;
            }

            const float step = 1f;
            float maxHeight = 0f;
            foreach (float height in data.Elevation.Heights)
                maxHeight = Mathf.Max(maxHeight, height);

            float tileSize = data.TileSize;
            for (float level = step; level <= maxHeight + step; level += step)
            {
                DrawContourLevel(data, level, tileSize);
            }
        }

        static void DrawContourLevel(HoleData data, float level, float tileSize)
        {
            Handles.color = new Color(1f, 0.95f, 0.4f, 0.35f);
            int width = data.Elevation.Width;
            int height = data.Elevation.Height;

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    float h00 = data.Elevation.Get(x, y);
                    float h10 = data.Elevation.Get(x + 1, y);
                    float h01 = data.Elevation.Get(x, y + 1);
                    float h11 = data.Elevation.Get(x + 1, y + 1);

                    DrawContourCellEdges(data, x, y, tileSize, level, h00, h10, h01, h11);
                }
            }
        }

        static void DrawContourCellEdges(HoleData data, int x, int y, float tileSize, float level, float h00, float h10, float h01, float h11)
        {
            Vector3 c00 = CourseEditorState.TileCornerWorld(data, x, y);
            Vector3 c10 = CourseEditorState.TileCornerWorld(data, x + 1, y);
            Vector3 c01 = CourseEditorState.TileCornerWorld(data, x, y + 1);
            Vector3 c11 = CourseEditorState.TileCornerWorld(data, x + 1, y + 1);

            TryDrawContourEdge(c00, c10, h00, h10, level);
            TryDrawContourEdge(c10, c11, h10, h11, level);
            TryDrawContourEdge(c11, c01, h11, h01, level);
            TryDrawContourEdge(c01, c00, h01, h00, level);
        }

        static void TryDrawContourEdge(Vector3 a, Vector3 b, float ha, float hb, float level)
        {
            bool aAbove = ha >= level;
            bool bAbove = hb >= level;
            if (aAbove == bAbove)
            {
                return;
            }

            float t = Mathf.InverseLerp(ha, hb, level);
            Vector3 point = Vector3.Lerp(a, b, t);
            Handles.DrawLine(point + Vector3.up * 0.05f, point + Vector3.up * 0.25f);
        }

        static void DrawHazardPolygons(HoleData data)
        {
            if (data.Hazards != null)
            {
                foreach (var hazard in data.Hazards)
                {
                    DrawHazardPolygon(data, hazard.Vertices, hazard.Type, 1f);
                }
            }

            if (CourseEditorState.HazardDraftVertices.Count >= 2)
            {
                DrawHazardPolygon(data, CourseEditorState.HazardDraftVertices, CourseEditorState.HazardBrushType, 0.75f);
            }

            foreach (var vertex in CourseEditorState.HazardDraftVertices)
            {
                Vector3 corner = CourseEditorState.TileCornerWorld(data, vertex.x, vertex.y);
                Handles.color = Color.white;
                Handles.SphereHandleCap(0, corner + Vector3.up * 0.1f, Quaternion.identity, 0.15f, EventType.Repaint);
            }
        }

        static void DrawHazardPolygon(HoleData data, IReadOnlyList<Vector2Int> vertices, HazardType type, float alpha)
        {
            if (vertices == null || vertices.Count < 2)
            {
                return;
            }

            Color fill = type == HazardType.OB ? ObPreview : WaterPreview;
            fill.a *= alpha;

            if (vertices.Count >= 3 && type == HazardType.Water)
            {
                DrawTerrainHazardFill(data, new HazardPolygon("preview", type, vertices), fill);
                return;
            }

            Handles.color = fill;

            var world = new Vector3[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                world[i] = CourseEditorState.TileCornerWorld(data, vertices[i].x, vertices[i].y) + Vector3.up * 0.08f;
            }

            Handles.DrawAAConvexPolygon(world);
            Handles.color = new Color(fill.r, fill.g, fill.b, 1f);
            for (int i = 0; i < world.Length; i++)
            {
                Handles.DrawLine(world[i], world[(i + 1) % world.Length]);
            }
        }

        static readonly List<Vector2Int> HazardPreviewTiles = new();

        static void DrawTerrainHazardFill(HoleData data, HazardPolygon hazard, Color fill)
        {
            HazardGeometry.CollectTilesInside(data, hazard, HazardPreviewTiles);
            Handles.color = fill;

            float tileSize = data.TileSize;
            float half = tileSize * 0.5f;
            const float lift = 0.1f;

            foreach (var tile in HazardPreviewTiles)
            {
                float centerX = data.Origin.x + tile.x * tileSize + half;
                float centerZ = data.Origin.y + tile.y * tileSize + half;
                var corners = HeightGridSampler.TileCornerHeights(data, tile.x, tile.y);
                var verts = new[]
                {
                    new Vector3(centerX - half, corners[0] + lift, centerZ - half),
                    new Vector3(centerX - half, corners[1] + lift, centerZ + half),
                    new Vector3(centerX + half, corners[2] + lift, centerZ + half),
                    new Vector3(centerX + half, corners[3] + lift, centerZ - half),
                };

                Handles.DrawAAConvexPolygon(verts);
            }
        }

        static void HandleInput(HoleData data)
        {
            Event evt = Event.current;
            int controlId = GUIUtility.GetControlID("CourseEditorPaint".GetHashCode(), FocusType.Passive);
            HandleUtility.AddDefaultControl(controlId);

            if (evt.type == EventType.KeyDown)
            {
                if (CourseEditorState.ActiveTool == CourseEditorTool.Hazard)
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        if (CommitHazardDraft(data))
                        {
                            evt.Use();
                            SceneView.RepaintAll();
                        }

                        return;
                    }

                    if (evt.keyCode == KeyCode.Escape)
                    {
                        if (TryCancelHazardDraft(data))
                        {
                            evt.Use();
                            SceneView.RepaintAll();
                        }

                        return;
                    }
                }
            }

            if (evt.type == EventType.Layout)
                return;

            bool isPaintEvent = evt.type == EventType.MouseDown || evt.type == EventType.MouseDrag;
            if (!isPaintEvent)
                return;

            if (evt.button != 0 || evt.alt)
                return;

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
                case CourseEditorTool.Elevate:
                    changed = ElevateAt(data, tile.x, tile.y, evt.shift);
                    break;
                case CourseEditorTool.Hazard:
                    if (evt.type == EventType.MouseDown)
                        changed = AppendHazardVertex(data, tile.x, tile.y);
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

        static CourseAuthoringState CreateAuthoringState() => new()
        {
            ActiveTool = (CourseAuthoringTool)CourseEditorState.ActiveTool,
            BrushType = CourseEditorState.BrushType,
            ElevateRadius = CourseEditorState.ElevateRadius,
            ElevateStrength = CourseEditorState.ElevateStrength,
            ElevateSmooth = CourseEditorState.ElevateSmooth,
            HazardBrushType = CourseEditorState.HazardBrushType,
            HazardDraftVertices = CourseEditorState.HazardDraftVertices,
        };

        static bool PaintTile(HoleData data, int x, int y)
        {
            var state = CreateAuthoringState();
            data.TryGetTile(x, y, out SurfaceTileType existing);
            if (existing != state.BrushType)
            {
                RecordUndo("Paint Tile");
            }

            if (!CourseAuthoringOperations.TryPaintTile(data, state, x, y))
            {
                return false;
            }

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
            if (!CourseAuthoringOperations.TryEraseTile(data, CreateAuthoringState(), x, y))
            {
                return false;
            }

            SyncDataAsset(data);
            return true;
        }

        static bool PlaceMarker(HoleData data, int x, int y, bool tee)
        {
            var state = CreateAuthoringState();
            Vector3 center = CourseAuthoringGrid.TileCenterWorld(data, x, y);
            Vector2 marker = new Vector2(center.x, center.z);
            if (tee && data.Hole.Tee == marker || !tee && data.Hole.Basket == marker)
            {
                return false;
            }

            RecordUndo(tee ? "Place Tee" : "Place Basket");
            bool changed = tee
                ? CourseAuthoringOperations.TryPlaceTee(data, state, x, y)
                : CourseAuthoringOperations.TryPlaceBasket(data, state, x, y);
            if (!changed)
            {
                return false;
            }

            SyncDataAsset(data);
            return true;
        }

        static bool ElevateAt(HoleData data, int centerX, int centerY, bool subtract)
        {
            RecordUndo("Elevate Terrain");
            bool changed = CourseAuthoringOperations.TryElevateAt(
                data, CreateAuthoringState(), centerX, centerY, subtract);
            if (changed)
            {
                SyncDataAsset(data);
            }

            return changed;
        }

        static bool AppendHazardVertex(HoleData data, int tileX, int tileY)
        {
            var state = CreateAuthoringState();
            var corner = new Vector2Int(tileX, tileY);
            if (state.HazardDraftVertices.Count > 0
                && state.HazardDraftVertices[state.HazardDraftVertices.Count - 1] == corner)
            {
                return false;
            }

            RecordUndo("Add Hazard Vertex");
            if (!CourseAuthoringOperations.TryAppendHazardVertex(data, state, tileX, tileY))
            {
                return false;
            }

            SyncDataAsset(data);
            return true;
        }

        static bool CommitHazardDraft(HoleData data)
        {
            var state = CreateAuthoringState();
            if (state.HazardDraftVertices.Count < 3)
            {
                return false;
            }

            RecordUndo("Add Hazard Polygon");
            if (!CourseAuthoringOperations.TryCommitHazardPolygon(data, state))
            {
                return false;
            }

            SyncDataAsset(data);
            CourseEditorState.IsDirty = true;
            return true;
        }

        static bool TryCancelHazardDraft(HoleData data)
        {
            if (!CourseAuthoringOperations.TryCancelHazardDraft(data, CreateAuthoringState()))
            {
                return false;
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
                float teeY = HeightGridSampler.SampleWorldY(data, hole.Tee.x, hole.Tee.y);
                Vector3 teePos = new Vector3(hole.Tee.x, teeY + 0.1f, hole.Tee.y);
                Handles.SphereHandleCap(0, teePos, Quaternion.identity, 0.3f, EventType.Repaint);
                Handles.Label(teePos + new Vector3(0f, 0.3f, 0f), "TEE");
            }

            if (hole.Basket != Vector2.zero)
            {
                Handles.color = new Color(1f, 0.45f, 0.2f, 0.95f);
                float basketY = HeightGridSampler.SampleWorldY(data, hole.Basket.x, hole.Basket.y);
                Vector3 basketPos = new Vector3(hole.Basket.x, basketY + 0.1f, hole.Basket.y);
                Handles.CubeHandleCap(0, basketPos, Quaternion.identity, 0.3f, EventType.Repaint);
                Handles.Label(basketPos + new Vector3(0f, 0.3f, 0f), "BASKET");
            }
        }

        static Vector3 TileCenter(HoleData data, int x, int y)
        {
            Vector3 center = CourseAuthoringGrid.TileCenterWorld(data, x, y);
            center.y += OverlayY;
            return center;
        }

        static void RecordUndo(string actionName)
        {
            Object undoTarget = CourseEditorState.DataAsset;
            if (undoTarget != null)
            {
                Undo.RecordObject(undoTarget, actionName);
                return;
            }
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
