using TentacleVania.Creature;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Overcharge Sac")]
    public sealed class OverchargeSacMutation : MutationDefinition
    {
        [SerializeField, Min(1f)] private float _capacity = 1.5f;

        public override MutationEffect CreateEffect() => new Effect(_capacity);

        private sealed class Effect : MutationEffect
        {
            private readonly float _scale;
            private CreatureCharge _charge;

            public Effect(float scale) => _scale = scale;

            public override void Enable(MutationHost host)
            {
                _charge = host.Health != null ? host.Health.GetComponent<CreatureCharge>() : null;
                if (_charge != null)
                    _charge.CapacityBonus *= _scale;
            }

            public override void Disable(MutationHost host)
            {
                if (_charge != null)
                    _charge.CapacityBonus /= _scale;
            }
        }
    }
}
