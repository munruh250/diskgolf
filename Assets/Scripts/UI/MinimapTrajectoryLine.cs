using DiskGolf.Core;
using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>World-space throw preview path — visible in the main game view while aiming.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class MinimapTrajectoryLine : MonoBehaviour
    {
        public static readonly Color PathColor = new(1f, 0.92f, 0.15f, 0.95f);

        const float PathHeightOffset = 0.2f;

        [SerializeField] ThrowController controller;

        LineRenderer _line;

        void Awake()
        {
            _line = GetComponent<LineRenderer>();
            ConfigureLine();
        }

        void LateUpdate()
        {
            if (_line == null || controller == null)
                return;

            if (controller.Phase != ThrowPhase.Aiming)
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

            for (int i = 0; i < wps.Count; i++)
                _line.SetPosition(i, wps[i].Position + Vector3.up * PathHeightOffset);
        }

        void ConfigureLine()
        {
            _line.useWorldSpace = true;
            _line.loop = false;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.startWidth = 1.4f;
            _line.endWidth = 1.4f;
            _line.startColor = PathColor;
            _line.endColor = PathColor;
            _line.material = new Material(Shader.Find("Sprites/Default"));
            _line.textureMode = LineTextureMode.Stretch;
            _line.alignment = LineAlignment.View;
        }

        public void Bind(ThrowController throwController)
        {
            controller = throwController;
        }
    }
}
