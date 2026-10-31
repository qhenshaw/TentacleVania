namespace TentacleVania.Creature.Movement
{
    public sealed class DashTimer
    {
        private readonly float _duration;
        private readonly float _cooldown;
        private readonly int _chain;
        private readonly float _lockout;

        private float _readyAt = float.NegativeInfinity;
        private float _lastEnd = float.NegativeInfinity;
        private int _count;

        public DashTimer(float duration, float cooldown, int chain, float lockout)
        {
            _duration = duration;
            _cooldown = cooldown;
            _chain = chain;
            _lockout = lockout;
        }

        public DashTimer(CreatureMotorSettings settings)
            : this(settings.DashDuration, settings.DashCooldown, settings.DashChain, settings.DashLockout) { }

        public int ChainCount => _count;

        public bool CanDash(float now) => now >= _readyAt;

        public bool TryStart(float now, float recoveryScale = 1f)
        {
            if (!CanDash(now))
                return false;
            if (now - _lastEnd >= _lockout)
                _count = 0;
            _count++;
            _lastEnd = now + _duration;
            var wait = (_count >= _chain ? _lockout : _cooldown) * recoveryScale;
            if (_count >= _chain)
                _count = 0;
            _readyAt = _lastEnd + wait;
            return true;
        }
    }
}
