using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Backdraft")]
    public sealed class BackdraftMutation : MutationDefinition
    {
        [SerializeField, Min(0.1f)] private float _radius = 3f;
        [SerializeField, Min(0f)] private float _damage = 10f;
        [SerializeField] private EventReference _sound;

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly BackdraftMutation _data;
            private MutationHost _host;
            private bool _busy;

            public Effect(BackdraftMutation data) => _data = data;

            public override void Enable(MutationHost host)
            {
                _host = host;
                CombatEvents.HitLanded += OnHit;
            }

            public override void Disable(MutationHost host) => CombatEvents.HitLanded -= OnHit;

            private void OnHit(Health target, in DamageInfo info, in DamageResult result)
            {
                if (_busy || !result.Killed || _host.Health == null || target == _host.Health || target.Team == _host.Health.Team)
                    return;
                if (!target.TryGetComponent<StatusEffects>(out var status) || !status.IsActive(StatusKind.Burn))
                    return;
                _busy = true;
                var center = target.transform.position;
                MutationStrikes.Play(_data._sound, center);
                MutationStrikes.Ring(_host.Health, center, _data._radius, _data._damage, 0f, DamageType.Fire, HitFlags.Reflected, StatusKind.Burn, target);
                _busy = false;
            }
        }
    }
}
