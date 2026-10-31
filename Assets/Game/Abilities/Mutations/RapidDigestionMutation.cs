using TentacleVania.Combat;
using TentacleVania.Creature;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Rapid Digestion")]
    public sealed class RapidDigestionMutation : MutationDefinition
    {
        [SerializeField, Range(0.1f, 1f)] private float _healDuration = 0.5f;

        public override MutationEffect CreateEffect() => new Effect(_healDuration);

        private sealed class Effect : MutationEffect
        {
            private readonly float _scale;

            public Effect(float scale) => _scale = scale;

            public override void Enable(MutationHost host) => host.Consume.HealDurationScale = _scale;

            public override void Disable(MutationHost host) => host.Consume.HealDurationScale = 1f;
        }
    }
}
