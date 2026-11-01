using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Rending Edges")]
    public sealed class RendingEdgesMutation : MutationDefinition
    {
        private const HitFlags NotCombo = HitFlags.Heavy | HitFlags.Finisher | HitFlags.DashAttack | HitFlags.Ability | HitFlags.DamageOverTime | HitFlags.Reflected | HitFlags.Hazard | HitFlags.ChainLightning;

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
                if (_host.Health == null || info.Attacker != _host.Health || target == _host.Health || (info.Flags & NotCombo) != 0 || info.Status == StatusKind.Bleed)
                    return;
                if (target.IsAlive && target.TryGetComponent<StatusEffects>(out var status))
                    status.Apply(StatusKind.Bleed, 0f, 0f, _host.Health.Team, 1f);
            }
        }
    }
}
