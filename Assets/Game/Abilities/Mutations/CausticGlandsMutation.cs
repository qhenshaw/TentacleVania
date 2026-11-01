using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Caustic Glands")]
    public sealed class CausticGlandsMutation : MutationDefinition
    {
        [SerializeField, Min(0.1f)] private float _radius = 1.5f;
        [SerializeField, Min(0.1f)] private float _duration = 3f;
        [SerializeField, Min(0f)] private float _damagePerSecond = 4f;
        [SerializeField, Min(0f)] private float _cooldown = 0.4f;

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly CausticGlandsMutation _data;
            private MutationHost _host;
            private float _ready;

            public Effect(CausticGlandsMutation data) => _data = data;

            public override void Enable(MutationHost host)
            {
                _host = host;
                _ready = 0f;
                CombatEvents.HitLanded += OnHit;
            }

            public override void Disable(MutationHost host) => CombatEvents.HitLanded -= OnHit;

            private void OnHit(Health target, in DamageInfo info, in DamageResult result)
            {
                if (_host.Health == null || info.Attacker != _host.Health || target == _host.Health || Time.time < _ready)
                    return;
                if ((info.Flags & (HitFlags.DashAttack | HitFlags.Heavy)) == 0 || (info.Flags & (HitFlags.Reflected | HitFlags.DamageOverTime | HitFlags.ChainLightning)) != 0)
                    return;
                _ready = Time.time + _data._cooldown;
                var position = target.transform.position;
                position.y = _host.Health.transform.position.y;
                AcidPools.Spawn(position, _host.Health.Team, _data._radius, _data._duration, _data._damagePerSecond);
            }
        }
    }
}
