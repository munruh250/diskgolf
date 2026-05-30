using System.Collections.Generic;
using DiskGolf.Core;
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

        UnityEngine.Camera _captureCam;

        RenderTexture _renderTexture;

        readonly List<Image> _trajectorySegments = new();

        RectTransform _trajectoryRoot;

        void Awake()
        {
            course ??= CourseLayout.EnsureInScene();
            controller ??= FindObjectOfType<ThrowController>();
            EnsureCaptureCamera();
            EnsureMapImage();
            EnsureMarkers();
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
            if (course == null)
                course = CourseLayout.EnsureInScene();

            if (course == null || mapImage == null)
                return;

            markerLayer ??= transform.Find("MapPanel/MarkerLayer") as RectTransform;

            if (markerLayer == null)
                return;

            var mapRect = mapImage.rectTransform;

            if (teeDot != null && hole != null)
                teeDot.anchoredPosition = course.WorldToMapAnchored(hole.TeePosition, mapRect);

            if (basketDot != null && hole != null)
                basketDot.anchoredPosition = course.WorldToMapAnchored(hole.BasketPosition, mapRect);

            if (discDot != null && discTransform != null)
                discDot.anchoredPosition = course.WorldToMapAnchored(discTransform.position, mapRect);

            UpdateTrajectoryOverlay(mapRect);
        }

        public void RefreshCapture()
        {
            course ??= CourseLayout.EnsureInScene();
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

            course ??= CourseLayout.EnsureInScene();
            if (course == null)
                return;

            _renderTexture = new RenderTexture(TextureWidth, TextureHeight, 16, RenderTextureFormat.ARGB32);
            _renderTexture.name = "MinimapRT";
            _renderTexture.Create();

            var camGo = new GameObject("MinimapCaptureCamera");
            var rigParent = course.transform.parent != null ? course.transform.parent : course.transform;
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
            if (_captureCam == null || course == null)
                return;

            var bounds = course.WorldBounds;
            _captureCam.transform.position = bounds.center + Vector3.up * 120f;
            _captureCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            float aspect = TextureWidth / (float)TextureHeight;
            float halfHeight = bounds.extents.z * 1.1f;
            float halfWidth = bounds.extents.x * 1.1f;
            _captureCam.orthographicSize = Mathf.Max(halfHeight, halfWidth / aspect);
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
                teeDot.anchoredPosition = course.WorldToMapAnchored(hole.TeePosition, mapRect);

            if (hole != null && basketDot != null)
                basketDot.anchoredPosition = course.WorldToMapAnchored(hole.BasketPosition, mapRect);
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

            for (int i = 0; i < segCount; i++)
            {
                var a = course.WorldToMapAnchored(wps[i].Position, mapRect);
                var b = course.WorldToMapAnchored(wps[i + 1].Position, mapRect);
                PlaceTrajectorySegment(_trajectorySegments[i], a, b);
            }

            for (int i = segCount; i < _trajectorySegments.Count; i++)
                _trajectorySegments[i].gameObject.SetActive(false);
        }

        static void PlaceTrajectorySegment(Image segment, Vector2 a, Vector2 b)
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
            controller ??= FindObjectOfType<ThrowController>();
            course ??= CourseLayout.EnsureInScene();
            if (course == null)
                return;

            MinimapTrajectoryLine primary = null;
            foreach (var line in FindObjectsOfType<MinimapTrajectoryLine>(true))
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
                go.transform.SetParent(course.transform, false);
                go.AddComponent<LineRenderer>();
                primary = go.AddComponent<MinimapTrajectoryLine>();
            }

            primary.Bind(controller);
        }

        public void Bind(CourseLayout layout, HoleSetup holeSetup, Transform disc, ThrowController throwController = null)
        {
            course = layout ?? CourseLayout.EnsureInScene();
            hole = holeSetup;
            discTransform = disc;
            controller = throwController ?? FindObjectOfType<ThrowController>();
            RefreshCapture();
        }
    }
}
