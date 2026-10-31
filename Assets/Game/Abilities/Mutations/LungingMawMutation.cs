using TentacleVania.Combat;
using TentacleVania.Creature;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Lunging Maw")]
    public sealed class LungingMawMutation : MutationDefinition
    {
        public override MutationEffect CreateEffect() => new Effect();

        private sealed class Effect : MutationEffect
        {
            public override void Enable(MutationHost host)
            {
                if (host.Grapple != null)
                    host.Grapple.GrabOnArrival = true;
            }

            public override void Disable(MutationHost host)
            {
                if (host.Grapple != null)
                    host.Grapple.GrabOnArrival = false;
            }
        }
    }
}
