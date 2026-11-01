using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Jagged Recoil")]
    public sealed class JaggedRecoilMutation : MutationDefinition
    {
        [SerializeField, Min(0f)] private float _staggerPerDamage = 1.5f;

        public override MutationEffect CreateEffect() => new Effect(_staggerPerDamage);

        private sealed class Effect : MutationEffect
        {
            private readonly float _stagger;

            public Effect(float stagger) => _stagger = stagger;

            public override void Enable(MutationHost host) => MutationStrikes.ThornsStaggerScale += _stagger;

            public override void Disable(MutationHost host) => MutationStrikes.ThornsStaggerScale = Mathf.Max(0f, MutationStrikes.ThornsStaggerScale - _stagger);
        }
    }
}
