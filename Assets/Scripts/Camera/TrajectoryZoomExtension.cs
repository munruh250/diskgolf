using Cinemachine;
using UnityEngine;

namespace DiskGolf.Camera
{
    /// <summary>Overrides side camera pose while traveling along the preview path.</summary>
    public sealed class TrajectoryZoomExtension : CinemachineExtension
    {
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Finalize)
                return;

            var driver = TrajectoryZoomDriver.Instance;
            if (driver == null || !driver.IsDriving || driver.Thrower == null)
                return;

            if (driver.ExitSideBlend >= 0.999f)
                return;

            var settings = FlightCameraSettings.Resolve(null);
            var zoomPose = TrajectoryZoomPose.Evaluate(
                driver.Waypoints,
                driver.Thrower,
                driver.Landing,
                driver.PathT,
                settings);

            float sideFov = settings != null ? settings.SideFieldOfView : CameraRig.SideFieldOfView;
            float zoomFov = settings != null ? settings.TargetZoomFieldOfView : CameraRig.TargetZoomFieldOfView;
            float zoomFovAtT = Mathf.Lerp(sideFov, zoomFov, Smooth01(driver.PathT));

            if (driver.ExitSideBlend > 0f)
            {
                float handoff = Smooth01(driver.ExitSideBlend);
                var zoomRot = LookAtRotation(zoomPose.Position, zoomPose.LookAt);
                state.RawPosition = Vector3.Lerp(zoomPose.Position, state.RawPosition, handoff);
                state.RawOrientation = Quaternion.Slerp(zoomRot, state.RawOrientation, handoff);
                state.Lens.FieldOfView = Mathf.Lerp(zoomFovAtT, state.Lens.FieldOfView, handoff);
                return;
            }

            state.RawPosition = zoomPose.Position;
            state.RawOrientation = LookAtRotation(zoomPose.Position, zoomPose.LookAt);
            state.Lens.FieldOfView = zoomFovAtT;
        }

        static Quaternion LookAtRotation(Vector3 position, Vector3 lookAt)
        {
            var toTarget = lookAt - position;
            return toTarget.sqrMagnitude > 1e-8f
                ? Quaternion.LookRotation(toTarget.normalized, Vector3.up)
                : Quaternion.identity;
        }

        static float Smooth01(float t) => t * t * (3f - 2f * t);
    }
}
