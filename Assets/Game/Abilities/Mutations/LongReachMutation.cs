using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Long Reach")]
    public sealed class LongReachMutation : MutationDefinition
    {
        [SerializeField, Min(1f)] private float _reachScale = 1.5f;

        public override MutationEffect CreateEffect() => new Effect(_reachScale);

        private sealed class Effect : MutationEffect
        {
            private readonly float _scale;

            public Effect(float scale) => _scale = scale;

            public override void Enable(MutationHost host) => host.Consume.ReachScale *= _scale;

            public override void Disable(MutationHost host) => host.Consume.ReachScale /= _scale;
        }
    }
}
