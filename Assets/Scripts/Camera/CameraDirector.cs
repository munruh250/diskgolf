using Cinemachine;
using DiskGolf.Core;
using DiskGolf.Gameplay;
using UnityEngine;

namespace DiskGolf.Camera
{
    /// <summary>Camera flow: side throw view → flight chase while disc is in the air and through the feet callout.</summary>
    public class CameraDirector : MonoBehaviour
    {
        [SerializeField] CinemachineVirtualCamera sideSetupCam;

        [SerializeField] CinemachineVirtualCamera flightChaseCam;

        [SerializeField] ThrowController throwController;

        [SerializeField] DiscFlightPresenter flightPresenter;

        [SerializeField] HoleSetup hole;

        ThrowPhase _phase;

        Vector3 _lockedThrowForward = Vector3.forward;

        CinemachineBrain _brain;

        void Awake()
        {
            sideSetupCam ??= CameraRig.FindSideSetupCam();
            flightChaseCam ??= CameraRig.FindFlightChaseCam();
            hole ??= FindObjectOfType<HoleSetup>();

            if (flightPresenter == null && throwController != null)
                flightPresenter = throwController.GetComponent<DiscFlightPresenter>();

            CacheBrain();
        }

        void CacheBrain()
        {
            var mainCam = UnityEngine.Camera.main;
            _brain = mainCam != null ? mainCam.GetComponent<CinemachineBrain>() : null;
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

        /// <summary>
        /// Instant cut to behind-the-thrower side view after the thrower has been relocated.
        /// Call when the feet callout ends so we never blend through the outbound flight heading.
        /// </summary>
        public void SnapToSideThrowView()
        {
            if (sideSetupCam == null)
                return;

            CacheBrain();
            BindSideThrowCam();

            var savedBlend = _brain != null ? _brain.m_DefaultBlend : default;

            if (_brain != null)
            {
                _brain.m_DefaultBlend = new CinemachineBlendDefinition(
                    CinemachineBlendDefinition.Style.Cut,
                    0f);
            }

            SetPriority(flightChaseCam, 0);
            SetActive(flightChaseCam, false);
            SetPriority(sideSetupCam, CameraRig.SidePriority);
            SetActive(sideSetupCam, true);

            sideSetupCam.PreviousStateIsValid = false;
            if (flightChaseCam != null)
                flightChaseCam.PreviousStateIsValid = false;

            if (_brain != null)
            {
                _brain.ManualUpdate();
                _brain.m_DefaultBlend = savedBlend;
            }
        }

        void OnPhase(ThrowPhase phase)
        {
            var previous = _phase;
            _phase = phase;

            bool sideThrowView = phase is ThrowPhase.Aiming
                or ThrowPhase.PowerMeter
                or ThrowPhase.HeightMeter
                or ThrowPhase.Putting;

            bool flightChaseView = phase is ThrowPhase.InFlight or ThrowPhase.Landed;

            bool cutFromLanded = previous == ThrowPhase.Landed && sideThrowView;

            if (flightChaseView)
                BindFlightChaseToDisc();
            else
                SetActive(flightChaseCam, false);

            if (sideThrowView)
            {
                if (cutFromLanded)
                    SnapToSideThrowView();
                else
                    SetSideThrowViewActive(true);
            }
            else
            {
                SetPriority(sideSetupCam, 0);
                SetActive(sideSetupCam, false);
            }
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

            hole.RefreshCameraAimPoint();
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
