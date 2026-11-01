using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Piercing Spines")]
    public sealed class PiercingSpinesMutation : MutationDefinition
    {
        public override MutationEffect CreateEffect() => new Effect();

        private sealed class Effect : MutationEffect
        {
            public override void Enable(MutationHost host) { }

            public override void Disable(MutationHost host) { }
        }
    }
}
