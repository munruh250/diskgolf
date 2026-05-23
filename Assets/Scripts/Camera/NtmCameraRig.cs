using Cinemachine;
using UnityEngine;

namespace DiskGolf.Camera
{
    /// <summary>Neo Turf Masters-style camera framing constants and setup.</summary>
    public static class NtmCameraRig
    {
        public const string SideSetupName = "SideSetupCam";
        public const string FlightChaseName = "FlightChaseCam";
        public const string TopDownName = "TopDownTrackCam";
        public const string AimPointName = "ThrowAimPoint";

        /// <summary>Camera behind thrower — pulled back for course view, player low in frame (NTM).</summary>
        public static readonly Vector3 SideFollowOffset = new(0.35f, 2.0f, -5.6f);

        public static readonly Vector3 FlightChaseOffset = new(0.5f, 2.25f, -4.8f);

        public const float SideFieldOfView = 54f;

        public const int SidePriority = 20;

        public const int FlightChasePriority = 18;

        public const int TopDownPriority = 22;

        public static Transform EnsureAimPoint(Transform thrower, Transform basket)
        {
            var existing = GameObject.Find(AimPointName);
            if (existing == null && Application.isPlaying)
                return basket;

            var go = existing ?? CreateAimPointObject();
            if (thrower == null || basket == null)
                return go.transform;

            var dir = (basket.position - thrower.position).normalized;
            var dist = Vector3.Distance(thrower.position, basket.position);
            go.transform.position = thrower.position + dir * Mathf.Clamp(dist * 0.38f, 22f, 42f) + Vector3.up * 0.55f;
            return go.transform;
        }

        static GameObject CreateAimPointObject()
        {
            var go = new GameObject(AimPointName);
            var gm = GameObject.Find("GameManager");
            if (gm != null)
                go.transform.SetParent(gm.transform, false);

            return go;
        }

        public static CinemachineVirtualCamera ConfigureSideThrowCam(
            CinemachineVirtualCamera vcam,
            Transform thrower,
            Transform aimPoint)
        {
            if (vcam == null || thrower == null)
                return vcam;

            vcam.Follow = thrower;
            vcam.LookAt = aimPoint != null ? aimPoint : thrower;
            vcam.Priority = SidePriority;
            vcam.m_Lens.FieldOfView = SideFieldOfView;

            var transposer = vcam.GetCinemachineComponent<CinemachineTransposer>()
                ?? vcam.AddCinemachineComponent<CinemachineTransposer>();

            transposer.m_BindingMode = CinemachineTransposer.BindingMode.LockToTargetWithWorldUp;
            transposer.m_FollowOffset = SideFollowOffset;

            var composer = vcam.GetCinemachineComponent<CinemachineComposer>()
                ?? vcam.AddCinemachineComponent<CinemachineComposer>();

            composer.m_ScreenX = 0.38f;
            composer.m_ScreenY = 0.30f;
            composer.m_DeadZoneWidth = 0.04f;
            composer.m_DeadZoneHeight = 0.05f;
            composer.m_SoftZoneWidth = 0.78f;
            composer.m_SoftZoneHeight = 0.72f;
            composer.m_TrackedObjectOffset = new Vector3(0f, 0.15f, 0f);

            vcam.gameObject.SetActive(true);
            return vcam;
        }

        public static CinemachineVirtualCamera ConfigureFlightChaseCam(
            CinemachineVirtualCamera vcam,
            Transform disc,
            Transform aimPoint)
        {
            if (vcam == null || disc == null)
                return vcam;

            vcam.Follow = disc;
            vcam.LookAt = aimPoint != null ? aimPoint : disc;
            vcam.Priority = FlightChasePriority;
            vcam.m_Lens.FieldOfView = SideFieldOfView;

            var transposer = vcam.GetCinemachineComponent<CinemachineTransposer>()
                ?? vcam.AddCinemachineComponent<CinemachineTransposer>();

            transposer.m_BindingMode = CinemachineTransposer.BindingMode.LockToTargetWithWorldUp;
            transposer.m_FollowOffset = FlightChaseOffset;

            vcam.gameObject.SetActive(false);
            return vcam;
        }

        public static CinemachineVirtualCamera FindSideSetupCam()
        {
            var cams = Object.FindObjectsOfType<CinemachineVirtualCamera>(true);
            CinemachineVirtualCamera found = null;

            foreach (var cam in cams)
            {
                if (cam.name != SideSetupName)
                    continue;

                found ??= cam;
            }

            return found;
        }

#if UNITY_EDITOR
        public static void RemoveDuplicateSideSetupCams()
        {
            var cams = Object.FindObjectsOfType<CinemachineVirtualCamera>(true);
            CinemachineVirtualCamera keep = null;

            foreach (var cam in cams)
            {
                if (cam.name != SideSetupName)
                    continue;

                if (keep == null)
                {
                    keep = cam;
                    continue;
                }

                Object.DestroyImmediate(cam.gameObject);
            }
        }
#endif
    }
}
