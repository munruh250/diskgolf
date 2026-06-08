using Cinemachine;
using UnityEngine;

namespace DiskGolf.Camera
{
    /// <summary>Locks a virtual camera to a captured world pose (used after the disc lands).</summary>
    public sealed class LandingCameraFreezeExtension : CinemachineExtension
    {
        public bool IsFrozen { get; set; }

        public Vector3 FrozenPosition { get; set; }

        public Quaternion FrozenRotation { get; set; } = Quaternion.identity;

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            if (!IsFrozen || stage != CinemachineCore.Stage.Finalize)
                return;

            state.RawPosition = FrozenPosition;
            state.RawOrientation = FrozenRotation;
        }
    }
}
