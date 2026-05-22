using DiskGolf.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    public class PowerMeterUI : MonoBehaviour
    {
        [SerializeField] Slider slider;

        TimingMeter _meter = new TimingMeter(0.8f);
        bool _active;

        void Awake()
        {
            if (slider != null)
                slider.value = 0f;
        }

        public void Begin()
        {
            _meter.Reset();
            _active = true;
        }

        public void Stop() => _active = false;

        public float Confirm()
        {
            _active = false;
            return _meter.Confirm();
        }

        void Update()
        {
            if (!_active || slider == null) return;

            _meter.Tick(Time.deltaTime);
            slider.value = _meter.Value / 1.1f;
        }
    }
}
