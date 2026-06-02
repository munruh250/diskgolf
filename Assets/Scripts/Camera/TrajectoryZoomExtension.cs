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

            var settings = FlightCameraSettings.Resolve(null);
            var pose = TrajectoryZoomPose.Evaluate(
                driver.Waypoints,
                driver.Thrower,
                driver.Landing,
                driver.PathT,
                settings);

            state.RawPosition = pose.Position;
            var toTarget = pose.LookAt - pose.Position;

            if (toTarget.sqrMagnitude > 1e-8f)
                state.RawOrientation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);

            float sideFov = settings != null ? settings.SideFieldOfView : CameraRig.SideFieldOfView;
            float zoomFov = settings != null ? settings.TargetZoomFieldOfView : CameraRig.TargetZoomFieldOfView;
            state.Lens.FieldOfView = Mathf.Lerp(sideFov, zoomFov, Smooth01(driver.PathT));
        }

        static float Smooth01(float t) => t * t * (3f - 2f * t);
    }
}
