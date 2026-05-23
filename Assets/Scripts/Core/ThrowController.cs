using System;
using DiskGolf.Disc;
using DiskGolf.Flight;
using DiskGolf.Gameplay;
using DiskGolf.Input;
using DiskGolf.UI;
using UnityEngine;

namespace DiskGolf.Core
{
    public class ThrowController : MonoBehaviour
    {
        const float PuttMaxDistanceFt = 80f;

        /// <summary>Holed tolerance in feet (converted with HoleSetup distance).</summary>
        const float HoledToleranceFt = 1f;

        [SerializeField] HoleSetup hole;

        [SerializeField] DiscBag bag;

        [SerializeField] ThrowInputHandler input;

        [SerializeField] DiscFlightPresenter presenter;

        [SerializeField] PowerMeterUI powerMeter;

        [SerializeField] HeightMeterUI heightMeter;

        [SerializeField] GameObject inTheCircleBanner;

        readonly ThrowStateMachine _state = new ThrowStateMachine();
        WindSettings _wind;

        float _confirmedPower;

        Vector3 _discPosition;

        bool _pendingPutOutcome;

        public ThrowPhase Phase => _state.Phase;

        public event Action<ThrowPhase> PhaseChanged;

        public WindSettings Wind => _wind;

        public ReleaseAngle ReleaseAngle => input != null ? input.ReleaseAngle : ReleaseAngle.Flat;

        public DiscProfile ActiveDisc => bag != null ? bag.Active : null;

        public Vector3 CurrentDiscWorld => _discPosition;

        public bool IsPreThrowPhase => _state.Phase is ThrowPhase.Aiming
            or ThrowPhase.PowerMeter
            or ThrowPhase.HeightMeter;

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
            if (hole == null || presenter == null || !IsPreThrowPhase || hole.Thrower == null)
                return;

            SyncDiscToHand();
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

                        heightMeter?.Stop();
                    }

                    break;
                case ThrowPhase.Putting:
                    HandlePutting();

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

            if (input.CycleNext) bag.CycleNext();

            if (input.CyclePrev) bag.CyclePrev();

            var hk = input.DiscHotkey;

            if (hk >= 0) bag.SelectIndex(hk);

            if (input.ConfirmPressed)
            {
                powerMeter?.Begin();

                _state.Advance();
            }
        }

        void HandlePutting()
        {
            bag.SelectIndex(0);

            if (input.ConfirmPressed && powerMeter != null)
                ExecutePuttPower(powerMeter.Confirm());
        }

        void ExecuteThrow(float power, ThrowHeight height)
        {
            if (presenter == null || hole == null)
                return;

            _pendingPutOutcome = false;

            var aim = hole.AimDirectionFrom(_discPosition);

            var throwInput = new ThrowInput(
                bag.Active,
                input.ReleaseAngle,
                power,
                height,
                _wind,
                _discPosition,
                aim);

            var path = FlightSimulator.Compute(throwInput);

            heightMeter?.Stop();

            _state.Advance(); // HeightMeter → Throwing
            _state.Advance(); // Throwing → InFlight
            presenter.Play(path, OnFlightComplete);
        }

        void ExecutePuttPower(float rawMeterPower)
        {
            if (presenter == null || hole == null || bag == null)
                return;

            bag.SelectIndex(0);

            var putter = bag.Active;

            float power =
                ClampPuttPower(rawMeterPower, putter?.maxDistanceFt ?? PuttMaxDistanceFt);

            var aim = hole.AimDirectionFrom(_discPosition);

            var throwInput = new ThrowInput(
                putter,
                ReleaseAngle.Flat,
                power,
                ThrowHeight.Nice,
                _wind,
                _discPosition,
                aim);

            var path = FlightSimulator.Compute(throwInput);

            _pendingPutOutcome = true;

            powerMeter?.Stop();

            heightMeter?.Stop();

            _state.Advance(); // Putting → Throwing

            _state.Advance(); // Throwing → InFlight
            presenter.Play(path, OnPuttOutcomeComplete);
        }

        void OnFlightComplete(FlightPath completedPath)
            => FinishThrowCommon(completedPath, allowEnterPutting: true);

        void OnPuttOutcomeComplete(FlightPath completedPath)
            => FinishThrowCommon(completedPath, allowEnterPutting: false);

        void FinishThrowCommon(FlightPath completedPath, bool allowEnterPutting)
        {
            var wps = completedPath?.Waypoints;

            if (wps != null && wps.Count > 0)
                _discPosition = wps[wps.Count - 1].Position;

            _state.Advance(); // InFlight → Landed

            float restFt =
                hole != null ? hole.DistanceToBasket(_discPosition) : float.PositiveInfinity;

            bool wasPutOutcome = _pendingPutOutcome;

            _pendingPutOutcome = false;

            if (wasPutOutcome)
            {
                ResolvePutOutcome(restFt);

                return;
            }

            // Drive / upshot landed

            if (allowEnterPutting && hole != null && restFt <= hole.CircleRadiusFt)
            {
                _state.EnterPutting();

                return;
            }

            _state.Advance(); // Landed → Resolve

            _state.Advance(); // Resolve → Aiming
        }

        void ResolvePutOutcome(float restFt)
        {
            if (hole != null && restFt <= HoledToleranceFt)
            {
                Debug.Log("[Disk Golf] Made putt");

                presenter?.SetPosition(hole.BasketPosition);

                ResetHole();

                return;
            }

            if (hole != null && restFt <= hole.CircleRadiusFt)
                _state.EnterPutting();
            else
            {
                _state.Advance(); // Landed → Resolve

                _state.Advance(); // Resolve → Aiming
            }
        }

        void OnPhaseChangedInternal(ThrowPhase phase)
        {
            switch (phase)
            {
                case ThrowPhase.HeightMeter:
                    heightMeter?.Begin();

                    break;
                case ThrowPhase.Putting:
                    bag?.SelectIndex(0);
                    powerMeter?.Begin();
                    heightMeter?.Stop();
                    EnableCircleBanner(true);

                    if (hole != null)
                        hole.PositionThrowerAtLie(_discPosition);

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
                    {
                        if (hole.IsNearTee(_discPosition))
                            hole.PositionThrowerAtTee();
                        else
                            hole.PositionThrowerAtLie(_discPosition);

                        SyncDiscToHand();
                    }

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

            if (hole != null)
            {
                _wind = hole.RollWind();
                hole.PositionThrowerAtTee();
                _discPosition = hole.DiscHoldPosition;
            }

            presenter?.SetPositionAndRotation(hole.DiscHoldPosition, hole.DiscHoldRotation);

            heightMeter?.Stop();

            powerMeter?.Stop();

            EnableCircleBanner(false);

            _state.TransitionTo(ThrowPhase.Aiming);
        }

        public FlightPath GetPreviewPath()
        {
            if (hole == null || bag?.Active == null || input == null)
                return null;

            var aim = hole.AimDirectionFrom(_discPosition);

            return FlightSimulator.Compute(new ThrowInput(
                bag.Active,
                input.ReleaseAngle,
                1f,
                ThrowHeight.Nice,
                _wind,
                _discPosition,
                aim));
        }

        static float ClampPuttPower(float meterValue, float discMaxFt)
        {
            float denom = Mathf.Max(discMaxFt, 1e-4f);

            float capped = Mathf.Clamp(PuttMaxDistanceFt / denom, 0f, 1.1f);

            float v =
                Mathf.Clamp(meterValue, 0f, 1.1f);

            return Mathf.Min(v, capped);
        }
    }
}
