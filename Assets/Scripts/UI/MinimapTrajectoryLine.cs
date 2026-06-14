using DiskGolf.Camera;
using DiskGolf.Core;
using DiskGolf.Flight;
using DiskGolf.Gameplay;
using UnityEngine;
using UnityEngine.Rendering;

namespace DiskGolf.UI
{
    /// <summary>World-space throw preview path — visible in the main game view while aiming.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class MinimapTrajectoryLine : MonoBehaviour
    {
        public static readonly Color PathColor = new(1f, 0.92f, 0.15f, 0.42f);

        public static readonly Color BlockedPathColor = new(0.95f, 0.18f, 0.12f, 0.55f);

        const float PathHeightOffset = 0.2f;

        const int TrajectorySortingOrder = 15;

        [SerializeField] ThrowController controller;

        LineRenderer _line;

        Material _material;

        CameraDirector _cameraDirector;

        TrajectoryLandingMarker _apexMarker;

        void Awake()
        {
            _line = GetComponent<LineRenderer>();
            ConfigureLine();
        }

        void LateUpdate()
        {
            if (_line == null || controller == null)
                return;

            if (!controller.ShowsTrajectoryPreview)
            {
                _line.enabled = false;
                ClearApexPreview();
                return;
            }

            var path = controller.GetPreviewPath();
            if (path?.Waypoints == null || path.Waypoints.Count < 2)
            {
                _line.enabled = false;
                ClearApexPreview();
                return;
            }

            _line.enabled = true;
            var wps = path.Waypoints;
            _line.positionCount = wps.Count;

            var points = new Vector3[wps.Count];
            for (int i = 0; i < wps.Count; i++)
            {
                points[i] = wps[i].Position + Vector3.up * PathHeightOffset;
                _line.SetPosition(i, points[i]);
            }

            ApplyPathColors(points);
            RefreshApexLabel(wps);
        }

        void RefreshApexLabel(System.Collections.Generic.IReadOnlyList<FlightWaypoint> waypoints)
        {
            _cameraDirector ??= FindFirstObjectByType<CameraDirector>();
            if (_cameraDirector != null && _cameraDirector.TrajectoryZoomActive)
            {
                ClearApexPreview();
                return;
            }

            _apexMarker ??= TrajectoryLandingMarker.Ensure();
            var apex = FindApexPosition(waypoints) + Vector3.up * PathHeightOffset;
            _apexMarker.UpdateApexPreview(apex, controller.PreviewDistanceYards);
        }

        void ClearApexPreview()
        {
            if (_apexMarker == null)
                _apexMarker = FindFirstObjectByType<TrajectoryLandingMarker>(FindObjectsInactive.Include);

            _apexMarker?.ClearApexPreview();
        }

        static Vector3 FindApexPosition(System.Collections.Generic.IReadOnlyList<FlightWaypoint> waypoints)
        {
            var apex = waypoints[0].Position;
            float apexY = apex.y;

            for (int i = 1; i < waypoints.Count; i++)
            {
                float y = waypoints[i].Position.y;
                if (y <= apexY)
                    continue;

                apexY = y;
                apex = waypoints[i].Position;
            }

            return apex;
        }

        void ApplyPathColors(Vector3[] points)
        {
            float discRadius = GreyboxScale.DiscDiameterM * 0.45f;
            int hitIndex = TreeObstacle.FindFirstHitWaypointIndex(points, discRadius);

            if (hitIndex < 0)
            {
                _line.colorGradient = SolidGradient(PathColor);
                return;
            }

            float hitT = hitIndex / (float)(points.Length - 1);
            _line.colorGradient = SplitGradient(PathColor, BlockedPathColor, hitT);
        }

        static Gradient SolidGradient(Color color)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(color.a, 1f) });
            return gradient;
        }

        static Gradient SplitGradient(Color safe, Color blocked, float splitT)
        {
            splitT = Mathf.Clamp01(splitT);
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(safe, 0f),
                    new GradientColorKey(safe, splitT),
                    new GradientColorKey(blocked, splitT),
                    new GradientColorKey(blocked, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(safe.a, 0f),
                    new GradientAlphaKey(safe.a, splitT),
                    new GradientAlphaKey(blocked.a, splitT),
                    new GradientAlphaKey(blocked.a, 1f),
                });
            return gradient;
        }

        void ConfigureLine()
        {
            float width = GreyboxScale.DiscDiameterM;

            _line.useWorldSpace = true;
            _line.loop = false;
            _line.shadowCastingMode = ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.startWidth = width;
            _line.endWidth = width;
            _line.sortingOrder = TrajectorySortingOrder;
            _line.material = _material = CreateTrajectoryMaterial();
            _line.textureMode = LineTextureMode.Stretch;
            _line.alignment = LineAlignment.View;
            _line.colorGradient = SolidGradient(PathColor);
        }

        static Material CreateTrajectoryMaterial()
        {
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.renderQueue = 3000;
            mat.SetInt("_ZWrite", 0);
            mat.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            return mat;
        }

        public void Bind(ThrowController throwController)
        {
            controller = throwController;
        }
    }
}
