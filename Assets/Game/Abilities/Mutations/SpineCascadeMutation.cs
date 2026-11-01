using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Spine Cascade")]
    public sealed class SpineCascadeMutation : MutationDefinition
    {
        public override MutationEffect CreateEffect() => new Effect();

        private sealed class Effect : MutationEffect
        {
            public override void Enable(MutationHost host) { }

            public override void Disable(MutationHost host) { }
        }
    }
}
