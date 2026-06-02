using UnityEngine;

namespace DiskGolf.Camera
{
    /// <summary>Inspector-tunable side and flight camera values. Add to CameraDirector or GameManager.</summary>
    public sealed class FlightCameraSettings : MonoBehaviour
    {
        public static FlightCameraSettings Active { get; private set; }

        [Header("Side throw / setup camera")]
        public Vector3 sideFollowOffset = new(-0.85f, 1.22f, -5.1f);

        [Range(30f, 70f)]
        public float sideFieldOfView = 50f;

        [Range(0f, 1f)]
        public float sideScreenX = 0.42f;

        [Range(0f, 1f)]
        public float sideScreenY = 0.72f;

        [Header("Trajectory target zoom (Z toggle)")]
        public Vector3 targetZoomOffset = new(0f, 7f, -9f);

        [Range(30f, 70f)]
        public float targetZoomFieldOfView = 44f;

        [Min(0.5f)]
        public float trajectoryZoomTravelSeconds = 1.85f;

        [Min(0.25f)]
        public float trajectoryZoomExitSeconds = 0.9f;

        [Header("In-flight chase camera")]
        public Vector3 chaseOffset = new(0f, 1.8f, -6f);

        [Tooltip("0.5 = vertical center. Raise slightly (e.g. 0.55) to clear bottom HUD.")]
        [Range(0.35f, 0.65f)]
        public float targetViewportY = 0.5f;

        public Vector3 SideFollowOffset => sideFollowOffset;

        public float SideFieldOfView => sideFieldOfView;

        public Vector3 ChaseOffset => chaseOffset;

        public Vector3 TargetZoomOffset => targetZoomOffset;

        public float TargetZoomFieldOfView => targetZoomFieldOfView;

        public float TrajectoryZoomTravelSeconds => trajectoryZoomTravelSeconds;

        public float TrajectoryZoomExitSeconds => trajectoryZoomExitSeconds;

        public float TargetViewportY => targetViewportY;

        void OnEnable() => Active = this;

        void OnDisable()
        {
            if (Active == this)
                Active = null;
        }

        public static FlightCameraSettings Resolve(MonoBehaviour host)
        {
            if (Active != null)
                return Active;

            if (host != null)
            {
                var onHost = host.GetComponent<FlightCameraSettings>();
                if (onHost != null)
                    return onHost;
            }

            return FindObjectOfType<FlightCameraSettings>();
        }
    }
}
