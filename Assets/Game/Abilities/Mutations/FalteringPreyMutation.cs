using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Faltering Prey")]
    public sealed class FalteringPreyMutation : MutationDefinition
    {
        [SerializeField, Min(0f)] private float _stun = 1f;

        public override MutationEffect CreateEffect() => new Effect(_stun);

        private sealed class Effect : MutationEffect
        {
            private readonly float _stun;
            private MutationHost _host;

            public Effect(float stun) => _stun = stun;

            public override void Enable(MutationHost host)
            {
                _host = host;
                CombatEvents.HitLanded += OnHit;
            }

            public override void Disable(MutationHost host) => CombatEvents.HitLanded -= OnHit;

            private void OnHit(Health target, in DamageInfo info, in DamageResult result)
            {
                if (target == _host.Health || !target.IsAlive || result.HealthDamage <= 0f || target.Max <= 0f)
                    return;
                if (!target.TryGetComponent<Consumable>(out var consumable) || consumable.IsBeingConsumed || consumable.Locked)
                    return;
                var threshold = consumable.EffectiveThreshold(Consumable.GrabThreshold);
                var before = (target.Current + result.HealthDamage) / target.Max;
                if (before >= threshold && target.Normalized < threshold && target.TryGetComponent<IStunnable>(out var stunnable))
                    stunnable.Stun(_stun);
            }
        }
    }
}
