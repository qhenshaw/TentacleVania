using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Creature;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Capacitor Organ")]
    public sealed class CapacitorOrganMutation : MutationDefinition
    {
        [SerializeField, Min(1f)] private float _gainScale = 1.3f;
        [SerializeField, Min(0f)] private float _perElectricHit = 2f;

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly CapacitorOrganMutation _data;
            private MutationHost _host;
            private CreatureCharge _charge;

            public Effect(CapacitorOrganMutation data) => _data = data;

            public override void Enable(MutationHost host)
            {
                _host = host;
                _charge = host.Health != null ? host.Health.GetComponent<CreatureCharge>() : null;
                if (_charge != null)
                    _charge.GainBonus *= _data._gainScale;
                CombatEvents.HitLanded += OnHit;
            }

            public override void Disable(MutationHost host)
            {
                CombatEvents.HitLanded -= OnHit;
                if (_charge != null)
                    _charge.GainBonus /= _data._gainScale;
            }

            private void OnHit(Health target, in DamageInfo info, in DamageResult result)
            {
                if (_charge != null && info.Attacker == _host.Health && target != _host.Health && info.Type == DamageType.Electric && (info.Flags & HitFlags.Reflected) == 0 && result.Applied)
                    _charge.Add(_data._perElectricHit);
            }
        }
    }
}
