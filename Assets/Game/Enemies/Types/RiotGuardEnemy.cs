using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Enemies.Types
{
    public sealed class RiotGuardEnemy : Enemy
    {
        private sealed class RiotShield : IDamageModifier
        {
            public RiotGuardEnemy Owner;
            public int Order => 940;

            public void Modify(ref DamageContext context)
            {
                if (Owner.IsStaggerBroken || !Owner.Blocks(context.Info))
                    return;
                context.HealthScale = 0f;
                Owner.BlockedHits++;
            }
        }

        [Tooltip("Hits from within this angle of the guard's facing are blocked by the riot shield (design: flank or stagger it).")]
        [SerializeField, Range(0f, 180f)] private float _shieldArc = 140f;

        private readonly RiotShield _shield = new();

        public int BlockedHits { get; private set; }

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
