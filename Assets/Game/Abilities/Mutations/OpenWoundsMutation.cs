using TentacleVania.Combat.Status;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Open Wounds")]
    public sealed class OpenWoundsMutation : MutationDefinition
    {
        [SerializeField, Min(0)] private int _extraStacks = 2;
        [SerializeField, Min(1f)] private float _rate = 1.3f;

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly OpenWoundsMutation _data;

            public Effect(OpenWoundsMutation data) => _data = data;

            public override void Enable(MutationHost host)
            {
                StatusEffects.BleedExtraStacks += _data._extraStacks;
                StatusEffects.BleedRateScale *= _data._rate;
            }

            public override void Disable(MutationHost host)
            {
                StatusEffects.BleedExtraStacks = Mathf.Max(0, StatusEffects.BleedExtraStacks - _data._extraStacks);
                StatusEffects.BleedRateScale /= _data._rate;
            }
        }
    }
}
