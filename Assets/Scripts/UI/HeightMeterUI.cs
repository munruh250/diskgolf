using DiskGolf.Core;
using DiskGolf.Flight;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    public class HeightMeterUI : MonoBehaviour
    {
        [SerializeField] Slider slider;

        [SerializeField] Text zoneLabel;

        TimingMeter _meter = new TimingMeter(1f);
        NtmHeightMeterVisual _visual;
        bool _active;
        bool _frozen;
        float _frozenDisplay;
        float _sweetCenter;
        float _sweetWidth;
        bool _hasSweetZone;

        public bool LastConfirmWasSweet { get; private set; }

        public bool IsFrozenForFlight => _frozen;

        void Awake()
        {
            if (slider != null)
                slider.gameObject.SetActive(false);
        }

        void Start()
        {
            _visual = TimingMeterHud.Height;
            _visual?.SetChromeVisible(true);
            _visual?.SetNeedleVisible(false);
        }

        public void Begin()
        {
            _visual ??= TimingMeterHud.Height;
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

            if (slider != null)
                slider.value = 0f;
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

            if (slider != null)
                slider.value = 0f;
        }

        bool IsInSweetZone(float display01) =>
            _hasSweetZone && Mathf.Abs(display01 - _sweetCenter) <= _sweetWidth * 0.5f;

        public void SetTargetZone(float center01, float width01)
        {
            _visual ??= TimingMeterHud.Height;
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

        void RefreshZoneLabel(ThrowHeight height)
        {
            if (zoneLabel == null)
                return;

            zoneLabel.text = height switch
            {
                ThrowHeight.Low => "LOW",
                ThrowHeight.Nice => "NICE",
                ThrowHeight.High => "HIGH",
                _ => "NICE"
            };
        }

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

            if (slider != null)
                slider.value = display;

            _visual?.SetIndicator(display);
            RefreshZoneLabel(HeightMeterZones.FromValue(_meter.Value));
        }
    }
}
