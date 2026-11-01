using TentacleVania.Combat;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Desperate Hunger")]
    public sealed class DesperateHungerMutation : MutationDefinition
    {
        [SerializeField, Range(0f, 1f)] private float _below = 0.3f;
        [SerializeField, Range(0f, 0.5f)] private float _bonus = 0.1f;

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly DesperateHungerMutation _data;
            private float _applied;

            public Effect(DesperateHungerMutation data) => _data = data;

            public override void Enable(MutationHost host) => _applied = 0f;

            public override void Disable(MutationHost host) => Set(0f);

            public override void Tick(MutationHost host, float now) =>
                Set(host.Health != null && host.Health.IsAlive && host.Health.Normalized < _data._below ? _data._bonus : 0f);

            private void Set(float bonus)
            {
                Consumable.ThresholdBonus += bonus - _applied;
                _applied = bonus;
            }
        }
    }
}
