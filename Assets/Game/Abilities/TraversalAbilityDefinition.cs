using UnityEngine;

namespace TentacleVania.Abilities
{
    [CreateAssetMenu(menuName = "TentacleVania/Abilities/Traversal Ability")]
    public sealed class TraversalAbilityDefinition : AbilityDefinition
    {
        public override bool SlotAbility => false;

        public override AbilityRuntime CreateRuntime(AbilityContext context) => null;
    }
}
