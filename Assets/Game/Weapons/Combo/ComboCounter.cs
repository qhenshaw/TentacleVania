namespace TentacleVania.Weapons.Combo
{
    public sealed class ComboCounter
    {
        private readonly int _steps;
        private readonly float _resetDelay;

        private int _next;
        private float _lastAttackEnd = float.NegativeInfinity;

        public ComboCounter(int steps, float resetDelay)
        {
            _steps = steps;
            _resetDelay = resetDelay;
        }

        public int Peek(float now) => now - _lastAttackEnd > _resetDelay ? 0 : _next;

        public int Advance(float now)
        {
            var step = Peek(now);
            _next = (step + 1) % _steps;
            return step;
        }

        public bool IsFinisher(int step) => step == _steps - 1;

        public void AttackEnded(float now) => _lastAttackEnd = now;

        public void Reset() => _next = 0;
    }
}
