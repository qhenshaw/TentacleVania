using TentacleVania.Enemies;
using UnityEngine;

namespace TentacleVania.World.Hazards
{
    public sealed class FloorPattern : MonoBehaviour
    {
        public enum PatternState
        {
            Idle,
            Gap,
            Warning,
            On,
        }

        [Tooltip("Electric floor zones, disabled until their pattern fires.")]
        [SerializeField] private HazardZone[] _zones = System.Array.Empty<HazardZone>();
        [Tooltip("Each pattern is a bitmask over the zones (bit i = zone i); cycled in order.")]
        [SerializeField] private int[] _patterns = { 0b010101, 0b101010, 0b001100, 0b110011 };
        [SerializeField, Min(0f)] private float _gapTime = 3f;
        [SerializeField, Min(0.1f)] private float _warningTime = 1.2f;
        [SerializeField, Min(0.1f)] private float _onTime = 2.5f;
        [SerializeField, Min(1f)] private float _blinkRate = 8f;
        [Tooltip("A boss this close runs the pattern (bosses spawn into the sector root scene, not the room scene).")]
        [SerializeField, Min(1f)] private float _bossRange = 40f;

        private Transform[] _surfaces;
        private float _phaseEnds;
        private int _pattern = -1;

        public PatternState State { get; private set; } = PatternState.Idle;
        public int CurrentMask => _pattern >= 0 && _patterns.Length > 0 ? _patterns[_pattern] : 0;

        public void Configure(HazardZone[] zones, int[] patterns)
        {
            _zones = zones;
            if (patterns != null && patterns.Length > 0)
                _patterns = patterns;
            Awake();
        }

        private void Awake()
        {
            _surfaces = new Transform[_zones.Length];
            for (var i = 0; i < _zones.Length; i++)
                _surfaces[i] = _zones[i] != null ? _zones[i].transform.Find("Surface") : null;
            Apply(0, false, false);
        }

        private void Update()
        {
            var now = Time.time;
            if (!BossFightRunning())
            {
                if (State != PatternState.Idle)
                {
                    State = PatternState.Idle;
                    Apply(0, false, false);
                }
                return;
            }
            if (State == PatternState.Idle)
            {
                State = PatternState.Gap;
                _phaseEnds = now + _gapTime;
            }
            if (now >= _phaseEnds)
            {
                switch (State)
                {
                    case PatternState.Gap:
                        _pattern = (_pattern + 1) % Mathf.Max(1, _patterns.Length);
                        State = PatternState.Warning;
                        _phaseEnds = now + _warningTime;
                        break;
                    case PatternState.Warning:
                        State = PatternState.On;
                        _phaseEnds = now + _onTime;
                        Apply(CurrentMask, true, true);
                        break;
                    default:
                        State = PatternState.Gap;
                        _phaseEnds = now + _gapTime;
                        Apply(0, false, false);
                        break;
                }
            }
            if (State == PatternState.Warning)
                Apply(CurrentMask, Mathf.Repeat(now * _blinkRate, 1f) < 0.5f, false);
        }

        private void Apply(int mask, bool visible, bool live)
        {
            for (var i = 0; i < _zones.Length; i++)
            {
                var chosen = (mask & (1 << i)) != 0;
                if (_zones[i] != null)
                    _zones[i].enabled = live && chosen;
                if (_surfaces != null && i < _surfaces.Length && _surfaces[i] != null)
                    _surfaces[i].gameObject.SetActive(visible && chosen);
            }
        }

        private bool BossFightRunning()
        {
            var manager = EnemyManager.Current;
            if (manager == null)
                return false;
            var enemies = manager.Enemies;
            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy.IsBoss && enemy.Health.IsAlive && enemy.State != EnemyState.Spawning && enemy.State != EnemyState.Dead
                    && (enemy.transform.position - transform.position).sqrMagnitude <= _bossRange * _bossRange)
                    return true;
            }
            return false;
        }
    }
}
