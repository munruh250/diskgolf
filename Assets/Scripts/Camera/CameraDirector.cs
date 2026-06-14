using System.Collections;
using Cinemachine;
using DiskGolf.Core;
using DiskGolf.Flight;
using DiskGolf.Gameplay;
using DiskGolf.UI;
using UnityEngine;

namespace DiskGolf.Camera
{
    /// <summary>Camera flow: side throw view → flight chase while disc is in the air and through the feet callout.</summary>
    public sealed class CameraDirector : MonoBehaviour
    {
        [SerializeField] CinemachineVirtualCamera sideSetupCam;

        [SerializeField] CinemachineVirtualCamera flightChaseCam;

        [SerializeField] ThrowController throwController;

        [SerializeField] DiscFlightPresenter flightPresenter;

        [SerializeField] HoleSetup hole;

        ThrowPhase _phase;

        bool _trajectoryZoomActive;

        bool _zoomAtEnd;

        Coroutine _zoomRoutine;

        TrajectoryZoomDriver _zoomDriver;

        TrajectoryZoomExtension _zoomExtension;

        CinemachineTransposer _sideTransposer;

        CinemachineComposer _sideComposer;

        TrajectoryLandingMarker _landingMarker;

        Vector3 _lockedThrowForward = Vector3.forward;

        LandingCameraFreezeExtension _freezeExtension;

        Vector3 _frozenPosition;

        Quaternion _frozenRotation = Quaternion.identity;

        bool _landingCameraFrozen;

        bool _holdLandingCameraUntilThrowSummary;

        CinemachineBrain _brain;

        public bool TrajectoryZoomActive => _trajectoryZoomActive;

        void Awake()
        {
            sideSetupCam ??= CameraRig.FindSideSetupCam();
            flightChaseCam ??= CameraRig.FindFlightChaseCam();
            hole ??= FindFirstObjectByType<HoleSetup>();

            if (flightPresenter == null && throwController != null)
                flightPresenter = throwController.GetComponent<DiscFlightPresenter>();

            EnsureZoomComponents();
            CacheBrain();
        }

