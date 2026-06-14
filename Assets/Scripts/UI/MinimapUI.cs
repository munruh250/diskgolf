using System.Collections.Generic;
using DiskGolf.Core;
using DiskGolf.CourseEditor;
using DiskGolf.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>
    /// NTM-style vertical minimap: orthographic capture of course geometry + marker overlays.
    /// </summary>
    public class MinimapUI : MonoBehaviour
    {
        const int TextureWidth = 248;

        const int TextureHeight = 392;

        const float ViewAspect = TextureWidth / (float)TextureHeight;

        const int MaxTrajectorySegments = 48;

        [SerializeField] ThrowController controller;

        [SerializeField] CourseLayout course;

        [SerializeField] HoleSetup hole;

        [SerializeField] Transform discTransform;

        [SerializeField] RawImage mapImage;

        [SerializeField] RectTransform markerLayer;

        [SerializeField] RectTransform discDot;

        [SerializeField] RectTransform teeDot;

        [SerializeField] RectTransform basketDot;

        static readonly Color TreeDotColor = new(0.45f, 0.28f, 0.12f, 1f);

        static readonly Vector2 TreeDotSize = new(8f, 8f);

        readonly List<RectTransform> _treeDots = new();

        UnityEngine.Camera _captureCam;

        RenderTexture _renderTexture;

        readonly List<Image> _trajectorySegments = new();

        RectTransform _trajectoryRoot;

        BuiltCourseHost builtCourse;

        void Awake()
        {
            ResolveCourseSources();
            controller ??= FindFirstObjectByType<ThrowController>();
            EnsureCaptureCamera();
            EnsureMapImage();
            EnsureMarkers();
            EnsureTreeMarkers();
            EnsureTrajectoryOverlay();
            EnsureTrajectoryLine();
        }

        void Start() => RefreshCapture();

        void OnDestroy()
        {
            if (_captureCam != null)
            {
                if (Application.isPlaying)
                    Destroy(_captureCam.gameObject);
                else
                    DestroyImmediate(_captureCam.gameObject);
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();

                if (Application.isPlaying)
                    Destroy(_renderTexture);
                else
                    DestroyImmediate(_renderTexture);
            }
        }

        void LateUpdate()
        {
            ResolveCourseSources();

            if (!HasCourseSource() || mapImage == null)
                return;

            markerLayer ??= transform.Find("MapPanel/MarkerLayer") as RectTransform;

            if (markerLayer == null)
                return;

            var mapRect = mapImage.rectTransform;

            if (teeDot != null && hole != null)
                teeDot.anchoredPosition = WorldToMapAnchored(hole.TeePosition, mapRect);

            if (basketDot != null && hole != null)
                basketDot.anchoredPosition = WorldToMapAnchored(hole.BasketPosition, mapRect);

            if (discDot != null && discTransform != null)
                discDot.anchoredPosition = WorldToMapAnchored(discTransform.position, mapRect);

            UpdateTreeMarkers(mapRect);
            UpdateTrajectoryOverlay(mapRect);
        }

        public void RefreshCapture()
        {
            ResolveCourseSources();
            builtCourse?.Refresh();
            builtCourse?.ApplyMinimapLayer();
            course?.Refresh();
            course?.ApplyMinimapLayer();
            FrameCourse();

            if (mapImage != null && _renderTexture != null)
                mapImage.texture = _renderTexture;
        }

        void EnsureCaptureCamera()
        {
            if (_captureCam != null)
                return;

            ResolveCourseSources();
            if (!HasCourseSource())
                return;

            _renderTexture = new RenderTexture(TextureWidth, TextureHeight, 16, RenderTextureFormat.ARGB32);
            _renderTexture.name = "MinimapRT";
            _renderTexture.Create();

            var camGo = new GameObject("MinimapCaptureCamera");
            var rigParent = ResolveCaptureParent();
            camGo.transform.SetParent(rigParent, false);

            _captureCam = camGo.AddComponent<UnityEngine.Camera>();
            _captureCam.orthographic = true;
            _captureCam.targetTexture = _renderTexture;
            _captureCam.clearFlags = CameraClearFlags.SolidColor;
            _captureCam.backgroundColor = new Color(0.07f, 0.1f, 0.07f);
            _captureCam.nearClipPlane = 1f;
            _captureCam.farClipPlane = 300f;
            _captureCam.depth = -20f;
            _captureCam.allowMSAA = false;
            _captureCam.useOcclusionCulling = false;

            int layer = LayerMask.NameToLayer(CourseLayout.MinimapLayerName);
            _captureCam.cullingMask = layer >= 0 ? 1 << layer : ~0;

            FrameCourse();
        }

        void FrameCourse()
        {
            if (_captureCam == null || !HasCourseSource())
                return;

            Vector3 center;
            float orthographicSize;
            if (builtCourse != null)
                builtCourse.ComputeMinimapFraming(ViewAspect, out center, out orthographicSize);
            else
                course.ComputeMinimapFraming(ViewAspect, out center, out orthographicSize);
            _captureCam.transform.position = center + Vector3.up * 120f;
            _captureCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _captureCam.orthographicSize = orthographicSize;
        }

        void EnsureMapImage()
        {
            var panel = transform.Find("MapPanel") as RectTransform;
            if (panel == null)
                return;

            mapImage = panel.GetComponentInChildren<RawImage>(true);
            if (mapImage == null)
            {
                var rawGo = new GameObject("MapRawImage", typeof(RectTransform), typeof(RawImage));
                var rt = rawGo.GetComponent<RectTransform>();
                rt.SetParent(panel, false);
                rt.SetAsFirstSibling();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                mapImage = rawGo.GetComponent<RawImage>();
                mapImage.color = Color.white;
            }

            if (_renderTexture != null)
                mapImage.texture = _renderTexture;
        }

        void EnsureMarkers()
        {
            markerLayer = transform.Find("MapPanel/MarkerLayer") as RectTransform;
            if (markerLayer == null)
                return;

            var mapRect = mapImage != null ? mapImage.rectTransform : markerLayer;
            teeDot ??= markerLayer.Find("TeeDot") as RectTransform;
            basketDot ??= markerLayer.Find("BasketDot") as RectTransform;
            discDot ??= markerLayer.Find("DiscDot") as RectTransform;

            if (hole != null && teeDot != null)
                teeDot.anchoredPosition = WorldToMapAnchored(hole.TeePosition, mapRect);

            if (hole != null && basketDot != null)
                basketDot.anchoredPosition = WorldToMapAnchored(hole.BasketPosition, mapRect);
        }

        void EnsureTreeMarkers()
        {
            markerLayer ??= transform.Find("MapPanel/MarkerLayer") as RectTransform;
            if (markerLayer == null || !HasCourseSource())
                return;

            var treesRoot = ResolveTreesRoot();
            if (treesRoot == null)
                return;

            int treeCount = treesRoot.childCount;
            while (_treeDots.Count < treeCount)
            {
                var dot = CreateMarkerDot(markerLayer, "TreeDot", TreeDotSize, TreeDotColor);
                dot.SetAsLastSibling();
                _treeDots.Add(dot);
            }

            for (int i = treeCount; i < _treeDots.Count; i++)
                _treeDots[i].gameObject.SetActive(false);
        }

        void UpdateTreeMarkers(RectTransform mapRect)
        {
            if (_treeDots.Count == 0)
                EnsureTreeMarkers();

            var treesRoot = ResolveTreesRoot();
            if (treesRoot == null)
                return;

            int treeCount = treesRoot.childCount;
            for (int i = 0; i < treeCount && i < _treeDots.Count; i++)
            {
                var dot = _treeDots[i];
                if (dot == null)
                    continue;

                dot.gameObject.SetActive(true);
                dot.anchoredPosition = WorldToMapAnchored(treesRoot.GetChild(i).position, mapRect);
            }

            for (int i = treeCount; i < _treeDots.Count; i++)
            {
                if (_treeDots[i] != null)
                    _treeDots[i].gameObject.SetActive(false);
            }
        }

        static RectTransform CreateMarkerDot(RectTransform parent, string name, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rt;
        }

        void EnsureTrajectoryOverlay()
        {
            markerLayer ??= transform.Find("MapPanel/MarkerLayer") as RectTransform;
            if (markerLayer == null)
                return;

            _trajectoryRoot = markerLayer.Find("TrajectoryOverlay") as RectTransform;
            if (_trajectoryRoot == null)
            {
                var go = new GameObject("TrajectoryOverlay", typeof(RectTransform));
                _trajectoryRoot = go.GetComponent<RectTransform>();
                _trajectoryRoot.SetParent(markerLayer, false);
                _trajectoryRoot.anchorMin = Vector2.zero;
                _trajectoryRoot.anchorMax = Vector2.one;
                _trajectoryRoot.offsetMin = Vector2.zero;
                _trajectoryRoot.offsetMax = Vector2.zero;
                _trajectoryRoot.SetAsLastSibling();
            }

            while (_trajectorySegments.Count < MaxTrajectorySegments)
            {
                var segGo = new GameObject("Seg", typeof(RectTransform), typeof(Image));
                var rt = segGo.GetComponent<RectTransform>();
                rt.SetParent(_trajectoryRoot, false);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

                var img = segGo.GetComponent<Image>();
                img.color = MinimapTrajectoryLine.PathColor;
                img.raycastTarget = false;
                segGo.SetActive(false);
                _trajectorySegments.Add(img);
            }
        }

        void UpdateTrajectoryOverlay(RectTransform mapRect)
        {
            if (_trajectorySegments.Count == 0)
                EnsureTrajectoryOverlay();

            if (controller == null || !controller.ShowsTrajectoryPreview)
            {
                HideTrajectorySegments();
                return;
            }

            var path = controller.GetPreviewPath();
            if (path?.Waypoints == null || path.Waypoints.Count < 2)
            {
                HideTrajectorySegments();
                return;
            }

            var wps = path.Waypoints;
            int segCount = Mathf.Min(wps.Count - 1, _trajectorySegments.Count);
            float discRadius = GreyboxScale.DiscDiameterM * 0.45f;
            var worldPoints = new Vector3[wps.Count];
            for (int i = 0; i < wps.Count; i++)
                worldPoints[i] = wps[i].Position;
            int hitWaypointIndex = TreeObstacle.FindFirstHitWaypointIndex(worldPoints, discRadius);

            for (int i = 0; i < segCount; i++)
            {
                var a = WorldToMapAnchored(wps[i].Position, mapRect);
                var b = WorldToMapAnchored(wps[i + 1].Position, mapRect);
                bool blocked = hitWaypointIndex >= 0 && i + 1 >= hitWaypointIndex;
                PlaceTrajectorySegment(_trajectorySegments[i], a, b, blocked);
            }

            for (int i = segCount; i < _trajectorySegments.Count; i++)
                _trajectorySegments[i].gameObject.SetActive(false);
        }

        static void PlaceTrajectorySegment(Image segment, Vector2 a, Vector2 b, bool blocked)
        {
            var rt = segment.rectTransform;
            var delta = b - a;
            float length = delta.magnitude;

            if (length < 0.5f)
            {
                segment.gameObject.SetActive(false);
                return;
            }

            segment.gameObject.SetActive(true);
            segment.color = blocked ? MinimapTrajectoryLine.BlockedPathColor : MinimapTrajectoryLine.PathColor;
            rt.anchoredPosition = (a + b) * 0.5f;
            rt.sizeDelta = new Vector2(length, 2f);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        void HideTrajectorySegments()
        {
            foreach (var segment in _trajectorySegments)
            {
                if (segment != null)
                    segment.gameObject.SetActive(false);
            }
        }

        void EnsureTrajectoryLine()
        {
            controller ??= FindFirstObjectByType<ThrowController>();
            ResolveCourseSources();
            if (!HasCourseSource())
                return;

            MinimapTrajectoryLine primary = null;
            foreach (var line in FindObjectsByType<MinimapTrajectoryLine>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (primary == null)
                {
                    primary = line;
                    continue;
                }

                if (Application.isPlaying)
                    Destroy(line.gameObject);
                else
                    DestroyImmediate(line.gameObject);
            }

            if (primary == null)
            {
                var go = new GameObject("MinimapTrajectoryLine");
                go.transform.SetParent(ResolveCaptureParent(), false);
                go.AddComponent<LineRenderer>();
                primary = go.AddComponent<MinimapTrajectoryLine>();
            }

            primary.Bind(controller);
        }

        void ResolveCourseSources()
        {
            builtCourse = FindFirstObjectByType<BuiltCourseHost>();
            if (builtCourse == null)
                course ??= CourseLayout.EnsureInScene();
            else
                course = null;
        }

        bool HasCourseSource() => builtCourse != null || course != null;

        Vector2 WorldToMapAnchored(Vector3 world, RectTransform mapRect)
        {
            if (builtCourse != null)
                return builtCourse.WorldToMapAnchored(world, mapRect, ViewAspect);

            if (course != null)
                return course.WorldToMapAnchored(world, mapRect, ViewAspect);

            return Vector2.zero;
        }

        Transform ResolveCaptureParent()
        {
            if (builtCourse != null)
                return builtCourse.transform;

            if (course != null)
                return course.transform.parent != null ? course.transform.parent : course.transform;

            return transform;
        }

        Transform ResolveTreesRoot()
        {
            if (builtCourse != null)
                return builtCourse.TreesRoot;

            return course != null ? course.TreesRoot : null;
        }

        public void Bind(CourseLayout layout, HoleSetup holeSetup, Transform disc, ThrowController throwController = null)
        {
            builtCourse = null;
            course = layout ?? CourseLayout.EnsureInScene();
            hole = holeSetup;
            discTransform = disc;
            controller = throwController ?? FindFirstObjectByType<ThrowController>();
            RefreshCapture();
        }
    }
}
