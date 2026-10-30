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
        private readonly float _maxHold;

        private bool _down;
        private float _pressedAt;

        public ChargeButton(float threshold, float maxHold)
        {
            _threshold = threshold;
            _maxHold = maxHold;
        }

        public bool IsDown => _down;

        public bool IsCharging(float now) => _down && now - _pressedAt >= _threshold;

        public float Charge(float now) => _down ? (now - _pressedAt) / _threshold : 0f;

        public ChargeEvent Update(bool pressed, bool held, float now)
        {
            if (pressed)
            {
                _down = true;
                _pressedAt = now;
                return ChargeEvent.None;
            }
            if (!_down)
                return ChargeEvent.None;
            var heldFor = now - _pressedAt;
            if (!held)
            {
                _down = false;
                return heldFor >= _threshold ? ChargeEvent.Heavy : ChargeEvent.Tap;
            }
            if (heldFor >= _maxHold)
            {
                _down = false;
                return ChargeEvent.Heavy;
            }
            return ChargeEvent.None;
        }

        public void Cancel() => _down = false;
    }
}
