using System;
using System.Collections;
using DiskGolf.Disc;
using DiskGolf.Flight;
using DiskGolf.Gameplay;
using DiskGolf.Input;
using DiskGolf.UI;
using DiskGolf.Camera;
using UnityEngine;

namespace DiskGolf.Core
{
    public class ThrowController : MonoBehaviour
    {
        const float HoledToleranceFt = 1f;

        [SerializeField] HoleSetup hole;

        [SerializeField] DiscBag bag;

        [SerializeField] ThrowInputHandler input;

        [SerializeField] DiscFlightPresenter presenter;

        [SerializeField] PowerMeterUI powerMeter;

        [SerializeField] HeightMeterUI heightMeter;

        [SerializeField] GameObject inTheCircleBanner;

        [SerializeField] ThrowResultBannerUI throwResultBanner;

        [SerializeField] HoleCompleteBannerUI holeCompleteBanner;

        [SerializeField] SweetSpotBannerUI sweetSpotBanner;

        [SerializeField] ThrowAimAdjust aimAdjust;

        CameraDirector _cameraDirector;

        readonly ThrowStateMachine _state = new ThrowStateMachine();
        WindSettings _wind;

        float _confirmedPower;

        Vector3 _discPosition;

        int _strokeCount;

        bool _holeCompletePending;

        Coroutine _holeCompleteRoutine;

        bool _pendingPutOutcome;

        DiscProfile _trackedDisc;

        Coroutine _postThrowRoutine;

        Coroutine _sweetBannerRoutine;

        bool _postThrowPending;

        float _pendingRestFt;

        bool _pendingAllowPutting;

        bool _pendingWasPut;

        bool _throwFromPutting;

        Vector3 _lastThrowAim = Vector3.forward;

        public ThrowPhase Phase => _state.Phase;

        public event Action<ThrowPhase> PhaseChanged;

        public WindSettings Wind => _wind;

        public ReleaseAngle ReleaseAngle => input != null ? input.ReleaseAngle : ReleaseAngle.Flat;

        public DiscProfile ActiveDisc => bag != null ? bag.Active : null;

        public Vector3 CurrentDiscWorld => _discPosition;

        public Vector3 LastThrowAimDirection => _lastThrowAim;

        public int StrokeCount => _strokeCount;

        public int HolePar => hole != null ? hole.Par : 3;

        public bool IsHoleComplete => _holeCompletePending;

        public float TargetTrajectoryFt => aimAdjust != null ? aimAdjust.TargetDistanceFt : 0f;

        public bool IsPreThrowPhase => _state.Phase is ThrowPhase.Aiming
            or ThrowPhase.PowerMeter
            or ThrowPhase.HeightMeter;

        public bool ShowsTrajectoryPreview => IsPreThrowPhase || _state.Phase == ThrowPhase.Putting;

        void Awake()
        {
            aimAdjust ??= GetComponent<ThrowAimAdjust>() ?? gameObject.AddComponent<ThrowAimAdjust>();
            inTheCircleBanner ??= GameObject.Find("InTheCircleBanner") ?? GameObject.Find("TMPRow");
            _cameraDirector ??= FindObjectOfType<CameraDirector>();
        }

        void Start()
        {
            _state.PhaseChanged += p => PhaseChanged?.Invoke(p);
            _state.PhaseChanged += OnPhaseChangedInternal;
            ResetHole();
        }

        void OnDestroy()
        {
            _state.PhaseChanged -= OnPhaseChangedInternal;
        }

        void LateUpdate()
        {
            if (hole == null || presenter == null || hole.Thrower == null)
                return;

            if (!IsPreThrowPhase && _state.Phase != ThrowPhase.Putting)
                return;

            SyncDiscToHand();
            RefreshMeterPreview();
        }

        void RefreshMeterPreview()
        {
            if (!ShowsTrajectoryPreview || bag?.Active == null || aimAdjust == null)
            {
                powerMeter?.ClearTargetZone();
                heightMeter?.ClearTargetZone();
                return;
            }

            if (powerMeter != null && !powerMeter.IsRunning && !powerMeter.IsFrozenForFlight)
            {
                float powerCenter = FlightSimulator.MeterPowerForTargetDistance(
                    bag.Active, aimAdjust.TargetDistanceFt, aimAdjust.PlannedHeight) / 1.1f;
                powerMeter.PreviewTargetZone(powerCenter, 0.08f);
            }

            if (heightMeter != null && !heightMeter.IsRunning && !heightMeter.IsFrozenForFlight)
            {
                float heightCenter = FlightSimulator.HeightMeterCenter(aimAdjust.PlannedHeight);
                heightMeter.PreviewTargetZone(heightCenter, 0.1f);
            }
        }

