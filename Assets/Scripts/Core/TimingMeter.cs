using UnityEngine;

namespace DiskGolf.Core
{
    public class TimingMeter
    {
        readonly float _speed;
        float _value;
        int _direction = 1;

        public TimingMeter(float speed = 1f) => _speed = speed;

        public float Value => _value;

        public void Tick(float deltaTime)
        {
            _value += _direction * _speed * deltaTime;
            if (_value >= 1.1f)
            {
                _value = 1.1f;
                _direction = -1;
            }

            if (_value <= 0f)
            {
                _value = 0f;
                _direction = 1;
            }
        }

        public void Reset(float start = 0f)
        {
            _value = start;
            _direction = 1;
        }

        public float Confirm() => _value;
    }
}
