using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Leech Tendrils")]
    public sealed class LeechTendrilsMutation : MutationDefinition
    {
        [SerializeField, Min(0f)] private float _healPerHit = 0.5f;

        public override MutationEffect CreateEffect() => new Effect(_healPerHit);

        private sealed class Effect : MutationEffect
        {
            private readonly float _heal;
            private MutationHost _host;

            public Effect(float heal) => _heal = heal;

            public override void Enable(MutationHost host)
            {
                _host = host;
                CombatEvents.HitLanded += OnHit;
            }

            public override void Disable(MutationHost host) => CombatEvents.HitLanded -= OnHit;

            private void OnHit(Health target, in DamageInfo info, in DamageResult result)
            {
                if (_host.Health == null || info.Attacker != _host.Health || target == _host.Health || result.HealthDamage + result.ArmorDamage <= 0f)
                    return;
                if ((info.Flags & (HitFlags.DamageOverTime | HitFlags.Reflected | HitFlags.Hazard)) == 0)
                    _host.Health.Heal(_heal);
            }
        }
    }
}
