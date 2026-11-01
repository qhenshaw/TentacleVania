using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Brood Implant")]
    public sealed class BroodImplantMutation : MutationDefinition
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
                if (_host.Health == null || info.Attacker != _host.Health || target == _host.Health || info.Status == StatusKind.Parasite)
                    return;
                if ((info.Flags & (HitFlags.DamageOverTime | HitFlags.Reflected | HitFlags.ChainLightning)) != 0)
                    return;
                if (target.IsAlive && target.TryGetComponent<StatusEffects>(out var status))
                    status.Apply(StatusKind.Parasite, 0f, 0f, _host.Health.Team, 1f);
            }
        }
    }
}
