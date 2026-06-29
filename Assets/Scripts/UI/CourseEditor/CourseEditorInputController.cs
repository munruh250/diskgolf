using System.Collections;
using System.Collections.Generic;
using DiskGolf.CourseEditor;
using DiskGolf.CourseEditor.Authoring;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DiskGolf.UI.CourseEditor
{
    public sealed class CourseEditorInputController : MonoBehaviour
    {
        const float RebakeDelaySeconds = 0.1f;

        [SerializeField] UnityEngine.Camera authoringCamera;

        CourseEditorSession session;
        Coroutine rebakeCoroutine;
        bool draggingPrimary;
        readonly List<Vector2Int> paintStroke = new();
        readonly HashSet<Vector2Int> paintStrokeSet = new();
        readonly List<Vector2Int> hazardStroke = new();
        readonly HashSet<Vector2Int> hazardStrokeSet = new();
        Vector2Int lastDragTile;
        bool hasLastDragTile;

        public static CourseEditorInputController Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            session = CourseEditorSession.Instance;
            authoringCamera ??= UnityEngine.Camera.main;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            if (session?.Hole == null || session.Mode != CourseEditorSessionMode.Editing)
                return;

            if (authoringCamera == null)
                authoringCamera = UnityEngine.Camera.main;

            if (authoringCamera == null)
                return;

            if (UnityEngine.Input.GetMouseButtonDown(0))
                OnPrimaryDown();

            if (draggingPrimary && UnityEngine.Input.GetMouseButton(0))
                OnPrimaryDrag();

            if (draggingPrimary && UnityEngine.Input.GetMouseButtonUp(0))
                OnPrimaryUp();
        }

        void OnEnable()
        {
            if (authoringCamera == null)
                authoringCamera = UnityEngine.Camera.main;
            CourseEditorHoverOverlay.Ensure(authoringCamera);
        }

        public void NotifyMutation()
        {
            MarkDirtyAndScheduleRebake();
        }

        void OnPrimaryDown()
        {
            if (IsPointerOverUi())
                return;

            if (session.Authoring.ActiveTool == CourseAuthoringTool.Erase)
            {
                if (!TryApplyErase(isDragSample: false))
                    return;
            }
            else if (!TryGetHoveredTile(out Vector2Int tile))
            {
                return;
            }
            else
            {
                draggingPrimary = true;
                hasLastDragTile = true;
                lastDragTile = tile;
                paintStroke.Clear();
                paintStrokeSet.Clear();
                hazardStroke.Clear();
                hazardStrokeSet.Clear();
                ApplyToolAtTile(tile, isDragSample: false);
                return;
            }

            draggingPrimary = true;
            hasLastDragTile = false;
            paintStroke.Clear();
            paintStrokeSet.Clear();
            hazardStroke.Clear();
            hazardStrokeSet.Clear();
        }

        void OnPrimaryDrag()
        {
            if (IsPointerOverUi())
                return;

            if (session.Authoring.ActiveTool == CourseAuthoringTool.Erase)
            {
                if (!TryApplyErase(isDragSample: true))
                    return;

                hasLastDragTile = false;
                return;
            }

            if (!TryGetHoveredTile(out Vector2Int tile))
                return;

            if (hasLastDragTile && tile == lastDragTile)
                return;

            hasLastDragTile = true;
            lastDragTile = tile;
            ApplyToolAtTile(tile, isDragSample: true);
        }

        void OnPrimaryUp()
        {
            draggingPrimary = false;

            if (session.Authoring.ActiveTool == CourseAuthoringTool.Paint)
            {
                if (paintStroke.Count > 0)
                {
                    var command = new PaintStrokeCommand(session.Hole, paintStroke, session.Authoring.BrushType);
                    session.Commands.Execute(command);
                    MarkDirtyAndScheduleRebake();
                }

                paintStroke.Clear();
                paintStrokeSet.Clear();
                return;
            }

            if (session.Authoring.ActiveTool != CourseAuthoringTool.Hazard)
                return;

            if (hazardStroke.Count == 0)
                return;

            var hazardCommand = new HazardPaintStrokeCommand(
                session.Hole,
                hazardStroke,
                session.Authoring.HazardBrushType);
            session.Commands.Execute(hazardCommand);
            MarkDirtyAndScheduleRebake();
            hazardStroke.Clear();
            hazardStrokeSet.Clear();
        }

        void ApplyToolAtTile(Vector2Int tile, bool isDragSample)
        {
            switch (session.Authoring.ActiveTool)
            {
                case CourseAuthoringTool.Paint:
                    if (CourseAuthoringOperations.TryPaintTile(session.Hole, session.Authoring, tile.x, tile.y))
                        MarkDirtyAndScheduleRebake();
                    AddPaintStrokeTile(tile);
                    break;
                case CourseAuthoringTool.Erase:
                    break;
                case CourseAuthoringTool.HoleTee:
                    if (!isDragSample)
                    {
                        session.Commands.Execute(new PlaceTeeCommand(session.Hole, tile.x, tile.y));
                        MarkDirtyAndScheduleRebake();
                    }
                    break;
                case CourseAuthoringTool.HoleBasket:
                    if (!isDragSample)
                    {
                        session.Commands.Execute(new PlaceBasketCommand(session.Hole, tile.x, tile.y));
                        MarkDirtyAndScheduleRebake();
                    }
                    break;
                case CourseAuthoringTool.Hazard:
                    if (CourseAuthoringOperations.TryPaintHazardTile(session.Hole, session.Authoring, tile.x, tile.y))
                        MarkDirtyAndScheduleRebake();
                    AddHazardStrokeTile(tile);
                    break;
                case CourseAuthoringTool.Foliage:
                    if (!isDragSample)
                    {
                        session.Commands.Execute(new PlaceFoliageCommand(
                            session.Hole,
                            session.Authoring.FoliageArchetype,
                            tile.x,
                            tile.y));
                        MarkDirtyAndScheduleRebake();
                    }
                    break;
                case CourseAuthoringTool.Skybox:
                    break;
            }
        }

        void AddPaintStrokeTile(Vector2Int tile)
        {
            if (paintStrokeSet.Add(tile))
                paintStroke.Add(tile);
        }

        void AddHazardStrokeTile(Vector2Int tile)
        {
            if (hazardStrokeSet.Add(tile))
                hazardStroke.Add(tile);
        }

        void MarkDirtyAndScheduleRebake()
        {
            session.IsDirty = true;
            session.Authoring.IsDirty = true;
            if (rebakeCoroutine != null)
                StopCoroutine(rebakeCoroutine);
            rebakeCoroutine = StartCoroutine(RebakeDebounced());
        }

        IEnumerator RebakeDebounced()
        {
            yield return new WaitForSeconds(RebakeDelaySeconds);
            session.Rebake();
            rebakeCoroutine = null;
        }

        bool TryApplyErase(bool isDragSample)
        {
            if (!CourseEditorPointerPick.TryPick(session.Hole, authoringCamera, out var pick))
                return false;

            switch (pick.Kind)
            {
                case CourseEditorPointerPick.PickKind.Foliage:
                    if (!isDragSample)
                    {
                        session.Commands.Execute(new RemoveFoliageCommand(session.Hole, pick.FoliageIndex));
                        MarkDirtyAndScheduleRebake();
                    }
                    return true;
                case CourseEditorPointerPick.PickKind.Tee:
                    if (!isDragSample && session.Hole.Hole.Tee != Vector2.zero)
                    {
                        session.Commands.Execute(new ClearTeeCommand(session.Hole));
                        MarkDirtyAndScheduleRebake();
                    }
                    return true;
                case CourseEditorPointerPick.PickKind.Basket:
                    if (!isDragSample && session.Hole.Hole.Basket != Vector2.zero)
                    {
                        session.Commands.Execute(new ClearBasketCommand(session.Hole));
                        MarkDirtyAndScheduleRebake();
                    }
                    return true;
                case CourseEditorPointerPick.PickKind.Tile:
                    if (isDragSample && hasLastDragTile && pick.Tile == lastDragTile)
                        return true;

                    session.Commands.Execute(new EraseTileCommand(session.Hole, pick.Tile.x, pick.Tile.y));
                    MarkDirtyAndScheduleRebake();
                    hasLastDragTile = true;
                    lastDragTile = pick.Tile;
                    return true;
            }

            return false;
        }

        bool TryGetHoveredTile(out Vector2Int tile)
        {
            tile = default;
            if (!CourseEditorPointerPick.TryPick(session.Hole, authoringCamera, out var pick))
                return false;

            if (pick.Kind == CourseEditorPointerPick.PickKind.Tile)
            {
                tile = pick.Tile;
                return true;
            }

            if (CourseAuthoringGrid.TryWorldToTile(session.Hole, pick.WorldPoint, out tile))
                return true;

            return false;
        }

        float SampleGroundY(Ray ray)
        {
            var hits = Physics.RaycastAll(ray, 10000f);
            if (hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hits)
                {
                    if (IsBuiltCourseCollider(hit.collider))
                        return hit.point.y;
                }
            }

            if (session.Hole.Hole.Tee != Vector2.zero)
                return HeightGridSampler.SampleWorldY(session.Hole, session.Hole.Hole.Tee.x, session.Hole.Hole.Tee.y);

            return 0f;
        }

        static bool IsBuiltCourseCollider(Collider collider)
        {
            return collider != null && collider.GetComponentInParent<BuiltCourseHost>() != null;
        }

        static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
