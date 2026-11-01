using FMODUnity;
using TentacleVania.Enemies;
using TentacleVania.Enemies.Data;
using TentacleVania.Enemies.Traps;
using UnityEngine;

namespace TentacleVania.Bosses.Reyes
{
    public sealed class ReyesBoss : BossEnemy
    {
        [Header("Fire zones (design: safe ground keeps shrinking and shifting)")]
        [SerializeField] private FireZone _fire;
        [SerializeField, Min(0f)] private float _fireDamagePerSecond = 12f;

        [Header("Napalm Grenades")]
        [SerializeField] private string _napalmAttack = "Napalm Grenade";
        [SerializeField, Min(0.1f)] private float _poolRadius = 2.2f;
        [SerializeField, Min(0.1f)] private float _poolDuration = 7f;

        [Header("Fire Slice (phase 2)")]
        [SerializeField, Min(0)] private int _slicePhase = 1;
        [SerializeField, Min(1f)] private float _sliceInterval = 7f;
        [SerializeField, Min(1f)] private float _sliceRadius = 14f;
        [SerializeField, Range(10f, 180f)] private float _sliceArc = 45f;
        [SerializeField, Min(0.4f)] private float _sliceWarn = 1.2f;
        [SerializeField, Min(0.1f)] private float _sliceDuration = 6f;
        [SerializeField] private EventReference _sliceSound;

        private float _nextSlice;
        private int _slices;

        public int Slices => _slices;

        protected override void OnEnable()
        {
            base.OnEnable();
            _nextSlice = float.MaxValue;
            _slices = 0;
        }

        protected override void OnActiveStart(EnemyAttack attack)
        {
            base.OnActiveStart(attack);
            if (_fire != null && attack.Name == _napalmAttack)
                EnemyManager.PlaceFire(_fire, AreaCenter, Vector3.forward, _poolRadius, 360f, attack.BurstFuse, _poolDuration, _fireDamagePerSecond, Health);
        }

        protected override void OnTick(float now, float dt)
        {
            base.OnTick(now, dt);
            if (_fire == null || Phase < _slicePhase || !Health.IsAlive || IsFinishing || State == EnemyState.Spawning || State == EnemyState.Dead || State == EnemyState.Consumed)
                return;
            if (_nextSlice == float.MaxValue)
                _nextSlice = now + 1f;
            if (now < _nextSlice || State == EnemyState.Attacking || State == EnemyState.Stunned)
                return;
            _nextSlice = now + _sliceInterval;
            var direction = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
            transform.rotation = Quaternion.LookRotation(direction);
            EnemyManager.PlaceFire(_fire, Position, direction, _sliceRadius, _sliceArc, _sliceWarn, _sliceDuration, _fireDamagePerSecond, Health);
            _slices++;
            if (!_sliceSound.IsNull)
                RuntimeManager.PlayOneShot(_sliceSound, Position);
        }
    }
}
