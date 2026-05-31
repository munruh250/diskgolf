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
        Vector3 _offset = new(0f, 3.2f, -5f);

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

            float pitch = SolvePitchToCenterDisc(forward, right, cameraPos, discPos, state.Lens);

            state.RawPosition = cameraPos;
            state.RawOrientation = Quaternion.AngleAxis(pitch, right)
                * Quaternion.LookRotation(forward, Vector3.up);
        }

        static float SolvePitchToCenterDisc(
            Vector3 forward,
            Vector3 right,
            Vector3 cameraPos,
            Vector3 discPos,
            LensSettings lens)
        {
            var settings = FlightCameraSettings.Active;
            float targetY = settings != null ? settings.TargetViewportY : 0.54f;
            int iterations = settings != null ? settings.pitchSolveIterations : 12;

            float lo = -32f;
            float hi = 28f;

            for (int i = 0; i < iterations; i++)
            {
                float mid = (lo + hi) * 0.5f;
                var rot = Quaternion.AngleAxis(mid, right) * Quaternion.LookRotation(forward, Vector3.up);
                float viewportY = WorldToViewport(discPos, cameraPos, rot, lens).y;

                if (viewportY < targetY)
                    hi = mid;
                else
                    lo = mid;
            }

            return (lo + hi) * 0.5f;
        }

        static Vector3 WorldToViewport(
            Vector3 worldPos,
            Vector3 cameraPos,
            Quaternion cameraRot,
            LensSettings lens)
        {
            var local = Quaternion.Inverse(cameraRot) * (worldPos - cameraPos);

            if (local.z <= 0.05f)
                return new Vector3(0.5f, 0f, local.z);

            float fovRad = lens.FieldOfView * Mathf.Deg2Rad;
            float tanHalfFov = Mathf.Tan(fovRad * 0.5f);
            float aspect = lens.Aspect > 0.01f ? lens.Aspect : 16f / 9f;

            float ndcX = local.x / (local.z * tanHalfFov * aspect);
            float ndcY = local.y / (local.z * tanHalfFov);

            return new Vector3(ndcX * 0.5f + 0.5f, ndcY * 0.5f + 0.5f, local.z);
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
