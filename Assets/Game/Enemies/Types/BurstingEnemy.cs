using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Enemies.Traps;
using UnityEngine;

namespace TentacleVania.Enemies.Types
{
    public sealed class BurstingEnemy : Enemy
    {
        [Tooltip("Pooled acid burst left where it dies (not when consumed).")]
        [SerializeField] private AcidBurst _burst;
        [SerializeField, Min(0.1f)] private float _burstRadius = 2.5f;
        [SerializeField, Min(0.4f)] private float _burstFuse = 1f;
        [SerializeField, Min(0f)] private float _burstDamage = 18f;
        [SerializeField, Min(0f)] private float _burstStagger;
        [Tooltip("Above 0, the creature coming this close kills it (the burst's fuse starts); used by the infested corpse.")]
        [SerializeField, Min(0f)] private float _triggerRange;
        [Tooltip("Never moves or turns (hazard enemies).")]
        [SerializeField] private bool _stationary;

        public bool Stationary => _stationary;
        public float TriggerRange => _triggerRange;

        public static bool InTrigger(Vector3 self, Vector3 creature, float range)
        {
            if (range <= 0f)
                return false;
            var offset = creature - self;
            offset.y = 0f;
            return offset.sqrMagnitude <= range * range;
        }

        protected override void Awake()
        {
            base.Awake();
            if (_burst != null)
                EnemyManager.PrewarmBursts(_burst, 2);
            if (_stationary)
                BaseSpeed = 0f;
        }

        protected override Vector3 ChaseDestination(Vector3 targetPosition) => _stationary ? Position : targetPosition;

        protected override Vector3 CircleDestination(Vector3 targetPosition, float dt) => _stationary ? Position : base.CircleDestination(targetPosition, dt);

        protected override bool FaceTarget(float distance) => !_stationary && base.FaceTarget(distance);

        protected override void OnTick(float now, float dt)
        {
            if (_triggerRange <= 0f || State == EnemyState.Spawning || State == EnemyState.Dead || State == EnemyState.Consumed)
                return;
            var creature = CombatTargets.Creature;
            if (creature != null && creature.IsAlive && InTrigger(Position, creature.transform.position, _triggerRange))
                Health.Kill();
        }

        protected override void OnDeath()
        {
            if (DiedConsumed || _burst == null)
                return;
            var info = DamageInfo.Create(_burstDamage, Team.Site, DamageType.Acid, _burstStagger);
            info.Status = StatusKind.Corrode;
            info.Attacker = Health;
            EnemyManager.PlaceBurst(_burst, Position, _burstRadius, _burstFuse, info);
        }
    }
}
