namespace TentacleVania.Enemies
{
    public sealed class VoiceLimiter
    {
        private readonly float[] _times;
        private readonly float _window;
        private int _next;

        public VoiceLimiter(int max, float window)
        {
            _times = new float[max];
            _window = window;
            Clear();
        }

        public void Clear()
        {
            for (var i = 0; i < _times.Length; i++)
                _times[i] = float.NegativeInfinity;
            _next = 0;
        }

        public bool TryUse(float now, bool important = false)
        {
            var oldest = _times[_next];
            if (!important && now - oldest < _window)
                return false;
            _times[_next] = now;
            _next = (_next + 1) % _times.Length;
            return true;
        }
    }
}
