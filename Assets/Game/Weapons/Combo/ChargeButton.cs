using UnityEngine;

namespace TentacleVania.Weapons.Combo
{
    public enum ChargeEvent
    {
        None,
        Tap,
        Heavy,
    }

    public sealed class ChargeButton
    {
        private readonly float _threshold;
        private readonly float _windUp;

        private bool _down;
        private bool _queued;
        private float _pressedAt;

        public ChargeButton(float threshold, float windUp)
        {
            _threshold = threshold;
            _windUp = windUp;
        }

        public bool IsDown => _down;

        public bool IsCharging(float now) => _queued || (_down && now - _pressedAt >= _threshold);

        public float Charge(float now) => _down ? (now - _pressedAt) / _threshold : 0f;

        public float WindUp(float now)
        {
            if (!IsCharging(now))
                return 0f;
            return _windUp > 0f ? Mathf.Clamp01((now - _pressedAt - _threshold) / _windUp) : 1f;
        }

        public ChargeEvent Update(bool pressed, bool held, float now)
        {
            if (_queued)
            {
                if (now - _pressedAt < _threshold + _windUp)
                    return ChargeEvent.None;
                _queued = false;
                return ChargeEvent.Heavy;
            }
            if (pressed)
            {
                _down = true;
                _pressedAt = now;
                return ChargeEvent.None;
            }
            if (!_down || held)
                return ChargeEvent.None;
            _down = false;
            var heldFor = now - _pressedAt;
            if (heldFor < _threshold)
                return ChargeEvent.Tap;
            if (heldFor >= _threshold + _windUp)
                return ChargeEvent.Heavy;
            _queued = true;
            return ChargeEvent.None;
        }

        public void Cancel()
        {
            _down = false;
            _queued = false;
        }
    }
}
