using TentacleVania.Combat;
using TentacleVania.Enemies.Traps;
using UnityEngine;

namespace TentacleVania.Enemies.Types
{
    public sealed class FrostSentryEnemy : TurretEnemy
    {
        [Header("Ice walls (design: raises ice walls that reshape the maze)")]
        [SerializeField] private IceWall _wall;
        [SerializeField, Min(1f)] private float _wallInterval = 9f;
        [SerializeField, Min(1f)] private float _wallWidth = 6f;
        [SerializeField, Min(1f)] private float _wallLifetime = 8f;
        [SerializeField, Min(1f)] private float _wallRange = 14f;
        [Tooltip("The wall rises this far from the creature, toward the sentry, so it cuts the approach without trapping it.")]
        [SerializeField, Min(1f)] private float _creatureClearance = 2.5f;

        private float _nextWall;

        public static bool WallSpot(Vector3 sentry, Vector3 creature, float clearance, float range, out Vector3 position, out Vector3 along)
        {
            var offset = sentry - creature;
            offset.y = 0f;
            var distance = offset.magnitude;
            position = creature;
            along = Vector3.forward;
            if (distance > range || distance < clearance + 1.5f)
                return false;
            var toSentry = offset / distance;
            position = creature + toSentry * clearance;
            along = Vector3.Cross(Vector3.up, toSentry);
            return true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _nextWall = Time.time + _wallInterval * 0.5f;
        }

        protected override void OnTick(float now, float dt)
        {
            base.OnTick(now, dt);
            if (_wall == null || now < _nextWall || State == EnemyState.Spawning || State == EnemyState.Stunned || State == EnemyState.Dead || State == EnemyState.Consumed)
                return;
            var creature = CombatTargets.Creature;
            if (creature == null || !creature.IsAlive || !WallSpot(Position, creature.transform.position, _creatureClearance, _wallRange, out var position, out var along))
                return;
            _nextWall = now + _wallInterval;
            EnemyManager.RaiseWall(_wall, new Vector3(position.x, Position.y, position.z), along, _wallWidth, _wallLifetime);
        }
    }
}