        void SyncDiscToHand()
        {
            var pos = hole.DiscHoldPosition;
            var rot = hole.DiscHoldRotation;
            presenter.SetPositionAndRotation(pos, rot);
            _discPosition = pos;
        }

        void Update()
        {
            if (input == null || bag == null || hole == null)
                return;

            if (input.ResetPressed)
            {
                ResetHole();

                return;
            }

            if (_holeCompletePending)
                return;

            switch (_state.Phase)
            {
                case ThrowPhase.Aiming:
                    HandleAimingDrive();

                    break;
                case ThrowPhase.PowerMeter:
                    if (input.ConfirmPressed)
                    {
                        if (powerMeter != null)
                            _confirmedPower = powerMeter.Confirm();

                        _state.Advance();
                    }

                    break;
                case ThrowPhase.HeightMeter:
                    if (input.ConfirmPressed)
                    {
                        float heightRaw =
                            heightMeter != null ? heightMeter.Confirm() : 0.55f;

                        var height = HeightMeterZones.FromValue(heightRaw);

                        ExecuteThrow(_confirmedPower, height);
                    }

                    break;
                case ThrowPhase.Putting:
                    HandlePutting();

                    break;
                case ThrowPhase.Landed:
                    if (input.ConfirmPressed)
                        CompletePostThrowTransition();

                    break;
                default:

                    // Throwing/InFlight handled by presenter; Resolve is transient elsewhere.

                    break;
            }
        }

        /// <remarks>Putting has its own power-only confirm path (<see cref="HandlePutting"/>).</remarks>
        void HandleAimingDrive()
        {
            if (_state.Phase != ThrowPhase.Aiming)
                return;

            if (input.CycleNext)
                bag.CycleNext();

            if (input.CyclePrev)
                bag.CyclePrev();

            var hk = input.DiscHotkey;

            if (hk >= 0)
                bag.SelectIndex(hk);

            if (bag.Active != null && bag.Active != _trackedDisc)
            {
                _trackedDisc = bag.Active;
                aimAdjust.ResetForLie(hole, _discPosition, bag.Active);
            }

            if (bag.Active != null)
            {
                aimAdjust.ApplyHeldInput(
                    hole,
                    _discPosition,
                    bag.Active,
                    input.AimLeft,
                    input.AimRight,
                    input.AimUp,
                    input.AimDown);
            }

            if (input.ConfirmPressed)
            {
                BeginThrowMeters();
                _state.Advance();
            }
        }

        void BeginThrowMeters()
        {
            var disc = bag.Active;
            if (disc == null)
                return;

            float powerCenter = FlightSimulator.MeterPowerForTargetDistance(
                disc, aimAdjust.TargetDistanceFt, aimAdjust.PlannedHeight) / 1.1f;

            powerMeter?.SetTargetZone(powerCenter, 0.08f);
            powerMeter?.Begin();
        }

        void BeginHeightMeter()
        {
            float center = FlightSimulator.HeightMeterCenter(aimAdjust.PlannedHeight);
            heightMeter?.SetTargetZone(center, 0.1f);
            heightMeter?.Begin();
        }

        void HandlePutting()
        {
            if (bag.Active != null)
            {
                aimAdjust.ApplyHeldInput(
                    hole,
                    _discPosition,
                    bag.Active,
                    input.AimLeft,
                    input.AimRight,
                    input.AimUp,
                    input.AimDown);
            }

            if (!input.ConfirmPressed)
                return;

            _throwFromPutting = true;
            BeginThrowMeters();
            _state.Advance();
        }

        void ExecuteThrow(float power, ThrowHeight height)
        {
            if (presenter == null || hole == null)
                return;

            throwResultBanner?.Hide();

            bool isPutt = _throwFromPutting;
            _throwFromPutting = false;
            _pendingPutOutcome = isPutt;

            if (isPutt)
                bag.SelectIndex(0);

            var aim = aimAdjust.AimDirection(hole, _discPosition);
            _lastThrowAim = aim;
            var release = isPutt ? ReleaseAngle.Flat : input.ReleaseAngle;

            var throwInput = new ThrowInput(
                bag.Active,
                release,
                power,
                height,
                _wind,
                _discPosition,
                aim);

            var path = FlightSimulator.Compute(throwInput);

            _strokeCount++;

            if (BothMetersHitSweetSpot())
                StartSweetBanner();

            _state.Advance(); // HeightMeter → Throwing
            _state.Advance(); // Throwing → InFlight
            presenter.Play(path, isPutt ? OnPuttOutcomeComplete : OnFlightComplete);
        }

        void OnFlightComplete(FlightPath completedPath)
            => FinishThrowCommon(completedPath, allowEnterPutting: true);

