using TentacleVania.Combat;
using TentacleVania.Combat.Status;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Hemorrhage")]
    public sealed class HemorrhageMutation : MutationDefinition
    {
        [SerializeField, Min(0f)] private float _burstScale = 1f;

        public override MutationEffect CreateEffect() => new Effect(_burstScale);

        private sealed class Effect : MutationEffect
        {
            private readonly float _scale;
            private MutationHost _host;

            public Effect(float scale) => _scale = scale;

            public override void Enable(MutationHost host)
            {
                _host = host;
                CombatEvents.StaggerBroken += OnBroken;
            }

            public override void Disable(MutationHost host) => CombatEvents.StaggerBroken -= OnBroken;

            private void OnBroken(Health target)
            {
                if (target == null || _host.Health == null || target.Team == _host.Health.Team)
                    return;
                if (target.TryGetComponent<StatusEffects>(out var status) && status.BleedStacks > 0)
                    status.BurstBleed(_scale);
            }
        }
    }
}
