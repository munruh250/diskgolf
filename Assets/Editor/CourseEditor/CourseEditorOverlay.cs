#if UNITY_EDITOR
using System.Collections.Generic;
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
            if (data.Elevation == null || data.Elevation.Width <= 1 || data.Elevation.Height <= 1)
            {
                return;
            }

            Handles.color = new Color(1f, 0.95f, 0.4f, 0.85f);
            float tileSize = data.TileSize;

            for (int y = 0; y < data.Elevation.Height; y++)
            {
                for (int x = 0; x < data.Elevation.Width; x++)
                {
                    float height = data.Elevation.Get(x, y);
                    if (Mathf.Abs(height) < 0.01f)
                    {
                        continue;
                    }

                    Vector3 corner = CourseEditorState.TileCornerWorld(data, x, y);
                    Handles.Label(corner + Vector3.up * 0.15f, height.ToString("0.0"));
                }
            }

            const float step = 1f;
            float maxHeight = 0f;
            foreach (float height in data.Elevation.Heights)
                maxHeight = Mathf.Max(maxHeight, height);

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
                        CourseEditorState.ClearHazardDraft();
                        evt.Use();
                        SceneView.RepaintAll();
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

        static bool PaintTile(HoleData data, int x, int y)
        {
            data.TryGetTile(x, y, out SurfaceTileType existing);
            if (existing == CourseEditorState.BrushType)
            {
                EnsureElevationGrid(data);
                return false;
            }

            RecordUndo("Paint Tile");
            data.SetTile(x, y, CourseEditorState.BrushType);
            EnsureElevationGrid(data);
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

        static bool ElevateAt(HoleData data, int centerX, int centerY, bool subtract)
        {
            EnsureElevationGrid(data);
            RecordUndo("Elevate Terrain");

            float delta = CourseEditorState.ElevateStrength * (subtract ? -1f : 1f);
            int radius = Mathf.Clamp(CourseEditorState.ElevateRadius, 1, 5);
            bool changed = false;

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy > radius * radius)
                    {
                        continue;
                    }

                    int x = centerX + dx;
                    int y = centerY + dy;
                    if (x < 0 || y < 0 || x >= data.Elevation.Width || y >= data.Elevation.Height)
                    {
                        continue;
                    }

                    if (CourseEditorState.ElevateSmooth)
                    {
                        changed |= SmoothCell(data, x, y);
                    }
                    else
                    {
                        float next = data.Elevation.Get(x, y) + delta;
                        data.Elevation.Set(x, y, next);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                SyncDataAsset(data);
            }

            return changed;
        }

        static bool SmoothCell(HoleData data, int x, int y)
        {
            float sum = 0f;
            int count = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int sx = x + dx;
                    int sy = y + dy;
                    if (sx < 0 || sy < 0 || sx >= data.Elevation.Width || sy >= data.Elevation.Height)
                    {
                        continue;
                    }

                    sum += data.Elevation.Get(sx, sy);
                    count++;
                }
            }

            if (count == 0)
            {
                return false;
            }

            float average = sum / count;
            if (Mathf.Approximately(data.Elevation.Get(x, y), average))
            {
                return false;
            }

            data.Elevation.Set(x, y, average);
            return true;
        }

        static bool AppendHazardVertex(HoleData data, int tileX, int tileY)
        {
            var corner = new Vector2Int(tileX, tileY);
            if (CourseEditorState.HazardDraftVertices.Count > 0
                && CourseEditorState.HazardDraftVertices[CourseEditorState.HazardDraftVertices.Count - 1] == corner)
            {
                return false;
            }

            RecordUndo("Add Hazard Vertex");
            CourseEditorState.HazardDraftVertices.Add(corner);
            SyncDataAsset(data);
            return true;
        }

        static bool CommitHazardDraft(HoleData data)
        {
            if (CourseEditorState.HazardDraftVertices.Count < 3)
            {
                return false;
            }

            RecordUndo("Add Hazard Polygon");
            string prefix = CourseEditorState.HazardBrushType == HazardType.OB ? "ob" : "water";
            int index = (data.Hazards?.Count ?? 0) + 1;
            data.Hazards.Add(new HazardPolygon(
                $"{prefix}_{index}",
                CourseEditorState.HazardBrushType,
                new List<Vector2Int>(CourseEditorState.HazardDraftVertices)));
            CourseEditorState.ClearHazardDraft();
            SyncDataAsset(data);
            CourseEditorState.IsDirty = true;
            return true;
        }

        static void EnsureElevationGrid(HoleData data)
        {
            var size = HeightGridSampler.GridSizeForHole(data);
            if (data.Elevation == null)
            {
                data.Elevation = new ElevationGrid(size.x, size.y);
                return;
            }

            data.Elevation.EnsureSize(size.x, size.y);
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
            Vector2 center2D = TileCenter2D(data, x, y);
            float y = HeightGridSampler.SampleWorldY(data, center2D.x, center2D.y);
            return new Vector3(center2D.x, y + OverlayY, center2D.y);
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
