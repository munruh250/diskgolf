using Cinemachine;
using DiskGolf.Gameplay;
using UnityEngine;

namespace DiskGolf.Camera
{
    /// <summary>Side and flight camera framing constants and setup.</summary>
    public static class CameraRig
    {
        public const string SideSetupName = "SideSetupCam";
        public const string FlightChaseName = "FlightChaseCam";
        public const string AimPointName = "ThrowAimPoint";

        // Legacy names kept for one-time scene cleanup only.
        public const string LegacyTopDownName = "TopDownTrackCam";
        public const string LegacyLieZoomName = "LieZoomCam";
        public const string LegacyOverheadPuttName = "OverheadPuttCam";

        /// <summary>Low behind-left camera — player lands at bottom of frame.</summary>
        public static readonly Vector3 SideFollowOffset = new(-0.85f, 1.22f, -5.1f);

        /// <summary>Behind and slightly above the disc — LookAt keeps the disc screen-centered.</summary>
        public static readonly Vector3 FlightChaseOffset = new(0f, 1.8f, -6f);

        public const float SideFieldOfView = 50f;

        public const int SidePriority = 20;

        public const int FlightChasePriority = 18;

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
            float aimDist = dist <= 18f
                ? Mathf.Clamp(dist * 0.65f, 1.5f, Mathf.Max(dist - 0.5f, 1.5f))
                : Mathf.Clamp(dist * 0.48f, 28f, 55f);
            go.transform.position = thrower.position + dir * aimDist + Vector3.up * 0.35f;
            return go.transform;
        }

        static GameObject CreateAimPointObject()
        {
            var go = new GameObject(AimPointName);
            var parent = GameObject.Find(SceneHierarchy.PlayerThrower)?.transform
                ?? GameObject.Find("GameManager")?.transform;
            if (parent != null)
                go.transform.SetParent(parent, false);

            return go;
        }

        public static CinemachineVirtualCamera ConfigureSideThrowCam(
            CinemachineVirtualCamera vcam,
            Transform thrower,
            Transform aimPoint)
        {
            if (vcam == null || thrower == null)
                return vcam;

            var settings = FlightCameraSettings.Resolve(vcam);

            vcam.Follow = thrower;
            vcam.LookAt = aimPoint != null ? aimPoint : thrower;
            vcam.Priority = SidePriority;
            vcam.m_Lens.FieldOfView = settings != null ? settings.SideFieldOfView : SideFieldOfView;

            var transposer = vcam.GetCinemachineComponent<CinemachineTransposer>()
                ?? vcam.AddCinemachineComponent<CinemachineTransposer>();

            transposer.m_BindingMode = CinemachineTransposer.BindingMode.LockToTargetWithWorldUp;
            transposer.m_FollowOffset = settings != null ? settings.SideFollowOffset : SideFollowOffset;

            var composer = vcam.GetCinemachineComponent<CinemachineComposer>()
                ?? vcam.AddCinemachineComponent<CinemachineComposer>();

            float screenX = settings != null ? settings.sideScreenX : 0.42f;
            float screenY = settings != null ? settings.sideScreenY : 0.72f;

            composer.m_ScreenX = screenX;
            composer.m_ScreenY = screenY;
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
            Vector3 throwForward)
        {
            if (vcam == null || disc == null)
                return vcam;

            var settings = FlightCameraSettings.Resolve(vcam);
            var chaseOffset = settings != null ? settings.ChaseOffset : FlightChaseOffset;

            vcam.Follow = disc;
            vcam.LookAt = null;
            vcam.Priority = FlightChasePriority;
            vcam.m_Lens.FieldOfView = settings != null ? settings.SideFieldOfView : SideFieldOfView;

            var transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
                transposer.enabled = false;

            var composer = vcam.GetCinemachineComponent<CinemachineComposer>();
            if (composer != null)
                composer.enabled = false;

            var lockExtension = vcam.GetComponent<FlightDirectionLockExtension>()
                ?? vcam.gameObject.AddComponent<FlightDirectionLockExtension>();
            lockExtension.Bind(throwForward, chaseOffset);

            vcam.gameObject.SetActive(false);
            return vcam;
        }

        public static CinemachineVirtualCamera FindSideSetupCam() => FindNamedVcam(SideSetupName);

        public static CinemachineVirtualCamera FindFlightChaseCam() => FindNamedVcam(FlightChaseName);

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
