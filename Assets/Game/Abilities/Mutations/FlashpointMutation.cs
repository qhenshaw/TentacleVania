using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Flashpoint")]
    public sealed class FlashpointMutation : MutationDefinition
    {
        [SerializeField, Min(0f)] private float _burnScale = 1.5f;
        [SerializeField, Min(0f)] private float _machineStagger = 25f;
        [SerializeField] private EventReference _sound;

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly FlashpointMutation _data;
            private MutationHost _host;
            private bool _busy;

            public Effect(FlashpointMutation data) => _data = data;

            public override void Enable(MutationHost host)
            {
                _host = host;
                CombatEvents.HitLanded += OnHit;
            }

            public override void Disable(MutationHost host) => CombatEvents.HitLanded -= OnHit;

            private void OnHit(Health target, in DamageInfo info, in DamageResult result)
            {
                if (_busy || _host.Health == null || info.Attacker != _host.Health || target == _host.Health || info.Type != DamageType.Electric || (info.Flags & HitFlags.Reflected) != 0 || !target.IsAlive)
                    return;
                if (!target.TryGetComponent<StatusEffects>(out var status) || !status.IsActive(StatusKind.Burn))
                    return;
                var damage = status.Remaining(StatusKind.Burn) * status.BurnDamagePerSecond * _data._burnScale;
                status.ClearBurn();
                if (damage <= 0f)
                    return;
                _busy = true;
                MutationStrikes.Play(_data._sound, target.transform.position);
                var blast = DamageInfo.Create(damage, _host.Health.Team, DamageType.Fire, target.Bleeds ? 0f : _data._machineStagger);
                blast.Flags = HitFlags.Reflected;
                blast.Attacker = _host.Health;
                blast.Point = target.transform.position + Vector3.up;
                blast.Direction = info.Direction;
                target.TakeDamage(blast);
                _busy = false;
            }
        }
    }
}
