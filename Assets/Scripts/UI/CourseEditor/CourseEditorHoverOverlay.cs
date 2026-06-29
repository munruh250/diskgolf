using DiskGolf.CourseEditor;
using DiskGolf.CourseEditor.Authoring;
using DiskGolf.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DiskGolf.UI.CourseEditor
{
    /// <summary>World-space tile/object hover outlines for paint and erase tools.</summary>
    public sealed class CourseEditorHoverOverlay : MonoBehaviour
    {
        const float GroundLift = 0.08f;
        const float LineWidth = 0.08f;
        const int TileLoopPoints = 5;

        static readonly Color PaintFairwayColor = new(0.35f, 0.82f, 0.42f, 0.95f);
        static readonly Color PaintRoughColor = new(0.45f, 0.62f, 0.32f, 0.95f);
        static readonly Color PaintGreenColor = new(0.2f, 0.9f, 0.45f, 0.95f);
        static readonly Color PaintWaterColor = new(0.28f, 0.62f, 0.95f, 0.95f);
        static readonly Color PaintObColor = new(0.95f, 0.35f, 0.3f, 0.95f);
        static readonly Color EraseTileColor = new(0.95f, 0.45f, 0.28f, 0.95f);
        static readonly Color EraseObjectColor = new(0.98f, 0.82f, 0.2f, 0.98f);
        static readonly Color FoliageEraseTint = new(1f, 0.92f, 0.2f, 1f);

        CourseEditorSession session;
        UnityEngine.Camera authoringCamera;
        LineRenderer tileOutline;
        LineRenderer objectOutline;
        SpriteRenderer highlightedFoliageRenderer;
        Color highlightedFoliageOriginalColor = Color.white;

        public static CourseEditorHoverOverlay Ensure(UnityEngine.Camera camera)
        {
            var existing = FindFirstObjectByType<CourseEditorHoverOverlay>();
            if (existing != null)
            {
                existing.authoringCamera = camera;
                return existing;
            }

            var go = new GameObject("CourseEditorHoverOverlay");
            var overlay = go.AddComponent<CourseEditorHoverOverlay>();
            overlay.authoringCamera = camera;
            return overlay;
        }

        void Awake()
        {
            session = CourseEditorSession.Instance;
            tileOutline = CreateOutline("TileHoverOutline", TileLoopPoints, true);
            objectOutline = CreateOutline("ObjectHoverOutline", 5, true);
            tileOutline.transform.SetParent(transform, false);
            objectOutline.transform.SetParent(transform, false);
            SetOutlineVisible(tileOutline, false);
            SetOutlineVisible(objectOutline, false);
        }

        void LateUpdate()
        {
            if (session?.Hole == null
                || session.Mode != CourseEditorSessionMode.Editing
                || authoringCamera == null
                || IsPointerOverUi())
            {
                HideAll();
                return;
            }

            var tool = session.Authoring.ActiveTool;
            bool showTile = tool == CourseAuthoringTool.Paint
                || tool == CourseAuthoringTool.Hazard
                || tool == CourseAuthoringTool.Erase;

            if (!showTile || !CourseEditorPointerPick.TryPick(session.Hole, authoringCamera, out var pick))
            {
                HideAll();
                return;
            }

            if (pick.Kind == CourseEditorPointerPick.PickKind.Tile
                || pick.Kind == CourseEditorPointerPick.PickKind.Tee
                || pick.Kind == CourseEditorPointerPick.PickKind.Basket)
            {
                Vector2Int tile = pick.Tile;
                if (pick.Kind != CourseEditorPointerPick.PickKind.Tile
                    && !CourseAuthoringGrid.TryWorldToTile(session.Hole, pick.WorldPoint, out tile))
                {
                    HideAll();
                    return;
                }

                SetTileOutline(session.Hole, tile, GetTileOutlineColor(tool));
                SetOutlineVisible(tileOutline, true);
            }
            else
            {
                SetOutlineVisible(tileOutline, false);
            }

            if (tool == CourseAuthoringTool.Erase && pick.Kind == CourseEditorPointerPick.PickKind.Foliage)
            {
                SetFoliageEraseHighlight(pick.FoliageIndex);
                SetOutlineVisible(objectOutline, false);
            }
            else
            {
                ClearFoliageEraseHighlight();
                if (tool == CourseAuthoringTool.Erase
                    && (pick.Kind == CourseEditorPointerPick.PickKind.Tee
                        || pick.Kind == CourseEditorPointerPick.PickKind.Basket))
                {
                    SetObjectOutline(pick);
                    SetOutlineVisible(objectOutline, true);
                }
                else
                {
                    SetOutlineVisible(objectOutline, false);
                }
            }
        }

        Color GetTileOutlineColor(CourseAuthoringTool tool)
        {
            if (tool == CourseAuthoringTool.Erase)
                return EraseTileColor;

            if (tool == CourseAuthoringTool.Hazard)
            {
                return session.Authoring.HazardBrushType == HazardType.OB
                    ? PaintObColor
                    : PaintWaterColor;
            }

            return session.Authoring.BrushType switch
            {
                SurfaceTileType.Rough => PaintRoughColor,
                SurfaceTileType.Green => PaintGreenColor,
                _ => PaintFairwayColor
            };
        }

        void SetTileOutline(HoleData data, Vector2Int tile, Color color)
        {
            var corner00 = Lift(CourseAuthoringGrid.TileCornerWorld(data, tile.x, tile.y));
            var corner10 = Lift(CourseAuthoringGrid.TileCornerWorld(data, tile.x + 1, tile.y));
            var corner11 = Lift(CourseAuthoringGrid.TileCornerWorld(data, tile.x + 1, tile.y + 1));
            var corner01 = Lift(CourseAuthoringGrid.TileCornerWorld(data, tile.x, tile.y + 1));

            tileOutline.startColor = tileOutline.endColor = color;
            tileOutline.SetPosition(0, corner00);
            tileOutline.SetPosition(1, corner10);
            tileOutline.SetPosition(2, corner11);
            tileOutline.SetPosition(3, corner01);
            tileOutline.SetPosition(4, corner00);
        }

        void SetObjectOutline(CourseEditorPointerPick.PickResult pick)
        {
            objectOutline.startColor = objectOutline.endColor = EraseObjectColor;
            var bounds = new Bounds(pick.WorldPoint, new Vector3(session.Hole.TileSize * 0.75f, 0.2f, session.Hole.TileSize * 0.75f));

            float y = bounds.center.y + GroundLift;
            var min = bounds.min;
            var max = bounds.max;
            objectOutline.SetPosition(0, new Vector3(min.x, y, min.z));
            objectOutline.SetPosition(1, new Vector3(max.x, y, min.z));
            objectOutline.SetPosition(2, new Vector3(max.x, y, max.z));
            objectOutline.SetPosition(3, new Vector3(min.x, y, max.z));
            objectOutline.SetPosition(4, new Vector3(min.x, y, min.z));
        }

        void SetFoliageEraseHighlight(int foliageIndex)
        {
            var renderer = FindFoliageRenderer(foliageIndex);
            if (renderer == null)
            {
                ClearFoliageEraseHighlight();
                return;
            }

            if (highlightedFoliageRenderer != renderer)
            {
                ClearFoliageEraseHighlight();
                highlightedFoliageRenderer = renderer;
                highlightedFoliageOriginalColor = renderer.color;
            }

            highlightedFoliageRenderer.color = FoliageEraseTint;
        }

        void ClearFoliageEraseHighlight()
        {
            if (highlightedFoliageRenderer != null)
            {
                highlightedFoliageRenderer.color = highlightedFoliageOriginalColor;
                highlightedFoliageRenderer = null;
            }
        }

        SpriteRenderer FindFoliageRenderer(int foliageIndex)
        {
            if (foliageIndex < 0 || foliageIndex >= session.Hole.Placements.Count)
                return null;

            var placement = session.Hole.Placements[foliageIndex];
            var host = session.CurrentBuiltCourse;
            var foliageRoot = host != null ? host.TreesRoot : null;
            if (foliageRoot == null)
                return null;

            foreach (Transform child in foliageRoot)
            {
                float dx = child.position.x - placement.X;
                float dz = child.position.z - placement.Z;
                if (dx * dx + dz * dz > 0.25f)
                    continue;

                return child.GetComponentInChildren<SpriteRenderer>();
            }

            return null;
        }

        Vector3 Lift(Vector3 point) => new(point.x, point.y + GroundLift, point.z);

        void HideAll()
        {
            ClearFoliageEraseHighlight();
            SetOutlineVisible(tileOutline, false);
            SetOutlineVisible(objectOutline, false);
        }

        static void SetOutlineVisible(LineRenderer line, bool visible)
        {
            if (line != null)
                line.enabled = visible;
        }

        static LineRenderer CreateOutline(string name, int pointCount, bool loop)
        {
            var go = new GameObject(name, typeof(LineRenderer));
            var line = go.GetComponent<LineRenderer>();
            line.loop = loop;
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.widthMultiplier = LineWidth;
            line.positionCount = pointCount;
            line.material = new Material(Shader.Find("Sprites/Default"));
            return line;
        }

        static bool IsPointerOverUi() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        void OnDestroy()
        {
            ClearFoliageEraseHighlight();
            if (tileOutline != null)
                Destroy(tileOutline.gameObject);
            if (objectOutline != null)
                Destroy(objectOutline.gameObject);
        }
    }
}
