using TentacleVania.Combat;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Galvanic Overload")]
    public sealed class GalvanicOverloadMutation : MutationDefinition
    {
        [SerializeField, Min(1f)] private float _machineStagger = 2f;

        public override MutationEffect CreateEffect() => new Effect(_machineStagger);

        private sealed class Effect : MutationEffect
        {
            private readonly float _scale;

            public Effect(float scale) => _scale = scale;

            public override void Enable(MutationHost host) => StaggerRules.MachineElectricScale *= _scale;

            public override void Disable(MutationHost host) => StaggerRules.MachineElectricScale /= _scale;
        }
    }
}