        void OnPuttOutcomeComplete(FlightPath completedPath)
            => FinishThrowCommon(completedPath, allowEnterPutting: false);

        void FinishThrowCommon(FlightPath completedPath, bool allowEnterPutting)
        {
            var wps = completedPath?.Waypoints;

            if (presenter != null)
                _discPosition = presenter.LandedPosition;
            else if (wps != null && wps.Count > 0)
                _discPosition = wps[wps.Count - 1].Position;

            _state.Advance(); // InFlight → Landed

            EndMeterFlightDisplay();

            if (IsDiscHoled(_discPosition))
            {
                CompleteHole();
                return;
            }

            _pendingRestFt = hole != null ? hole.DistanceToBasket(_discPosition) : float.PositiveInfinity;
            _pendingAllowPutting = allowEnterPutting;
            _pendingWasPut = _pendingPutOutcome;
            _pendingPutOutcome = false;
            _postThrowPending = true;

            if (_postThrowRoutine != null)
                StopCoroutine(_postThrowRoutine);

            _postThrowRoutine = StartCoroutine(PostThrowRoutine(completedPath));
        }

        IEnumerator PostThrowRoutine(FlightPath completedPath)
        {
            if (completedPath != null)
            {
                throwResultBanner ??= ThrowResultBannerUI.Ensure();
                throwResultBanner?.ShowThrowDistance(completedPath.TotalDistanceFt);
            }

            float wait = throwResultBanner != null ? throwResultBanner.DisplaySeconds : 2.75f;
            float elapsed = 0f;

            while (elapsed < wait && _postThrowPending)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            CompletePostThrowTransition();
            _postThrowRoutine = null;
        }

        void CompletePostThrowTransition()
        {
            if (!_postThrowPending)
                return;

            _postThrowPending = false;
            throwResultBanner?.Hide();

            if (_pendingWasPut)
            {
                ResolvePutOutcome(_pendingRestFt);

                return;
            }

            RelocateThrowerForNextShot();
            SnapSideCameraForNextShot();

            if (_pendingAllowPutting && hole != null && _pendingRestFt <= hole.CircleRadiusFt)
            {
                _state.EnterPutting();

                return;
            }

            ResumeAimingFromLanded();
        }

        void ResumeAimingFromLanded()
        {
            if (_state.Phase != ThrowPhase.Landed)
                return;

            EnableCircleBanner(false);
            _state.TransitionTo(ThrowPhase.Aiming);
        }

        void SnapSideCameraForNextShot()
        {
            _cameraDirector ??= FindObjectOfType<CameraDirector>();
            _cameraDirector?.SnapToSideThrowView();
        }

        void ResolvePutOutcome(float restFt)
        {
            if (hole != null && (restFt <= HoledToleranceFt || IsDiscHoled(_discPosition)))
            {
                CompleteHole();
                return;
            }

            RelocateThrowerForNextShot();
            SnapSideCameraForNextShot();

            if (hole != null && restFt <= hole.CircleRadiusFt)
                _state.EnterPutting();
            else
                ResumeAimingFromLanded();
        }

        void RelocateThrowerForNextShot()
        {
            if (hole == null)
                return;

            if (hole.IsNearTee(_discPosition))
                hole.PositionThrowerAtTee();
            else
                hole.PositionThrowerAtLie(_discPosition);
        }

        bool IsDiscHoled(Vector3 discWorld) =>
            (presenter != null && presenter.LastFlightHoled)
            || BasketCatchDetector.ContainsPoint(discWorld, GreyboxScale.DiscDiameterM * 0.45f);

        void CompleteHole()
        {
            _postThrowPending = false;

            if (_postThrowRoutine != null)
            {
                StopCoroutine(_postThrowRoutine);
                _postThrowRoutine = null;
            }

            throwResultBanner?.Hide();
            EndMeterFlightDisplay();
            EnableCircleBanner(false);

            presenter?.SetPosition(hole.BasketPosition);
            _discPosition = hole.BasketPosition;

            _holeCompletePending = true;
            holeCompleteBanner ??= HoleCompleteBannerUI.Ensure();
            holeCompleteBanner?.Show(_strokeCount, HolePar);

            if (_holeCompleteRoutine != null)
                StopCoroutine(_holeCompleteRoutine);

            _holeCompleteRoutine = StartCoroutine(HoleCompleteRoutine());
        }

        IEnumerator HoleCompleteRoutine()
        {
            float wait = holeCompleteBanner != null ? holeCompleteBanner.DisplaySeconds : 3.5f;
            float elapsed = 0f;

            while (elapsed < wait && _holeCompletePending)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            _holeCompleteRoutine = null;
            ResetHole();
        }

