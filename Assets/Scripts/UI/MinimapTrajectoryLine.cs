using DiskGolf.Core;
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

        const int TrajectorySortingOrder = 200;

        [SerializeField] ThrowController controller;

        LineRenderer _line;

        Material _material;

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
                return;
            }

            var path = controller.GetPreviewPath();
            if (path?.Waypoints == null || path.Waypoints.Count < 2)
            {
                _line.enabled = false;
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
            _line.material = _material = CreateAlwaysVisibleMaterial();
            _line.textureMode = LineTextureMode.Stretch;
            _line.alignment = LineAlignment.View;
            _line.colorGradient = SolidGradient(PathColor);
        }

        static Material CreateAlwaysVisibleMaterial()
        {
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.renderQueue = 3200;
            mat.SetInt("_ZWrite", 0);
            mat.SetInt("_ZTest", (int)CompareFunction.Always);
            return mat;
        }

        public void Bind(ThrowController throwController)
        {
            controller = throwController;
        }
    }
}
