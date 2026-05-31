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

        [Header("In-flight chase camera")]
        public Vector3 chaseOffset = new(0f, 3.2f, -5f);

        [Tooltip("0.5 = vertical center. Raise (e.g. 0.58) to lift disc above bottom HUD buttons.")]
        [Range(0.35f, 0.65f)]
        public float targetViewportY = 0.54f;

        [Range(8, 16)]
        public int pitchSolveIterations = 12;

        public Vector3 SideFollowOffset => sideFollowOffset;

        public float SideFieldOfView => sideFieldOfView;

        public Vector3 ChaseOffset => chaseOffset;

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
