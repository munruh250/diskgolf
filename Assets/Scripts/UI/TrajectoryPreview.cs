using DiskGolf.Core;
using UnityEngine;

namespace DiskGolf.UI
{
    [RequireComponent(typeof(LineRenderer))]
    public class TrajectoryPreview : MonoBehaviour
    {
        public static readonly Color PathColor = MinimapTrajectoryLine.PathColor;

        [SerializeField] ThrowController controller;

        LineRenderer _line;

        void Awake() => _line = GetComponent<LineRenderer>();

        void LateUpdate()
        {
            if (controller == null || _line == null)
                return;

            if (controller.Phase != ThrowPhase.Aiming)
            {
                _line.enabled = false;

                return;
            }

            _line.enabled = true;

            var path =
                controller != null ? controller.GetPreviewPath() : null;

            if (path == null || path.Waypoints == null ||
                path.Waypoints.Count == 0)
            {
                _line.enabled =
                    false;

                return;
            }

            var wps = path.Waypoints;

            _line.positionCount = wps.Count;

            for (int i = 0; i < wps.Count; i++)
                _line.SetPosition(i, wps[i].Position + Vector3.up * 0.1f);

            _line.startColor = _line.endColor = PathColor;
            _line.startWidth = _line.endWidth = 0.15f;
        }
    }
}
