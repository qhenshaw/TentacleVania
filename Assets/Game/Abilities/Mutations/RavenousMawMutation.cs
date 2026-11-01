using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Ravenous Maw")]
    public sealed class RavenousMawMutation : MutationDefinition
    {
        [SerializeField, Min(1f)] private float _healScale = 1.25f;

        public override MutationEffect CreateEffect() => new Effect(_healScale);

        private sealed class Effect : MutationEffect
        {
            private readonly float _scale;
            private readonly float _lowScale;

            public Effect(float scale)
            {
                _scale = scale;
                _lowScale = (1f + 2f * (scale - 1f)) / scale;
            }

            public override void Enable(MutationHost host)
            {
                host.Consume.HealScale *= _scale;
                host.Consume.LowHealthHealScale *= _lowScale;
            }

            public override void Disable(MutationHost host)
            {
                host.Consume.HealScale /= _scale;
                host.Consume.LowHealthHealScale /= _lowScale;
            }
        }
    }
}
