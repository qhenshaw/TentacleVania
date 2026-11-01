using TentacleVania.Combat;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Dissolving Grip")]
    public sealed class DissolvingGripMutation : MutationDefinition
    {
        [SerializeField, Range(0f, 1f)] private float _threshold = 0.4f;

        public override MutationEffect CreateEffect() => new Effect(_threshold);

        private sealed class Effect : MutationEffect
        {
            private readonly float _threshold;

            public Effect(float threshold) => _threshold = threshold;

            public override void Enable(MutationHost host) => Consumable.CorrodedThreshold = _threshold;

            public override void Disable(MutationHost host) => Consumable.CorrodedThreshold = 0f;
        }
    }
}