        void EnsureZoomComponents()
        {
            if (sideSetupCam == null)
                return;

            _zoomExtension = sideSetupCam.GetComponent<TrajectoryZoomExtension>()
                ?? sideSetupCam.gameObject.AddComponent<TrajectoryZoomExtension>();
            _zoomExtension.enabled = false;

            _sideTransposer = sideSetupCam.GetCinemachineComponent<CinemachineTransposer>();
            _sideComposer = sideSetupCam.GetCinemachineComponent<CinemachineComposer>();

            _zoomDriver = GetComponent<TrajectoryZoomDriver>()
                ?? gameObject.AddComponent<TrajectoryZoomDriver>();

            _landingMarker = TrajectoryLandingMarker.Ensure();
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

        public void ToggleTrajectoryZoom(FlightPath path, Vector3 targetWorld, float yards)
        {
            if (sideSetupCam == null)
                return;

            if (_trajectoryZoomActive)
            {
                StopZoomRoutine();
                _trajectoryZoomActive = false;
                _zoomAtEnd = false;
                _landingMarker?.SetVisible(false);
                StartZoomRoutine(ExitZoomRoutine(path, targetWorld));
                return;
            }

            _trajectoryZoomActive = true;
            _zoomAtEnd = false;
            _zoomDriver.ExitSideBlend = 0f;
            _landingMarker?.UpdateLanding(targetWorld, yards);
            StartZoomRoutine(EnterZoomRoutine(path, targetWorld));
        }

        public void RefreshTrajectoryZoom(FlightPath path, Vector3 targetWorld, float yards)
        {
            if (!_trajectoryZoomActive)
                return;

            _landingMarker?.UpdateLanding(targetWorld, yards);

            if (_zoomDriver != null && path?.Waypoints != null)
            {
                _zoomDriver.Waypoints = path.Waypoints;
                _zoomDriver.Landing = targetWorld;
            }
        }

        public void ClearTrajectoryZoom()
        {
            StopZoomRoutine();
            _trajectoryZoomActive = false;
            _zoomAtEnd = false;
            _zoomDriver.ExitSideBlend = 0f;
            _landingMarker?.SetVisible(false);
            SetPathDriveActive(false);

            if (sideSetupCam != null && !_landingCameraFrozen && !_holdLandingCameraUntilThrowSummary)
                BindSideThrowCam();
        }

        void StartZoomRoutine(IEnumerator routine)
        {
            StopZoomRoutine();
            _zoomRoutine = StartCoroutine(routine);
        }

        void StopZoomRoutine()
        {
            if (_zoomRoutine == null)
                return;

            StopCoroutine(_zoomRoutine);
            _zoomRoutine = null;
        }

        IEnumerator EnterZoomRoutine(FlightPath path, Vector3 targetWorld)
        {
            hole ??= FindFirstObjectByType<HoleSetup>();
            var thrower = hole != null ? hole.Thrower : null;
            if (thrower == null || path?.Waypoints == null || path.Waypoints.Count < 2)
            {
                _zoomDriver.BindThrower(thrower);
                _zoomDriver.Waypoints = path?.Waypoints;
                _zoomDriver.Landing = targetWorld;
                _zoomDriver.PathT = 1f;
                SetPathDriveActive(true);
                _zoomAtEnd = true;
                _zoomRoutine = null;
                yield break;
            }

            _zoomDriver.BindThrower(thrower);
            _zoomDriver.Waypoints = path.Waypoints;
            _zoomDriver.Landing = targetWorld;
            _zoomDriver.PathT = 0f;
            _zoomDriver.ExitSideBlend = 0f;

            SetPathDriveActive(true);
            SetPriority(sideSetupCam, CameraRig.SidePriority);
            SetActive(sideSetupCam, true);

            var settings = FlightCameraSettings.Resolve(this);
            float duration = settings != null ? settings.TrajectoryZoomTravelSeconds : 1.85f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _zoomDriver.PathT = Smooth01(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _zoomDriver.PathT = 1f;
            _zoomAtEnd = true;
            _zoomRoutine = null;
        }

        IEnumerator ExitZoomRoutine(FlightPath path, Vector3 targetWorld)
        {
            hole ??= FindFirstObjectByType<HoleSetup>();
            var thrower = hole != null ? hole.Thrower : null;

            _zoomDriver.ExitSideBlend = 0f;
            PrepareSideThrowCamForHandoff();

            if (thrower != null && path?.Waypoints != null && path.Waypoints.Count >= 2)
            {
                _zoomDriver.BindThrower(thrower);
                _zoomDriver.Waypoints = path.Waypoints;
                _zoomDriver.Landing = targetWorld;

                SetPathDriveActive(true);

                var settings = FlightCameraSettings.Resolve(this);
                float duration = settings != null ? settings.TrajectoryZoomExitSeconds : 0.9f;
                float startT = _zoomDriver.PathT;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float u = Smooth01(Mathf.Clamp01(elapsed / duration));
                    _zoomDriver.PathT = Mathf.Lerp(startT, 0f, u);
                    float handoff = Smooth01(Mathf.InverseLerp(0.5f, 1f, u));
                    _zoomDriver.ExitSideBlend = handoff;

                    if (handoff > 0.08f)
                        SetSideThrowCamComponentsActive(true);

                    yield return null;
                }

                _zoomDriver.PathT = 0f;
            }

            _zoomDriver.ExitSideBlend = 1f;
            SetSideThrowCamComponentsActive(true);
            PrepareSideThrowCamForHandoff();
            SetPathDriveActive(false);
            _zoomDriver.ExitSideBlend = 0f;
            _zoomRoutine = null;
        }

        void PrepareSideThrowCamForHandoff()
        {
            if (sideSetupCam == null)
                return;

            hole ??= FindFirstObjectByType<HoleSetup>();
            var thrower = hole != null ? hole.Thrower : null;
            if (thrower == null)
                return;

            hole.RefreshCameraAimPoint();
            CameraRig.BindSideThrowCam(sideSetupCam, thrower, hole.BasketTransform);
        }

        void SetSideThrowCamComponentsActive(bool on)
        {
            if (_sideTransposer != null)
                _sideTransposer.enabled = on;

            if (_sideComposer != null)
                _sideComposer.enabled = on;
        }

        void SetPathDriveActive(bool on)
        {
            if (_zoomDriver != null)
                _zoomDriver.IsDriving = on;

            if (_zoomExtension != null)
                _zoomExtension.enabled = on;

            if (_sideTransposer != null)
                _sideTransposer.enabled = !on;

            if (_sideComposer != null)
                _sideComposer.enabled = !on;
        }

        static float Smooth01(float t) => t * t * (3f - 2f * t);

        /// <summary>
        /// Instant cut to behind-the-thrower side view after the thrower has been relocated.
        /// Call when the feet callout ends so we never blend through the outbound flight heading.
        /// </summary>
        public void SnapToSideThrowView()
        {
            if (sideSetupCam == null)
                return;

            ClearTrajectoryZoom();
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

        public void HoldLandingCameraUntilThrowSummary()
        {
            _holdLandingCameraUntilThrowSummary = true;
            FreezeLandingCamera();
        }

        public void ReleaseLandingCameraHold()
        {
            if (!_holdLandingCameraUntilThrowSummary)
                return;

            _holdLandingCameraUntilThrowSummary = false;
            UnfreezeLandingCamera();
            SnapToSideThrowView();
        }

        public void ClearLandingCameraHold()
        {
            _holdLandingCameraUntilThrowSummary = false;
            UnfreezeLandingCamera();
        }

        public void FreezeLandingCamera()
        {
            if (flightChaseCam == null || _landingCameraFrozen)
                return;

            EnsureFreezeExtension();
            CacheBrain();

            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                _frozenPosition = cam.transform.position;
                _frozenRotation = cam.transform.rotation;
            }

            _landingCameraFrozen = true;
            _freezeExtension.IsFrozen = true;
            _freezeExtension.FrozenPosition = _frozenPosition;
            _freezeExtension.FrozenRotation = _frozenRotation;

            flightChaseCam.Follow = null;
            flightChaseCam.LookAt = null;
            SetFlightDirectionLockEnabled(false);
            SetPriority(flightChaseCam, CameraRig.FlightChasePriority);
            SetActive(flightChaseCam, true);
            SetPriority(sideSetupCam, 0);
            SetActive(sideSetupCam, false);

            _brain?.ManualUpdate();
        }

        void UnfreezeLandingCamera()
        {
            if (!_landingCameraFrozen)
                return;

            _landingCameraFrozen = false;

            if (_freezeExtension != null)
                _freezeExtension.IsFrozen = false;

            SetFlightDirectionLockEnabled(true);
        }

        void SetFlightDirectionLockEnabled(bool on)
        {
            if (flightChaseCam == null)
                return;

            var lockExt = flightChaseCam.GetComponent<FlightDirectionLockExtension>();
            if (lockExt != null)
                lockExt.enabled = on;
        }

        void EnsureFreezeExtension()
        {
            if (flightChaseCam == null)
                return;

            _freezeExtension ??= flightChaseCam.GetComponent<LandingCameraFreezeExtension>()
                ?? flightChaseCam.gameObject.AddComponent<LandingCameraFreezeExtension>();
        }

        void OnPhase(ThrowPhase phase)
        {
            var previous = _phase;
            _phase = phase;

            bool sideThrowView = phase is ThrowPhase.Aiming
                or ThrowPhase.PowerMeter
                or ThrowPhase.HeightMeter
                or ThrowPhase.Putting;

            bool holdLandingCamera = _holdLandingCameraUntilThrowSummary
                && phase is ThrowPhase.Aiming or ThrowPhase.Putting;

            bool flightChaseView = phase is ThrowPhase.InFlight or ThrowPhase.Landed || holdLandingCamera;

            bool cutFromLanded = previous == ThrowPhase.Landed && sideThrowView && !holdLandingCamera;

            if (!sideThrowView)
                ClearTrajectoryZoom();

            if (flightChaseView)
                BindFlightChaseToDisc();
            else
                SetActive(flightChaseCam, false);

            if (sideThrowView && !holdLandingCamera)
            {
                if (cutFromLanded)
                    SnapToSideThrowView();
                else
                    SetSideThrowViewActive(true);
            }
            else if (!sideThrowView)
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

            if (_landingCameraFrozen)
            {
                SetPriority(flightChaseCam, CameraRig.FlightChasePriority);
                SetActive(flightChaseCam, true);
                return;
            }

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

            hole ??= FindFirstObjectByType<HoleSetup>();

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
            if (on && _trajectoryZoomActive && _zoomAtEnd)
                SetPathDriveActive(true);
            else if (on && !_trajectoryZoomActive)
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

            SetPathDriveActive(false);
            hole ??= FindFirstObjectByType<HoleSetup>();
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
