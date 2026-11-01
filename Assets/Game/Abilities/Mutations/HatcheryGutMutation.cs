using TentacleVania.Combat;
using TentacleVania.Combat.Status;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Hatchery Gut")]
    public sealed class HatcheryGutMutation : MutationDefinition
    {
        [SerializeField, Min(1)] private int _minions = 2;

        public override MutationEffect CreateEffect() => new Effect(_minions);

        private sealed class Effect : MutationEffect
        {
            private readonly int _minions;
            private MutationHost _host;

            public Effect(int minions) => _minions = minions;

            public override void Enable(MutationHost host)
            {
                _host = host;
                CombatEvents.Consumed += OnConsumed;
            }

            public override void Disable(MutationHost host) => CombatEvents.Consumed -= OnConsumed;

            private void OnConsumed(Consumable victim)
            {
                if (_host.Health != null && victim != null)
                    ParasiteSwarm.Spawn(_host.Health.transform.position, _minions, _host.Health.Team);
            }
        }
    }
}
