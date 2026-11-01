using TentacleVania.Combat.Damage;
using TentacleVania.Enemies;
using TentacleVania.Enemies.AI;
using UnityEngine;

namespace TentacleVania.Bosses.Vance
{
    public sealed class VanceBoss : BossEnemy
    {
        private sealed class RiotShield : IDamageModifier
        {
            public VanceBoss Owner;
            public int Order => 940;

            public void Modify(ref DamageContext context)
            {
                if (Owner.ShieldUp && !Owner.IsStaggerBroken && Owner.Blocks(context.Info))
                    context.HealthScale = 0f;
            }
        }

        [Header("Riot shield (design: Vance fights with a shield and shock baton)")]
        [Tooltip("Hits from within this angle of his facing are blocked while the shield is up.")]
        [SerializeField, Range(0f, 180f)] private float _shieldArc = 120f;
        [Tooltip("Boss phase index from which the shield is gone (the last phase drops it).")]
        [SerializeField, Min(0)] private int _shieldDropPhase = 2;
        [SerializeField] private GameObject _shieldModel;

        [Header("Shield charge")]
        [SerializeField] private string _chargeAttack = "Shield Charge";
        [SerializeField, Min(0f)] private float _chargeSpeed = 16f;

        private readonly RiotShield _shield = new();

        public bool ShieldUp => Phase < _shieldDropPhase;

        protected override void Awake()
        {
            base.Awake();
            _shield.Owner = this;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Health.Incoming.Add(_shield);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Health.Incoming.Remove(_shield);
        }

        protected override void OnTick(float now, float dt)
        {
            base.OnTick(now, dt);
            if (_shieldModel != null && _shieldModel.activeSelf != ShieldUp)
                _shieldModel.SetActive(ShieldUp);
            if (State == EnemyState.Attacking && CurrentAttack != null && CurrentAttack.Name == _chargeAttack
                && CurrentPhase == AttackPhase.Active && Agent.enabled && Agent.isOnNavMesh)
                Agent.Move(transform.forward * (_chargeSpeed * dt));
        }

        public bool Blocks(in DamageInfo info)
        {
            if ((info.Flags & (HitFlags.Hazard | HitFlags.DamageOverTime)) != 0)
                return false;
            var toAttacker = info.Attacker != null ? info.Attacker.transform.position - transform.position : -info.Direction;
            toAttacker.y = 0f;
            return toAttacker.sqrMagnitude > 1e-4f && Vector3.Angle(transform.forward, toAttacker) <= _shieldArc * 0.5f;
        }
    }
}
