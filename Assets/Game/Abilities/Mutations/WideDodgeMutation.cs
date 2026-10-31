using TentacleVania.Combat;
using TentacleVania.Creature;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Wide Dodge")]
    public sealed class WideDodgeMutation : MutationDefinition
    {
        [SerializeField, Min(0f)] private float _dashInvulnerability = 0.3f;

        public override MutationEffect CreateEffect() => new Effect(_dashInvulnerability);

        private sealed class Effect : MutationEffect
        {
            private readonly float _seconds;

            public Effect(float seconds) => _seconds = seconds;

            public override void Enable(MutationHost host) => host.Motor.MinDashInvulnerability = Mathf.Max(host.Motor.MinDashInvulnerability, _seconds);

            public override void Disable(MutationHost host) => host.Motor.MinDashInvulnerability = 0f;
        }
    }
}
