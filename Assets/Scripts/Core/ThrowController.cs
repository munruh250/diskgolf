using System;
using System.Collections;
using DiskGolf.Disc;
using DiskGolf.Flight;
using DiskGolf.Gameplay;
using DiskGolf.Input;
using DiskGolf.UI;
using DiskGolf.Camera;
using DiskGolf.UI.Callouts;
using UnityEngine;
using UnityEngine.EventSystems;

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

        [SerializeField] OnTheGreenBannerUI onTheGreenBanner;

        [SerializeField] LieLandingBannerUI lieLandingBanner;

        [SerializeField] ThrowResultBannerUI throwResultBanner;

        [SerializeField] HoleCompleteBannerUI holeCompleteBanner;

        [SerializeField] HoleCompleteCutsceneUI holeCompleteCutscene;

        [SerializeField] ThrowSummaryBannerUI throwSummaryBanner;

        [SerializeField] GameplayCalloutHost calloutHost;

        [SerializeField] SweetSpotBannerUI sweetSpotBanner;

        [SerializeField] ThrowAimAdjust aimAdjust;

        [SerializeField] float basketCelebrationDelaySeconds = 2f;

        [SerializeField] float holeCompleteCutsceneSeconds = 3f;

        [SerializeField] AudioClip basketChainSfx;

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

        Coroutine _throwPresentationRoutine;

        bool _throwPresentationReady;

        bool _postThrowPending;

        float _pendingRestFt;

        bool _pendingAllowPutting;

        bool _pendingWasPut;

        bool _throwFromPutting;

        bool _showOnGreenLandingCallout = true;

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

        public float PreviewDistanceYards => aimAdjust != null ? aimAdjust.TargetDistanceFt / 3f : 0f;

        public bool IsPreThrowPhase => _state.Phase is ThrowPhase.Aiming
            or ThrowPhase.PowerMeter
            or ThrowPhase.HeightMeter;

        public bool ThrowPresentationReady => _throwPresentationReady;

        public bool ShowsTrajectoryPreview =>
            _throwPresentationReady && (IsPreThrowPhase || _state.Phase == ThrowPhase.Putting);

        void Awake()
        {
            aimAdjust ??= GetComponent<ThrowAimAdjust>() ?? gameObject.AddComponent<ThrowAimAdjust>();
            calloutHost ??= GameplayCalloutHost.Ensure();
            calloutHost?.BindReferences();
            if (calloutHost == null)
            {
                holeCompleteBanner ??= HoleCompleteBannerUI.Ensure();
                onTheGreenBanner ??= OnTheGreenBannerUI.Ensure();
                lieLandingBanner ??= LieLandingBannerUI.Ensure();
            }

            inTheCircleBanner ??= GameObject.Find("InTheCircleBanner") ?? GameObject.Find("TMPRow");
            if (inTheCircleBanner != null)
                inTheCircleBanner.SetActive(false);
            _cameraDirector ??= FindObjectOfType<CameraDirector>();
        }

        void Start()
        {
            calloutHost ??= GameplayCalloutHost.Ensure();
            calloutHost?.BindReferences();
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

            if (!_throwPresentationReady)
                return;

            if (!IsPreThrowPhase && _state.Phase != ThrowPhase.Putting)
                return;

            SyncDiscToHand();
            HandlePreThrowAimInput();
            RefreshMeterPreview();
            RefreshTrajectoryZoomCamera();
            ApplyThrowerAimLean();
        }

        void ApplyThrowerAimLean()
        {
            if (hole?.Thrower == null || aimAdjust == null)
                return;

            var visual = hole.Thrower.GetComponent<ThrowerVisual>();
            visual?.ApplyAimLean(aimAdjust.YawOffsetDegrees);
        }

        void HandlePreThrowAimInput()
        {
            if (input == null || bag?.Active == null || aimAdjust == null || hole == null)
                return;

            if (!input.AnyAimHeld)
                return;

            ClearUiSelectionForAim();

            aimAdjust.ApplyHeldInput(
                hole,
                _discPosition,
                bag.Active,
                input.AimLeftHeld,
                input.AimRightHeld,
                input.AimUpHeld,
                input.AimDownHeld,
                Time.deltaTime);
        }

        static void ClearUiSelectionForAim()
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        void RefreshTrajectoryZoomCamera()
        {
            if (_cameraDirector == null || !_cameraDirector.TrajectoryZoomActive)
                return;

            _cameraDirector.RefreshTrajectoryZoom(
                GetPreviewPath(),
                GetPreviewTargetWorld(),
                PreviewDistanceYards);
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
                heightMeter.PreviewTargetZone(AccuracyMeterZones.MeterCenter, AccuracyMeterZones.MeterWidth);
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

            if (ShowsTrajectoryPreview && input.TrajectoryZoomTogglePressed)
            {
                _cameraDirector?.ToggleTrajectoryZoom(
                    GetPreviewPath(),
                    GetPreviewTargetWorld(),
                    PreviewDistanceYards);
            }

            switch (_state.Phase)
            {
                case ThrowPhase.Aiming:
                    if (_throwPresentationReady)
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
                        float accuracyRaw =
                            heightMeter != null ? heightMeter.Confirm() : AccuracyMeterZones.MeterCenter;

                        var accuracy = AccuracyMeterZones.FromValue(accuracyRaw);
                        ExecuteThrow(_confirmedPower, accuracy);
                    }

                    break;
                case ThrowPhase.Putting:
                    if (_throwPresentationReady)
                        HandlePutting();

                    break;
                case ThrowPhase.Landed:
                    if (input.ConfirmPressed && _postThrowRoutine == null)
                        StartCoroutine(CompletePostThrowTransitionRoutine());

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
            heightMeter?.SetTargetZone(AccuracyMeterZones.MeterCenter, AccuracyMeterZones.MeterWidth);
            heightMeter?.Begin();
        }

        void SyncArcHeightToAim()
        {
            if (input == null || aimAdjust == null)
                return;

            aimAdjust.SetPlannedHeight(input.ArcHeight);
        }

        void HandlePutting()
        {
            if (!input.ConfirmPressed)
                return;

            _throwFromPutting = true;
            BeginThrowMeters();
            _state.Advance();
        }

        void ExecuteThrow(float power, AccuracyZone accuracy)
        {
            if (presenter == null || hole == null)
                return;

            HideThrowDistanceCallout();

            bool isPutt = _throwFromPutting;
            _throwFromPutting = false;
            _pendingPutOutcome = isPutt;

            if (isPutt)
                bag.SelectIndex(0);

            SyncArcHeightToAim();
            var height = aimAdjust.PlannedHeight;
            var aim = aimAdjust.AimDirection(hole, _discPosition);
            AccuracyMeterZones.ApplyToThrow(ref aim, ref power, accuracy);
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
            bool holed = presenter != null && presenter.LastFlightHoled;

            if (presenter != null)
            {
                if (holed)
                {
                    _discPosition = presenter.LandedPosition;
                }
                else
                {
                    float originGroundY = wps != null && wps.Count > 0 ? wps[0].Position.y : _discPosition.y;
                    _discPosition = DiscLieGround.SnapLie(presenter.LandedPosition, originGroundY);
                    presenter.SetPosition(_discPosition);
                }
            }
            else if (wps != null && wps.Count > 0)
                _discPosition = DiscLieGround.SnapLie(wps[wps.Count - 1].Position, wps[0].Position.y);

            _cameraDirector?.HoldLandingCameraUntilThrowSummary();
            _state.Advance(); // InFlight → Landed

            EndMeterFlightDisplay();

            if (IsDiscHoled(_discPosition))
            {
                BeginHoleComplete();
                return;
            }

            _pendingRestFt = hole != null ? hole.DistanceToBasket(_discPosition) : float.PositiveInfinity;
            _pendingAllowPutting = allowEnterPutting;
            _pendingWasPut = _pendingPutOutcome;
            _pendingPutOutcome = false;
            _postThrowPending = true;
            _throwPresentationReady = false;
            _cameraDirector?.ClearTrajectoryZoom();
            ApplyThrowPresentationVisibility();

            if (_postThrowRoutine != null)
                StopCoroutine(_postThrowRoutine);

            _postThrowRoutine = StartCoroutine(PostThrowRoutine(completedPath));
        }

        IEnumerator PostThrowRoutine(FlightPath completedPath)
        {
            float wait = ShowLandingCallout(completedPath);
            yield return new WaitForSeconds(wait);

            HideLandingCallout();

            yield return CompletePostThrowTransitionRoutine();
            _postThrowRoutine = null;
        }

        float ShowLandingCallout(FlightPath completedPath)
        {
            const float defaultWait = 2.25f;
            float wait = defaultWait;

            bool onGreen = IsDiscOnGreenSurface(_discPosition)
                || (_pendingAllowPutting && hole != null && _pendingRestFt <= hole.CircleRadiusFt);

            if (!onGreen)
                _showOnGreenLandingCallout = true;

            if (onGreen && _showOnGreenLandingCallout)
            {
                var onGreenCallout = ResolveCalloutHost()?.OnTheGreen;
                if (onGreenCallout != null)
                {
                    onGreenCallout.ShowBriefly(ScoreBannerSprites.OnTheGreen);
                    wait = onGreenCallout.DisplaySeconds;
                }
                else
                {
                    onTheGreenBanner ??= OnTheGreenBannerUI.Ensure();
                    onTheGreenBanner?.Show();
                    wait = onTheGreenBanner != null ? onTheGreenBanner.DisplaySeconds : defaultWait;
                }

                _showOnGreenLandingCallout = false;
            }
            else if (hole != null && !hole.IsNearTee(_discPosition))
            {
                float fallbackGroundY = _discPosition.y - DiscLieGround.DiscRestLift;
                var lie = DiscLieGround.SampleLieType(_discPosition, fallbackGroundY);
                if (lie is LieType.Fairway or LieType.Rough)
                {
                    var lieCallout = ResolveCalloutHost()?.LieLanding;
                    var lieSprite = lie == LieType.Fairway
                        ? ScoreBannerSprites.Fairway
                        : ScoreBannerSprites.Rough;
                    if (lieCallout != null && lieSprite != null)
                    {
                        lieCallout.Show(lieSprite);
                        wait = lieCallout.DisplaySeconds;
                    }
                    else
                    {
                        lieLandingBanner ??= LieLandingBannerUI.Ensure();
                        lieLandingBanner?.Show(lie);
                        wait = lieLandingBanner != null ? lieLandingBanner.DisplaySeconds : defaultWait;
                    }
                }
            }

            if (completedPath != null)
            {
                if (ResolveCalloutHost()?.ThrowDistance != null)
                {
                    calloutHost.ThrowDistance.ApplyFeetStyle();
                    calloutHost.ThrowDistance.ShowThrowDistance(completedPath.TotalDistanceFt);
                }
                else
                {
                    throwResultBanner ??= ThrowResultBannerUI.Ensure();
                    throwResultBanner?.ShowThrowDistance(completedPath.TotalDistanceFt);
                }
            }

            if (onGreen && ResolveCalloutHost()?.OnTheGreen != null && calloutHost.OnTheGreen.gameObject.activeSelf)
                calloutHost.OnTheGreen.transform.SetAsLastSibling();
            else if (onGreen && onTheGreenBanner != null && onTheGreenBanner.gameObject.activeSelf)
                onTheGreenBanner.transform.SetAsLastSibling();
            else if (ResolveCalloutHost()?.LieLanding != null && calloutHost.LieLanding.gameObject.activeSelf)
                calloutHost.LieLanding.transform.SetAsLastSibling();
            else if (lieLandingBanner != null && lieLandingBanner.gameObject.activeSelf)
                lieLandingBanner.transform.SetAsLastSibling();

            return wait;
        }

        void HideLandingCallout()
        {
            HideThrowDistanceCallout();
            HideOnTheGreenBanner();
            HideLieLandingBanner();
        }

        IEnumerator CompletePostThrowTransitionRoutine()
        {
            if (!_postThrowPending)
                yield break;

            _postThrowPending = false;

            if (_pendingWasPut)
            {
                yield return ResolvePutOutcomeRoutine(_pendingRestFt);
                yield break;
            }

            _throwPresentationReady = false;
            ApplyThrowPresentationVisibility();

            RelocateThrowerForNextShot();

            bool onGreen = IsDiscOnGreenSurface(_discPosition)
                || (_pendingAllowPutting && hole != null && _pendingRestFt <= hole.CircleRadiusFt);
            if (onGreen)
                _state.EnterPutting();
            else
                ResumeAimingFromLanded();
        }

        static bool IsDiscOnGreenSurface(Vector3 discPosition)
        {
            float fallbackGroundY = discPosition.y - DiscLieGround.DiscRestLift;
            return DiscLieGround.SampleLieType(discPosition, fallbackGroundY) == LieType.Green;
        }

        void ResumeAimingFromLanded()
        {
            if (_state.Phase != ThrowPhase.Landed)
                return;

            EnableCircleBanner(false);
            _state.TransitionTo(ThrowPhase.Aiming);
        }

        IEnumerator ResolvePutOutcomeRoutine(float restFt)
        {
            if (hole != null && (restFt <= HoledToleranceFt || IsDiscHoled(_discPosition)))
            {
                BeginHoleComplete();
                yield break;
            }

            _throwPresentationReady = false;
            ApplyThrowPresentationVisibility();

            RelocateThrowerForNextShot();

            if (hole != null && (IsDiscOnGreenSurface(_discPosition) || restFt <= hole.CircleRadiusFt))
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

        void BeginHoleComplete()
        {
            _postThrowPending = false;

            if (_postThrowRoutine != null)
            {
                StopCoroutine(_postThrowRoutine);
                _postThrowRoutine = null;
            }

            if (_throwPresentationRoutine != null)
            {
                StopCoroutine(_throwPresentationRoutine);
                _throwPresentationRoutine = null;
            }

            HideThrowDistanceCallout();
            EndMeterFlightDisplay();
            HideOnTheGreenBanner();
            HideLieLandingBanner();
            HideThrowSummaryBanner();
            _throwPresentationReady = false;
            ApplyThrowPresentationVisibility();

            _holeCompletePending = true;

            if (_holeCompleteRoutine != null)
                StopCoroutine(_holeCompleteRoutine);

            _holeCompleteRoutine = StartCoroutine(HoleCompleteSequence());
        }

        IEnumerator HoleCompleteSequence()
        {
            float delay = Mathf.Max(0f, basketCelebrationDelaySeconds);
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            PlayBasketChainSfx();

            var hostCutscene = ResolveCalloutHost()?.HoleCutscene;
            if (hostCutscene != null)
                hostCutscene.Show(_strokeCount, HolePar, holeCompleteCutsceneSeconds);
            else
            {
                holeCompleteCutscene ??= HoleCompleteCutsceneUI.Ensure();
                holeCompleteCutscene?.Show(_strokeCount, HolePar, holeCompleteCutsceneSeconds);
            }

            float wait = hostCutscene != null
                ? hostCutscene.DisplaySeconds
                : holeCompleteCutscene != null
                    ? holeCompleteCutscene.DisplaySeconds
                    : holeCompleteCutsceneSeconds;
            yield return new WaitForSeconds(Mathf.Max(0f, wait));

            hostCutscene?.Hide();
            holeCompleteCutscene?.Hide();
            var holeCompleteCallout = ResolveCalloutHost()?.HoleComplete;
            if (holeCompleteCallout != null)
                holeCompleteCallout.Hide();
            else
                holeCompleteBanner?.Hide();

            _holeCompleteRoutine = null;
            ResetHole();
        }

        void PlayBasketChainSfx()
        {
            var clip = basketChainSfx != null
                ? basketChainSfx
                : Resources.Load<AudioClip>("Audio/BasketChain");

            GameplayAudio.PlayOneShot(clip);
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
                    BeginThrowPresentationSequence();

                    break;
                case ThrowPhase.Resolve:
                    powerMeter?.Stop();

                    EnableCircleBanner(false);

                    break;
                case ThrowPhase.Aiming:
                    powerMeter?.Stop();
                    heightMeter?.Stop();

                    EnableCircleBanner(false);
                    ClearUiSelectionForAim();

                    if (hole != null && bag != null)
                    {
                        bag.SelectForDistance(hole.DistanceForDiscSelection(_discPosition));
                        _trackedDisc = bag.Active;
                    }

                    input?.ResetArcHeight();

                    if (hole != null && bag?.Active != null)
                        aimAdjust.ResetForLie(hole, _discPosition, bag.Active);

                    SyncArcHeightToAim();
                    BeginThrowPresentationSequence();

                    break;
            }
        }

        void BeginThrowPresentationSequence()
        {
            _throwPresentationReady = false;
            _cameraDirector?.ClearTrajectoryZoom();
            ApplyThrowPresentationVisibility();

            if (_throwPresentationRoutine != null)
                StopCoroutine(_throwPresentationRoutine);

            _throwPresentationRoutine = StartCoroutine(ThrowPresentationSequence());
        }

        IEnumerator ThrowPresentationSequence()
        {
            _cameraDirector?.ReleaseLandingCameraHold();

            var summaryHost = ResolveCalloutHost()?.ThrowSummary;
            if (summaryHost == null)
                throwSummaryBanner ??= ThrowSummaryBannerUI.Ensure();

            var summary = summaryHost ?? throwSummaryBanner;
            if (summary == null)
            {
                _throwPresentationReady = true;
                ApplyThrowPresentationVisibility();
                SyncDiscToHand();
                _throwPresentationRoutine = null;
                yield break;
            }

            bool dismissed = false;
            summary.ShowBriefly(
                _strokeCount + 1,
                GameSessionSettings.ActiveCharacter,
                () => dismissed = true);

            while (!dismissed)
                yield return null;

            _throwPresentationReady = true;
            ApplyThrowPresentationVisibility();
            SyncDiscToHand();
            _throwPresentationRoutine = null;
        }

        void ApplyThrowPresentationVisibility()
        {
            if (hole?.Thrower == null)
                return;

            if (_state.Phase is ThrowPhase.InFlight or ThrowPhase.Throwing)
            {
                hole.Thrower.gameObject.SetActive(true);
                return;
            }

            if (_postThrowPending || _state.Phase == ThrowPhase.Landed)
            {
                hole.Thrower.gameObject.SetActive(false);
                return;
            }

            bool inPreThrow = IsPreThrowPhase || _state.Phase == ThrowPhase.Putting;
            hole.Thrower.gameObject.SetActive(_throwPresentationReady && inPreThrow);
        }

        void HideThrowSummaryBanner()
        {
            if (ResolveCalloutHost()?.ThrowSummary != null)
                calloutHost.ThrowSummary.Hide();
            else
            {
                throwSummaryBanner ??= ThrowSummaryBannerUI.Ensure();
                throwSummaryBanner?.Hide();
            }
        }

        void EnableCircleBanner(bool on)
        {
            if (on)
            {
                if (calloutHost?.OnTheGreen != null)
                    calloutHost.OnTheGreen.ShowBriefly(ScoreBannerSprites.OnTheGreen);
                else
                {
                    onTheGreenBanner ??= OnTheGreenBannerUI.Ensure();
                    onTheGreenBanner?.ShowBriefly();
                }

                return;
            }

            HideOnTheGreenBanner();
            HideLieLandingBanner();
        }

        void HideOnTheGreenBanner()
        {
            if (ResolveCalloutHost()?.OnTheGreen != null)
                calloutHost.OnTheGreen.Hide();
            else
            {
                onTheGreenBanner ??= OnTheGreenBannerUI.Ensure();
                onTheGreenBanner?.Hide();
            }

            if (inTheCircleBanner != null)
                inTheCircleBanner.SetActive(false);
        }

        void HideLieLandingBanner()
        {
            if (ResolveCalloutHost()?.LieLanding != null)
                calloutHost.LieLanding.Hide();
            else
            {
                lieLandingBanner ??= LieLandingBannerUI.Ensure();
                lieLandingBanner?.Hide();
            }
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

            if (_throwPresentationRoutine != null)
            {
                StopCoroutine(_throwPresentationRoutine);
                _throwPresentationRoutine = null;
            }

            HideThrowDistanceCallout();
            if (ResolveCalloutHost()?.HoleComplete != null)
                calloutHost.HoleComplete.Hide();
            else
                holeCompleteBanner?.Hide();

            if (ResolveCalloutHost()?.HoleCutscene != null)
                calloutHost.HoleCutscene.Hide();
            else
                holeCompleteCutscene?.Hide();
            HideThrowSummaryBanner();
            HideLieLandingBanner();
            _cameraDirector?.ClearLandingCameraHold();
            _throwPresentationReady = false;
            ApplyThrowPresentationVisibility();
            sweetSpotBanner?.Hide();

            if (_sweetBannerRoutine != null)
            {
                StopCoroutine(_sweetBannerRoutine);
                _sweetBannerRoutine = null;
            }

            _strokeCount = 0;
            _throwFromPutting = false;
            _showOnGreenLandingCallout = true;
            _cameraDirector?.ClearTrajectoryZoom();

            if (hole != null)
            {
                _wind = hole.RollWind();
                hole.PositionThrowerAtTee();
                _discPosition = hole.TeePosition + Vector3.up * DiscLieGround.DiscRestLift;

                input?.ResetArcHeight();

                if (bag != null)
                {
                    bag.SelectForDistance(hole.DistanceForDiscSelection(_discPosition));
                    _trackedDisc = bag.Active;
                }

                if (bag?.Active != null)
                    aimAdjust.ResetForLie(hole, _discPosition, bag.Active);

                SyncArcHeightToAim();
            }

            presenter?.SetPosition(_discPosition);

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
            var sweetHost = ResolveCalloutHost()?.SweetSpot;
            if (sweetHost != null)
            {
                sweetHost.ApplySweetSpotStyle();
                sweetHost.Show("SWEET!");
            }
            else
            {
                sweetSpotBanner ??= SweetSpotBannerUI.Ensure();
                sweetSpotBanner?.Show();
            }

            float wait = sweetHost != null ? sweetHost.DisplaySeconds : sweetSpotBanner != null ? sweetSpotBanner.DisplaySeconds : 2f;
            yield return new WaitForSeconds(wait);

            sweetHost?.Hide();
            sweetSpotBanner?.Hide();
            _sweetBannerRoutine = null;
        }

        GameplayCalloutHost ResolveCalloutHost()
        {
            calloutHost ??= GameplayCalloutHost.Ensure();
            calloutHost?.BindReferences();
            return calloutHost;
        }

        void HideThrowDistanceCallout()
        {
            if (ResolveCalloutHost()?.ThrowDistance != null)
                calloutHost.ThrowDistance.Hide();
            else
                throwResultBanner?.Hide();
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

        public Vector3 GetPreviewTargetWorld()
        {
            var path = GetPreviewPath();
            var wps = path?.Waypoints;

            if (wps != null && wps.Count > 0)
                return wps[wps.Count - 1].Position;

            if (hole == null || aimAdjust == null)
                return _discPosition;

            var aim = aimAdjust.AimDirection(hole, _discPosition);
            return _discPosition + aim * (aimAdjust.TargetDistanceFt * 0.3048f);
        }
    }
}
