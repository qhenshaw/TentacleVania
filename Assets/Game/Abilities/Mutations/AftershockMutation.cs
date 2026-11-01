using System.Collections.Generic;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Aftershock")]
    public sealed class AftershockMutation : MutationDefinition
    {
        [SerializeField, Min(0f)] private float _extend = 1f;

        public override MutationEffect CreateEffect() => new Effect(_extend);

        private sealed class Effect : MutationEffect
        {
            private readonly HashSet<Health> _armed = new();
            private readonly float _extend;
            private MutationHost _host;

            public Effect(float extend) => _extend = extend;

            public override void Enable(MutationHost host)
            {
                _host = host;
                CombatEvents.StaggerBroken += OnBroken;
                CombatEvents.HitLanded += OnHit;
            }

            public override void Disable(MutationHost host)
            {
                CombatEvents.StaggerBroken -= OnBroken;
                CombatEvents.HitLanded -= OnHit;
                _armed.Clear();
            }

            private void OnBroken(Health target)
            {
                if (target != null && _host.Health != null && target.Team != _host.Health.Team)
                    _armed.Add(target);
            }

            private void OnHit(Health target, in DamageInfo info, in DamageResult result)
            {
                if (info.Attacker != _host.Health || (info.Flags & HitFlags.Heavy) == 0 || !_armed.Remove(target))
                    return;
                if (target.IsAlive && target.IsStaggered && target.TryGetComponent<IStunnable>(out var stunnable))
                    stunnable.ExtendStun(_extend);
            }
        }
    }
}
