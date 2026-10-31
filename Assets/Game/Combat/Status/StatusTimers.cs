using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Combat.Status
{
    public sealed class StatusTimers
    {
        private const int Count = 3;

        private readonly float[] _endsAt = new float[Count];
        private readonly float[] _magnitude = new float[Count];

        public StatusTimers()
        {
            for (var i = 0; i < Count; i++)
                _endsAt[i] = float.NegativeInfinity;
        }

        public bool Apply(StatusKind kind, float magnitude, float duration, float now)
        {
            if (kind == StatusKind.None || duration <= 0f)
                return false;
            var index = (int)kind;
            var wasActive = IsActive(kind, now);
            _magnitude[index] = wasActive ? Mathf.Max(_magnitude[index], magnitude) : magnitude;
            _endsAt[index] = Mathf.Max(_endsAt[index], now + duration);
            return !wasActive;
        }

        public bool IsActive(StatusKind kind, float now) => kind != StatusKind.None && now < _endsAt[(int)kind];

        public float Magnitude(StatusKind kind, float now) => IsActive(kind, now) ? _magnitude[(int)kind] : 0f;

        public float Remaining(StatusKind kind, float now) => IsActive(kind, now) ? _endsAt[(int)kind] - now : 0f;

        public void Clear(StatusKind kind)
        {
            if (kind == StatusKind.None)
                return;
            _endsAt[(int)kind] = float.NegativeInfinity;
            _magnitude[(int)kind] = 0f;
        }

        public float MoveScale(float now) => IsActive(StatusKind.Root, now) ? 0f : Mathf.Clamp01(1f - Magnitude(StatusKind.Slow, now));
    }
}
