using TentacleVania.Combat.Status;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Arc Conductor")]
    public sealed class ArcConductorMutation : MutationDefinition
    {
        [SerializeField, Min(1f)] private float _buildScale = 1.5f;
        [SerializeField, Min(0)] private int _extraTargets = 2;

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly ArcConductorMutation _data;

            public Effect(ArcConductorMutation data) => _data = data;

            public override void Enable(MutationHost host)
            {
                StatusEffects.StaticBuildScale *= _data._buildScale;
                StatusEffects.ExtraChainTargets += _data._extraTargets;
            }

            public override void Disable(MutationHost host)
            {
                StatusEffects.StaticBuildScale /= _data._buildScale;
                StatusEffects.ExtraChainTargets = Mathf.Max(0, StatusEffects.ExtraChainTargets - _data._extraTargets);
            }
        }
    }
}
