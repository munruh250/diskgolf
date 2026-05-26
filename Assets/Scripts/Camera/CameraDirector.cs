using Cinemachine;
using DiskGolf.Core;
using DiskGolf.Gameplay;
using UnityEngine;

namespace DiskGolf.Camera
{
    /// <summary>NTM-style camera flow: side throw view → chase → top-down.</summary>
    public class CameraDirector : MonoBehaviour
    {
        [SerializeField] CinemachineVirtualCamera sideSetupCam;

        [SerializeField] CinemachineVirtualCamera flightChaseCam;

        [SerializeField] CinemachineVirtualCamera topDownTrackCam;

        [SerializeField] CinemachineVirtualCamera lieZoomCam;

        [SerializeField] CinemachineVirtualCamera overheadPuttCam;

        [SerializeField] ThrowController throwController;

        [SerializeField] DiscFlightPresenter flightPresenter;

        [SerializeField] float topDownDelaySeconds = 1f;

        ThrowPhase _phase;

        float _inFlightElapsed;

        bool _usingTopDown;

        void Awake()
        {
            sideSetupCam ??= NtmCameraRig.FindSideSetupCam();
            flightChaseCam ??= FindVcam(NtmCameraRig.FlightChaseName);
            topDownTrackCam ??= FindVcam(NtmCameraRig.TopDownName);

            if (flightPresenter == null && throwController != null)
                flightPresenter = throwController.GetComponent<DiscFlightPresenter>();
        }

        void Start()
        {
            if (throwController != null)
                OnPhase(throwController.Phase);
            else
                SetThrowViewActive(true);
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

        void Update()
        {
            if (_phase != ThrowPhase.InFlight || _usingTopDown)
                return;

            _inFlightElapsed += Time.deltaTime;

            if (_inFlightElapsed < topDownDelaySeconds)
                return;

            _usingTopDown = true;
            SetPriority(flightChaseCam, 0);
            SetPriority(topDownTrackCam, NtmCameraRig.TopDownPriority);
            SetActive(topDownTrackCam, true);
            SetActive(flightChaseCam, false);
        }

        void OnPhase(ThrowPhase phase)
        {
            _phase = phase;

            bool throwView = phase is ThrowPhase.Aiming
                or ThrowPhase.PowerMeter
                or ThrowPhase.HeightMeter;

            SetThrowViewActive(throwView);

            if (phase == ThrowPhase.InFlight)
            {
                _inFlightElapsed = 0f;
                _usingTopDown = false;
                SetPriority(sideSetupCam, 0);
                SetPriority(flightChaseCam, NtmCameraRig.FlightChasePriority);
                SetPriority(topDownTrackCam, 0);
                SetActive(flightChaseCam, true);
                SetActive(topDownTrackCam, false);
            }
            else if (phase != ThrowPhase.Landed && phase != ThrowPhase.Putting)
            {
                SetActive(flightChaseCam, false);
                SetActive(topDownTrackCam, false);
            }

            SetActive(lieZoomCam, phase == ThrowPhase.Landed);
            SetActive(overheadPuttCam, phase == ThrowPhase.Putting);
        }

        void SetThrowViewActive(bool on)
        {
            SetPriority(sideSetupCam, on ? NtmCameraRig.SidePriority : 0);
            SetActive(sideSetupCam, on);

            if (on && sideSetupCam != null)
                sideSetupCam.gameObject.SetActive(true);
        }

        static CinemachineVirtualCamera FindVcam(string name) => NtmCameraRig.FindNamedVcam(name);

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
