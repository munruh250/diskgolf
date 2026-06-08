using Cinemachine;
using UnityEngine;

namespace DiskGolf.Camera
{
    /// <summary>
    /// Follows the disc on a locked throw heading and keeps the disc centered on screen.
    /// </summary>
    public sealed class FlightDirectionLockExtension : CinemachineExtension
    {
        Vector3 _throwForward = Vector3.forward;
        Vector3 _offset = new(0f, 1.8f, -6f);

        public void Bind(Vector3 throwForward, Vector3 offset)
        {
            _throwForward = FlattenForward(throwForward);
            _offset = offset;
        }

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Finalize)
                return;

            var follow = vcam.Follow;
            if (follow == null)
                return;

            var forward = _throwForward;
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var discPos = follow.position;

            var cameraPos = discPos
                + forward * _offset.z
                + Vector3.up * _offset.y
                + right * _offset.x;

            var lookTarget = DiscLookTarget(discPos, cameraPos, forward, right, state.Lens);

            state.RawPosition = cameraPos;
            var toTarget = lookTarget - cameraPos;

            if (toTarget.sqrMagnitude > 1e-8f)
                state.RawOrientation = Quaternion.LookRotation(toTarget, Vector3.up);
        }

        Vector3 DiscLookTarget(
            Vector3 discPos,
            Vector3 cameraPos,
            Vector3 forward,
            Vector3 right,
            LensSettings lens)
        {
            var settings = FlightCameraSettings.Active;
            float targetY = settings != null ? settings.TargetViewportY : 0.5f;

            if (Mathf.Approximately(targetY, 0.5f))
                return discPos;

            float fovRad = lens.FieldOfView * Mathf.Deg2Rad;
            float tanHalfFov = Mathf.Tan(fovRad * 0.5f);
            float aspect = lens.Aspect > 0.01f ? lens.Aspect : 16f / 9f;
            float dist = Vector3.Distance(cameraPos, discPos);
            float verticalShift = (0.5f - targetY) * 2f * dist * tanHalfFov;

            return discPos + Vector3.up * verticalShift;
        }

        static Vector3 FlattenForward(Vector3 forward)
        {
            forward.y = 0f;

            if (forward.sqrMagnitude < 1e-6f)
                return Vector3.forward;

            return forward.normalized;
        }
    }
}
