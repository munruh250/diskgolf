using DiskGolf.Core;
using UnityEngine;

namespace DiskGolf.UI
{
    public class PowerMeterUI : MonoBehaviour
    {
        TimingMeter _meter = new TimingMeter(0.8f);
        PowerMeterVisual _visual;
        bool _active;
        bool _frozen;
        float _frozenDisplay;
        float _sweetCenter;
        float _sweetWidth;
        bool _hasSweetZone;

        public bool LastConfirmWasSweet { get; private set; }

        public bool IsFrozenForFlight => _frozen;

        void Start()
        {
            _visual = TimingMeterHud.Power;
            _visual?.SetChromeVisible(true);
            _visual?.SetNeedleVisible(false);
        }

        public void Begin()
        {
            _visual ??= TimingMeterHud.Power;
            _meter.Reset();
            _active = true;
            _visual?.SetChromeVisible(true);
            _visual?.SetNeedleVisible(true);
        }

        public void Stop()
        {
            _active = false;
            _frozen = false;
            _hasSweetZone = false;
            LastConfirmWasSweet = false;
            ClearTargetZone();
            _visual?.SetNeedleVisible(false);
        }

        public bool IsRunning => _active;

        public float Confirm()
        {
            _active = false;
            _frozen = true;
            _frozenDisplay = _meter.Value / 1.1f;
            LastConfirmWasSweet = IsInSweetZone(_frozenDisplay);
            _visual?.SetIndicator(_frozenDisplay);
            _visual?.SetNeedleVisible(true);
            return _meter.Confirm();
        }

        public void EndFlightDisplay()
        {
            _frozen = false;
            _hasSweetZone = false;
            LastConfirmWasSweet = false;
            ClearTargetZone();
            _visual?.SetNeedleVisible(false);
        }

        bool IsInSweetZone(float display01) =>
            _hasSweetZone && Mathf.Abs(display01 - _sweetCenter) <= _sweetWidth * 0.5f;

        public void SetTargetZone(float center01, float width01)
        {
            _visual ??= TimingMeterHud.Power;
            _sweetCenter = center01;
            _sweetWidth = width01;
            _hasSweetZone = true;
            _visual?.SetSweetSpot(center01, width01);
        }

        public void PreviewTargetZone(float center01, float width01)
        {
            if (_active)
                return;

            SetTargetZone(center01, width01);
        }

        public void ClearTargetZone() => _visual?.HideSweetSpot();

        void Update()
        {
            if (_frozen)
            {
                _visual?.SetIndicator(_frozenDisplay);
                return;
            }

            if (!_active)
                return;

            _meter.Tick(Time.deltaTime);
            float display = _meter.Value / 1.1f;
            _visual?.SetIndicator(display);
        }
    }
}
