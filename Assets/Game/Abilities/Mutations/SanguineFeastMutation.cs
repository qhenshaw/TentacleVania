using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Sanguine Feast")]
    public sealed class SanguineFeastMutation : MutationDefinition
    {
        [SerializeField, Min(0f)] private float _healPerStack = 0.02f;

        public override MutationEffect CreateEffect() => new Effect(_healPerStack);

        private sealed class Effect : MutationEffect
        {
            private readonly float _perStack;

            public Effect(float perStack) => _perStack = perStack;

            public override void Enable(MutationHost host) => host.Consume.BleedStackHeal += _perStack;

            public override void Disable(MutationHost host) => host.Consume.BleedStackHeal = Mathf.Max(0f, host.Consume.BleedStackHeal - _perStack);
        }
    }
}
