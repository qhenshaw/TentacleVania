using UnityEngine;

namespace TentacleVania.Combat.Status
{
    public sealed class DamageOverTime
    {
        private float _endsAt = float.NegativeInfinity;
        private float _nextTick;

        public int Stacks { get; private set; }
        public float PerStack { get; private set; }
        public bool IsActive => Stacks > 0;
        public float DamagePerSecond => Stacks * PerStack;

        public float Remaining(float now) => IsActive ? Mathf.Max(0f, _endsAt - now) : 0f;

        public bool Apply(float perStack, float duration, int maxStacks, float now, float interval)
        {
            if (perStack <= 0f || duration <= 0f || maxStacks < 1)
                return false;
            var started = !IsActive;
            if (started)
            {
                PerStack = perStack;
                _nextTick = now + interval;
            }
            else
                PerStack = Mathf.Max(PerStack, perStack);
            Stacks = Mathf.Min(Stacks + 1, maxStacks);
            _endsAt = Mathf.Max(_endsAt, now + duration);
            return started;
        }

        public float Tick(float now, float interval)
        {
            if (!IsActive)
                return 0f;
            var damage = 0f;
            while (_nextTick <= now && _nextTick <= _endsAt + 1e-4f)
            {
                damage += DamagePerSecond * interval;
                _nextTick += interval;
            }
            if (_nextTick > _endsAt + 1e-4f)
                Clear();
            return damage;
        }

        public float Burst(float now)
        {
            var damage = DamagePerSecond * Remaining(now);
            Clear();
            return damage;
        }

        public void Clear()
        {
            Stacks = 0;
            PerStack = 0f;
            _endsAt = float.NegativeInfinity;
        }
    }
}
