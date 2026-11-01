using UnityEngine;

namespace TentacleVania.Abilities.Feats
{
    public sealed class FeatCounter
    {
        private readonly float[] _times;
        private readonly float _window;
        private int _count;

        public FeatCounter(int target, float window)
        {
            _times = new float[Mathf.Max(1, target)];
            _window = window;
        }

        public int Count => _count;
        public int Target => _times.Length;
        public bool Done => _count >= _times.Length;

        public void Set(int count) => _count = Mathf.Clamp(count, 0, _times.Length);

        public void Reset() => _count = 0;

        public int Record(float now)
        {
            if (_window > 0f)
            {
                var kept = 0;
                for (var i = 0; i < _count; i++)
                    if (now - _times[i] <= _window)
                        _times[kept++] = _times[i];
                _count = kept;
            }
            if (_count < _times.Length)
                _times[_count++] = now;
            return _count;
        }
    }
}
