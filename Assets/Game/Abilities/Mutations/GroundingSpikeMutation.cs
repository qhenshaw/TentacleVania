using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Grounding Spike")]
    public sealed class GroundingSpikeMutation : MutationDefinition
    {
        public override MutationEffect CreateEffect() => new Effect();

        private sealed class Effect : MutationEffect
        {
            private MutationHost _host;

            public override void Enable(MutationHost host)
            {
                _host = host;
                CombatEvents.HitLanded += OnHit;
            }

            public override void Disable(MutationHost host) => CombatEvents.HitLanded -= OnHit;

            private void OnHit(Health target, in DamageInfo info, in DamageResult result)
            {
                if (_host.Health == null || info.Attacker != _host.Health || target == _host.Health || (info.Flags & HitFlags.Heavy) == 0 || (info.Flags & (HitFlags.Reflected | HitFlags.ChainLightning)) != 0)
                    return;
                if (target.TryGetComponent<StatusEffects>(out var status))
                    status.ReleaseAnyStatic(_host.Health.Team);
            }
        }
    }
}
