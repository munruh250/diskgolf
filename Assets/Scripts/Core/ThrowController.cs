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
        [SerializeField] HoleSetup hole;

        [SerializeField] DiscBag bag;

        [SerializeField] ThrowInputHandler input;

        [SerializeField] DiscFlightPresenter presenter;

        [SerializeField] PowerMeterUI powerMeter;

        [SerializeField] HeightMeterUI heightMeter;

        readonly ThrowStateMachine _state = new ThrowStateMachine();
        WindSettings _wind;

        float _confirmedPower;

        Vector3 _discPosition;

        public ThrowPhase Phase => _state.Phase;

        public event Action<ThrowPhase> PhaseChanged;

        public WindSettings Wind => _wind;

        public ReleaseAngle ReleaseAngle => input != null ? input.ReleaseAngle : ReleaseAngle.Flat;

        public DiscProfile ActiveDisc => bag != null ? bag.Active : null;

        void Start()
        {
            _state.PhaseChanged += p => PhaseChanged?.Invoke(p);
            _state.PhaseChanged += OnPhaseChanged;
            ResetHole();
        }

        void OnDestroy()
        {
            _state.PhaseChanged -= OnPhaseChanged;
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
                    HandleAiming();

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
                        var heightRaw = heightMeter != null ? heightMeter.Confirm() : 0.55f;
                        var height = HeightMeterZones.FromValue(heightRaw);
                        ExecuteThrow(_confirmedPower, height);
                    }

                    break;
            }
        }

        void HandleAiming()
        {
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

        void ExecuteThrow(float power, ThrowHeight height)
        {
            if (presenter == null || hole == null)
                return;

            var throwInput = new ThrowInput(
                bag.Active,
                input.ReleaseAngle,
                power,
                height,
                _wind,
                _discPosition,
                hole.AimDirection);

            var path = FlightSimulator.Compute(throwInput);
            _state.Advance(); // HeightMeter → Throwing
            _state.Advance(); // Throwing → InFlight (during path playback)
            presenter.Play(path, OnFlightComplete);
        }

        void OnFlightComplete(FlightPath completedPath)
        {
            var wps = completedPath?.Waypoints;

            if (wps != null && wps.Count > 0)
                _discPosition = wps[wps.Count - 1].Position;

            _state.Advance(); // InFlight → Landed

            var rest = hole != null ? hole.DistanceToBasket(_discPosition) : float.PositiveInfinity;

            if (rest <= hole.CircleRadiusFt)
                _state.EnterPutting();
            else
            {
                _state.Advance(); // Landed → Resolve
                _state.Advance(); // Resolve → Aiming
            }
        }

        void OnPhaseChanged(ThrowPhase phase)
        {
            switch (phase)
            {
                case ThrowPhase.HeightMeter:
                    heightMeter?.Begin();

                    break;
                case ThrowPhase.Putting:
                    // Putting flow expanded in Task 13

                    break;
            }
        }

        public void ResetHole()
        {
            if (hole != null)
            {
                _wind = hole.RollWind();
                _discPosition = hole.TeePosition;
            }

            presenter?.SetPosition(_discPosition);
            heightMeter?.Stop();
            powerMeter?.Stop();

            _state.TransitionTo(ThrowPhase.Aiming);
        }

        public FlightPath GetPreviewPath()
        {
            return FlightSimulator.Compute(new ThrowInput(
                bag.Active,
                input.ReleaseAngle,
                1f,
                ThrowHeight.Nice,
                _wind,
                _discPosition,
                hole.AimDirection));
        }
    }
}
