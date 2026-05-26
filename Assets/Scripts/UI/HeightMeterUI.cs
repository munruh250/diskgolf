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
        MeterTargetZoneUI _targetZone;
        bool _active;

        void Awake()
        {
            if (slider != null)
                slider.value = 0f;

            EnsureTargetZone();
            RefreshZoneLabel(ThrowHeight.Nice);
        }

        public void Begin()
        {
            _meter.Reset();
            _active = true;
        }

        public void Stop()
        {
            _active = false;
            ClearTargetZone();
        }

        public float Confirm()
        {
            _active = false;
            ClearTargetZone();
            return _meter.Confirm();
        }

        public void SetTargetZone(float center01, float width01)
        {
            EnsureTargetZone();
            _targetZone?.SetZone(center01, width01);
        }

        public void ClearTargetZone() => _targetZone?.Hide();

        void EnsureTargetZone()
        {
            if (_targetZone != null)
                return;

            _targetZone = GetComponent<MeterTargetZoneUI>();
            if (_targetZone == null)
                _targetZone = gameObject.AddComponent<MeterTargetZoneUI>();
        }

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
            if (!_active || slider == null)
                return;

            _meter.Tick(Time.deltaTime);
            slider.value = _meter.Value / 1.1f;
            RefreshZoneLabel(HeightMeterZones.FromValue(_meter.Value));
        }
    }
}
