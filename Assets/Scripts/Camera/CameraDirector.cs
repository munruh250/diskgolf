using Cinemachine;
using DiskGolf.Core;
using UnityEngine;

namespace DiskGolf.Camera
{
    /// <summary>Swaps Cinemachine virtual cameras based on ThrowController phase.</summary>
    public class CameraDirector : MonoBehaviour
    {
        [SerializeField] CinemachineVirtualCamera sideSetupCam;

        [SerializeField] CinemachineVirtualCamera topDownTrackCam;

        [SerializeField] CinemachineVirtualCamera lieZoomCam;

        [SerializeField] CinemachineVirtualCamera overheadPuttCam;

        [SerializeField] ThrowController throwController;

        void OnEnable()
        {
            if (throwController != null)
                throwController.PhaseChanged += OnPhase;
        }

        void OnDisable()
        {
            if (throwController != null)
                throwController.PhaseChanged -= OnPhase;
        }

        void OnPhase(ThrowPhase phase)
        {
            bool aimingGroup = phase is ThrowPhase.Aiming
                or ThrowPhase.PowerMeter
                or ThrowPhase.HeightMeter;

            SetActive(sideSetupCam, aimingGroup);
            SetActive(topDownTrackCam, phase == ThrowPhase.InFlight);
            SetActive(lieZoomCam, phase == ThrowPhase.Landed);
            SetActive(overheadPuttCam, phase == ThrowPhase.Putting);
        }

        static void SetActive(CinemachineVirtualCamera vcam, bool on)
        {
            if (vcam != null)
                vcam.gameObject.SetActive(on);
        }
    }
}
