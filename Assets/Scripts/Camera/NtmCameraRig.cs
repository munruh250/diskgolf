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
        public const string LieZoomName = "LieZoomCam";
        public const string OverheadPuttName = "OverheadPuttCam";
        public const string AimPointName = "ThrowAimPoint";

        /// <summary>Low behind-left camera — player lands at bottom of frame (NTM).</summary>
        public static readonly Vector3 SideFollowOffset = new(-0.85f, 1.22f, -5.1f);

        public static readonly Vector3 FlightChaseOffset = new(0.1f, 3.2f, -7.5f);

        public const float SideFieldOfView = 50f;

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
            go.transform.position = thrower.position + dir * Mathf.Clamp(dist * 0.48f, 28f, 55f) + Vector3.up * 0.35f;
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

            // Aim point high in frame → thrower/tee sit low in viewport like NTM.
            composer.m_ScreenX = 0.42f;
            composer.m_ScreenY = 0.72f;
            composer.m_DeadZoneWidth = 0.06f;
            composer.m_DeadZoneHeight = 0.08f;
            composer.m_SoftZoneWidth = 0.75f;
            composer.m_SoftZoneHeight = 0.70f;
            composer.m_TrackedObjectOffset = Vector3.zero;

            vcam.gameObject.SetActive(true);
            return vcam;
        }

        public static void BindSideThrowCam(
            CinemachineVirtualCamera vcam,
            Transform thrower,
            Transform basket)
        {
            if (vcam == null || thrower == null)
                return;

            var aimPoint = EnsureAimPoint(thrower, basket);
            ConfigureSideThrowCam(vcam, thrower, aimPoint);
        }

        public static CinemachineVirtualCamera ConfigureFlightChaseCam(
            CinemachineVirtualCamera vcam,
            Transform disc,
            Transform lookTarget = null)
        {
            if (vcam == null || disc == null)
                return vcam;

            vcam.Follow = disc;
            vcam.LookAt = disc;
            vcam.Priority = FlightChasePriority;
            vcam.m_Lens.FieldOfView = SideFieldOfView;

            var transposer = vcam.GetCinemachineComponent<CinemachineTransposer>()
                ?? vcam.AddCinemachineComponent<CinemachineTransposer>();

            transposer.m_BindingMode = CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp;
            transposer.m_FollowOffset = FlightChaseOffset;

            var composer = vcam.GetCinemachineComponent<CinemachineComposer>()
                ?? vcam.AddCinemachineComponent<CinemachineComposer>();

            composer.m_ScreenX = 0.5f;
            composer.m_ScreenY = 0.4f;
            composer.m_DeadZoneWidth = 0.08f;
            composer.m_DeadZoneHeight = 0.08f;
            composer.m_SoftZoneWidth = 0.85f;
            composer.m_SoftZoneHeight = 0.85f;
            composer.m_TrackedObjectOffset = new Vector3(0f, 0.35f, 0f);

            vcam.gameObject.SetActive(false);
            return vcam;
        }

        public static CinemachineVirtualCamera FindSideSetupCam() => FindNamedVcam(SideSetupName);

        public static CinemachineVirtualCamera FindFlightChaseCam() => FindNamedVcam(FlightChaseName);

        public static CinemachineVirtualCamera FindTopDownCam() => FindNamedVcam(TopDownName);

        /// <summary>Find first vcam by name (includes inactive objects).</summary>
        public static CinemachineVirtualCamera FindNamedVcam(string name)
        {
            CinemachineVirtualCamera found = null;
            var cams = Object.FindObjectsOfType<CinemachineVirtualCamera>(true);

            foreach (var cam in cams)
            {
                if (cam.name != name)
                    continue;

                found ??= cam;
            }

            return found;
        }

        public static void RemoveDuplicateVcams()
        {
            RemoveDuplicateNamed(SideSetupName);
            RemoveDuplicateNamed(FlightChaseName);
            RemoveDuplicateNamed(TopDownName);
            RemoveDuplicateNamed(LieZoomName);
            RemoveDuplicateNamed(OverheadPuttName);
        }

        static void RemoveDuplicateNamed(string name)
        {
            CinemachineVirtualCamera keep = null;
            var cams = Object.FindObjectsOfType<CinemachineVirtualCamera>(true);

            foreach (var cam in cams)
            {
                if (cam.name != name)
                    continue;

                if (keep == null)
                {
                    keep = cam;
                    continue;
                }

                if (Application.isPlaying)
                    Object.Destroy(cam.gameObject);
                else
                    Object.DestroyImmediate(cam.gameObject);
            }
        }

#if UNITY_EDITOR
        public static void RemoveDuplicateSideSetupCams() => RemoveDuplicateNamed(SideSetupName);
#endif
    }
}
