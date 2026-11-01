using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Retaliation Plating")]
    public sealed class RetaliationPlatingMutation : MutationDefinition
    {
        [SerializeField, Min(0f)] private float _reflectScale = 1f;

        public override MutationEffect CreateEffect() => new Effect(_reflectScale);

        private sealed class Effect : MutationEffect
        {
            private readonly float _scale;
            private MutationHost _host;

            public Effect(float scale) => _scale = scale;

            public override void Enable(MutationHost host)
            {
                _host = host;
                if (host.Health != null)
                    host.Health.Damaged += OnDamaged;
            }

            public override void Disable(MutationHost host)
            {
                if (host.Health != null)
                    host.Health.Damaged -= OnDamaged;
            }

            private void OnDamaged(Health target, in DamageInfo info, in DamageResult result)
            {
                if (_host.Consume == null || !_host.Consume.IsArmored || (info.Flags & (HitFlags.Reflected | HitFlags.Hazard | HitFlags.DamageOverTime)) != 0)
                    return;
                MutationStrikes.Thorns(target, info.Attacker, (result.HealthDamage + result.ArmorDamage) * _scale);
            }
        }
    }
}
