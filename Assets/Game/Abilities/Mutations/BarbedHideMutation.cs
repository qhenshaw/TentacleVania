using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Barbed Hide")]
    public sealed class BarbedHideMutation : MutationDefinition
    {
        [SerializeField, Min(0f)] private float _damage = 6f;

        public override MutationEffect CreateEffect() => new Effect(_damage);

        private sealed class Effect : MutationEffect
        {
            private readonly float _damage;

            public Effect(float damage) => _damage = damage;

            public override void Enable(MutationHost host)
            {
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
                if (MutationStrikes.IsMelee(target, info))
                    MutationStrikes.Thorns(target, info.Attacker, _damage);
            }
        }
    }
}