        void OnPhaseChangedInternal(ThrowPhase phase)
        {
            switch (phase)
            {
                case ThrowPhase.HeightMeter:
                    BeginHeightMeter();

                    break;
                case ThrowPhase.Putting:
                    bag?.SelectIndex(0);
                    _trackedDisc = bag?.Active;

                    if (hole != null && bag?.Active != null)
                        aimAdjust?.ResetForLie(hole, _discPosition, bag.Active);

                    powerMeter?.Stop();
                    heightMeter?.Stop();
                    EnableCircleBanner(true);

                    SyncDiscToHand();

                    break;
                case ThrowPhase.Resolve:
                    powerMeter?.Stop();

                    EnableCircleBanner(false);

                    break;
                case ThrowPhase.Aiming:
                    powerMeter?.Stop();
                    heightMeter?.Stop();

                    EnableCircleBanner(false);

                    if (hole != null && presenter != null)
                        SyncDiscToHand();

                    if (hole != null && bag != null)
                    {
                        bag.SelectForDistance(hole.DistanceForDiscSelection(_discPosition));
                        _trackedDisc = bag.Active;
                    }

                    if (hole != null && bag?.Active != null)
                        aimAdjust.ResetForLie(hole, _discPosition, bag.Active);

                    break;
            }
        }

        void EnableCircleBanner(bool on)
        {
            if (inTheCircleBanner != null && inTheCircleBanner.activeSelf != on)
                inTheCircleBanner.SetActive(on);
        }

        public void ResetHole()
        {
            _pendingPutOutcome = false;
            _postThrowPending = false;
            _holeCompletePending = false;

            if (_postThrowRoutine != null)
            {
                StopCoroutine(_postThrowRoutine);
                _postThrowRoutine = null;
            }

            if (_holeCompleteRoutine != null)
            {
                StopCoroutine(_holeCompleteRoutine);
                _holeCompleteRoutine = null;
            }

            throwResultBanner?.Hide();
            holeCompleteBanner?.Hide();
            sweetSpotBanner?.Hide();

            if (_sweetBannerRoutine != null)
            {
                StopCoroutine(_sweetBannerRoutine);
                _sweetBannerRoutine = null;
            }

            _strokeCount = 0;
            _throwFromPutting = false;

            if (hole != null)
            {
                _wind = hole.RollWind();
                hole.PositionThrowerAtTee();
                _discPosition = hole.DiscHoldPosition;

                if (bag != null)
                {
                    bag.SelectForDistance(hole.DistanceForDiscSelection(_discPosition));
                    _trackedDisc = bag.Active;
                }

                if (bag?.Active != null)
                    aimAdjust.ResetForLie(hole, _discPosition, bag.Active);
            }

            presenter?.SetPositionAndRotation(hole.DiscHoldPosition, hole.DiscHoldRotation);

            heightMeter?.Stop();

            powerMeter?.Stop();

            EndMeterFlightDisplay();

            EnableCircleBanner(false);

            _state.TransitionTo(ThrowPhase.Aiming);
        }

        bool BothMetersHitSweetSpot() =>
            powerMeter != null && powerMeter.LastConfirmWasSweet
            && heightMeter != null && heightMeter.LastConfirmWasSweet;

        void StartSweetBanner()
        {
            if (_sweetBannerRoutine != null)
                StopCoroutine(_sweetBannerRoutine);

            _sweetBannerRoutine = StartCoroutine(SweetBannerRoutine());
        }

        IEnumerator SweetBannerRoutine()
        {
            sweetSpotBanner ??= SweetSpotBannerUI.Ensure();
            sweetSpotBanner?.Show();

            float wait = sweetSpotBanner != null ? sweetSpotBanner.DisplaySeconds : 2f;
            yield return new WaitForSeconds(wait);

            sweetSpotBanner?.Hide();
            _sweetBannerRoutine = null;
        }

        void EndMeterFlightDisplay()
        {
            powerMeter?.EndFlightDisplay();
            heightMeter?.EndFlightDisplay();
        }

        public FlightPath GetPreviewPath()
        {
            if (!ShowsTrajectoryPreview)
                return null;

            if (hole == null || bag?.Active == null || input == null || aimAdjust == null)
                return null;

            var aim = aimAdjust.AimDirection(hole, _discPosition);
            float previewPower = FlightSimulator.MeterPowerForTargetDistance(
                bag.Active, aimAdjust.TargetDistanceFt, aimAdjust.PlannedHeight);

            return FlightSimulator.Compute(new ThrowInput(
                bag.Active,
                _state.Phase == ThrowPhase.Putting ? ReleaseAngle.Flat : input.ReleaseAngle,
                previewPower,
                aimAdjust.PlannedHeight,
                _wind,
                _discPosition,
                aim));
        }
    }
}
