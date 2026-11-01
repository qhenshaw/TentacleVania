using TentacleVania.Enemies.Traps;
using UnityEngine;

namespace TentacleVania.Enemies.Types
{
    public sealed class ZoneTrailEnemy : GuardEnemy
    {
        [Header("Hazard trail (design: leaves radiation zones)")]
        [SerializeField] private FireZone _zone;
        [SerializeField, Min(0.5f)] private float _interval = 4f;
        [SerializeField, Min(0.1f)] private float _radius = 2f;
        [SerializeField, Min(0.4f)] private float _warn = 0.8f;
        [SerializeField, Min(0.1f)] private float _duration = 6f;
        [SerializeField, Min(0f)] private float _damagePerSecond = 10f;

        private float _next;

        protected override void OnEnable()
        {
            base.OnEnable();
            _next = Time.time + _interval;
        }

        protected override void OnTick(float now, float dt)
        {
            base.OnTick(now, dt);
            if (_zone == null || now < _next || State == EnemyState.Spawning || State == EnemyState.Stunned || State == EnemyState.Dead || State == EnemyState.Consumed)
                return;
            _next = now + _interval;
            EnemyManager.PlaceFire(_zone, Position, Vector3.forward, _radius, 360f, _warn, _duration, _damagePerSecond, Health);
        }
    }
}
