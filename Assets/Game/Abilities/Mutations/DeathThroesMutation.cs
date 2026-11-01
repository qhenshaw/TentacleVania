using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Death Throes")]
    public sealed class DeathThroesMutation : MutationDefinition
    {
        [SerializeField, Range(0f, 1f)] private float _below = 0.3f;
        [SerializeField, Min(1f)] private float _damageScale = 1.5f;

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly DeathThroesMutation _data;
            private readonly ScaleModifier _modifier = new(320);

            public Effect(DeathThroesMutation data) => _data = data;

            public override void Enable(MutationHost host)
            {
                _modifier.HealthScale = 1f;
                if (host.Health != null)
                    host.Health.Outgoing.Add(_modifier);
            }

            public override void Disable(MutationHost host)
            {
                if (host.Health != null)
                    host.Health.Outgoing.Remove(_modifier);
            }

            public override void Tick(MutationHost host, float now) =>
                _modifier.HealthScale = host.Health != null && host.Health.IsAlive && host.Health.Normalized < _data._below ? _data._damageScale : 1f;
        }
    }
}
