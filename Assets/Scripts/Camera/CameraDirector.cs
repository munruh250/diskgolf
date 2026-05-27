using Cinemachine;
using DiskGolf.Core;
using DiskGolf.Gameplay;
using UnityEngine;

namespace DiskGolf.Camera
{
    /// <summary>NTM-style camera flow: side throw view → chase toward the pin for the whole flight.</summary>
    public class CameraDirector : MonoBehaviour
    {
        [SerializeField] CinemachineVirtualCamera sideSetupCam;

        [SerializeField] CinemachineVirtualCamera flightChaseCam;

        [SerializeField] CinemachineVirtualCamera lieZoomCam;

        [SerializeField] CinemachineVirtualCamera overheadPuttCam;

        [SerializeField] ThrowController throwController;

        [SerializeField] DiscFlightPresenter flightPresenter;

        [SerializeField] HoleSetup hole;

        ThrowPhase _phase;

        void Awake()
        {
            sideSetupCam ??= NtmCameraRig.FindSideSetupCam();
            flightChaseCam ??= NtmCameraRig.FindFlightChaseCam();
            hole ??= FindObjectOfType<HoleSetup>();

            if (flightPresenter == null && throwController != null)
                flightPresenter = throwController.GetComponent<DiscFlightPresenter>();
        }

        void Start()
        {
            if (throwController != null)
                OnPhase(throwController.Phase);
            else
                SetSideThrowViewActive(true);
        }

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
            _phase = phase;

            bool sideThrowView = phase is ThrowPhase.Aiming
                or ThrowPhase.PowerMeter
                or ThrowPhase.HeightMeter
                or ThrowPhase.Landed;

            if (phase == ThrowPhase.InFlight)
                BindFlightChaseToDisc();
            else
                SetActive(flightChaseCam, false);

            SetSideThrowViewActive(sideThrowView);

            SetActive(lieZoomCam, false);
            SetActive(overheadPuttCam, phase == ThrowPhase.Putting);
        }

        void BindFlightChaseToDisc()
        {
            SetPriority(sideSetupCam, 0);

            if (flightChaseCam == null)
                return;

            var disc = flightPresenter != null ? flightPresenter.DiscTransform : null;
            if (disc != null)
                NtmCameraRig.ConfigureFlightChaseCam(flightChaseCam, disc);

            SetPriority(flightChaseCam, NtmCameraRig.FlightChasePriority);
            SetActive(flightChaseCam, true);
        }

        void SetSideThrowViewActive(bool on)
        {
            if (on)
                BindSideThrowCam();

            SetPriority(sideSetupCam, on ? NtmCameraRig.SidePriority : 0);
            SetActive(sideSetupCam, on);

            if (on && sideSetupCam != null)
                sideSetupCam.gameObject.SetActive(true);
        }

        void BindSideThrowCam()
        {
            if (sideSetupCam == null)
                return;

            hole ??= FindObjectOfType<HoleSetup>();
            var thrower = hole != null ? hole.Thrower : null;
            if (thrower == null)
                return;

            NtmCameraRig.BindSideThrowCam(sideSetupCam, thrower, hole.BasketTransform);
        }

        static void SetActive(CinemachineVirtualCamera vcam, bool on)
        {
            if (vcam != null)
                vcam.gameObject.SetActive(on);
        }

        static void SetPriority(CinemachineVirtualCamera vcam, int priority)
        {
            if (vcam != null)
                vcam.Priority = priority;
        }
    }
}
