using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Enemies.Types
{
    public sealed class AmbushEnemy : Enemy
    {
        [Header("Ambush (design: frozen in an ice block, breaks free as the creature passes)")]
        [SerializeField] private GameObject _dormantShell;
        [SerializeField, Min(0f)] private float _wakeRange = 3.5f;
        [SerializeField] private EventReference _wakeSound;

        private bool _dormant;
        private float _speed;

        public bool Dormant => _dormant;

        protected override void OnEnable()
        {
            base.OnEnable();
            _dormant = true;
            _speed = BaseSpeed;
            if (_dormantShell != null)
                _dormantShell.SetActive(true);
            Health.Damaged += OnHit;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Health.Damaged -= OnHit;
        }

        private void OnHit(Health health, in DamageInfo info, in DamageResult result) => Wake();

        protected override bool AttackAllowed(int index) => !_dormant;

        protected override Vector3 ChaseDestination(Vector3 targetPosition) => _dormant ? Position : targetPosition;

        protected override Vector3 CircleDestination(Vector3 targetPosition, float dt) => _dormant ? Position : base.CircleDestination(targetPosition, dt);

        protected override bool FaceTarget(float distance) => !_dormant && base.FaceTarget(distance);

        protected override void OnTick(float now, float dt)
        {
            if (!_dormant)
                return;
            BaseSpeed = 0f;
            var creature = CombatTargets.Creature;
            if (State != EnemyState.Spawning && creature != null && creature.IsAlive && BurstingEnemy.InTrigger(Position, creature.transform.position, _wakeRange))
                Wake();
        }

        private void Wake()
        {
            if (!_dormant)
                return;
            _dormant = false;
            BaseSpeed = _speed;
            if (_dormantShell != null)
                _dormantShell.SetActive(false);
            if (!_wakeSound.IsNull)
                RuntimeManager.PlayOneShot(_wakeSound, Position);
        }
    }
}
