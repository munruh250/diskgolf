using Cinemachine;
using DiskGolf.Core;
using DiskGolf.Gameplay;
using UnityEngine;

namespace DiskGolf.Camera
{
    /// <summary>Camera flow: side throw view → locked-direction chase for the whole flight.</summary>
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

        Vector3 _lockedThrowForward = Vector3.forward;

        void Awake()
        {
            sideSetupCam ??= CameraRig.FindSideSetupCam();
            flightChaseCam ??= CameraRig.FindFlightChaseCam();
            overheadPuttCam ??= CameraRig.FindNamedVcam(CameraRig.OverheadPuttName);
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
                or ThrowPhase.Putting
                or ThrowPhase.Landed;

            if (phase == ThrowPhase.InFlight)
                BindFlightChaseToDisc();
            else
                SetActive(flightChaseCam, false);

            SetSideThrowViewActive(sideThrowView);

            SetActive(lieZoomCam, false);
            SetActive(overheadPuttCam, false);
        }

        void BindFlightChaseToDisc()
        {
            SetPriority(sideSetupCam, 0);

            if (flightChaseCam == null)
                return;

            var disc = flightPresenter != null ? flightPresenter.DiscTransform : null;
            if (disc != null)
            {
                _lockedThrowForward = ResolveThrowForward(disc.position);
                CameraRig.ConfigureFlightChaseCam(flightChaseCam, disc, _lockedThrowForward);
            }

            SetPriority(flightChaseCam, CameraRig.FlightChasePriority);
            SetActive(flightChaseCam, true);
        }

        Vector3 ResolveThrowForward(Vector3 discLie)
        {
            if (throwController != null)
            {
                var aim = throwController.LastThrowAimDirection;
                aim.y = 0f;

                if (aim.sqrMagnitude > 1e-6f)
                    return aim.normalized;
            }

            hole ??= FindObjectOfType<HoleSetup>();

            if (hole != null)
            {
                var aim = hole.AimDirectionFrom(discLie);
                aim.y = 0f;

                if (aim.sqrMagnitude > 1e-6f)
                    return aim.normalized;
            }

            return Vector3.forward;
        }

        void SetSideThrowViewActive(bool on)
        {
            if (on)
                BindSideThrowCam();

            SetPriority(sideSetupCam, on ? CameraRig.SidePriority : 0);
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

            CameraRig.BindSideThrowCam(sideSetupCam, thrower, hole.BasketTransform);
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
